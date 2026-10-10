using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Run files (13), replay (12 §4), secrets (OUT-04): T2 whole-world runs.</summary>
    public class OutputPlayTests : WorldFixture
    {
        static string Root => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "EvoSim", "test-runs");

        static IEnumerable<JObject> Lines(string file) =>
            File.ReadAllLines(file).Where(l => l.Trim().Length > 0).Select(JObject.Parse);

        World Run(string name, int ticks, Action<WorldBuilder> more = null, bool compatibility = false)
        {
            string folder = Path.Combine(Root, name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            var b = New(17, name: name).Lab1(prey: 30, predators: 5).Recorder(true, "Logs/EvoSim/test-runs", name, compatibility)
                .Configure(w => w.TickLimit = ticks);
            more?.Invoke(b);
            var world = b.Build();
            while (world.State != RunState.Stopped) world.Advance(50);
            return world;
        }

        [Test, Description("T-OUT-01 (OUT-01, OUT-02): a 500-tick run → its own folder with every file of 13 §2; events in tick order, keys sorted, each with kind, t, id, species")]
        public void RunFiles()
        {
            var w = Run("out01", 500);
            string folder = w.Service<RunRecorder>().RunFolder;
            foreach (var f in new[] { "events.jsonl", "stats.csv", "alleles.jsonl", "final_population.json", "summary.json", "run_info.json" })
                Assert.IsTrue(File.Exists(Path.Combine(folder, f)), f);
            int last = 0;
            foreach (var raw in File.ReadAllLines(Path.Combine(folder, "events.jsonl")))
            {
                var e = JObject.Parse(raw);
                var keys = e.Properties().Select(p => p.Name).ToList();
                CollectionAssert.AreEqual(keys.OrderBy(k => k, StringComparer.Ordinal).ToList(), keys, "keys sorted");
                Assert.IsNotNull(e["kind"]); Assert.IsNotNull(e["t"]);
                if ((string)e["kind"] != "species_created") { Assert.IsNotNull(e["id"]); Assert.IsNotNull(e["species"]); }
                Assert.GreaterOrEqual((int)e["t"], last, "tick order");
                last = (int)e["t"];
            }
            var stats = File.ReadAllLines(Path.Combine(folder, "stats.csv"));
            Assert.AreEqual(6, stats.Length, "a header and one row every 100 ticks");
            StringAssert.StartsWith("t,prey_pop,prey_mean_energy", stats[0]);
            var summary = JObject.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
            Assert.AreEqual(500, (int)summary["ticks"]);
            Assert.AreEqual(w.Events.ShortHash, (string)summary["events_sha"]);
            Assert.AreEqual("tick limit", (string)summary["stopped"]);
            Assert.IsNotNull(summary["species"]["predator"]["kills"]);
            var info = JObject.Parse(File.ReadAllText(Path.Combine(folder, "run_info.json")));
            Assert.AreEqual(17, (int)info["seed"]);
            Assert.IsNotNull(info["settings"]["prey/Life/Litter/Litter/max"], "the full configuration (CFG-03)");
        }

        [Test, Description("T-OUT-02 (OUT-03): every animal's genome at every tick, rebuilt from events.jsonl and alleles.jsonl, equals the live state")]
        public void GenomesRebuiltFromFiles()
        {
            var live = new Dictionary<int, Dictionary<int, string>>();               // tick → id → genome
            var w = Run("out02", 300, b =>
            {
                b.Root.GetComponentsInChildren<LlmMutation>(true).ToList().ForEach(m => m.Rate = 0.5f);
                b.Phase<SnapshotPhase>(p => p.Live = live);
            });
            Assert.Greater(w.Mutations.Successes, 0, "mutations happened");
            string folder = w.Service<RunRecorder>().RunFolder;
            var alleles = Lines(Path.Combine(folder, "alleles.jsonl")).ToDictionary(a => (string)a["id"]);
            var genome = new Dictionary<int, string>();
            var alive = new HashSet<int>();
            var events = Lines(Path.Combine(folder, "events.jsonl")).ToList();
            int k = 0;
            for (int tick = 0; tick < 300; tick++)
            {
                for (; k < events.Count && (int)events[k]["t"] <= tick; k++)
                {
                    var e = events[k];
                    string kind = (string)e["kind"];
                    if (kind == "founder" || kind == "immigrant" || kind == "birth")
                    {
                        var ids = e["genome"].Select(x => (string)x).ToList();
                        foreach (var id in ids) Assert.IsTrue(alleles.ContainsKey(id), $"allele {id} is in alleles.jsonl");
                        genome[(int)e["id"]] = string.Join(",", ids.Select(id => (string)alleles[id]["text"]));
                        alive.Add((int)e["id"]);
                    }
                    else if (kind == "death") alive.Remove((int)e["id"]);
                }
                CollectionAssert.AreEquivalent(live[tick].Keys, alive, $"tick {tick}: the living animals");
                foreach (var kv in live[tick]) Assert.AreEqual(kv.Value, genome[kv.Key], $"tick {tick}: animal {kv.Key}");
            }
        }

        [Test, Description("T-OUT-03 (OUT-04, V-61): an Ollama brain and mutator read their key from OLLAMA_API_KEY and send it; the key appears in no run file, no log line (HTTP failures included) and no asset")]
        public void NoSecrets()
        {
            const string secret = "sk-evosim-test-SECRET-1234567890";
            var logs = new List<string>();
            Application.LogCallback grab = (text, stack, type) => { lock (logs) logs.Add(text + " | " + stack); };
            Application.logMessageReceivedThreaded += grab;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Environment.SetEnvironmentVariable("OLLAMA_API_KEY", secret);
            try
            {
                int brainCalls = 0, mutatorCalls = 0;
                var brainServer = FakeHttpTransport.OllamaPoints();
                brainServer.FailWith = (path, body) => path == "/api/chat" && System.Threading.Interlocked.Increment(ref brainCalls) % 5 == 0 ? 500 : 0;
                string[] mutants = { "Eat when the sun is high.", "Run from anything that moves.", "Stay near cover at night.", "Rest after every meal.", "Follow the oldest animal." };
                var mutatorServer = new FakeHttpTransport
                {
                    Respond = (method, path, body) => path == "/api/tags"
                        ? new JObject { ["models"] = new JArray(new JObject { ["name"] = "fake", ["digest"] = "feedfacecafe0002" }) }
                        : FakeHttpTransport.Chat(mutants[Hashing.Sha256Hex(FakeHttpTransport.Prompt(body), 2)[0] % mutants.Length]),
                    FailWith = (path, body) => path == "/api/chat" && System.Threading.Interlocked.Increment(ref mutatorCalls) % 4 == 0 ? 503 : 0,
                };
                var w = Run("out03", 600, b =>                                              // founders mature at 150: births, so mutations
                {
                    b.DefaultBrain<OllamaPointsBrain>(o =>
                    {
                        o.Transport = brainServer;
                        o.ConfigureClient("http://ollama.test", 5f, 1, 0f, 2, "OLLAMA_API_KEY");
                        o.Strict = false;                                               // a failed call falls back, the run goes on
                    });
                    var fake = b.Get<FakeMutator>();
                    var go = fake.gameObject;
                    UnityEngine.Object.DestroyImmediate(fake);
                    var client = go.AddComponent<OllamaMutatorClient>();
                    client.Transport = mutatorServer;
                    client.ConfigureClient("http://mutator.test", 5f, 1, 0f, 2, "OLLAMA_API_KEY");
                    foreach (var m in b.Root.GetComponentsInChildren<LlmMutation>(true)) m.Rate = 0.5f;
                });
                Assert.IsTrue(brainServer.Requests.Any(r => r.ApiKey == secret), "the brain read the key from the environment and sent it");
                Assert.IsTrue(mutatorServer.Requests.Any(r => r.Path == "/api/chat" && r.ApiKey == secret), "so did the mutator");
                Assert.Greater(w.AllSpecies.Sum(s => s.Counters.Failures), 0, "some brain calls failed, so failures were handled");
                string folder = w.Service<RunRecorder>().RunFolder;
                foreach (var f in Directory.GetFiles(folder))
                    Assert.IsFalse(File.ReadAllText(f).Contains(secret), Path.GetFileName(f));
                lock (logs) foreach (var l in logs) Assert.IsFalse(l.Contains(secret), "log: " + l);
                if (File.Exists(Application.consoleLogPath))
                    using (var stream = new FileStream(Application.consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                        Assert.IsFalse(reader.ReadToEnd().Contains(secret), "the editor log");
                foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "EvoSim"), "*", SearchOption.AllDirectories))
                    if (f.EndsWith(".asset") || f.EndsWith(".unity") || f.EndsWith(".prefab")) Assert.IsFalse(File.ReadAllText(f).Contains(secret), f);
            }
            finally
            {
                Application.logMessageReceivedThreaded -= grab;
                Environment.SetEnvironmentVariable("OLLAMA_API_KEY", null);
            }
        }

        [Test, Description("RAND-20, OUT-03: destroying a running World (leaving Play mode) stops the run, so alleles.jsonl, final_population.json and summary.json are written")]
        public void LeavingPlayWritesEverything()
        {
            const string name = "leave";
            string root = Path.Combine(Root, name);
            if (Directory.Exists(root)) Directory.Delete(root, true);
            var b = New(17, name: name).Lab1(prey: 30, predators: 5).Recorder(true, "Logs/EvoSim/test-runs", name);
            var w = b.Build();
            w.Advance(120);
            string folder = w.Service<RunRecorder>().RunFolder;
            UnityEngine.Object.DestroyImmediate(b.Root);
            foreach (var f in new[] { "events.jsonl", "stats.csv", "alleles.jsonl", "final_population.json", "summary.json", "run_info.json" })
                Assert.IsTrue(File.Exists(Path.Combine(folder, f)), f);
            var summary = JObject.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
            Assert.AreEqual("world disabled", (string)summary["stopped"]);
            Assert.AreEqual(120, (int)summary["ticks"]);
            var alleles = File.ReadAllLines(Path.Combine(folder, "alleles.jsonl")).Where(l => l.Length > 0).Select(l => (string)JObject.Parse(l)["id"]).ToList();
            foreach (var line in File.ReadAllLines(Path.Combine(folder, "events.jsonl")).Where(l => l.Contains("\"genome\"")))
                foreach (var id in JObject.Parse(line)["genome"]) CollectionAssert.Contains(alleles, (string)id, "every genome's alleles are listed (OUT-03)");
        }

        [Test, Description("OUT-05: the compatibility mode writes prey events without a species field, unprefixed prey loci, predator events with species, kills as \"predator\"")]
        public void CompatibilityMode()
        {
            var w = Run("out05", 300, compatibility: true);
            string folder = w.Service<RunRecorder>().RunFolder;
            var events = Lines(Path.Combine(folder, "events.jsonl")).ToList();
            var preyFounder = events.First(e => (string)e["kind"] == "founder" && e["species"] == null);
            StringAssert.StartsWith("eat:", (string)preyFounder["genome"][0]);
            var predFounder = events.First(e => (string)e["kind"] == "founder" && (string)e["species"] == "predator");
            StringAssert.StartsWith("predator.hunt:", (string)predFounder["genome"][0]);
            Assert.IsFalse(events.Any(e => (string)e["cause"] == "killed"));
            var header = File.ReadAllLines(Path.Combine(folder, "stats.csv"))[0];
            StringAssert.StartsWith("t,pop,mean_energy", header);
            StringAssert.Contains("pred_pop", header);
            var summary = JObject.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
            Assert.IsNotNull(summary["predators"], "predators under their own key, like the prototype");
            Assert.IsNotNull(summary["pop_final"]);
            var alleles = Lines(Path.Combine(folder, "alleles.jsonl")).ToList();
            Assert.IsTrue(alleles.Any(a => (string)a["id"] == "eat:0"));
        }

        [Test, Description("T-RAND-06, R-05 (RAND-20, RAND-21, MUT-14, DEC-33): stop at tick 300 with a stop file, run the same command again → the first 300 ticks replay with 0 model calls and the same events")]
        public void ResumeByReplay()
        {
            string cache = Path.Combine(Application.temporaryCachePath, "evosim-replay-cache");
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
            string stop = Path.Combine(Application.temporaryCachePath, "evosim-STOP");
            if (File.Exists(stop)) File.Delete(stop);

            World Make(string name, out ScriptedBrain brain, out FakeMutator mutator)
            {
                ScriptedBrain b = null;
                var builder = New(23, name: name).Lab1(prey: 30, predators: 5, randomBrain: false)
                    .Service<AnswerCache>("Answer cache", c => c.SetFolder(cache))
                    .DefaultBrain<ScriptedBrain>(x => { x.UsesCache = true; b = x; })
                    .Configure(w => { w.StopFile = stop; w.TickLimit = 600; });
                builder.Root.GetComponentsInChildren<LlmMutation>(true).ToList().ForEach(m => m.Rate = 0.3f);
                builder.Root.GetComponentInChildren<MutatorService>(true).Configure("fake", 1.2f, cached: true);
                var world = builder.Build();
                brain = b;
                mutator = world.GetComponentInChildren<FakeMutator>();
                return world;
            }

            var first = Make("first", out var brain1, out var mutator1);
            while (first.Tick < 300) first.Advance(1);
            File.WriteAllText(stop, "");
            first.Advance(1);
            File.Delete(stop);
            Assert.AreEqual(RunState.Stopped, first.State);
            Assert.AreEqual("stop file", first.StopReason);
            Assert.AreEqual(300, first.Tick, "stopped cleanly at the tick boundary");
            Assert.Greater(brain1.Calls, 0);
            Assert.Greater(mutator1.Calls, 0);
            string hash300 = first.Events.Hash;

            var again = Make("again", out var brain2, out var mutator2);
            while (again.Tick < 300) again.Advance(1);
            Assert.AreEqual(hash300, again.Events.Hash, "the same events");
            Assert.AreEqual(0, brain2.Calls, "decisions replayed from the answer cache");
            Assert.AreEqual(0, mutator2.Calls, "mutations replayed from the cache");
            again.Advance(100);
            Assert.Greater(brain2.Calls, 0, "then the run continues with new calls");
        }
    }
}
