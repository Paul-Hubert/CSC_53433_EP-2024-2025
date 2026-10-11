using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The nearest animal of a set (threats, prey, kin, listed species), in bands (06 §4). With readiness on, a kin
    /// sense also says whether that animal is ready to mate, when it is within the partner range; beyond it readiness
    /// is unknown and not written. Hidden and killed animals are never reported (SENSE-06).
    /// </summary>
    /// <remarks>
    /// <para>The reference of teaching-path exercise 5 (<c>MyNearestAnimalSense</c>, 22 §0). What it does:</para>
    /// <list type="bullet">
    /// <item><description>Tokens (<see cref="BuildTokens"/>): for each band in order, "close, ready" and "close, not ready"
    /// when readiness is on and part of the band lies within the partner range, then the plain "close" when readiness is off
    /// or part of the band lies beyond it; "none" last. The reference threat sense has 5 tokens (4 bands, none), the
    /// reference kin sense 9 (4 bands × ready / not ready, none).</description></item>
    /// <item><description><see cref="Read"/>: the animal's vision trait is the radius; the nearest visible animal of the
    /// set (<see cref="SenseContext.NearestAnimal(Animal, AnimalSet, float, IReadOnlyList{Species})"/>) gives the band; within
    /// the partner range, <see cref="SenseContext.IsReady"/> picks "ready" or "not ready"; nothing found gives
    /// "none".</description></item>
    /// <item><description><see cref="Write"/>: the banded text ("Animal: 1-4 meters away.") plus ", ready to mate" in V1, or
    /// " It is ready to mate." after the sentence in V2.</description></item>
    /// <item><description>Default label: "Animal" for kin; for threats and prey, the display name of the only species in
    /// the set, else "Predator" or "Prey".</description></item>
    /// </list>
    /// <para>The reference prey has two (a threat sense "Predator" and a kin sense "Animal" with readiness); the reference
    /// predator has a prey sense "Prey" and a kin sense "Other predator" with readiness.</para>
    /// </remarks>
    public class NearestAnimalSense : BandedSense
    {
        [SerializeField, Tooltip("Threats (\"Predator\"), Prey, Kin (\"Animal\") or Listed species (SPEC-12).")]
        AnimalSet targets = AnimalSet.Threats;
        [SerializeField, Tooltip("Species ids or names, when targets is Listed (ARCH-08: by name).")]
        List<string> listed = new List<string>();
        [SerializeField, Tooltip("Add \", ready to mate\" or \", not ready to mate\" within the partner range (06 §4).")]
        bool reportReadiness;
        [SerializeField, Min(0f), Tooltip("Readiness is seen within this distance, in meters (reference 20 = the vision).")]
        float partnerRange = 20f;

        readonly List<Species> listedSpecies = new List<Species>();   // the listed names resolved to species, in Initialize
        /// <summary>Per token: 0 readiness not written, 1 ready, 2 not ready.</summary>
        readonly List<int> readinessOf = new List<int>();

        /// <summary>Which animals the sense looks for: threats, prey, kin or the listed species (SPEC-12).</summary>
        public AnimalSet Targets => targets;

        /// <summary>True when the sense says whether the animal found is ready to mate (kin senses, 06 §4).</summary>
        public bool ReportsReadiness => reportReadiness;

        /// <summary>
        /// Within this distance, in meters, readiness is seen (reference 20 = the vision). The mate action never targets a
        /// partner beyond it (ACT-05).
        /// </summary>
        public float PartnerRange => partnerRange;

        /// <summary>
        /// Kin: "Animal". Threats and prey: the species' display name when the set holds one species (so renaming a
        /// species changes what the brain reads, SPEC-20), else "Predator" or "Prey".
        /// </summary>
        protected override string DefaultLabel
        {
            get
            {
                if (targets == AnimalSet.Kin) return "Animal";
                var set = World != null && Species != null ? World.Resolve(Species, targets, listedSpecies) : null;
                if (set != null && set.Count == 1) return Capitalize(set[0].DisplayName);
                return targets == AnimalSet.Prey ? "Prey" : "Predator";
            }
        }

        /// <summary>The text with its first letter in upper case ("predator" gives "Predator").</summary>
        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        /// <summary>
        /// Finds the listed species by id or name (unknown names are skipped; validation reports them), then builds the
        /// tokens.
        /// </summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            listedSpecies.Clear();
            foreach (var n in listed)
            {
                var s = World.FindSpeciesByName(n);
                if (s != null) listedSpecies.Add(s);
            }
            base.Initialize();
        }

        /// <summary>V2 noun: kin read as "other animal" unless the label already says "Other …".</summary>
        public override string Noun =>
            !string.IsNullOrEmpty(noun) ? noun
            : targets == AnimalSet.Kin && !Label.StartsWith("Other", System.StringComparison.OrdinalIgnoreCase) ? "other " + Label.ToLowerInvariant()
            : Label.ToLowerInvariant();

        /// <summary>
        /// Per band: "close, ready" and "close, not ready" where the band reaches within the partner range, "close" where it
        /// reaches beyond it (or always, readiness off); then "none". No "here" token.
        /// </summary>
        protected override void BuildTokens()
        {
            tokens.Clear();
            bandOf.Clear();
            readinessOf.Clear();
            for (int b = 0; b < bands.Count; b++)
            {
                // A band that straddles the partner range needs both kinds of tokens: with readiness, and without.
                var (lower, upper) = bands.Range(b, bands.vision);
                string name = bands.NameOf(b);
                bool someWithin = reportReadiness && lower < partnerRange;
                bool someBeyond = !reportReadiness || upper > partnerRange;
                if (someWithin)
                {
                    AddToken(name + ", ready", b, 1);
                    AddToken(name + ", not ready", b, 2);
                }
                if (someBeyond) AddToken(name, b, 0);
            }
            AddToken(Bands.None, -1, 0);
        }

        /// <summary>Adds a token with its band and its readiness (0 not written, 1 ready, 2 not ready).</summary>
        void AddToken(string token, int band, int readiness)
        {
            Add(token, band);
            readinessOf.Add(readiness);
        }

        /// <summary>
        /// The band of the nearest visible animal of the set within the animal's vision and, within the partner range with
        /// readiness on, whether it is ready to mate; "none" when there is none.
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            float vision = a.Trait(Vision);
            var nearest = s.NearestAnimal(a, targets, vision, listedSpecies);   // hidden and killed excluded
            if (nearest == null) return NoneToken;
            int band = bands.IndexOf(nearest.Value.Distance, vision);
            if (band < 0) return NoneToken;
            int readiness = 0;
            if (reportReadiness && nearest.Value.Distance <= partnerRange)
                readiness = s.IsReady(nearest.Value.Animal) ? 1 : 2;
            for (int t = 0; t < tokens.Count; t++)
                if (bandOf[t] == band && readinessOf[t] == readiness) return t;
            return NoneToken;
        }

        /// <summary>
        /// The banded text, with readiness when the token has it: V1 "Animal: 1-4 meters away, ready to mate.", V2 "The
        /// nearest other animal is 1-4 meters away. It is ready to mate.".
        /// </summary>
        /// <inheritdoc/>
        public override string Write(int token, TextStyle style)
        {
            string text = base.Write(token, style);
            int r = readinessOf[token];
            if (r == 0) return text;
            if (style == TextStyle.V1) return text.Substring(0, text.Length - 1) + (r == 1 ? ", ready to mate." : ", not ready to mate.");
            return text + (r == 1 ? " It is ready to mate." : " It is not ready to mate.");
        }

        /// <summary>Sets the sense from code (WorldBuilder, tests), as the Inspector fields do.</summary>
        /// <param name="set">Which animals to look for.</param>
        /// <param name="readiness">True to report whether a kin animal is ready to mate.</param>
        /// <param name="partner">The partner range, in meters.</param>
        /// <param name="listedNames">Species ids or names for <see cref="AnimalSet.Listed"/>; null for none.</param>
        /// <returns>This sense, to chain calls.</returns>
        public NearestAnimalSense Configure(AnimalSet set, bool readiness = false, float partner = 20f, IEnumerable<string> listedNames = null)
        {
            targets = set;
            reportReadiness = readiness;
            partnerRange = partner;
            listed = listedNames != null ? new List<string>(listedNames) : new List<string>();
            return this;
        }

        /// <summary>
        /// Adds to the band checks a V-21 warning when the set is empty for this species (no threat, no prey…) and V-20 for
        /// a listed name that is no species.
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (targets != AnimalSet.Listed && World.Resolve(Species, targets).Count == 0)
                report.Warning("V-21", this, $"Sense '{Label}' looks for {targets}, and species '{Species.DisplayName}' has none.");
            foreach (var n in listed)
                if (World.FindSpeciesByName(n) == null) report.Error("V-20", this, $"Sense '{Label}' lists '{n}', which is no species.");
        }
    }
}
