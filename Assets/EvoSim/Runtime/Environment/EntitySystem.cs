using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>The entities of one kind, found through spatial queries like animals (ENV-20).</summary>
    public abstract class EntitySystem<T> : EntityKind where T : Entity
    {
        protected readonly List<T> entities = new List<T>();

        /// <summary>Living entities, in creation (id) order.</summary>
        public IReadOnlyList<T> Entities => entities;
        public override int EntityCount => entities.Count;

        public override void Initialize() => entities.Clear();

        /// <summary>Adds an entity: it gets an id and its creation tick.</summary>
        public T Add(T e)
        {
            e.Id = World.NextEntityId();
            e.CreatedTick = World.Tick;
            entities.Add(e);
            return e;
        }

        /// <summary>
        /// The nearest entity within radius that passes the filter (SPACE-07); ties by id (SPACE-08).
        /// The filter receives the viewer, so it needs no closure.
        /// </summary>
        public bool Nearest(Vector3 p, float radius, Animal viewer, Func<T, Animal, bool> filter, out T found, out float distance)
        {
            found = null;
            distance = float.PositiveInfinity;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (e.UsedUp || (filter != null && !filter(e, viewer))) continue;
                float d = World.Distance(p, e.Position);
                if (d > radius) continue;
                if (found == null || d < distance - 1e-6f || (Mathf.Abs(d - distance) <= 1e-6f && e.Id < found.Id))
                {
                    found = e;
                    distance = d;
                }
            }
            return found != null;
        }

        /// <summary>Moves, ages and removes entities; the environment phase calls it once per tick (ENV-22).</summary>
        public override void UpdateEntities(TickContext t)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                Move(e, t);
                if (e.LifetimeLeft > 0) e.LifetimeLeft--;
            }
            int w = 0;
            for (int r = 0; r < entities.Count; r++)
            {
                var e = entities[r];
                if (e.UsedUp || e.LifetimeLeft == 0) { OnRemoved(e, e.UsedUp ? "used up" : "expired", t); continue; }
                entities[w++] = e;
            }
            if (w < entities.Count) entities.RemoveRange(w, entities.Count - w);
        }

        /// <summary>Moves an entity (ENV-21); none by default. Only phases move entities, never the visuals.</summary>
        protected virtual void Move(T e, TickContext t) { }

        /// <summary>Called when an entity is removed in the environment phase.</summary>
        protected virtual void OnRemoved(T e, string reason, TickContext t) { }

        /// <summary>Removes everything (tests).</summary>
        public void Clear() => entities.Clear();
    }
}
