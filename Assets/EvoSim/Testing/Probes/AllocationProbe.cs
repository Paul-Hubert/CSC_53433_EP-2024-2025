namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase placed just before and just after another phase: the pair measures the bytes the main thread
    /// allocated in between, every tick (R-07: no allocation per tick in the act phase after warm-up).
    /// </summary>
    /// <example>
    /// <code>
    /// var before = new GameObject("alloc before act").AddComponent&lt;AllocationProbe&gt;();
    /// before.transform.SetParent(phases, false);
    /// before.transform.SetSiblingIndex(act.GetSiblingIndex());
    /// var after = new GameObject("alloc after act").AddComponent&lt;AllocationProbe&gt;();
    /// after.transform.SetParent(phases, false);
    /// after.transform.SetSiblingIndex(act.GetSiblingIndex() + 1);
    /// after.Start = before;
    /// // ... Initialize, warm up, then:
    /// after.Measuring = true;
    /// w.Advance(200);
    /// long allocating = after.PerTick.Count(b =&gt; b &gt; 0);
    /// </code>
    /// </example>
    public class AllocationProbe : TickPhase
    {
        /// <summary>On the probe placed after the measured phase: the probe placed before it. Null on that first probe itself.</summary>
        public AllocationProbe Start;
        /// <summary>On the first probe: the bytes allocated so far when it last ran.</summary>
        public long Before;
        /// <summary>On the second probe: the bytes allocated between the two probes, one entry per tick while measuring.</summary>
        public readonly System.Collections.Generic.List<long> PerTick = new System.Collections.Generic.List<long>(4096);
        /// <summary>On the second probe: true to record <see cref="PerTick"/>; set it after the warm-up ticks.</summary>
        public bool Measuring;

        /// <summary>Allocated bytes so far: per thread where the runtime counts them, else the managed heap's size.</summary>
        static long Allocated()
        {
            long perThread = System.GC.GetAllocatedBytesForCurrentThread();
            return perThread > 0 ? perThread : System.GC.GetTotalMemory(false);
        }

        /// <summary>The first probe notes the bytes allocated so far; the second records the difference since the first.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            long now = Allocated();
            if (Start == null) { Before = now; return; }
            if (Measuring) PerTick.Add(System.Math.Max(0, now - Start.Before));   // a collection in between reads as 0
        }
    }
}
