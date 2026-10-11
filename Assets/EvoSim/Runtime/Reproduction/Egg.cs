using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A conceived baby waiting to hatch (REPRO-20): its parents, generation, genome after crossover, the mutations
    /// still running, and when it hatches. Not an animal: it doesn't sense, decide, move or count toward caps (REPRO-22).
    /// </summary>
    /// <remarks>
    /// The breed phase fills one per baby and lays it with <see cref="EggSystem.Lay"/>; the hatch phase turns it into an
    /// animal at <see cref="HatchTick"/>, once <see cref="IsComplete"/> (MUT-32).
    /// </remarks>
    public class Egg : Entity
    {
        /// <summary>The entity kind's name: <c>"egg"</c>.</summary>
        /// <inheritdoc/>
        public override string Kind => "egg";

        /// <summary>The species of the baby inside.</summary>
        public Species Species;
        /// <summary>The parents' animal ids: two, or one for a single parent.</summary>
        public int[] Parents;
        /// <summary>The baby's generation: the larger parent generation + 1 (REPRO-12).</summary>
        public int Generation;
        /// <summary>The genome after crossover; mutated loci are replaced when their job succeeds.</summary>
        public Allele[] Alleles;
        /// <summary>Mutations drawn at conception, in locus order (MUT-30).</summary>
        public readonly List<EggMutation> Mutations = new List<EggMutation>();
        /// <summary>The tick of conception.</summary>
        public int ConceivedTick;
        /// <summary>The tick it hatches: the conception tick plus the species' incubation, in ticks (REPRO-20).</summary>
        public int HatchTick;
        /// <summary>The number of babies in its litter, recorded in the birth event.</summary>
        public int LitterSize;
        /// <summary>The baby's heading at birth, in degrees, drawn at conception from the litter stream.</summary>
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
