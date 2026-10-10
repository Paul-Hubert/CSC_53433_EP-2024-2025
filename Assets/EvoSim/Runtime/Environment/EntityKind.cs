namespace EvoSim
{
    /// <summary>A kind of entity and its system (ENV-20): carcasses, eggs. Diets name it "&lt;kind&gt;:&lt;species&gt;".</summary>
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

        public abstract int EntityCount { get; }

        /// <summary>The i-th living entity, in creation order (views read them; 0 ≤ i &lt; EntityCount), or null if the kind keeps none.</summary>
        public virtual Entity EntityAt(int i) => null;

        /// <summary>The environment phase: entities move, rot, and the used-up ones are removed (ENV-21, ENV-22).</summary>
        public abstract void UpdateEntities(TickContext t);
    }
}
