using UnityEngine;

namespace EvoSim
{
    /// <summary>A world object with a position that is not an animal: a carcass, an egg, a water hole (ENV-20).</summary>
    public abstract class Entity
    {
        /// <summary>Unique in the run, increasing (its own counter, separate from animal ids).</summary>
        public int Id { get; internal set; } = -1;
        public int CreatedTick { get; internal set; }
        public Vector3 Position;
        /// <summary>Ticks left before it disappears; -1 = no lifetime (ENV-20).</summary>
        public int LifetimeLeft = -1;
        /// <summary>Used up: removed in the environment phase of this tick (ENV-22).</summary>
        public bool UsedUp;
        /// <summary>The entity kind's name, e.g. "carcass".</summary>
        public abstract string Kind { get; }
    }
}
