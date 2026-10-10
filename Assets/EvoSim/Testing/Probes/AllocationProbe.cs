namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase placed just before and just after another phase: the pair measures the bytes the main thread
    /// allocated in between, every tick (R-07: no allocation per tick in the act phase after warm-up).
    /// </summary>
    public class AllocationProbe : TickPhase
    {
        /// <summary>The probe placed before the measured phase; null on that probe itself.</summary>
        public AllocationProbe Start;
        public long Before;
        /// <summary>Bytes allocated between the two probes, per tick, once measuring.</summary>
        public readonly System.Collections.Generic.List<long> PerTick = new System.Collections.Generic.List<long>(4096);
        public bool Measuring;

        /// <summary>Allocated bytes so far: per thread where the runtime counts them, else the managed heap's size.</summary>
        static long Allocated()
        {
            long perThread = System.GC.GetAllocatedBytesForCurrentThread();
            return perThread > 0 ? perThread : System.GC.GetTotalMemory(false);
        }

        public override void Run(TickContext t)
        {
            long now = Allocated();
            if (Start == null) { Before = now; return; }
            if (Measuring) PerTick.Add(System.Math.Max(0, now - Start.Before));   // a collection in between reads as 0
        }
    }
}
