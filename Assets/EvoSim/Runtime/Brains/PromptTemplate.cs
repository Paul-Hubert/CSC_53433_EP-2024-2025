using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A species' prompt with three holes: {genes}, {situation}, {ask} (PROMPT-01, PROMPT-04). Its id is a hash of
    /// this text, so any change to a header, an action line or a rule changes the id and the cache key (PROMPT-05).
    /// </summary>
    /// <remarks>
    /// <see cref="PromptWriter.Build"/> makes it when the run starts (<see cref="Species.Prompt"/>); each query then fills
    /// the holes with <see cref="DecisionQuery.Prompt"/>. Module placeholders such as {runSpeed} are already filled in
    /// <see cref="Text"/>: only the three holes remain.
    /// </remarks>
    public sealed class PromptTemplate
    {
        /// <summary>The template: header, action lines, rule lines, genes intro, and the three holes.</summary>
        public string Text { get; }

        /// <summary>The prompt id: a 16-digit hexadecimal SHA-256 prefix of <see cref="Text"/>, part of cache keys (PROMPT-05).</summary>
        public string Id { get; }

        /// <summary>Unknown placeholders and missing holes (V-53).</summary>
        public IReadOnlyList<string> Problems { get; }

        /// <summary>Makes a template and computes its id.</summary>
        /// <param name="text">The template text, module placeholders filled, the three holes kept.</param>
        /// <param name="problems">What went wrong while writing it (copied); validation reports them as V-53.</param>
        public PromptTemplate(string text, IReadOnlyList<string> problems)
        {
            Text = text;
            Id = Hashing.Sha256Hex(text);
            Problems = new List<string>(problems);
        }

        /// <summary>Fills the three holes to make the full prompt for one query (PROMPT-04).</summary>
        /// <param name="genes">The genes block (<see cref="DecisionQuery.GenesBlock"/>) for {genes}; null for empty.</param>
        /// <param name="situation">The situation text for {situation}; null for empty.</param>
        /// <param name="ask">The brain's answer instruction for {ask}; null for empty.</param>
        /// <returns>The prompt, without trailing new lines or spaces.</returns>
        public string Fill(string genes, string situation, string ask)
        {
            string text = Text.Replace("{genes}", genes ?? "").Replace("{situation}", situation ?? "").Replace("{ask}", ask ?? "");
            return text.TrimEnd('\n', ' ');
        }

        /// <summary>Gives <see cref="Text"/>.</summary>
        /// <inheritdoc/>
        public override string ToString() => Text;
    }
}
