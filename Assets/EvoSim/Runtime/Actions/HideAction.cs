using UnityEngine;

namespace EvoSim
{
    /// <summary>Go to the nearest cover and stay in it (07 §4, ENV-13). No cover in sight, or no cover module: search.</summary>
    public class HideAction : AnimalAction
    {
        protected override string DefaultDescription => "go to the nearest cover and stay in it; predators can't see or catch an animal in cover";

        [SerializeField, Tooltip("Run into cover instead of walking.")]
        bool run;
        [SerializeField, Tooltip("The sense whose token says whether cover is in sight (CTRL-10).")]
        string relevantSense = "Cover";

        public override void Act(Animal a, ActContext c)
        {
            if (c.InCover(a)) { c.Stay(); return; }                     // stay once inside
            var cover = c.NearestCover(a);
            if (cover == null) { c.Search(); return; }                  // ENV-12: no cover module → search
            if (run) c.RunTo(cover.Value.Point, stopAt: 0f);
            else c.WalkTo(cover.Value.Point, stopAt: 0f);
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }
    }
}
