namespace EvoSim
{
    /// <summary>One step of a tick. The World runs its phases in hierarchy order (TICK-01).</summary>
    public abstract class TickPhase : WorldModule
    {
        /// <summary>Work this phase must wait for (brain answers, mutations); null = none (TICK-03).</summary>
        public virtual Pending WaitsFor(TickContext t) => null;

        /// <summary>Do this phase's work for the current tick.</summary>
        public abstract void Run(TickContext t);

        /// <summary>A short name for logs and the profiler.</summary>
        public virtual string PhaseName => GetType().Name;
    }
}
