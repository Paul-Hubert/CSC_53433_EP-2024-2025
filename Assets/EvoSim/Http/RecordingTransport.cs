using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>
    /// Wraps a transport and keeps every request body and answer, for integrity checks that inspect what a model
    /// received (B-11). The API key is passed through and never kept (OUT-04).
    /// </summary>
    public sealed class RecordingTransport : IHttpTransport
    {
        readonly IHttpTransport inner;
        readonly object gate = new object();
        readonly List<(string Method, string Url, string Body, string Answer)> calls = new List<(string, string, string, string)>();

        public RecordingTransport(IHttpTransport inner = null) { this.inner = inner ?? new SystemHttpTransport(); }

        /// <summary>A copy of the calls so far: method, URL, request body, answer (null when it failed).</summary>
        public List<(string Method, string Url, string Body, string Answer)> Calls
        {
            get { lock (gate) return new List<(string, string, string, string)>(calls); }
        }

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
