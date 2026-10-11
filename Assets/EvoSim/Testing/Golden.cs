using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Golden files under Assets/EvoSim/Tests/Golden/ (30 §2): situation texts, prompts, hashes. A missing file is
    /// written from the current output (pinned from the first accepted run); an existing one must match exactly.
    /// Re-pinning is a deliberate commit that deletes or edits the file and says why.
    /// </summary>
    /// <remarks>
    /// Line endings are compared as LF, so a file edited on Windows still matches. A failed check names the first line that
    /// differs, with the golden text and the new one.
    /// </remarks>
    /// <example>
    /// T-SENSE-08 pins every situation text of the reference species:
    /// <code>
    /// var sb = new StringBuilder();
    /// foreach (var o in preyObservations)
    ///     sb.Append("prey\t").Append(o.Key).Append("\tV1\t").Append(prey.Describe(o, TextStyle.V1)).Append('\n');
    /// Golden.Check("situations.tsv", sb.ToString());
    /// </code>
    /// </example>
    public static class Golden
    {
        /// <summary>The folder of the golden files: <c>Assets/EvoSim/Tests/Golden</c>, as a full path.</summary>
        public static string Folder => Path.Combine(Application.dataPath, "EvoSim", "Tests", "Golden");

        /// <summary>The full path of a golden file.</summary>
        /// <param name="name">The file's name, e.g. <c>"situations.tsv"</c>.</param>
        /// <returns>The path under <see cref="Folder"/>.</returns>
        public static string PathOf(string name) => Path.Combine(Folder, name);

        /// <summary>True when a golden file of this name has been pinned.</summary>
        /// <param name="name">The file's name.</param>
        /// <returns>Whether the file exists.</returns>
        public static bool Exists(string name) => File.Exists(PathOf(name));

        /// <summary>Reads a golden file, with LF line endings.</summary>
        /// <param name="name">The file's name.</param>
        /// <returns>The file's text, or null when it doesn't exist.</returns>
        public static string Read(string name) => File.Exists(PathOf(name)) ? File.ReadAllText(PathOf(name)).Replace("\r\n", "\n") : null;

        /// <summary>
        /// Compares a test's output with its golden file and fails the test at the first different line; writes the file
        /// (UTF-8, LF) when it doesn't exist yet, which pins the output.
        /// </summary>
        /// <param name="name">The golden file's name, e.g. <c>"prompts.txt"</c>.</param>
        /// <param name="content">The output to compare; CRLF line endings count as LF.</param>
        public static void Check(string name, string content)
        {
            content = content.Replace("\r\n", "\n");
            var existing = Read(name);
            if (existing == null)
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(PathOf(name), System.Text.Encoding.UTF8.GetBytes(content));
                Debug.Log($"EvoSim: pinned the golden file {name} from this run.");
                return;
            }
            if (existing == content) return;
            var a = existing.Split('\n');
            var b = content.Split('\n');
            int line = 0;
            while (line < a.Length && line < b.Length && a[line] == b[line]) line++;
            Assert.Fail($"Golden file {name} differs at line {line + 1}:\n  golden: {(line < a.Length ? a[line] : "<end>")}\n  now:    {(line < b.Length ? b[line] : "<end>")}");
        }
    }
}
