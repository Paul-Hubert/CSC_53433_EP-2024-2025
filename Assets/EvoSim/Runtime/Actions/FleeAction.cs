using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Run away from the nearest threat (07 §4). The prototype's "flee into cover" is an option, off in the reference:
    /// already in cover, stay; cover within coverSeek, run into it.
    /// </summary>
    public class FleeAction : AnimalAction
    {
        protected override string DefaultDescription => "run away from the nearest predator";

        [SerializeField, Tooltip("Whom to flee: the species' threats (reference), or listed species (SPEC-12).")]
        AnimalSet from = AnimalSet.Threats;
        [SerializeField, Tooltip("Species ids or names, when 'from' is Listed (ARCH-08: by name).")]
        List<string> listed = new List<string>();
        [SerializeField, Tooltip("The prototype's behaviour (S06): stay in cover, or run into cover within coverSeek. Off in the reference.")]
        bool fleeIntoCover;
        [SerializeField, Min(0f), Tooltip("Cover within this distance is run into, in meters (reference 6).")]
        float coverSeek = 6f;
        [SerializeField, Tooltip("The sense whose token says whether a threat is in sight (CTRL-10).")]
        string relevantSense = "Predator";

        readonly List<Species> listedSpecies = new List<Species>();

        public bool FleeIntoCover { get => fleeIntoCover; set => fleeIntoCover = value; }

        public override void Initialize()
        {
            listedSpecies.Clear();
            foreach (var n in listed)
            {
                var s = World.FindSpeciesByName(n);
                if (s != null) listedSpecies.Add(s);
            }
        }

        public override void Act(Animal a, ActContext c)
        {
            var threat = c.NearestAnimal(a, from, listedSpecies);
            if (threat == null) { c.Search(); return; }
            if (fleeIntoCover)
            {
                if (c.InCover(a)) { c.Stay(); return; }
                var cover = c.NearestCover(a, coverSeek);
                if (cover != null) { c.RunTo(cover.Value.Point, stopAt: 0f); return; }
            }
            c.RunAwayFrom(threat.Value.Animal);
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }

        public override void Validate(ValidationReport report)
        {
            if (from != AnimalSet.Listed && World.Resolve(Species, from).Count == 0)
                report.Warning("V-21", this, $"Flee has no {from} to flee from in species '{Species.DisplayName}'.");
            foreach (var n in listed)
                if (World.FindSpeciesByName(n) == null) report.Error("V-20", this, $"Flee lists '{n}', which is no species.");
        }
    }
}
