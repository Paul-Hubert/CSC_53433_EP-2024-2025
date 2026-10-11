using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>
    /// Wraps a transport and keeps every request body and answer, for integrity checks that inspect what a model
    /// received (B-11). The API key is passed through and never kept (OUT-04).
    /// </summary>
    /// <remarks>Set it as a brain's or mutator client's <c>Transport</c>; it is thread-safe, like every transport.</remarks>
    public sealed class RecordingTransport : IHttpTransport
    {
        readonly IHttpTransport inner;                                         // the transport that really sends
        readonly object gate = new object();                                   // guards the call list
        readonly List<(string Method, string Url, string Body, string Answer)> calls = new List<(string, string, string, string)>();

        /// <summary>Wraps a transport.</summary>
        /// <param name="inner">The transport that really sends; null for a new <see cref="SystemHttpTransport"/>.</param>
        public RecordingTransport(IHttpTransport inner = null) { this.inner = inner ?? new SystemHttpTransport(); }

        /// <summary>A copy of the calls so far: method, URL, request body, answer (null when it failed).</summary>
        public List<(string Method, string Url, string Body, string Answer)> Calls
        {
            get { lock (gate) return new List<(string, string, string, string)>(calls); }
        }

        /// <summary>Sends through the wrapped transport and records the call, whether it succeeded or failed.</summary>
        /// <inheritdoc/>
        public async Task<string> SendAsync(string method, string url, string json, string apiKey, CancellationToken cancel)
        {
            string answer = null;
            try
            {
                answer = await inner.SendAsync(method, url, json, apiKey, cancel).ConfigureAwait(false);
                return answer;
            }
            finally
            {
                lock (gate) calls.Add((method, url, json, answer));
            }
        }
    }
}
