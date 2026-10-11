namespace EvoSim
{
    /// <summary>The checks a gene sentence must pass, founder or mutant (MUT-12, GENE-22). Pure functions.</summary>
    /// <remarks>
    /// <see cref="TextGene.Check"/> applies them, with the gene's word limit: to founders before the run (V-40) and to every
    /// mutant at hatching. The LLM operator's answers also go through <see cref="MutationText.Reject"/>, which adds the
    /// "same as the parent" and "only punctuation changed" checks.
    /// </remarks>
    public static class SentenceGuards
    {
        /// <summary>The reference word limit of a gene sentence: 12 words (GENE-22).</summary>
        public const int ReferenceMaxWords = 12;
        /// <summary>Characters allowed besides ASCII letters, digits and spaces (MUT-12).</summary>
        public const string AllowedPunctuation = ",.'’;:!?-";

        /// <summary>Why a sentence is rejected ("too many words", …), or null if it passes.</summary>
        /// <param name="sentence">The sentence; null counts as empty.</param>
        /// <param name="maxWords">The longest sentence allowed, in words (reference 12).</param>
        /// <returns><c>"empty"</c>, <c>"13 words (at most 12)"</c>, <c>"forbidden character '…'"</c>, or null when it passes.</returns>
        public static string Check(string sentence, int maxWords = ReferenceMaxWords)
        {
            if (sentence == null) return "empty";
            int words = CountWords(sentence);
            if (words == 0) return "empty";
            if (words > maxWords) return $"{words} words (at most {maxWords})";
            foreach (char c in sentence)
                if (!IsAllowed(c)) return $"forbidden character '{c}'";
            return null;
        }

        /// <summary>Counts the words of a sentence: the runs of characters between whitespace.</summary>
        /// <param name="s">The sentence; must not be null.</param>
        /// <returns>The number of words; 0 for an empty or blank sentence.</returns>
        public static int CountWords(string s)
        {
            int n = 0;
            bool inWord = false;
            foreach (char c in s)
            {
                bool space = char.IsWhiteSpace(c);
                if (!space && !inWord) n++;
                inWord = !space;
            }
            return n;
        }

        /// <summary>ASCII letters and digits, a space, or the allowed punctuation.</summary>
        /// <param name="c">One character of a sentence.</param>
        /// <returns>True when a gene sentence may hold it; false for any other character, tabs and line breaks included.</returns>
        public static bool IsAllowed(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' ||
            AllowedPunctuation.IndexOf(c) >= 0;
    }
}
