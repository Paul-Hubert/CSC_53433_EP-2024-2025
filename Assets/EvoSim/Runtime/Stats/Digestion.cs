using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// After a kill or a carcass portion the eater is busy digesting for round(digestTicks × gain / killGain) ticks:
    /// 50 after a kill (+60), 25 after a portion (+30). Grazing never makes an animal busy (05 §5).
    /// </summary>
    /// <remarks>
    /// A busy animal doesn't decide and doesn't move; it pays the busy cost and recovers stamina, and its busy count
    /// drops by one per tick in the act phase (ANIM-30). The interaction resolver calls <see cref="AfterMeal"/> after a
    /// kill or a portion, and so does <see cref="InteractionContext.Feed"/> when asked to digest.
    /// </remarks>
    public class Digestion : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Busy ticks after a meal worth the reference gain (reference 50).")]
        int digestTicks = 50;
        [SerializeField, Min(0.01f), Tooltip("The gain that takes digestTicks to digest (reference: a kill, 60).")]
        float referenceGain = 60f;

        /// <summary>The busy reason this module sets: <c>"digesting"</c> (ANIM-31).</summary>
        public const string Reason = "digesting";

        /// <summary>Makes the animal busy for a meal of this energy (ANIM-30, ANIM-31).</summary>
        /// <param name="a">The animal that just ate.</param>
        /// <param name="gain">The meal's energy in points, as given to the eater (before the energy stat is capped).</param>
        /// <remarks>
        /// It sets the busy count to <c>round(digestTicks × gain / referenceGain)</c> ticks, replacing any count the animal
        /// had, and the reason to <see cref="Reason"/>. A count of 0 leaves the animal free.
        /// </remarks>
        public virtual void AfterMeal(Animal a, float gain)
        {
            int ticks = Mathf.RoundToInt(digestTicks * gain / referenceGain);
            if (ticks <= 0) return;
            a.BusyTicks = ticks;
            a.BusyReason = Reason;
        }

        /// <summary>The busy ticks a meal of this energy gives, by the same formula, without changing any animal.</summary>
        /// <param name="gain">The meal's energy, in points.</param>
        /// <returns><c>round(digestTicks × gain / referenceGain)</c>, in ticks.</returns>
        public int TicksFor(float gain) => Mathf.RoundToInt(digestTicks * gain / referenceGain);

        /// <summary>Sets the settings from code (WorldBuilder, tests).</summary>
        /// <param name="ticks">Busy ticks after a meal worth <paramref name="gain"/>.</param>
        /// <param name="gain">The reference gain, in energy points.</param>
        public void Configure(int ticks, float gain)
        {
            digestTicks = ticks;
            referenceGain = gain;
        }
    }
}
