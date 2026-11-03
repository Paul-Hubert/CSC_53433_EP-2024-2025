namespace EvoSim
{
    /// <summary>What kind of value a gene holds (GENE-02): a sentence, a number or a vector of numbers.</summary>
    public enum AlleleKind
    {
        /// <summary>A sentence the brain reads (a text gene, GENE-03).</summary>
        Text,
        /// <summary>A number expressed into a trait (a number gene, GENE-04).</summary>
        Number,
        /// <summary>
        /// A vector of numbers held as one allele, such as the weights of a small neural network (GENE-07, plan M16 §4 C1).
        /// </summary>
        Vector
    }
}
