using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>The entities of one kind, found through spatial queries like animals (ENV-20).</summary>
    /// <typeparam name="T">The entity type it keeps, e.g. <see cref="Carcass"/> or <see cref="Egg"/>.</typeparam>
    /// <remarks>
    /// <para>Derive from it with your entity type, give the kind a name (<see cref="EntityKind.Kind"/>), and place the
    /// component under the World (reference: under Environment). Entities enter through <see cref="Add"/>; actions and senses
    /// find them with <see cref="Nearest"/> or <see cref="WorldQueries.NearestEntity{T}"/>. Once per tick the environment
    /// phase calls <see cref="UpdateEntities"/>: <see cref="Move"/> each one, count lifetimes down, then remove the used-up
    /// and expired ones, calling <see cref="OnRemoved"/> for each.</para>
    /// <para>The list stays in creation (id) order, so every loop over it is deterministic (RAND-05). An override of
    /// <see cref="Move"/> draws random numbers only from a named stream of <c>t</c>.</para>
    /// <para>Put an <see cref="Edible"/> on the same GameObject to make the kind food; diets then name it
    /// "&lt;kind&gt;:&lt;species&gt;" (22 §15).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // A kind of one's own (from EvoSim's test probes): nothing moves or rots unless given a lifetime.
    /// public class Fruit : Entity
    /// {
    ///     public override string Kind =&gt; "fruit";
    /// }
    ///
    /// public class FruitSystem : EntitySystem&lt;Fruit&gt;
    /// {
    ///     public override string Kind =&gt; "fruit";
    /// }
    ///
    /// // From a phase: a fruit falls at p and rots in 50 ticks.
    /// World.Service&lt;FruitSystem&gt;().Add(new Fruit { Position = p, LifetimeLeft = 50 });
    /// </code>
    /// </example>
    public abstract class EntitySystem<T> : EntityKind where T : Entity
    {
        /// <summary>The living entities, in creation (id) order; derived systems read it, and change it only through the base methods.</summary>
        protected readonly List<T> entities = new List<T>();

        /// <summary>Living entities, in creation (id) order.</summary>
        public IReadOnlyList<T> Entities => entities;
        /// <inheritdoc/>
        public override int EntityCount => entities.Count;
        /// <inheritdoc/>
        public override Entity EntityAt(int i) => entities[i];

        /// <summary>Removes every entity: a new run starts empty.</summary>
        /// <inheritdoc/>
        public override void Initialize() => entities.Clear();

        /// <summary>Adds an entity: it gets an id and its creation tick.</summary>
        /// <param name="e">The new entity, its position and lifetime set.</param>
        /// <returns>The same entity, for chaining.</returns>
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
        /// <remarks>Used-up entities are skipped. Distances are the world's (<see cref="World.Distance"/>).</remarks>
        /// <param name="p">The point searched from, usually the viewer's position.</param>
        /// <param name="radius">The largest distance, in meters (inclusive).</param>
        /// <param name="viewer">The animal that looks, passed to the filter; may be null.</param>
        /// <param name="filter">Keeps an entity when it returns true, e.g. <c>(c, a) =&gt; c.CanBeEatenBy(a)</c>; null keeps all.</param>
        /// <param name="found">The nearest entity, or null.</param>
        /// <param name="distance">Its distance, in meters, or positive infinity when none is found.</param>
        /// <returns>True when an entity was found within the radius.</returns>
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
        /// <remarks>
        /// An entity whose lifetime reaches 0 here is removed in the same call ("expired"), as is a used-up one ("used up").
        /// The others keep their order.
        /// </remarks>
        /// <inheritdoc/>
        public override void UpdateEntities(TickContext t)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                Move(e, t);
                if (e.LifetimeLeft > 0) e.LifetimeLeft--;
            }
            // Compact in place: survivors keep their creation order (RAND-05).
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
        /// <remarks>Called by <see cref="UpdateEntities"/> for every entity, used-up ones included, before lifetimes count down.</remarks>
        /// <param name="e">The entity to move; change its <see cref="Entity.Position"/>, keeping it on walkable ground.</param>
        /// <param name="t">This tick's context; draw random numbers only from its named streams (RAND-02).</param>
        protected virtual void Move(T e, TickContext t) { }

        /// <summary>Called when an entity is removed in the environment phase.</summary>
        /// <remarks>
        /// Record an event or recycle the object here (the carcass system pools them). Don't add or remove entities from
        /// here: the list is being compacted.
        /// </remarks>
        /// <param name="e">The removed entity.</param>
        /// <param name="reason"><c>"used up"</c> or <c>"expired"</c> (its lifetime ran out).</param>
        /// <param name="t">This tick's context.</param>
        protected virtual void OnRemoved(T e, string reason, TickContext t) { }

        /// <summary>Removes everything (tests).</summary>
        public void Clear() => entities.Clear();
    }
}
