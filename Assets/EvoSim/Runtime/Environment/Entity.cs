using UnityEngine;

namespace EvoSim
{
    /// <summary>A world object with a position that is not an animal: a carcass, an egg, a water hole (ENV-20).</summary>
    /// <remarks>
    /// <para>Derive from it for a kind of your own (fruit, nests…), and keep the entities in an
    /// <see cref="EntitySystem{T}"/>, which gives them their id and creation tick and removes them when they are used up
    /// or their lifetime ends. Change an entity only from a phase or a module, never from the visuals (ENV-21).</para>
    /// </remarks>
    public abstract class Entity
    {
        /// <summary>Unique in the run, increasing (its own counter, separate from animal ids).</summary>
        public int Id { get; internal set; } = -1;
        /// <summary>The tick at which its system added it.</summary>
        public int CreatedTick { get; internal set; }
        /// <summary>Its position in the world, in meters; only phases move it (ENV-21).</summary>
        public Vector3 Position;
        /// <summary>Ticks left before it disappears; -1 = no lifetime (ENV-20).</summary>
        public int LifetimeLeft = -1;
        /// <summary>Used up: removed in the environment phase of this tick (ENV-22).</summary>
        public bool UsedUp;
        /// <summary>The entity kind's name, e.g. "carcass".</summary>
        public abstract string Kind { get; }
    }
}
