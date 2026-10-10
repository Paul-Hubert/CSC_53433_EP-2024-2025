using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A sense that reports how far the nearest something is, in bands (06 §3): "here" (optional), the bands, "none".
    /// Wording (SENSE-10…13): V1 "Food: 1-4 meters away." / V2 "The nearest food is 1-4 meters away.".
    /// </summary>
    public abstract class BandedSense : Sense
    {
        [SerializeField, Tooltip("Band edges and the vision default, in meters (SENSE-20).")]
        protected Bands bands = Bands.Reference;
        [SerializeField, Tooltip("The noun of V2 texts: \"food\", \"predator\", \"other animal\"… Empty = the label in lower case.")]
        protected string noun = "";

        protected readonly List<string> tokens = new List<string>();
        /// <summary>Per token: the band (0…k), -1 for none, -2 for here.</summary>
        protected readonly List<int> bandOf = new List<int>();

        public Bands Bands => bands;
        public TraitId Vision { get; private set; }
        public override IReadOnlyList<string> Tokens => tokens;
        public virtual string Noun => string.IsNullOrEmpty(noun) ? Label.ToLowerInvariant() : noun;

        /// <summary>Whether this sense has a "here" token (resources, cover).</summary>
        protected virtual bool HasHere => false;

        public override void Declare(SpeciesBuilder b) => Vision = b.DeclareTrait("vision", bands.vision, 0.01f, 10000f);

        public override void Initialize() => BuildTokens();

        /// <summary>here (optional), one token per band, none; subclasses may add more per band.</summary>
        protected virtual void BuildTokens()
        {
            tokens.Clear();
            bandOf.Clear();
            if (HasHere) Add(Bands.Here, -2);
            for (int b = 0; b < bands.Count; b++) Add(bands.NameOf(b), b);
            Add(Bands.None, -1);
        }

        protected void Add(string token, int band)
        {
            tokens.Add(token);
            bandOf.Add(band);
        }

        /// <summary>The token for a distance (SENSE-20), with "here" when asked for.</summary>
        protected int TokenFor(float distance, float vision, bool here = false)
        {
            if (here && HasHere) return bandOf.IndexOf(-2);
            int band = bands.IndexOf(distance, vision);
            return bandOf.IndexOf(band);
        }

        protected int NoneToken => bandOf.IndexOf(-1);

        /// <summary>The vision the texts state: the trait's default (one text per token, SENSE-10).</summary>
        protected float StatedVision => Vision.IsValid ? Vision.Default : bands.vision;

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

        /// <summary>V2 text for "here": "I am standing on food." by default.</summary>
        protected virtual string HereSentence() => $"I am standing on {Noun}.";

        public override void Validate(ValidationReport report)
        {
            var problem = bands.Check();
            if (problem != null)
                report.Error("V-30", this, $"Sense '{Label}': {problem} (SENSE-21).",
                             new ValidationFix("Sort", () => System.Array.Sort(bands.edges)));
        }

        /// <summary>Sets the bands from code (tests).</summary>
        public void SetBands(float[] edges, float vision)
        {
            bands = new Bands { edges = edges, vision = vision };
        }

        public void SetNoun(string text) => noun = text;
    }
}
