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
    /// <remarks>
    /// <para>The <see cref="MutatorService"/> gets each mutation with its tries already drawn (MUT-30), looks each prompt up
    /// in its cache, and calls <see cref="Send"/> on the main thread with the rest of a round. <see cref="Send"/> returns a
    /// <see cref="MutatorReply"/> at once and posts every request from the thread pool, at most <c>maxParallel</c> at a
    /// time, with the time-out and tries of <see cref="Caller"/> (DEC-42). A request that still fails gets an error in
    /// the reply: the service then leaves the gene unchanged, and a strict service also stops the run (MUT-04).</para>
    /// <para>To use another server, derive from it and give <see cref="ChatPath"/>, <see cref="HealthPath"/>,
    /// <see cref="Body"/> and <see cref="Content"/>. The body holds nothing but the prompt as the user message (B-11):
    /// build it with <see cref="UserMessage"/>. <see cref="Body"/> and <see cref="Content"/> run on the thread pool, so
    /// they must not touch Unity objects. Place the component under the World, next to the MutatorService. The API key
    /// never goes in a field: <c>apiKeyVariable</c> names the environment variable that holds it (OUT-04).</para>
    /// </remarks>
    /// <example>
    /// A trimmed <see cref="OpenAIMutatorClient"/>:
    /// <code>
    /// public class OpenAIMutatorClient : HttpMutatorClient
    /// {
    ///     protected override string ChatPath => "/v1/chat/completions";
    ///     protected override string HealthPath => "/v1/models";
    ///
    ///     public override JObject Body(MutatorRequest r) => new JObject
    ///     {
    ///         ["model"] = r.Model,
    ///         ["messages"] = UserMessage(r.Prompt),
    ///         ["seed"] = r.Seed,
    ///         ["temperature"] = HttpCaller.Number(r.Temperature),
    ///     };
    ///
    ///     protected override string Content(JToken response) => (string)response["choices"]?[0]?["message"]?["content"] ?? "";
    /// }
    /// </code>
    /// </example>
    public abstract class HttpMutatorClient : MutatorClient
    {
        /// <summary>The server's address: <c>http://localhost:11434</c> for Ollama, <c>http://localhost:8001</c> for vLLM.</summary>
        [SerializeField, Tooltip("The server's address (reference Ollama http://localhost:11434, or vLLM http://localhost:8001).")]
        protected string host = "http://localhost:11434";
        /// <summary>
        /// The name of the environment variable that holds the API key, or empty when the server needs none. Never the key
        /// itself (OUT-04): the key is read at each call and only sent.
        /// </summary>
        [SerializeField, Tooltip("Name of the environment variable that holds the API key, if the server needs one; never the key itself (OUT-04).")]
        protected string apiKeyVariable = "";
        /// <summary>The time-out of one try, in seconds (reference 60).</summary>
        [SerializeField, Min(0.01f), Tooltip("Time-out of one try, in seconds (reference 60).")]
        protected float timeoutSeconds = 60f;
        /// <summary>Tries per request before it fails, 1 to 10 (reference 3, DEC-42).</summary>
        [SerializeField, Range(1, 10), Tooltip("Tries per request before it fails (reference 3, DEC-42).")]
        protected int tries = 3;
        /// <summary>The wait before the second try, in seconds; it doubles before each further try (reference 1).</summary>
        [SerializeField, Min(0f), Tooltip("Wait before the second try, in seconds, doubling at each try (reference 1).")]
        protected float backoffSeconds = 1f;
        /// <summary>The most requests in flight at once; the others wait their turn (reference 2 for a model on the CPU).</summary>
        [SerializeField, Range(1, 64), Tooltip("Requests in flight at once (reference 2 for a CPU model).")]
        protected int maxParallel = 2;

        HttpCaller caller;
        IHttpTransport transport;
        SystemHttpTransport own;                                             // made on first use, disposed with the component

        /// <summary>The server's address; setting it makes a new <see cref="Caller"/> on its next use.</summary>
        public string Host { get => host; set { host = value; caller = null; } }
        /// <summary>
        /// The transport set from code, or null for the real one (<see cref="SystemHttpTransport"/>). Tests set a fake server
        /// here, and the B-11 check a <see cref="RecordingTransport"/>; setting it makes a new <see cref="Caller"/>.
        /// </summary>
        public IHttpTransport Transport { get => transport; set { transport = value; caller = null; } }
        /// <summary>
        /// The client the requests go through, with this component's settings; made on first use and again after a setting
        /// changed.
        /// </summary>
        public HttpCaller Caller => caller ?? (caller = new HttpCaller(host, apiKeyVariable, timeoutSeconds, tries, backoffSeconds, maxParallel, Wire));
        /// <summary>What this client's calls did since it was made: tries, retries, failures, time-outs, latencies.</summary>
        public HttpStats Stats => Caller.Stats;
        /// <summary>The transport calls go through: the injected one, else this component's own HttpClient (ARCH-07).</summary>
        protected IHttpTransport Wire => transport ?? (own ?? (own = new SystemHttpTransport()));

        /// <summary>Disposes this component's own HttpClient, if it made one. Called by Unity when the component is destroyed.</summary>
        void OnDestroy() => own?.Dispose();

        /// <summary>Sets the client settings from code (tests, scenarios); the next call uses a new <see cref="Caller"/>.</summary>
        /// <param name="address">The server's address, such as <c>http://localhost:11434</c>.</param>
        /// <param name="timeout">The time-out of one try, in seconds.</param>
        /// <param name="triesPerRequest">Tries per request before it fails.</param>
        /// <param name="backoff">The wait before the second try, in seconds; it doubles before each further try.</param>
        /// <param name="parallel">The most requests in flight at once.</param>
        /// <param name="keyVariable">The name of the environment variable that holds the API key; empty for none.</param>
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

        /// <summary>Drops the client, so that the run uses the current settings and counts its calls from 0.</summary>
        /// <inheritdoc/>
        public override void Initialize() => caller = null;

        /// <summary>
        /// Posts every request from the thread pool and returns at once a reply that fills as the answers come back; it
        /// counts one model call per request.
        /// </summary>
        /// <inheritdoc/>
        public override MutatorReply Send(IReadOnlyList<MutatorRequest> requests)
        {
            var reply = new MutatorReply(requests.Count) { ModelCalls = requests.Count };
            var c = Caller;
            var copy = new List<MutatorRequest>(requests);                      // the service reuses its list for the next round
            Task.Run(() => Run(c, copy, reply));
            return reply;
        }

        /// <summary>
        /// Sends every request at once (the caller caps how many are in flight), then completes the reply, whatever happened.
        /// </summary>
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

        /// <summary>Sends request <paramref name="i"/> and sets its answer text, or its error when the call or the answer failed.</summary>
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

        /// <summary>The chat endpoint's path, such as <c>/api/chat</c> (Ollama) or <c>/v1/chat/completions</c>.</summary>
        protected abstract string ChatPath { get; }
        /// <summary>The path a connection test reads, such as <c>/api/tags</c> or <c>/v1/models</c> (V-60).</summary>
        protected abstract string HealthPath { get; }
        /// <summary>The request body for one prompt: nothing but the prompt as the user message (B-11).</summary>
        /// <remarks>Called on the thread pool: no Unity API here.</remarks>
        /// <param name="r">The request: prompt, seed, temperature and model name.</param>
        /// <returns>The JSON body posted to <see cref="ChatPath"/>.</returns>
        public abstract JObject Body(MutatorRequest r);
        /// <summary>The answer text in a response.</summary>
        /// <remarks>Called on the thread pool: no Unity API here.</remarks>
        /// <param name="response">The server's JSON answer.</param>
        /// <returns>The model's answer text, or null when there is none (the request then fails).</returns>
        protected abstract string Content(JToken response);

        /// <summary>The chat messages for a prompt: one user message, no system message (MUT-11).</summary>
        /// <param name="prompt">The mutation prompt.</param>
        /// <returns>The <c>messages</c> array of a chat request.</returns>
        protected static JArray UserMessage(string prompt) => new JArray(new JObject { ["role"] = "user", ["content"] = prompt });

        /// <summary>
        /// Reads <see cref="HealthPath"/> once, with a time-out of at most 5 s: the V-60 check, run on demand by the Brain
        /// health window's Test connection button.
        /// </summary>
        /// <returns>A task that gives null when the server answers, else what went wrong.</returns>
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

        /// <summary>Checks that the address is an http(s) URL (V-20) and that no key sits in a serialized field (V-61).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            HttpChecks.Host(report, this, host);
            Secrets.CheckFields(report, this);                                  // V-61 (also run by the World on every module)
        }
    }
}
