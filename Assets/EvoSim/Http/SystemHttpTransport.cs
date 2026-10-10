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
        readonly HttpClient client;

        public SystemHttpTransport()
        {
            // Mono allows 2 connections per server by default; the brains' own cap (maxParallel) is the limit that counts.
            if (System.Net.ServicePointManager.DefaultConnectionLimit < 64) System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        }

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
                    if (!response.IsSuccessStatusCode)
                        throw new HttpFailure($"{method} {new Uri(url).AbsolutePath}: HTTP {status} {Short(text)}", status, HttpFailure.IsRetryable(status));
                    return text;
                }
            }
        }

        public void Dispose() => client.Dispose();

        static string Short(string s) => string.IsNullOrEmpty(s) ? "" : s.Length <= 300 ? s : s.Substring(0, 300) + "…";
    }
}
