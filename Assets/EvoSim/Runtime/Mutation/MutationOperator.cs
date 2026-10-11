using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A rule that changes one allele, blind to everything but the gene and the allele (CORE-07, 10 §1).
    /// Exactly one operator applies to a locus: the one configured for the gene, else the species' default for its kind (MUT-02).
    /// </summary>
    /// <remarks>
    /// <para>Place operators as components under a species (the reference scenes keep them on a Mutation object). At each
    /// conception the breed phase picks, for each locus, the first operator that <see cref="Accepts"/> the gene and lists it
    /// in <see cref="OnlyThese"/>, else the first that accepts it and lists no gene (<see cref="BreedPhase.OperatorFor"/>). Then,
    /// in locus order and from the species' mutation stream, it draws whether the locus mutates with probability
    /// <see cref="Rate"/> (MUT-01), asks <see cref="Gene.MayMutate"/>, and calls <see cref="StartMutation"/>.</para>
    /// <para>To write an operator, derive from it and implement <see cref="Accepts"/> and <see cref="StartMutation"/>. What it
    /// must keep:</para>
    /// <list type="bullet">
    /// <item><description>it sees only the gene and the parent allele, never the animal, its parents or the world
    /// (CORE-07);</description></item>
    /// <item><description>it draws random numbers only from the stream it is given, all of them inside
    /// <see cref="StartMutation"/>, as many whatever happens later (MUT-30): the same stream state gives the same job;</description></item>
    /// <item><description>its result must pass the gene's <see cref="Gene.Check"/> at hatching, and a result equal to the
    /// parent counts as a failure (MUT-04).</description></item>
    /// </list>
    /// <para>The job may finish at once (<see cref="MutationJob.Done(AlleleValue, string, string, long?)"/>,
    /// <see cref="MutationJob.Failed"/>) or later (<see cref="MutationJob.Running"/>, or the <see cref="MutatorService"/>):
    /// the egg hatches only when it is done (MUT-32). See <see cref="GaussianMutation"/>, <see cref="LlmMutation"/> and the
    /// IntensityLadder sample (22 §8).</para>
    /// </remarks>
    /// <example>
    /// A number operator, as the MutationOperator script template writes it (values it pushes out of the gene's range are
    /// refused at hatching):
    /// <code>
    /// public class Nudge : MutationOperator
    /// {
    ///     public override bool Accepts(Gene g) => g.Kind == AlleleKind.Number;
    ///
    ///     public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng) =>
    ///         MutationJob.Done(parent.Number + (rng.Chance(0.5f) ? 1f : -1f), "nudge");
    /// }
    /// </code>
    /// </example>
    public abstract class MutationOperator : SpeciesModule
    {
        [SerializeField, Range(0f, 1f), Tooltip("Probability per gene per baby that this gene mutates (reference 0.03; MUT-01).")]
        float rate = 0.03f;
        [SerializeField, Tooltip("Genes this operator is configured for; empty = every gene of its kind without a configured operator (MUT-02).")]
        List<Gene> onlyThese = new List<Gene>();

        /// <summary>
        /// The probability, per gene per baby, that a gene this operator applies to mutates (reference 0.03; MUT-01); set
        /// values are clamped to [0, 1]. The world's no-mutation control (C2) uses 0 instead.
        /// </summary>
        public float Rate { get => rate; set => rate = Mathf.Clamp01(value); }
        /// <summary>The genes this operator is configured for (empty: the species' default for its kind).</summary>
        public IReadOnlyList<Gene> OnlyThese => onlyThese;

        /// <summary>
        /// True for an operator that sends its work to the World's MutatorService (LlmMutation): the client then reads the
        /// model's digest at the start of the run, to record it (GENE-12).
        /// </summary>
        public virtual bool UsesMutatorService => false;

        /// <summary>Whether this operator can change genes of this kind.</summary>
        /// <param name="g">A gene of the species.</param>
        /// <returns>True when it can mutate this gene's alleles.</returns>
        /// <remarks>Asked at each conception for each locus (MUT-02) and by the validator, which warns about a gene no
        /// operator accepts (V-48).</remarks>
        public abstract bool Accepts(Gene g);

        /// <summary>Draw every random choice now (MUT-30), then start the work; the job may finish later.</summary>
        /// <param name="gene">The gene (locus) that mutates: its settings are all the operator may read.</param>
        /// <param name="parent">The allele the baby inherited at this locus, to change.</param>
        /// <param name="rng">The species' mutation stream: draw only from it, and only inside this call.</param>
        /// <returns>The job: done at once with a value, failed, or still running.</returns>
        /// <remarks>Called by the breed phase at conception, once per mutating locus of each baby, in locus order.</remarks>
        public abstract MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng);

        /// <summary>Sets the genes this operator is configured for, from code.</summary>
        /// <param name="genes">The genes; empty = the species' default for the genes it accepts.</param>
        public void SetOnlyThese(IEnumerable<Gene> genes) => onlyThese = new List<Gene>(genes);

        /// <summary>Reports a rate outside [0, 1] (V-35). An override should call it.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (!(rate >= 0f && rate <= 1f))
                report.Error("V-35", this, $"The mutation rate {rate} is outside [0, 1] (MUT-01).", new ValidationFix("Clamp", () => Rate = float.IsNaN(rate) ? 0f : rate));
        }
    }
}
