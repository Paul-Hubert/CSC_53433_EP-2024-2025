using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Stamina: the meters an animal can move before it must stop (05 §4). Each meter moved costs one point; standing
    /// still brings it back, which costs some energy until it is full (ANIM-21).
    /// </summary>
    public class Stamina : SpeciesModule
    {
        [SerializeField, Min(1f), Tooltip("Maximum stamina, in meters (a trait; reference prey 60, predators 30).")]
        float max = 60f;
        [SerializeField, Min(0f), Tooltip("Stamina recovered per tick without moving (a trait; reference 2).")]
        float regenPerTick = 2f;
        [SerializeField, Min(0f), Tooltip("Energy per tick while stamina recovers (reference 0.3).")]
        float regenEnergyCost = 0.3f;
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt (PROMPT-03).")]
        string promptRule = "Every meter moved costs stamina. Standing still brings it back, which costs some " +
                            "energy until stamina is full. Without stamina an animal cannot move.";

        public StatId Value { get; private set; }
        public TraitId Max { get; private set; }
        public TraitId Regen { get; private set; }
        public float RegenEnergyCost => regenEnergyCost;

        public override void Declare(SpeciesBuilder b)
        {
            Max = b.DeclareTrait("stamina.max", max, 1f, 1000f);
            Regen = b.DeclareTrait("stamina.regen", regenPerTick, 0f, 100f);
            Value = b.DeclareStat("stamina", StatStart.FullTrait(Max), 0f, float.MaxValue, Max);
        }

        /// <summary>How far the animal may move this tick (ANIM-21).</summary>
        public float Budget(Animal a) => Mathf.Max(0f, a[Value]);

        /// <summary>Called by the metabolism after the animal acted: pay the meters, or recover (ANIM-21).</summary>
        public void Settle(Animal a, float metersMoved, Energy energy)
        {
            if (metersMoved > Units.Epsilon) { a[Value] -= metersMoved; return; }
            if (a[Value] < a.Trait(Max))
            {
                a[Value] = a[Value] + a.Trait(Regen);           // clamped to stamina.max
                if (energy != null) energy.Pay(a, regenEnergyCost);
            }
        }

        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        public void Configure(float maximum, float regen = 2f, float regenCost = 0.3f)
        {
            max = maximum;
            regenPerTick = regen;
            regenEnergyCost = regenCost;
        }

        public void SetPromptRule(string text) => promptRule = text;
    }
}
