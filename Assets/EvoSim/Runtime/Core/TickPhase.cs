namespace EvoSim
{
    /// <summary>One step of a tick. The World runs its phases in hierarchy order (TICK-01).</summary>
    /// <remarks>
    /// <para>Derive from it, implement <see cref="Run"/>, and place the component among the World's phases (the children
    /// of its <c>Phases</c> object in the reference scenes): its place there is its place in every tick. Phases run in
    /// the same order every tick (TICK-07); a phase with nothing to do returns at once.</para>
    /// <para>A phase changes only what it is responsible for; what it changes is visible to the phases after it in the
    /// same tick (TICK-02). It draws random numbers only from named streams (<see cref="TickContext.Stream(string)"/>,
    /// RAND-02) and loops only over ordered lists (RAND-05), so a seed always gives the same run.</para>
    /// <para>A phase that needs answers from a service (brains, the mutator) returns them as a <see cref="Pending"/> from
    /// <see cref="WaitsFor"/>: the World runs it only once they are in (TICK-03).</para>
    /// </remarks>
    /// <example>
    /// Recipe 22 §9, a phase placed before Environment that slows regrowth in winter:
    /// <code>
    /// public class SeasonsPhase : TickPhase
    /// {
    ///     [SerializeField] int yearLength = 2000;
    ///     [SerializeField] float winterRegrowFactor = 0.2f;
    ///     FoodGrid food;
    ///
    ///     public override void Initialize() => food = World.Service&lt;FoodGrid&gt;("Grass");
    ///
    ///     public override void Run(TickContext t)
    ///     {
    ///         if (food != null) food.RegrowFactor = t.Tick % yearLength &gt;= yearLength / 2 ? winterRegrowFactor : 1f;
    ///     }
    /// }
    /// </code>
    /// A phase that waits for the brains' answers (the Choose actions phase):
    /// <code>
    /// public override Pending WaitsFor(TickContext t) => World.Decisions.Pending;
    /// </code>
    /// </example>
    public abstract class TickPhase : WorldModule
    {
        /// <summary>Work this phase must wait for (brain answers, mutations); null = none (TICK-03).</summary>
        /// <param name="t">The current tick's context.</param>
        /// <returns>What must be done before <see cref="Run"/>, or null when nothing is awaited. A Pending already done
        /// lets the phase run at once.</returns>
        /// <remarks>
        /// The World asks before every attempt to run the phase: in Responsive mode, again at every Advance call while
        /// the tick waits. Return the same work for the same tick, and don't change the world or draw random numbers
        /// here, so both wait modes give the same run (SPACE-14, RAND-11).
        /// </remarks>
        public virtual Pending WaitsFor(TickContext t) => null;

        /// <summary>Do this phase's work for the current tick.</summary>
        /// <param name="t">The current tick's context: the World, the tick number and the random streams.</param>
        /// <remarks>Called once per tick, after <see cref="WaitsFor"/>'s work is done. An exception stops the run.</remarks>
        public abstract void Run(TickContext t);

        /// <summary>A short name for logs and the profiler.</summary>
        /// <remarks>By default the class name; the profiler marker of the phase is <c>EvoSim.&lt;PhaseName&gt;</c>.</remarks>
        public virtual string PhaseName => GetType().Name;

        /// <summary>V-08's fix: move this phase just after another one, when both sit under the same GameObject.</summary>
        /// <param name="other">The phase this one must follow, or null.</param>
        /// <returns>The fix, or null when <paramref name="other"/> is null, is this phase, or has another parent.</returns>
        /// <example>
        /// <code>
        /// report.Error("V-08", this, "Ask brains must come after the Sense phase.", PlaceAfter(World.Phase&lt;SensePhase&gt;()));
        /// </code>
        /// </example>
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
        /// <param name="other">The phase this one must precede, or null.</param>
        /// <returns>The fix, or null when <paramref name="other"/> is null, is this phase, or has another parent.</returns>
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
