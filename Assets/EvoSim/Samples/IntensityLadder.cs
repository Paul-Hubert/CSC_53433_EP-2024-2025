using System;
using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §8 (MUT-21): a rule-based text operator that moves the sentence's intensity word one step along
    /// never, rarely, sometimes, often, always — no model. It sees only the gene and its stream (MUT-30).
    /// </summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A mutation of text genes without a language model: "Rest often when tired." becomes "Rest
    /// always when tired." or "Rest sometimes when tired.", keeping the word's capital and punctuation. A sentence without an
    /// intensity word fails the job ("no intensity word"), and the gene stays as it was.</para>
    /// <para><b>Mechanism.</b> A <see cref="MutationOperator"/>: <see cref="Accepts"/> says which genes it can change, and
    /// <see cref="StartMutation"/> draws its random choice from the stream it is given and returns a finished job. It is blind
    /// (CORE-07): it never reads the world or other genes. The usual guards still check its result.</para>
    /// <para><b>In a scene.</b> Add it under the species' <c>Mutation</c> object and list in its <c>onlyThese</c> the genes it
    /// applies to; the other text genes keep the species' default operator (MUT-02).</para>
    /// </remarks>
    public class IntensityLadder : MutationOperator
    {
        static readonly string[] Ladder = { "never", "rarely", "sometimes", "often", "always" };   // the intensity words, weakest first

        /// <summary>Text genes only.</summary>
        /// <inheritdoc/>
        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;

        /// <summary>
        /// Finds the sentence's first intensity word and moves it one step up or down the ladder (one coin flip from
        /// <paramref name="rng"/>), clamped at both ends; fails when the sentence has none.
        /// </summary>
        /// <param name="g">The gene being mutated (not read).</param>
        /// <param name="parent">The allele to change; its text is read.</param>
        /// <param name="rng">The mutation stream: the only source of randomness (MUT-30).</param>
        /// <returns>A finished job: the new sentence with the detail "ladder+1" or "ladder-1", or a failure.</returns>
        public override MutationJob StartMutation(Gene g, Allele parent, RandomStream rng)
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
            // Keep the word's trailing punctuation and its capital letter.
            string original = words[i];
            string trail = original.Substring(original.TrimEnd('.', ',', '!', '?', ';', ':').Length);
            if (char.IsUpper(original[0])) next = char.ToUpperInvariant(next[0]) + next.Substring(1);
            words[i] = next + trail;
            return MutationJob.Done(string.Join(" ", words), step > 0 ? "ladder+1" : "ladder-1");
        }
    }
}
