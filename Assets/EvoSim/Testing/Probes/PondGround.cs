using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test ground: flat, with a round pond (not walkable) whose shore animals can reach (samples 22 §2–§3). It answers
    /// the water queries that the water sense and the drink action ask.
    /// </summary>
    /// <example>
    /// <code>
    /// var w = New().Ground&lt;PondGround&gt;(20, 20).Food(0f, 0f)
    ///     .Species("prey", s =&gt; s.PreyBody().Action&lt;RestAction&gt;("rest").Sense&lt;NearestWaterSense&gt;()).Build();
    /// </code>
    /// </example>
    public class PondGround : FlatGround
    {
        /// <summary>The pond's center; its height is ignored.</summary>
        public Vector3 PondCenter = new Vector3(10f, 0f, 10f);
        /// <summary>The pond's radius, in meters.</summary>
        public float PondRadius = 3f;

        /// <summary>True inside the pond.</summary>
        /// <inheritdoc/>
        public override bool IsWater(Vector3 p) => Flat(p, PondCenter) < PondRadius;

        /// <summary>Walkable on the flat ground and outside the pond.</summary>
        /// <inheritdoc/>
        public override bool IsWalkable(Vector3 p) => base.IsWalkable(p) && !IsWater(p);

        /// <summary>
        /// The pond seen from <paramref name="p"/>: its water just inside the edge, its shore just outside (1 cm each way), and
        /// the distance to the edge (0 in the pond); null when the edge is farther than <paramref name="radius"/>.
        /// </summary>
        /// <inheritdoc/>
        public override WaterHit? NearestWater(Vector3 p, float radius)
        {
            float d = Mathf.Max(0f, Flat(p, PondCenter) - PondRadius);
            if (d > radius) return null;
            var dir = new Vector3(p.x - PondCenter.x, 0f, p.z - PondCenter.z);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;   // at the very center: any direction will do
            dir.Normalize();
            var water = PondCenter + dir * (PondRadius - 0.01f);
            var shore = PondCenter + dir * (PondRadius + 0.01f);
            return new WaterHit(water, shore, d);
        }

        /// <summary>The horizontal distance between two points, in meters.</summary>
        static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
