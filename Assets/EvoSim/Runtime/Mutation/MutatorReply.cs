namespace EvoSim
{
    /// <summary>The answers to a batch of mutator requests, filled later: an answer or an error per request.</summary>
    /// <remarks>
    /// A <see cref="MutatorClient"/> makes and fills it; the <see cref="MutatorService"/> reads it once
    /// <see cref="Pending"/> is done.
    /// </remarks>
    public sealed class MutatorReply
    {
        readonly string[] answers, errors;                           // per request: the answer, or null and why
        readonly ManualPending own;                                  // the reply's own pending, when none was given

        /// <summary>The number of requests in the batch.</summary>
        public int Count => answers.Length;
        /// <summary>Done when every answer or error is in.</summary>
        public Pending Pending { get; }
        /// <summary>The model calls the batch made, for the statistics (MUT-05); 0 counts one call per request.</summary>
        public int ModelCalls { get; set; }

        /// <summary>Makes an empty reply for a batch.</summary>
        /// <param name="count">The number of requests.</param>
        /// <param name="wait">What tells when the reply is complete; null: the reply's own pending, which
        /// <see cref="Complete"/> sets done.</param>
        public MutatorReply(int count, Pending wait = null)
        {
            answers = new string[count];
            errors = new string[count];
            if (wait != null) Pending = wait;
            else { own = new ManualPending(); Pending = own; }
        }

        /// <summary>Records the answer to one request.</summary>
        /// <param name="i">The request's index in the batch.</param>
        /// <param name="text">The model's raw answer.</param>
        public void SetAnswer(int i, string text) => answers[i] = text;
        /// <summary>Records that one request failed: it gets no answer.</summary>
        /// <param name="i">The request's index in the batch.</param>
        /// <param name="error">Why; null counts as <c>"failed"</c>.</param>
        public void Fail(int i, string error) { answers[i] = null; errors[i] = error ?? "failed"; }
        /// <summary>
        /// Marks the reply complete, once every answer or error is in. Does nothing when the constructor was given a
        /// pending: that one decides.
        /// </summary>
        public void Complete() => own?.SetDone();

        /// <summary>The answer to one request.</summary>
        /// <param name="i">The request's index in the batch.</param>
        /// <returns>The raw answer, or null when the request failed or has no answer.</returns>
        public string Answer(int i) => answers[i];
        /// <summary>Why one request has no answer.</summary>
        /// <param name="i">The request's index in the batch.</param>
        /// <returns>The error (<c>"no answer"</c> when none was recorded), or null when the request has an answer.</returns>
        public string Error(int i) => answers[i] == null ? errors[i] ?? "no answer" : null;
    }
}
