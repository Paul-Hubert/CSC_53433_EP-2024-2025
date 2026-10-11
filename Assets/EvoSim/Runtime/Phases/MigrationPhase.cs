using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Above its cap, a species loses animals chosen uniformly at random with its migration stream, ignoring genes,
    /// stats and position (POP-01, POP-02); animals born this tick are spared unless too few older ones remain.
    /// </summary>
    /// <remarks>
    /// Eighth phase of the reference tick, after Deaths and before Environment. Only species whose <see cref="CapRule"/>
    /// is in <see cref="CapRule.Mode.Migrate"/> mode are thinned here (the Block mode limits litters in the breed phase
    /// instead). Leavers die with the cause <c>migrated</c> through <see cref="World.RecordDeath"/>, drawn from the
    /// species' <c>migration</c> stream (RAND-03); newcomers count as older animals.
    /// </remarks>
    public class MigrationPhase : TickPhase
    {
        readonly List<Animal> older = new List<Animal>();       // living animals not born this tick: they leave first
        readonly List<Animal> young = new List<Animal>();       // babies born this tick: they leave only when no older one is left

        /// <summary>Removes each capped species' excess animals, chosen at random, older ones first.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            foreach (var s in World.AllSpecies)
            {
                var cap = s.Module<CapRule>();
                if (cap == null || cap.Rule != CapRule.Mode.Migrate) continue;
                older.Clear();
                young.Clear();
                foreach (var a in s.Animals)
                    if (!a.IsGone) (a.BornTick < t.Tick || a.Origin != "birth" ? older : young).Add(a);
                int excess = older.Count + young.Count - cap.Cap;
                if (excess <= 0) continue;
                var rng = t.Stream(s, "migration");
                for (; excess > 0; excess--)
                {
                    var pool = older.Count > 0 ? older : young;
                    int i = rng.Range(0, pool.Count);
                    var leaving = pool[i];
                    pool.RemoveAt(i);
                    World.RecordDeath(leaving, "migrated");
                }
                s.RemoveGone();
            }
        }
    }
}
