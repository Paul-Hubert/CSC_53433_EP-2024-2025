using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A species' own decision period, and staggering (DEC-04, MAY): an animal decides when (tick + id) % period == 0.
    /// Both change behaviour. In the reference scenes every species has one, staggered, with the World's period (owner
    /// 2026-10-10); a species without one decides all together, every World period, as the Python prototype does.
    /// </summary>
    /// <remarks>
    /// <para>Place it under a species, at most one: the sense phase asks <see cref="IsDue"/> for each living, not busy animal.
    /// Whatever the schedule says, a busy animal never decides (DEC-02) and an animal without an action decides at once
    /// (DEC-01).</para>
    /// <para>Staggering spreads the brain's load: with a period of 4, about a quarter of the animals decide on each tick
    /// instead of all of them every fourth tick.</para>
    /// </remarks>
    public class DecisionSchedule : SpeciesModule
    {
        [SerializeField, Min(0), Tooltip("Ticks between decisions for this species; 0 = the World's period.")]
        int period;
        [SerializeField, Tooltip("Spread decisions over the period by animal id.")]
        bool staggered;

        /// <summary>Gives the ticks between two decisions of this species.</summary>
        /// <param name="w">The world, for its decision period.</param>
        /// <returns>This module's period, or the World's (reference 4) when it is 0.</returns>
        public int Period(World w) => period > 0 ? period : w.DecisionPeriod;

        /// <summary>Spread decisions over the period by animal id (DEC-04).</summary>
        public bool Staggered { get => staggered; set => staggered = value; }

        /// <summary>Tells whether this animal's periodic decision falls on this tick.</summary>
        /// <param name="a">A living, not busy animal of this species.</param>
        /// <param name="tick">The current tick.</param>
        /// <param name="w">The world.</param>
        /// <returns>True on the animal's decision ticks: (tick + id) % period == 0 staggered, tick % period == 0 otherwise.</returns>
        /// <remarks>Called by the sense phase at the start of every tick. An override must depend only on the tick and the
        /// animal's state, and draw no random number, so a seed still repeats a run.</remarks>
        public virtual bool IsDue(Animal a, int tick, World w)
        {
            int p = Period(w);
            return staggered ? (tick + a.Id) % p == 0 : tick % p == 0;
        }

        /// <summary>Sets the schedule from code (reference scenes, tests), as the Inspector fields do.</summary>
        /// <param name="ticks">Ticks between decisions; 0 for the World's period.</param>
        /// <param name="spread">True to stagger decisions by animal id.</param>
        public void Configure(int ticks, bool spread)
        {
            period = ticks;
            staggered = spread;
        }
    }
}
