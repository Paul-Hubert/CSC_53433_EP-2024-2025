using UnityEngine;

namespace EvoSim
{
    /// <summary>Walk toward the nearest other animal of the species, stopping at 1 m (07 §4).</summary>
    /// <remarks>
    /// The reference follow action. Each tick it finds the nearest visible animal of its own species
    /// (<see cref="ActContext.NearestAnimal"/> with <see cref="AnimalSet.Kin"/>, never itself) and walks toward it,
    /// stopping <c>stopAt</c> meters from it (<see cref="ActContext.WalkTo(Animal, float)"/>). With no kin in sight it
    /// searches (ACT-04).
    /// </remarks>
    public class FollowAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "move toward the nearest other animal";

        [SerializeField, Min(0f), Tooltip("Stop this far from the followed animal, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The sense whose token says whether kin is in sight (CTRL-10).")]
        string relevantSense = "Animal";

        /// <summary>
        /// Walks toward the nearest kin in sight and stops <c>stopAt</c> meters from it; searches when none is in sight
        /// (ACT-04).
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            var kin = c.NearestAnimal(a, AnimalSet.Kin);
            if (kin == null) { c.Search(); return; }
            c.WalkTo(kin.Value.Animal, stopAt);
        }

        /// <summary>True when the kin sense (<c>relevantSense</c>, reference <c>"Animal"</c>) reports kin in sight (CTRL-10).</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }
    }
}
