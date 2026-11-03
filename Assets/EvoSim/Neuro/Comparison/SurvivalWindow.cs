using System.Collections.Generic;

namespace EvoSim.Neuro
{
    /// <summary>
    /// One species' measures over one time window [<see cref="From"/>, <see cref="To"/>) of a run (plan M16 §6.2): time at
    /// risk, deaths by cause, arrivals, population and kills. <see cref="SurvivalAnalysis.Window"/> makes them.
    /// </summary>
    public sealed class SurvivalWindow
    {
        /// <summary>The species' id.</summary>
        public string Species;
        /// <summary>The window's first tick.</summary>
        public int From;
        /// <summary>The tick after the window's last.</summary>
        public int To;
        /// <summary>Animal-ticks alive in the window.</summary>
        public long AnimalTicks;
        /// <summary>Deaths (and migrations) in the window by cause.</summary>
        public readonly SortedDictionary<string, int> Deaths = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
        /// <summary>Births in the window.</summary>
        public int Births;
        /// <summary>Newcomers (immigrants below the floor) in the window.</summary>
        public int Immigrants;
        /// <summary>Kills made by this species in the window, by victim species.</summary>
        public readonly SortedDictionary<string, int> Kills = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
        /// <summary>The mean of the alive animals' generations, weighted by their ticks in the window.</summary>
        public double MeanGeneration;

        /// <summary>Deaths of one cause in the window; null = every cause but migration.</summary>
        /// <param name="cause">A cause, or null.</param>
        /// <returns>The count.</returns>
        public int DeathsBy(string cause)
        {
            if (cause != null) return Deaths.TryGetValue(cause, out var n) ? n : 0;
            int all = 0;
            foreach (var kv in Deaths) if (kv.Key != SurvivalAnalysis.Migrated) all += kv.Value;
            return all;
        }

        /// <summary>Deaths of one cause (null = all but migration) per 1 000 animal-ticks; 0 without time at risk.</summary>
        /// <param name="cause">A cause, or null.</param>
        /// <returns>The hazard.</returns>
        public double Hazard(string cause) => AnimalTicks > 0 ? 1000.0 * DeathsBy(cause) / AnimalTicks : 0;

        /// <summary>The mean population over the window.</summary>
        public double MeanPopulation => To > From ? (double)AnimalTicks / (To - From) : 0;

        /// <summary>Births per animal over the window (births ÷ mean population); 0 with nobody alive.</summary>
        public double BirthsPerAnimal => MeanPopulation > 0 ? Births / MeanPopulation : 0;
    }
}
