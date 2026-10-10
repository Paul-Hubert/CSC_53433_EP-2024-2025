namespace EvoSim.Testing
{
    /// <summary>A test-only uniform crossover that counts its calls (GENE-32: one cross per baby).</summary>
    public class CountingCrossover : UniformCrossover
    {
        public int Calls;

        public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
        {
            Calls++;
            return base.Cross(a, b, rng);
        }
    }
}
