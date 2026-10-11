using UnityEngine;

namespace EvoSim
{
    /// <summary>The reference operator for number genes (MUT-20): v' = clamp(v + N(0, σ)) to the gene's range.</summary>
    /// <remarks>
    /// It accepts number genes that give a range (<see cref="INumberRange"/>, e.g. <see cref="NumberGene"/>), draws one
    /// Gaussian number from the mutation stream and finishes at once with the operator detail <c>gauss</c>. At the edge of
    /// the range the clamp may give the parent's value back: the hatch phase then counts the mutation as unchanged (MUT-04).
    /// </remarks>
    public class GaussianMutation : MutationOperator
    {
        [SerializeField, Min(0f), Tooltip("Standard deviation of the change (reference 5).")]
        float sigma = 5f;

        /// <summary>The standard deviation of the change, in the gene's unit (reference 5); set values below 0 become 0.</summary>
        public float Sigma { get => sigma; set => sigma = Mathf.Max(0f, value); }

        /// <summary>Number genes that implement <see cref="INumberRange"/>.</summary>
        /// <inheritdoc/>
        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Number && g is INumberRange;

        /// <summary>
        /// Adds <see cref="Sigma"/> times one Gaussian draw to the parent's value and clamps it to the gene's range; done at once.
        /// </summary>
        /// <inheritdoc/>
        public override MutationJob StartMutation(Gene g, Allele parent, RandomStream rng)
        {
            var range = (INumberRange)g;
            float v = Mathf.Clamp(parent.Number + sigma * (float)rng.NextGaussian(), range.Min, range.Max);
            return MutationJob.Done(v, "gauss");                    // MUT-20
        }
    }
}
