using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A sense that reports how far the nearest something is, in bands (06 §3): "here" (optional), the bands, "none".
    /// Wording (SENSE-10…13): V1 "Food: 1-4 meters away." / V2 "The nearest food is 1-4 meters away.".
    /// </summary>
    /// <remarks>
    /// <para>Derive from it to sense the distance to anything: implement <see cref="Sense.Read"/> with a
    /// <see cref="SenseContext"/> query and return <see cref="TokenFor"/> or <see cref="NoneToken"/>; override
    /// <see cref="HasHere"/> for a "here" token and <see cref="HereSentence"/> for its V2 text. The base class declares the
    /// "vision" trait, builds the token list in <see cref="Initialize"/>, writes both text styles and validates the bands.</para>
    /// <para>The texts state the vision trait's default (<see cref="StatedVision"/>), not each animal's own vision, so one
    /// token always gives one text (SENSE-10).</para>
    /// </remarks>
    /// <example>
    /// The sample <c>NearestWaterSense</c> (22 §2):
    /// <code>
    /// public class NearestWaterSense : BandedSense
    /// {
    ///     protected override bool HasHere => true;
    ///     protected override string DefaultLabel => "Water";
    ///     protected override string HereSentence() => "I am at the water's edge.";
    ///
    ///     public override int Read(Animal a, SenseContext s)
    ///     {
    ///         if (s.Ground == null) return NoneToken;
    ///         float vision = s.Vision(a);
    ///         var water = s.Ground.NearestWater(a.Position, vision);
    ///         return water == null ? NoneToken : TokenFor(water.Value.Distance, vision, here: water.Value.Distance &lt;= 0.5f);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class BandedSense : Sense
    {
        /// <summary>The band edges and the default vision, in meters, set in the Inspector (SENSE-20).</summary>
        [SerializeField, Tooltip("Band edges and the vision default, in meters (SENSE-20).")]
        protected Bands bands = Bands.Reference;

        /// <summary>
        /// The noun typed in the Inspector for V2 texts, or empty for the label in lower case. Read it through
        /// <see cref="Noun"/>.
        /// </summary>
        [SerializeField, Tooltip("The noun of V2 texts: \"food\", \"predator\", \"other animal\"… Empty = the label in lower case.")]
        protected string noun = "";

        /// <summary>The token list <see cref="Tokens"/> returns, built by <see cref="BuildTokens"/>.</summary>
        protected readonly List<string> tokens = new List<string>();

        /// <summary>Per token: the band (0…k), -1 for none, -2 for here.</summary>
        protected readonly List<int> bandOf = new List<int>();

        /// <summary>The band edges and the default vision this sense uses.</summary>
        public Bands Bands => bands;

        /// <summary>
        /// The species' "vision" trait, declared by <see cref="Declare"/>: read an animal's vision with
        /// <c>a.Trait(Vision)</c>, in meters.
        /// </summary>
        public TraitId Vision { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>
        /// The noun of V2 texts ("The nearest food is…", "No predator within…"): the Inspector's noun, or the label in lower
        /// case.
        /// </summary>
        public virtual string Noun => string.IsNullOrEmpty(noun) ? Label.ToLowerInvariant() : noun;

        /// <summary>True when this sense has a "here" token (resources, cover); false by default.</summary>
        protected virtual bool HasHere => false;

        /// <summary>
        /// Declares the "vision" trait with the bands' vision as its default (SENSE-22). Several senses share it: the first
        /// declaration counts.
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) => Vision = b.DeclareTrait("vision", bands.vision, 0.01f, 10000f);

        /// <summary>Builds the token list (<see cref="BuildTokens"/>). An override must call it.</summary>
        /// <inheritdoc/>
        public override void Initialize() => BuildTokens();

        /// <summary>
        /// Fills <see cref="tokens"/> and <see cref="bandOf"/>: "here" (optional), one token per band, then "none".
        /// Subclasses may add more tokens per band, as <see cref="NearestAnimalSense"/> does for readiness.
        /// </summary>
        protected virtual void BuildTokens()
        {
            tokens.Clear();
            bandOf.Clear();
            if (HasHere) Add(Bands.Here, -2);
            for (int b = 0; b < bands.Count; b++) Add(bands.NameOf(b), b);
            Add(Bands.None, -1);
        }

        /// <summary>Adds a token and its band to the list, in <see cref="BuildTokens"/>.</summary>
        /// <param name="token">The token's text, such as "close".</param>
        /// <param name="band">Its band: 0…k, -1 for none, -2 for here.</param>
        protected void Add(string token, int band)
        {
            tokens.Add(token);
            bandOf.Add(band);
        }

        /// <summary>Gives the token for a distance (SENSE-20), or "here" when asked for and the sense has it.</summary>
        /// <param name="distance">The distance to the nearest thing, in meters.</param>
        /// <param name="vision">The animal's vision, in meters: beyond it the token is "none".</param>
        /// <param name="here">True when the animal is on the thing (within reach, in cover).</param>
        /// <returns>An index into <see cref="Tokens"/>.</returns>
        protected int TokenFor(float distance, float vision, bool here = false)
        {
            if (here && HasHere) return bandOf.IndexOf(-2);
            int band = bands.IndexOf(distance, vision);
            return bandOf.IndexOf(band);
        }

        /// <summary>The index of the "none" token: return it when nothing is in sight.</summary>
        protected int NoneToken => bandOf.IndexOf(-1);

        /// <summary>The vision the texts state, in meters: the trait's default (one text per token, SENSE-10).</summary>
        protected float StatedVision => Vision.IsValid ? Vision.Default : bands.vision;

        /// <summary>
        /// Writes "Label: here." / "Label: 1-4 meters away." / "Label: none within 20 meters." in V1, and the sentence of
        /// <see cref="HereSentence"/> / "The nearest noun is 1-4 meters away." / "No noun within 20 meters." in V2.
        /// </summary>
        /// <inheritdoc/>
        public override string Write(int token, TextStyle style)
        {
            int band = bandOf[token];
            if (style == TextStyle.V1)
            {
                if (band == -2) return $"{Label}: here.";
                if (band == -1) return $"{Label}: {bands.DescribeNone(StatedVision)}.";
                return $"{Label}: {bands.Describe(band, StatedVision)}.";
            }
            if (band == -2) return HereSentence();
            if (band == -1) return $"No {Noun} within {Bands.Meters(StatedVision)}.";
            return $"The nearest {Noun} is {bands.Describe(band, StatedVision)}.";
        }

        /// <summary>Writes the V2 text of "here".</summary>
        /// <returns>"I am standing on food." by default (with <see cref="Noun"/>); "I am in cover." for the cover sense.</returns>
        protected virtual string HereSentence() => $"I am standing on {Noun}.";

        /// <summary>Reports V-30 when the band edges are not positive, increasing and below the vision (SENSE-21).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            var problem = bands.Check();
            if (problem != null)
                report.Error("V-30", this, $"Sense '{Label}': {problem} (SENSE-21).",
                             new ValidationFix("Sort", () => System.Array.Sort(bands.edges)));
        }

        /// <summary>Sets the bands from code (tests), with the reference band names.</summary>
        /// <param name="edges">Increasing band edges, in meters.</param>
        /// <param name="vision">The default vision, in meters, above the last edge.</param>
        public void SetBands(float[] edges, float vision)
        {
            bands = new Bands { edges = edges, vision = vision };
        }

        /// <summary>Sets the V2 noun from code (WorldBuilder, tests), as the Inspector field does.</summary>
        /// <param name="text">The noun, such as "other predator"; empty for the label in lower case.</param>
        public void SetNoun(string text) => noun = text;
    }
}
