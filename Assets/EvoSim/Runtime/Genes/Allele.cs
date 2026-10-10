namespace EvoSim
{
    /// <summary>One registered allele with its lineage (GENE-11, GENE-12).</summary>
    public sealed class Allele
    {
        /// <summary>"&lt;locus id&gt;:&lt;n&gt;", e.g. prey.eat:3.</summary>
        public string Id { get; }
        public string Locus { get; }
        /// <summary>n: the registration order within the locus, from 0.</summary>
        public int Index { get; }
        public AlleleValue Value { get; }
        /// <summary>founder, neutral, contrast, control, mutant or custom.</summary>
        public string Origin { get; }
        public string ParentId { get; }
        /// <summary>The operator detail, e.g. llm#3 or gauss.</summary>
        public string Operator { get; }
        public string Model { get; }
        public long? Seed { get; }

        public string Text => Value.Text;
        public float Number => Value.Number;
        public AlleleKind Kind => Value.Kind;

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

        public override string ToString() => $"{Id} {Value}";
    }
}
