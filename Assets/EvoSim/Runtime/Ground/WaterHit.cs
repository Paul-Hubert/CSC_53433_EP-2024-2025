using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest water found by a ground: the water point, a walkable shore point next to it, the distance.</summary>
    public readonly struct WaterHit
    {
        public readonly Vector3 Position;
        public readonly Vector3 Shore;
        public readonly float Distance;

        public WaterHit(Vector3 position, Vector3 shore, float distance)
        {
            Position = position; Shore = shore; Distance = distance;
        }
    }
}
