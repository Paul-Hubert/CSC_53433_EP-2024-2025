using UnityEngine;

namespace EvoSim
{
    /// <summary>Walk toward the nearest other animal of the species, stopping at 1 m (07 §4).</summary>
    public class FollowAction : AnimalAction
    {
        protected override string DefaultDescription => "move toward the nearest other animal";

        [SerializeField, Min(0f), Tooltip("Stop this far from the followed animal, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The sense whose token says whether kin is in sight (CTRL-10).")]
        string relevantSense = "Animal";

        public override void Act(Animal a, ActContext c)
        {
            var kin = c.NearestAnimal(a, AnimalSet.Kin);
            if (kin == null) { c.Search(); return; }
            c.WalkTo(kin.Value.Animal, stopAt);
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }
    }
}
