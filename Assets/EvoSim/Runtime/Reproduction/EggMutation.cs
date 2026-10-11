namespace EvoSim
{
    /// <summary>One pending mutation of an egg: the locus, the inherited allele, the operator and its job (MUT-30).</summary>
    public sealed class EggMutation
    {
        /// <summary>The locus that mutates: an index into the egg's alleles.</summary>
        public int Locus;
        /// <summary>The allele inherited at that locus after crossover: the mutation's parent.</summary>
        public Allele Parent;
        /// <summary>The operator that applies to the locus (MUT-02).</summary>
        public MutationOperator Operator;
        /// <summary>The job the operator started; the egg hatches when it is done (MUT-32).</summary>
        public MutationJob Job;
    }
}
