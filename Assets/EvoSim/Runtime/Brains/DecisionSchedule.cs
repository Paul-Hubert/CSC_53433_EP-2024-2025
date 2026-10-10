using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A species' own decision period, and staggering (DEC-04, MAY): an animal decides when (tick + id) % period == 0.
    /// Both change behaviour; both are off in the reference (no schedule module: the World's period, all at once).
    /// </summary>
    public class DecisionSchedule : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Ticks between decisions for this species; 0 = the World's period.")]
        int period;
        [SerializeField, Tooltip("Spread decisions over the period by animal id.")]
        bool staggered;

        public int Period(World w) => period > 0 ? period : w.DecisionPeriod;

        /// <summary>Whether this animal's periodic decision falls on this tick.</summary>
        public bool IsDue(Animal a, int tick, World w)
        {
            int p = Period(w);
            return staggered ? (tick + a.Id) % p == 0 : tick % p == 0;
        }

        public void Configure(int ticks, bool spread)
        {
            period = ticks;
            staggered = spread;
        }
    }
}
