namespace EvoSim.Testing
{
    /// <summary>A test-only action defined outside the runtime assembly (T-CORE-02): it counts its calls and stays.</summary>
    /// <example>
    /// <code>
    /// var w = New().Flat(10, 10).Phase&lt;TracePhase&gt;()
    ///     .Species("prey", s =&gt; s.Population(2).Action&lt;ProbeAction&gt;().Sense&lt;ProbeSense&gt;())
    ///     .Build();
    /// Assert.IsInstanceOf&lt;ProbeAction&gt;(w.AllSpecies[0].Actions[0]);
    /// </code>
    /// </example>
    public class ProbeAction : AnimalAction
    {
        /// <summary>"do nothing (a test probe)".</summary>
        protected override string DefaultDescription => "do nothing (a test probe)";

        /// <summary>The calls to <see cref="Act"/> so far.</summary>
        public int Calls;
        /// <summary>Calls for an animal already killed this tick (must stay 0, ANIM-41).</summary>
        public int CallsWhileKilled;

        /// <summary>Counts the call (and whether the animal was already killed) and gives no intent: the animal stays.</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            Calls++;
            if (a.Killed) CallsWhileKilled++;
        }
    }
}
