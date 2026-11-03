using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The "long enough" stop rule of the measured runs (plan M16 §6.3): a phase that closes a window every
    /// <see cref="WindowTicks"/> ticks and asks for a clean stop (RAND-20) at the first window boundary where every
    /// tracked species has a mean generation of at least <see cref="MinGenerations"/> and has reached a plateau.
    /// </summary>
    /// <remarks>
    /// <para>A plateau (<see cref="Plateau"/>): over the last <see cref="PlateauWindows"/> windows, each measure (mean
    /// population, starvation hazard, predation hazard, births per animal) stays within ±<see cref="Tolerance"/> of its
    /// mean over those windows. A measure that is 0 in all of them counts as flat. The hard cap stops a run that never
    /// settles, with the reason <c>plateau cap</c>.</para>
    /// <para>Place it among the World's phases, after Record, so a stop at a window boundary comes after that tick's row
    /// (RAND-20). Tracked species: the ids listed, or every species with a <see cref="NeuralGenomeGene"/>. With
    /// <see cref="StopsRun"/> off it only records the tick (<see cref="PlateauTick"/>, in <c>survival.json</c>): the second
    /// seed of an arm then runs to the same length as the first (§6.3). It only reads the world.</para>
    /// </remarks>
    public class PlateauStop : TickPhase
    {
        [SerializeField, Min(1), Tooltip("Window length in ticks (plan: 2 000).")]
        int windowTicks = 2000;
        [SerializeField, Min(0), Tooltip("Least mean generation of every tracked species in the last window (plan: 50).")]
        int minGenerations = 50;
        [SerializeField, Range(0f, 1f), Tooltip("Largest relative spread of a measure around its mean over the checked windows (plan: 0.1).")]
        float tolerance = 0.1f;
        [SerializeField, Min(1), Tooltip("Windows a plateau must last (plan: 3).")]
        int plateauWindows = 3;
        [SerializeField, Min(1), Tooltip("Hard cap in ticks for a run that never settles (plan: 60 000).")]
        int capTicks = 60000;
        [SerializeField, Tooltip("On: stop the run at the plateau. Off: only record its tick (a seed run to a set length).")]
        bool stopsRun = true;
        [SerializeField, Tooltip("Species to track, by id; empty = every species with a NeuralGenomeGene.")]
        List<string> species = new List<string>();

        readonly List<Species> tracked = new List<Species>();
        readonly Dictionary<Species, List<PlateauWindow>> windows = new Dictionary<Species, List<PlateauWindow>>();
        readonly Dictionary<Species, Tally> tallies = new Dictionary<Species, Tally>();

        /// <summary>One species' running sums since the last window boundary.</summary>
        sealed class Tally { public long PopTicks; public double GenTicks; public int Starved, Killed, Births; }

        /// <summary>The window length in ticks.</summary>
        public int WindowTicks => windowTicks;
        /// <summary>The least mean generation.</summary>
        public int MinGenerations => minGenerations;
        /// <summary>The largest relative spread.</summary>
        public float Tolerance => tolerance;
        /// <summary>The windows a plateau must last.</summary>
        public int PlateauWindows => plateauWindows;
        /// <summary>The hard cap in ticks.</summary>
        public int CapTicks => capTicks;
        /// <summary>Whether it stops the run (else it only records the tick).</summary>
        public bool StopsRun => stopsRun;
        /// <summary>The tick the rule was first met, or −1.</summary>
        public int PlateauTick { get; private set; } = -1;
        /// <summary>The closed windows of a tracked species.</summary>
        /// <param name="s">A tracked species.</param>
        /// <returns>Its windows, oldest first (empty when untracked).</returns>
        public IReadOnlyList<PlateauWindow> WindowsOf(Species s) => windows.TryGetValue(s, out var w) ? w : (IReadOnlyList<PlateauWindow>)Array.Empty<PlateauWindow>();

        /// <summary>Sets the rule from code (scenarios, tests), as the Inspector does.</summary>
        /// <param name="window">Window length.</param>
        /// <param name="generations">Least mean generation.</param>
        /// <param name="spread">Relative tolerance.</param>
        /// <param name="count">Windows a plateau lasts.</param>
        /// <param name="cap">Hard cap.</param>
        /// <param name="stop">True to stop the run.</param>
        /// <returns>This phase.</returns>
        public PlateauStop Configure(int window, int generations, float spread = 0.1f, int count = 3, int cap = 60000, bool stop = true)
        {
            windowTicks = Mathf.Max(1, window);
            minGenerations = Mathf.Max(0, generations);
            tolerance = Mathf.Clamp01(spread);
            plateauWindows = Mathf.Max(1, count);
            capTicks = Mathf.Max(1, cap);
            stopsRun = stop;
            return this;
        }

        /// <summary>Finds the tracked species and clears the windows.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            tracked.Clear(); windows.Clear(); tallies.Clear();
            PlateauTick = -1;
            foreach (var s in World.AllSpecies)
                if (species.Count > 0 ? species.Contains(s.Id) : s.Module<NeuralGenomeGene>() != null)
                {
                    tracked.Add(s);
                    windows[s] = new List<PlateauWindow>();
                    tallies[s] = Snapshot(s, new Tally());
                }
        }

        /// <summary>Adds this tick to the sums; at a window boundary, closes the window and checks the rule.</summary>
        /// <param name="t">The tick.</param>
        public override void Run(TickContext t)
        {
            int ticksDone = t.Tick + 1;
            foreach (var s in tracked)
            {
                var tally = tallies[s];
                var animals = s.Animals;
                tally.PopTicks += animals.Count;
                for (int i = 0; i < animals.Count; i++) tally.GenTicks += animals[i].Generation;
            }
            if (ticksDone % windowTicks == 0 && tracked.Count > 0)
            {
                foreach (var s in tracked) windows[s].Add(Close(s));
                if (PlateauTick < 0 && Met())
                {
                    PlateauTick = ticksDone;
                    if (stopsRun) World.RequestStop("plateau");
                }
            }
            if (stopsRun && ticksDone >= capTicks && PlateauTick < 0) World.RequestStop("plateau cap");
        }

        /// <summary>True when every tracked species meets the rule on its windows so far.</summary>
        bool Met()
        {
            foreach (var s in tracked) if (!Plateau(windows[s], minGenerations, tolerance, plateauWindows)) return false;
            return true;
        }

        /// <summary>
        /// The rule on one species' windows: at least <paramref name="count"/> windows, the last one's mean generation at
        /// least <paramref name="minGenerations"/>, and each measure within ±<paramref name="spread"/> of its mean over the
        /// last <paramref name="count"/> windows.
        /// </summary>
        /// <param name="list">The species' windows, oldest first.</param>
        /// <param name="minGenerations">The least mean generation.</param>
        /// <param name="spread">The relative tolerance.</param>
        /// <param name="count">The windows checked.</param>
        /// <returns>True at a plateau.</returns>
        public static bool Plateau(IReadOnlyList<PlateauWindow> list, int minGenerations, float spread, int count)
        {
            if (list.Count < count || list[list.Count - 1].MeanGeneration < minGenerations) return false;
            return Flat(list, count, w => w.MeanPopulation, spread) && Flat(list, count, w => w.Starvation, spread) &&
                   Flat(list, count, w => w.Predation, spread) && Flat(list, count, w => w.BirthsPerAnimal, spread);
        }

        /// <summary>Whether a measure of the last windows stays within ±spread of its mean (all zero counts as flat).</summary>
        static bool Flat(IReadOnlyList<PlateauWindow> list, int count, Func<PlateauWindow, double> measure, float spread)
        {
            double mean = 0;
            for (int i = list.Count - count; i < list.Count; i++) mean += measure(list[i]);
            mean /= count;
            for (int i = list.Count - count; i < list.Count; i++)
                if (Math.Abs(measure(list[i]) - mean) > spread * Math.Abs(mean) + 1e-12) return false;
            return true;
        }

        /// <summary>The window just ended for one species, and a new tally from now.</summary>
        PlateauWindow Close(Species s)
        {
            var old = tallies[s];
            var now = Snapshot(s, new Tally());
            double pop = (double)old.PopTicks / windowTicks;
            var w = new PlateauWindow(s.Id, pop, old.PopTicks > 0 ? old.GenTicks / old.PopTicks : 0,
                old.PopTicks > 0 ? 1000.0 * (now.Starved - old.Starved) / old.PopTicks : 0,
                old.PopTicks > 0 ? 1000.0 * (now.Killed - old.Killed) / old.PopTicks : 0,
                pop > 0 ? (now.Births - old.Births) / pop : 0);
            tallies[s] = now;
            return w;
        }

        /// <summary>A tally with the species' cumulative counters now and empty sums.</summary>
        static Tally Snapshot(Species s, Tally t)
        {
            t.Starved = s.Counters.DeathsBy("starvation");
            t.Killed = s.Counters.DeathsBy("killed");
            t.Births = s.Counters.Births;
            return t;
        }
    }
}
