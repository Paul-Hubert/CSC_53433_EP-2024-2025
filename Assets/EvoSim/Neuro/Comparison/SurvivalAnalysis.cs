using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The survival analysis of one run (plan M16 §6.2), from the events every run writes: each animal's life rebuilt from
    /// its arrival (founder, birth, newcomer) to its death with cause, age and killer, or its censoring (migration, the run's
    /// end). Pure C#: Kaplan–Meier curves with Greenwood bands, death-rate ratios, per-window measures and the kill matrix.
    /// </summary>
    /// <remarks>
    /// <para>Build it from a run folder (<see cref="FromFolder"/>: <c>events.jsonl</c>, <c>summary.json</c>, and
    /// <c>survival.json</c> when <see cref="SurvivalStats"/> wrote one) or from event lines (<see cref="FromEvents"/>).
    /// Ages are in ticks; founders start at age 0.</para>
    /// <para>A death at tick t ends the animal's time at risk at t: an animal that comes at tick e and leaves at x was alive
    /// for x − e animal-ticks.</para>
    /// </remarks>
    public sealed class SurvivalAnalysis
    {
        /// <summary>The cause the core gives a migration: censoring, not death.</summary>
        public const string Migrated = "migrated";

        readonly List<AnimalLife> lives = new List<AnimalLife>();
        readonly List<string> species = new List<string>();

        /// <summary>Every animal's life, in arrival order.</summary>
        public IReadOnlyList<AnimalLife> Lives => lives;
        /// <summary>The species ids, in order of first appearance.</summary>
        public IReadOnlyList<string> Species => species;
        /// <summary>The run's last tick: animals alive then are censored at it.</summary>
        public int EndTick { get; private set; }
        /// <summary>The run's seed (from <c>summary.json</c>), or 0.</summary>
        public long Seed { get; set; }
        /// <summary>Why the run stopped, or empty.</summary>
        public string StopReason { get; set; } = "";
        /// <summary>The scenario (from <c>run_info.json</c>), or empty.</summary>
        public string Scenario { get; set; } = "";
        /// <summary>The variant (from <c>run_info.json</c>), or empty.</summary>
        public string Variant { get; set; } = "";
        /// <summary>The run folder it was read from, or empty.</summary>
        public string Folder { get; set; } = "";
        /// <summary>The matched pairs (JEV id, neural id), from <c>survival.json</c>.</summary>
        public List<(string jev, string neural)> Pairs { get; } = new List<(string, string)>();
        /// <summary>Each species' maturity in ticks, from <c>survival.json</c> (for juvenile survival).</summary>
        public Dictionary<string, int> Maturity { get; } = new Dictionary<string, int>();
        /// <summary>Each species' cap, from <c>survival.json</c> (for the share of ticks at the cap).</summary>
        public Dictionary<string, int> Caps { get; } = new Dictionary<string, int>();
        /// <summary>The tick the plateau rule was met (<see cref="PlateauStop"/>), or −1.</summary>
        public int PlateauTick { get; set; } = -1;

        // ---- Building ----

        /// <summary>Reads a run folder: its events, its summary, and its run info and survival file when present.</summary>
        /// <param name="folder">A run folder (<c>Logs/EvoSim/runs/…</c>).</param>
        /// <returns>The analysis.</returns>
        public static SurvivalAnalysis FromFolder(string folder)
        {
            var summary = JObject.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
            int end = (int?)summary["ticks"] ?? 0;
            var a = FromEvents(File.ReadLines(Path.Combine(folder, "events.jsonl")), end);
            a.Folder = folder;
            a.Seed = (long?)summary["seed"] ?? 0;
            a.StopReason = (string)summary["stopped"] ?? "";
            string info = Path.Combine(folder, "run_info.json");
            if (File.Exists(info))
            {
                var j = JObject.Parse(File.ReadAllText(info));
                a.Scenario = (string)j["scenario"] ?? "";
                a.Variant = (string)j["variant"] ?? "";
            }
            string survival = Path.Combine(folder, "survival.json");
            if (File.Exists(survival)) a.ReadSurvival(JObject.Parse(File.ReadAllText(survival)));
            return a;
        }

        /// <summary>Rebuilds the lives from event lines (one JSON object each, as <c>events.jsonl</c>).</summary>
        /// <param name="lines">The run's events, in order.</param>
        /// <param name="endTick">The run's last tick: the living are censored there.</param>
        /// <returns>The analysis.</returns>
        public static SurvivalAnalysis FromEvents(IEnumerable<string> lines, int endTick)
        {
            var a = new SurvivalAnalysis { EndTick = endTick };
            var byId = new Dictionary<int, AnimalLife>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var e = JObject.Parse(line);
                string kind = (string)e["kind"];
                int t = (int?)e["t"] ?? 0;
                if (kind == "founder" || kind == "birth" || kind == "immigrant")
                {
                    var life = new AnimalLife
                    {
                        Id = (int)e["id"], Species = (string)e["species"], Origin = kind, Entry = t, Exit = endTick,
                        Generation = (int?)e["gen"] ?? 0,
                    };
                    byId[life.Id] = life;
                    a.lives.Add(life);
                    if (!a.species.Contains(life.Species)) a.species.Add(life.Species);
                    if (e["parents"] is JArray parents)
                        foreach (var p in parents)
                            if (byId.TryGetValue((int)p, out var parent)) parent.Offspring++;
                }
                else if (kind == "death" && byId.TryGetValue((int)e["id"], out var dead))
                {
                    dead.Exit = t;
                    dead.Cause = (string)e["cause"];
                    dead.Age = (int?)e["age"] ?? t - dead.Entry;
                    if (e["killer"] != null && byId.TryGetValue((int)e["killer"], out var killer)) dead.KillerSpecies = killer.Species;
                }
            }
            foreach (var l in a.lives) if (l.Cause == null) l.Age = l.Exit - l.Entry;
            return a;
        }

        /// <summary>Reads the pairs, maturities, caps and plateau tick of a <c>survival.json</c>.</summary>
        void ReadSurvival(JObject j)
        {
            if (j["pairs"] is JArray pairs)
                foreach (var p in pairs) Pairs.Add(((string)p[0], (string)p[1]));
            if (j["species"] is JObject sp)
                foreach (var kv in sp)
                {
                    if (kv.Value["maturity"] != null) Maturity[kv.Key] = (int)kv.Value["maturity"];
                    if (kv.Value["cap"] != null) Caps[kv.Key] = (int)kv.Value["cap"];
                }
            PlateauTick = (int?)j["plateau_tick"] ?? -1;
        }

        // ---- Statistics ----

        /// <summary>
        /// The Kaplan–Meier estimate over ages (deaths and censored times), with Greenwood's variance: one step per age
        /// with at least one death. At equal ages deaths come before censoring.
        /// </summary>
        /// <param name="times">Each animal's age at exit and whether it died there (false: censored).</param>
        /// <returns>The steps, by increasing age.</returns>
        public static List<KaplanMeierStep> KaplanMeier(IEnumerable<(int time, bool dead)> times)
        {
            var sorted = new List<(int time, bool dead)>(times);
            sorted.Sort((x, y) => x.time != y.time ? x.time.CompareTo(y.time) : y.dead.CompareTo(x.dead));
            var steps = new List<KaplanMeierStep>();
            int atRisk = sorted.Count;
            double s = 1.0, sum = 0.0;
            for (int i = 0; i < sorted.Count;)
            {
                int t = sorted[i].time, d = 0, c = 0;
                while (i < sorted.Count && sorted[i].time == t) { if (sorted[i].dead) d++; else c++; i++; }
                if (d > 0)
                {
                    s *= 1.0 - (double)d / atRisk;
                    if (atRisk > d) sum += (double)d / ((double)atRisk * (atRisk - d));
                    steps.Add(new KaplanMeierStep(t, atRisk, d, s, s * s * sum));
                }
                atRisk -= d + c;
            }
            return steps;
        }

        /// <summary>A species' Kaplan–Meier curve over age: deaths of any cause but migration; the rest censored.</summary>
        /// <param name="speciesId">The species.</param>
        /// <returns>The steps.</returns>
        public List<KaplanMeierStep> Curve(string speciesId)
        {
            var times = new List<(int, bool)>();
            foreach (var l in lives) if (l.Species == speciesId) times.Add((l.Age, l.Dead));
            return KaplanMeier(times);
        }

        /// <summary>The median lifespan: the first age where S(t) ≤ 0.5; −1 when the curve stays above.</summary>
        /// <param name="speciesId">The species.</param>
        /// <returns>The age in ticks, or −1.</returns>
        public int MedianLifespan(string speciesId)
        {
            foreach (var step in Curve(speciesId)) if (step.Survival <= 0.5) return step.Time;
            return -1;
        }

        /// <summary>One species' measures over the window [from, to).</summary>
        /// <param name="speciesId">The species.</param>
        /// <param name="from">The first tick.</param>
        /// <param name="to">The tick after the last.</param>
        /// <returns>The window.</returns>
        public SurvivalWindow Window(string speciesId, int from, int to)
        {
            var w = new SurvivalWindow { Species = speciesId, From = from, To = to };
            double genTicks = 0;
            foreach (var l in lives)
            {
                if (l.KillerSpecies == speciesId && l.Cause != null && l.Exit >= from && l.Exit < to)
                    w.Kills[l.Species] = (w.Kills.TryGetValue(l.Species, out var k) ? k : 0) + 1;
                if (l.Species != speciesId) continue;
                long overlap = Math.Max(0, Math.Min(l.Exit, to) - Math.Max(l.Entry, from));
                w.AnimalTicks += overlap;
                genTicks += (double)overlap * l.Generation;
                if (l.Entry >= from && l.Entry < to)
                {
                    if (l.Origin == "birth") w.Births++;
                    else if (l.Origin == "immigrant") w.Immigrants++;
                }
                if (l.Cause != null && l.Exit >= from && l.Exit < to)
                    w.Deaths[l.Cause] = (w.Deaths.TryGetValue(l.Cause, out var n) ? n : 0) + 1;
            }
            w.MeanGeneration = w.AnimalTicks > 0 ? genTicks / w.AnimalTicks : 0;
            return w;
        }

        /// <summary>The windows of one species: [0, size), [size, 2·size)… up to the end tick (the last may be shorter).</summary>
        /// <param name="speciesId">The species.</param>
        /// <param name="size">The window length in ticks (plan: 2 000).</param>
        /// <returns>The windows in time order.</returns>
        public List<SurvivalWindow> Windows(string speciesId, int size)
        {
            var list = new List<SurvivalWindow>();
            for (int from = 0; from < EndTick; from += size) list.Add(Window(speciesId, from, Math.Min(EndTick, from + size)));
            return list;
        }

        /// <summary>The late windows of §6.2: the last <paramref name="count"/> full windows before the end, merged into one.</summary>
        /// <param name="speciesId">The species.</param>
        /// <param name="size">The window length.</param>
        /// <param name="count">How many windows (plan: 3).</param>
        /// <returns>One window over their span (the whole run when it is shorter).</returns>
        public SurvivalWindow Late(string speciesId, int size, int count = 3)
        {
            int to = EndTick / size * size;
            if (to == 0) to = EndTick;
            return Window(speciesId, Math.Max(0, to - size * count), to);
        }

        /// <summary>The death-rate ratio of two species over a span: (deaths/animal-ticks) of a over b, all causes but migration.</summary>
        /// <param name="a">The first window (the neural species).</param>
        /// <param name="b">The second window (the JEV species).</param>
        /// <param name="cause">One cause, or null for all but migration.</param>
        /// <returns>The ratio with its interval.</returns>
        public static RateRatio Ratio(SurvivalWindow a, SurvivalWindow b, string cause = null) =>
            new RateRatio(a.DeathsBy(cause), a.AnimalTicks, b.DeathsBy(cause), b.AnimalTicks);

        /// <summary>The kill matrix over [from, to): kills by killer species (rows) and victim species (columns).</summary>
        /// <param name="from">The first tick.</param>
        /// <param name="to">The tick after the last.</param>
        /// <returns>Counts by (killer, victim).</returns>
        public SortedDictionary<(string killer, string victim), int> KillMatrix(int from = 0, int to = int.MaxValue)
        {
            var m = new SortedDictionary<(string, string), int>();
            foreach (var l in lives)
                if (l.KillerSpecies != null && l.Exit >= from && l.Exit < to)
                    m[(l.KillerSpecies, l.Species)] = (m.TryGetValue((l.KillerSpecies, l.Species), out var n) ? n : 0) + 1;
            return m;
        }

        /// <summary>The share of animals born that reached maturity, among those whose fate is known (dead, or adult).</summary>
        /// <param name="speciesId">The species.</param>
        /// <returns>The share, or NaN without a maturity or a known fate.</returns>
        public double JuvenileSurvival(string speciesId)
        {
            if (!Maturity.TryGetValue(speciesId, out int m)) return double.NaN;
            int known = 0, adult = 0;
            foreach (var l in lives)
            {
                if (l.Species != speciesId || l.Origin != "birth") continue;
                if (l.Age >= m) { known++; adult++; }
                else if (l.Dead) known++;
            }
            return known > 0 ? (double)adult / known : double.NaN;
        }

        /// <summary>Offspring per animal over its life, and the share that ever bred, among the animals that left (died or migrated).</summary>
        /// <param name="speciesId">The species.</param>
        /// <param name="shareThatBred">The share with at least one baby.</param>
        /// <returns>The mean offspring, or 0 without such animals.</returns>
        public double OffspringPerAnimal(string speciesId, out double shareThatBred)
        {
            int n = 0, bred = 0, babies = 0;
            foreach (var l in lives)
            {
                if (l.Species != speciesId || l.Cause == null) continue;
                n++;
                babies += l.Offspring;
                if (l.Offspring > 0) bred++;
            }
            shareThatBred = n > 0 ? (double)bred / n : 0;
            return n > 0 ? (double)babies / n : 0;
        }

        /// <summary>The population over the run: its minimum, maximum, and the share of ticks at or above the cap.</summary>
        /// <param name="speciesId">The species.</param>
        /// <param name="min">The smallest population.</param>
        /// <param name="max">The largest population.</param>
        /// <returns>The share of ticks at the cap, or NaN without a known cap.</returns>
        public double Population(string speciesId, out int min, out int max)
        {
            var change = new SortedDictionary<int, int>();
            foreach (var l in lives)
            {
                if (l.Species != speciesId) continue;
                change[l.Entry] = (change.TryGetValue(l.Entry, out var a) ? a : 0) + 1;
                change[l.Exit] = (change.TryGetValue(l.Exit, out var b) ? b : 0) - 1;
            }
            int pop = 0, prev = 0, atCap = 0;
            bool hasCap = Caps.TryGetValue(speciesId, out int cap);
            min = int.MaxValue; max = 0;
            foreach (var kv in change)
            {
                if (kv.Key > prev && kv.Key <= EndTick)
                {
                    if (hasCap && pop >= cap) atCap += kv.Key - prev;
                    min = Math.Min(min, pop); max = Math.Max(max, pop);
                }
                prev = Math.Max(prev, kv.Key);
                pop += kv.Value;
            }
            if (min == int.MaxValue) min = 0;
            return hasCap && EndTick > 0 ? (double)atCap / EndTick : double.NaN;
        }
    }
}
