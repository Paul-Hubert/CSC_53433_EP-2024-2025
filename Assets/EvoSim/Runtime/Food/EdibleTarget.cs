namespace EvoSim
{
    /// <summary>One resolved diet entry: what is eaten, how (its Edible), and the energy scale (SPEC-15).</summary>
    /// <remarks>
    /// <see cref="Diet"/> builds them from its entries (<see cref="Diet.Resolved"/>) and hands one to
    /// <see cref="Diet.EnergyFrom"/> with every meal. Treat them as read-only.
    /// </remarks>
    public sealed class EdibleTarget
    {
        /// <summary>What the entry points at: a layer's items, a species' animals, its carcasses or its eggs.</summary>
        public EdibleTargetKind Kind;
        /// <summary>The entry as written in the diet, e.g. "Grass" or "carcass:prey".</summary>
        public string Name;
        /// <summary>The resource layer grazed (<see cref="EdibleTargetKind.Layer"/>), or null.</summary>
        public ResourceLayer Layer;
        /// <summary>The species eaten (Species), or whose carcasses or eggs are eaten.</summary>
        public Species Species;
        /// <summary>
        /// The entity system holding the carcasses or eggs (<see cref="EdibleTargetKind.Carcass"/>,
        /// <see cref="EdibleTargetKind.Egg"/>), or null.
        /// </summary>
        public EntityKind Entities;
        /// <summary>The Edible that says how it is eaten and how much it gives, or null: not edible (SPEC-10, V-24).</summary>
        public Edible Edible;
        /// <summary>Multiplies the energy gained: the diet entry's scale (default 1).</summary>
        public float EnergyScale = 1f;

        /// <summary>Whether a species (or one cloned from it, SPEC-31) is the one this entry names.</summary>
        /// <param name="s">A species, or null.</param>
        /// <returns>True when <paramref name="s"/> is the named species or descends from it; false for null.</returns>
        public bool Names(Species s) => Species != null && s != null && s.DescendsFrom(Species);

        /// <summary>The energy one item, kill or portion gives: the edible's energy × the diet's scale (ACT-11).</summary>
        /// <remarks>
        /// For carcasses it is the Edible's energy per portion. A real portion's energy comes from the carcass itself, which
        /// copied it at the kill. 0 when not edible.
        /// </remarks>
        public float Energy => Edible == null ? 0f
            : (Kind == EdibleTargetKind.Carcass ? Edible.carcassEnergy : Edible.energy) * EnergyScale;

        /// <summary>The entry's name, as written in the diet.</summary>
        /// <returns><see cref="Name"/>.</returns>
        public override string ToString() => Name;
    }
}
