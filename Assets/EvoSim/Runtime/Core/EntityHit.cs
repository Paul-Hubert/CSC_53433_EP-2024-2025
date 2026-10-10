using UnityEngine;

namespace EvoSim
{
    /// <summary>An entity found by a spatial query, with its distance (ENV-20).</summary>
    public readonly struct EntityHit<T> where T : Entity
    {
        public readonly T Entity;
        public readonly float Distance;

        public EntityHit(T entity, float distance) { Entity = entity; Distance = distance; }

        public Vector3 Position => Entity.Position;
    }
}
