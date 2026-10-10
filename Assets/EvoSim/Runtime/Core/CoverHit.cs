using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest cover found by a query: a point in cover and its distance (ENV-10).</summary>
    public readonly struct CoverHit
    {
        public readonly Vector3 Point;
        public readonly float Distance;

        public CoverHit(Vector3 point, float distance) { Point = point; Distance = distance; }

        public Vector3 Position => Point;
    }
}
