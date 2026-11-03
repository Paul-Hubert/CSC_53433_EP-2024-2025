using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The Survival Report of one scenario and arm (plan M16 §6.2): the markdown page and the per-window CSV, from the
    /// <see cref="SurvivalAnalysis"/> of each of its runs (one per seed).
    /// </summary>
    /// <remarks>
    /// <para>Headline: for each matched pair, the death hazard ratio neural/JEV over the late windows (the last
    /// <see cref="LateWindows"/> windows of <see cref="WindowTicks"/>), per seed and pooled (deaths and animal-ticks summed),
    /// with its 95 % interval. An effect counts only when every seed points the same way and the pooled interval excludes 1.
    /// Then the per-species measures, cause-specific ratios, the kill matrix, the survival curves and every window.</para>
    /// <para>Pairs come from the runs' <c>survival.json</c>; without one, a species <c>x-nn</c> is paired with <c>x</c>.</para>
    /// </remarks>
    public sealed class SurvivalReport
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        readonly List<SurvivalAnalysis> runs;

        /// <summary>The window length in ticks (plan: 2 000).</summary>
        public int WindowTicks { get; set; } = 2000;
        /// <summary>The late windows of the headline (plan: 3).</summary>
        public int LateWindows { get; set; } = 3;
        /// <summary>Lines listed under "Settings that differ from the other arm" (the window fills them).</summary>
        public List<string> Notes { get; } = new List<string>();

        /// <summary>A report over runs of one scenario and arm.</summary>
        /// <param name="analyses">One analysis per run (seed).</param>
        public SurvivalReport(IEnumerable<SurvivalAnalysis> analyses) { runs = analyses.ToList(); }

        /// <summary>The runs, in the order given.</summary>
        public IReadOnlyList<SurvivalAnalysis> Runs => runs;

        /// <summary>The matched pairs (JEV, neural) of the runs; by the <c>-nn</c> suffix when no run names any.</summary>
        public List<(string jev, string neural)> Pairs()
        {
            var pairs = new List<(string, string)>();
            foreach (var r in runs) foreach (var p in r.Pairs) if (!pairs.Contains(p)) pairs.Add(p);
            if (pairs.Count > 0) return pairs;
            foreach (var s in AllSpecies())
                if (s.EndsWith("-nn") && AllSpecies().Contains(s.Substring(0, s.Length - 3))) pairs.Add((s.Substring(0, s.Length - 3), s));
            return pairs;
        }

        /// <summary>Every species of the runs, in order of first appearance.</summary>
        public List<string> AllSpecies()
        {
            var all = new List<string>();
            foreach (var r in runs) foreach (var s in r.Species) if (!all.Contains(s)) all.Add(s);
            return all;
        }

        /// <summary>The headline ratio of one pair: per run, then pooled (the last entry).</summary>
        /// <param name="pair">The pair.</param>
        /// <param name="cause">One cause, or null for all but migration.</param>
        /// <returns>One ratio per run, then the pooled one.</returns>
        public List<RateRatio> Headline((string jev, string neural) pair, string cause = null)
        {
            var list = new List<RateRatio>();
            double d1 = 0, t1 = 0, d2 = 0, t2 = 0;
            foreach (var r in runs)
            {
                var n = r.Late(pair.neural, WindowTicks, LateWindows);
                var j = r.Late(pair.jev, WindowTicks, LateWindows);
                var rr = SurvivalAnalysis.Ratio(n, j, cause);
                list.Add(rr);
                d1 += rr.D1; t1 += rr.T1; d2 += rr.D2; t2 += rr.T2;
            }
            list.Add(new RateRatio(d1, t1, d2, t2));
            return list;
        }

        /// <summary>The verdict on a pair's ratios: an effect only when every seed points the same way and the pooled interval excludes 1.</summary>
        /// <param name="ratios">The ratios of <see cref="Headline"/>.</param>
        /// <returns>"neural survives better", "JEV survives better", or "no effect".</returns>
        public static string Verdict(IReadOnlyList<RateRatio> ratios)
        {
            var pooled = ratios[ratios.Count - 1];
            if (!pooled.ExcludesOne) return "no effect";
            bool below = pooled.Ratio < 1;
            for (int i = 0; i < ratios.Count - 1; i++)
                if (!ratios[i].Defined || (ratios[i].Ratio < 1) != below) return "no effect (the seeds disagree)";
            return below ? "neural survives better" : "JEV survives better";
        }

        /// <summary>The markdown page.</summary>
        /// <param name="title">The page title (scenario and arm).</param>
        /// <returns>The text.</returns>
        public string Markdown(string title)
        {
            var sb = new StringBuilder();
            sb.Append("# Survival report — ").Append(title).Append("\n\n");
            sb.Append("Windows of ").Append(WindowTicks).Append(" ticks; late windows = the last ").Append(LateWindows)
              .Append(". Rates per 1 000 animal-ticks. Migration and the run's end are censoring.\n\n");

            sb.Append("| Run | Seed | Ticks | Stopped | Plateau tick |\n|---|---|---|---|---|\n");
            foreach (var r in runs)
                sb.Append($"| {Path(r)} | {r.Seed} | {r.EndTick} | {r.StopReason} | {(r.PlateauTick >= 0 ? r.PlateauTick.ToString(Inv) : "—")} |\n");
            if (Notes.Count > 0)
            {
                sb.Append("\n**Settings that differ from the other arm:**\n");
                foreach (var n in Notes) sb.Append("- ").Append(n).Append('\n');
            }

            sb.Append("\n## Headline: death hazard ratio, neural / JEV (late windows)\n\n");
            sb.Append("| Pair | Seed | Neural deaths / animal-ticks | JEV deaths / animal-ticks | Ratio | 95 % interval |\n|---|---|---|---|---|---|\n");
            foreach (var p in Pairs())
            {
                var hr = Headline(p);
                for (int i = 0; i < hr.Count; i++)
                    sb.Append($"| {p.neural} / {p.jev} | {(i < runs.Count ? runs[i].Seed.ToString(Inv) : "pooled")} | {hr[i].D1:0} / {hr[i].T1:0} | {hr[i].D2:0} / {hr[i].T2:0} | {Num(hr[i].Ratio)} | {Num(hr[i].Lower)}–{Num(hr[i].Upper)} |\n");
                sb.Append($"\n**{p.neural} vs {p.jev}: {Verdict(hr)}.**\n\n");
            }

            sb.Append("## Cause-specific hazard ratios, pooled (late windows)\n\n| Pair | Cause | Ratio | 95 % interval |\n|---|---|---|---|\n");
            foreach (var p in Pairs())
                foreach (var cause in new[] { "starvation", "killed", "old_age" })
                {
                    var hr = Headline(p, cause);
                    var pooled = hr[hr.Count - 1];
                    sb.Append($"| {p.neural} / {p.jev} | {cause} | {Num(pooled.Ratio)} | {Num(pooled.Lower)}–{Num(pooled.Upper)} |\n");
                }

            sb.Append("\n## Per species (whole run)\n\n| Species | Seed | Median lifespan | Starvation | Killed | Old age | Juvenile survival | Offspring / animal | Bred | Newcomers / 1 000 ticks | Population mean (min–max) | At cap |\n|---|---|---|---|---|---|---|---|---|---|---|---|\n");
            foreach (var s in AllSpecies())
                foreach (var r in runs)
                {
                    if (!r.Species.Contains(s)) continue;
                    var w = r.Window(s, 0, r.EndTick);
                    double off = r.OffspringPerAnimal(s, out double bred);
                    double atCap = r.Population(s, out int min, out int max);
                    int median = r.MedianLifespan(s);
                    sb.Append($"| {s} | {r.Seed} | {(median >= 0 ? median.ToString(Inv) : "> run")} | {Num(w.Hazard("starvation"))} | {Num(w.Hazard("killed"))} | {Num(w.Hazard("old_age"))} | {Pct(r.JuvenileSurvival(s))} | {Num(off)} | {Pct(bred)} | {Num(r.EndTick > 0 ? 1000.0 * w.Immigrants / r.EndTick : 0)} | {Num(w.MeanPopulation)} ({min}–{max}) | {Pct(atCap)} |\n");
                }

            sb.Append("\n## Kill matrix (whole run, pooled)\n\nKills, then per 1 000 killer-ticks and per 1 000 victim-ticks.\n\n");
            var victims = AllSpecies();
            sb.Append("| Killer \\ victim | ").Append(string.Join(" | ", victims)).Append(" |\n|---|").Append(string.Concat(victims.Select(_ => "---|"))).Append('\n');
            foreach (var killer in victims)
            {
                var row = new List<string>();
                bool any = false;
                foreach (var victim in victims)
                {
                    int kills = 0; double kt = 0, vt = 0;
                    foreach (var r in runs)
                    {
                        if (r.KillMatrix().TryGetValue((killer, victim), out var k)) kills += k;
                        kt += r.Window(killer, 0, r.EndTick).AnimalTicks;
                        vt += r.Window(victim, 0, r.EndTick).AnimalTicks;
                    }
                    any |= kills > 0;
                    row.Add(kills == 0 ? "0" : $"{kills} ({Num(1000.0 * kills / Math.Max(1, kt))}, {Num(1000.0 * kills / Math.Max(1, vt))})");
                }
                if (any) sb.Append("| ").Append(killer).Append(" | ").Append(string.Join(" | ", row)).Append(" |\n");
            }

            sb.Append("\n## Survival curves (Kaplan–Meier, 95 % Greenwood band)\n\n| Species | Seed | S(100) | S(250) | S(500) | S(1 000) | S(1 500) |\n|---|---|---|---|---|---|---|\n");
            foreach (var s in AllSpecies())
                foreach (var r in runs)
                {
                    if (!r.Species.Contains(s)) continue;
                    var curve = r.Curve(s);
                    sb.Append($"| {s} | {r.Seed} |");
                    foreach (int age in new[] { 100, 250, 500, 1000, 1500 }) sb.Append(' ').Append(At(curve, age)).Append(" |");
                    sb.Append('\n');
                }

            sb.Append("\n## Per window\n\n| Seed | Species | Window | Population | Mean generation | All deaths | Starvation | Killed | Births / animal | Newcomers |\n|---|---|---|---|---|---|---|---|---|---|\n");
            foreach (var r in runs)
                foreach (var s in r.Species)
                    foreach (var w in r.Windows(s, WindowTicks))
                        sb.Append($"| {r.Seed} | {s} | {w.From}–{w.To} | {Num(w.MeanPopulation)} | {Num(w.MeanGeneration)} | {Num(w.Hazard(null))} | {Num(w.Hazard("starvation"))} | {Num(w.Hazard("killed"))} | {Num(w.BirthsPerAnimal)} | {w.Immigrants} |\n");
            return sb.ToString();
        }

        /// <summary>The per-window table as CSV, one row per run, species and window.</summary>
        /// <returns>The CSV text with its header.</returns>
        public string WindowsCsv()
        {
            var sb = new StringBuilder("scenario,variant,seed,species,from,to,animal_ticks,mean_population,mean_generation,deaths,starvation,killed,old_age,migrated,births,immigrants\n");
            foreach (var r in runs)
                foreach (var s in r.Species)
                    foreach (var w in r.Windows(s, WindowTicks))
                        sb.Append(string.Join(",", r.Scenario, r.Variant, r.Seed.ToString(Inv), s, w.From.ToString(Inv), w.To.ToString(Inv),
                            w.AnimalTicks.ToString(Inv), Num(w.MeanPopulation), Num(w.MeanGeneration), w.DeathsBy(null).ToString(Inv),
                            w.DeathsBy("starvation").ToString(Inv), w.DeathsBy("killed").ToString(Inv), w.DeathsBy("old_age").ToString(Inv),
                            w.DeathsBy(SurvivalAnalysis.Migrated).ToString(Inv), w.Births.ToString(Inv), w.Immigrants.ToString(Inv))).Append('\n');
            return sb.ToString();
        }

        /// <summary>S(t) at an age with its band, from a curve: "0.82 (0.78–0.86)".</summary>
        static string At(List<KaplanMeierStep> curve, int age)
        {
            KaplanMeierStep? last = null;
            foreach (var step in curve) { if (step.Time > age) break; last = step; }
            return last == null ? "1" : $"{Num(last.Value.Survival)} ({Num(last.Value.Lower)}–{Num(last.Value.Upper)})";
        }

        static string Path(SurvivalAnalysis r) => string.IsNullOrEmpty(r.Folder) ? "—" : "`" + System.IO.Path.GetFileName(r.Folder) + "`";
        static string Num(double v) => double.IsNaN(v) ? "—" : v.ToString("0.###", Inv);
        static string Pct(double v) => double.IsNaN(v) ? "—" : (100 * v).ToString("0.#", Inv) + " %";
    }
}
