namespace EvoSim.Testing
{
    /// <summary>A test module declaring one trait (default 60, range [1, 120]) and one capped stat (T-ANIM-02, T-ANIM-03).</summary>
    public class TraitProbe : SpeciesModule
    {
        /// <summary>The trait's default, minimum and maximum; change them before <c>Build</c>.</summary>
        public float Default = 60f, Min = 1f, Max = 120f;

        /// <summary>The trait <c>probe.trait</c>, declared with <see cref="Default"/>, <see cref="Min"/> and <see cref="Max"/>.</summary>
        public TraitId Trait { get; private set; }

        /// <summary>The stat <c>probe.level</c>: starts at 5, kept in [0, 10].</summary>
        public StatId Level { get; private set; }

        /// <summary>Declares the trait and the stat.</summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            Trait = b.DeclareTrait("probe.trait", Default, Min, Max);
            Level = b.DeclareStat("probe.level", StatStart.Fixed(5f), 0f, 10f);
        }
    }
}
