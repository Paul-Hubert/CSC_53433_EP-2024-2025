using System.Threading;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>
    /// Sends one HTTP request and returns the response body; throws HttpFailure for an error status. The real one is
    /// SystemHttpTransport; tests inject a fake (30 §2) so brains and mutators are tested without servers.
    /// </summary>
    /// <remarks>
    /// <para>Set a transport through <see cref="HttpBrain.Transport"/> or <see cref="HttpMutatorClient.Transport"/>; null
    /// gives back the component's own <see cref="SystemHttpTransport"/>. <see cref="HttpCaller"/> wraps it with the
    /// time-outs, tries and parallel cap, so a transport sends exactly one request per call.</para>
    /// <para>It is called from the thread pool, several calls at once: an implementation must be thread-safe and must not
    /// touch Unity objects. <c>EvoSim.Testing.FakeHttpTransport</c> (scripted answers, failures, hangs) and
    /// <see cref="RecordingTransport"/> (keeps every call) are the implementations to read.</para>
    /// </remarks>
    /// <example>
    /// A test gives a JEV brain a fake server instead of a real one:
    /// <code>
    /// var fake = FakeHttpTransport.Jev();
    /// jev.Transport = fake;
    /// jev.ConfigureClient("http://jev.test", 5f, 3, 0f, 4);
    /// </code>
    /// </example>
    public interface IHttpTransport
    {
        /// <summary>Sends one request and returns the response body.</summary>
        /// <param name="method"><c>GET</c> or <c>POST</c>.</param>
        /// <param name="url">The full address: the host and the path.</param>
        /// <param name="json">The request body, or null for a GET.</param>
        /// <param name="apiKey">Sent as a bearer token when not empty; never logged (OUT-04).</param>
        /// <param name="cancel">Cancelled by the caller at its time-out; the caller doesn't rely on the transport honouring it.</param>
        /// <returns>A task that gives the response body.</returns>
        /// <exception cref="HttpFailure">The server answered with an error status, with whether another try may succeed.</exception>
        Task<string> SendAsync(string method, string url, string json, string apiKey, CancellationToken cancel);
    }
}
