using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A behaviour the brain can choose (ACT-01). Each tick it gives an intent and maybe an interaction; it never moves
    /// anything itself.
    /// </summary>
    /// <remarks>
    /// <para>Derive from it, implement <see cref="Act"/> and <see cref="DefaultDescription"/>, and place the component on
    /// its own GameObject under a species. The GameObject's name is the action's name; the depth-first order of the
    /// species' actions is their order in the prompt and in the brain's answer (SPEC-03). A <see cref="TextGene"/> on the
    /// same GameObject, or on a child of it, is bound to it (GENE-05).</para>
    /// <para>Once the brain chose it, <see cref="Act"/> runs every tick until the next decision, or until the animal
    /// becomes busy or dies (ACT-02). An action done in passing (a call) is an <see cref="Overlay"/>: it runs beside a
    /// second, moving action (ACT-07). What <see cref="Act"/> must keep:</para>
    /// <list type="bullet">
    /// <item><description>Plan only through the <see cref="ActContext"/>: never set the position, stats or flags of any
    /// animal (ACT-06). Effects on others go through interactions, which check the reach and the diet.</description></item>
    /// <item><description>Target only what the context's queries return: what the animal's senses could report
    /// (ACT-05).</description></item>
    /// <item><description>With nothing to act on, call <see cref="ActContext.Search"/> (ACT-04).</description></item>
    /// <item><description>Never read gene sentences: genes reach an action only through traits (ACT-20).</description></item>
    /// <item><description>Draw random numbers only from the species' streams (<c>World.Random.For(Species, "actions")</c>),
    /// never from <c>UnityEngine.Random</c>, so that a seed repeats a run.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// The reference <see cref="FollowAction"/>, without its relevance test:
    /// <code>
    /// public class FollowAction : AnimalAction
    /// {
    ///     protected override string DefaultDescription =&gt; "move toward the nearest other animal";
    ///
    ///     [SerializeField, Min(0f), Tooltip("Stop this far from the followed animal, in meters (reference 1).")]
    ///     float stopAt = 1f;
    ///
    ///     public override void Act(Animal a, ActContext c)
    ///     {
    ///         var kin = c.NearestAnimal(a, AnimalSet.Kin);
    ///         if (kin == null) { c.Search(); return; }
    ///         c.WalkTo(kin.Value.Animal, stopAt);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class AnimalAction : SpeciesModule
    {
        /// <summary>
        /// The action's line in the prompt, after its name; empty uses <see cref="DefaultDescription"/> (PROMPT-01,
        /// PROMPT-07).
        /// </summary>
        [SerializeField, TextArea(1, 4), Tooltip("The action's line in the prompt, after its name (PROMPT-01, PROMPT-07).")]
        protected string description = "";

        /// <summary>The action's name, unique in its species (ACT-01): its GameObject's name.</summary>
        public string Name => gameObject.name;
        /// <summary>The prompt line: the Inspector's text, or <see cref="DefaultDescription"/> when it is empty.</summary>
        public string Description => string.IsNullOrEmpty(description) ? DefaultDescription : description;

        /// <summary>
        /// The reference prompt line of this action (08 §7), used when the Inspector field is empty. Every action should
        /// override it with a short phrase, such as "go to the nearest visible food and eat it".
        /// </summary>
        protected virtual string DefaultDescription => "";

        /// <summary>
        /// The text gene bound to this action (GENE-05), or null (V-05). The species binds it when it builds its action
        /// order.
        /// </summary>
        public TextGene Gene { get; internal set; }

        /// <summary>
        /// True for an action that chases and strikes animals: senses such as the sample <c>ChasedSense</c> read it.
        /// Override it to return true in a hunting action of your own.
        /// </summary>
        public virtual bool Hunts => false;

        /// <summary>
        /// This action's index in the species' action order (SPEC-03): its place in the prompt and in the brain's
        /// probabilities, and the value of <see cref="Animal.Action"/> while it is chosen; -1 before the species is built.
        /// </summary>
        public int Index { get; internal set; } = -1;

        /// <summary>Every tick while this action is chosen: say where to go, and what to do on arrival (ACT-02, ACT-03).</summary>
        /// <param name="a">The animal that acts: alive and not busy.</param>
        /// <param name="c">
        /// The context to query and plan through; <see cref="ActContext.Begin"/> was already called for
        /// <paramref name="a"/>.
        /// </param>
        /// <remarks>
        /// Called by the act phase, through <see cref="ActAll"/>, once per tick for each animal that chose this action.
        /// Look the target up again each tick, so that a moving target is followed (ACT-02). The plan is the last intent
        /// and the last interaction the method set; doing nothing means stay.
        /// </remarks>
        public abstract void Act(Animal a, ActContext c);

        /// <summary>
        /// The batch entry point (20 §10, ARCH-12): the act phase plans every animal of the group (all chose this action)
        /// through it. The default calls Act for each; an override must give the plans Act would give.
        /// </summary>
        /// <param name="group">
        /// The animals to plan, all of which chose this action: one animal in the sequential act orders, every such animal
        /// in the simultaneous one (ACT-30).
        /// </param>
        /// <param name="c">The shared context; call <see cref="ActContext.Begin"/> before planning each animal.</param>
        /// <param name="plans">Where to write each animal's plan, at the same index as in <paramref name="group"/>.</param>
        /// <remarks>
        /// Override it only to plan many animals at once (a job, a batched query). The single-animal <see cref="Act"/>
        /// stays the readable reference: the override must give the same plans, with the same random draws, and pass
        /// the same conformance tests.
        /// </remarks>
        public virtual void ActAll(IReadOnlyList<Animal> group, ActContext c, Span<ActPlan> plans)
        {
            for (int i = 0; i < group.Count; i++)
            {
                c.Begin(group[i]);
                Act(group[i], c);
                plans[i] = c.Plan;
            }
        }

        /// <summary>
        /// Directed tests: is this observation relevant for this action (CTRL-10)? For eat, food in sight; for flee, a
        /// threat; for rest, any observation (the default).
        /// </summary>
        /// <param name="o">An observation of this species, read by sense label with <see cref="ObservationView.Token"/>.</param>
        /// <returns>True when the action would have something to act on in that situation.</returns>
        public virtual bool IsRelevant(ObservationView o) => true;

        /// <summary>
        /// True for actions that rest: the metabolism charges its rest cost, not its base cost, when the animal stays
        /// still (05 §4).
        /// </summary>
        public virtual bool Rests => false;

        /// <summary>True for actions that seek a partner: the breed phase breeds animals that chose one (REPRO-02).</summary>
        public virtual bool Mates => false;

        /// <summary>
        /// True for an action done in passing (a call, a scent mark): when the brain's draw picks it, the animal also does a
        /// second action, the carrier, drawn from the rest of the row; this one adds its interaction only (ACT-07).
        /// </summary>
        /// <remarks>
        /// <para>The carrier is drawn from the same probabilities with every overlay set to 0, so it is what the animal
        /// would have done without the overlay (DEC-20). Each tick the carrier plans the intent and its interaction as
        /// usual; then the overlay's <see cref="Act"/> runs in the same context: its intent is ignored (the carrier moves
        /// the animal) and its interaction applies after the carrier's, measured from the point its own intent names
        /// (07 §3). It acts for <see cref="OverlayTicks"/> ticks, and ends when the animal becomes busy or dies.</para>
        /// <para>An overlay must not call <see cref="ActContext.Search"/> or <see cref="ActContext.Wander"/>: its intent
        /// is thrown away. It may not mate or hunt (V-27), and a species needs at least one action that is not an overlay
        /// (V-19).</para>
        /// </remarks>
        /// <example>
        /// A scent mark left in passing:
        /// <code>
        /// public class MarkAction : AnimalAction
        /// {
        ///     public override bool Overlay =&gt; true;
        ///     protected override string DefaultDescription =&gt; "leave a scent mark; you also do your next choice";
        ///
        ///     public override void Act(Animal a, ActContext c) =&gt; c.OnArrival(new Mark());   // no intent: the carrier moves
        /// }
        /// </code>
        /// </example>
        public virtual bool Overlay => false;

        /// <summary>
        /// How many ticks an overlay acts, counting the tick of its decision; 0 = until the next decision. Default 1: once.
        /// Ignored unless <see cref="Overlay"/> is true (ACT-07).
        /// </summary>
        public virtual int OverlayTicks => 1;

        /// <summary>Adds the action's line to the prompt's action list: <c>- name: description</c> (PROMPT-01).</summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) => w.Action(Name, Description);

        /// <summary>Sets the prompt line from code (WorldBuilder, tests).</summary>
        /// <param name="text">The new line; empty uses <see cref="DefaultDescription"/>.</param>
        public void SetDescription(string text) => description = text;
    }
}
