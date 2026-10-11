using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Walk to the nearest ready partner in sight, stopping at 1 m (07 §4). Breeding itself happens in the breed phase,
    /// never here (ACT-13).
    /// </summary>
    /// <remarks>
    /// The reference mate action. Each tick it finds the nearest visible kin that is ready to mate
    /// (<see cref="ActContext.NearestReadyPartner"/>) and walks toward it, stopping <c>stopAt</c> meters away; with no
    /// ready partner in sight it searches (ACT-04). <see cref="Mates"/> is true, so the breed phase breeds the animals
    /// that chose it (REPRO-02).
    /// </remarks>
    public class MateAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "walk to the nearest ready partner in sight and breed with it";

        [SerializeField, Min(0f), Tooltip("Stop this far from the partner, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The kin sense whose token says a partner is near (CTRL-10).")]
        string relevantSense = "Animal";

        /// <summary>True: the breed phase breeds animals that chose this action (REPRO-02).</summary>
        public override bool Mates => true;

        /// <summary>
        /// Walks to the nearest ready partner in sight and stops <c>stopAt</c> meters from it; searches when none is in
        /// sight (ACT-04).
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            var partner = c.NearestReadyPartner(a);
            if (partner == null) { c.Search(); return; }
            c.WalkTo(partner.Value.Animal, stopAt);
        }

        /// <summary>
        /// Relevant when kin is in the near bands (CTRL-10): the kin sense (<c>relevantSense</c>, reference <c>"Animal"</c>)
        /// reports it adjacent, close or at medium distance.
        /// </summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && (t.StartsWith("adjacent") || t.StartsWith("close") || t.StartsWith("medium"));
        }
    }
}
