namespace EvoSim
{
    /// <summary>For each locus independently, the allele of one parent or the other with probability ½ (GENE-30).</summary>
    public class UniformCrossover : Crossover
    {
        public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
        {
            CheckSpecies(a, b);
            var child = new Allele[a.Count];
            for (int i = 0; i < child.Length; i++) child[i] = rng.NextBool() ? a[i] : b[i];
            return child;
        }
    }
}
