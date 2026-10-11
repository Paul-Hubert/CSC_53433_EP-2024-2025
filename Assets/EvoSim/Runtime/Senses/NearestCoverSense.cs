namespace EvoSim
{
    /// <summary>
    /// The nearest cover (06 §4): "here" when standing in cover, else the bands or none. Without a cover module it
    /// always says none (ENV-12). Reference label "Cover".
    /// </summary>
    /// <remarks>Tokens: here, adjacent, close, medium, far, none. Texts: V1 "Cover: here." / "Cover: 1-4 meters away.", V2
    /// "I am in cover." / "The nearest cover is 1-4 meters away." / "No cover within 20 meters.".</remarks>
    public class NearestCoverSense : BandedSense
    {
        /// <summary>True: standing in cover is "here".</summary>
        /// <inheritdoc/>
        protected override bool HasHere => true;

        /// <summary>"Cover".</summary>
        /// <inheritdoc/>
        protected override string DefaultLabel => "Cover";

        /// <summary>"here" when the animal stands in cover; else the band of the nearest cover within its vision, or "none".</summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            if (s.InCover(a)) return TokenFor(0f, 0f, here: true);
            float vision = a.Trait(Vision);
            var cover = s.NearestCover(a, vision);
            return cover == null ? NoneToken : TokenFor(cover.Value.Distance, vision);
        }

        /// <summary>"I am in cover."</summary>
        /// <inheritdoc/>
        protected override string HereSentence() => "I am in cover.";
    }
}
