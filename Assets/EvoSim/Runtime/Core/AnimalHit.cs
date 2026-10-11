using UnityEngine;

namespace EvoSim
{
    /// <summary>An animal found by a spatial query, with its distance from the asker (SPACE-07).</summary>
    /// <remarks>Queries return it as <c>AnimalHit?</c>: null when nothing was found, so read <c>hit.Value.Animal</c>
    /// after a null check.</remarks>
    public readonly struct AnimalHit
    {
        /// <summary>The animal found, or null for an empty hit.</summary>
        public readonly Animal Animal;
        /// <summary>The world distance from the asker to the animal, in meters; infinity for an empty hit.</summary>
        public readonly float Distance;

        /// <summary>Makes a hit.</summary>
        /// <param name="animal">The animal found, or null.</param>
        /// <param name="distance">Its world distance from the asker, in meters.</param>
        public AnimalHit(Animal animal, float distance) { Animal = animal; Distance = distance; }

        /// <summary>Where the animal stands now.</summary>
        public Vector3 Position => Animal.Position;
        /// <summary>True when the hit holds an animal.</summary>
        public bool Found => Animal != null;
    }
}
