using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test locomotion that ignores the intent's direction: it turns it by 30° and halves the distance (T-MOVE-06).</summary>
    /// <remarks>
    /// The tick must go on with where the animal really is: metabolism, interactions and the next senses use the actual
    /// position (MOVE-01, MOVE-06).
    /// </remarks>
    public class TurningLocomotion : KinematicLocomotion
    {
        /// <summary>
        /// Moves half the kinematic distance (the smallest of speed, stamina and the intent's maximum), in the intent's
        /// direction turned by 30° to the right, sliding along borders; never sidesteps.
        /// </summary>
        /// <inheritdoc/>
        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (intent.IsStay) return 0f;
            var turned = Quaternion.Euler(0f, 30f, 0f) * intent.Direction;
            float d = 0.5f * Mathf.Min(a.Trait(intent.Run ? RunSpeed : WalkSpeed), Mathf.Min(staminaBudget, intent.MaxDistance));
            var from = a.Position;
            a.Position = c.Ground.SlideTo(from, from + turned * d);
            return c.Distance(from, a.Position);
        }

        /// <summary>0.1 energy for every move, to check that the extra cost is charged (MOVE-06).</summary>
        /// <inheritdoc/>
        public override float ExtraCost(Animal a, Vector3 from, Vector3 to) => 0.1f;
    }
}
