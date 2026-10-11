using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// Shared state for test phases that change "the world" and read it (T-TICK-02): one blackboard is given to several
    /// <see cref="BlackboardPhase"/>s, to see which phases see a change.
    /// </summary>
    public sealed class Blackboard
    {
        /// <summary>The shared value the phases write and read.</summary>
        public int Value;
        /// <summary>What each phase saw, in order: <c>"tick:phase=value"</c>.</summary>
        public readonly List<string> Seen = new List<string>();
    }
}
