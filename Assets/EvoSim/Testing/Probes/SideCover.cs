using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test-only cover: every point with x in [MinX, MaxX] is in cover. Several covers in one world combine (ENV-11).</summary>
    /// <example>
    /// Cover on both sides of the world, open ground in the middle:
    /// <code>
    /// b.Service&lt;SideCover&gt;("Left", c =&gt; c.MaxX = 5f).Service&lt;SideCover&gt;("Right", c =&gt; c.MinX = 15f);
    /// </code>
    /// </example>
    public class SideCover : Cover
    {
        /// <summary>The cover's band along x, in meters; unbounded by default.</summary>
        public float MinX = float.NegativeInfinity, MaxX = float.PositiveInfinity;

        /// <summary>True when x is in [<see cref="MinX"/>, <see cref="MaxX"/>].</summary>
        /// <inheritdoc/>
        public override bool InCover(Vector3 p) => p.x >= MinX && p.x <= MaxX;

        /// <summary>The nearest point of the band, straight along x (distance 0 inside it).</summary>
        /// <inheritdoc/>
        public override bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance)
        {
            point = new Vector3(Mathf.Clamp(p.x, MinX, MaxX), p.y, p.z);
            distance = Mathf.Abs(point.x - p.x);
            return distance <= radius;
        }
    }
}
