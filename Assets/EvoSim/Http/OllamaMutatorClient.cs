using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference mutator (10 §2): a model in Ollama on the CPU (gemma4:26b, owner 2026-10-10; num_gpu 0, num_ctx 1 024),
    /// thinking off, so the GPU stays JEV's.
    /// </summary>
    public class OllamaMutatorClient : HttpMutatorClient
    {
        [SerializeField, Tooltip("Run the model on the CPU (num_gpu 0, reference on: the GPU is JEV's).")]
        bool cpuOnly = true;
        [SerializeField, Min(256), Tooltip("Context length in tokens (reference 1 024).")]
        int contextLength = 1024;
        [SerializeField, Tooltip("Let thinking models reason before answering (reference off: gemma4 and Qwen3.5 think by default).")]
        bool think;

        string digest;

        public override string ModelIdentity => (World != null && World.Service<MutatorService>() != null ? World.Service<MutatorService>().Model : "") + "@" + (digest ?? "unknown");

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

        protected override string ChatPath => "/api/chat";
        protected override string HealthPath => "/api/tags";

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

        protected override string Content(JToken response) => (string)response["message"]?["content"];
    }
}
