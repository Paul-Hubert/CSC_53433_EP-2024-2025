using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test mutator (30 §2): answers mutation prompts with a scripted function (by default it appends "quickly" to the
    /// sentence), records every prompt and batch, and can answer N Advance calls late or fail.
    /// </summary>
    public class FakeMutator : MutatorClient
    {
        /// <summary>The answer for (sentence, seed, full prompt); default: the sentence with "quickly" before its period.</summary>
        public Func<string, long, string, string> Answer = (sentence, seed, prompt) => sentence.TrimEnd('.') + " quickly.";
        /// <summary>A failure message for a request, or null.</summary>
        public Func<string, long, string> FailWhen;
        public int DelayCalls;

        public int Calls;
        public readonly List<string> Prompts = new List<string>();
        public readonly List<int> BatchSizes = new List<int>();

        public override string ModelIdentity => "fake-mutator";

        public override MutatorReply Send(IReadOnlyList<MutatorRequest> requests)
        {
            BatchSizes.Add(requests.Count);
            var reply = DelayCalls > 0 ? new MutatorReply(requests.Count, new AdvancePending(World, DelayCalls)) : new MutatorReply(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                Calls++;
                var r = requests[i];
                Prompts.Add(r.Prompt);
                string sentence = SentenceOf(r.Prompt);
                string fail = FailWhen?.Invoke(sentence, r.Seed);
                if (fail != null) reply.Fail(i, fail);
                else reply.SetAnswer(i, Answer(sentence, r.Seed, r.Prompt));
            }
            reply.ModelCalls = requests.Count;
            reply.Complete();
            return reply;
        }

        /// <summary>The quoted sentence of a mutation prompt (MUT-11).</summary>
        public static string SentenceOf(string prompt)
        {
            int a = prompt.IndexOf("\n\n\"", StringComparison.Ordinal);
            int b = prompt.LastIndexOf("\"\n\n", StringComparison.Ordinal);
            return a >= 0 && b > a ? prompt.Substring(a + 3, b - a - 3) : prompt;
        }
    }
}
