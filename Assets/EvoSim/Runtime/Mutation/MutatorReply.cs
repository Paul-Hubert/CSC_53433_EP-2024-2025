namespace EvoSim
{
    /// <summary>The answers to a batch of mutator requests, filled later: an answer or an error per request.</summary>
    public sealed class MutatorReply
    {
        readonly string[] answers, errors;
        readonly ManualPending own;

        public int Count => answers.Length;
        /// <summary>Done when every answer or error is in.</summary>
        public Pending Pending { get; }
        public int ModelCalls { get; set; }

        public MutatorReply(int count, Pending wait = null)
        {
            answers = new string[count];
            errors = new string[count];
            if (wait != null) Pending = wait;
            else { own = new ManualPending(); Pending = own; }
        }

        public void SetAnswer(int i, string text) => answers[i] = text;
        public void Fail(int i, string error) { answers[i] = null; errors[i] = error ?? "failed"; }
        public void Complete() => own?.SetDone();

        public string Answer(int i) => answers[i];
        public string Error(int i) => answers[i] == null ? errors[i] ?? "no answer" : null;
    }
}
