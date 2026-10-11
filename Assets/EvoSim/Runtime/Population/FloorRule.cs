using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The minimum population (POP-03): below it, newcomers with founder genomes arrive at random walkable places in the
    /// floor phase. A world that needs them is reported (POP-05): they restart that species' evolution.
    /// </summary>
    /// <remarks>
    /// Place it under the species. The floor phase adds newcomers with <see cref="World.AddNewcomer"/> until the species
    /// has <see cref="Floor"/> animals: founder genome, generation 0, recorded as immigrants; they decide from the next
    /// tick (TICK-05).
    /// </remarks>
    public class FloorRule : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Minimum population, in animals (reference prey 10, predators 3).")]
        int floor = 10;

        /// <summary>
        /// The minimum population, in animals (reference prey 10, predators 3); 0 = no newcomers. Set values below 0 become 0.
        /// </summary>
        public int Floor { get => floor; set => floor = Mathf.Max(0, value); }
    }
}
