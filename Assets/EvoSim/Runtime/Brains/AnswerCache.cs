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
    /// <remarks>
    /// <para>Place one under the World's services. A brain uses it when its <see cref="Brain.UsesCache"/> is on: the
    /// ask-brains phase looks each query up with <see cref="TryGetRow"/> under the brain's <see cref="Brain.Id"/>, and the
    /// choose-actions phase stores good rows with <see cref="StoreRow"/>, never a failure (DEC-34). The mutator stores its
    /// texts under "mutator-" and its model's name.</para>
    /// <para>The memo works within one run; this cache works across runs. Delete the folder to forget every answer.</para>
    /// </remarks>
    public class AnswerCache : WorldService
    {
        [SerializeField, Tooltip("Folder of the cache files, relative to the project (gitignored Library by default).")]
        string folder = "Library/EvoSim/AnswerCache";
        [SerializeField, Tooltip("Answer from the cache when possible.")]
        bool read = true;
        [SerializeField, Tooltip("Store new answers.")]
        bool write = true;

        // The answers of each store by key, read from its file on first use.
        readonly Dictionary<string, Dictionary<string, string>> files = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>Lookups answered from the cache since the run started.</summary>
        public int Hits { get; private set; }

        /// <summary>Answers written to disk since the run started.</summary>
        public int Stores { get; private set; }

        /// <summary>The full path of the folder that holds the cache files, one <c>.jsonl</c> file per store.</summary>
        public string Folder => Path.GetFullPath(Path.Combine(ProjectRoot, folder));

        /// <summary>True when lookups may answer from the cache; false makes every lookup miss.</summary>
        public bool Reads { get => read; set => read = value; }

        /// <summary>True when new answers are appended to the files; false keeps them in memory for this run only.</summary>
        public bool Writes { get => write; set => write = value; }

        /// <summary>The Unity project's folder (the parent of Assets).</summary>
        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>Forgets the loaded files, so they are read again at the first lookup, and resets the counters.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            files.Clear();
            Hits = Stores = 0;
        }

        /// <summary>Sets the folder from code (tests), as the Inspector field does, and forgets the loaded files.</summary>
        /// <param name="path">The folder, relative to the project or absolute.</param>
        public void SetFolder(string path) { folder = path; files.Clear(); }

        /// <summary>
        /// The cache key of a decision query for a brain (DEC-33): brain, model, prompt id, mode, style, genome, situation.
        /// </summary>
        /// <param name="brain">The brain that answers.</param>
        /// <param name="q">The query.</param>
        /// <param name="attachmentKey">The attachments' key (SENSE-41), or null for none.</param>
        /// <returns>A 32-digit hexadecimal SHA-256 prefix of those parts.</returns>
        public static string KeyFor(Brain brain, DecisionQuery q, string attachmentKey = null) =>
            Hashing.Sha256Hex(string.Join("\n", brain.Id, brain.ModelIdentity, q.Species.Prompt.Id, brain.Mode, q.Style.ToString(),
                                          q.GenomeKey, q.Situation, attachmentKey ?? ""), 32);

        /// <summary>Looks up a cached row for this brain and key, and counts a hit.</summary>
        /// <param name="store">The store's name: the brain's <see cref="Brain.Id"/>.</param>
        /// <param name="key">A key from <see cref="KeyFor"/>; null never matches.</param>
        /// <param name="row">The cached row, a new array; null when not found.</param>
        /// <returns>True when found (and <see cref="Reads"/> is on).</returns>
        public bool TryGetRow(string store, string key, out float[] row)
        {
            row = null;
            if (!TryGetText(store, key, out var text)) return false;
            var arr = JArray.Parse(text);
            row = new float[arr.Count];
            for (int i = 0; i < row.Length; i++) row[i] = float.Parse((string)arr[i], CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>Stores a row for this brain and key, each number written so it reads back bit for bit.</summary>
        /// <param name="store">The store's name: the brain's <see cref="Brain.Id"/>.</param>
        /// <param name="key">A key from <see cref="KeyFor"/>; null stores nothing.</param>
        /// <param name="row">A good row, never a failure (DEC-34).</param>
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

        /// <summary>Looks up a cached text answer (the mutator's) for this store and key, and counts a hit.</summary>
        /// <param name="store">The store's name, such as "mutator-" and the model's name.</param>
        /// <param name="key">The key; null never matches.</param>
        /// <param name="value">The cached text, or null when not found.</param>
        /// <returns>True when found (and <see cref="Reads"/> is on).</returns>
        public bool TryGetText(string store, string key, out string value)
        {
            value = null;
            if (!read || key == null) return false;
            if (!File(store).TryGetValue(key, out value)) return false;
            Hits++;
            return true;
        }

        /// <summary>Appends an answer to the store's file (one JSON object per line, keys sorted).</summary>
        /// <param name="store">The store's name: a brain's id, or "mutator-" and the model's name.</param>
        /// <param name="key">The key; null stores nothing.</param>
        /// <param name="value">The answer; null stores nothing.</param>
        /// <remarks>The answer is also kept in memory, even when <see cref="Writes"/> is off. A key stored twice keeps the
        /// last value.</remarks>
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

        /// <summary>The answers of a store by key, read from its file at the first use.</summary>
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

        /// <summary>The file of a store: its sanitized name with <c>.jsonl</c>, in <see cref="Folder"/>.</summary>
        string PathOf(string store) => Path.Combine(Folder, Sanitize(store) + ".jsonl");

        /// <summary>A store name made safe for a file name: letters, digits, '-', '_' and '.' kept, anything else '_'.</summary>
        static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            return sb.ToString();
        }

        /// <summary>V-62: the cache folder must be writable (checked only when <see cref="Writes"/> is on).</summary>
        /// <inheritdoc/>
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
