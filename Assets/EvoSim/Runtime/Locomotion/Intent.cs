using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What an action wants this tick (ACT-03): a direction (or none: stay), walk or run, and the farthest it should go
    /// (to stop at a target). The locomotion decides what actually happens (MOVE-01).
    /// </summary>
    public readonly struct Intent
    {
        /// <summary>A unit vector on the horizontal plane, or zero for stay.</summary>
        public readonly Vector3 Direction;
        public readonly bool Run;
        /// <summary>The farthest to go this tick, in meters (infinity: as far as speed and stamina allow).</summary>
        public readonly float MaxDistance;
        /// <summary>Where the action is heading (its target), or the animal's own position.</summary>
        public readonly Vector3 Target;
        public readonly bool Wandering;

        public Intent(Vector3 direction, bool run, float maxDistance, Vector3 target, bool wandering = false)
        {
            direction.y = 0f;
            Direction = direction.sqrMagnitude > 1e-12f ? direction.normalized : Vector3.zero;
            Run = run;
            MaxDistance = Direction == Vector3.zero ? 0f : Mathf.Max(0f, maxDistance);
            Target = target;
            Wandering = wandering;
        }

        /// <summary>No movement this tick.</summary>
        public bool IsStay => Direction == Vector3.zero || MaxDistance <= 0f;

        /// <summary>A stay intent at a position.</summary>
        public static Intent StayAt(Vector3 position) => new Intent(Vector3.zero, false, 0f, position);

        /// <summary>This intent turned into a stay (a locomotion that refuses to go, 22 §14).</summary>
        public Intent Stay() => new Intent(Vector3.zero, Run, 0f, Target, Wandering);

        /// <summary>Toward a point, stopping at a distance from it (MOVE-04).</summary>
        public static Intent Toward(Vector3 from, Vector3 to, float stopAt, bool run, float distance)
        {
            if (distance <= stopAt + 1e-6f) return StayAt(to);
            return new Intent(to - from, run, distance - stopAt, to);
        }

        /// <summary>Away from a point.</summary>
        public static Intent AwayFrom(Vector3 from, Vector3 threat, bool run, Vector3 fallbackDirection)
        {
            var d = from - threat;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-10f) d = fallbackDirection;
            return new Intent(d, run, float.PositiveInfinity, threat);
        }

        public override string ToString() => IsStay ? "stay" : $"{(Run ? "run" : "walk")} {Direction} ≤ {MaxDistance:0.##} m";
    }
}
