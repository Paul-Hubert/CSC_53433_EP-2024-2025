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

        public HttpFailure(string message, int status, bool retryable, Exception inner = null) : base(message, inner)
        {
            Status = status;
            Retryable = retryable;
        }

        /// <summary>Whether a status is worth another try.</summary>
        public static bool IsRetryable(int status) => status == 0 || status == 408 || status == 429 || status >= 500;
    }
}
