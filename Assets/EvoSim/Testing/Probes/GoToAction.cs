using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test action that walks or runs toward a fixed point, stopping at a distance (T-SPACE-01, T-MOVE-01).</summary>
    public class GoToAction : AnimalAction
    {
        protected override string DefaultDescription => "go to a fixed point (a test probe)";

        public Vector3 Target;
        public float StopAt;
        public bool Run;
        /// <summary>When set, the action gives this direction with no distance limit (T-SPACE-03).</summary>
        public Vector3 Direction;

        public override void Act(Animal a, ActContext c)
        {
            if (Direction != Vector3.zero) { c.WalkTo(a.Position + Direction * 1000f); return; }
            if (Run) c.RunTo(Target, StopAt);
            else c.WalkTo(Target, StopAt);
        }
    }
}
