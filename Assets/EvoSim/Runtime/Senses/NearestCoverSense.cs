namespace EvoSim
{
    /// <summary>
    /// The nearest cover (06 §4): "here" when standing in cover, else the bands or none. Without a cover module it
    /// always says none (ENV-12). Reference label "Cover".
    /// </summary>
    public class NearestCoverSense : BandedSense
    {
        protected override bool HasHere => true;

        protected override string DefaultLabel => "Cover";

        public override int Read(Animal a, SenseContext s)
        {
            if (s.InCover(a)) return TokenFor(0f, 0f, here: true);
            float vision = a.Trait(Vision);
            var cover = s.NearestCover(a, vision);
            return cover == null ? NoneToken : TokenFor(cover.Value.Distance, vision);
        }

        protected override string HereSentence() => "I am in cover.";
    }
}
