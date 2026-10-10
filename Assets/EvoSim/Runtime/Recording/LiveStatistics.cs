using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Time series for the editor's ecology monitor (21 §4, OUT-06): population, mean energy and stamina, births,
    /// deaths and memo hit rate per species, sampled every few ticks into fixed-size buffers.
    /// </summary>
    public class LiveStatistics : WorldService
    {
        [SerializeField, Min(1), Tooltip("A sample every this many ticks.")]
        int every = 10;
        [SerializeField, Min(10), Tooltip("Samples kept per series (older ones are dropped).")]
        int capacity = 2000;

        /// <summary>One sample of one species.</summary>
        public struct Point
        {
            public int Tick, Population, Births, Deaths, Decisions, MemoHits;
            public float MeanEnergy, MeanStamina;
        }

        readonly Dictionary<string, List<Point>> series = new Dictionary<string, List<Point>>();
        readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        int lastTick;

        /// <summary>Ticks per second over the last sampling interval.</summary>
        public float TicksPerSecond { get; private set; }

        public override void Initialize()
        {
            series.Clear();
            clock.Restart();
            lastTick = 0;
        }

        /// <summary>The samples of a species, oldest first.</summary>
        public IReadOnlyList<Point> Of(string speciesId) => series.TryGetValue(speciesId, out var l) ? l : (IReadOnlyList<Point>)new List<Point>();

        /// <summary>Takes a sample if the tick is on the interval (the record phase calls it every tick).</summary>
        public void Sample(int tick)
        {
            if (tick % every != 0) return;
            double seconds = clock.Elapsed.TotalSeconds;
            if (seconds > 0) TicksPerSecond = (float)((tick - lastTick) / seconds);
            clock.Restart();
            lastTick = tick;
            foreach (var s in World.AllSpecies)
            {
                if (!series.TryGetValue(s.Id, out var list)) series[s.Id] = list = new List<Point>();
                var energy = s.Declarations.FindStat("energy");
                var stamina = s.Declarations.FindStat("stamina");
                float e = 0, st = 0;
                int n = 0;
                foreach (var a in s.Animals)
                {
                    if (a.IsGone) continue;
                    n++;
                    if (energy.IsValid) e += a[energy];
                    if (stamina.IsValid) st += a[stamina];
                }
                list.Add(new Point
                {
                    Tick = tick, Population = n, MeanEnergy = n > 0 ? e / n : 0, MeanStamina = n > 0 ? st / n : 0,
                    Births = s.Counters.Births, Deaths = s.Counters.DeathsTotal, Decisions = s.Counters.Decisions, MemoHits = s.Counters.MemoHits,
                });
                if (list.Count > capacity) list.RemoveAt(0);
            }
        }
    }
}
