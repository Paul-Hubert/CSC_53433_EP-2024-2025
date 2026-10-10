using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A mutator model behind a server (10 §2): one chat call per request with the prompt alone (CORE-07, MUT-11), the
    /// request's seed and temperature, thinking off. Calls run on the thread pool and fill the reply; the
    /// MutatorService caches answers and batches rounds (MUT-14, MUT-31).
    /// </summary>
    public abstract class HttpMutatorClient : MutatorClient
    {
        [SerializeField, Tooltip("The server's address (reference Ollama http://localhost:11434, or vLLM http://localhost:8001).")]
        protected string host = "http://localhost:11434";
        [SerializeField, Tooltip("Name of the environment variable that holds the API key, if the server needs one; never the key itself (OUT-04).")]
        protected string apiKeyVariable = "";
        [SerializeField, Min(0.01f), Tooltip("Time-out of one try, in seconds (reference 60).")]
        protected float timeoutSeconds = 60f;
        [SerializeField, Range(1, 10), Tooltip("Tries per request before it fails (reference 3, DEC-42).")]
        protected int tries = 3;
        [SerializeField, Min(0f), Tooltip("Wait before the second try, in seconds, doubling at each try (reference 1).")]
        protected float backoffSeconds = 1f;
        [SerializeField, Range(1, 64), Tooltip("Requests in flight at once (reference 2 for a CPU model).")]
        protected int maxParallel = 2;

        HttpCaller caller;
        IHttpTransport transport;
        SystemHttpTransport own;

        public string Host { get => host; set { host = value; caller = null; } }
        public IHttpTransport Transport { get => transport; set { transport = value; caller = null; } }
        public HttpCaller Caller => caller ?? (caller = new HttpCaller(host, apiKeyVariable, timeoutSeconds, tries, backoffSeconds, maxParallel, Wire));
        public HttpStats Stats => Caller.Stats;
        /// <summary>The transport calls go through: the injected one, else this component's own HttpClient (ARCH-07).</summary>
        protected IHttpTransport Wire => transport ?? (own ?? (own = new SystemHttpTransport()));

        void OnDestroy() => own?.Dispose();

        public void ConfigureClient(string address, float timeout, int triesPerRequest, float backoff, int parallel, string keyVariable = "")
        {
            host = address;
            timeoutSeconds = timeout;
            tries = triesPerRequest;
            backoffSeconds = backoff;
            maxParallel = parallel;
            apiKeyVariable = keyVariable;
            caller = null;
        }

        public override void Initialize() => caller = null;

        public override MutatorReply Send(IReadOnlyList<MutatorRequest> requests)
        {
            var reply = new MutatorReply(requests.Count) { ModelCalls = requests.Count };
            var c = Caller;
            var copy = new List<MutatorRequest>(requests);
            Task.Run(() => Run(c, copy, reply));
            return reply;
        }

        async Task Run(HttpCaller c, List<MutatorRequest> requests, MutatorReply reply)
        {
            try
            {
                var calls = new List<Task>(requests.Count);
                for (int i = 0; i < requests.Count; i++) calls.Add(One(c, i, requests[i], reply));
                await Task.WhenAll(calls).ConfigureAwait(false);
            }
            finally
            {
                reply.Complete();
            }
        }

        async Task One(HttpCaller c, int i, MutatorRequest r, MutatorReply reply)
        {
            try
            {
                var response = await c.PostAsync(ChatPath, Body(r)).ConfigureAwait(false);
                string text = Content(response);
                if (text == null) reply.Fail(i, "no answer text");
                else reply.SetAnswer(i, text);
            }
            catch (Exception e)
            {
                reply.Fail(i, e.Message);
            }
        }

        /// <summary>The chat endpoint's path.</summary>
        protected abstract string ChatPath { get; }
        /// <summary>The path a connection test reads (V-60).</summary>
        protected abstract string HealthPath { get; }
        /// <summary>The request body for one prompt: nothing but the prompt as the user message (B-11).</summary>
        public abstract JObject Body(MutatorRequest r);
        /// <summary>The answer text in a response.</summary>
        protected abstract string Content(JToken response);

        protected static JArray UserMessage(string prompt) => new JArray(new JObject { ["role"] = "user", ["content"] = prompt });

        /// <summary>V-60 on demand (Test connection): null when the server answers.</summary>
        public async Task<string> TestConnection()
        {
            try
            {
                await new HttpCaller(host, apiKeyVariable, Math.Min(timeoutSeconds, 5f), 1, 0f, 1, Wire).GetAsync(HealthPath).ConfigureAwait(false);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public override void Validate(ValidationReport report)
        {
            HttpChecks.Host(report, this, host);
            Secrets.CheckFields(report, this);                                  // V-61 (also run by the World on every module)
        }
    }
}
