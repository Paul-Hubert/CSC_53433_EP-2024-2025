using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>A cause of death checked in the death phase (ANIM-40): return the cause's name, or null to live on.</summary>
    public abstract class DeathRule : SpeciesModule
    {
        /// <summary>The causes this rule can give, for the statistics columns (deaths_&lt;cause&gt;).</summary>
        public virtual IEnumerable<string> Causes => System.Array.Empty<string>();

        /// <summary>The cause of death for this animal now, or null.</summary>
        public abstract string CauseOfDeath(Animal a);

        /// <summary>The batch entry point (20 §10): writes each animal's cause, or null, into causes.</summary>
        public virtual void CheckAll(IReadOnlyList<Animal> animals, string[] causes)
        {
            for (int i = 0; i < animals.Count; i++)
                if (causes[i] == null) causes[i] = CauseOfDeath(animals[i]);
        }
    }
}
