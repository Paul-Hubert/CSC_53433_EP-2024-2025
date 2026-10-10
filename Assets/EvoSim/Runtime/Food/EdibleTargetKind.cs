namespace EvoSim
{
    /// <summary>What a diet entry points at (SPEC-15).</summary>
    public enum EdibleTargetKind
    {
        /// <summary>A resource layer's items, by the layer's name.</summary>
        Layer,
        /// <summary>A species' living animals, by its id or name.</summary>
        Species,
        /// <summary>Carcasses of a species: "carcass:&lt;species&gt;".</summary>
        Carcass,
        /// <summary>Eggs of a species: "egg:&lt;species&gt;".</summary>
        Egg
    }
}
