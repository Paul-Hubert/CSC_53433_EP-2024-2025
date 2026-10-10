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

        Energy energy;
        Stamina stamina;

        public float BaseCost => baseCost;
        public float MoveCost => moveCost;
        public float RestCost => restCost;

        public override void Initialize()
        {
            energy = Species.Module<Energy>();
            stamina = Species.Module<Stamina>();
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

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null)
                report.Warning("V-20", this, "Metabolism without an Energy module charges nothing.");
        }
    }
}
