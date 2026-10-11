using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A kinematic locomotion that refuses slopes steeper than a limit (T-MOVE-07, after 22 §14).</summary>
    /// <remarks>Unlike the sample <c>SlopeLocomotion</c>, it charges no energy for climbing.</remarks>
    public class SlopeLimitLocomotion : KinematicLocomotion
    {
        /// <summary>Steeper ground is refused, in degrees.</summary>
        public float MaxSlopeDegrees = 20f;

        /// <summary>
        /// Looks at the slope over the next step (between 0.25 m and 1 m ahead); too steep, the animal stays. Otherwise it
        /// moves as the kinematic locomotion does.
        /// </summary>
        /// <param name="a">The animal to move.</param>
        /// <param name="intent">Where its action wants it to go.</param>
        /// <param name="stamina">The meters its stamina allows this tick (the base class's <c>staminaBudget</c>).</param>
        /// <param name="c">The ground, distances and random streams of the act phase.</param>
        /// <returns>The meters actually moved (MOVE-06).</returns>
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
