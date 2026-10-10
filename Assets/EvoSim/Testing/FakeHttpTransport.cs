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
    public class FakeHttpTransport : IHttpTransport
    {
        /// <summary>The answer for (method, path, body); body is null for a GET.</summary>
        public Func<string, string, JObject, JToken> Respond = (method, path, body) => new JObject();
        /// <summary>A status to fail with for a request (0 = answer normally).</summary>
        public Func<string, JObject, int> FailWith;
        /// <summary>Requests that never answer (until the caller's time-out).</summary>
        public Func<string, JObject, bool> Hang;
        /// <summary>Delay before each answer, in milliseconds.</summary>
        public int DelayMs;

        readonly object gate = new object();
        int inFlight;

        public readonly List<(string Method, string Path, JObject Body, string ApiKey)> Requests = new List<(string, string, JObject, string)>();
        public int MaxInFlight { get; private set; }

        public int Count
        {
            get { lock (gate) return Requests.Count; }
        }

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

        /// <summary>A fake JEV server: deterministic log-probabilities from each text and option slot.</summary>
        public static FakeHttpTransport Jev(int delayMs = 0) => new FakeHttpTransport
        {
            DelayMs = delayMs,
            Respond = (method, path, body) => path == "/v1/completions" ? Completions(body, FakeLogProbability) : new JObject { ["data"] = new JArray() },
        };

        /// <summary>The fake JEV's log-probability of option slot k for a text.</summary>
        public static double FakeLogProbability(string text, int k) => -((Hashing.Sha256Hex(text + "#" + k, 4)[0] % 16) / 4.0);

        /// <summary>A fake Ollama points server: deterministic points from each prompt for the schema's actions.</summary>
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

        /// <summary>A fake server for a known HTTP brain, or null.</summary>
        public static FakeHttpTransport For(HttpBrain brain) => brain is JevBrain ? Jev() : brain is OllamaPointsBrain ? OllamaPoints() : null;

        /// <summary>A vLLM completions answer: for each prompt, the log-probabilities of the allowed ids from a function of (prompt, slot).</summary>
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

        /// <summary>An Ollama chat answer with the given content.</summary>
        public static JObject Chat(string content) =>
            new JObject { ["message"] = new JObject { ["role"] = "assistant", ["content"] = content }, ["done"] = true };

        /// <summary>The user message of a chat request.</summary>
        public static string Prompt(JObject body) => (string)body["messages"]?[0]?["content"];
    }
}
