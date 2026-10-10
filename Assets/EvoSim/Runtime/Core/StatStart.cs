namespace EvoSim
{
    /// <summary>The value a stat takes when an animal is created (ANIM-10).</summary>
    public readonly struct StatStart
    {
        public readonly float Value;
        /// <summary>When ≥ 0, the stat starts at this trait's value (full stamina).</summary>
        public readonly int Trait;

        StatStart(float value, int trait) { Value = value; Trait = trait; }

        /// <summary>Starts at a fixed value.</summary>
        public static StatStart Fixed(float value) => new StatStart(value, -1);

        /// <summary>Starts at the animal's value of a trait (e.g. stamina starts at stamina.max).</summary>
        public static StatStart FullTrait(TraitId trait) => new StatStart(0f, trait.Index);
    }
}
