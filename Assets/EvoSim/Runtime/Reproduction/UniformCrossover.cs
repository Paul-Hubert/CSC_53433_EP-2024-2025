namespace EvoSim
{
    /// <summary>For each locus independently, the allele of one parent or the other with probability ½ (GENE-30).</summary>
    /// <remarks>
    /// The reference crossover (the exercise MyUniformCrossover rewrites it). It checks that both genomes belong to the
    /// species, then, for each locus in locus order, draws one <c>rng.NextBool()</c>: true takes the first parent's allele,
    /// false the partner's. One draw per locus, nothing else.
    /// </remarks>
    public class UniformCrossover : Crossover
    {
        /// <summary>One coin flip per locus, in locus order, picks the parent whose allele the baby gets.</summary>
        /// <inheritdoc/>
        public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
        {
            CheckSpecies(a, b);
            var child = new Allele[a.Count];
            for (int i = 0; i < child.Length; i++) child[i] = rng.NextBool() ? a[i] : b[i];
            return child;
        }
    }
}
