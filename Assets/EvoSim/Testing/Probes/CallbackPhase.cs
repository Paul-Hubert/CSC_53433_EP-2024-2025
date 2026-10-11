using System;

namespace EvoSim.Testing
{
    /// <summary>A test-only phase that runs a callback at its place in the tick (to look at the state between two phases).</summary>
    /// <example>
    /// Counts the eggs laid by the end of the act phase, before the breed phase:
    /// <code>
    /// int laidAfterAct = -1;
    /// var w = New(name: "eggs").Ecology(40f, 0f, 0f, regrow: 0f, prey: s =&gt; s.LifeRules(40, 0))
    ///     .Phase&lt;ActPhase&gt;()
    ///     .Phase&lt;CallbackPhase&gt;(p =&gt; p.OnRun = t =&gt; laidAfterAct = t.World.FindSpecies("prey").Counters.EggsLaid)
    ///     .Phase&lt;BreedPhase&gt;().Phase&lt;HatchPhase&gt;().Build();
    /// </code>
    /// </example>
    public class CallbackPhase : TickPhase
    {
        /// <summary>Called with the tick's context each time the phase runs; null = nothing.</summary>
        public Action<TickContext> OnRun;

        /// <summary>Calls <see cref="OnRun"/>.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t) => OnRun?.Invoke(t);
    }
}
