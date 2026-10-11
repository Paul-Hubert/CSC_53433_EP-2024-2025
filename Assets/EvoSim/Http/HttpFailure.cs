using System;

namespace EvoSim
{
    /// <summary>An HTTP call that failed: an error status, or every try used (DEC-40, DEC-42). Messages never hold keys (OUT-04).</summary>
    public class HttpFailure : Exception
    {
        /// <summary>The HTTP status, or 0 when no answer came.</summary>
        public int Status { get; }
        /// <summary>Whether another try may succeed: no answer, a time-out, 408, 429 or 5xx.</summary>
        public bool Retryable { get; }

        /// <summary>Makes a failure with its message, its status and whether another try may succeed.</summary>
        /// <param name="message">What failed, for logs and the stop reason; never an API key.</param>
        /// <param name="status">The HTTP status, or 0 when no answer came.</param>
        /// <param name="retryable">True when another try may succeed (see <see cref="IsRetryable"/>).</param>
        /// <param name="inner">The error that caused it, or null.</param>
        public HttpFailure(string message, int status, bool retryable, Exception inner = null) : base(message, inner)
        {
            Status = status;
            Retryable = retryable;
        }

        /// <summary>Tells whether a status is worth another try.</summary>
        /// <param name="status">The HTTP status, or 0 for no answer.</param>
        /// <returns>True for 0, 408 (time-out), 429 (too many requests) and 5xx (server errors).</returns>
        public static bool IsRetryable(int status) => status == 0 || status == 408 || status == 429 || status >= 500;
    }
}
