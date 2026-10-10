using System;
using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §8 (MUT-21): a rule-based text operator that moves the sentence's intensity word one step along
    /// never, rarely, sometimes, often, always — no model. It sees only the gene and its stream (MUT-30).
    /// </summary>
    public class IntensityLadder : MutationOperator
    {
        static readonly string[] Ladder = { "never", "rarely", "sometimes", "often", "always" };

        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;

        public override MutationJob Start(Gene g, Allele parent, RandomStream rng)
        {
            var words = parent.Text.Split(' ');
            int i = -1, k = -1;
            for (int w = 0; w < words.Length && i < 0; w++)
            {
                k = Array.IndexOf(Ladder, words[w].ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':'));
                if (k >= 0) i = w;
            }
            if (i < 0) return MutationJob.Failed("no intensity word");
            int step = rng.NextBool() ? 1 : -1;
            string next = Ladder[Mathf.Clamp(k + step, 0, Ladder.Length - 1)];
            string original = words[i];
            string trail = original.Substring(original.TrimEnd('.', ',', '!', '?', ';', ':').Length);
            if (char.IsUpper(original[0])) next = char.ToUpperInvariant(next[0]) + next.Substring(1);
            words[i] = next + trail;
            return MutationJob.Done(string.Join(" ", words), step > 0 ? "ladder+1" : "ladder-1");
        }
    }
}
