namespace EvoSim
{
    /// <summary>The order in which animals act within a tick (ACT-30).</summary>
    public enum ActOrder
    {
        /// <summary>Species in the world's order; within a species, a fresh random order each tick (the reference).</summary>
        SpeciesInTurn,
        /// <summary>Every animal of every species in one fresh random order each tick.</summary>
        AllMixed,
        /// <summary>Every intent from the same state; all move; interactions in a fresh random order (random winner).</summary>
        Simultaneous
    }
}
