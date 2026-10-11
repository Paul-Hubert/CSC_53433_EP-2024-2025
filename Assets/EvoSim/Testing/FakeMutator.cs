using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test mutator client (30 §2): answers mutation prompts with a scripted function (by default it appends "quickly" to the
    /// sentence), records every prompt and batch, and can answer N Advance calls late or fail.
    /// </summary>
    /// <remarks>
    /// Add it with <see cref="Lab.FakeMutation"/>, which also adds the <see cref="MutatorService"/> it answers for; the
    /// <see cref="Lab.Lab1"/> world has one already. The mutation operators and guards stay the real ones: only the model is
    /// replaced, so an answer the guards reject is redrawn as usual (MUT-13).
    /// </remarks>
    /// <example>
    /// T-MUT-11: the first three answers repeat the parent sentence, which the guards reject, so they are redrawn in a second batch.
    /// <code>
    /// int calls = 0;
    /// var w = New().Flat(20, 20).Food(0f, 0f).Service&lt;EggSystem&gt;("Eggs")
    ///     .FakeMutation(f =&gt; f.Answer = (sentence, seed, prompt) =&gt;
    ///         ++calls &lt;= 3 ? sentence : sentence.TrimEnd('.') + " quickly.")
    ///     .Configure(x =&gt; x.Asexual = true).Phase&lt;BreedPhase&gt;().Phase&lt;HatchPhase&gt;()
    ///     .Species("prey", s =&gt;
    ///     {
    ///         s.PreyBody().Action&lt;MateAction&gt;("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f);
    ///         s.Get&lt;Litter&gt;().Configure(3, 3, 40f);
    ///     })
    ///     .Build();
    /// var sp = w.AllSpecies[0];
    /// for (int i = 0; i &lt; 4; i++)
    /// {
    ///     var a = Place.Animal(sp, Place.At(5 + 3 * i, 10));
    ///     a.Age = 200; a.SetStat("energy", 100); a.Choose("mate");
    /// }
    /// w.Advance(1);
    /// CollectionAssert.AreEqual(new[] { 12, 3 }, w.GetComponentInChildren&lt;FakeMutator&gt;().BatchSizes);
    /// </code>
    /// </example>
    public class FakeMutator : MutatorClient
    {
        /// <summary>The answer for (sentence, seed, full prompt); the default puts "quickly" before the sentence's period.</summary>
        public Func<string, long, string, string> Answer = (sentence, seed, prompt) => sentence.TrimEnd('.') + " quickly.";
        /// <summary>A failure message for (sentence, seed), or null to answer the request.</summary>
        public Func<string, long, string> FailWhen;
        /// <summary>Answers arrive this many <see cref="World.Advance"/> calls late (T-MUT-12); 0 = at once.</summary>
        public int DelayCalls;

        /// <summary>The requests received so far (one per try, failed ones included), across all batches.</summary>
        public int Calls;
        /// <summary>Every prompt received, in order (T-MUT-06 checks their exact text).</summary>
        public readonly List<string> Prompts = new List<string>();
        /// <summary>The size of each batch received, in order (T-MUT-11).</summary>
        public readonly List<int> BatchSizes = new List<int>();

        /// <summary>"fake-mutator": the model name recorded with the alleles it makes (GENE-12).</summary>
        public override string ModelIdentity => "fake-mutator";

        /// <summary>
        /// Records the batch and answers each request from <see cref="FailWhen"/> or <see cref="Answer"/>, given the sentence
        /// quoted in its prompt, at once or <see cref="DelayCalls"/> Advance calls later.
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>The sentence a mutation prompt quotes: the text between <c>\n\n"</c> and the last <c>"\n\n</c> (MUT-11).</summary>
        /// <param name="prompt">A mutation prompt.</param>
        /// <returns>The quoted sentence, or the whole prompt when it quotes none.</returns>
        public static string SentenceOf(string prompt)
        {
            int a = prompt.IndexOf("\n\n\"", StringComparison.Ordinal);
            int b = prompt.LastIndexOf("\"\n\n", StringComparison.Ordinal);
            return a >= 0 && b > a ? prompt.Substring(a + 3, b - a - 3) : prompt;
        }
    }
}
