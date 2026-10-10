using UnityEngine;

namespace EvoSim
{
    /// <summary>The energy stat (05 §4): founders and newcomers start at 60, the maximum is 100. Gains are clamped (ANIM-22).</summary>
    public class Energy : SpeciesModule
    {
        [SerializeField, Min(1f), Tooltip("Maximum energy (reference 100).")]
        float max = 100f;
        [SerializeField, Min(0f), Tooltip("Energy of founders and newcomers (reference 60). Babies get the litter's child energy.")]
        float start = 60f;

        public StatId Value { get; private set; }
        public float Max => max;
        public float Start => start;

        public override void Declare(SpeciesBuilder b) =>
            Value = b.DeclareStat("energy", StatStart.Fixed(start), float.MinValue, max);   // may go below 0 (REPRO-10)

        /// <summary>Adds energy from a meal, clamped to the maximum (ANIM-22).</summary>
        public void Gain(Animal a, float amount) => a[Value] += amount;

        /// <summary>Removes energy (costs, paying for a litter); it may fall below zero.</summary>
        public void Pay(Animal a, float amount) => a[Value] -= amount;

        public void Configure(float maximum, float startEnergy)
        {
            max = maximum;
            start = startEnergy;
        }
    }
}
