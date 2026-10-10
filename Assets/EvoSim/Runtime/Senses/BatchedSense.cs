namespace EvoSim
{
    /// <summary>
    /// A sense that casts rays (line of sight, terrain ahead): it asks for its rays first; the sense phase casts the
    /// rays of all deciding animals in one batch, then each sense reads its results (SENSE-30). It must give the same
    /// tokens as evaluating it one animal at a time (SENSE-31).
    /// </summary>
    public abstract class BatchedSense : Sense
    {
        /// <summary>Ask for this animal's rays (read their results in Read through SenseContext.Rays).</summary>
        public abstract void RequestRays(Animal a, RayBatch rays);
    }
}
