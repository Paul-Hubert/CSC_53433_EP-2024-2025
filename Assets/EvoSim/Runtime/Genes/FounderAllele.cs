namespace EvoSim
{
    /// <summary>One value of a founder pool and its origin (founder, neutral, control…).</summary>
    public readonly struct FounderAllele
    {
        public readonly AlleleValue Value;
        public readonly string Origin;

        public FounderAllele(AlleleValue value, string origin) { Value = value; Origin = origin; }

        public override string ToString() => $"{Value} ({Origin})";
    }
}
