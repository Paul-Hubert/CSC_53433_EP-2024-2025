namespace EvoSim
{
    /// <summary>How fast a running world advances (02 §4, run controls). Results never depend on it (SPACE-11).</summary>
    public enum RunSpeed
    {
        /// <summary>A fixed number of ticks per FixedUpdate.</summary>
        PerFixedUpdate,
        /// <summary>As many ticks per frame as fit in a time budget.</summary>
        Fast,
        /// <summary>A fixed number of ticks per second, for watching.</summary>
        RealTime
    }
}
