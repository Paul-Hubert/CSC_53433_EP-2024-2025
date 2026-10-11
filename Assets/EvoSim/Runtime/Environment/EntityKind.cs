namespace EvoSim
{
    /// <summary>A kind of entity and its system (ENV-20): carcasses, eggs. Diets name it "&lt;kind&gt;:&lt;species&gt;".</summary>
    /// <remarks>
    /// <para>The non-generic face of <see cref="EntitySystem{T}"/>: the environment phase, the views and the diets use it
    /// without knowing the entity type. Derive from <see cref="EntitySystem{T}"/> rather than from this class.</para>
    /// </remarks>
    public abstract class EntityKind : WorldService
    {
        /// <summary>The kind's name in diet entries, e.g. "carcass" or "egg".</summary>
        public abstract string Kind { get; }

        /// <summary>The kind's edible component (SPEC-10), or null: then nothing eats it.</summary>
        public Edible Edible
        {
            get
            {
                var e = GetComponent<Edible>();
                return e != null && e.enabled ? e : null;
            }
        }

        /// <summary>How many entities the kind holds now, used-up ones not yet removed included.</summary>
        public abstract int EntityCount { get; }

        /// <summary>The i-th living entity, in creation order (views read them; 0 ≤ i &lt; EntityCount), or null if the kind keeps none.</summary>
        /// <param name="i">An index from 0 to <see cref="EntityCount"/> - 1.</param>
        /// <returns>The entity, or null when the kind keeps no list.</returns>
        public virtual Entity EntityAt(int i) => null;

        /// <summary>The environment phase: entities move, rot, and the used-up ones are removed (ENV-21, ENV-22).</summary>
        /// <remarks>Called by <see cref="EnvironmentPhase"/> once per tick, in the order of the World's services.</remarks>
        /// <param name="t">This tick's context: the tick number and the named random streams.</param>
        public abstract void UpdateEntities(TickContext t);
    }
}
