namespace EvoSim
{
    /// <summary>What kind of value a gene holds (GENE-02): a sentence or a number.</summary>
    public enum AlleleKind
    {
        /// <summary>A sentence the brain reads (a text gene, GENE-03).</summary>
        Text,
        /// <summary>A number expressed into a trait (a number gene, GENE-04).</summary>
        Number
    }
}
