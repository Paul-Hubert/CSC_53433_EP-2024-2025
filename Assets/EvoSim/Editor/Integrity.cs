using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EvoSim
{
    /// <summary>
    /// Brain and mutator integrity checks against the real servers (32 §2): B-01 smoke and B-11 blindness so far; the
    /// gates come with M11. Command line: -executeMethod EvoSim.Integrity.RunIntegrity [-checks B-01,B-11]
    /// [-scene path] [-brain JEV]; in the open editor: Integrity.RunNamed through the unity CLI's eval.
    /// </summary>
    public static class Integrity
    {
        public const string DefaultScene = "Assets/EvoSim/Scenes/Lab1_Small.unity";
        static readonly string[] MutatorFields = { "chat_template_kwargs", "max_tokens", "messages", "model", "options", "seed", "stream", "temperature", "think" };

        /// <summary>Command-line entry point; exits 1 when a blocking check fails.</summary>
        public static void RunIntegrity()
        {
            int code = 0;
            try
            {
                var reports = Run(Arg("-checks") ?? "B-01,B-11", Arg("-scene") ?? DefaultScene, Arg("-brain") ?? "JEV", false);
                foreach (var r in reports)
                {
                    Debug.Log("EvoSim integrity: " + r.Line);
                    if (!r.Passed && r.Blocking) code = 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 2;
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        /// <summary>The same from the open editor: one line per check.</summary>
        public static string RunNamed(string checks = "B-01,B-11", string scene = DefaultScene, string brain = "JEV", bool cpuOnly = false) =>
            string.Join("\n", Run(checks, scene, brain, cpuOnly).Select(r => r.Line));

        static List<IntegrityReport> Run(string checks, string scene, string brain, bool cpuOnly)
        {
            var reports = new List<IntegrityReport>();
            foreach (var raw in checks.Split(','))
            {
                string id = raw.Trim();
                if (id == "B-01") reports.Add(Smoke(scene, brain, cpuOnly));
                else if (id == "B-11") reports.Add(Blindness(scene));
                else
                {
                    var r = new IntegrityReport(id, "not implemented yet (M11)", false);
                    r.Fail("not implemented yet");
                    reports.Add(r);
                }
            }
            return reports;
        }

        /// <summary>
        /// B-01 smoke: one query per species with founder genes and three situations (every sense at its first, middle and
        /// last token), through the scene's real prompt and brain; every row valid; latency recorded. Blocking.
        /// </summary>
        public static IntegrityReport Smoke(string scenePath, string brainName, bool cpuOnly = false)
        {
            var report = new IntegrityReport("B-01", "Smoke", true);
            Batch.WithScene(scenePath, world =>
            {
                var brain = FindBrain(world, brainName);
                if (brain == null) { report.Fail($"no brain named {brainName} in {scenePath}"); return ""; }
                if (cpuOnly && brain is OllamaPointsBrain points) points.CpuOnly = true;
                Prepare(world, brain);
                world.NoMutation = true;
                if (!world.Initialize()) { report.Fail("validation: " + world.LastReport); return ""; }
                report.Facts["scene"] = scenePath;
                report.Facts["brain"] = brain.Id;
                report.Facts["model"] = brain.ModelIdentity;
                report.Facts["mode"] = brain.Mode;
                report.Facts["style"] = world.TextStyle.ToString();
                if (brain is HttpBrain http) report.Facts["host"] = http.Host;
                foreach (var s in world.AllSpecies)
                {
                    report.Facts["prompt " + s.Id] = s.Prompt.Id;
                    if (s.Animals.Count == 0) { report.Fail($"{s.Id}: no founder"); continue; }
                    var genome = s.Animals[0].Genome;
                    var queries = new List<DecisionQuery>();
                    for (int which = 0; which < 3; which++)
                    {
                        var o = Situation(s, which);
                        queries.Add(new DecisionQuery(s, DecisionQuery.BrainGenes(s, genome), o, s.Describe(o, world.TextStyle), world.TextStyle, genome.BrainKey));
                    }
                    var clock = Stopwatch.StartNew();
                    var answer = brain.Ask(queries);
                    answer.Pending.Wait();
                    double seconds = clock.Elapsed.TotalSeconds;
                    for (int i = 0; i < queries.Count; i++)
                    {
                        string problem = answer.Failed(i) ? answer.Error(i) : BrainAnswer.Check(answer.Row(i), s.Actions.Count);
                        var row = report.Row();
                        row["species"] = s.Id;
                        row["situation"] = queries[i].Situation;
                        row["row"] = answer.Failed(i) ? null : answer.Row(i).Select(p => Math.Round(p, 3)).ToArray();
                        row["actions"] = string.Join(" ", HttpBrain.ActionNames(s));
                        row["batch_seconds"] = Math.Round(seconds, 3);
                        row["valid"] = problem == null;
                        if (problem != null) report.Fail($"{s.Id}, situation {i + 1}: {problem}");
                    }
                }
                if (brain is HttpBrain h)
                {
                    report.Facts["tries"] = h.Stats.Tries;
                    report.Facts["mean_latency_s"] = h.Stats.Latencies.Count > 0 ? Math.Round(h.Stats.Latencies.Average(), 3) : 0.0;
                }
                if (brain is JevBrain jev) report.Facts["too_long"] = jev.TooLong;
                return "";
            });
            report.Write();
            return report;
        }

        /// <summary>
        /// B-11 blindness: the prompts the mutator model actually receives during a run (every text gene mutating, cache
        /// off) hold only the context line, one deck instruction, the sentence and the reply line (MUT-11, CORE-07).
        /// </summary>
        public static IntegrityReport Blindness(string scenePath, int requests = 12, int maxTicks = 400)
        {
            var report = new IntegrityReport("B-11", "Blindness", false);
            Batch.WithScene(scenePath, world =>
            {
                Prepare(world, world.GetComponentInChildren<RandomBrain>(true));
                world.NoMutation = false;
                var client = world.GetComponentInChildren<HttpMutatorClient>(true);
                var service = world.GetComponentInChildren<MutatorService>(true);
                if (client == null || service == null) { report.Fail("no HTTP mutator client or mutator service in the scene"); return ""; }
                var recorder = new RecordingTransport(client.Transport);
                client.Transport = recorder;
                service.Configure(service.Model, service.Temperature, cached: false);              // every prompt reaches the model
                var operators = world.GetComponentsInChildren<LlmMutation>(true);
                foreach (var m in operators) m.Rate = 1f;
                if (!world.Initialize()) { report.Fail("validation: " + world.LastReport); return ""; }
                report.Facts["scene"] = scenePath;
                report.Facts["mutator"] = client.ModelIdentity;
                report.Facts["host"] = client.Host;
                report.Facts["temperature"] = service.Temperature;

                var instructions = new HashSet<string>(operators.SelectMany(m => m.Instructions));
                var contexts = new HashSet<string>(operators.Select(m => m.ContextLine));
                int Chats() => recorder.Calls.Count(c => c.Method == "POST");
                while (world.State != RunState.Stopped && world.Tick < maxTicks && Chats() < requests) world.Advance(1);
                world.Advance(1);                                                                   // let the last round come back

                int checkedCount = 0;
                foreach (var call in recorder.Calls.Where(c => c.Method == "POST"))
                {
                    checkedCount++;
                    var body = JObject.Parse(call.Body);
                    var problems = new List<string>();
                    foreach (var p in body.Properties()) if (!MutatorFields.Contains(p.Name)) problems.Add("extra field " + p.Name);
                    var messages = body["messages"] as JArray;
                    string prompt = messages != null && messages.Count == 1 && (string)messages[0]["role"] == "user" ? (string)messages[0]["content"] : null;
                    if (prompt == null) problems.Add("not exactly one user message");
                    var m = prompt == null ? Match.Empty : Regex.Match(prompt, "^(?:(?<context>[^\\n]+)\\n)?(?<instruction>[^\\n]+)\\n\\n\"(?<sentence>[^\\n\"]+)\"\\n\\n" + Regex.Escape(MutationText.ReplyLine) + "$");
                    if (prompt != null && !m.Success) problems.Add("not the MUT-11 layout");
                    if (m.Success)
                    {
                        if (m.Groups["context"].Success && !contexts.Contains(m.Groups["context"].Value)) problems.Add("an unknown first line");
                        if (!instructions.Contains(m.Groups["instruction"].Value)) problems.Add("an instruction not in the deck");
                        if (m.Groups["sentence"].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 12) problems.Add("a sentence of more than 12 words");
                    }
                    var row = report.Row();
                    row["instruction"] = m.Success ? m.Groups["instruction"].Value : "";
                    row["sentence"] = m.Success ? m.Groups["sentence"].Value : "";
                    row["answer"] = call.Answer == null ? "(failed)" : Content(call.Answer);
                    row["blind"] = problems.Count == 0;
                    if (problems.Count > 0) report.Fail($"request {checkedCount}: {string.Join(", ", problems)}");
                }
                report.Facts["requests"] = checkedCount;
                report.Facts["ticks"] = world.Tick;
                if (checkedCount == 0) report.Fail("the mutator received no request");
                return "";
            });
            report.Write();
            return report;
        }

        static string Content(string answer)
        {
            try
            {
                var o = JObject.Parse(answer);
                return (string)o["message"]?["content"] ?? (string)o["choices"]?[0]?["message"]?["content"] ?? "";
            }
            catch (Exception) { return answer; }
        }

        static void Prepare(World world, Brain brain)
        {
            if (brain != null) world.DefaultBrain = brain;
            world.WaitMode = WaitMode.Freeze;
            world.StartOnPlay = false;
            world.Seed = 1234;
            foreach (var r in world.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
        }

        static Brain FindBrain(World world, string name)
        {
            foreach (var b in world.GetComponentsInChildren<Brain>(true))
                if (b.gameObject.name == name || b.Id == name) return b;
            return null;
        }

        /// <summary>Every sense at its first (0), middle (1) or last (2) token.</summary>
        static Observation Situation(Species s, int which)
        {
            var tokens = new int[s.Senses.Count];
            for (int k = 0; k < tokens.Length; k++)
            {
                int n = s.Senses[k].Tokens.Count;
                tokens[k] = which == 0 ? 0 : which == 1 ? n / 2 : n - 1;
            }
            return new Observation(tokens);
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
