namespace EvoSim
{
    /// <summary>
    /// One registered allele with its lineage (GENE-11, GENE-12): one version of a gene, such as the sentence
    /// "Rest when you are tired." at the prey's rest gene, or the number 60 at a stamina gene.
    /// </summary>
    /// <remarks>
    /// <para>Alleles are made by <see cref="AlleleRegistry.Register"/>, once per (locus, value): every animal carrying the
    /// same sentence at the same gene shares one <see cref="Allele"/> object. An allele never changes once registered.</para>
    /// <para>A mutant records the allele it came from (<see cref="ParentId"/>) and how it was made (<see cref="Operator"/>,
    /// <see cref="Model"/>, <see cref="Seed"/>): following parent ids back gives the lineage of a gene down to a founder.</para>
    /// </remarks>
    public sealed class Allele
    {
        /// <summary>The allele id, <c>"&lt;locus id&gt;:&lt;n&gt;"</c>, e.g. <c>prey.eat:3</c> (GENE-11).</summary>
        public string Id { get; }
        /// <summary>The locus id of the gene it belongs to, e.g. <c>prey.eat</c> (see <see cref="Gene.LocusId"/>).</summary>
        public string Locus { get; }
        /// <summary>n: the registration order within the locus, from 0.</summary>
        public int Index { get; }
        /// <summary>The value: a sentence or a number.</summary>
        public AlleleValue Value { get; }
        /// <summary>
        /// Where it came from: <c>founder</c>, <c>neutral</c>, <c>contrast</c>, <c>control</c>, <c>mutant</c> or
        /// <c>custom</c> (placed by a test or a tool) (GENE-12).
        /// </summary>
        public string Origin { get; }
        /// <summary>The id of the allele it mutated from, or null when it is not a mutant.</summary>
        public string ParentId { get; }
        /// <summary>
        /// The operator detail of a mutant, e.g. <c>llm#3</c> (instruction 3 of the deck) or <c>gauss</c>; null otherwise.
        /// </summary>
        public string Operator { get; }
        /// <summary>The name of the mutator model that wrote a mutant sentence, or null.</summary>
        public string Model { get; }
        /// <summary>The seed of the mutator call that wrote a mutant sentence, or null.</summary>
        public long? Seed { get; }

        /// <summary>The sentence of a text allele; null for a number allele.</summary>
        public string Text => Value.Text;
        /// <summary>The number of a number allele; 0 for a text allele.</summary>
        public float Number => Value.Number;
        /// <summary>Whether it holds a sentence or a number.</summary>
        public AlleleKind Kind => Value.Kind;

        /// <summary>
        /// Creates an allele. Code outside the registry should call <see cref="AlleleRegistry.Register"/> instead, which
        /// numbers alleles and keeps one per value.
        /// </summary>
        /// <param name="locus">The locus id of its gene, e.g. <c>prey.eat</c>.</param>
        /// <param name="index">Its number within the locus, from 0.</param>
        /// <param name="value">The sentence or number.</param>
        /// <param name="origin">Where it came from: <c>founder</c>, <c>neutral</c>, <c>mutant</c>… (GENE-12).</param>
        /// <param name="parentId">The id of the allele it mutated from, or null.</param>
        /// <param name="op">The operator detail of a mutant (<c>llm#3</c>, <c>gauss</c>), or null.</param>
        /// <param name="model">The mutator model's name, or null.</param>
        /// <param name="seed">The mutator call's seed, or null.</param>
        public Allele(string locus, int index, AlleleValue value, string origin, string parentId = null,
                      string op = null, string model = null, long? seed = null)
        {
            Locus = locus;
            Index = index;
            Id = locus + ":" + index;
            Value = value;
            Origin = origin;
            ParentId = parentId;
            Operator = op;
            Model = model;
            Seed = seed;
        }

        /// <summary>The id and the value, e.g. <c>prey.rest:0 Rest when you are tired.</c></summary>
        /// <inheritdoc/>
        public override string ToString() => $"{Id} {Value}";
    }
}
