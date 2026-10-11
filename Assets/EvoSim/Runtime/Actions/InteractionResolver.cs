using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Applies the interactions of 07 §3 after moving (ACT-10, ACT-11): only within reach, only if the target is
    /// edible and the eater's diet lists it (SPEC-13). Hunters strike at most once per tick (ACT-12).
    /// </summary>
    /// <remarks>
    /// <para>The act phase makes one at initialization, with the reaches of its Inspector, and calls <see cref="Apply"/>
    /// for every animal that planned an interaction, right after that animal moved: in the act order for the sequential
    /// orders, in a fresh random order for the simultaneous one (ACT-30). Tests make their own.</para>
    /// <para>Every meal goes through the same steps: the eater's <see cref="Energy.Gain"/> (after
    /// <see cref="Diet.EnergyFrom"/>), one more meal on the animal and its species, and, after a kill or a carcass
    /// portion, <see cref="Digestion.AfterMeal"/>. A strike's kill chance is drawn from the hunter species'
    /// <c>actions</c> stream.</para>
    /// </remarks>
    public sealed class InteractionResolver
    {
        readonly World world;                       // whose diets, services, queries and streams it uses
        readonly InteractionContext context;        // handed to every custom interaction

        /// <summary>
        /// The reaches, in meters (ACT-10): graze (also for eggs), strike and scavenge; reference 0.5, 1 and 1. The act
        /// phase sets them from its own settings.
        /// </summary>
        public float GrazeReach = 0.5f, StrikeReach = 1f, ScavengeReach = 1f;

        /// <summary>Creates a resolver for a world, with the reference reaches.</summary>
        /// <param name="world">The world whose diets, carcasses, eggs and random streams it uses.</param>
        public InteractionResolver(World world)
        {
            this.world = world;
            context = new InteractionContext(world);
        }

        /// <summary>Applies one planned interaction for an animal that has just moved; returns whether something happened.</summary>
        /// <param name="a">The animal that acted; nothing happens if it was killed earlier in the tick or is gone.</param>
        /// <param name="i">The interaction its action planned.</param>
        /// <param name="target">The point its intent was heading to: a custom interaction's reach is measured from there.</param>
        /// <returns>
        /// True when something happened: a meal, a strike (even one that did not kill) or a custom effect; false when out
        /// of reach, not allowed by the diet, or the food is already gone.
        /// </returns>
        public bool Apply(Animal a, in Interaction i, Vector3 target)
        {
            if (a.Killed || a.IsGone) return false;
            switch (i.Kind)
            {
                case InteractionKind.Graze: return Graze(a, i.Item);
                case InteractionKind.Strike: return Strike(a, i.Target);
                case InteractionKind.Scavenge: return Scavenge(a, i.Carcass);
                case InteractionKind.EatEgg: return EatEgg(a, i.Egg);
                case InteractionKind.Custom:
                    if (i.Custom == null || !Within(world.Distance(a.Position, target), i.Custom.Reach)) return false;
                    i.Custom.Apply(a, context);
                    return true;
                default: return false;
            }
        }

        /// <summary>True when a distance is within a reach, with a 1 mm tolerance (<see cref="Units.Epsilon"/>).</summary>
        static bool Within(float distance, float reach) => distance <= reach + Units.Epsilon;

        /// <summary>
        /// Grazes an item: the diet must graze its layer and the layer be edible, the animal must be within the graze
        /// reach, and the item must not have been eaten earlier in the tick (ENV-02). No digestion.
        /// </summary>
        bool Graze(Animal a, ResourceItem item)
        {
            var layer = item.Layer;
            var diet = a.Species.Module<Diet>();
            var entry = layer != null ? diet?.Grazing(layer) : null;
            if (entry == null) return false;
            if (!Within(world.Distance(a.Position, item.Position), GrazeReach)) return false;
            if (!layer.Consume(item)) return false;                         // eaten by someone earlier (ENV-02)
            Eat(a, diet.EnergyFrom(a, entry, entry.Energy), digest: false);
            return true;
        }

        /// <summary>
        /// Strikes a prey: the diet must strike its species, the prey must be alive, within the strike reach and not hidden
        /// from the hunter in cover (ENV-11), and the hunter must not have struck yet this tick (ACT-12). With the kill
        /// chance the prey is killed, a carcass may appear, and the hunter eats and digests.
        /// </summary>
        bool Strike(Animal hunter, Animal prey)
        {
            if (prey == null || hunter.StruckThisTick || prey == hunter || prey.Killed || prey.IsGone) return false;
            var diet = hunter.Species.Module<Diet>();
            var entry = diet?.Striking(prey.Species);
            if (entry == null) return false;
            if (world.Queries.IsHiddenFrom(prey, hunter.Species)) return false;     // ENV-11: never struck in cover
            if (!Within(world.Distance(hunter.Position, prey.Position), StrikeReach)) return false;
            hunter.StruckThisTick = true;                                           // ACT-12
            float killP = diet.KillChanceAgainst(hunter, prey);
            if (!world.Random.For(hunter.Species, "actions").Chance(killP)) return true;   // missed: the strike is used all the same
            prey.Killed = true;                                                     // ANIM-41: marked now, removed in the death phase
            prey.KilledBy = hunter.Id;
            hunter.Species.Counters.Kills++;
            world.Service<CarcassSystem>()?.Create(prey, hunter.Id);
            Eat(hunter, diet.EnergyFrom(hunter, entry, entry.Energy), digest: true);
            return true;
        }

        /// <summary>
        /// Eats one portion of a carcass the diet scavenges, within the scavenge reach; its killer and the animals that
        /// already ate from it may not (03 §3). The eater digests.
        /// </summary>
        bool Scavenge(Animal a, Carcass c)
        {
            if (c == null || c.UsedUp) return false;
            var diet = a.Species.Module<Diet>();
            var entry = diet?.Scavenging(c.Of);
            var cs = world.Service<CarcassSystem>();
            if (entry == null || cs == null) return false;
            if (!Within(world.Distance(a.Position, c.Position), ScavengeReach)) return false;
            float gain = diet.EnergyFrom(a, entry, c.EnergyPerPortion * entry.EnergyScale);
            if (!cs.EatPortion(c, a)) return false;
            a.Species.Counters.Portions++;
            Eat(a, gain, digest: true);
            return true;
        }

        /// <summary>
        /// Eats an egg the diet eats, within the graze reach: the egg is lost with the reason "eaten" and never hatches
        /// (REPRO-23). No digestion.
        /// </summary>
        bool EatEgg(Animal a, Egg egg)
        {
            if (egg == null || egg.UsedUp) return false;
            var diet = a.Species.Module<Diet>();
            var entry = diet?.EatingEggs(egg.Species);
            var eggs = world.Service<EggSystem>();
            if (entry == null || eggs == null) return false;
            if (!Within(world.Distance(a.Position, egg.Position), GrazeReach)) return false;
            eggs.Lose(egg, "eaten");                                                // REPRO-23: never hatches, recorded
            Eat(a, diet.EnergyFrom(a, entry, entry.Energy), digest: false);
            return true;
        }

        /// <summary>
        /// Feeds an animal: energy gained (the stat stays at or below its maximum), one meal more for the animal and its
        /// species, and digestion after it when <paramref name="digest"/> is true. Also behind <see cref="InteractionContext.Feed"/>.
        /// </summary>
        internal static void Eat(Animal a, float gain, bool digest)
        {
            a.Species.Module<Energy>()?.Gain(a, gain);
            a.Meals++;
            a.Species.Counters.Meals++;
            if (digest) a.Species.Module<Digestion>()?.AfterMeal(a, gain);
        }
    }
}
