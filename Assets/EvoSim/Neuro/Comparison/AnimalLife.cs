namespace EvoSim.Neuro
{
    /// <summary>
    /// One animal's life as <see cref="SurvivalAnalysis"/> rebuilds it from a run's events (plan M16 §6.2): when it came,
    /// how, and how it left. Migration and the run's end are censoring, not death.
    /// </summary>
    public sealed class AnimalLife
    {
        /// <summary>The animal's id.</summary>
        public int Id;
        /// <summary>Its species' id.</summary>
        public string Species;
        /// <summary>How it came: <c>founder</c>, <c>birth</c> or <c>immigrant</c> (a newcomer below the floor).</summary>
        public string Origin;
        /// <summary>The tick it came.</summary>
        public int Entry;
        /// <summary>The tick it left: its death, its migration, or the run's end.</summary>
        public int Exit;
        /// <summary>Its age when it left, in ticks (the death event's age; otherwise exit − entry).</summary>
        public int Age;
        /// <summary>Its generation.</summary>
        public int Generation;
        /// <summary>The cause of death, <c>migrated</c>, or null when alive at the end.</summary>
        public string Cause;
        /// <summary>The killer's species when it was killed by an animal, else null.</summary>
        public string KillerSpecies;
        /// <summary>The babies that named it as a parent.</summary>
        public int Offspring;

        /// <summary>True for a death (any cause but migration); false when censored (migrated, or alive at the end).</summary>
        public bool Dead => Cause != null && Cause != SurvivalAnalysis.Migrated;
    }
}
