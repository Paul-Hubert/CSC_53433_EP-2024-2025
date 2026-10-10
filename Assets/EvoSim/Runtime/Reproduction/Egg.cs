using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A conceived baby waiting to hatch (REPRO-20): its parents, generation, genome after crossover, the mutations
    /// still running, and when it hatches. Not an animal: it doesn't sense, decide, move or count toward caps (REPRO-22).
    /// </summary>
    public sealed class Egg : Entity
    {
        public override string Kind => "egg";

        public Species Species;
        public int[] Parents;
        public int Generation;
        /// <summary>The genome after crossover; mutated loci are replaced when their job succeeds.</summary>
        public Allele[] Alleles;
        /// <summary>Mutations drawn at conception, in locus order (MUT-30).</summary>
        public readonly List<EggMutation> Mutations = new List<EggMutation>();
        public int ConceivedTick;
        public int HatchTick;
        public int LitterSize;
        public float Heading;
        /// <summary>The order of conception within the run (REPRO-22: eggs hatch in conception order).</summary>
        public long ConceptionOrder;
        /// <summary>Why the egg will never hatch ("eaten"…), or null (REPRO-23).</summary>
        public string LostReason;

        /// <summary>True once every mutation job is done (MUT-32).</summary>
        public bool IsComplete
        {
            get
            {
                foreach (var m in Mutations) if (!m.Job.IsDone) return false;
                return true;
            }
        }
    }
}
