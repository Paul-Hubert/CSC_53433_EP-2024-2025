using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace EvoSim.Testing
{
    /// <summary>
    /// A fake server for HTTP brains and mutators (30 §2): answers each request with a scripted function, records every
    /// request, can fail, hang until the time-out, or answer late; counts requests in flight at once.
    /// </summary>
    /// <remarks>
    /// <para>Give it to an HTTP brain or mutator client through its <c>Transport</c> property, so tests never call a real
    /// server (30 §2). <see cref="Jev"/> and <see cref="OllamaPoints"/> make fakes that answer like the two reference servers,
    /// with deterministic numbers; <see cref="For"/> picks the right one for a brain.</para>
    /// <para>It may be called from several threads at once: <see cref="Requests"/> is filled under a lock.</para>
    /// </remarks>
    /// <example>
    /// A server that is down, and one that answers every chat with fixed points:
    /// <code>
    /// var down = new FakeHttpTransport { FailWith = (path, body) =&gt; 503 };
    ///
    /// var fake = new FakeHttpTransport();
    /// fake.Respond = (method, path, body) =&gt; path == "/api/tags"
    ///     ? new JObject { ["models"] = new JArray(new JObject { ["name"] = "gemma4:12b", ["digest"] = "0123456789abcdef0123" }) }
    ///     : FakeHttpTransport.Chat("{\"eat\": 50, \"flee\": 30, \"hide\": 0, \"follow\": 10, \"rest\": 10, \"mate\": 0, \"hunt\": 60}");
    /// var w = New(9).Lab1(prey: 10, predators: 2, randomBrain: false)
    ///     .DefaultBrain&lt;OllamaPointsBrain&gt;(o =&gt;
    ///     {
    ///         o.Transport = fake;
    ///         o.ConfigureClient("http://ollama.test", 5f, 3, 0f, 2);
    ///     }).Build();
    /// w.Advance(1);
    /// Assert.LessOrEqual(fake.MaxInFlight, 2, "the parallel cap");
    /// </code>
    /// </example>
    public class FakeHttpTransport : IHttpTransport
    {
        /// <summary>The answer's JSON for (method, path, body); body is null for a GET. The default answers <c>{}</c>.</summary>
        public Func<string, string, JObject, JToken> Respond = (method, path, body) => new JObject();
        /// <summary>An HTTP status to fail with for (path, body), thrown as an <see cref="HttpFailure"/>; 0 = answer normally.</summary>
        public Func<string, JObject, int> FailWith;
        /// <summary>True for (path, body) of requests that never answer: they wait until the caller's time-out cancels them.</summary>
        public Func<string, JObject, bool> Hang;
        /// <summary>The delay before each answer, in milliseconds; 0 = none.</summary>
        public int DelayMs;

        readonly object gate = new object();   // guards Requests, inFlight and MaxInFlight across threads
        int inFlight;                          // requests sent and not yet answered

        /// <summary>Every request received, in arrival order: method, path, body (null for a GET) and API key.</summary>
        public readonly List<(string Method, string Path, JObject Body, string ApiKey)> Requests = new List<(string, string, JObject, string)>();

        /// <summary>The most requests that were in flight at the same time (to check a client's parallel cap).</summary>
        public int MaxInFlight { get; private set; }

        /// <summary>The number of requests received so far, read safely while requests are in flight.</summary>
        public int Count
        {
            get { lock (gate) return Requests.Count; }
        }

        /// <summary>
        /// Records the request, then hangs, waits, fails or answers as scripted by <see cref="Hang"/>, <see cref="DelayMs"/>,
        /// <see cref="FailWith"/> and <see cref="Respond"/>.
        /// </summary>
        /// <inheritdoc/>
        public async Task<string> SendAsync(string method, string url, string json, string apiKey, CancellationToken cancel)
        {
            string path = new Uri(url).AbsolutePath;
            var body = json != null ? JObject.Parse(json) : null;
            lock (gate)
            {
                Requests.Add((method, path, body, apiKey));
                inFlight++;
                if (inFlight > MaxInFlight) MaxInFlight = inFlight;
            }
            try
            {
                if (Hang != null && Hang(path, body)) await Task.Delay(Timeout.Infinite, cancel).ConfigureAwait(false);
                if (DelayMs > 0) await Task.Delay(DelayMs, cancel).ConfigureAwait(false);
                int status = FailWith?.Invoke(path, body) ?? 0;
                if (status != 0) throw new HttpFailure($"{method} {path}: HTTP {status}", status, HttpFailure.IsRetryable(status));
                return Respond(method, path, body).ToString(Newtonsoft.Json.Formatting.None);
            }
            finally
            {
                lock (gate) inFlight--;
            }
        }

        /// <summary>
        /// A fake JEV server: <c>/v1/completions</c> gets log-probabilities from <see cref="FakeLogProbability"/>, any other path
        /// an empty model list.
        /// </summary>
        /// <param name="delayMs">The delay before each answer, in milliseconds.</param>
        /// <returns>A new fake.</returns>
        public static FakeHttpTransport Jev(int delayMs = 0) => new FakeHttpTransport
        {
            DelayMs = delayMs,
            Respond = (method, path, body) => path == "/v1/completions" ? Completions(body, FakeLogProbability) : new JObject { ["data"] = new JArray() },
        };

        /// <summary>
        /// The fake JEV's log-probability of option slot <paramref name="k"/> for a text: from a hash of both, a multiple of 0.25
        /// between -2.25 and 0.
        /// </summary>
        /// <param name="text">The prompt text.</param>
        /// <param name="k">The option slot (0 for A, 1 for B…).</param>
        /// <returns>The same number for the same text and slot, so a test can compute the expected rows itself.</returns>
        public static double FakeLogProbability(string text, int k) => -((Hashing.Sha256Hex(text + "#" + k, 4)[0] % 16) / 4.0);

        /// <summary>
        /// A fake Ollama server in points mode: <c>/api/chat</c> answers 0 to 100 points for each action the request's JSON
        /// schema requires, a hash of the prompt and the action; any other path an empty model list.
        /// </summary>
        /// <param name="delayMs">The delay before each answer, in milliseconds.</param>
        /// <returns>A new fake.</returns>
        public static FakeHttpTransport OllamaPoints(int delayMs = 0) => new FakeHttpTransport
        {
            DelayMs = delayMs,
            Respond = (method, path, body) =>
            {
                if (path != "/api/chat") return new JObject { ["models"] = new JArray() };
                var points = new JObject();
                string prompt = Prompt(body);
                foreach (var a in (JArray)body["format"]["required"])
                    points[(string)a] = (int)(System.Convert.ToUInt32(Hashing.Sha256Hex(prompt + "#" + (string)a, 8), 16) % 101);
                return Chat(points.ToString(Newtonsoft.Json.Formatting.None));
            },
        };

        /// <summary>
        /// A fake server for a known HTTP brain: <see cref="Jev"/> for a <see cref="JevBrain"/>, <see cref="OllamaPoints"/> for an
        /// <see cref="OllamaPointsBrain"/>.
        /// </summary>
        /// <param name="brain">The brain to answer.</param>
        /// <returns>A new fake, or null for any other brain.</returns>
        public static FakeHttpTransport For(HttpBrain brain) => brain is JevBrain ? Jev() : brain is OllamaPointsBrain ? OllamaPoints() : null;

        /// <summary>
        /// A vLLM completions answer: for each prompt of the request, the log-probabilities of its allowed token ids, from a
        /// function of (prompt, slot); 100 prompt tokens are counted per prompt.
        /// </summary>
        /// <param name="body">The request's body, with its <c>prompt</c> array and <c>allowed_token_ids</c>.</param>
        /// <param name="logProbability">The log-probability of slot k (the k-th allowed id) for a prompt.</param>
        /// <returns>The answer's JSON.</returns>
        public static JObject Completions(JObject body, Func<string, int, double> logProbability)
        {
            var prompts = (JArray)body["prompt"];
            var ids = (JArray)body["allowed_token_ids"];
            var choices = new JArray();
            for (int i = 0; i < prompts.Count; i++)
            {
                var top = new JObject();
                for (int k = 0; k < ids.Count; k++) top["token_id:" + (int)ids[k]] = logProbability((string)prompts[i], k);
                choices.Add(new JObject { ["index"] = i, ["text"] = "", ["logprobs"] = new JObject { ["top_logprobs"] = new JArray(top) } });
            }
            return new JObject { ["choices"] = choices, ["usage"] = new JObject { ["prompt_tokens"] = 100 * prompts.Count } };
        }

        /// <summary>An Ollama chat answer whose message has the given content.</summary>
        /// <param name="content">The assistant's text, e.g. a JSON object of points.</param>
        /// <returns>The answer's JSON.</returns>
        public static JObject Chat(string content) =>
            new JObject { ["message"] = new JObject { ["role"] = "assistant", ["content"] = content }, ["done"] = true };

        /// <summary>The user message of a chat request: the prompt the brain or mutator sent.</summary>
        /// <param name="body">The request's body.</param>
        /// <returns>The first message's content, or null when there is none.</returns>
        public static string Prompt(JObject body) => (string)body["messages"]?[0]?["content"];
    }
}
