using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Applies the interactions of 07 §3 after moving (ACT-10, ACT-11): only within reach, only if the target is
    /// edible and the eater's diet lists it (SPEC-13). Hunters strike at most once per tick (ACT-12).
    /// </summary>
    public sealed class InteractionResolver
    {
        readonly World world;
        readonly InteractionContext context;

        public float GrazeReach = 0.5f, StrikeReach = 1f, ScavengeReach = 1f;

        public InteractionResolver(World world)
        {
            this.world = world;
            context = new InteractionContext(world);
        }

        /// <summary>Applies one planned interaction for an animal that has just moved; returns whether something happened.</summary>
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

        static bool Within(float distance, float reach) => distance <= reach + Units.Epsilon;

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
            if (!world.Random.For(hunter.Species, "actions").Chance(killP)) return true;
            prey.Killed = true;                                                     // ANIM-41: marked now, removed in the death phase
            prey.KilledBy = hunter.Id;
            hunter.Species.Counters.Kills++;
            world.Service<CarcassSystem>()?.Create(prey, hunter.Id);
            Eat(hunter, diet.EnergyFrom(hunter, entry, entry.Energy), digest: true);
            return true;
        }

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

        internal static void Eat(Animal a, float gain, bool digest)
        {
            a.Species.Module<Energy>()?.Gain(a, gain);
            a.Meals++;
            a.Species.Counters.Meals++;
            if (digest) a.Species.Module<Digestion>()?.AfterMeal(a, gain);
        }
    }
}
