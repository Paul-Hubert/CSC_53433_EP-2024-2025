using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference operator for text genes (10 §2): one instruction drawn from a deck, sent to the mutator with the
    /// sentence after one fixed context line, and nothing else (CORE-07, MUT-11). Every try's instruction and seed are
    /// drawn now (MUT-30); rejected answers are drawn again, up to `tries` (MUT-13).
    /// </summary>
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
        static readonly char[] LineBreaks = { '\n', '\r' };

        public IReadOnlyList<string> Instructions => instructions;
        public string ContextLine => contextLine;
        public int MaxWords => maxWords;

        public override void Initialize() => instructions = MutationText.Instructions(deck != null ? deck.text : "");

        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;
        public override bool UsesMutatorService => true;

        public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng)
        {
            var plan = new int[tries];
            var seeds = new long[tries];
            for (int i = 0; i < tries; i++)
            {
                plan[i] = rng.Range(0, Mathf.Max(1, instructions.Count));
                seeds[i] = rng.NextUInt() & 0x7FFFFFFF;
            }
            var service = World.Service<MutatorService>();
            if (service == null || instructions.Count == 0) return MutationJob.Failed("no mutator");
            int words = gene is TextGene tg ? Mathf.Min(maxWords, tg.MaxWords) : maxWords;
            return service.Submit(parent.Text, plan, seeds, contextLine, instructions, words);
        }

        public void Configure(TextAsset deckAsset, string context, int triesCount = 5, int words = 12)
        {
            deck = deckAsset;
            contextLine = context;
            tries = triesCount;
            maxWords = words;
        }

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
