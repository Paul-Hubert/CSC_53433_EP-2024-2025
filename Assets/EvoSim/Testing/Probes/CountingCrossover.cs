namespace EvoSim.Testing
{
    /// <summary>A test-only uniform crossover that counts its calls (GENE-32: one cross per baby).</summary>
    public class CountingCrossover : UniformCrossover
    {
        /// <summary>The number of crossovers made so far.</summary>
        public int Calls;

        /// <summary>Counts the call, then crosses as the uniform crossover does.</summary>
        /// <inheritdoc/>
        public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
        {
            Calls++;
            return base.Cross(a, b, rng);
        }
    }
}
