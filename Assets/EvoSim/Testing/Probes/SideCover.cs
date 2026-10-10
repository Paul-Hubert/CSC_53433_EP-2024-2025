using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test-only cover: every point with x in [MinX, MaxX] is in cover.</summary>
    public class SideCover : Cover
    {
        public float MinX = float.NegativeInfinity, MaxX = float.PositiveInfinity;

        public override bool InCover(Vector3 p) => p.x >= MinX && p.x <= MaxX;

        public override bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance)
        {
            point = new Vector3(Mathf.Clamp(p.x, MinX, MaxX), p.y, p.z);
            distance = Mathf.Abs(point.x - p.x);
            return distance <= radius;
        }
    }
}
