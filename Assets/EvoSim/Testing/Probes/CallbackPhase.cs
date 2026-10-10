using System;

namespace EvoSim.Testing
{
    /// <summary>A test-only phase that runs a callback at its place in the tick (to look at the state between two phases).</summary>
    public class CallbackPhase : TickPhase
    {
        public Action<TickContext> OnRun;

        public override void Run(TickContext t) => OnRun?.Invoke(t);
    }
}
