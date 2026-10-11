namespace EvoSim
{
    /// <summary>A set of species resolved from the food web (SPEC-12): whom an animal flees, hunts or follows.</summary>
    /// <remarks>
    /// The World derives threats and prey from the diets once the species are initialized, and again when a species is
    /// added during a run (<see cref="World.RebuildFoodWeb"/>); <see cref="World.Resolve"/> turns a set into its species.
    /// </remarks>
    public enum AnimalSet
    {
        /// <summary>Species whose diet strikes this one (derived); a cannibal species is among its own threats (SPEC-11).</summary>
        Threats,
        /// <summary>Species this one's diet strikes (derived).</summary>
        Prey,
        /// <summary>This species: its own animals, never the asking animal itself.</summary>
        Kin,
        /// <summary>The species listed on the module that uses the set, by id or display name (flee only from wolves).</summary>
        Listed
    }
}
