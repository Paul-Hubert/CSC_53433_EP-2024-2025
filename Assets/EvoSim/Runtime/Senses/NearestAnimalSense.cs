using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The nearest animal of a set (threats, prey, kin, listed species), in bands (06 §4). With readiness on, a kin
    /// sense also says whether that animal is ready to mate, when it is within the partner range; beyond it readiness
    /// is unknown and not written. Hidden and killed animals are never reported (SENSE-06).
    /// </summary>
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

        readonly List<Species> listedSpecies = new List<Species>();
        /// <summary>Per token: 0 readiness not written, 1 ready, 2 not ready.</summary>
        readonly List<int> readinessOf = new List<int>();

        public AnimalSet Targets => targets;
        public bool ReportsReadiness => reportReadiness;
        public float PartnerRange => partnerRange;

        protected override string DefaultLabel =>
            targets == AnimalSet.Threats ? "Predator" : targets == AnimalSet.Prey ? "Prey" : targets == AnimalSet.Kin ? "Animal" : "Animal";

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

        /// <summary>none, then per band: "close, ready" and "close, not ready" within the partner range, "close" beyond it.</summary>
        protected override void BuildTokens()
        {
            tokens.Clear();
            bandOf.Clear();
            readinessOf.Clear();
            for (int b = 0; b < bands.Count; b++)
            {
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

        void AddToken(string token, int band, int readiness)
        {
            Add(token, band);
            readinessOf.Add(readiness);
        }

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

        public override string Write(int token, TextStyle style)
        {
            string text = base.Write(token, style);
            int r = readinessOf[token];
            if (r == 0) return text;
            if (style == TextStyle.V1) return text.Substring(0, text.Length - 1) + (r == 1 ? ", ready to mate." : ", not ready to mate.");
            return text + (r == 1 ? " It is ready to mate." : " It is not ready to mate.");
        }

        /// <summary>Sets the sense from code (WorldBuilder, tests).</summary>
        public NearestAnimalSense Configure(AnimalSet set, bool readiness = false, float partner = 20f, IEnumerable<string> listedNames = null)
        {
            targets = set;
            reportReadiness = readiness;
            partnerRange = partner;
            listed = listedNames != null ? new List<string>(listedNames) : new List<string>();
            return this;
        }

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
