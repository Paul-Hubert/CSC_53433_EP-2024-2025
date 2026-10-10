using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase that waits, every <see cref="Every"/> ticks, for an answer that arrives <see cref="DelayCalls"/>
    /// Advance calls after it was asked (like a slow brain); in Freeze mode Wait() delivers it at once.
    /// </summary>
    public class DelayedPhase : TickPhase
    {
        public int DelayCalls = 3;
        public int Every = 1;
        /// <summary>The ticks at which Run happened, and the Advance call it happened in.</summary>
        public List<int> RanAtTick = new List<int>();
        public List<int> RanAtCall = new List<int>();

        int askedTick = -1, readyAtCall;
        ManualPending pending;

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

        public override void Run(TickContext t)
        {
            RanAtTick.Add(t.Tick);
            RanAtCall.Add(World.AdvanceCalls);
            t.World.Events.Record(new SimEvent("delayed", t.Tick, -1, null).With("value", t.Stream("delayed").Range(0, 1000)));
        }
    }
}
