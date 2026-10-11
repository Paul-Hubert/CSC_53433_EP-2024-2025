namespace EvoSim
{
    /// <summary>A handle to a declared trait (ANIM-15): a per-animal number fixed at birth, with a default and a range.</summary>
    /// <remarks>Get one from <see cref="SpeciesBuilder.DeclareTrait"/> (or <see cref="SpeciesBuilder.FindTrait"/>) and read
    /// the trait with <c>a.Trait(id)</c>. A number gene sets it when the animal is created. The default value is an
    /// invalid id.</remarks>
    public readonly struct TraitId
    {
        /// <summary>The trait's slot in every animal of the species.</summary>
        public readonly int Index;
        /// <summary>The trait's name, e.g. <c>"vision"</c>; null for an invalid id.</summary>
        public readonly string Name;
        /// <summary>Default: the value when no gene sets it. Min and Max: the trait's range (ANIM-18).</summary>
        public readonly float Default, Min, Max;
        /// <summary>False for every reference trait: traits don't change during life (ANIM-17).</summary>
        public readonly bool Changeable;

        /// <summary>Makes a handle; <see cref="SpeciesBuilder.DeclareTrait"/> makes them.</summary>
        /// <param name="index">The slot in the animal record.</param>
        /// <param name="name">The trait's name.</param>
        /// <param name="defaultValue">The value when no gene sets it.</param>
        /// <param name="min">The lowest value.</param>
        /// <param name="max">The highest value.</param>
        /// <param name="changeable">True when it may change during life.</param>
        public TraitId(int index, string name, float defaultValue, float min, float max, bool changeable)
        {
            Index = index; Name = name; Default = defaultValue; Min = min; Max = max; Changeable = changeable;
        }

        /// <summary>True for a declared trait; false for the default id (a trait no module declared).</summary>
        public bool IsValid => Name != null;

        /// <summary>A value clamped to the trait's range (ANIM-18).</summary>
        /// <param name="value">The wanted value.</param>
        /// <returns>The value kept between <see cref="Min"/> and <see cref="Max"/>.</returns>
        public float Clamp(float value) => value < Min ? Min : value > Max ? Max : value;

        /// <summary>The trait's name.</summary>
        /// <returns>The name, or null for an invalid id.</returns>
        public override string ToString() => Name;
    }
}
