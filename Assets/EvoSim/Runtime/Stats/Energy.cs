using UnityEngine;

namespace EvoSim
{
    /// <summary>The energy stat (05 §4): founders and newcomers start at 60, the maximum is 100. Gains are clamped (ANIM-22).</summary>
    /// <remarks>
    /// Meals add energy through <see cref="Gain"/>; the <see cref="Metabolism"/>, the stamina recovery and litters take
    /// it through <see cref="Pay"/>. The stat has no minimum: an animal at 0 or below dies of <see cref="Starvation"/> in
    /// the death phase (ANIM-40). Babies start with the energy their litter gives them, not with <see cref="Start"/>.
    /// </remarks>
    public class Energy : SpeciesModule
    {
        [SerializeField, Min(1f), Tooltip("Maximum energy (reference 100).")]
        float max = 100f;
        [SerializeField, Min(0f), Tooltip("Energy of founders and newcomers (reference 60). Babies get the litter's child energy.")]
        float start = 60f;

        /// <summary>The <c>energy</c> stat, in points: read and write it with <c>a[Value]</c>.</summary>
        public StatId Value { get; private set; }
        /// <summary>The maximum energy setting, in points (reference 100).</summary>
        public float Max => max;
        /// <summary>The energy of founders and newcomers, in points (reference 60).</summary>
        public float Start => start;

        /// <summary>
        /// Declares the <c>energy</c> stat: it starts at <see cref="Start"/>, has no minimum and is capped at
        /// <see cref="Max"/>.
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) =>
            Value = b.DeclareStat("energy", StatStart.Fixed(start), float.MinValue, max);   // may go below 0 (REPRO-10)

        /// <summary>Adds energy from a meal, clamped to the maximum (ANIM-22).</summary>
        /// <param name="a">The animal that eats.</param>
        /// <param name="amount">The energy gained, in points.</param>
        /// <remarks>Every meal goes through it: grazing, kills, carcass portions, eggs and <see cref="InteractionContext.Feed"/>.</remarks>
        public virtual void Gain(Animal a, float amount) => a[Value] += amount;

        /// <summary>Removes energy (costs, paying for a litter); it may fall below zero.</summary>
        /// <param name="a">The animal that pays.</param>
        /// <param name="amount">The energy paid, in points.</param>
        public virtual void Pay(Animal a, float amount) => a[Value] -= amount;

        /// <summary>Sets the settings from code (WorldBuilder, scenarios), before the species declares its stats.</summary>
        /// <param name="maximum">Maximum energy, in points.</param>
        /// <param name="startEnergy">Energy of founders and newcomers, in points.</param>
        public void Configure(float maximum, float startEnergy)
        {
            max = maximum;
            start = startEnergy;
        }
    }
}
