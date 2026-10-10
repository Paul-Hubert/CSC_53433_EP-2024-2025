using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The result of one brain or mutator integrity check (32 §2): pass or fail, facts (models, digests or revisions,
    /// prompt ids, date), rows, written as JSON and Markdown under Logs/EvoSim/Reports so results compare over time.
    /// </summary>
    public sealed class IntegrityReport
    {
        public const string Folder = "Logs/EvoSim/Reports";

        public string Id { get; }
        public string Title { get; }
        /// <summary>Whether a failure blocks (B-01) or is tracked as a trend (gates).</summary>
        public bool Blocking { get; }
        public bool Passed { get; private set; } = true;
        public List<string> Problems { get; } = new List<string>();
        public SortedDictionary<string, object> Facts { get; } = new SortedDictionary<string, object>(StringComparer.Ordinal);
        public List<SortedDictionary<string, object>> Rows { get; } = new List<SortedDictionary<string, object>>();
        public string JsonPath { get; private set; }
        public string MarkdownPath { get; private set; }

        public IntegrityReport(string id, string title, bool blocking)
        {
            Id = id;
            Title = title;
            Blocking = blocking;
            Facts["date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            Facts["unity"] = Application.unityVersion;
        }

        public void Fail(string problem)
        {
            Passed = false;
            Problems.Add(problem);
        }

        public SortedDictionary<string, object> Row()
        {
            var r = new SortedDictionary<string, object>(StringComparer.Ordinal);
            Rows.Add(r);
            return r;
        }

        /// <summary>One line for logs and the CLI.</summary>
        public string Line => $"{Id} {Title}: {(Passed ? "PASS" : "FAIL")}{(Problems.Count > 0 ? " — " + Problems[0] : "")} ({Rows.Count} rows) → {MarkdownPath}";

        /// <summary>Writes the JSON and Markdown reports.</summary>
        public void Write()
        {
            string root = Path.Combine(Path.GetDirectoryName(Application.dataPath), Folder);
            Directory.CreateDirectory(root);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            JsonPath = Path.Combine(root, $"{Id}-{stamp}.json");
            MarkdownPath = Path.Combine(root, $"{Id}-{stamp}.md");
            var doc = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["id"] = Id, ["title"] = Title, ["passed"] = Passed, ["blocking"] = Blocking,
                ["problems"] = Problems, ["facts"] = Facts, ["rows"] = Rows,
            };
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(doc, Formatting.Indented) + "\n", new UTF8Encoding(false));

            var md = new StringBuilder();
            md.Append("# ").Append(Id).Append(' ').Append(Title).Append(" — ").Append(Passed ? "PASS" : "FAIL").Append("\n\n");
            foreach (var kv in Facts) md.Append("- **").Append(kv.Key).Append("**: ").Append(Text(kv.Value)).Append('\n');
            if (Problems.Count > 0)
            {
                md.Append("\n## Problems\n\n");
                foreach (var p in Problems) md.Append("- ").Append(p).Append('\n');
            }
            if (Rows.Count > 0)
            {
                var columns = new List<string>(Rows[0].Keys);
                md.Append("\n| ").Append(string.Join(" | ", columns)).Append(" |\n|").Append(string.Concat(columns.ConvertAll(c => "---|"))).Append('\n');
                foreach (var r in Rows)
                {
                    md.Append('|');
                    foreach (var c in columns) md.Append(' ').Append(Text(r.TryGetValue(c, out var v) ? v : "").Replace("|", "\\|").Replace("\n", " ")).Append(" |");
                    md.Append('\n');
                }
            }
            File.WriteAllText(MarkdownPath, md.ToString(), new UTF8Encoding(false));
        }

        static string Text(object v) => v is string s ? s : JsonConvert.SerializeObject(v);
    }
}
