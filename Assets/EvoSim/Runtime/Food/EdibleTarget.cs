namespace EvoSim
{
    /// <summary>One resolved diet entry: what is eaten, how (its Edible), and the energy scale (SPEC-15).</summary>
    public sealed class EdibleTarget
    {
        public EdibleTargetKind Kind;
        /// <summary>The entry as written in the diet, e.g. "Grass" or "carcass:prey".</summary>
        public string Name;
        public ResourceLayer Layer;
        /// <summary>The species eaten (Species), or whose carcasses or eggs are eaten.</summary>
        public Species Species;
        public EntityKind Entities;
        /// <summary>The Edible that says how it is eaten and how much it gives, or null: not edible (SPEC-10, V-24).</summary>
        public Edible Edible;
        public float EnergyScale = 1f;

        /// <summary>Whether a species (or one cloned from it, SPEC-31) is the one this entry names.</summary>
        public bool Names(Species s) => Species != null && s != null && s.DescendsFrom(Species);

        /// <summary>The energy one item, kill or portion gives: the edible's energy × the diet's scale (ACT-11).</summary>
        public float Energy => Edible == null ? 0f
            : (Kind == EdibleTargetKind.Carcass ? Edible.carcassEnergy : Edible.energy) * EnergyScale;

        public override string ToString() => Name;
    }
}
