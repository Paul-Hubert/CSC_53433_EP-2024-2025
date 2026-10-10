using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Cumulative counts of one species for the statistics rows and the summary (13 §4, §5).</summary>
    public sealed class SpeciesCounters
    {
        public int Founders, Births, Immigrants, Hatched, EggsLaid, EggsLost;
        public int Decisions, BrainQueries, MemoHits, Invalid, Failures;
        public int Kills, Portions, Meals;
        /// <summary>Animal-ticks that began without stamina for any movement (13 §4).</summary>
        public long Exhausted;
        /// <summary>Animal-ticks lived (for shares).</summary>
        public long AnimalTicks;
        public double BrainMilliseconds;
        /// <summary>Sum of ages at death, for the mean lifespan.</summary>
        public long LifespanSum;
        public int MaxGeneration;
        /// <summary>Decisions per action, in action order.</summary>
        public int[] ActionCounts = Array.Empty<int>();
        /// <summary>Deaths by cause, in cause order (sorted).</summary>
        public readonly SortedDictionary<string, int> Deaths = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public int DeathsTotal
        {
            get
            {
                int n = 0;
                foreach (var kv in Deaths) n += kv.Value;
                return n;
            }
        }

        public int DeathsBy(string cause) => Deaths.TryGetValue(cause, out var n) ? n : 0;

        public void CountDeath(string cause, int age)
        {
            Deaths[cause] = DeathsBy(cause) + 1;
            LifespanSum += age;
        }

        internal void Resize(int actionCount)
        {
            if (ActionCounts.Length != actionCount) Array.Resize(ref ActionCounts, actionCount);
        }
    }
}
