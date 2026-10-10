namespace EvoSim.Testing
{
    /// <summary>A test module declaring one trait (default 60, range [1, 120]) and one capped stat (T-ANIM-02, T-ANIM-03).</summary>
    public class TraitProbe : SpeciesModule
    {
        public float Default = 60f, Min = 1f, Max = 120f;
        public TraitId Trait { get; private set; }
        public StatId Level { get; private set; }

        public override void Declare(SpeciesBuilder b)
        {
            Trait = b.DeclareTrait("probe.trait", Default, Min, Max);
            Level = b.DeclareStat("probe.level", StatStart.Fixed(5f), 0f, 10f);
        }
    }
}
