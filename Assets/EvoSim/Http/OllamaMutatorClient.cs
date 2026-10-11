using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference mutator (10 §2): a model in Ollama on the CPU (gemma4:26b, owner 2026-10-10; num_gpu 0, num_ctx 1 024),
    /// thinking off, so the GPU stays JEV's.
    /// </summary>
    /// <remarks>
    /// The server is Ollama on <c>http://localhost:11434</c>: the chat endpoint <c>/api/chat</c> without streaming, and
    /// <c>/api/tags</c> for the connection test and the model's digest. The model's name comes from the
    /// <see cref="MutatorService"/>, as it is part of each request.
    /// </remarks>
    public class OllamaMutatorClient : HttpMutatorClient
    {
        [SerializeField, Tooltip("Run the model on the CPU (num_gpu 0, reference on: the GPU is JEV's).")]
        bool cpuOnly = true;
        [SerializeField, Min(256), Tooltip("Context length in tokens (reference 1 024).")]
        int contextLength = 1024;
        [SerializeField, Tooltip("Let thinking models reason before answering (reference off: gemma4 and Qwen3.5 think by default).")]
        bool think;

        string digest;                                                     // from /api/tags, asked once in Begin; null before

        /// <summary>
        /// The MutatorService's model and its digest from the server, such as <c>gemma4:26b@1a2b3c4d5e6f</c>; the digest is
        /// <c>unknown</c> before <see cref="Begin"/> asked, or when the server didn't say.
        /// </summary>
        /// <inheritdoc/>
        public override string ModelIdentity => (World != null && World.Service<MutatorService>() != null ? World.Service<MutatorService>().Model : "") + "@" + (digest ?? "unknown");

        /// <summary>
        /// Asks Ollama for the model's digest, once, when the run begins with mutation on and an operator that uses the
        /// MutatorService. It blocks the main thread for at most about 5 s; an unreachable server gives <c>unknown</c>.
        /// </summary>
        /// <inheritdoc/>
        public override void Begin()
        {
            var service = World.Service<MutatorService>();
            if (service == null || digest != null || World.NoMutation || !Used()) return;
            string model = service.Model;
            digest = Task.Run(() => OllamaTags.DigestAsync(new HttpCaller(host, apiKeyVariable, 5f, 1, 0f, 1, Wire), model)).GetAwaiter().GetResult();
        }

        /// <summary>Whether an operator sends work to the mutator service (LlmMutation, or a student's own).</summary>
        bool Used()
        {
            foreach (var op in World.GetComponentsInChildren<MutationOperator>(true))
                if (op.UsesMutatorService) return true;
            return false;
        }

        /// <summary><c>/api/chat</c>.</summary>
        /// <inheritdoc/>
        protected override string ChatPath => "/api/chat";
        /// <summary><c>/api/tags</c>.</summary>
        /// <inheritdoc/>
        protected override string HealthPath => "/api/tags";

        /// <summary>
        /// The <c>/api/chat</c> body: the prompt as the only user message, no streaming, the request's seed and temperature,
        /// the context length, the CPU switch (<c>num_gpu</c> 0) and the thinking switch.
        /// </summary>
        /// <inheritdoc/>
        public override JObject Body(MutatorRequest r)
        {
            var options = new JObject();
            if (cpuOnly) options["num_gpu"] = 0;
            options["num_ctx"] = contextLength;
            options["seed"] = r.Seed;
            options["temperature"] = HttpCaller.Number(r.Temperature);
            return new JObject
            {
                ["model"] = r.Model,
                ["messages"] = UserMessage(r.Prompt),
                ["stream"] = false,
                ["options"] = options,
                ["think"] = think,
            };
        }

        /// <summary>The <c>message.content</c> of an Ollama chat answer, or null.</summary>
        /// <inheritdoc/>
        protected override string Content(JToken response) => (string)response["message"]?["content"];
    }
}
