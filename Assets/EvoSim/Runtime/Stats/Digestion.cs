using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// After a kill or a carcass portion the eater is busy digesting for round(digestTicks × gain / killGain) ticks:
    /// 50 after a kill (+60), 25 after a portion (+30). Grazing never makes an animal busy (05 §5).
    /// </summary>
    public class Digestion : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Busy ticks after a meal worth the reference gain (reference 50).")]
        int digestTicks = 50;
        [SerializeField, Min(0.01f), Tooltip("The gain that takes digestTicks to digest (reference: a kill, 60).")]
        float referenceGain = 60f;

        public const string Reason = "digesting";

        /// <summary>Makes the animal busy for a meal of this energy (ANIM-30, ANIM-31).</summary>
        public virtual void AfterMeal(Animal a, float gain)
        {
            int ticks = Mathf.RoundToInt(digestTicks * gain / referenceGain);
            if (ticks <= 0) return;
            a.BusyTicks = ticks;
            a.BusyReason = Reason;
        }

        public int TicksFor(float gain) => Mathf.RoundToInt(digestTicks * gain / referenceGain);

        public void Configure(int ticks, float gain)
        {
            digestTicks = ticks;
            referenceGain = gain;
        }
    }
}
