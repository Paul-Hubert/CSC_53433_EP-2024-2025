using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference operator for text genes (10 §2): one instruction drawn from a deck, sent to the mutator with the
    /// sentence after one fixed context line, and nothing else (CORE-07, MUT-11). Every try's instruction and seed are
    /// drawn now (MUT-30); rejected answers are drawn again, up to <c>tries</c> (MUT-13).
    /// </summary>
    /// <remarks>
    /// <para>What the reference does, step by step (the exercise MyLlmMutation rewrites it):</para>
    /// <list type="number">
    /// <item><description><see cref="Initialize"/> reads the deck: one instruction per line, blank lines and lines starting
    /// with <c>#</c> skipped (<see cref="MutationText.Instructions"/>, MUT-10).</description></item>
    /// <item><description><see cref="StartMutation"/> draws, for each of the <c>tries</c> tries in order, an instruction index
    /// (<c>rng.Range(0, count)</c>) and a seed (<c>rng.NextUInt() &amp; 0x7FFFFFFF</c>): all of them at conception, even the
    /// tries a good first answer will make useless, so the stream never depends on the answers (MUT-30).</description></item>
    /// <item><description>It submits the parent sentence, these draws, the context line, the deck and the word limit (the
    /// smaller of its own and the gene's) to the World's <see cref="MutatorService"/>, which returns a running job.</description></item>
    /// <item><description>The service sends the first tries of the tick as one batch, cleans and checks each answer
    /// (<see cref="MutationText.Clean"/>, <see cref="MutationText.Reject"/>, MUT-12), and sends a rejected answer's next
    /// try in the next batch (MUT-13). When every try is rejected the job fails and the gene keeps its allele
    /// (MUT-04).</description></item>
    /// <item><description>Without a MutatorService, or with an empty deck, the job fails at once (<c>"no mutator"</c>),
    /// after the draws.</description></item>
    /// </list>
    /// <para>The mutator is blind (CORE-07): its prompt holds the context line, the instruction and the sentence, nothing
    /// about the animal or the world. A mutant's operator detail is <c>llm#n</c>, n being the instruction's index in the deck.</para>
    /// </remarks>
    public class LlmMutation : MutationOperator
    {
        [SerializeField, Tooltip("One instruction per line; lines starting with # are ignored (MUT-10). Reference: mutate_v4.")]
        TextAsset deck;
        [SerializeField, Tooltip("The one fixed line before the instruction (empty = none).")]
        string contextLine = "The sentence below is a rule that a wild animal follows.";
        [SerializeField, Range(1, 20), Tooltip("Tries before the gene stays unchanged (reference 5).")]
        int tries = 5;
        [SerializeField, Range(1, 64), Tooltip("Longest mutant, in words (reference 12).")]
        int maxWords = SentenceGuards.ReferenceMaxWords;

        List<string> instructions = new List<string>();
        static readonly char[] LineBreaks = { '\n', '\r' };          // a context line must hold none (V-49)

        /// <summary>The deck's instructions, in deck order; the n of a mutant's <c>llm#n</c> is an index into it.</summary>
        public IReadOnlyList<string> Instructions => instructions;
        /// <summary>The fixed line written before the instruction (MUT-11), or empty for none.</summary>
        public string ContextLine => contextLine;
        /// <summary>The longest mutant allowed, in words (reference 12); a text gene's own limit applies when smaller.</summary>
        public int MaxWords => maxWords;

        /// <summary>Reads the deck's instructions (MUT-10).</summary>
        /// <inheritdoc/>
        public override void Initialize() => instructions = MutationText.Instructions(deck != null ? deck.text : "");

        /// <summary>Text genes.</summary>
        /// <inheritdoc/>
        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;
        /// <summary>True: the work goes to the World's <see cref="MutatorService"/>.</summary>
        /// <inheritdoc/>
        public override bool UsesMutatorService => true;

        /// <summary>
        /// Draws every try's instruction and seed (MUT-30), then submits them to the World's <see cref="MutatorService"/>;
        /// the job finishes when an answer passes the guards, or fails when every try is rejected.
        /// </summary>
        /// <inheritdoc/>
        public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng)
        {
            // Every try is drawn now, used or not: the stream must not depend on the answers (MUT-30).
            var plan = new int[tries];
            var seeds = new long[tries];
            for (int i = 0; i < tries; i++)
            {
                plan[i] = rng.Range(0, Mathf.Max(1, instructions.Count));
                seeds[i] = rng.NextUInt() & 0x7FFFFFFF;
            }
            var service = World.Service<MutatorService>();
            if (service == null || instructions.Count == 0) return MutationJob.Failed("no mutator");
            int words = gene is TextGene tg ? Mathf.Min(maxWords, tg.MaxWords) : maxWords;   // the stricter limit
            return service.Submit(parent.Text, plan, seeds, contextLine, instructions, words);
        }

        /// <summary>Sets the operator from code (WorldBuilder, tests).</summary>
        /// <param name="deckAsset">The deck: one instruction per line.</param>
        /// <param name="context">The fixed line before the instruction; empty for none.</param>
        /// <param name="triesCount">Tries before the gene stays unchanged (reference 5).</param>
        /// <param name="words">The longest mutant, in words (reference 12).</param>
        public void Configure(TextAsset deckAsset, string context, int triesCount = 5, int words = 12)
        {
            deck = deckAsset;
            contextLine = context;
            tries = triesCount;
            maxWords = words;
        }

        /// <summary>
        /// Also reports a missing or empty deck (V-20), a missing mutator service or client (V-63), and a context line with a
        /// line break or a placeholder (V-49).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (deck == null || MutationText.Instructions(deck.text).Count == 0)
                report.Error("V-20", this, "LLM mutation needs a deck with at least one instruction (MUT-10).");
            if (Rate > 0f && !World.NoMutation)
            {
                var service = World.Service<MutatorService>();
                if (service == null)
                    report.Warning("V-63", this, "LLM mutation without a MutatorService: text genes never mutate.");
                else if (!service.HasClient)
                    report.Warning("V-63", service, "The MutatorService has no mutator client: every LLM mutation fails (\"no mutator\").");
            }
            if (contextLine.IndexOfAny(LineBreaks) >= 0 || contextLine.IndexOf('{') >= 0)
                report.Error("V-49", this, "The context line must be one fixed line without placeholders: the mutator sees nothing of the world (CORE-07, MUT-11).",
                             new ValidationFix("Keep the first line", () => contextLine = contextLine.Split(LineBreaks)[0].Replace("{", "").Replace("}", "")));
        }
    }
}
