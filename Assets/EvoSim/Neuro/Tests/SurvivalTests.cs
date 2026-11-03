using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// T-NEURO-13 (<see cref="SurvivalAnalysis"/>, <see cref="SurvivalStats"/>) and T-NEURO-14 (<see cref="PlateauStop"/>),
    /// plan M16 §6.2, §6.3.
    /// </summary>
    public class SurvivalTests : WorldFixture
    {
        // ---- T-NEURO-13: SurvivalAnalysis on hand-made logs ----

        [Test, Description("T-NEURO-13: Kaplan–Meier and Greenwood on a worked example (deaths at 1, 3, 3, 5; censored at 2, 4)")]
        public void KaplanMeierWorkedExample()
        {
            var steps = SurvivalAnalysis.KaplanMeier(new[] { (1, true), (2, false), (3, true), (3, true), (4, false), (5, true) });
            Assert.AreEqual(new[] { 1, 3, 5 }, steps.Select(s => s.Time).ToArray());
            Assert.AreEqual(new[] { 6, 4, 1 }, steps.Select(s => s.AtRisk).ToArray(), "censored at 2 and 4 leave the risk set");
            Assert.AreEqual(5.0 / 6.0, steps[0].Survival, 1e-9);
            Assert.AreEqual(5.0 / 12.0, steps[1].Survival, 1e-9, "5/6 · 2/4");
            Assert.AreEqual(0.0, steps[2].Survival, 1e-9);
            Assert.AreEqual(0.0231481, steps[0].Variance, 1e-6, "S² · 1/(6·5)");
            Assert.AreEqual(0.0491898, steps[1].Variance, 1e-6, "S² · (1/30 + 2/(4·2))");
            Assert.AreEqual(0.8333 - 1.96 * System.Math.Sqrt(0.0231481), steps[0].Lower, 1e-3);
            Assert.AreEqual(1.0, steps[0].Upper, 1e-9, "clamped at 1");
        }

        [Test, Description("T-NEURO-13: the rate ratio and its 95 % interval from the log rate ratio equal hand-computed values")]
        public void RateRatioByHand()
        {
            var rr = new RateRatio(10, 1000, 20, 1000);
            Assert.AreEqual(0.5, rr.Ratio, 1e-12);
            Assert.AreEqual(System.Math.Sqrt(0.15), rr.LogSe, 1e-12);
            Assert.AreEqual(0.2340, rr.Lower, 1e-3, "0.5 · e^(−1.96 · 0.3873)");
            Assert.AreEqual(1.0681, rr.Upper, 1e-3);
            Assert.IsFalse(rr.ExcludesOne);
            Assert.IsTrue(new RateRatio(10, 1000, 60, 1000).ExcludesOne);
            Assert.IsFalse(new RateRatio(0, 1000, 5, 1000).Defined);
        }

        static readonly string[] Log =
        {
            "{\"kind\":\"founder\",\"t\":0,\"id\":0,\"species\":\"prey\",\"genome\":[]}",
            "{\"kind\":\"founder\",\"t\":0,\"id\":1,\"species\":\"prey\",\"genome\":[]}",
            "{\"kind\":\"founder\",\"t\":0,\"id\":2,\"species\":\"predator\",\"genome\":[]}",
            "{\"kind\":\"birth\",\"t\":10,\"id\":3,\"species\":\"prey\",\"parents\":[0],\"gen\":1}",
            "{\"kind\":\"death\",\"t\":20,\"id\":0,\"species\":\"prey\",\"cause\":\"killed\",\"age\":20,\"killer\":2}",
            "{\"kind\":\"death\",\"t\":30,\"id\":3,\"species\":\"prey\",\"cause\":\"migrated\",\"age\":20}",
            "{\"kind\":\"immigrant\",\"t\":40,\"id\":4,\"species\":\"prey\",\"genome\":[]}",
            "{\"kind\":\"death\",\"t\":50,\"id\":1,\"species\":\"prey\",\"cause\":\"starvation\",\"age\":50}",
        };

        [Test, Description("T-NEURO-13: lives from events; migration and the run's end are censoring; windows, kills and offspring")]
        public void LivesFromEvents()
        {
            var a = SurvivalAnalysis.FromEvents(Log, 100);
            Assert.AreEqual(new[] { "prey", "predator" }, a.Species.ToArray());
            var byId = a.Lives.ToDictionary(l => l.Id);
            Assert.IsTrue(byId[0].Dead);
            Assert.AreEqual("predator", byId[0].KillerSpecies);
            Assert.AreEqual(1, byId[0].Offspring);
            Assert.IsFalse(byId[3].Dead, "migrated: censored");
            Assert.AreEqual(SurvivalAnalysis.Migrated, byId[3].Cause);
            Assert.IsFalse(byId[2].Dead, "alive at the end: censored");
            Assert.AreEqual(100, byId[2].Age);
            Assert.AreEqual(60, byId[4].Age, "an immigrant at 40, censored at 100");

            var w = a.Window("prey", 0, 100);
            Assert.AreEqual(20 + 50 + 20 + 60, w.AnimalTicks);
            Assert.AreEqual(2, w.DeathsBy(null), "killed and starved; the migration is not a death");
            Assert.AreEqual(1, w.DeathsBy(SurvivalAnalysis.Migrated));
            Assert.AreEqual(1, w.Births);
            Assert.AreEqual(1, w.Immigrants);
            var first = a.Window("prey", 0, 25);
            Assert.AreEqual(1, first.DeathsBy("killed"));
            Assert.AreEqual(20 + 25 + 15, first.AnimalTicks);
            Assert.AreEqual(1, a.Window("predator", 0, 100).Kills["prey"]);
            Assert.AreEqual(1, a.KillMatrix()[("predator", "prey")]);

            var curve = a.Curve("prey");
            Assert.AreEqual(new[] { 20, 50 }, curve.Select(s => s.Time).ToArray(), "deaths at ages 20 and 50");
            Assert.AreEqual(4, curve[0].AtRisk);
            Assert.AreEqual(0.75, curve[0].Survival, 1e-9);
            Assert.AreEqual(0.75 * (1 - 1.0 / 2), curve[1].Survival, 1e-9, "two at risk at 50: the immigrant (60) and the starved one");
            Assert.AreEqual(50, a.MedianLifespan("prey"));
            double off = a.OffspringPerAnimal("prey", out double bred);
            Assert.AreEqual(1.0 / 3.0, off, 1e-9, "among the three that left");
            Assert.AreEqual(1.0 / 3.0, bred, 1e-9);
        }

        [Test, Description("T-NEURO-13: the report pairs x-nn with x, and an effect needs every seed to agree")]
        public void ReportVerdict()
        {
            Assert.AreEqual("neural survives better", SurvivalReport.Verdict(new[] { new RateRatio(10, 1000, 30, 1000), new RateRatio(12, 1000, 28, 1000), new RateRatio(22, 2000, 58, 2000) }));
            Assert.AreEqual("no effect (the seeds disagree)", SurvivalReport.Verdict(new[] { new RateRatio(10, 1000, 60, 1000), new RateRatio(30, 1000, 25, 1000), new RateRatio(40, 2000, 85, 2000) }));
            Assert.AreEqual("no effect", SurvivalReport.Verdict(new[] { new RateRatio(20, 1000, 21, 1000), new RateRatio(20, 1000, 21, 1000) }));
            var lines = Log.Select(l => l.Replace("\"predator\"", "\"prey-nn\"")).ToArray();
            var r = new SurvivalReport(new[] { SurvivalAnalysis.FromEvents(lines, 100) }) { WindowTicks = 50, LateWindows = 2 };
            Assert.AreEqual(new[] { ("prey", "prey-nn") }, r.Pairs().ToArray());
            StringAssert.Contains("## Headline", r.Markdown("test"));
            StringAssert.StartsWith("scenario,variant,seed,species,from,to", r.WindowsCsv());
        }

        // ---- T-NEURO-13: SurvivalStats on a 4-species run ----

        [Test, Description("T-NEURO-13: S32 L0 for 400 ticks: SurvivalStats' deaths by cause and kills by killer equal the events; the kill matrix sums to the kills; survival.json is written")]
        public void SurvivalStatsMatchTheEvents()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/S32_Brains2x2.asset");
            var counts = new Dictionary<string, double>();
            string line = EvoSim.Batch.Run(scenario, "L0-default", 1, 400, w =>
            {
                w.GetComponentInChildren<RunRecorder>(true).OutputRoot = "Logs/EvoSim/test-runs";
            });
            StringAssert.DoesNotStartWith("FAILED", line);
            string folder = line.Substring(line.IndexOf("folder ") + 7).Trim();
            try
            {
                Assert.IsTrue(File.Exists(Path.Combine(folder, "survival.json")), "survival.json");
                var a = SurvivalAnalysis.FromFolder(folder);
                CollectionAssert.AreEquivalent(new[] { "prey", "predator", "prey-nn", "predator-nn" }, a.Species);
                Assert.AreEqual(2, a.Pairs.Count, "the pairs from survival.json");
                var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(folder, "survival.json")));
                int kills = 0;
                foreach (var s in a.Species)
                {
                    var w = a.Window(s, 0, a.EndTick + 1);
                    foreach (var cause in new[] { "starvation", "killed", "old_age", SurvivalAnalysis.Migrated })
                        Assert.AreEqual(w.DeathsBy(cause), (int?)json["species"][s]["deaths"][cause] ?? 0, $"{s} {cause}");
                    foreach (var v in a.Species)
                    {
                        int k = w.Kills.TryGetValue(v, out var n) ? n : 0;
                        Assert.AreEqual(k, (int?)json["species"][s]["kills"][v] ?? 0, $"{s} kills {v}");
                        kills += k;
                    }
                }
                Assert.Greater(kills, 0, "predators killed in 400 ticks");
                Assert.AreEqual(kills, a.KillMatrix().Values.Sum(), "the matrix sums to the kills");
                Assert.AreEqual(kills, a.Lives.Count(l => l.Cause == "killed"), "every kill has a killer");
            }
            finally { Directory.Delete(folder, true); }
        }

        // ---- T-NEURO-14: PlateauStop ----

        static PlateauWindow W(double pop, double gen, double starve = 1, double kill = 0, double births = 0.5) =>
            new PlateauWindow("x", pop, gen, starve, kill, births);

        [Test, Description("T-NEURO-14: no stop before 50 generations; a ±10 % plateau over three windows stops; one window outside does not")]
        public void PlateauRule()
        {
            var flat = new List<PlateauWindow> { W(100, 48), W(102, 49), W(98, 49.9) };
            Assert.IsFalse(PlateauStop.Plateau(flat, 50, 0.1f, 3), "below 50 generations");
            flat.Add(W(100, 50));
            Assert.IsTrue(PlateauStop.Plateau(flat, 50, 0.1f, 3), "flat and 50 generations");
            Assert.IsFalse(PlateauStop.Plateau(new[] { W(100, 60), W(100, 60) }, 50, 0.1f, 3), "fewer than three windows");

            var series = new List<PlateauWindow> { W(60, 55), W(100, 56), W(80, 57), W(100, 58), W(104, 59), W(97, 60), W(100, 61) };
            int first = -1;
            for (int i = 1; i <= series.Count && first < 0; i++)
                if (PlateauStop.Plateau(series.Take(i).ToList(), 50, 0.1f, 3)) first = i;
            Assert.AreEqual(6, first, "the first boundary where the last three windows (100, 104, 97) stay within ±10 %");
            Assert.IsFalse(PlateauStop.Plateau(new[] { W(100, 60, starve: 1), W(100, 60, starve: 1.3), W(100, 60, starve: 1) }, 50, 0.1f, 3), "the starvation hazard moves");
            Assert.IsTrue(PlateauStop.Plateau(new[] { W(100, 60, kill: 0), W(100, 60, kill: 0), W(100, 60, kill: 0) }, 50, 0.1f, 3), "a measure at 0 throughout is flat");
        }

        /// <summary>A world of neural prey grazing, with a recorder writing to the test-runs folder and a plateau phase.</summary>
        World PlateauWorld(string name, System.Action<PlateauStop> configure)
        {
            return New(4, name: name).Flat(40, 40).Food(0.3f, 0.01f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .Phase<PlateauStop>(configure)
                .Recorder(files: true, name: name)
                .DefaultBrain<ScriptedBrain>(b => { b.ReadsText = false; b.TakesAttachments = true; b.Policy = q => new[] { 0f, 0f, 1f, 0f, 0f }; })
                .Species("prey", s =>
                {
                    s.PreyBody().Sense<InteroceptionSense>().Module<HungerClock>();
                    s.Action<TurnAction>("left", t => t.Configure(false), withGene: false)
                     .Action<TurnAction>("right", t => t.Configure(true), withGene: false)
                     .Action<ForwardAction>("walk", f => f.Configure(false), withGene: false)
                     .Action<ForwardAction>("run", f => f.Configure(true), withGene: false)
                     .Action<StopAction>("stop", withGene: false)
                     .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { 4 }, 8); }, "Brain")
                     .Population(10);
                })
                .Build();
        }

        static void AssertComplete(World w)
        {
            string folder = w.Service<RunRecorder>().RunFolder;
            try
            {
                foreach (var f in new[] { "events.jsonl", "stats.csv", "summary.json", "final_population.json", "run_info.json" })
                    Assert.IsTrue(File.Exists(Path.Combine(folder, f)), f);
                var summary = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
                Assert.AreEqual(w.Tick, (int)summary["ticks"], "the summary closes at the stop tick");
            }
            finally { Directory.Delete(folder, true); }
        }

        [Test, Description("T-NEURO-14 (RAND-20): the rule met at the first window boundary → a clean stop there, outputs complete")]
        public void StopsAtThePlateau()
        {
            var w = PlateauWorld("plateau", p => p.Configure(20, 0, 1f, 1, 1000));
            w.Advance(200);
            Assert.AreEqual(RunState.Stopped, w.State);
            Assert.AreEqual("plateau", w.StopReason);
            Assert.AreEqual(20, w.Tick, "the first window boundary");
            Assert.AreEqual(20, w.GetComponentInChildren<PlateauStop>().PlateauTick);
            AssertComplete(w);
        }

        [Test, Description("T-NEURO-14: a species that never reaches 50 generations → the run stops at the cap; observe-only never stops")]
        public void StopsAtTheCap()
        {
            var w = PlateauWorld("plateau cap", p => p.Configure(20, 50, 0.1f, 3, 60));
            w.Advance(200);
            Assert.AreEqual("plateau cap", w.StopReason);
            Assert.AreEqual(60, w.Tick);
            Assert.AreEqual(-1, w.GetComponentInChildren<PlateauStop>().PlateauTick);
            AssertComplete(w);

            var watch = PlateauWorld("plateau watch", p => p.Configure(20, 0, 1f, 1, 60, stop: false));
            watch.Advance(100);
            Assert.AreNotEqual(RunState.Stopped, watch.State, "observe only");
            Assert.AreEqual(20, watch.GetComponentInChildren<PlateauStop>().PlateauTick, "the tick is recorded");
            watch.Stop("test");
            Directory.Delete(watch.Service<RunRecorder>().RunFolder, true);
        }
    }
}
