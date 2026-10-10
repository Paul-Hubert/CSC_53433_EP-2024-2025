using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Ticks between conception and hatching (REPRO-20, REPRO-21): 0 in the reference (babies hatch the same tick);
    /// a few ticks hide the mutator's latency (MUT-33).
    /// </summary>
    public class Incubation : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Ticks from conception to hatching (reference 0).")]
        int ticks;

        public int Ticks { get => ticks; set => ticks = Mathf.Max(0, value); }
    }
}
