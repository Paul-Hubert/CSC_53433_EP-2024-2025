using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The maximum population of a species (POP-01). Migrate (reference): births go on, and above the cap random older
    /// animals leave the world. Block: no conception at the cap, litters cut to the places left.
    /// </summary>
    /// <remarks>
    /// Place it under the species. The migration phase applies <see cref="Mode.Migrate"/>: above the cap it removes animals
    /// chosen uniformly with the species' migration stream, ignoring genes, stats and position (POP-02), sparing the ones
    /// born this tick unless too few older ones remain; they die with the cause <c>migrated</c>. The breed phase applies
    /// <see cref="Mode.Block"/> at each conception. Eggs don't count toward the cap (REPRO-22).
    /// </remarks>
    public class CapRule : SpeciesModule
    {
        /// <summary>How the cap is kept (POP-01).</summary>
        public enum Mode
        {
            /// <summary>
            /// Births go on; in the migration phase, random older animals leave until the species is back at the cap (reference).
            /// </summary>
            Migrate,
            /// <summary>No conception at or above the cap; a litter is cut to the places left.</summary>
            Block
        }

        [SerializeField, Min(1), Tooltip("Maximum population, in animals (reference prey 270 / small 40, predators 68 / small 6).")]
        int cap = 270;
        [SerializeField, Tooltip("Migrate (reference) or Block.")]
        Mode mode = Mode.Migrate;

        /// <summary>The maximum population, in animals (reference prey 270, predators 68; small scenes 40 and 6).</summary>
        public virtual int Cap => cap;
        /// <summary>How the cap is kept: <see cref="Mode.Migrate"/> (reference) or <see cref="Mode.Block"/>.</summary>
        public Mode Rule => mode;

        /// <summary>Sets the cap from code (WorldBuilder, tests).</summary>
        /// <param name="maximum">The maximum population, in animals.</param>
        /// <param name="rule">How the cap is kept.</param>
        public void Configure(int maximum, Mode rule = Mode.Migrate)
        {
            cap = maximum;
            mode = rule;
        }

        /// <summary>Reports a floor or an initial population above the cap (V-32).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            var floor = Species.Module<FloorRule>();
            if (floor != null && floor.Floor > cap) report.Error("V-32", this, $"Floor {floor.Floor} above cap {cap}.");
            if (Species.InitialPopulation > cap) report.Error("V-32", this, $"Initial population {Species.InitialPopulation} above cap {cap}.");
        }
    }
}
