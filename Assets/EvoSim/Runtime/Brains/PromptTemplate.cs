using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A species' prompt with three holes: {genes}, {situation}, {ask} (PROMPT-01, PROMPT-04). Its id is a hash of
    /// this text, so any change to a header, an action line or a rule changes the id and the cache key (PROMPT-05).
    /// </summary>
    public sealed class PromptTemplate
    {
        public string Text { get; }
        public string Id { get; }
        /// <summary>Unknown placeholders and missing holes (V-53).</summary>
        public IReadOnlyList<string> Problems { get; }

        public PromptTemplate(string text, IReadOnlyList<string> problems)
        {
            Text = text;
            Id = Hashing.Sha256Hex(text);
            Problems = new List<string>(problems);
        }

        /// <summary>The full prompt for one query: exactly the three holes filled (PROMPT-04).</summary>
        public string Fill(string genes, string situation, string ask)
        {
            string text = Text.Replace("{genes}", genes ?? "").Replace("{situation}", situation ?? "").Replace("{ask}", ask ?? "");
            return text.TrimEnd('\n', ' ');
        }

        public override string ToString() => Text;
    }
}
