using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>
    /// The real transport: an HttpClient owned by one brain or mutator client (ARCH-07: nothing shared between worlds),
    /// every await with ConfigureAwait(false) so answers never need the main thread (the Freeze mode blocks it,
    /// SPACE-14). Time-outs come from the caller (DEC-42).
    /// </summary>
    public sealed class SystemHttpTransport : IHttpTransport, IDisposable
    {
        readonly HttpClient client;                                            // no time-out of its own: HttpCaller has one

        /// <summary>Makes the HttpClient, and raises the process's connection limit per server to 64.</summary>
        public SystemHttpTransport()
        {
            // Mono allows 2 connections per server by default; the brains' own cap (maxParallel) is the limit that counts.
            if (System.Net.ServicePointManager.DefaultConnectionLimit < 64) System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        }

        /// <summary>
        /// Sends the request with a JSON body and a bearer token when given; an error status throws an
        /// <see cref="HttpFailure"/> with the status, whether to try again, and the start of the body.
        /// </summary>
        /// <inheritdoc/>
        public async Task<string> SendAsync(string method, string url, string json, string apiKey, CancellationToken cancel)
        {
            using (var request = new HttpRequestMessage(new HttpMethod(method), url))
            {
                if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                using (var response = await client.SendAsync(request, cancel).ConfigureAwait(false))
                {
                    string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    int status = (int)response.StatusCode;
                    if (!response.IsSuccessStatusCode)                             // the path only: a URL could carry a key
                        throw new HttpFailure($"{method} {new Uri(url).AbsolutePath}: HTTP {status} {Short(text)}", status, HttpFailure.IsRetryable(status));
                    return text;
                }
            }
        }

        /// <summary>Releases the HttpClient. The owning brain or mutator client calls it when it is destroyed.</summary>
        public void Dispose() => client.Dispose();

        /// <summary>The start of a response body for an error message: at most 300 characters.</summary>
        static string Short(string s) => string.IsNullOrEmpty(s) ? "" : s.Length <= 300 ? s : s.Substring(0, 300) + "…";
    }
}
