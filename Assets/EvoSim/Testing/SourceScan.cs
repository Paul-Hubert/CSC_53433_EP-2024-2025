using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Reads the EvoSim source files for static checks (T-RAND-01, T-CORE-08, T-ACT-09): comments removed, line numbers kept.
    /// </summary>
    /// <remarks>
    /// A static check reads code instead of running it: "no action reads a gene's text", "no <c>UnityEngine.Random</c> in the
    /// runtime". Comments are blanked so that a rule named in a comment is not a hit; strings are kept.
    /// </remarks>
    /// <example>
    /// T-ACT-09: no action reads gene values or allele texts.
    /// <code>
    /// var files = new[] { "Runtime/Actions", "Runtime/Senses" }.SelectMany(SourceScan.Files);
    /// var hits = SourceScan.Find(files, @"\.Gene\b|Genome|Allele|FounderPool");
    /// Assert.IsEmpty(hits, string.Join("\n", hits));
    /// </code>
    /// </example>
    public static class SourceScan
    {
        /// <summary>The .cs files under Assets/EvoSim/&lt;folder&gt;, sub-folders included, in a fixed (ordinal) order.</summary>
        /// <param name="folder">A folder relative to Assets/EvoSim, e.g. <c>"Runtime"</c> or <c>"Runtime/Actions"</c>.</param>
        /// <returns>Full paths; none when the folder doesn't exist.</returns>
        public static IEnumerable<string> Files(string folder)
        {
            string root = Path.Combine(Application.dataPath, "EvoSim", folder);
            if (!Directory.Exists(root)) yield break;
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            System.Array.Sort(files, System.StringComparer.Ordinal);
            foreach (var f in files) yield return f;
        }

        /// <summary>The code of a file with its <c>//</c> and <c>/* */</c> comments blanked out; strings and line breaks kept.</summary>
        /// <param name="path">The file's full path.</param>
        /// <returns>The code, with the same number of lines as the file.</returns>
        public static string Code(string path)
        {
            string s = File.ReadAllText(path);
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    while (i < s.Length && s[i] != '\n') i++;
                    if (i < s.Length) sb.Append('\n');
                    continue;
                }
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/')) { if (s[i] == '\n') sb.Append('\n'); i++; }
                    i++;
                    continue;
                }
                if (c == '"' || c == '\'')
                {
                    // Copy a string or char literal whole, so "//" inside it is not taken for a comment.
                    char q = c;
                    bool verbatim = c == '"' && i > 0 && s[i - 1] == '@';
                    sb.Append(c);
                    for (i++; i < s.Length; i++)
                    {
                        sb.Append(s[i]);
                        if (!verbatim && s[i] == '\\' && i + 1 < s.Length) { sb.Append(s[++i]); continue; }
                        if (s[i] == q) break;
                    }
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Every line of the files' comment-free code that matches a pattern.</summary>
        /// <param name="files">The files to read, e.g. from <see cref="Files"/>.</param>
        /// <param name="pattern">A .NET regular expression, matched line by line.</param>
        /// <returns>One <c>"file:line: text"</c> entry per matching line; empty when nothing matches.</returns>
        public static List<string> Find(IEnumerable<string> files, string pattern)
        {
            var hits = new List<string>();
            var re = new Regex(pattern);
            foreach (var f in files)
            {
                var lines = Code(f).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    if (re.IsMatch(lines[i])) hits.Add($"{Path.GetFileName(f)}:{i + 1}: {lines[i].Trim()}");
            }
            return hits;
        }

        /// <summary>The string literals of a file's comment-free code, with line numbers (T-CORE-08 looks for species names).</summary>
        /// <param name="path">The file's full path.</param>
        /// <returns>Each literal's line (from 1) and text, still escaped as in the source.</returns>
        public static List<(int line, string text)> Literals(string path)
        {
            var result = new List<(int, string)>();
            var lines = Code(path).Split('\n');
            var re = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
            for (int i = 0; i < lines.Length; i++)
                foreach (Match m in re.Matches(lines[i])) result.Add((i + 1, m.Groups[1].Value));
            return result;
        }
    }
}
