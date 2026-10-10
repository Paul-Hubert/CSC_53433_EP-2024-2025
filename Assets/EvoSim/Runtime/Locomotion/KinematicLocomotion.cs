using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference locomotion (MOVE-04): straight lines, no physics. Moves by the smallest of speed, stamina and the
    /// intent's maximum distance; slides along borders and obstacles; if stuck, takes a random sidestep.
    /// </summary>
    public class KinematicLocomotion : Locomotion
    {
        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (intent.IsStay) return 0f;
            float speed = a.Trait(intent.Run ? RunSpeed : WalkSpeed);
            float d = Mathf.Min(speed, Mathf.Min(staminaBudget, intent.MaxDistance));
            if (d <= 0f) return 0f;
            var from = a.Position;
            var ground = c.Ground;
            Vector3 reached = ground != null ? ground.SlideTo(from, from + intent.Direction * d) : from + intent.Direction * d;
            if (ground != null && c.Distance(from, reached) < Units.Epsilon)                    // stuck: a random sidestep
                reached = ground.SideStep(from, d, c.Stream(Species, "actions"));
            float moved = c.Distance(from, reached);
            a.Position = reached;
            return moved;
        }
    }
}
