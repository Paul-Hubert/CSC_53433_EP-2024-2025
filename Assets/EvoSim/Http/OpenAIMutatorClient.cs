using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A mutator behind an OpenAI-compatible server (vLLM, docker profile gpu-mutator): /v1/chat/completions with the
    /// seed and temperature, thinking off through chat_template_kwargs.
    /// </summary>
    public class OpenAIMutatorClient : HttpMutatorClient
    {
        [SerializeField, Range(1, 512), Tooltip("Longest answer in tokens (reference 64; a gene has at most 12 words).")]
        int maxTokens = 64;
        [SerializeField, Tooltip("Send chat_template_kwargs.enable_thinking = false (reference on, for Qwen3.5).")]
        bool thinkingOff = true;

        public OpenAIMutatorClient()
        {
            host = "http://localhost:8001";
        }

        public override string ModelIdentity => World != null && World.Service<MutatorService>() != null ? World.Service<MutatorService>().Model : "";

        protected override string ChatPath => "/v1/chat/completions";
        protected override string HealthPath => "/v1/models";

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

        protected override string Content(JToken response) => (string)response["choices"]?[0]?["message"]?["content"] ?? "";
    }
}
