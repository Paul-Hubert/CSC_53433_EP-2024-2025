using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test locomotion that ignores the intent's direction: it turns it by 30° and halves the distance (T-MOVE-06).</summary>
    public class TurningLocomotion : KinematicLocomotion
    {
        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (intent.IsStay) return 0f;
            var turned = Quaternion.Euler(0f, 30f, 0f) * intent.Direction;
            float d = 0.5f * Mathf.Min(a.Trait(intent.Run ? RunSpeed : WalkSpeed), Mathf.Min(staminaBudget, intent.MaxDistance));
            var from = a.Position;
            a.Position = c.Ground.SlideTo(from, from + turned * d);
            return c.Distance(from, a.Position);
        }

        public override float ExtraCost(Animal a, Vector3 from, Vector3 to) => 0.1f;
    }
}
