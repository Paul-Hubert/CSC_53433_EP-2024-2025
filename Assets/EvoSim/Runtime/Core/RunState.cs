namespace EvoSim
{
    /// <summary>The World's run loop states (brief, chapter B §13).</summary>
    /// <remarks>Read it from <see cref="World.State"/>; the run controls (<see cref="World.Play"/>,
    /// <see cref="World.Pause"/>, <see cref="World.Step"/>…) change it.</remarks>
    public enum RunState
    {
        /// <summary>Not initialised.</summary>
        Idle,
        /// <summary>Ticks advance every FixedUpdate (or per the run speed).</summary>
        Running,
        /// <summary>Inside a tick, waiting for answers while Unity renders (Responsive).</summary>
        Waiting,
        /// <summary>Inside a tick, the main thread blocked on answers (Freeze).</summary>
        Blocked,
        /// <summary>Between ticks or inside one; nothing advances.</summary>
        Paused,
        /// <summary>One tick, then Paused.</summary>
        Stepping,
        /// <summary>At a tick boundary, every output written (RAND-20).</summary>
        Stopped
    }
}
