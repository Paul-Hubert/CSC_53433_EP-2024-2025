using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A rule that changes one allele, blind to everything but the gene and the allele (CORE-07, 10 §1).
    /// Exactly one operator applies to a locus: the one configured for the gene, else the species' default for its kind (MUT-02).
    /// </summary>
    public abstract class MutationOperator : SpeciesModule
    {
        [SerializeField, Range(0f, 1f), Tooltip("Probability per gene per baby that this gene mutates (reference 0.03; MUT-01).")]
        float rate = 0.03f;
        [SerializeField, Tooltip("Genes this operator is configured for; empty = every gene of its kind without a configured operator (MUT-02).")]
        List<Gene> onlyThese = new List<Gene>();

        public float Rate { get => rate; set => rate = Mathf.Clamp01(value); }
        /// <summary>The genes this operator is configured for (empty: the species' default for its kind).</summary>
        public IReadOnlyList<Gene> OnlyThese => onlyThese;

        /// <summary>Whether this operator can change genes of this kind.</summary>
        public abstract bool Accepts(Gene g);

        /// <summary>Draw every random choice now (MUT-30), then start the work; the job may finish later.</summary>
        public abstract MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng);

        public void SetOnlyThese(IEnumerable<Gene> genes) => onlyThese = new List<Gene>(genes);
    }
}
