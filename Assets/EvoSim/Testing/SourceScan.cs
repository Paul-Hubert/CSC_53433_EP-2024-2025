using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>Reads the EvoSim source files for static checks (T-RAND-01, T-CORE-08, T-ACT-09): comments removed, line numbers kept.</summary>
    public static class SourceScan
    {
        /// <summary>The .cs files under Assets/EvoSim/&lt;folder&gt; (e.g. "Runtime").</summary>
        public static IEnumerable<string> Files(string folder)
        {
            string root = Path.Combine(Application.dataPath, "EvoSim", folder);
            if (!Directory.Exists(root)) yield break;
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            System.Array.Sort(files, System.StringComparer.Ordinal);
            foreach (var f in files) yield return f;
        }

        /// <summary>The code of a file with comments blanked out (strings kept), line breaks preserved.</summary>
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

        /// <summary>Every match of a pattern in the comment-free code of the files, as "file:line: text".</summary>
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

        /// <summary>The string literals of a file's code, with their line numbers.</summary>
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
