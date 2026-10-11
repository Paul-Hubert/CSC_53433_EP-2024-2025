using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Stamina: the meters an animal can move before it must stop (05 §4). Each meter moved costs one point; standing
    /// still brings it back, which costs some energy until it is full (ANIM-21).
    /// </summary>
    /// <remarks>
    /// <para>Teaching-path exercise 8. The <c>stamina</c> stat starts full and is capped by the animal's
    /// <c>stamina.max</c> trait. In the act phase the locomotion gets <see cref="Budget"/> as the most it may move this
    /// tick (through <see cref="MoveContext.StaminaBudget"/>); after the animal acted, the metabolism calls
    /// <see cref="Settle"/>:</para>
    /// <list type="bullet">
    /// <item><description>moved more than 1 mm: stamina drops by the meters moved;</description></item>
    /// <item><description>still, and below its <c>stamina.max</c>: stamina rises by the <c>stamina.regen</c> trait (up to
    /// the maximum) and the animal pays <see cref="RegenEnergyCost"/> energy;</description></item>
    /// <item><description>still and full: nothing.</description></item>
    /// </list>
    /// <para>A busy animal never moves, so it recovers like any still animal. Other modules find stamina with
    /// <c>Species.Module&lt;Stamina&gt;()</c>, so a replacement derives from this class.</para>
    /// </remarks>
    public class Stamina : SpeciesModule
    {
        [SerializeField, Min(1f), Tooltip("Maximum stamina, in meters (a trait; reference prey 60, predators 30).")]
        float max = 60f;
        [SerializeField, Min(0f), Tooltip("Stamina recovered per tick without moving (a trait; reference 2).")]
        float regenPerTick = 2f;
        [SerializeField, Min(0f), Tooltip("Energy per tick while stamina recovers (reference 0.3).")]
        float regenEnergyCost = 0.3f;
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt (PROMPT-03).")]
        string promptRule = "Every meter moved costs stamina. Standing still brings it back, which costs some\n" +
                            "energy until stamina is full. Without stamina an animal cannot move.";

        /// <summary>
        /// The <c>stamina</c> stat, in meters: how far the animal may still move. Read and write it with
        /// <c>a[Value]</c>.
        /// </summary>
        public StatId Value { get; private set; }
        /// <summary>The <c>stamina.max</c> trait: the stat's cap, in meters (reference prey 60, predators 30).</summary>
        public TraitId Max { get; private set; }
        /// <summary>The <c>stamina.regen</c> trait: stamina recovered per still tick, in meters (reference 2).</summary>
        public TraitId Regen { get; private set; }
        /// <summary>Energy paid per tick while stamina recovers, in points (reference 0.3).</summary>
        public float RegenEnergyCost => regenEnergyCost;

        /// <summary>
        /// Declares the traits <c>stamina.max</c> (1 to 1 000) and <c>stamina.regen</c> (0 to 100), and the stat
        /// <c>stamina</c>, which starts full and is capped by <c>stamina.max</c>.
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            Max = b.DeclareTrait("stamina.max", max, 1f, 1000f);
            Regen = b.DeclareTrait("stamina.regen", regenPerTick, 0f, 100f);
            Value = b.DeclareStat("stamina", StatStart.FullTrait(Max), 0f, float.MaxValue, Max);
        }

        /// <summary>How far the animal may move this tick (ANIM-21).</summary>
        /// <param name="a">The animal about to move.</param>
        /// <returns>Its stamina, in meters, never below 0.</returns>
        public virtual float Budget(Animal a) => Mathf.Max(0f, a[Value]);

        /// <summary>Called by the metabolism after the animal acted: pay the meters, or recover (ANIM-21).</summary>
        /// <param name="a">The animal that just acted.</param>
        /// <param name="metersMoved">The meters its locomotion reported this tick; less than 1 mm counts as still (ANIM-23).</param>
        /// <param name="energy">The species' energy module, which pays for the recovery; null makes the recovery free.</param>
        /// <remarks>Without a metabolism, the act phase calls it directly.</remarks>
        public virtual void Settle(Animal a, float metersMoved, Energy energy)
        {
            if (metersMoved > Units.Epsilon) { a[Value] -= metersMoved; return; }
            if (a[Value] < a.Trait(Max))
            {
                a[Value] = a[Value] + a.Trait(Regen);           // clamped to stamina.max
                if (energy != null) energy.Pay(a, regenEnergyCost);
            }
        }

        /// <summary>Adds the stamina rule line to the prompt's rules (PROMPT-03).</summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        /// <summary>Sets the settings from code (WorldBuilder, scenarios), before the species declares its traits.</summary>
        /// <param name="maximum">Maximum stamina, in meters.</param>
        /// <param name="regen">Stamina recovered per still tick, in meters.</param>
        /// <param name="regenCost">Energy paid per tick while stamina recovers, in points.</param>
        public void Configure(float maximum, float regen = 2f, float regenCost = 0.3f)
        {
            max = maximum;
            regenPerTick = regen;
            regenEnergyCost = regenCost;
        }

        /// <summary>Sets the prompt rule line from code (WorldBuilder, tests).</summary>
        /// <param name="text">The new line; empty adds no line.</param>
        public void SetPromptRule(string text) => promptRule = text;
    }
}
