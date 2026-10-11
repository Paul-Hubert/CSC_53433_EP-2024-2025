using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Makes what it sits on edible (SPEC-10): a species' animals, a resource layer's items, an entity kind.
    /// Without it nothing can eat that thing, whatever the diets say (ENV-23).
    /// </summary>
    /// <remarks>
    /// <para>Put it on the same GameObject as the <see cref="Species"/>, the <see cref="ResourceLayer"/> or the
    /// <see cref="EntityKind"/> it makes edible. The eater's <see cref="Diet"/> says what it eats; this component says how
    /// it is eaten and how much it gives (20 §3.12). A disabled Edible counts as none.</para>
    /// <para>The reference: the Grass layer is grazed for 25; the prey is struck for 60 and leaves a carcass of 2 portions
    /// of 30 that rots in 100 ticks; eggs have none.</para>
    /// </remarks>
    public class Edible : MonoBehaviour
    {
        /// <summary>How it is eaten: grazed (items, eggs), struck (animals) or scavenged (carcasses).</summary>
        [Tooltip("Graze (items), Strike (animals) or Scavenge (carcasses).")]
        public EatMethod method = EatMethod.Graze;
        /// <summary>Energy points gained per item, per kill or per portion, before the eater's diet scale.</summary>
        [Min(0f), Tooltip("Energy per item, per kill or per portion (reference: food 25, prey kill 60).")]
        public float energy = 25f;

        /// <summary>Strike only: portions of the carcass a kill leaves, one each for eaters other than the killer (0 = no carcass).</summary>
        [Header("Strike only: what a kill leaves")]
        [Min(0), Tooltip("Carcass portions, one each for other eaters (reference 2; 0 = no carcass).")]
        public int carcassPortions = 0;
        /// <summary>Strike only: energy points per carcass portion, before the eater's diet scale.</summary>
        [Min(0f), Tooltip("Energy per carcass portion (reference 30).")]
        public float carcassEnergy = 30f;
        /// <summary>Strike only: ticks before the carcass rots away, eaten or not.</summary>
        [Min(1), Tooltip("Ticks before the carcass rots away (reference 100).")]
        public int carcassTicks = 100;

        /// <summary>Sets the values from code (WorldBuilder, tests).</summary>
        /// <param name="m">How it is eaten.</param>
        /// <param name="e">Energy points per item, kill or portion.</param>
        /// <param name="portions">Carcass portions a kill leaves (strike only; 0 = no carcass).</param>
        /// <param name="portionEnergy">Energy points per carcass portion.</param>
        /// <param name="rotTicks">Ticks before a carcass rots away.</param>
        /// <returns>This component, to chain calls.</returns>
        public Edible Configure(EatMethod m, float e, int portions = 0, float portionEnergy = 30f, int rotTicks = 100)
        {
            method = m; energy = e; carcassPortions = portions; carcassEnergy = portionEnergy; carcassTicks = rotTicks;
            return this;
        }
    }
}
