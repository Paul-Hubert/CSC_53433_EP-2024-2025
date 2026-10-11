namespace EvoSim
{
    /// <summary>The value a stat takes when an animal is created (ANIM-10).</summary>
    /// <remarks>Make one with <see cref="Fixed"/> or <see cref="FullTrait"/> and pass it to
    /// <see cref="SpeciesBuilder.DeclareStat"/>.</remarks>
    public readonly struct StatStart
    {
        /// <summary>The fixed start value; unused when <see cref="Trait"/> is 0 or more.</summary>
        public readonly float Value;
        /// <summary>When ≥ 0, the stat starts at this trait's value (full stamina).</summary>
        public readonly int Trait;

        /// <summary>Makes a start value; use <see cref="Fixed"/> or <see cref="FullTrait"/>.</summary>
        StatStart(float value, int trait) { Value = value; Trait = trait; }

        /// <summary>Starts at a fixed value.</summary>
        /// <param name="value">The start value, clamped to the stat's range.</param>
        /// <returns>The start value.</returns>
        public static StatStart Fixed(float value) => new StatStart(value, -1);

        /// <summary>Starts at the animal's value of a trait (e.g. stamina starts at stamina.max).</summary>
        /// <param name="trait">The trait, as <see cref="SpeciesBuilder.DeclareTrait"/> returned it.</param>
        /// <returns>The start value.</returns>
        public static StatStart FullTrait(TraitId trait) => new StatStart(0f, trait.Index);
    }
}
