namespace EvoSim
{
    /// <summary>One pending mutation of an egg: the locus, the inherited allele, the operator and its job (MUT-30).</summary>
    public sealed class EggMutation
    {
        public int Locus;
        public Allele Parent;
        public MutationOperator Operator;
        public MutationJob Job;
    }
}
