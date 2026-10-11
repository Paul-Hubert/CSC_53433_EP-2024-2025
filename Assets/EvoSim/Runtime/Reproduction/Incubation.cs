using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Ticks between conception and hatching (REPRO-20, REPRO-21): 0 in the reference (babies hatch the same tick);
    /// a few ticks hide the mutator's latency (MUT-33).
    /// </summary>
    /// <remarks>
    /// Place it under the species. The breed phase reads <see cref="Ticks"/> at each conception; without the module the
    /// incubation is 0. Eggs exist as entities only while they incubate, so they can be eaten only with an incubation
    /// above 0 (22 §15).
    /// </remarks>
    public class Incubation : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Ticks from conception to hatching (reference 0).")]
        int ticks;

        /// <summary>
        /// Ticks from conception to hatching (reference 0); set values below 0 become 0. An override must not draw random
        /// numbers.
        /// </summary>
        public virtual int Ticks { get => ticks; set => ticks = Mathf.Max(0, value); }
    }
}
