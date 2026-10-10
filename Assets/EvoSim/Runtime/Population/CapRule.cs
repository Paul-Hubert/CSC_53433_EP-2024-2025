using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The maximum population of a species (POP-01). Migrate (reference): births go on, and above the cap random older
    /// animals leave the world. Block: no conception at the cap, litters cut to the places left.
    /// </summary>
    public class CapRule : SpeciesModule
    {
        public enum Mode { Migrate, Block }

        [SerializeField, Min(1), Tooltip("Maximum population, in animals (reference prey 270 / small 40, predators 68 / small 6).")]
        int cap = 270;
        [SerializeField, Tooltip("Migrate (reference) or Block.")]
        Mode mode = Mode.Migrate;

        public virtual int Cap => cap;
        public Mode Rule => mode;

        public void Configure(int maximum, Mode rule = Mode.Migrate)
        {
            cap = maximum;
            mode = rule;
        }

        public override void Validate(ValidationReport report)
        {
            var floor = Species.Module<FloorRule>();
            if (floor != null && floor.Floor > cap) report.Error("V-32", this, $"Floor {floor.Floor} above cap {cap}.");
            if (Species.InitialPopulation > cap) report.Error("V-32", this, $"Initial population {Species.InitialPopulation} above cap {cap}.");
        }
    }
}
