using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Makes what it sits on edible (SPEC-10): a species' animals, a resource layer's items, an entity kind.
    /// Without it nothing can eat that thing, whatever the diets say (ENV-23).
    /// </summary>
    public class Edible : MonoBehaviour
    {
        [Tooltip("Graze (items), Strike (animals) or Scavenge (carcasses).")]
        public EatMethod method = EatMethod.Graze;
        [Min(0f), Tooltip("Energy per item, per kill or per portion (reference: food 25, prey kill 60).")]
        public float energy = 25f;

        [Header("Strike only: what a kill leaves")]
        [Min(0), Tooltip("Carcass portions, one each for other eaters (reference 2; 0 = no carcass).")]
        public int carcassPortions = 0;
        [Min(0f), Tooltip("Energy per carcass portion (reference 30).")]
        public float carcassEnergy = 30f;
        [Min(1), Tooltip("Ticks before the carcass rots away (reference 100).")]
        public int carcassTicks = 100;

        /// <summary>Sets the values from code (WorldBuilder, tests).</summary>
        public Edible Configure(EatMethod m, float e, int portions = 0, float portionEnergy = 30f, int rotTicks = 100)
        {
            method = m; energy = e; carcassPortions = portions; carcassEnergy = portionEnergy; carcassTicks = rotTicks;
            return this;
        }
    }
}
