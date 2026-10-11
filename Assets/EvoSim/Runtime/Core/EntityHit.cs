using UnityEngine;

namespace EvoSim
{
    /// <summary>An entity found by a spatial query, with its distance (ENV-20).</summary>
    /// <typeparam name="T">The kind of entity: <see cref="Carcass"/>, <see cref="Egg"/>, or a kind of your own.</typeparam>
    public readonly struct EntityHit<T> where T : Entity
    {
        /// <summary>The entity found.</summary>
        public readonly T Entity;
        /// <summary>The world distance from the asker to the entity, in meters.</summary>
        public readonly float Distance;

        /// <summary>Makes a hit.</summary>
        /// <param name="entity">The entity found.</param>
        /// <param name="distance">Its world distance from the asker, in meters.</param>
        public EntityHit(T entity, float distance) { Entity = entity; Distance = distance; }

        /// <summary>Where the entity is.</summary>
        public Vector3 Position => Entity.Position;
    }
}
