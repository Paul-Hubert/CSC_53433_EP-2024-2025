using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest cover found by a query: a point in cover and its distance (ENV-10).</summary>
    public readonly struct CoverHit
    {
        /// <summary>A point in cover: the nearest one found, or where the asker stands when it is in cover already.</summary>
        public readonly Vector3 Point;
        /// <summary>The world distance from the asker to <see cref="Point"/>, in meters; 0 when it stands in cover.</summary>
        public readonly float Distance;

        /// <summary>Makes a hit.</summary>
        /// <param name="point">A point in cover.</param>
        /// <param name="distance">Its world distance from the asker, in meters.</param>
        public CoverHit(Vector3 point, float distance) { Point = point; Distance = distance; }

        /// <summary>The same as <see cref="Point"/>, so every hit kind has a Position.</summary>
        public Vector3 Position => Point;
    }
}
