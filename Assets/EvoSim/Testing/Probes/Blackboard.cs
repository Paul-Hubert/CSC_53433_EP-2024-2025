using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>Shared state for test phases that change "the world" and read it (T-TICK-02).</summary>
    public sealed class Blackboard
    {
        public int Value;
        public readonly List<string> Seen = new List<string>();
    }
}
