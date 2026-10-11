using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest water found by a ground: the water point, a walkable shore point next to it, the distance.</summary>
    /// <remarks>Returned by <see cref="Ground.NearestWater"/>; an action walks to <see cref="Shore"/>, since water itself is not walkable.</remarks>
    public readonly struct WaterHit
    {
        /// <summary>A point of water, on the ground.</summary>
        public readonly Vector3 Position;
        /// <summary>A walkable point next to the water, on the ground: where a drinking animal stands.</summary>
        public readonly Vector3 Shore;
        /// <summary>The world distance from the query point to the water, in meters.</summary>
        public readonly float Distance;

        /// <summary>A water hit, as a ground's <see cref="Ground.NearestWater"/> builds it.</summary>
        /// <param name="position">The water point.</param>
        /// <param name="shore">The walkable shore point next to it.</param>
        /// <param name="distance">The distance from the query point to the water, in meters.</param>
        public WaterHit(Vector3 position, Vector3 shore, float distance)
        {
            Position = position; Shore = shore; Distance = distance;
        }
    }
}
