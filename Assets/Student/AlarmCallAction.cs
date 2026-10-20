using EvoSim;
using UnityEngine;

/// <summary>An action done in passing: the animal gives an alarm call that its kin hear, and goes on with its next choice.</summary>
/// <remarks>
/// <para><b>What it adds.</b> A behaviour the brain can choose, <c>alarm</c>. It is an overlay (ACT-07): when the brain's
/// draw picks it, the engine also draws a second action, the carrier, from the rest of the row (flee, hide, eat…), which
/// moves the animal. The call itself happens once, on the tick of the decision: the caller pays a little energy, and its
/// call is added to the world's <see cref="AlarmCalls"/>; kin within <c>range</c> meters hear it through their
/// <see cref="AlarmSense"/>. Put a <see cref="TextGene"/> on the same GameObject so evolution can tune how often animals
/// call.</para>
/// <para><b>Mechanism.</b> <see cref="Overlay"/> is true. <see cref="Act"/> asks for the interaction <see cref="Call"/>
/// only: the act phase ignores an overlay's intent, so the carrier's movement stands (ACT-06). The interaction runs after
/// the carrier's, once the animal moved, and is the only place that changes the animal (its energy) and the world (a new
/// call). Its reach is unlimited: it always happens. It draws no random number.</para>
/// <para><b>Why an overlay.</b> A chosen action runs every tick until the next decision (ACT-02), up to a whole decision
/// period. A first version stood still to call, next to the predator, for that long: it was caught, and evolution would
/// only have silenced the gene. As an overlay the call costs no movement: the animal flees, hides or eats as its row
/// says. It may also call with no predator in sight, a false alarm: the gene and the brain decide whether that pays.</para>
/// <para><b>In a scene.</b> A child of the species' <c>Actions</c> object named <c>alarm</c>, with this component and a
/// text gene, beside at least one action that is not an overlay (V-19). The World must hold an <see cref="AlarmCalls"/>
/// service (V-26 otherwise).</para>
/// </remarks>
[RequiresModule(typeof(AlarmCalls))]
public class AlarmCallAction : AnimalAction
{
    [SerializeField, Min(1f), Tooltip("How far the call carries, in meters (20, the reference vision).")]
    float range = 20f;
    [SerializeField, Min(0f), Tooltip("Energy paid for each call, in points (0.5).")]
    float cost = 0.5f;
    [SerializeField, Tooltip("The sense whose token says whether a predator is in sight (CTRL-10).")]
    string relevantSense = "Predator";

    AlarmCalls calls;   // the world's alarm calls, found in Initialize; null when the world has none
    Energy energy;      // the species' energy module, found in Initialize; null when the species has none

    /// <summary>How far the call carries, in meters.</summary>
    public float Range => range;

    /// <summary>The energy paid for each call, in points.</summary>
    public float Cost => cost;

    /// <summary>True: the call is done in passing, beside a carrier drawn from the rest of the row (ACT-07).</summary>
    public override bool Overlay => true;

    /// <inheritdoc/>
    protected override string DefaultDescription => $"give an alarm call that kin within {Bands.Number(range)} meters hear";

    /// <summary>Finds the world's <see cref="AlarmCalls"/> and the species' <see cref="Energy"/>.</summary>
    /// <inheritdoc/>
    public override void Initialize()
    {
        calls = World.Service<AlarmCalls>();
        energy = Species.Module<Energy>();
    }

    /// <summary>
    /// Asks for the call, and nothing else: no intent, the carrier moves the animal (ACT-07). Without an
    /// <see cref="AlarmCalls"/> service it does nothing.
    /// </summary>
    /// <inheritdoc/>
    public override void Act(Animal a, ActContext c)
    {
        if (calls != null) c.OnArrival(new Call(this));
    }

    /// <summary>Relevant when a predator is in sight: that is when a call can save kin (CTRL-10).</summary>
    /// <inheritdoc/>
    public override bool IsRelevant(ObservationView o)
    {
        var t = o.Token(relevantSense);
        return t != null && t != "none";
    }

    /// <summary>Adds the rule line "An alarm call costs 0.5 energy; kin within 20 meters hear it." to the prompt.</summary>
    /// <inheritdoc/>
    public override void WritePromptRules(PromptWriter w)
    {
        base.WritePromptRules(w);
        w.Rule($"An alarm call costs {Bands.Number(cost)} energy; kin within {Bands.Number(range)} meters hear it.");
    }

    /// <summary>Sets the action from code (tests).</summary>
    /// <param name="callRange">How far the call carries, in meters.</param>
    /// <param name="energyCost">The energy paid for each call, in points.</param>
    public void Configure(float callRange, float energyCost)
    {
        range = callRange;
        cost = energyCost;
    }

    /// <summary>The effect of calling: the caller pays energy and its call is recorded where it stands.</summary>
    sealed class Call : IInteraction
    {
        readonly AlarmCallAction action;   // the action whose settings, service and energy module are used

        /// <summary>Makes the interaction for one call.</summary>
        /// <param name="owner">The action that asked for it.</param>
        public Call(AlarmCallAction owner) { action = owner; }

        /// <summary>Unlimited: the call always happens, wherever the carrier took the animal.</summary>
        public float Reach => float.PositiveInfinity;

        /// <summary>Takes the cost from the caller's energy and records its call where it ended its move this tick.</summary>
        /// <inheritdoc/>
        public void Apply(Animal a, InteractionContext c)
        {
            if (action.energy != null) action.energy.Pay(a, action.cost);
            action.calls.Register(a, c.Tick, action.range);
        }
    }
}
