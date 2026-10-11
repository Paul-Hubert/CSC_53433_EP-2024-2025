using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Cumulative counts of one species for the statistics rows and the summary (13 §4, §5).</summary>
    /// <remarks>Every count runs from the start of the run (<see cref="Species.Counters"/>, new at each set-up). The
    /// phases and modules that do the thing add to them; the run recorder only reads them.</remarks>
    public sealed class SpeciesCounters
    {
        /// <summary>
        /// Animals that joined the species: founders at tick 0, births (babies), immigrants (newcomers below the floor),
        /// eggs hatched; eggs laid, and eggs lost (eaten or otherwise gone before hatching).
        /// </summary>
        public int Founders, Births, Immigrants, Hatched, EggsLaid, EggsLost;
        /// <summary>
        /// Decisions taken; queries sent to a brain; decisions answered by the memo; choices that found nothing to act
        /// on (searching, ACT-04, the prototype's "invalid"); decisions whose brain failed (a uniform row was used, DEC-40).
        /// </summary>
        public int Decisions, BrainQueries, MemoHits, Invalid, Failures;
        /// <summary>Successful strikes by its hunters; carcass portions eaten; meals of every kind.</summary>
        public int Kills, Portions, Meals;
        /// <summary>Animal-ticks that began without stamina for any movement (13 §4).</summary>
        public long Exhausted;
        /// <summary>Animal-ticks lived (for shares).</summary>
        public long AnimalTicks;
        /// <summary>The brain's time spent on this species' queries, in milliseconds (each query's share of its batch).</summary>
        public double BrainMilliseconds;
        /// <summary>Sum of ages at death, for the mean lifespan.</summary>
        public long LifespanSum;
        /// <summary>The highest generation hatched so far (0 while only founders and newcomers lived).</summary>
        public int MaxGeneration;
        /// <summary>Decisions per action, in action order.</summary>
        public int[] ActionCounts = Array.Empty<int>();
        /// <summary>Deaths by cause, in cause order (sorted).</summary>
        public readonly SortedDictionary<string, int> Deaths = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Deaths of every cause.</summary>
        public int DeathsTotal
        {
            get
            {
                int n = 0;
                foreach (var kv in Deaths) n += kv.Value;
                return n;
            }
        }

        /// <summary>Deaths of one cause.</summary>
        /// <param name="cause">The cause, e.g. <c>"starvation"</c>.</param>
        /// <returns>How many animals died of it; 0 for a cause never seen.</returns>
        public int DeathsBy(string cause) => Deaths.TryGetValue(cause, out var n) ? n : 0;

        /// <summary>Counts one death; <see cref="World.RecordDeath"/> calls it.</summary>
        /// <param name="cause">The cause of death.</param>
        /// <param name="age">The animal's age at death, in ticks.</param>
        public void CountDeath(string cause, int age)
        {
            Deaths[cause] = DeathsBy(cause) + 1;
            LifespanSum += age;
        }

        /// <summary>Gives <see cref="ActionCounts"/> one slot per action, keeping the counts of the actions it already had.</summary>
        internal void Resize(int actionCount)
        {
            if (ActionCounts.Length != actionCount) Array.Resize(ref ActionCounts, actionCount);
        }
    }
}
