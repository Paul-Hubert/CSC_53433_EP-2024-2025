using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a tick costs, once per living animal after it acted (ANIM-20), from the meters its locomotion reported,
    /// its action and whether it was busy (05 §4 reference table).
    /// </summary>
    public class Metabolism : SpeciesModule
    {
        [SerializeField, Min(0f), Tooltip("Energy per tick when moving or doing anything but rest (reference 0.7).")]
        float baseCost = 0.7f;
        [SerializeField, Min(0f), Tooltip("Energy per meter moved (reference 0.5).")]
        float moveCost = 0.5f;
        [SerializeField, Min(0f), Tooltip("Energy per tick resting or busy, without moving (reference 0.2).")]
        float restCost = 0.2f;
        [SerializeField, Tooltip("A trait the tick's cost grows with (09 §5: a number gene's price), e.g. stamina.max; empty = none (reference).")]
        string costTrait = "";
        [SerializeField, Min(0f), Tooltip("Extra energy per tick per unit of that trait (e.g. 0.005: 60 stamina costs 0.3 a tick).")]
        float costPerUnit = 0.005f;

        Energy energy;
        Stamina stamina;
        TraitId costId;

        public float BaseCost => baseCost;
        public float MoveCost => moveCost;
        public float RestCost => restCost;

        public string CostTrait => costTrait;
        public float CostPerUnit => costPerUnit;

        public override void Declare(SpeciesBuilder b) => b.DeclareCost(costTrait);

        public override void Initialize()
        {
            energy = Species.Module<Energy>();
            stamina = Species.Module<Stamina>();
            costId = string.IsNullOrEmpty(costTrait) ? default : Species.Declarations.FindTrait(costTrait);
        }

        /// <summary>Charges the tick (ANIM-20): moved, busy, resting or standing still; stamina pays or recovers.</summary>
        public virtual void Settle(Animal a, float metersMoved, float extraCost, bool wasBusy)
        {
            if (energy != null)
            {
                float cost;
                if (metersMoved > Units.Epsilon) cost = baseCost + moveCost * metersMoved;    // ANIM-23: 1 mm counts as still
                else if (wasBusy) cost = restCost;
                else cost = a.CurrentAction != null && a.CurrentAction.Rests ? restCost : baseCost;
                if (costId.IsValid) cost += costPerUnit * a.Trait(costId);                    // 09 §5
                energy.Pay(a, cost + Mathf.Max(0f, extraCost));
            }
            stamina?.Settle(a, metersMoved, energy);
        }

        public void Configure(float baseTick, float perMeter, float rest)
        {
            baseCost = baseTick;
            moveCost = perMeter;
            restCost = rest;
        }

        /// <summary>A tick cost that grows with a trait (09 §5); an empty trait removes it.</summary>
        public void SetTraitCost(string trait, float perUnit)
        {
            costTrait = trait ?? "";
            costPerUnit = perUnit;
        }

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null)
                report.Warning("V-26", this, "Metabolism without an Energy module charges nothing.", BreedPhase.AddModule<Energy>(Species, "Energy"));
            if (!string.IsNullOrEmpty(costTrait) && !Species.Declarations.FindTrait(costTrait).IsValid)
                report.Error("V-45", this, $"Metabolism's cost grows with the trait '{costTrait}', which no module declares.");
        }
    }
}
