namespace EvoSim
{
    /// <summary>How a tick waits for brain answers and mutations (SPACE-14). Both give the same events hash.</summary>
    public enum WaitMode
    {
        /// <summary>The main thread blocks until the answers are in; Unity freezes (batch mode, tests).</summary>
        Freeze,
        /// <summary>The tick pauses at the phase that waits; Unity keeps rendering (the editor default).</summary>
        Responsive
    }
}
