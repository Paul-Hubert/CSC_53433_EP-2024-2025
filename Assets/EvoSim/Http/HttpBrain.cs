using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The base of the brains that ask a model server over HTTP (08 §6): it turns a batch of queries into requests, sends
    /// them off the main thread and fills the <see cref="BrainAnswer"/> when the answers come back.
    /// </summary>
    /// <remarks>
    /// <para>The Ask brains phase calls <see cref="Ask"/> on the main thread with the queries that the memo and the answer
    /// cache could not answer (DEC-30, DEC-33). <see cref="Ask"/> builds one <see cref="BrainRequest"/> per query, with the
    /// text from <see cref="TextFor"/>, returns an unfinished answer at once and runs <see cref="AnswerAsync"/> on the
    /// thread pool. Every await uses <c>ConfigureAwait(false)</c>, so the answers never need the main thread: the Freeze
    /// wait mode can block it, and the Responsive mode keeps the editor drawing (SPACE-14). When
    /// <see cref="AnswerAsync"/> ends, or throws, every request left without a row fails and the answer is completed.</para>
    /// <para><see cref="Caller"/> applies the time-out of each try, the tries with doubling waits and the cap on requests
    /// in flight (DEC-42). A request that still fails becomes a failed row. A strict brain (the default for HTTP brains)
    /// then stops the run cleanly; a non-strict one gets a uniform row, counted and never cached (DEC-40, DEC-34). The
    /// answers are stored in the answer cache by the decision phases, not here (DEC-33).</para>
    /// <para>To call another server, derive from it: implement <see cref="AnswerAsync"/> (post each body with
    /// <see cref="HttpCaller.PostAsync"/>, read the answer, then call <see cref="BrainAnswer.SetRow"/> or
    /// <see cref="BrainAnswer.Fail"/> for every request) and <see cref="HealthPath"/>, and override <see cref="Brain.Id"/>.
    /// Override <see cref="Brain.ModelIdentity"/> and <see cref="Brain.Mode"/> too, so that cache keys change with the
    /// model. <see cref="AnswerAsync"/> runs on another thread: it must not touch a Unity object. Place the component
    /// under the World, like any brain. An API key never goes in a field: <c>apiKeyVariable</c> names the environment
    /// variable that holds it (OUT-04).</para>
    /// </remarks>
    /// <example>
    /// The heart of <see cref="OllamaPointsBrain"/>: one chat call per request, the points turned into a row.
    /// <code>
    /// public class OllamaPointsBrain : HttpBrain
    /// {
    ///     public override string Id => "ollama-points";
    ///     public override string Mode => "points";
    ///     public override string AnswerInstruction => Instruction;
    ///     protected override string HealthPath => "/api/tags";
    ///
    ///     protected override async Task AnswerAsync(HttpCaller c, IReadOnlyList&lt;BrainRequest&gt; requests, BrainAnswer answer)
    ///     {
    ///         var calls = new List&lt;Task&gt;(requests.Count);
    ///         foreach (var r in requests) calls.Add(One(c, r, answer));
    ///         await Task.WhenAll(calls).ConfigureAwait(false);
    ///     }
    ///
    ///     async Task One(HttpCaller c, BrainRequest r, BrainAnswer answer)
    ///     {
    ///         try
    ///         {
    ///             var response = await c.PostAsync("/api/chat", Body(r.Text, r.Actions)).ConfigureAwait(false);
    ///             var row = PointsAnswer.ToRow((string)response["message"]?["content"], r.Actions, out var problem);
    ///             if (row == null) answer.Fail(r.Index, problem);
    ///             else answer.SetRow(r.Index, row);
    ///         }
    ///         catch (Exception e)
    ///         {
    ///             answer.Fail(r.Index, e.Message);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class HttpBrain : Brain
    {
        /// <summary>The server's address: <c>http://localhost:8000</c> for JEV on vLLM, <c>http://localhost:11434</c> for Ollama.</summary>
        [SerializeField, Tooltip("The server's address, e.g. http://localhost:8000.")]
        protected string host = "http://localhost:8000";
        /// <summary>
        /// The name of the environment variable that holds the API key, or empty when the server needs none. Never the key
        /// itself (OUT-04): the key is read at each call and only sent.
        /// </summary>
        [SerializeField, Tooltip("Name of the environment variable that holds the API key, if the server needs one; never the key itself (OUT-04).")]
        protected string apiKeyVariable = "";
        /// <summary>The time-out of one try, in seconds (reference 60–120, DEC-42).</summary>
        [SerializeField, Min(0.01f), Tooltip("Time-out of one try, in seconds (reference 60–120, DEC-42).")]
        protected float timeoutSeconds = 120f;
        /// <summary>Tries per request before it fails, 1 to 10 (reference 3, DEC-42).</summary>
        [SerializeField, Range(1, 10), Tooltip("Tries per request before it fails (reference 3, DEC-42).")]
        protected int tries = 3;
        /// <summary>The wait before the second try, in seconds; it doubles before each further try (reference 1, DEC-42).</summary>
        [SerializeField, Min(0f), Tooltip("Wait before the second try, in seconds, doubling at each try (reference 1, DEC-42).")]
        protected float backoffSeconds = 1f;
        /// <summary>The most requests in flight at once; the others wait their turn (reference 2 for Ollama, 4 for vLLM).</summary>
        [SerializeField, Range(1, 64), Tooltip("Requests in flight at once (reference 2 for Ollama, 4 for vLLM).")]
        protected int maxParallel = 4;

        HttpCaller caller;
        IHttpTransport transport;
        SystemHttpTransport own;                                             // made on first use, disposed with the component

        /// <summary>Sets the defaults of a brain behind a language model: strict (DEC-40) and with the answer cache (DEC-33).</summary>
        protected HttpBrain()
        {
            strict = true;                                                   // DEC-40: the reference for LLM brains
            useCache = true;                                                 // DEC-33
        }

        /// <summary>The server's address; setting it makes a new <see cref="Caller"/> on its next use.</summary>
        public string Host { get => host; set { host = value; caller = null; } }
        /// <summary>
        /// The transport set from code, or null for the real one (<see cref="SystemHttpTransport"/>). Tests set a fake server
        /// here (30 §2); setting it makes a new <see cref="Caller"/> on its next use.
        /// </summary>
        public IHttpTransport Transport { get => transport; set { transport = value; caller = null; } }
        /// <summary>
        /// The client the requests go through, with this brain's settings; made on first use and again after a setting changed.
        /// </summary>
        public HttpCaller Caller => caller ?? (caller = new HttpCaller(host, apiKeyVariable, timeoutSeconds, tries, backoffSeconds, maxParallel, Wire));
        /// <summary>What this brain's calls did since its client was made: tries, retries, failures, time-outs, latencies (B-01).</summary>
        public HttpStats Stats => Caller.Stats;
        /// <summary>The transport calls go through: the injected one, else this component's own HttpClient (ARCH-07).</summary>
        protected IHttpTransport Wire => transport ?? (own ?? (own = new SystemHttpTransport()));

        /// <summary>Disposes this component's own HttpClient, if it made one. Called by Unity when the component is destroyed.</summary>
        void OnDestroy() => own?.Dispose();

        /// <summary>Sets the client settings from code (tests, scenarios); the next call uses a new <see cref="Caller"/>.</summary>
        /// <param name="address">The server's address, such as <c>http://localhost:8000</c>.</param>
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
        /// Builds one request per query on the main thread, starts sending them on the thread pool and returns at once an
        /// answer that fills later (DEC-11). It counts one model call per query.
        /// </summary>
        /// <inheritdoc/>
        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var requests = new List<BrainRequest>(batch.Count);
            for (int i = 0; i < batch.Count; i++) requests.Add(new BrainRequest(i, TextFor(batch[i]), ActionNames(batch[i].Species), batch[i].Species.Id));
            var answer = BrainAnswer.Later(batch.Count);
            answer.ModelCalls = requests.Count;                                // one forward pass or chat call per distinct query
            var c = Caller;                                                    // on the main thread; the whole batch shares its cap
            Task.Run(() => Run(c, requests, answer));
            return answer;
        }

        /// <summary>
        /// Gives the text sent for one query: by default the full prompt, ending with this brain's answer instruction.
        /// Called on the main thread, so an override may read Unity objects.
        /// </summary>
        /// <param name="q">The query: species, genes, observation and situation text.</param>
        /// <returns>The text for the model.</returns>
        public virtual string TextFor(DecisionQuery q) => q.Prompt(this);

        /// <summary>Lists a species' action names in action order. Call it on the main thread: the names are GameObject names.</summary>
        /// <param name="s">The species.</param>
        /// <returns>One name per action, in the species' action order (the row order, DEC-11).</returns>
        public static string[] ActionNames(Species s)
        {
            var names = new string[s.Actions.Count];
            for (int i = 0; i < names.Length; i++) names[i] = s.Actions[i].Name;
            return names;
        }

        /// <summary>
        /// Runs <see cref="AnswerAsync"/> on the thread pool; then fails every request it left without a row and completes
        /// the answer, whatever happened, so the tick never waits forever.
        /// </summary>
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

        /// <summary>Sends the requests and fills a row or a failure for each of them.</summary>
        /// <remarks>
        /// Runs on the thread pool: use no Unity API here, and await with <c>ConfigureAwait(false)</c>. Don't call
        /// <see cref="BrainAnswer.Complete"/>: the base does it when this task ends. An exception thrown here fails every
        /// request still without a row, with its message.
        /// </remarks>
        /// <param name="c">The client to post with: it applies the time-out, the tries and the cap on requests in flight.</param>
        /// <param name="requests">The batch's requests; each one's <see cref="BrainRequest.Index"/> is the row to fill.</param>
        /// <param name="answer">The answer to fill with <see cref="BrainAnswer.SetRow"/> or <see cref="BrainAnswer.Fail"/>.</param>
        /// <returns>A task that ends when every request has its row or its failure.</returns>
        protected abstract Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer);

        /// <summary>The path a connection test reads, such as <c>/v1/models</c> or <c>/api/tags</c> (V-60).</summary>
        protected abstract string HealthPath { get; }

        /// <summary>
        /// Reads <see cref="HealthPath"/> once, with a time-out of at most 5 s: the V-60 check, run on demand by the Brain
        /// health window's Test connection button. Its call isn't counted in <see cref="Stats"/>.
        /// </summary>
        /// <returns>A task that gives null when the server answers, else what went wrong.</returns>
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

        /// <summary>Checks that the address is an http(s) URL (V-20) and that no key sits in a serialized field (V-61).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            HttpChecks.Host(report, this, host);
            Secrets.CheckFields(report, this);                                  // V-61 (also run by the World on every module)
        }
    }
}
