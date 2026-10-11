using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a tick costs, once per living animal after it acted (ANIM-20), from the meters its locomotion reported,
    /// its action and whether it was busy (05 §4 reference table).
    /// </summary>
    /// <remarks>
    /// <para>The act phase calls <see cref="Settle"/> for each living animal right after it moved and its interaction
    /// applied. The energy paid that tick is:</para>
    /// <list type="bullet">
    /// <item><description>moved (more than 1 mm): <c>baseCost + moveCost × meters</c> (reference 0.7 + 0.5 per meter);</description></item>
    /// <item><description>still and busy at the start of the tick: <c>restCost</c> (0.2);</description></item>
    /// <item><description>still, with an action that rests (<see cref="AnimalAction.Rests"/>): <c>restCost</c> (0.2);</description></item>
    /// <item><description>still otherwise: <c>baseCost</c> (0.7);</description></item>
    /// </list>
    /// <para>plus <c>costPerUnit</c> × the cost trait when one is set (09 §5), plus the locomotion's extra cost. Then the
    /// species' <see cref="Stamina"/> pays the meters or recovers (<see cref="Stamina.Settle"/>); recovering costs
    /// <see cref="Stamina.RegenEnergyCost"/> more energy per tick (reference 0.3).</para>
    /// </remarks>
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

        Energy energy;      // the species' energy module, or null: then no energy is charged
        Stamina stamina;    // the species' stamina module, or null: then no stamina is paid or recovered
        TraitId costId;     // the trait the cost grows with; invalid when costTrait is empty

        /// <summary>Energy per tick when moving, or when still with an action that doesn't rest, in points (reference 0.7).</summary>
        public float BaseCost => baseCost;
        /// <summary>Energy per meter moved, in points (reference 0.5).</summary>
        public float MoveCost => moveCost;
        /// <summary>Energy per tick when still and busy, or still with a resting action, in points (reference 0.2).</summary>
        public float RestCost => restCost;

        /// <summary>The trait the tick's cost grows with (09 §5), such as <c>stamina.max</c>; empty for none (reference).</summary>
        public string CostTrait => costTrait;
        /// <summary>Extra energy per tick per unit of <see cref="CostTrait"/>, in points.</summary>
        public float CostPerUnit => costPerUnit;

        /// <summary>
        /// Declares that a cost depends on <see cref="CostTrait"/> (09 §5), so that validation knows the number gene on
        /// it has a price (V-47).
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) => b.DeclareCost(costTrait);

        /// <summary>Finds the species' <see cref="Energy"/> and <see cref="Stamina"/> modules and the cost trait.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            energy = Species.Module<Energy>();
            stamina = Species.Module<Stamina>();
            costId = string.IsNullOrEmpty(costTrait) ? default : Species.Declarations.FindTrait(costTrait);
        }

        /// <summary>Charges the tick (ANIM-20): moved, busy, resting or standing still; stamina pays or recovers.</summary>
        /// <param name="a">The animal that just acted.</param>
        /// <param name="metersMoved">
        /// The meters its locomotion reported this tick (MOVE-06); less than 1 mm counts as still (ANIM-23).
        /// </param>
        /// <param name="extraCost">
        /// The locomotion's extra cost for this move (<see cref="Locomotion.ExtraCost"/>); a negative value counts as
        /// 0.
        /// </param>
        /// <param name="wasBusy">True when the animal was busy at the start of the tick.</param>
        /// <remarks>
        /// Called by the act phase once per living animal per tick. A species without a metabolism still settles its
        /// stamina: the act phase then calls <see cref="Stamina.Settle"/> itself.
        /// </remarks>
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

        /// <summary>Sets the three costs from code (WorldBuilder, scenarios).</summary>
        /// <param name="baseTick">Energy per tick when moving or not resting, in points.</param>
        /// <param name="perMeter">Energy per meter moved, in points.</param>
        /// <param name="rest">Energy per tick when still and busy or resting, in points.</param>
        public void Configure(float baseTick, float perMeter, float rest)
        {
            baseCost = baseTick;
            moveCost = perMeter;
            restCost = rest;
        }

        /// <summary>A tick cost that grows with a trait (09 §5); an empty trait removes it.</summary>
        /// <param name="trait">A trait's name, such as <c>stamina.max</c>; empty or null removes the cost.</param>
        /// <param name="perUnit">Extra energy per tick per unit of the trait, in points.</param>
        public void SetTraitCost(string trait, float perUnit)
        {
            costTrait = trait ?? "";
            costPerUnit = perUnit;
        }

        /// <summary>
        /// Warns when the species has no Energy module (V-26, with a fix) and reports a cost trait that no module
        /// declares (V-45).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null)
                report.Warning("V-26", this, "Metabolism without an Energy module charges nothing.", BreedPhase.AddModule<Energy>(Species, "Energy"));
            if (!string.IsNullOrEmpty(costTrait) && !Species.Declarations.FindTrait(costTrait).IsValid)
                report.Error("V-45", this, $"Metabolism's cost grows with the trait '{costTrait}', which no module declares.");
        }
    }
}
