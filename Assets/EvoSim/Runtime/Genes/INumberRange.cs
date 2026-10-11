namespace EvoSim
{
    /// <summary>
    /// A gene whose values are numbers within a range: what number operators (Gaussian, MUT-20) and the random-founders
    /// control (CTRL-01) need, so a student's own number gene kind works with them.
    /// </summary>
    /// <remarks>
    /// <see cref="NumberGene"/> implements it. <see cref="GaussianMutation"/> accepts only number genes that do, and clamps
    /// its results to the range; the random-founders control draws founder values uniformly in it.
    /// </remarks>
    public interface INumberRange
    {
        /// <summary>The smallest value the gene may take, in the unit of its trait.</summary>
        float Min { get; }
        /// <summary>The largest value the gene may take, in the unit of its trait.</summary>
        float Max { get; }
    }
}
