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
    /// <remarks>
    /// The server is Ollama on <c>http://localhost:11434</c> (<c>/api/chat</c>, and <c>/api/tags</c> for the connection
    /// test and the digest), with two requests in flight by default. The model's tag and digest are part of the answer
    /// cache's keys (DEC-33): pin the digest in the Inspector, or it is asked from the server when the run begins.
    /// </remarks>
    public class OllamaPointsBrain : HttpBrain
    {
        /// <summary>The answer instruction that ends every prompt (PROMPT-07); <see cref="PointsAnswer"/> reads the answer.</summary>
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

        string serverDigest;                                               // from /api/tags when no digest is pinned; null before

        /// <summary>Sets the Ollama defaults: the server on <c>http://localhost:11434</c>, two requests in flight.</summary>
        public OllamaPointsBrain()
        {
            host = "http://localhost:11434";
            maxParallel = 2;
        }

        /// <summary><c>ollama-points</c>.</summary>
        /// <inheritdoc/>
        public override string Id => "ollama-points";
        /// <summary><c>points</c>.</summary>
        /// <inheritdoc/>
        public override string Mode => "points";
        /// <summary><see cref="Instruction"/>: the answer is a share of 100 points per action.</summary>
        /// <inheritdoc/>
        public override string AnswerInstruction => Instruction;
        /// <summary>The context length, in tokens (reference 4 096).</summary>
        /// <inheritdoc/>
        public override int MaxPromptTokens => contextLength;
        /// <summary>
        /// <c>tag@digest</c>: the pinned digest, else the one the server gave when the run began, else <c>unknown</c>
        /// (part of cache keys, DEC-33).
        /// </summary>
        /// <inheritdoc/>
        public override string ModelIdentity => model + "@" + (digest.Length > 0 ? digest : serverDigest ?? "unknown");
        /// <summary>The Ollama model tag, such as <c>gemma4:12b</c>.</summary>
        public string Model => model;
        /// <summary>True to run the model on the CPU (<c>num_gpu</c> 0): slower, for a machine whose GPU is busy.</summary>
        public bool CpuOnly { get => cpuOnly; set => cpuOnly = value; }

        /// <summary>Sets the model from code (tests, scenarios).</summary>
        /// <param name="modelTag">The Ollama model tag, such as <c>gemma4:12b</c>.</param>
        /// <param name="fixedSeed">The seed of every call (DEC-12).</param>
        /// <param name="context">The context length, in tokens.</param>
        /// <param name="pinnedDigest">The model's digest for cache keys, or empty to ask the server when the run begins.</param>
        public void Configure(string modelTag, int fixedSeed = 0, int context = 4096, string pinnedDigest = "")
        {
            model = modelTag;
            seed = fixedSeed;
            contextLength = context;
            digest = pinnedDigest;
        }

        /// <summary>
        /// Asks Ollama for the model's digest, once, when no digest is pinned and a species uses this brain. It blocks the
        /// main thread for at most about 5 s; an unreachable server gives <c>unknown</c>.
        /// </summary>
        /// <inheritdoc/>
        public override void Begin()
        {
            if (digest.Length > 0 || serverDigest != null || !UsedByASpecies()) return;
            serverDigest = Task.Run(() => OllamaTags.DigestAsync(new HttpCaller(host, apiKeyVariable, 5f, 1, 0f, 1, Wire), model))
                               .GetAwaiter().GetResult();
        }

        /// <summary>True when a species of the World decides with this brain.</summary>
        bool UsedByASpecies()
        {
            foreach (var s in World.AllSpecies) if (s.Brain == this) return true;
            return false;
        }

        /// <summary>The chat request for one prompt (also used by the golden test).</summary>
        /// <param name="prompt">The full prompt, ending with <see cref="Instruction"/>.</param>
        /// <param name="actions">The species' action names, in action order: the keys of the answer's JSON schema.</param>
        /// <returns>The <c>/api/chat</c> body: the prompt as the user message, the options, the schema and the thinking switch.</returns>
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

        /// <summary>The model options: the CPU switch, the context length, the fixed seed and temperature 0 (DEC-12).</summary>
        JObject Options()
        {
            var o = new JObject();
            if (cpuOnly) o["num_gpu"] = 0;
            o["num_ctx"] = contextLength;
            o["seed"] = seed;
            o["temperature"] = 0;
            return o;
        }

        /// <summary>
        /// Sends one chat call per request, all at once; <see cref="HttpBrain.Caller"/> keeps at most <c>maxParallel</c> in flight.
        /// </summary>
        /// <inheritdoc/>
        protected override async Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer)
        {
            var calls = new List<Task>(requests.Count);
            foreach (var r in requests) calls.Add(One(c, r, answer));
            await Task.WhenAll(calls).ConfigureAwait(false);
        }

        /// <summary>Sends one request and sets its row from the points (DEC-41), or its failure with the reason (thread pool).</summary>
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

        /// <summary><c>/api/tags</c>.</summary>
        /// <inheritdoc/>
        protected override string HealthPath => "/api/tags";
    }
}
