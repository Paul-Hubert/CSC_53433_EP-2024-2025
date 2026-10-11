namespace EvoSim
{
    /// <summary>One value of a founder pool and its origin (founder, neutral, control…) (GENE-20).</summary>
    public readonly struct FounderAllele
    {
        /// <summary>A value founders may get at this gene.</summary>
        public readonly AlleleValue Value;
        /// <summary>The origin it is registered with: <c>founder</c>, or <c>neutral</c> for the neutral allele (GENE-12).</summary>
        public readonly string Origin;

        /// <summary>Pairs a founder value with its origin.</summary>
        /// <param name="value">The sentence or number.</param>
        /// <param name="origin"><c>founder</c>, <c>neutral</c>…</param>
        public FounderAllele(AlleleValue value, string origin) { Value = value; Origin = origin; }

        /// <summary>The value and its origin in parentheses, e.g. <c>No preference. (neutral)</c>.</summary>
        /// <inheritdoc/>
        public override string ToString() => $"{Value} ({Origin})";
    }
}
