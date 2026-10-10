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

        [Test, Category("T3"), Description("R-01 (RAND-11): events hash after 2 000 ticks for each reference scene and seeds 1234, 7, 42, random brain and a scripted prey policy → equal to Tests/Golden/hashes.json")]
        public void PinnedHashes()
        {
            var sb = new StringBuilder("{\n");
            var lines = new List<string>();
            foreach (var scene in Scenes)
                foreach (int seed in Seeds)
                {
                    string random = Batch.HashOf(scene, seed, 2000);
                    string scripted = Batch.HashOf(scene, seed, 2000, w =>
                    {
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

        [Test, Category("T3"), Description("R-04 (SPACE-11, SPACE-14): Freeze = Responsive with a brain that answers late; 1 tick per call = 10 ticks per call")]
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
