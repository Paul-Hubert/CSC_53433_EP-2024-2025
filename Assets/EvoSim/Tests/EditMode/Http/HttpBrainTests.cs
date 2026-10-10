using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>HTTP brains against fake transports (08 §5–§6, 30 §2): request formats, rows, failures, retries, caches, wait modes.</summary>
    public class HttpBrainTests : WorldFixture
    {
        const string PredatorQuestion = "Which action does this predator take now?";
        static TextAsset Model(string file) => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/" + file);
        static JObject HeadJson => JObject.Parse(Model("decision_head.json").text);
        static double ChoiceT => (double)JObject.Parse(Model("calibration.json").text)["per_kind"]["choice"];

        static FakeHttpTransport FakeJev(int delayMs = 0) => FakeHttpTransport.Jev(delayMs);

        static double[] Expected(string text, int n)
        {
            var head = HeadJson;
            int start = (int)head["slots"]["ranges"]["choice"][0];
            var z = new double[n];
            for (int k = 0; k < n; k++) z[k] = (FakeHttpTransport.FakeLogProbability(text, k) + (double)head["bias"][start + k]) / ChoiceT;
            double max = z.Max(), sum = 0;
            for (int k = 0; k < n; k++) { z[k] = Math.Exp(z[k] - max); sum += z[k]; }
            return z.Select(x => x / sum).ToArray();
        }

        World JevWorld(FakeHttpTransport fake, int seed = 5, WaitMode mode = WaitMode.Freeze, string cache = null, Action<JevBrain> more = null, string name = "jev")
        {
            var b = New(seed, mode, name).Lab1(prey: 20, predators: 4, randomBrain: false);
            if (cache != null) b.Service<AnswerCache>("Answer cache", c => c.SetFolder(cache));
            b.DefaultBrain<JevBrain>(j =>
            {
                j.SetModelFiles(Model("decision_head.json"), Model("calibration.json"));
                j.Transport = fake;
                j.ConfigureClient("http://jev.test", 5f, 3, 0f, 4);
                j.SetQuestion("predator", PredatorQuestion);
                more?.Invoke(j);
            });
            return b.Build();
        }

        static string TempFolder(string name)
        {
            string dir = Path.Combine(Application.temporaryCachePath, name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            return dir;
        }

        [Test, Description("08 §6 JEV: one /v1/completions request per batch and option count, max_tokens 1, the option letters' ids as allowed tokens, log-probabilities; rows = softmax((log-prob + bias) / T)")]
        public void JevRequests()
        {
            var fake = FakeJev();
            var w = JevWorld(fake);
            w.Advance(1);
            var posts = fake.Requests.Where(r => r.Method == "POST").ToList();
            Assert.AreEqual(2, posts.Count, "one request for the 6-option prey, one for the 4-option predators");
            var ids = HeadJson["verbalizer_ids"].Select(x => (int)x).ToList();
            var expectedRows = new List<double[]>();
            foreach (var (_, path, body, _) in posts)
            {
                Assert.AreEqual("/v1/completions", path);
                int n = (int)body["logprobs"];
                Assert.AreEqual("jev-decision", (string)body["model"]);
                Assert.AreEqual(1, (int)body["max_tokens"]);
                Assert.AreEqual(1.0, (double)body["temperature"]);
                Assert.IsFalse((bool)body["add_special_tokens"]);
                Assert.IsTrue((bool)body["return_tokens_as_token_ids"]);
                CollectionAssert.AreEqual(ids.Skip(8).Take(n).ToList(), body["allowed_token_ids"].Select(x => (int)x).ToList(), "A, B, C… of the head");
                var texts = body["prompt"].Select(x => (string)x).ToList();
                CollectionAssert.AllItemsAreUnique(texts, "identical keys are sent once (DEC-32)");
                foreach (var text in texts)
                {
                    StringAssert.StartsWith("[kind] choice\n[state] You decide what ", text);
                    StringAssert.EndsWith("\n[decision]:", text);
                    StringAssert.DoesNotContain("Distribute 100 points", text, "the state has no answer instruction");
                    if (n == 6)
                    {
                        StringAssert.Contains("\n[question] Which action does this animal take now?\n[options]\nA) eat\nB) flee\nC) hide\nD) follow\nE) rest\nF) mate\n[decision]:", text);
                    }
                    else
                    {
                        Assert.AreEqual(4, n);
                        StringAssert.Contains("\n[question] " + PredatorQuestion + "\n[options]\nA) hunt\nB) follow\nC) rest\nD) mate\n[decision]:", text);
                    }
                    expectedRows.Add(Expected(text, n));
                }
            }
            Assert.AreEqual(expectedRows.Count, w.Decisions.ModelCalls, "one forward pass per distinct query");
            foreach (var s in w.AllSpecies)
                foreach (var a in s.Animals)
                {
                    Assert.IsNull(BrainAnswer.Check(a.LastProbabilities, s.Actions.Count));
                    Assert.IsTrue(expectedRows.Any(e => e.Length == a.LastProbabilities.Length &&
                                                        e.Select((p, i) => Math.Abs(p - a.LastProbabilities[i])).Max() < 1e-6),
                                  "each row is the head's softmax of its text's log-probabilities");
                }
        }

        [Test, Description("08 §6: the JEV text of the 08 §7 example → pinned (Tests/Golden/jev_text.txt); the state is the prompt without the ask line")]
        public void JevTextGolden()
        {
            var w = New().Ecology(48f).ReferencePhases()
                .DefaultBrain<JevBrain>(j => j.SetModelFiles(Model("decision_head.json"), Model("calibration.json"))).Build();
            var prey = w.FindSpecies("prey");
            var set = new ObservationSet();
            set.Add(new Dictionary<string, string>
            {
                { "energy", "low" }, { "stamina", "high" }, { "food", "close" }, { "predator", "medium" }, { "cover", "close" }, { "animal", "none" }, { "age", "adult" },
            });
            var o = set.ToObservation(set.Rows[0], prey);
            var genome = Place.Animal(prey, Place.At(1, 1)).Genome;
            var genes = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < prey.Genes.Count; i++) genes.Add(new KeyValuePair<string, string>(prey.Genes[i].Label, genome[i].Text));
            var q = new DecisionQuery(prey, genes, o, prey.Describe(o, w.TextStyle), w.TextStyle, genome.BrainKey);
            var jev = w.GetComponentInChildren<JevBrain>();
            Golden.Check("jev_text.txt", jev.TextFor(q) + "\n");
        }

        [Test, Description("08 §6: the head's row — equal log-probabilities give softmax(bias / T); an option missing from the answer gets −1e9, so ~0")]
        public void JevHeadRows()
        {
            var head = JevHead.Parse(Model("decision_head.json").text, Model("calibration.json").text);
            Assert.AreEqual(16, head.MaxOptions);
            Assert.AreEqual(ChoiceT, head.Temperature);
            var ids = head.ChoiceIds(3);
            var row = head.Row(new Dictionary<int, double> { { ids[0], -1.0 }, { ids[1], -1.0 } }, 3);
            Assert.IsNull(BrainAnswer.Check(row, 3));
            Assert.Less(row[2], 1e-6f);
            Assert.AreEqual(0.5, row[0], 1e-3);
            Assert.Throws<ArgumentException>(() => head.ChoiceIds(17));
            Assert.AreEqual(16, new GameObject("j").AddComponent<JevBrain>().MaxActions, "DEC-14: at most 16 options (A–P)");
        }

        [Test, Description("08 §6 points mode: one /api/chat call per distinct query with the JSON schema, temperature 0, the seed, num_ctx, thinking off; the digest from /api/tags is part of the cache key")]
        public void OllamaPointsRequests()
        {
            var fake = new FakeHttpTransport();
            fake.Respond = (method, path, body) => path == "/api/tags"
                ? new JObject { ["models"] = new JArray(new JObject { ["name"] = "gemma4:12b", ["digest"] = "0123456789abcdef0123" }) }
                : FakeHttpTransport.Chat("{\"eat\": 50, \"flee\": 30, \"hide\": 0, \"follow\": 10, \"rest\": 10, \"mate\": 0, \"hunt\": 60}");
            var w = New(9).Lab1(prey: 10, predators: 2, randomBrain: false)
                .DefaultBrain<OllamaPointsBrain>(o => { o.Transport = fake; o.ConfigureClient("http://ollama.test", 5f, 3, 0f, 2); }).Build();
            var brain = w.GetComponentInChildren<OllamaPointsBrain>();
            Assert.AreEqual("gemma4:12b@0123456789ab", brain.ModelIdentity);
            w.Advance(1);
            var chats = fake.Requests.Where(r => r.Path == "/api/chat").ToList();
            Assert.AreEqual(w.Decisions.ModelCalls, chats.Count, "one chat call per distinct query");
            Assert.LessOrEqual(fake.MaxInFlight, 2, "the parallel cap");
            foreach (var (_, _, body, _) in chats)
            {
                Assert.AreEqual("gemma4:12b", (string)body["model"]);
                Assert.IsFalse((bool)body["stream"]);
                Assert.IsFalse((bool)body["think"]);
                Assert.AreEqual(4096, (int)body["options"]["num_ctx"]);
                Assert.AreEqual(0, (int)body["options"]["seed"]);
                Assert.AreEqual(0, (int)body["options"]["temperature"]);
                StringAssert.EndsWith(OllamaPointsBrain.Instruction, FakeHttpTransport.Prompt(body));
                var actions = ((JArray)body["format"]["required"]).Select(x => (string)x).ToList();
                Assert.IsTrue(actions.SequenceEqual(new[] { "eat", "flee", "hide", "follow", "rest", "mate" }) || actions.SequenceEqual(new[] { "hunt", "follow", "rest", "mate" }));
                Assert.AreEqual(100, (int)body["format"]["properties"][actions[0]]["maximum"]);
            }
            var preyRow = w.FindSpecies("prey").Animals[0].LastProbabilities;
            double total = 100 + 6 * 0.01;
            Assert.AreEqual((50 + 0.01) / total, preyRow[0], 1e-6);
            Assert.AreEqual(0.01 / total, preyRow[2], 1e-6);
        }

        [Test, Description("T-DEC-10 (DEC-41): points {eat: 120, flee: −5}, all zeros, a missing action → repaired; text or non-numbers → failures")]
        public void PointsRepair()
        {
            var actions = new[] { "eat", "flee", "rest" };
            var row = PointsAnswer.ToRow("{\"eat\": 120, \"flee\": -5, \"rest\": 0}", actions, out var problem);
            Assert.IsNull(problem);
            Assert.IsNull(BrainAnswer.Check(row, 3));
            Assert.AreEqual(120.01 / 120.03, row[0], 1e-6);
            Assert.AreEqual(0.01 / 120.03, row[1], 1e-6, "negative points count 0");
            foreach (var p in PointsAnswer.ToRow("{\"eat\": 0, \"flee\": 0, \"rest\": 0}", actions, out _)) Assert.AreEqual(1.0 / 3, p, 1e-6, "all zeros → uniform");
            var missing = PointsAnswer.ToRow("{\"eat\": 10}", actions, out problem);
            Assert.IsNull(problem);
            Assert.AreEqual(0.01 / 10.03, missing[2], 1e-6, "a missing action counts 0");
            Assert.AreEqual(5.01 / 5.03, PointsAnswer.ToRow("{\"eat\": \"5\"}", actions, out _)[0], 1e-6, "a numeric string counts, as int() did");
            Assert.IsNull(PointsAnswer.ToRow("eat: 50", actions, out problem));
            StringAssert.Contains("JSON", problem);
            Assert.IsNull(PointsAnswer.ToRow("{\"eat\": \"lots\"}", actions, out problem));
            StringAssert.Contains("eat", problem);
        }

        [Test, Description("T-DEC-09 over HTTP (DEC-34, DEC-40, RAND-20): a server failing every call; strict → the run stops cleanly with its files; non-strict → uniform rows, counted, nothing in memo or cache")]
        public void FailingServer()
        {
            var fake = new FakeHttpTransport { FailWith = (path, body) => 503 };
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "EvoSim", "test-runs", "http-strict");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            var strict = New(3, name: "http-strict").Lab1(prey: 10, predators: 2, randomBrain: false)
                .Recorder(true, "Logs/EvoSim/test-runs", "http-strict")
                .DefaultBrain<JevBrain>(j => { j.SetModelFiles(Model("decision_head.json"), Model("calibration.json")); j.Transport = fake; j.ConfigureClient("http://jev.test", 5f, 3, 0f, 4); })
                .Build();
            strict.Advance(5);
            Assert.AreEqual(RunState.Stopped, strict.State);
            StringAssert.StartsWith("brain failure: jev", strict.StopReason);
            Assert.AreEqual(0, strict.Tick, "stopped before the tick changed anything");
            var brain = strict.GetComponentInChildren<JevBrain>();
            Assert.AreEqual(2 * 3, brain.Stats.Tries, "2 requests × 3 tries");
            Assert.AreEqual(2, brain.Stats.Failures);
            StringAssert.Contains("brain failure", File.ReadAllText(Path.Combine(folder, "summary.json")));

            string cache = TempFolder("evosim-http-fail-cache");
            var soft = JevWorld(new FakeHttpTransport { FailWith = (path, body) => 500 }, cache: cache, more: j => j.Strict = false, name: "http-soft");
            soft.Advance(3);
            Assert.AreEqual(3, soft.Tick, "the run goes on");
            Assert.Greater(soft.FindSpecies("prey").Counters.Failures, 0);
            foreach (var a in soft.FindSpecies("prey").Animals) foreach (var p in a.LastProbabilities) Assert.AreEqual(1.0 / 6, p, 1e-6);
            Assert.AreEqual(0, soft.Decisions.Memo.Count, "never in the memo");
            Assert.AreEqual(0, soft.GetComponentInChildren<AnswerCache>().Stores, "never in the cache");
        }

        static JToken Sync(Func<Task<JToken>> call) => Task.Run(call).GetAwaiter().GetResult();

        [Test, Description("DEC-42: tries with doubling waits, a time-out per try, no retry on a client error (4xx), the parallel cap, the key from its environment variable only")]
        public void ClientBehaviour()
        {
            var fake = new FakeHttpTransport { FailWith = (path, body) => path == "/bad" ? 400 : path == "/down" ? 503 : 0 };
            var c = new HttpCaller("http://x.test", "", 5f, 3, 0.05f, 4, fake);
            var clock = Stopwatch.StartNew();
            var e = Assert.Throws<HttpFailure>(() => Sync(() => c.PostAsync("/down", new JObject())));
            Assert.AreEqual(3, fake.Requests.Count, "3 tries");
            Assert.GreaterOrEqual(clock.Elapsed.TotalSeconds, 0.14, "waits of 0.05 then 0.1 s");
            StringAssert.Contains("after 3 tries", e.Message);
            fake.Requests.Clear();
            Assert.Throws<HttpFailure>(() => Sync(() => c.PostAsync("/bad", new JObject())));
            Assert.AreEqual(1, fake.Requests.Count, "a 400 isn't tried again");

            var hang = new FakeHttpTransport { Hang = (path, body) => true };
            var slow = new HttpCaller("http://x.test", "", 0.1f, 2, 0f, 4, hang);
            e = Assert.Throws<HttpFailure>(() => Sync(() => slow.GetAsync("/v1/models")));
            StringAssert.Contains("no answer in 0.1 s", e.Message);
            Assert.AreEqual(2, slow.Stats.Timeouts);

            var busy = new FakeHttpTransport { DelayMs = 40 };
            var capped = new HttpCaller("http://x.test", "", 5f, 1, 0f, 2, busy);
            Task.Run(() => Task.WhenAll(Enumerable.Range(0, 8).Select(i => capped.PostAsync("/p", new JObject { ["i"] = i })))).GetAwaiter().GetResult();
            Assert.AreEqual(2, busy.MaxInFlight, "at most 2 in flight");

            var keyed = new FakeHttpTransport();
            Environment.SetEnvironmentVariable("EVOSIM_TEST_KEY", "test-key-value");
            try
            {
                Sync(() => new HttpCaller("http://x.test", "EVOSIM_TEST_KEY", 5f, 1, 0f, 1, keyed).PostAsync("/p", new JObject()));
                Assert.AreEqual("test-key-value", keyed.Requests[0].ApiKey, "sent as the bearer token");
                StringAssert.DoesNotContain("test-key-value", keyed.Requests[0].Body.ToString());
            }
            finally { Environment.SetEnvironmentVariable("EVOSIM_TEST_KEY", null); }
            Sync(() => new HttpCaller("http://x.test", "", 5f, 1, 0f, 1, keyed).PostAsync("/p", new JObject()));
            Assert.IsNull(keyed.Requests[1].ApiKey);
        }

        [Test, Description("V-61 (OUT-04): a key in the key-variable field or in the address is an error; a variable name is fine")]
        public void KeysOnlyInEnvironment()
        {
            string Check(string host, string variable)
            {
                var j = new GameObject("jev").AddComponent<JevBrain>();
                j.SetModelFiles(Model("decision_head.json"), Model("calibration.json"));
                j.ConfigureClient(host, 5f, 1, 0f, 1, variable);
                var report = new ValidationReport();
                j.Validate(report);
                UnityEngine.Object.DestroyImmediate(j.gameObject);
                return string.Join(",", report.Messages.Select(m => m.Id));
            }
            Assert.AreEqual("", Check("http://localhost:8000", "JEV_API_KEY"));
            Assert.AreEqual("V-61", Check("http://localhost:8000", "sk-123456789"));
            Assert.AreEqual("V-61", Check("http://user:secret@localhost:8000", ""));
            Assert.AreEqual("V-20", Check("localhost:8000", ""));
        }

        [Test, Description("SPACE-13, SPACE-14, DEC-12, DEC-33 over HTTP: answers arriving late on other threads give the same events in Freeze and Responsive modes; a second run replays from the answer cache with no request")]
        public void WaitModesAndReplay()
        {
            string Run(WaitMode mode, string cache, out int requests, int delay)
            {
                var fake = FakeJev(delay);
                var w = JevWorld(fake, 21, mode, cache, name: $"jev-{mode}-{delay}");
                int calls = 0;
                while (w.Tick < 40) { w.Advance(1); calls++; }
                requests = fake.Requests.Count(r => r.Path == "/v1/completions");
                if (mode == WaitMode.Responsive && requests > 0) Assert.Greater(calls, 40, "Responsive: some calls returned while the answers were out");
                return w.Events.Hash;
            }
            string cacheDir = TempFolder("evosim-http-replay-cache");
            string freeze = Run(WaitMode.Freeze, null, out int r1, 0);
            Assert.Greater(r1, 0);
            Assert.AreEqual(freeze, Run(WaitMode.Responsive, null, out _, 15), "Freeze = Responsive");
            Assert.AreEqual(freeze, Run(WaitMode.Freeze, cacheDir, out int r2, 0), "the cache doesn't change answers");
            Assert.AreEqual(r1, r2);
            Assert.AreEqual(freeze, Run(WaitMode.Responsive, cacheDir, out int r3, 15));
            Assert.AreEqual(0, r3, "replayed from the cache with zero calls");
        }

        [Test, Description("DEC-30, DEC-33 (13 §4): a run replayed from the answer cache counts like the live run — per species the same decisions, brain queries after the memo and memo hits — and its model calls become cache hits")]
        public void ReplayCountsLikeTheLiveRun()
        {
            string cacheDir = TempFolder("evosim-replay-counters");
            World Run(string name)
            {
                var w = JevWorld(FakeJev(), 23, WaitMode.Freeze, cacheDir, name: name);
                while (w.Tick < 120) w.Advance(1);
                return w;
            }
            var live = Run("jev-live");
            var replay = Run("jev-replay");
            Assert.AreEqual(live.Events.Hash, replay.Events.Hash);
            Assert.Greater(live.Decisions.ModelCalls, 0);
            Assert.AreEqual(0, replay.Decisions.ModelCalls, "the replay asks no model");
            Assert.Greater(replay.Decisions.CacheHits, 0);
            foreach (var s in live.AllSpecies)
            {
                var r = replay.FindSpecies(s.Id).Counters;
                Assert.Greater(s.Counters.MemoHits, 0, s.Id + ": the memo was used, so the comparison means something");
                Assert.AreEqual(s.Counters.Decisions, r.Decisions, s.Id + " decisions");
                Assert.AreEqual(s.Counters.BrainQueries, r.BrainQueries, s.Id + " brain queries after the memo (stats backend_queries)");
                Assert.AreEqual(s.Counters.MemoHits, r.MemoHits, s.Id + " memo hits (summary memo_hit_rate)");
            }
        }
    }
}
