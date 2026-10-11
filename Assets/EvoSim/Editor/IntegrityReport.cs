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
    /// <remarks>
    /// A check creates one, adds facts with <c>Facts["key"] = value</c>, one row per measured case with <see cref="Row"/>,
    /// calls <see cref="Fail"/> for each problem, and ends with <see cref="Write"/>. The files are named
    /// <c>&lt;id&gt;-&lt;yyyyMMdd-HHmmss&gt;.json</c> and <c>.md</c>; nothing is overwritten.
    /// </remarks>
    public sealed class IntegrityReport
    {
        /// <summary>The reports folder, relative to the project folder: <c>Logs/EvoSim/Reports</c>.</summary>
        public const string Folder = "Logs/EvoSim/Reports";

        /// <summary>The check's id, such as <c>B-02</c>; it starts the file names.</summary>
        public string Id { get; }
        /// <summary>The check's short title, such as <c>G1 directed</c>.</summary>
        public string Title { get; }
        /// <summary>Whether a failure blocks (B-01) or is tracked as a trend (gates).</summary>
        public bool Blocking { get; }
        /// <summary>True until <see cref="Fail"/> is called once.</summary>
        public bool Passed { get; private set; } = true;
        /// <summary>The problems found, in the order they were reported; empty when the check passed.</summary>
        public List<string> Problems { get; } = new List<string>();
        /// <summary>Named facts of the run (models, digests, prompt ids, measured values), sorted by name; the date and the Unity
        /// version are always there.</summary>
        public SortedDictionary<string, object> Facts { get; } = new SortedDictionary<string, object>(StringComparer.Ordinal);
        /// <summary>The table of measured cases: one dictionary per row, column name to value. The first row's columns head
        /// the Markdown table.</summary>
        public List<SortedDictionary<string, object>> Rows { get; } = new List<SortedDictionary<string, object>>();
        /// <summary>The full path of the JSON report, or null before <see cref="Write"/>.</summary>
        public string JsonPath { get; private set; }
        /// <summary>The full path of the Markdown report, or null before <see cref="Write"/>.</summary>
        public string MarkdownPath { get; private set; }

        /// <summary>Starts a passing report and records the date (UTC) and the Unity version as facts.</summary>
        /// <param name="id">The check's id, such as <c>B-02</c>.</param>
        /// <param name="title">The check's short title.</param>
        /// <param name="blocking">True when a failure must block (B-01); false for a gate tracked as a trend.</param>
        public IntegrityReport(string id, string title, bool blocking)
        {
            Id = id;
            Title = title;
            Blocking = blocking;
            Facts["date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            Facts["unity"] = Application.unityVersion;
        }

        /// <summary>Marks the check as failed and records why; it can be called several times.</summary>
        /// <param name="problem">One sentence on what failed, with the measured value and the threshold.</param>
        public void Fail(string problem)
        {
            Passed = false;
            Problems.Add(problem);
        }

        /// <summary>Adds an empty row to <see cref="Rows"/>.</summary>
        /// <returns>The new row, to fill with <c>row["column"] = value</c>.</returns>
        public SortedDictionary<string, object> Row()
        {
            var r = new SortedDictionary<string, object>(StringComparer.Ordinal);
            Rows.Add(r);
            return r;
        }

        /// <summary>One line for logs and the CLI.</summary>
        public string Line => $"{Id} {Title}: {(Passed ? "PASS" : "FAIL")}{(Problems.Count > 0 ? " — " + Problems[0] : "")} ({Rows.Count} rows) → {MarkdownPath}";

        /// <summary>
        /// Writes the JSON and Markdown reports under <see cref="Folder"/> (UTF-8, timestamped names) and sets
        /// <see cref="JsonPath"/> and <see cref="MarkdownPath"/>.
        /// </summary>
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

        /// <summary>A value as Markdown text: a string as it is, anything else as JSON.</summary>
        static string Text(object v) => v is string s ? s : JsonConvert.SerializeObject(v);
    }
}
