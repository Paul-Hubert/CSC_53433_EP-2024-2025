using UnityEngine;

namespace EvoSim
{
    /// <summary>An animal found by a spatial query, with its distance from the asker (SPACE-07).</summary>
    public readonly struct AnimalHit
    {
        public readonly Animal Animal;
        public readonly float Distance;

        public AnimalHit(Animal animal, float distance) { Animal = animal; Distance = distance; }

        public Vector3 Position => Animal.Position;
        public bool Found => Animal != null;
    }
}
