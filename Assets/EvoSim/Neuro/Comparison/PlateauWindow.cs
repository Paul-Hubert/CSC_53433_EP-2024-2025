namespace EvoSim.Neuro
{
    /// <summary>
    /// One species' measures over one window, as <see cref="PlateauStop"/> checks them (plan M16 §6.3): mean population,
    /// mean generation, starvation and predation hazards, and births per animal.
    /// </summary>
    public readonly struct PlateauWindow
    {
        /// <summary>The species' id.</summary>
        public readonly string Species;
        /// <summary>The window's mean population.</summary>
        public readonly double MeanPopulation;
        /// <summary>The window's mean generation (weighted by ticks alive).</summary>
        public readonly double MeanGeneration;
        /// <summary>Starvation deaths per 1 000 animal-ticks.</summary>
        public readonly double Starvation;
        /// <summary>Deaths by predators per 1 000 animal-ticks.</summary>
        public readonly double Predation;
        /// <summary>Births per animal (births ÷ mean population).</summary>
        public readonly double BirthsPerAnimal;

        /// <summary>A window's measures.</summary>
        /// <param name="species">The species' id.</param>
        /// <param name="population">Mean population.</param>
        /// <param name="generation">Mean generation.</param>
        /// <param name="starvation">Starvation hazard.</param>
        /// <param name="predation">Predation hazard.</param>
        /// <param name="births">Births per animal.</param>
        public PlateauWindow(string species, double population, double generation, double starvation, double predation, double births)
        {
            Species = species; MeanPopulation = population; MeanGeneration = generation;
            Starvation = starvation; Predation = predation; BirthsPerAnimal = births;
        }
    }
}
