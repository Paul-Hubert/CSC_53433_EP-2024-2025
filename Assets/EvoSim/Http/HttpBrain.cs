using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A brain behind a model server (08 §6). Requests are built on the main thread, sent from the thread pool with
    /// ConfigureAwait(false), and fill the BrainAnswer when they come back, so the Freeze mode can block the main
    /// thread and the Responsive mode keeps the editor drawing (SPACE-14). Retries, time-outs and the parallel cap
    /// follow DEC-42; strict (reference) stops the run on a failure after retries (DEC-40); answers go to the answer
    /// cache through the decision phases (DEC-33).
    /// </summary>
    public abstract class HttpBrain : Brain
    {
        [SerializeField, Tooltip("The server's address, e.g. http://localhost:8000.")]
        protected string host = "http://localhost:8000";
        [SerializeField, Tooltip("Name of the environment variable that holds the API key, if the server needs one; never the key itself (OUT-04).")]
        protected string apiKeyVariable = "";
        [SerializeField, Min(0.01f), Tooltip("Time-out of one try, in seconds (reference 60–120, DEC-42).")]
        protected float timeoutSeconds = 120f;
        [SerializeField, Range(1, 10), Tooltip("Tries per request before it fails (reference 3, DEC-42).")]
        protected int tries = 3;
        [SerializeField, Min(0f), Tooltip("Wait before the second try, in seconds, doubling at each try (reference 1, DEC-42).")]
        protected float backoffSeconds = 1f;
        [SerializeField, Range(1, 64), Tooltip("Requests in flight at once (reference 2 for Ollama, 4 for vLLM).")]
        protected int maxParallel = 4;

        HttpCaller caller;
        IHttpTransport transport;
        SystemHttpTransport own;

        protected HttpBrain()
        {
            strict = true;                                                   // DEC-40: the reference for LLM brains
            useCache = true;                                                 // DEC-33
        }

        public string Host { get => host; set { host = value; caller = null; } }
        /// <summary>The transport; null = the real one. Tests inject fakes (30 §2).</summary>
        public IHttpTransport Transport { get => transport; set { transport = value; caller = null; } }
        public HttpCaller Caller => caller ?? (caller = new HttpCaller(host, apiKeyVariable, timeoutSeconds, tries, backoffSeconds, maxParallel, Wire));
        public HttpStats Stats => Caller.Stats;
        /// <summary>The transport calls go through: the injected one, else this component's own HttpClient (ARCH-07).</summary>
        protected IHttpTransport Wire => transport ?? (own ?? (own = new SystemHttpTransport()));

        void OnDestroy() => own?.Dispose();

        /// <summary>Client settings from code (tests, scenarios).</summary>
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

        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var requests = new List<BrainRequest>(batch.Count);
            for (int i = 0; i < batch.Count; i++) requests.Add(new BrainRequest(i, TextFor(batch[i]), ActionNames(batch[i].Species), batch[i].Species.Id));
            var answer = BrainAnswer.Later(batch.Count);
            answer.ModelCalls = requests.Count;                                // one forward pass or chat call per distinct query
            var c = Caller;
            Task.Run(() => Run(c, requests, answer));
            return answer;
        }

        /// <summary>The text sent for a query (main thread): the full prompt by default.</summary>
        public virtual string TextFor(DecisionQuery q) => q.Prompt(this);

        /// <summary>The species' action names in action order (main thread: they are GameObject names).</summary>
        public static string[] ActionNames(Species s)
        {
            var names = new string[s.Actions.Count];
            for (int i = 0; i < names.Length; i++) names[i] = s.Actions[i].Name;
            return names;
        }

        async Task Run(HttpCaller c, List<BrainRequest> requests, BrainAnswer answer)
        {
            try
            {
                await AnswerAsync(c, requests, answer).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                foreach (var r in requests) if (answer.Row(r.Index) == null) answer.Fail(r.Index, e.Message);
            }
            finally
            {
                answer.Complete();
            }
        }

        /// <summary>Sends the requests and fills a row or a failure for each (thread pool; no Unity API here).</summary>
        protected abstract Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer);

        /// <summary>The path a connection test reads (V-60).</summary>
        protected abstract string HealthPath { get; }

        /// <summary>V-60 on demand (Test connection): null when the server answers, else what went wrong.</summary>
        public async Task<string> TestConnection()
        {
            try
            {
                var probe = new HttpCaller(host, apiKeyVariable, Math.Min(timeoutSeconds, 5f), 1, 0f, 1, Wire);
                await probe.GetAsync(HealthPath).ConfigureAwait(false);
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
            HttpChecks.Secrets(report, this, host, apiKeyVariable);
        }
    }
}
