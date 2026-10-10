using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A kinematic locomotion that refuses slopes steeper than a limit (T-MOVE-07, after 22 §14).</summary>
    public class SlopeLimitLocomotion : KinematicLocomotion
    {
        public float MaxSlopeDegrees = 20f;

        public override float Move(Animal a, Intent intent, float stamina, MoveContext c)
        {
            if (!intent.IsStay)
            {
                var ahead = a.Position + intent.Direction * Mathf.Max(0.25f, Mathf.Min(1f, intent.MaxDistance));
                if (c.Ground.SlopeDegrees(a.Position, ahead) > MaxSlopeDegrees) intent = intent.Stay();
            }
            return base.Move(a, intent, stamina, c);
        }
    }
}
