using System.Threading;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>
    /// Sends one HTTP request and returns the response body; throws HttpFailure for an error status. The real one is
    /// SystemHttpTransport; tests inject a fake (30 §2) so brains and mutators are tested without servers.
    /// </summary>
    public interface IHttpTransport
    {
        /// <param name="json">The request body, or null for a GET.</param>
        /// <param name="apiKey">Sent as a bearer token when not empty; never logged (OUT-04).</param>
        Task<string> SendAsync(string method, string url, string json, string apiKey, CancellationToken cancel);
    }
}
