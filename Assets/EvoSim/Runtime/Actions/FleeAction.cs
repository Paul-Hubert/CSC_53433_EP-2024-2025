using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Run away from the nearest threat (07 §4). The prototype's "flee into cover" is an option, off in the reference:
    /// already in cover, stay; cover within coverSeek, run into it.
    /// </summary>
    /// <remarks>
    /// <para>The reference flee action of the prey, and teaching-path exercise 6. Each tick it:</para>
    /// <list type="bullet">
    /// <item><description>finds the nearest visible animal it flees from (<see cref="ActContext.NearestAnimal"/>): by
    /// default its species' threats, the species whose diet strikes it (SPEC-12); or the species listed by id or display
    /// name when <c>from</c> is <see cref="AnimalSet.Listed"/>;</description></item>
    /// <item><description>with none in sight, calls <see cref="ActContext.Search"/>;</description></item>
    /// <item><description>with <see cref="FleeIntoCover"/> on: stays when already in cover, or runs into the nearest
    /// cover within <c>coverSeek</c> meters;</description></item>
    /// <item><description>otherwise runs straight away from the threat
    /// (<see cref="ActContext.RunAwayFrom(Animal)"/>), as far as its running speed and stamina allow.</description></item>
    /// </list>
    /// </remarks>
    public class FleeAction : AnimalAction
    {
        /// <inheritdoc/>
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

        readonly List<Species> listedSpecies = new List<Species>();   // the listed names, resolved to species in Initialize

        /// <summary>
        /// True to flee like the prototype (scenario S06): stay when in cover, run into cover within <c>coverSeek</c>
        /// meters. Off in the reference, where hiding is its own action.
        /// </summary>
        public bool FleeIntoCover { get => fleeIntoCover; set => fleeIntoCover = value; }

        /// <summary>Resolves the listed species by id or display name (ARCH-08); names that match no species are skipped (V-20).</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            listedSpecies.Clear();
            foreach (var n in listed)
            {
                var s = World.FindSpeciesByName(n);
                if (s != null) listedSpecies.Add(s);
            }
        }

        /// <summary>
        /// Runs away from the nearest threat in sight, or into nearby cover when <see cref="FleeIntoCover"/> is on;
        /// searches when no threat is in sight (ACT-04).
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>
        /// True when the threat sense (<c>relevantSense</c>, reference <c>"Predator"</c>) reports a threat in sight
        /// (CTRL-10).
        /// </summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }

        /// <summary>Warns when the species has nothing to flee from (V-21) and reports listed names that are no species (V-20).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (from != AnimalSet.Listed && World.Resolve(Species, from).Count == 0)
                report.Warning("V-21", this, $"Flee has no {from} to flee from in species '{Species.DisplayName}'.");
            foreach (var n in listed)
                if (World.FindSpeciesByName(n) == null) report.Error("V-20", this, $"Flee lists '{n}', which is no species.");
        }
    }
}
