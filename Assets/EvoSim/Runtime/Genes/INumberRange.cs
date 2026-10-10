namespace EvoSim
{
    /// <summary>
    /// A gene whose values are numbers within a range: what number operators (Gaussian, MUT-20) and the random-founders
    /// control (CTRL-01) need, so a student's own number gene kind works with them.
    /// </summary>
    public interface INumberRange
    {
        float Min { get; }
        float Max { get; }
    }
}
