using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test phase that keeps every living animal's genome texts at the end of each tick (T-OUT-02).</summary>
    public class SnapshotPhase : TickPhase
    {
        public Dictionary<int, Dictionary<int, string>> Live;

        public override void Run(TickContext t)
        {
            var map = new Dictionary<int, string>();
            foreach (var s in World.AllSpecies)
                foreach (var a in s.Animals)
                    if (!a.IsGone) map[a.Id] = string.Join(",", System.Linq.Enumerable.Select(a.Genome.Alleles, x => x.Text));
            Live[t.Tick] = map;
        }
    }
}
