using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Above its cap, a species loses animals chosen uniformly at random with its migration stream, ignoring genes,
    /// stats and position (POP-01, POP-02); animals born this tick are spared unless too few older ones remain.
    /// </summary>
    public class MigrationPhase : TickPhase
    {
        readonly List<Animal> older = new List<Animal>();
        readonly List<Animal> young = new List<Animal>();

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
