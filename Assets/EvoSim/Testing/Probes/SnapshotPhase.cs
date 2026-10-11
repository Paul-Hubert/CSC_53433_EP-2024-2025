using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test phase that keeps every living animal's genome texts at the end of each tick (T-OUT-02).</summary>
    /// <remarks>The test then rebuilds the genomes from the run's files and compares them with these snapshots.</remarks>
    public class SnapshotPhase : TickPhase
    {
        /// <summary>
        /// The snapshots, filled by the phase: tick → animal id → its alleles' texts joined by commas. The test must set it to
        /// an empty dictionary before the world runs.
        /// </summary>
        public Dictionary<int, Dictionary<int, string>> Live;

        /// <summary>Stores this tick's snapshot of every living animal in <see cref="Live"/>.</summary>
        /// <inheritdoc/>
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
