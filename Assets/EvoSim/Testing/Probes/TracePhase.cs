using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test phase that writes "tick:name" to a log each time it runs (T-TICK-01).</summary>
    /// <example>
    /// Three phases sharing one log run in hierarchy order:
    /// <code>
    /// var b = New().Flat(10, 10)
    ///     .Phase&lt;TracePhase&gt;(p =&gt; p.Label = "A")
    ///     .Phase&lt;TracePhase&gt;(p =&gt; p.Label = "B")
    ///     .Phase&lt;TracePhase&gt;(p =&gt; p.Label = "C");
    /// var phases = b.Root.GetComponentsInChildren&lt;TracePhase&gt;(true);
    /// var log = phases[0].Log;
    /// foreach (var p in phases) p.Log = log;
    /// var w = b.Build();
    /// w.Advance(2);
    /// CollectionAssert.AreEqual(new[] { "0:A", "0:B", "0:C", "1:A", "1:B", "1:C" }, log);
    /// </code>
    /// </example>
    public class TracePhase : TickPhase
    {
        /// <summary>The log it writes to; give several trace phases the same list to see their order.</summary>
        public List<string> Log = new List<string>();
        /// <summary>The name written to the log; empty = the GameObject's name.</summary>
        public string Label = "";

        /// <summary><see cref="Label"/>, or the GameObject's name when the label is empty.</summary>
        public override string PhaseName => string.IsNullOrEmpty(Label) ? gameObject.name : Label;

        /// <summary>Writes <c>"tick:name"</c> to <see cref="Log"/>.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t) => Log.Add(t.Tick + ":" + PhaseName);
    }
}
