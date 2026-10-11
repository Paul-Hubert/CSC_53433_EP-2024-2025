using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A mutator behind an OpenAI-compatible server (vLLM, docker profile gpu-mutator): /v1/chat/completions with the
    /// seed and temperature, thinking off through chat_template_kwargs.
    /// </summary>
    /// <remarks>
    /// The default server is vLLM on <c>http://localhost:8001</c>; <c>/v1/models</c> serves the connection test. The
    /// model's name comes from the <see cref="MutatorService"/>, as it is part of each request.
    /// </remarks>
    public class OpenAIMutatorClient : HttpMutatorClient
    {
        [SerializeField, Range(1, 512), Tooltip("Longest answer in tokens (reference 64; a gene has at most 12 words).")]
        int maxTokens = 64;
        [SerializeField, Tooltip("Send chat_template_kwargs.enable_thinking = false (reference on, for Qwen3.5).")]
        bool thinkingOff = true;

        /// <summary>Sets the default address, <c>http://localhost:8001</c> (vLLM's mutator profile).</summary>
        public OpenAIMutatorClient()
        {
            host = "http://localhost:8001";
        }

        /// <summary>The MutatorService's model name, with no digest: an OpenAI-compatible server gives none.</summary>
        /// <inheritdoc/>
        public override string ModelIdentity => World != null && World.Service<MutatorService>() != null ? World.Service<MutatorService>().Model : "";

        /// <summary><c>/v1/chat/completions</c>.</summary>
        /// <inheritdoc/>
        protected override string ChatPath => "/v1/chat/completions";
        /// <summary><c>/v1/models</c>.</summary>
        /// <inheritdoc/>
        protected override string HealthPath => "/v1/models";

        /// <summary>
        /// The <c>/v1/chat/completions</c> body: the prompt as the only user message, the longest answer, the request's seed
        /// and temperature, and thinking off when asked.
        /// </summary>
        /// <inheritdoc/>
        public override JObject Body(MutatorRequest r)
        {
            var body = new JObject
            {
                ["model"] = r.Model,
                ["messages"] = UserMessage(r.Prompt),
                ["max_tokens"] = maxTokens,
                ["seed"] = r.Seed,
                ["temperature"] = HttpCaller.Number(r.Temperature),
            };
            if (thinkingOff) body["chat_template_kwargs"] = new JObject { ["enable_thinking"] = false };
            return body;
        }

        /// <summary>
        /// The first choice's <c>message.content</c>, or an empty text when there is none: the service then rejects it as a
        /// mutation and draws the next try, where Ollama's client would fail the request.
        /// </summary>
        /// <inheritdoc/>
        protected override string Content(JToken response) => (string)response["choices"]?[0]?["message"]?["content"] ?? "";
    }
}
