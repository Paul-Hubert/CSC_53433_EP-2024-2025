using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Eggs as entities (REPRO-20). In the reference they have no Edible, so nothing eats them (owner decision);
    /// an Edible on this GameObject and a diet entry "egg:&lt;species&gt;" make them food (22 §15).
    /// </summary>
    /// <remarks>
    /// A world service (the reference scenes name it Eggs); a species that breeds needs one (V-20). The breed phase lays
    /// eggs, the hatch phase hatches the due ones, and a lost egg is removed in the environment phase (ENV-22). With the
    /// reference incubation of 0, eggs hatch in the tick they are laid.
    /// </remarks>
    public class EggSystem : EntitySystem<Egg>
    {
        long conceptions;                                            // eggs laid in this run: the next conception order

        /// <summary>The entity kind's name: <c>"egg"</c>.</summary>
        /// <inheritdoc/>
        public override string Kind => "egg";

        /// <summary>Removes every egg and restarts the conception count, at the start of a run.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            conceptions = 0;
        }

        /// <summary>Lays an egg (REPRO-20); it has no lifetime and waits for the hatch phase.</summary>
        /// <param name="egg">A new egg, with its species, parents, alleles, mutations and hatch tick set.</param>
        /// <returns>The same egg, with its id, creation tick and conception order.</returns>
        public Egg Lay(Egg egg)
        {
            egg.ConceptionOrder = conceptions++;
            egg.LifetimeLeft = -1;
            return Add(egg);
        }

        /// <summary>Eggs due to hatch at this tick, in conception order (REPRO-22).</summary>
        /// <param name="tick">The current tick.</param>
        /// <param name="result">Cleared, then filled with the eggs whose hatch tick has come, lost eggs left out.</param>
        public void Due(int tick, List<Egg> result)
        {
            result.Clear();
            foreach (var e in entities)
                if (!e.UsedUp && e.HatchTick <= tick) result.Add(e);
        }

        /// <summary>Removes a hatched egg.</summary>
        /// <param name="egg">The egg the hatch phase just turned into an animal.</param>
        public void Hatched(Egg egg) => entities.Remove(egg);

        /// <summary>An egg that will never hatch (eaten, destroyed), recorded as lost (REPRO-23, OUT egg_lost).</summary>
        /// <param name="egg">The egg; nothing happens if it is already lost.</param>
        /// <param name="reason">Why, e.g. <c>"eaten"</c>.</param>
        public void Lose(Egg egg, string reason)
        {
            if (egg.UsedUp) return;
            egg.UsedUp = true;
            egg.LostReason = reason;
            egg.Species.Counters.EggsLost++;
            World.Events.Record(new SimEvent("egg_lost", World.Tick, egg.Id, egg.Species.Id)
                .With("parents", new List<int>(egg.Parents ?? System.Array.Empty<int>()))
                .With("reason", reason));
        }

        /// <summary>Eggs of a species, not yet hatched or lost.</summary>
        /// <param name="s">The species.</param>
        /// <returns>The number of its eggs still waiting to hatch.</returns>
        public int CountOf(Species s)
        {
            int n = 0;
            foreach (var e in entities) if (!e.UsedUp && e.Species == s) n++;
            return n;
        }
    }
}
