using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test phase that writes "tick:name" to a log each time it runs (T-TICK-01).</summary>
    public class TracePhase : TickPhase
    {
        /// <summary>Shared by several trace phases to see their order.</summary>
        public List<string> Log = new List<string>();
        public string Label = "";

        public override string PhaseName => string.IsNullOrEmpty(Label) ? gameObject.name : Label;
        public override void Run(TickContext t) => Log.Add(t.Tick + ":" + PhaseName);
    }
}
