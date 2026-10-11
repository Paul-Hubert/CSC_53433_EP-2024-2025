using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase that waits, every <see cref="Every"/> ticks, for an answer that arrives <see cref="DelayCalls"/>
    /// Advance calls after it was asked (like a slow brain); in Freeze mode Wait() delivers it at once.
    /// </summary>
    /// <remarks>
    /// It tests the tick loop's waiting (SPACE-13, SPACE-14): in the Responsive mode the tick stops at this phase and goes on
    /// in a later Advance call; both modes must give the same events hash. Each run records a "delayed" event with a draw
    /// from its own stream, so the hash depends on it.
    /// </remarks>
    /// <example>
    /// <code>
    /// var w = New(1234, WaitMode.Responsive).Flat(48, 48).Phase&lt;RandomEventPhase&gt;()
    ///     .Phase&lt;DelayedPhase&gt;(p =&gt; { p.DelayCalls = 3; p.Every = 2; }).Phase&lt;TracePhase&gt;()
    ///     .Configure(x =&gt; x.TickLimit = 20).Build();
    /// while (w.State != RunState.Stopped) w.Advance(5);
    /// </code>
    /// </example>
    public class DelayedPhase : TickPhase
    {
        /// <summary>How many Advance calls after it is asked the answer arrives; 0 = at once.</summary>
        public int DelayCalls = 3;
        /// <summary>The phase waits on ticks that are multiples of this; on the others it runs at once.</summary>
        public int Every = 1;
        /// <summary>The tick of each run, in order.</summary>
        public List<int> RanAtTick = new List<int>();
        /// <summary>The Advance call (<see cref="World.AdvanceCalls"/>) each run happened in, in order.</summary>
        public List<int> RanAtCall = new List<int>();

        int askedTick = -1, readyAtCall;   // the tick the current wait was started for, and the Advance call that ends it
        ManualPending pending;             // the current wait, made done once enough Advance calls have passed

        /// <summary>
        /// On every <see cref="Every"/>-th tick, a wait that is done <see cref="DelayCalls"/> Advance calls after the tick
        /// first asked; on other ticks, nothing to wait for.
        /// </summary>
        /// <inheritdoc/>
        public override Pending WaitsFor(TickContext t)
        {
            if (t.Tick % Every != 0) return null;
            if (askedTick != t.Tick)
            {
                askedTick = t.Tick;
                readyAtCall = World.AdvanceCalls + DelayCalls;
                pending = new ManualPending(() => { });
            }
            if (World.AdvanceCalls >= readyAtCall) pending.SetDone();
            return pending;
        }

        /// <summary>Records the tick and the Advance call, and a "delayed" event with a draw from the "delayed" stream.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            RanAtTick.Add(t.Tick);
            RanAtCall.Add(World.AdvanceCalls);
            t.World.Events.Record(new SimEvent("delayed", t.Tick, -1, null).With("value", t.Stream("delayed").Range(0, 1000)));
        }
    }
}
