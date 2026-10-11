using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// JSON calls to one server with the reference client behaviour (DEC-42): a time-out per try, tries with doubling
    /// waits between them, a cap on requests in flight. The API key is read from its environment variable at each
    /// call and only sent, never stored or logged (OUT-04).
    /// </summary>
    /// <remarks>
    /// <para>Each HTTP brain and mutator client makes its own (<see cref="HttpBrain.Caller"/>,
    /// <see cref="HttpMutatorClient.Caller"/>): nothing is shared between worlds (ARCH-07). It is safe to call from several
    /// threads at once; requests above <see cref="MaxParallel"/> wait for a free place.</para>
    /// <para>A try that times out, gets no answer or gets a retryable status (408, 429, 5xx) is tried again after
    /// <see cref="Backoff"/>, then twice as long, and so on, until <see cref="Tries"/> are used. Any other error status
    /// fails at once.</para>
    /// </remarks>
    public sealed class HttpCaller
    {
        readonly SemaphoreSlim inFlight;                                       // the MaxParallel places for requests

        /// <summary>The server's address without a trailing slash, such as <c>http://localhost:8000</c>.</summary>
        public string Host { get; }
        /// <summary>The environment variable whose value is sent as the bearer token, or empty for none (OUT-04).</summary>
        public string ApiKeyVariable { get; }
        /// <summary>The time-out of one try (at least 0.01 s).</summary>
        public TimeSpan Timeout { get; }
        /// <summary>Tries per request, at least 1 (DEC-42).</summary>
        public int Tries { get; }
        /// <summary>The wait before the second try; it doubles before each further try (DEC-42).</summary>
        public TimeSpan Backoff { get; }
        /// <summary>The most requests in flight at once, at least 1; a request keeps its place while it waits between tries.</summary>
        public int MaxParallel { get; }
        /// <summary>What sends the requests: a brain's or client's own <see cref="SystemHttpTransport"/>, or a fake in tests.</summary>
        public IHttpTransport Transport { get; }
        /// <summary>What the calls did so far: tries, retries, failures, time-outs, latencies.</summary>
        public HttpStats Stats { get; } = new HttpStats();

        /// <summary>Makes a client for one server. Values below their minimum are raised to it.</summary>
        /// <param name="host">The server's address, such as <c>http://localhost:11434</c>; a trailing slash is removed.</param>
        /// <param name="apiKeyVariable">The name of the environment variable that holds the API key; null or empty for none.</param>
        /// <param name="timeoutSeconds">The time-out of one try, in seconds (at least 0.01).</param>
        /// <param name="tries">Tries per request (at least 1).</param>
        /// <param name="backoffSeconds">The wait before the second try, in seconds (at least 0).</param>
        /// <param name="maxParallel">The most requests in flight at once (at least 1).</param>
        /// <param name="transport">What sends the requests; never null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="transport"/> is null.</exception>
        public HttpCaller(string host, string apiKeyVariable, float timeoutSeconds, int tries, float backoffSeconds, int maxParallel,
                          IHttpTransport transport)
        {
            Host = (host ?? "").TrimEnd('/');
            ApiKeyVariable = apiKeyVariable ?? "";
            Timeout = TimeSpan.FromSeconds(Math.Max(0.01, timeoutSeconds));
            Tries = Math.Max(1, tries);
            Backoff = TimeSpan.FromSeconds(Math.Max(0, backoffSeconds));
            MaxParallel = Math.Max(1, maxParallel);
            Transport = transport ?? throw new ArgumentNullException(nameof(transport), "a transport: the component's own SystemHttpTransport or a fake");
            inFlight = new SemaphoreSlim(MaxParallel, MaxParallel);
        }

        /// <summary>Posts a JSON body to a path of the server, with the time-out and tries, and parses the JSON answer.</summary>
        /// <param name="path">The path after the host, such as <c>/v1/completions</c> or <c>/api/chat</c>.</param>
        /// <param name="body">The request body.</param>
        /// <param name="cancel">Cancels the call and the waits between tries.</param>
        /// <returns>A task that gives the answer's JSON.</returns>
        /// <exception cref="HttpFailure">A non-retryable error status, or every try failed.</exception>
        /// <exception cref="JsonReaderException">The answer isn't JSON (not tried again).</exception>
        public async Task<JToken> PostAsync(string path, JObject body, CancellationToken cancel = default)
        {
            string text = await SendAsync("POST", path, body.ToString(Formatting.None), cancel).ConfigureAwait(false);
            return JToken.Parse(text);
        }

        /// <summary>Reads a path of the server with a GET, with the time-out and tries, and parses the JSON answer.</summary>
        /// <param name="path">The path after the host, such as <c>/api/tags</c>.</param>
        /// <param name="cancel">Cancels the call and the waits between tries.</param>
        /// <returns>A task that gives the answer's JSON.</returns>
        /// <exception cref="HttpFailure">A non-retryable error status, or every try failed.</exception>
        /// <exception cref="JsonReaderException">The answer isn't JSON (not tried again).</exception>
        public async Task<JToken> GetAsync(string path, CancellationToken cancel = default)
        {
            string text = await SendAsync("GET", path, null, cancel).ConfigureAwait(false);
            return JToken.Parse(text);
        }

        /// <summary>
        /// Sends one request in one of the <see cref="MaxParallel"/> places, trying again after a time-out, no answer or a
        /// retryable status, with doubling waits (DEC-42); returns the answer's body. Counts everything in <see cref="Stats"/>.
        /// </summary>
        async Task<string> SendAsync(string method, string path, string json, CancellationToken cancel)
        {
            await inFlight.WaitAsync(cancel).ConfigureAwait(false);
            try
            {
                Exception last = null;
                for (int i = 0; i < Tries; i++)
                {
                    if (i > 0)
                    {
                        Stats.AddRetry();
                        var wait = TimeSpan.FromTicks(Backoff.Ticks << (i - 1));                 // 1, 2, 4 … s (DEC-42)
                        if (wait > TimeSpan.Zero) await Task.Delay(wait, cancel).ConfigureAwait(false);
                    }
                    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel))
                    {
                        timeout.CancelAfter(Timeout);
                        var clock = Stopwatch.StartNew();
                        Stats.AddTry();
                        try
                        {
                            var send = Transport.SendAsync(method, Host + path, json, ApiKey(), timeout.Token);
                            // The time-out doesn't rely on the transport honouring cancellation (Mono's HttpClient may not).
                            var first = await Task.WhenAny(send, Task.Delay(Timeout, cancel)).ConfigureAwait(false);
                            if (first != send)
                            {
                                timeout.Cancel();
                                Observe(send);
                                cancel.ThrowIfCancellationRequested();
                                throw new OperationCanceledException();
                            }
                            string text = await send.ConfigureAwait(false);
                            Stats.AddLatency(clock.Elapsed.TotalSeconds);
                            return text;
                        }
                        catch (HttpFailure e) when (!e.Retryable)
                        {
                            // An error status other than 408, 429 and 5xx: another try would get the same answer.
                            Stats.AddFailure();
                            throw;
                        }
                        catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
                        {
                            // Our own time-out, not the caller's cancellation: count it and try again.
                            Stats.AddTimeout();
                            last = new TimeoutException($"no answer in {Timeout.TotalSeconds:0.##} s");
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            // A retryable status or a network error: try again.
                            last = e;
                        }
                    }
                }
                Stats.AddFailure();
                throw new HttpFailure($"{method} {Host}{path} failed after {Tries} tries: {last?.Message}", 0, false, last);
            }
            finally
            {
                inFlight.Release();
            }
        }

        /// <summary>An abandoned request's eventual failure is observed, so it never surfaces as an unobserved exception.</summary>
        static void Observe(Task t) => t.ContinueWith(x => { _ = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        /// <summary>
        /// Reads the API key from its environment variable at each call, or gives null when none is named; never stored (OUT-04).
        /// </summary>
        string ApiKey() =>ApiKeyVariable.Length == 0 ? null : Environment.GetEnvironmentVariable(ApiKeyVariable);

        /// <summary>A float as the JSON number a person would write (1.2, not 1.2000000476837158).</summary>
        /// <param name="value">The number, such as a temperature.</param>
        /// <returns>The JSON number with the float's shortest exact text.</returns>
        public static JValue Number(float value) =>
            new JValue(double.Parse(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture));
    }
}
