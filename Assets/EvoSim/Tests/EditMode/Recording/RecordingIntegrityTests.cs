using System;
using System.IO;
using System.Linq;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Run files the P1 audit found incomplete (13 §2–§5): short runs, species added during a run, brain time, the compatibility summary.</summary>
    public class RecordingIntegrityTests : WorldFixture
    {
        static string Root => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "EvoSim", "test-runs");

        World Recorded(string name, Action<WorldBuilder> more = null, bool compatibility = false, bool randomBrain = true)
        {
            string folder = Path.Combine(Root, name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            var b = New(19, name: name).Lab1(prey: 20, predators: 3, randomBrain: randomBrain).Recorder(true, "Logs/EvoSim/test-runs", name, compatibility);
            more?.Invoke(b);
            return b.Build();
        }

        static string FileOf(World w, string file) => Path.Combine(w.Service<RunRecorder>().RunFolder, file);

        [Test, Description("13 §2 (OUT-01): a run shorter than one statistics period still has stats.csv, with its header")]
        public void ShortRunsHaveStats()
        {
            var w = Recorded("short");
            w.Advance(20);
            w.Stop("test");
            var lines = File.ReadAllLines(FileOf(w, "stats.csv"));
            Assert.AreEqual(1, lines.Length, "the header only");
            StringAssert.StartsWith("t,prey_pop", lines[0]);
        }

        [Test, Description("SPEC-30, 13 §4: a species added during the run gets its stats.csv columns; the rows before it leave them empty")]
        public void AddedSpeciesGetColumns()
        {
            var w = Recorded("columns");
            w.Advance(100);
            w.AddSpecies(w.FindSpecies("prey"), "deer", w.FindSpecies("prey"), founders: 4);
            w.Advance(100);
            w.Stop("test");
            var lines = File.ReadAllLines(FileOf(w, "stats.csv"));
            var header = lines[0].Split(',');
            Assert.AreEqual(3, lines.Length, "the header and two rows");
            int col = Array.IndexOf(header, "deer_pop");
            Assert.GreaterOrEqual(col, 0, lines[0]);
            Assert.AreEqual("", lines[1].Split(',')[col], "before the deer existed");
            Assert.AreEqual(w.FindSpecies("deer").Animals.Count(a => !a.IsGone).ToString(), lines[2].Split(',')[col]);
            foreach (var l in lines) Assert.AreEqual(header.Length, l.Split(',').Length, "every row has every column");
        }

        [Test, Description("13 §5: backend_s is the brains' time per species, here a server that answers in 20 ms")]
        public void BrainTimeIsRecorded()
        {
            var fake = FakeHttpTransport.Jev(delayMs: 20);
            var w = Recorded("backend", b => b.DefaultBrain<JevBrain>(j =>
            {
                j.SetModelFiles(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/decision_head.json"),
                                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/calibration.json"));
                j.Transport = fake;
                j.ConfigureClient("http://jev.test", 5f, 1, 0f, 2);
            }), randomBrain: false);
            w.Advance(9);
            w.Stop("test");
            var summary = JObject.Parse(File.ReadAllText(FileOf(w, "summary.json")));
            foreach (var s in new[] { "prey", "predator" })
                Assert.Greater((double)summary["species"][s]["backend_s"], 0.0, s);
        }

        [Test, Description("OUT-01: two runs under the same name each get their own folder (the second with a suffix)")]
        public void EachRunHasItsOwnFolder()
        {
            foreach (var d in Directory.Exists(Root) ? Directory.GetDirectories(Root, "twice*") : new string[0]) Directory.Delete(d, true);
            string Folder()
            {
                var w = New(3, name: "twice").Lab1(prey: 4, predators: 1).Recorder(true, "Logs/EvoSim/test-runs", "twice").Build();
                w.Stop("test");
                return w.Service<RunRecorder>().RunFolder;
            }
            string first = Folder(), second = Folder();
            Assert.AreNotEqual(first, second);
            StringAssert.EndsWith("twice", first);
            StringAssert.EndsWith("twice-2", second);
        }

        [Test, Description("CTRL-01 (13 §2): run_info.json records the controls of the run")]
        public void ControlsAreRecorded()
        {
            var w = Recorded("controls", b => b.Configure(x => { x.NoMutation = true; x.Asexual = true; }));
            w.Stop("test");
            var controls = JObject.Parse(File.ReadAllText(FileOf(w, "run_info.json")))["controls"];
            Assert.IsTrue((bool)controls["no_mutation"]);
            Assert.IsTrue((bool)controls["asexual"]);
            Assert.IsFalse((bool)controls["shuffled"]);
            Assert.IsFalse((bool)controls["random_founders"]);
            Assert.IsTrue((bool)controls["null_brain"], "the random brain decides");
        }

        [Test, Description("RAND-20: the wall-clock limit stops the run at a tick boundary, with its files")]
        public void WallClockLimit()
        {
            var w = Recorded("wall clock", b => b.Configure(x => x.WallClockMinutes = 0.0002f));
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (w.State != RunState.Stopped && clock.Elapsed.TotalSeconds < 60) w.Advance(1);
            Assert.AreEqual("wall-clock limit", w.StopReason);
            Assert.AreEqual(0, w.NextPhaseIndex);
            Assert.AreEqual("wall-clock limit", (string)JObject.Parse(File.ReadAllText(FileOf(w, "summary.json")))["stopped"]);
        }

        [Test, Description("C4 random founders (CTRL-01): number genes start uniform within their range")]
        public void RandomFoundersSpreadNumberGenes()
        {
            var w = New(8, name: "random founders").Flat(40, 40).Food(0f, 0f)
                .Species("prey", s => s.Population(200).PreyBody().Action<RestAction>("rest", founders: new[] { "Rest." })
                    .Module<NumberGene>(g => g.Configure("stamina.max", 20f, 120f, new[] { 60f }, geneLabel: "endurance")))
                .Configure(x => { x.RandomFounders = true; x.SetControlSentences(new[] { "Do as you like." }); }).Build();
            var s = w.AllSpecies[0];
            int locus = s.Genes.First(g => g is NumberGene).Locus;
            var values = s.Animals.Select(a => a.Genome[locus].Number).ToList();
            Assert.IsTrue(values.All(v => v >= 20f && v <= 120f), "within the range");
            Assert.Less(values.Min(), 35f);
            Assert.Greater(values.Max(), 105f);
            Assert.AreEqual(70.0, values.Average(), 6.0, "uniform: the mean is the middle");
        }

        [Test, Description("OUT-05: in the compatibility summary the run's failure total isn't replaced by the first species' own")]
        public void CompatibilityKeepsTheRunsFailures()
        {
            var w = Recorded("compat failures", b => b.DefaultBrain<ScriptedBrain>(x => x.FailWhen = q => q.Species.Id == "predator" ? "down" : null),
                             compatibility: true, randomBrain: false);
            w.Advance(9);
            w.Stop("test");
            var summary = JObject.Parse(File.ReadAllText(FileOf(w, "summary.json")));
            int predatorFailures = w.FindSpecies("predator").Counters.Failures;
            Assert.Greater(predatorFailures, 0);
            Assert.AreEqual(0, w.FindSpecies("prey").Counters.Failures);
            Assert.AreEqual(predatorFailures, (int)summary["failures"], "the run's total");
        }
    }
}
