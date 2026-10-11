using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EvoSim
{
    /// <summary>
    /// The LLM mutation's prompt, cleaning and guards (MUT-10…12), as the prototype does them
    /// (prototype/promptevo/evolution/mutation.py). Pure functions.
    /// </summary>
    /// <remarks>
    /// <see cref="LlmMutation"/> reads its deck with <see cref="Instructions"/>; the <see cref="MutatorService"/> builds each
    /// request with <see cref="Prompt"/>, then turns each answer into a mutant with <see cref="Clean"/> and
    /// <see cref="Reject"/>.
    /// </remarks>
    public static class MutationText
    {
        /// <summary>The last line of every mutation prompt (MUT-11).</summary>
        public const string ReplyLine = "Reply with the new sentence only.";

        static readonly Regex Quoted = new Regex("[\"“]([^\"“”]{3,})[\"”]");      // the first quoted part, 3 characters or more
        // A leading "Here is the new rule:", "Modified sentence:"…
        static readonly Regex Preamble = new Regex(@"^(here is|here's|modified|new|mutated|rewritten)[^:]*:\s*", RegexOptions.IgnoreCase);
        static readonly Regex FirstSentence = new Regex(@"^(.+?[.!?])(\s|$)", RegexOptions.Singleline);   // up to the first . ! or ?
        static readonly Regex Allowed = new Regex("^[A-Za-z0-9 ,.'’;:!?-]+$");                // the characters a mutant may hold (MUT-12)
        const string QuoteChars = "\"“”'`";                                                    // stripped from both ends

        /// <summary>The deck's instructions: one per line, blank lines and lines starting with # skipped (MUT-10).</summary>
        /// <param name="deck">The deck's text; null counts as empty.</param>
        /// <returns>The instructions, trimmed, in deck order.</returns>
        public static List<string> Instructions(string deck)
        {
            var list = new List<string>();
            foreach (var raw in (deck ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                list.Add(line);
            }
            return list;
        }

        /// <summary>Exactly MUT-11: the context line (if any), the instruction, the quoted sentence, the reply line.</summary>
        /// <param name="contextLine">The fixed first line, or null or empty for none.</param>
        /// <param name="instruction">One instruction of the deck.</param>
        /// <param name="sentence">The parent sentence.</param>
        /// <returns>
        /// The prompt: <c>{context line}</c>, a line break, <c>{instruction}</c>, a blank line, <c>"{sentence}"</c>, a blank
        /// line, then <see cref="ReplyLine"/>.
        /// </returns>
        public static string Prompt(string contextLine, string instruction, string sentence)
        {
            string p = instruction + "\n\n\"" + sentence + "\"\n\n" + ReplyLine;
            return string.IsNullOrEmpty(contextLine) ? p : contextLine + "\n" + p;
        }

        /// <summary>
        /// MUT-12 cleaning: the quoted part if there is one; quotes and backticks stripped; a leading "Here is …:" dropped;
        /// the first line, then the first sentence; spaces collapsed; a final period; a capital first letter.
        /// </summary>
        /// <param name="answer">The model's raw answer; null counts as empty.</param>
        /// <returns>The cleaned sentence, possibly empty.</returns>
        public static string Clean(string answer)
        {
            string text = answer ?? "";
            var q = Quoted.Match(text);
            string t = q.Success ? q.Groups[1].Value : text;
            t = t.Trim().Trim(QuoteChars.ToCharArray()).Trim();
            t = Preamble.Replace(t, "", 1);
            t = t.Split('\n')[0].Trim().Trim(QuoteChars.ToCharArray());
            var m = FirstSentence.Match(t);
            if (m.Success) t = m.Groups[1].Value;
            t = string.Join(" ", t.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries));
            if (t.Length > 0 && ".!?".IndexOf(t[t.Length - 1]) < 0) t += ".";
            return t.Length > 0 ? char.ToUpperInvariant(t[0]) + t.Substring(1) : t;
        }

        /// <summary>Why a cleaned answer can't be the mutant: "invalid" or "unchanged" (MUT-12); null if it can.</summary>
        /// <param name="cleaned">An answer after <see cref="Clean"/>.</param>
        /// <param name="parent">The sentence the answer should change.</param>
        /// <param name="maxWords">The longest mutant allowed, in words.</param>
        /// <returns>
        /// <c>"invalid"</c> (no word, too many words, equal to the parent ignoring case, or a forbidden character),
        /// <c>"unchanged"</c> (only punctuation differs from the parent), or null when it can be the mutant.
        /// </returns>
        public static string Reject(string cleaned, string parent, int maxWords)
        {
            int n = cleaned.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries).Length;
            if (n == 0 || n > maxWords || cleaned.ToLowerInvariant() == (parent ?? "").ToLowerInvariant() || !Allowed.IsMatch(cleaned))
                return "invalid";
            if (Words(cleaned) == Words(parent)) return "unchanged";
            return null;
        }

        /// <summary>The words of a sentence, lower case, without punctuation (to spot punctuation-only changes).</summary>
        /// <param name="t">The sentence; null counts as empty.</param>
        /// <returns>The words separated by single spaces, without the final . ! ? and without commas, colons and semicolons.</returns>
        public static string Words(string t)
        {
            string s = (t ?? "").ToLowerInvariant().TrimEnd('.', '!', '?').Replace(",", " ").Replace(":", " ").Replace(";", " ");
            return string.Join(" ", s.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
