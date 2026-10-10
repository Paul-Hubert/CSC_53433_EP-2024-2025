namespace EvoSim
{
    /// <summary>A set of species resolved from the food web (SPEC-12): whom an animal flees, hunts or follows.</summary>
    public enum AnimalSet
    {
        /// <summary>Species whose diet strikes this one (derived).</summary>
        Threats,
        /// <summary>Species this one strikes (derived).</summary>
        Prey,
        /// <summary>This species.</summary>
        Kin,
        /// <summary>The species listed on the module.</summary>
        Listed
    }
}
