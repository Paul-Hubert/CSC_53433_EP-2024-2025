using UnityEngine;

namespace EvoSim
{
    /// <summary>The reference operator for number genes (MUT-20): v' = clamp(v + N(0, σ)) to the gene's range.</summary>
    public class GaussianMutation : MutationOperator
    {
        [SerializeField, Min(0f), Tooltip("Standard deviation of the change (reference 5).")]
        float sigma = 5f;

        public float Sigma { get => sigma; set => sigma = Mathf.Max(0f, value); }

        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Number;

        public override MutationJob Start(Gene g, Allele parent, RandomStream rng)
        {
            var gene = (NumberGene)g;
            float v = Mathf.Clamp(parent.Number + sigma * (float)rng.NextGaussian(), gene.Min, gene.Max);
            return MutationJob.Done(v, "gauss");                    // MUT-20
        }
    }
}
