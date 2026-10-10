using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Walk to the nearest ready partner in sight, stopping at 1 m (07 §4). Breeding itself happens in the breed phase,
    /// never here (ACT-13).
    /// </summary>
    public class MateAction : AnimalAction
    {
        protected override string DefaultDescription => "walk to the nearest ready partner in sight and breed with it";

        [SerializeField, Min(0f), Tooltip("Stop this far from the partner, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The kin sense whose token says a partner is near (CTRL-10).")]
        string relevantSense = "Animal";

        public override bool Mates => true;

        public override void Act(Animal a, ActContext c)
        {
            var partner = c.NearestReadyPartner(a);
            if (partner == null) { c.Search(); return; }
            c.WalkTo(partner.Value.Animal, stopAt);
        }

        /// <summary>Relevant when kin is in the near bands (CTRL-10).</summary>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && (t.StartsWith("adjacent") || t.StartsWith("close") || t.StartsWith("medium"));
        }
    }
}
