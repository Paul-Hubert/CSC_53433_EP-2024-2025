namespace EvoSim
{
    /// <summary>The checks a gene sentence must pass, founder or mutant (MUT-12, GENE-22). Pure functions.</summary>
    public static class SentenceGuards
    {
        public const int ReferenceMaxWords = 12;
        /// <summary>Characters allowed besides ASCII letters, digits and spaces (MUT-12).</summary>
        public const string AllowedPunctuation = ",.'’;:!?-";

        /// <summary>Why a sentence is rejected ("too many words", …), or null if it passes.</summary>
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
        public static bool IsAllowed(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' ||
            AllowedPunctuation.IndexOf(c) >= 0;
    }
}
