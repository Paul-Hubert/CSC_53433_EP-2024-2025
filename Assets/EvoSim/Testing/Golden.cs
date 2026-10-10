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
    public static class Golden
    {
        public static string Folder => Path.Combine(Application.dataPath, "EvoSim", "Tests", "Golden");

        public static string PathOf(string name) => Path.Combine(Folder, name);

        public static bool Exists(string name) => File.Exists(PathOf(name));

        public static string Read(string name) => File.Exists(PathOf(name)) ? File.ReadAllText(PathOf(name)).Replace("\r\n", "\n") : null;

        /// <summary>Compares content with the golden file, writing it (LF) if it doesn't exist yet.</summary>
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
