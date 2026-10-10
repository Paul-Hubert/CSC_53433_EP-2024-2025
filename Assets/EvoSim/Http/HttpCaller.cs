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
    public sealed class HttpCaller
    {
        readonly SemaphoreSlim inFlight;

        public string Host { get; }
        public string ApiKeyVariable { get; }
        public TimeSpan Timeout { get; }
        public int Tries { get; }
        public TimeSpan Backoff { get; }
        public int MaxParallel { get; }
        public IHttpTransport Transport { get; }
        public HttpStats Stats { get; } = new HttpStats();

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

        public async Task<JToken> PostAsync(string path, JObject body, CancellationToken cancel = default)
        {
            string text = await SendAsync("POST", path, body.ToString(Formatting.None), cancel).ConfigureAwait(false);
            return JToken.Parse(text);
        }

        public async Task<JToken> GetAsync(string path, CancellationToken cancel = default)
        {
            string text = await SendAsync("GET", path, null, cancel).ConfigureAwait(false);
            return JToken.Parse(text);
        }

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
                            Stats.AddFailure();
                            throw;
                        }
                        catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
                        {
                            Stats.AddTimeout();
                            last = new TimeoutException($"no answer in {Timeout.TotalSeconds:0.##} s");
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
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

        string ApiKey() =>ApiKeyVariable.Length == 0 ? null : Environment.GetEnvironmentVariable(ApiKeyVariable);

        /// <summary>A float as the JSON number a person would write (1.2, not 1.2000000476837158).</summary>
        public static JValue Number(float value) =>
            new JValue(double.Parse(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture));
    }
}
