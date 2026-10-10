using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Eggs as entities (REPRO-20). In the reference they have no Edible, so nothing eats them (owner decision);
    /// an Edible on this GameObject and a diet entry "egg:&lt;species&gt;" make them food (22 §15).
    /// </summary>
    public class EggSystem : EntitySystem<Egg>
    {
        long conceptions;

        public override string Kind => "egg";

        public override void Initialize()
        {
            base.Initialize();
            conceptions = 0;
        }

        /// <summary>Lays an egg (REPRO-20); it has no lifetime and waits for the hatch phase.</summary>
        public Egg Lay(Egg egg)
        {
            egg.ConceptionOrder = conceptions++;
            egg.LifetimeLeft = -1;
            return Add(egg);
        }

        /// <summary>Eggs due to hatch at this tick, in conception order (REPRO-22).</summary>
        public void Due(int tick, List<Egg> result)
        {
            result.Clear();
            foreach (var e in entities)
                if (!e.UsedUp && e.HatchTick <= tick) result.Add(e);
        }

        /// <summary>Removes a hatched egg.</summary>
        public void Hatched(Egg egg) => entities.Remove(egg);

        /// <summary>An egg that will never hatch (eaten, destroyed), recorded as lost (REPRO-23, OUT egg_lost).</summary>
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
        public int CountOf(Species s)
        {
            int n = 0;
            foreach (var e in entities) if (!e.UsedUp && e.Species == s) n++;
            return n;
        }
    }
}
