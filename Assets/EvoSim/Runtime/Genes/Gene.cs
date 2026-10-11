using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A gene (locus) of a species: a sentence for the brain or a number for a trait (GENE-01…04). Each animal carries one
    /// allele (one value) of every gene of its species.
    /// </summary>
    /// <remarks>
    /// <para>A gene is a component under a species. Its place among the species' genes (depth first in the hierarchy) is its
    /// locus, <see cref="Locus"/>: the index of its allele in every <see cref="Genome"/> of the species (GENE-06). A text
    /// gene on or under an action is bound to that action and takes its name as label (GENE-05).</para>
    /// <para>To add a gene kind, derive from it and implement <see cref="Kind"/>, <see cref="FounderPool"/> and
    /// <see cref="Express"/>. Override <see cref="Check"/> to refuse some values, <see cref="MayMutate"/> to protect some
    /// alleles, <see cref="Decimals"/> to compare numbers at another precision, and <see cref="ReadByBrain"/> when the brain
    /// should read it. A number gene kind should also implement <see cref="INumberRange"/>, so that
    /// <see cref="GaussianMutation"/> and the random-founders control (CTRL-01) work with it.</para>
    /// <para>What a gene kind must keep: it holds settings only, never per-animal state; <see cref="FounderPool"/> gives the
    /// same values in the same order in every run (GENE-21); <see cref="Express"/> acts only through the
    /// <see cref="Expression"/> it is given and draws no random number.</para>
    /// </remarks>
    /// <example>
    /// A number gene kind, as the Gene script template writes it (the trait must be declared by a module of the species):
    /// <code>
    /// public class CourageGene : Gene
    /// {
    ///     [SerializeField, Tooltip("The trait this gene sets.")] string trait = "courage";
    ///     [SerializeField, Tooltip("Founder values.")] List&lt;float&gt; founders = new List&lt;float&gt; { 1f };
    ///
    ///     public override AlleleKind Kind => AlleleKind.Number;
    ///
    ///     public override IReadOnlyList&lt;FounderAllele&gt; FounderPool
    ///     {
    ///         get
    ///         {
    ///             var pool = new List&lt;FounderAllele&gt;();
    ///             foreach (var f in founders) pool.Add(new FounderAllele(AlleleValue.OfNumber(f), "founder"));
    ///             return pool;
    ///         }
    ///     }
    ///
    ///     public override void Express(AlleleValue value, Expression e) => e.SetTrait(trait, value.Number, false);
    /// }
    /// </code>
    /// </example>
    public abstract class Gene : SpeciesModule
    {
        /// <summary>The label set in the Inspector; empty = the bound action's name (GENE-05). Read <see cref="Label"/>.</summary>
        [SerializeField, Tooltip("What the brain and the inspector show. Empty = the bound action's name (GENE-05).")]
        protected string label = "";

        /// <summary>The action this gene is bound to (GENE-05), or null for a free gene.</summary>
        public AnimalAction Action { get; internal set; }

        /// <summary>
        /// What the brain and the Inspector show for this gene: the label field, else the bound action's name, else the
        /// GameObject's name (GENE-05).
        /// </summary>
        public string Label =>
            !string.IsNullOrEmpty(label) ? label : Action != null ? Action.Name : gameObject.name;

        /// <summary>The locus id, <c>"&lt;species id&gt;.&lt;label&gt;"</c>, e.g. <c>prey.eat</c>; unique in the world (GENE-11).</summary>
        public string LocusId => Species.Id + "." + Label;

        /// <summary>Sets the label from code (WorldBuilder, tests).</summary>
        /// <param name="newLabel">The new label; empty = the bound action's name.</param>
        public void SetLabel(string newLabel) => label = newLabel;

        /// <summary>
        /// This gene's position in the species' locus order: the index of its allele in every genome of the species;
        /// -1 before the species is set up.
        /// </summary>
        public int Locus { get; internal set; } = -1;

        /// <summary>Whether this gene's alleles are sentences or numbers (GENE-02).</summary>
        public abstract AlleleKind Kind { get; }

        /// <summary>
        /// Whether the brain reads this gene (text genes; number genes only if configured, GENE-04). Only these genes enter
        /// the genes block of the prompt and the brain-visible genome key (DEC-31).
        /// </summary>
        public virtual bool ReadByBrain => Kind == AlleleKind.Text;

        /// <summary>Precision used to compare number alleles (GENE-10), in decimals; reference 4. Ignored for sentences.</summary>
        public virtual int Decimals => 4;

        /// <summary>The founder pool with its origins, neutral allele included (GENE-20).</summary>
        /// <remarks>
        /// A founder genome draws each locus uniformly from it, and newcomers below the floor too (POP-03). The World
        /// registers the whole pool first, in this order, so founder alleles get the same ids in every run (GENE-21): it
        /// must give the same values in the same order every time. The validator reports an empty pool (V-41).
        /// </remarks>
        public abstract IReadOnlyList<FounderAllele> FounderPool { get; }

        /// <summary>Whether this allele may mutate (the neutral allele's setting, MUT-22).</summary>
        /// <param name="allele">The allele a baby inherited at this locus.</param>
        /// <returns>True when it may mutate (the default); false keeps it unchanged.</returns>
        /// <remarks>The breed phase asks at conception, after drawing the mutation rate, before starting a mutation.</remarks>
        public virtual bool MayMutate(Allele allele) => true;

        /// <summary>Turn this gene's allele into what acts: a prompt line or a trait (GENE-03/04).</summary>
        /// <param name="value">The allele's value.</param>
        /// <param name="e">The target: call <see cref="Expression.SetTrait"/> for a trait, <see cref="Expression.PromptGene"/>
        /// for a line of the genes block.</param>
        /// <remarks>
        /// Called at birth with the newborn (prompt lines are then dropped), and each time a decision's genes block is
        /// written, with no animal (traits are then dropped), for the genes the brain reads. Act only through
        /// <paramref name="e"/>.
        /// </remarks>
        public abstract void Express(AlleleValue value, Expression e);

        /// <summary>Values allowed for this gene, e.g. the mutation guards for sentences (GENE-22).</summary>
        /// <param name="value">A value: a founder, or a mutant about to be registered.</param>
        /// <returns>Why the value is not allowed (e.g. <c>"13 words (at most 12)"</c>), or null when it is.</returns>
        /// <remarks>
        /// The hatch phase checks every mutant with it, whatever operator made it: a refused mutant leaves the inherited
        /// allele and is counted as <c>not allowed</c>. Text genes also check their founders with it (V-40).
        /// </remarks>
        public virtual string Check(AlleleValue value) => null;
    }
}
