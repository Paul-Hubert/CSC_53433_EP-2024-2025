using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The minimum population (POP-03): below it, newcomers with founder genomes arrive at random walkable places in the
    /// floor phase. A world that needs them is reported (POP-05): they restart that species' evolution.
    /// </summary>
    public class FloorRule : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Minimum population, in animals (reference prey 10, predators 3).")]
        int floor = 10;

        public int Floor { get => floor; set => floor = Mathf.Max(0, value); }
    }
}
