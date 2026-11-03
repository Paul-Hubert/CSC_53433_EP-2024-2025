using System;
using System.Collections.Generic;

namespace EvoSim.Neuro
{
    /// <summary>
    /// One reusable item per animal per tick (plan M16 §9 M16.8, 20 §9 "no allocation per animal per tick"): the neural
    /// senses keep their fans and attachments here instead of making new arrays at every read.
    /// </summary>
    /// <remarks>
    /// <para>At the first request of a new tick, every item of the previous tick goes back to the free list; an animal's
    /// item stays the same for the rest of the tick, so a sense read twice in one tick (the token, then the attachment)
    /// can reuse its work. An item handed out in one tick may be handed to another animal in the next: the only reader of
    /// the attachments, <see cref="NeuralBrain"/>, copies them on the main thread before the tick ends (D4).</para>
    /// <para>Owned by one sense on one world; not thread-safe (the senses run on the main thread).</para>
    /// </remarks>
    /// <typeparam name="T">The pooled item.</typeparam>
    public sealed class TickPool<T> where T : class
    {
        readonly Func<T> make;
        readonly Dictionary<int, T> live = new Dictionary<int, T>();     // this tick's items by animal id
        readonly List<T> free = new List<T>();                           // items from earlier ticks
        int tick = int.MinValue;

        /// <summary>A pool that makes its items with the given function.</summary>
        /// <param name="make">Makes a new item when the free list is empty.</param>
        public TickPool(Func<T> make) { this.make = make ?? throw new ArgumentNullException(nameof(make)); }

        /// <summary>The items made so far (tests: it stops growing once the population is steady).</summary>
        public int Made { get; private set; }

        /// <summary>The animal's item for this tick.</summary>
        /// <param name="now">The world's tick.</param>
        /// <param name="animalId">The animal's id.</param>
        /// <param name="item">The item.</param>
        /// <returns>True when the item is new to the animal this tick (its contents are stale); false when the animal already
        /// had it this tick.</returns>
        public bool Get(int now, int animalId, out T item)
        {
            if (now != tick)
            {
                foreach (var old in live.Values) free.Add(old);
                live.Clear();
                tick = now;
            }
            if (live.TryGetValue(animalId, out item)) return false;
            if (free.Count > 0)
            {
                item = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
            }
            else
            {
                item = make();
                Made++;
            }
            live[animalId] = item;
            return true;
        }

        /// <summary>Drops every item (the items' shape changed, or the world is starting again).</summary>
        public void Clear()
        {
            live.Clear();
            free.Clear();
            tick = int.MinValue;
        }
    }
}
