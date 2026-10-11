namespace EvoSim
{
    /// <summary>A handle to a declared stat (ANIM-10): its slot in the animal record, its name and its range.</summary>
    /// <remarks>Get one from <see cref="SpeciesBuilder.DeclareStat"/> (or <see cref="SpeciesBuilder.FindStat"/>) and read
    /// or write the stat with <c>a[id]</c>. The default value is an invalid id.</remarks>
    public readonly struct StatId
    {
        /// <summary>The stat's slot in every animal of the species.</summary>
        public readonly int Index;
        /// <summary>The stat's name, e.g. <c>"energy"</c>; null for an invalid id.</summary>
        public readonly string Name;
        /// <summary>The lowest and highest values of the stat.</summary>
        public readonly float Min, Max;
        /// <summary>The trait that caps this stat per animal (stamina ≤ stamina.max), or -1.</summary>
        public readonly int MaxTrait;

        /// <summary>Makes a handle; <see cref="SpeciesBuilder.DeclareStat"/> makes them.</summary>
        /// <param name="index">The slot in the animal record.</param>
        /// <param name="name">The stat's name.</param>
        /// <param name="min">The lowest value.</param>
        /// <param name="max">The highest value.</param>
        /// <param name="maxTrait">The index of the trait that caps it, or -1.</param>
        public StatId(int index, string name, float min, float max, int maxTrait)
        {
            Index = index; Name = name; Min = min; Max = max; MaxTrait = maxTrait;
        }

        /// <summary>True for a declared stat; false for the default id (a stat no module declared).</summary>
        public bool IsValid => Name != null;

        /// <summary>Clamps a value to the stat's range; gains never pass the maximum (ANIM-22).</summary>
        /// <param name="value">The wanted value.</param>
        /// <param name="traits">The animal's trait values, for the cap trait.</param>
        /// <returns>The value kept between <see cref="Min"/> and the smaller of <see cref="Max"/> and the cap trait's value.</returns>
        public float Clamp(float value, float[] traits)
        {
            float max = MaxTrait >= 0 ? System.Math.Min(Max, traits[MaxTrait]) : Max;
            if (value > max) return max;
            if (value < Min) return Min;
            return value;
        }

        /// <summary>The stat's name.</summary>
        /// <returns>The name, or null for an invalid id.</returns>
        public override string ToString() => Name;
    }
}
