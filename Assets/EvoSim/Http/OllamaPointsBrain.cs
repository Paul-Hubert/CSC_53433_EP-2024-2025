using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// LLM, points mode (08 §6): one Ollama chat call per distinct query; the prompt ends with the points instruction,
    /// the answer is constrained by a JSON schema, temperature 0, a fixed seed, a fixed context length, thinking off;
    /// points become a row by DEC-41.
    /// </summary>
    public class OllamaPointsBrain : HttpBrain
    {
        public const string Instruction = "Distribute 100 points across the actions according to how likely this animal is to choose each.";

        [SerializeField, Tooltip("The Ollama model tag (reference gemma4:12b), or a cloud model tag.")]
        string model = "gemma4:12b";
        [SerializeField, Tooltip("The fixed seed of every call (reference 0, DEC-12).")]
        int seed;
        [SerializeField, Min(256), Tooltip("Context length in tokens (reference 4 096; without it the server reloads the model).")]
        int contextLength = 4096;
        [SerializeField, Tooltip("Let thinking models reason before answering (reference off).")]
        bool think;
        [SerializeField, Tooltip("How long the server keeps the model loaded between calls (reference 30m).")]
        string keepAlive = "30m";
        [SerializeField, Tooltip("The model digest for cache keys (DEC-33); empty = asked from the server when the run begins.")]
        string digest = "";
        [SerializeField, Tooltip("Run the model on the CPU (num_gpu 0): slower, for machines whose GPU is busy (reference off).")]
        bool cpuOnly;

        string serverDigest;

        public OllamaPointsBrain()
        {
            host = "http://localhost:11434";
            maxParallel = 2;
        }

        public override string Id => "ollama-points";
        public override string Mode => "points";
        public override string AnswerInstruction => Instruction;
        public override int MaxPromptTokens => contextLength;
        public override string ModelIdentity => model + "@" + (digest.Length > 0 ? digest : serverDigest ?? "unknown");
        public string Model => model;
        public bool CpuOnly { get => cpuOnly; set => cpuOnly = value; }

        public void Configure(string modelTag, int fixedSeed = 0, int context = 4096, string pinnedDigest = "")
        {
            model = modelTag;
            seed = fixedSeed;
            contextLength = context;
            digest = pinnedDigest;
        }

        public override void Begin()
        {
            if (digest.Length > 0 || serverDigest != null || !UsedByASpecies()) return;
            serverDigest = Task.Run(() => OllamaTags.DigestAsync(new HttpCaller(host, apiKeyVariable, 5f, 1, 0f, 1, Wire), model))
                               .GetAwaiter().GetResult();
        }

        bool UsedByASpecies()
        {
            foreach (var s in World.AllSpecies) if (s.Brain == this) return true;
            return false;
        }

        /// <summary>The chat request for one prompt (also used by the golden test).</summary>
        public JObject Body(string prompt, IReadOnlyList<string> actions)
        {
            var body = new JObject
            {
                ["model"] = model,
                ["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = prompt }),
                ["stream"] = false,
                ["options"] = Options(),
                ["format"] = PointsAnswer.Schema(actions),
                ["think"] = think,
            };
            if (!string.IsNullOrEmpty(keepAlive)) body["keep_alive"] = keepAlive;
            return body;
        }

        JObject Options()
        {
            var o = new JObject();
            if (cpuOnly) o["num_gpu"] = 0;
            o["num_ctx"] = contextLength;
            o["seed"] = seed;
            o["temperature"] = 0;
            return o;
        }

        protected override async Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer)
        {
            var calls = new List<Task>(requests.Count);
            foreach (var r in requests) calls.Add(One(c, r, answer));
            await Task.WhenAll(calls).ConfigureAwait(false);
        }

        async Task One(HttpCaller c, BrainRequest r, BrainAnswer answer)
        {
            try
            {
                var response = await c.PostAsync("/api/chat", Body(r.Text, r.Actions)).ConfigureAwait(false);
                string content = (string)response["message"]?["content"];
                var row = PointsAnswer.ToRow(content, r.Actions, out var problem);
                if (row == null) answer.Fail(r.Index, problem);
                else answer.SetRow(r.Index, row);
            }
            catch (Exception e)
            {
                answer.Fail(r.Index, e.Message);
            }
        }

        protected override string HealthPath => "/api/tags";
    }
}
