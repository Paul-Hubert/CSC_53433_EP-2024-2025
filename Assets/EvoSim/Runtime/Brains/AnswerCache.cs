using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A persistent store of brain and mutator answers (DEC-33, MUT-14): append-only JSON lines, one file per brain,
    /// loaded at the first lookup. Keys hash everything that can change an answer, so a repeated run replays
    /// without model calls (RAND-21). Rows are stored with round-trip precision, so a replay is bit-identical.
    /// </summary>
    public class AnswerCache : WorldService
    {
        [SerializeField, Tooltip("Folder of the cache files, relative to the project (gitignored Library by default).")]
        string folder = "Library/EvoSim/AnswerCache";
        [SerializeField, Tooltip("Answer from the cache when possible.")]
        bool read = true;
        [SerializeField, Tooltip("Store new answers.")]
        bool write = true;

        readonly Dictionary<string, Dictionary<string, string>> files = new Dictionary<string, Dictionary<string, string>>();

        public int Hits { get; private set; }
        public int Stores { get; private set; }
        public string Folder => Path.GetFullPath(Path.Combine(ProjectRoot, folder));
        public bool Reads { get => read; set => read = value; }
        public bool Writes { get => write; set => write = value; }

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public override void Initialize()
        {
            files.Clear();
            Hits = Stores = 0;
        }

        public void SetFolder(string path) { folder = path; files.Clear(); }

        /// <summary>The cache key of a decision query for a brain (DEC-33): brain, model, prompt id, mode, style, genome, situation.</summary>
        public static string KeyFor(Brain brain, DecisionQuery q, string attachmentKey = null) =>
            Hashing.Sha256Hex(string.Join("\n", brain.Id, brain.ModelIdentity, q.Species.Prompt.Id, brain.Mode, q.Style.ToString(),
                                          q.GenomeKey, q.Situation, attachmentKey ?? ""), 32);

        /// <summary>A cached row for this brain and key.</summary>
        public bool TryGetRow(string store, string key, out float[] row)
        {
            row = null;
            if (!TryGetText(store, key, out var text)) return false;
            var arr = JArray.Parse(text);
            row = new float[arr.Count];
            for (int i = 0; i < row.Length; i++) row[i] = float.Parse((string)arr[i], CultureInfo.InvariantCulture);
            return true;
        }

        public void StoreRow(string store, string key, float[] row)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(row[i].ToString("R", CultureInfo.InvariantCulture)).Append('"');
            }
            StoreText(store, key, sb.Append(']').ToString());
        }

        /// <summary>A cached text answer (the mutator's) for this store and key.</summary>
        public bool TryGetText(string store, string key, out string value)
        {
            value = null;
            if (!read || key == null) return false;
            if (!File(store).TryGetValue(key, out value)) return false;
            Hits++;
            return true;
        }

        /// <summary>Appends an answer to the store's file (one JSON object per line, keys sorted).</summary>
        public void StoreText(string store, string key, string value)
        {
            if (key == null || value == null) return;
            var map = File(store);
            map[key] = value;
            if (!write) return;
            Directory.CreateDirectory(Folder);
            string line = CanonicalJson.Write(new SortedDictionary<string, object>(StringComparer.Ordinal) { { "key", key }, { "value", value } });
            System.IO.File.AppendAllText(PathOf(store), line + "\n", new UTF8Encoding(false));
            Stores++;
        }

        Dictionary<string, string> File(string store)
        {
            if (files.TryGetValue(store, out var map)) return map;
            map = new Dictionary<string, string>();
            string path = PathOf(store);
            if (System.IO.File.Exists(path))
                foreach (var line in System.IO.File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var o = JObject.Parse(line);
                        map[(string)o["key"]] = (string)o["value"];
                    }
                    catch (Exception) { /* a torn last line after a hard stop is skipped */ }
                }
            files.Add(store, map);
            return map;
        }

        string PathOf(string store) => Path.Combine(Folder, Sanitize(store) + ".jsonl");

        static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            return sb.ToString();
        }

        /// <summary>V-62: the cache folder must be writable.</summary>
        public override void Validate(ValidationReport report)
        {
            if (!write) return;
            try
            {
                Directory.CreateDirectory(Folder);
                string probe = Path.Combine(Folder, ".write-test");
                System.IO.File.WriteAllText(probe, "");
                System.IO.File.Delete(probe);
            }
            catch (Exception e)
            {
                report.Error("V-62", this, $"The answer cache folder {Folder} can't be written: {e.Message}");
            }
        }
    }
}
