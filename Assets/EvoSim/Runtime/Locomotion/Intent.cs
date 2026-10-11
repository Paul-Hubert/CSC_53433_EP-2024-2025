using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What an action wants this tick (ACT-03): a direction (or none: stay), walk or run, and the farthest it should go
    /// (to stop at a target). The locomotion decides what actually happens (MOVE-01).
    /// </summary>
    /// <remarks>
    /// Actions don't build intents themselves: they call the <see cref="ActContext"/> intent methods
    /// (<c>WalkTo</c>, <c>RunTo</c>, <c>RunAwayFrom</c>, <c>Stay</c>, <c>Wander</c>, <c>Search</c>), which use the static
    /// methods below. A <see cref="Locomotion"/> reads the intent in <see cref="Locomotion.Move"/> and may also change it,
    /// for example with <see cref="Stay()"/> when the ground ahead is too steep (22 §14).
    /// </remarks>
    public readonly struct Intent
    {
        /// <summary>A unit vector on the horizontal plane, or zero for stay.</summary>
        public readonly Vector3 Direction;
        /// <summary>True to run (the species' running speed), false to walk.</summary>
        public readonly bool Run;
        /// <summary>The farthest to go this tick, in meters (infinity: as far as speed and stamina allow).</summary>
        public readonly float MaxDistance;
        /// <summary>
        /// Where the action is heading: the point walked or run to, the point run away from, one meter ahead when
        /// wandering, or the animal's own position for a stay. A custom interaction's reach is measured from it.
        /// </summary>
        public readonly Vector3 Target;
        /// <summary>
        /// True for a wandering intent (<see cref="ActContext.Wander"/>): the act phase then keeps the heading the wander
        /// chose, instead of turning the animal toward where it moved.
        /// </summary>
        public readonly bool Wandering;

        /// <summary>
        /// Creates an intent. The direction is flattened onto the horizontal plane and normalized; a zero direction makes a
        /// stay, whose maximum distance is 0.
        /// </summary>
        /// <param name="direction">Where to go; only its horizontal direction counts. Zero for stay.</param>
        /// <param name="run">True to run, false to walk.</param>
        /// <param name="maxDistance">The farthest to go this tick, in meters; infinity for as far as speed and stamina allow.</param>
        /// <param name="target">The point the action is heading to (see <see cref="Target"/>).</param>
        /// <param name="wandering">True for a wandering intent.</param>
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
        /// <param name="position">The point recorded as its target, usually the animal's own position.</param>
        /// <returns>An intent that doesn't move the animal.</returns>
        public static Intent StayAt(Vector3 position) => new Intent(Vector3.zero, false, 0f, position);

        /// <summary>This intent turned into a stay (a locomotion that refuses to go, 22 §14).</summary>
        /// <returns>A stay that keeps this intent's walk or run, target and wandering flag.</returns>
        public Intent Stay() => new Intent(Vector3.zero, Run, 0f, Target, Wandering);

        /// <summary>
        /// Toward a point, stopping at a distance from it (MOVE-04): the maximum distance is what is left before the stop
        /// distance, so the locomotion never overshoots it. Already within the stop distance: a stay.
        /// </summary>
        /// <param name="from">The animal's position.</param>
        /// <param name="to">The point to reach.</param>
        /// <param name="stopAt">How far from <paramref name="to"/> to stop, in meters.</param>
        /// <param name="run">True to run, false to walk.</param>
        /// <param name="distance">The world distance from <paramref name="from"/> to <paramref name="to"/>, in meters (SPACE-02).</param>
        /// <returns>The intent, with <paramref name="to"/> as its target.</returns>
        public static Intent Toward(Vector3 from, Vector3 to, float stopAt, bool run, float distance)
        {
            if (distance <= stopAt + 1e-6f) return StayAt(to);
            return new Intent(to - from, run, distance - stopAt, to);
        }

        /// <summary>Away from a point, with no distance limit: as far as speed and stamina allow.</summary>
        /// <param name="from">The animal's position.</param>
        /// <param name="threat">The point to move away from; it becomes the intent's target.</param>
        /// <param name="run">True to run, false to walk.</param>
        /// <param name="fallbackDirection">The direction to take when the animal stands on the threat's point.</param>
        /// <returns>The intent.</returns>
        public static Intent AwayFrom(Vector3 from, Vector3 threat, bool run, Vector3 fallbackDirection)
        {
            var d = from - threat;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-10f) d = fallbackDirection;
            return new Intent(d, run, float.PositiveInfinity, threat);
        }

        /// <summary>
        /// A short text for logs and the inspector: <c>stay</c>, or walk or run with the direction and the maximum
        /// distance.
        /// </summary>
        /// <returns>The text, such as <c>stay</c> or <c>run (direction) ≤ 2 m</c>.</returns>
        public override string ToString() => IsStay ? "stay" : $"{(Run ? "run" : "walk")} {Direction} ≤ {MaxDistance:0.##} m";
    }
}
