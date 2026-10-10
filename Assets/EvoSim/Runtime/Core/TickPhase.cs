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

        /// <summary>V-08's fix: move this phase just after another one, when both sit under the same GameObject.</summary>
        protected ValidationFix PlaceAfter(TickPhase other)
        {
            if (other == null || other.transform.parent != transform.parent || other.transform == transform) return null;
            var me = transform;
            return new ValidationFix($"Move after {other.PhaseName}", () =>
            {
                int target = other.transform.GetSiblingIndex();
                me.SetSiblingIndex(me.GetSiblingIndex() < target ? target : target + 1);
            });
        }

        /// <summary>V-08's fix: move this phase just before another one, when both sit under the same GameObject.</summary>
        protected ValidationFix PlaceBefore(TickPhase other)
        {
            if (other == null || other.transform.parent != transform.parent || other.transform == transform) return null;
            var me = transform;
            return new ValidationFix($"Move before {other.PhaseName}", () =>
            {
                int target = other.transform.GetSiblingIndex();
                me.SetSiblingIndex(me.GetSiblingIndex() > target ? target : target - 1);
            });
        }
    }
}
