using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Regression (30 §6): pinned hashes, equivalences, scene validation, the reference phase order.</summary>
    public class RegressionTests : WorldFixture
    {
        static readonly string[] Scenes = { "Assets/EvoSim/Scenes/Lab1_Small.unity", "Assets/EvoSim/Scenes/Lab1_Full.unity" };
        static readonly int[] Seeds = { 1234, 7, 42 };

        /// <summary>A scripted policy for the prey (R-01): flee a close threat, eat food in sight, mate with a near partner, else rest.</summary>
        static float[] PreyPolicy(DecisionQuery q)
        {
            if (q.Species.Id != "prey") return Brain.Uniform(q.Species.Actions.Count);
            string threat = ScriptedBrain.Token(q, "Predator"), food = ScriptedBrain.Token(q, "Food"), kin = ScriptedBrain.Token(q, "Animal");
            string action = threat == "adjacent" || threat == "close" ? "flee"
                          : food != null && food != "none" ? "eat"
                          : kin != null && kin.EndsWith(", ready") ? "mate" : "rest";
            return ScriptedBrain.Prefer(q.Species, action, 0.02f);
        }

        /// <summary>The scene's mutator client replaced by the fake one: pinned runs never call a model (30 §2).</summary>
        static void OfflineMutator(World w)
        {
            var client = w.GetComponentInChildren<MutatorClient>(true);
            var service = w.GetComponentInChildren<MutatorService>(true);
            var go = client != null ? client.gameObject : service != null ? service.gameObject : null;
            if (client != null) Object.DestroyImmediate(client);
            if (go != null) go.AddComponent<FakeMutator>();
        }

        [Test, Category("T3"), Description("R-01 (RAND-11): events hash after 2 000 ticks for each reference scene and seeds 1234, 7, 42, random brain and a scripted prey policy → equal to Tests/Golden/hashes.json")]
        public void PinnedHashes()
        {
            var sb = new StringBuilder("{\n");
            var lines = new List<string>();
            foreach (var scene in Scenes)
                foreach (int seed in Seeds)
                {
                    string random = Batch.HashOf(scene, seed, 2000, OfflineMutator);
                    string scripted = Batch.HashOf(scene, seed, 2000, w =>
                    {
                        OfflineMutator(w);
                        var brain = new GameObject("Scripted prey").AddComponent<ScriptedBrain>();
                        brain.transform.SetParent(w.transform.Find("Brains"), false);
                        brain.Policy = PreyPolicy;
                        w.transform.Find("Prey").GetComponent<Species>().SetBrain(brain);
                    });
                    string name = Path.GetFileNameWithoutExtension(scene);
                    lines.Add($"  \"{name}/{seed}/random\": \"{random}\"");
                    lines.Add($"  \"{name}/{seed}/scripted-prey\": \"{scripted}\"");
                }
            sb.Append(string.Join(",\n", lines)).Append("\n}\n");
            Golden.Check("hashes.json", sb.ToString());
        }

        static string Offline(World w)
        {
            w.DefaultBrain = w.GetComponentInChildren<RandomBrain>(true);
            w.NoMutation = true;
            w.WaitMode = WaitMode.Freeze;
            w.StartOnPlay = false;
            foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
            return null;
        }

        [Test, Category("T3"), Timeout(1800000), Description("R-06 soak: 100 000 ticks of the full world with the random brain → no exception, invariants hold (checked every 50 ticks), managed memory grows less than 5 % after tick 10 000")]
        public void Soak()
        {
            string result = Batch.WithScene(Scenes[1], w =>
            {
                Offline(w);
                w.TickLimit = 0;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                long at10k = 0;
                for (int t = 0; t < 100000 && w.State != RunState.Stopped; t += 50)
                {
                    w.Advance(50);
                    var problems = Invariants.Check(w);
                    Assert.IsEmpty(problems, $"tick {w.Tick}: {string.Join("; ", problems.Take(5))}");
                    if (w.Tick == 10000) at10k = System.GC.GetTotalMemory(true);
                }
                Assert.AreEqual(100000, w.Tick);
                long end = System.GC.GetTotalMemory(true);
                TestContext.WriteLine($"R-06: managed memory {at10k / 1048576.0:0.0} MB at tick 10 000, {end / 1048576.0:0.0} MB at 100 000");
                Assert.Less(end, at10k * 1.05, "memory stable");
                return "ok";
            });
            Assert.AreEqual("ok", result);
        }

        [Test, Category("T3"), Description("R-07 performance (20 §9): one tick of the full world (about 340 animals, random brain) under 2 ms on this machine; no allocation in the act phase after warm-up")]
        public void Performance()
        {
            string result = Batch.WithScene(Scenes[1], w =>
            {
                Offline(w);
                var phases = w.transform.Find("Phases");
                var act = w.GetComponentInChildren<ActPhase>(true).transform;
                var before = new GameObject("alloc before act").AddComponent<AllocationProbe>();
                before.transform.SetParent(phases, false);
                before.transform.SetSiblingIndex(act.GetSiblingIndex());
                var after = new GameObject("alloc after act").AddComponent<AllocationProbe>();
                after.transform.SetParent(phases, false);
                after.transform.SetSiblingIndex(act.GetSiblingIndex() + 1);
                after.Start = before;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                w.Advance(300);                                                        // warm-up: pools and lists reach their size
                after.Measuring = true;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                w.Advance(200);
                double ms = clock.Elapsed.TotalMilliseconds / 200.0;
                int animals = w.AllSpecies.Sum(s => s.Animals.Count);
                long allocating = after.PerTick.Count(b => b > 0);
                TestContext.WriteLine($"R-07: {animals} animals, {ms:0.000} ms per tick; act phase allocated in {allocating} of {after.PerTick.Count} ticks (max {after.PerTick.DefaultIfEmpty(0).Max()} bytes)");
                Assert.Less(ms, 2.0, "tick time");
                Assert.AreEqual(0, allocating, "allocation per tick in the act phase");
                return "ok";
            });
            Assert.AreEqual("ok", result);
        }

        [Test, Category("T3"), Description("R-08 (OUT-05): the prototype's gene_report and gene_timeline read a Unity run written in compatibility mode (a prototype-shaped world: no hide action) without error")]
        public void PrototypeToolsReadCompatibilityRuns()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string python = Path.Combine(root, "prototype", ".venv", "Scripts", "python.exe");
            if (!File.Exists(python)) Assert.Ignore("no prototype virtual environment (prototype/.venv)");
            // The tools write to results/: run them from a copy of the prototype's code under Temp, never in prototype/.
            string tools = Path.Combine(root, "Temp", "EvoSimPrototype");
            foreach (var dir in new[] { "configs", "data", "experiments", "promptevo", "prompts" }) CopyDirectory(Path.Combine(root, "prototype", dir), Path.Combine(tools, dir));
            File.Copy(Path.Combine(root, "prototype", "pyproject.toml"), Path.Combine(tools, "pyproject.toml"), true);
            Directory.CreateDirectory(Path.Combine(tools, "results"));

            string folder = Path.Combine(root, "Logs", "EvoSim", "test-runs", "r08");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            var b = New(8, name: "r08").Lab1(prey: 30, predators: 5).Recorder(true, "Logs/EvoSim/test-runs", "r08", true);
            var prey = b.Root.GetComponentsInChildren<Species>(true).First(s => s.gameObject.name == "prey");
            Object.DestroyImmediate(prey.GetComponentInChildren<HideAction>(true).gameObject);
            Object.DestroyImmediate(prey.GetComponentInChildren<NearestCoverSense>(true).gameObject);
            var w = b.Configure(x => x.TickLimit = 600).Build();
            while (w.State != RunState.Stopped) w.Advance(100);
            string run = w.Service<RunRecorder>().RunFolder;

            foreach (var args in new[]
            {
                $"-m experiments.gene_report \"{run}\" --tag r08 --min-carriers 1",
                $"-m experiments.gene_report \"{run}\" --tag r08_predator --species predator --min-carriers 1",
                $"-m experiments.gene_timeline \"{run}\" --tag r08 --every 100 --window 200 --drops 20",
            })
            {
                var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, args)
                {
                    WorkingDirectory = tools, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
                });
                string err = p.StandardError.ReadToEnd();
                p.StandardOutput.ReadToEnd();
                Assert.IsTrue(p.WaitForExit(300000), args + ": no answer in 5 minutes");
                Assert.AreEqual(0, p.ExitCode, args + "\n" + err);
            }
        }

        static void CopyDirectory(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(from))
                if (!Path.GetFileName(d).StartsWith("__pycache__")) CopyDirectory(d, Path.Combine(to, Path.GetFileName(d)));
        }

        [Test, Category("T3"), Description("R-04 (SPACE-11, SPACE-13, SPACE-14): Freeze = Responsive with a brain that answers late; 1 tick per call = 10 ticks per call")]
        public void Equivalences()
        {
            string Run(WaitMode mode, int perCall, int delay)
            {
                var w = New(31, mode, $"{mode}-{perCall}-{delay}").Lab1(prey: 30, predators: 5, randomBrain: false)
                    .DefaultBrain<ScriptedBrain>(b => b.DelayCalls = delay).Configure(x => x.TickLimit = 500).Build();
                while (w.State != RunState.Stopped) w.Advance(perCall);
                return w.Events.Hash;
            }
            string reference = Run(WaitMode.Freeze, 1, 0);
            Assert.AreEqual(reference, Run(WaitMode.Responsive, 1, 2), "Freeze = Responsive");
            Assert.AreEqual(reference, Run(WaitMode.Freeze, 10, 0), "1 = 10 ticks per call");
            Assert.AreEqual(reference, Run(WaitMode.Responsive, 10, 3), "both");
        }

        [Test, Description("M7 done-when (32 §4): S03 runs headless with the random brain, writes every file, and a second run gives the same hash")]
        public void ScenarioRunsHeadless()
        {
            var scenario = Batch.FindScenario("S03");
            Assert.IsNotNull(scenario, "Scenarios/S03_Lab1Small.asset");
            string Once() => Batch.Run(scenario, "", 1234, 300, w =>
            {
                var r = w.GetComponentInChildren<RunRecorder>(true);
                r.Configure("Logs/EvoSim/test-runs", r.RunName, false, true);
            });
            string first = Once();
            StringAssert.Contains("300 ticks", first);
            string folder = first.Substring(first.IndexOf("folder ") + 7).Trim();
            foreach (var f in new[] { "events.jsonl", "stats.csv", "alleles.jsonl", "final_population.json", "summary.json", "run_info.json" })
                Assert.IsTrue(File.Exists(Path.Combine(folder, f)), f);
            Assert.AreEqual("S03_Lab1Small", (string)JObject.Parse(File.ReadAllText(Path.Combine(folder, "run_info.json")))["scenario"]);
            string Hash(string line) => line.Substring(line.IndexOf("hash ") + 5, 16);
            Assert.AreEqual(Hash(first), Hash(Once()), "replays to the same hash");
        }

        [Test, Description("T-EDIT-03 (EDIT-01): every scene in Scenes/ validates without errors")]
        public void ScenesValidate()
        {
            var scenes = Directory.GetFiles(Path.Combine(Application.dataPath, "EvoSim", "Scenes"), "*.unity");
            Assert.Greater(scenes.Length, 0);
            foreach (var file in scenes)
            {
                string path = "Assets/EvoSim/Scenes/" + Path.GetFileName(file);
                string result = Batch.WithScene(path, w =>
                {
                    var report = new ValidationReport();
                    w.Prepare(report);
                    return report.HasErrors ? report.ToString() : "ok";
                });
                Assert.AreEqual("ok", result, path);
            }
        }

        [Test, Description("T-TICK-01 complete (TICK-01, TICK-04): a recording phase between each pair of phases of the reference scene shows the reference order")]
        public void ReferencePhaseOrder()
        {
            string order = Batch.WithScene(Scenes[0], w =>
            {
                var phases = w.transform.Find("Phases");
                var log = new List<string>();
                int n = phases.childCount;
                for (int i = n - 1; i >= 0; i--)
                {
                    var trace = new GameObject("trace after " + phases.GetChild(i).name).AddComponent<TracePhase>();
                    trace.transform.SetParent(phases, false);
                    trace.transform.SetSiblingIndex(i + 1);
                    trace.Log = log;
                }
                w.WaitMode = WaitMode.Freeze;
                w.DefaultBrain = w.GetComponentInChildren<RandomBrain>(true);
                w.NoMutation = true;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                w.Advance(1);
                return string.Join(",", w.Phases.Where(p => !(p is TracePhase)).Select(p => p.GetType().Name)) + "|" + string.Join(",", log);
            });
            var parts = order.Split('|');
            Assert.AreEqual("SensePhase,AskBrainsPhase,ChooseActionsPhase,ActPhase,BreedPhase,HatchPhase,DeathPhase,MigrationPhase,EnvironmentPhase,FloorPhase,RecordPhase", parts[0]);
            StringAssert.StartsWith("0:trace after Sense,0:trace after Ask brains,0:trace after Choose actions,0:trace after Act", parts[1]);
        }
    }
}
