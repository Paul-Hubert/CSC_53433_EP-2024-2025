using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Scenario S18 (ENV-21): walk to the nearest carcass in sight, then drag it 1 m per tick toward the nearest cover.
    /// The carcass moves in the act phase through an interaction, never from the visuals.
    /// </summary>
    public class DragAction : AnimalAction
    {
        [SerializeField, Min(0.1f), Tooltip("How close the animal must be to grab the carcass, in meters (reference 1).")]
        float grabReach = 1f;

        protected override string DefaultDescription => "drag the nearest carcass toward cover";

        public override void Act(Animal a, ActContext c)
        {
            var carcass = c.NearestCarcass(a);
            if (carcass == null) { c.Search(); return; }
            if (carcass.Value.Distance > grabReach) { c.WalkTo(carcass.Value.Position, 0f); return; }
            var cover = c.NearestCover(a);
            if (cover == null) { c.Stay(); return; }
            c.WalkTo(cover.Value.Point, 0f);
            c.OnArrival(new Drag(carcass.Value.Entity));
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token("Carcass");
            return t != null && t != "none";
        }

        /// <summary>After moving, the carcass follows the animal (it was within reach before the move).</summary>
        sealed class Drag : IInteraction
        {
            readonly Carcass carcass;
            public Drag(Carcass c) { carcass = c; }
            public float Reach => float.PositiveInfinity;
            public void Apply(Animal a, InteractionContext c)
            {
                if (!carcass.UsedUp) carcass.Position = a.Position;
            }
        }
    }
}
