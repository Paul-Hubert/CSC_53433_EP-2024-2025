namespace EvoSim
{
    /// <summary>What a diet entry points at (SPEC-15).</summary>
    public enum EdibleTargetKind
    {
        /// <summary>A resource layer's items, by the layer's name: grazed.</summary>
        Layer,
        /// <summary>A species' living animals, by its id or name: struck.</summary>
        Species,
        /// <summary>Carcasses of a species: "carcass:&lt;species&gt;", scavenged one portion at a time.</summary>
        Carcass,
        /// <summary>
        /// Eggs of a species: "egg:&lt;species&gt;", eaten whole (REPRO-23). An entry for any other entity kind,
        /// "&lt;kind&gt;:&lt;species&gt;", resolves to this kind too.
        /// </summary>
        Egg
    }
}
