using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Writes a run into its own folder (13): events.jsonl, stats.csv, alleles.jsonl, final_population.json,
    /// summary.json, run_info.json. Events and stats are flushed every 500 ticks, alleles every 5 000 (RAND-22), and
    /// every file is written when the run stops, whatever the reason (RAND-20). The compatibility mode writes a
    /// two-species world exactly as the prototype does (OUT-05).
    /// </summary>
    public class RunRecorder : WorldService, IEventSink
    {
        [SerializeField, Tooltip("Folder that receives one sub-folder per run, relative to the project (gitignored Logs by default).")]
        string outputRoot = "Logs/EvoSim/runs";
        [SerializeField, Tooltip("Run folder name; empty = <world>-s<seed>-<date>. Placeholders: {world}, {seed}.")]
        string runName = "";
        [SerializeField, Min(1), Tooltip("A statistics row every this many ticks (reference 100).")]
        int statsEvery = 100;
        [SerializeField, Min(1), Tooltip("Flush events and stats every this many ticks (reference 500).")]
        int flushEvery = 500;
        [SerializeField, Min(1), Tooltip("Rewrite alleles.jsonl every this many ticks (reference 5 000).")]
        int allelesEvery = 5000;
        [SerializeField, Tooltip("Write two-species worlds as the prototype does (OUT-05), for its analysis tools.")]
        bool compatibility;
        [SerializeField, Tooltip("Write files (off: keep the hash and statistics only, e.g. in tests).")]
        bool writeFiles = true;

        StreamWriter events, stats;
        readonly List<string> statColumns = new List<string>();
        readonly List<Dictionary<string, string>> statRows = new List<Dictionary<string, string>>();   // to rewrite the file when a species is added
        readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        bool started, finished;

        public string RunFolder { get; private set; }
        public bool Compatibility { get => compatibility; set => compatibility = value; }
        public bool WriteFiles { get => writeFiles; set => writeFiles = value; }
        public int StatsEvery => statsEvery;
        public string OutputRoot { get => outputRoot; set => outputRoot = value; }
        public string RunName { get => runName; set => runName = value; }
        /// <summary>Extra entries for run_info.json (the scenario, its overrides), set before the run.</summary>
        public SortedDictionary<string, object> ExtraInfo { get; } = new SortedDictionary<string, object>(StringComparer.Ordinal);

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public override void Initialize()
        {
            Close();
            started = finished = false;
            RunFolder = null;
            statRows.Clear();
        }

        /// <summary>After everything is ready, before founders: open the folder and the files.</summary>
        public override void Begin()
        {
            World.Events.AddSink(this);
            clock.Restart();
            started = true;
            if (!writeFiles) return;
            string name = string.IsNullOrEmpty(runName)
                ? $"{World.name}-s{World.Seed}-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}"
                : runName.Replace("{world}", World.name).Replace("{seed}", World.Seed.ToString(CultureInfo.InvariantCulture));
            name = Sanitize(name);
            string root = Path.GetFullPath(Path.Combine(ProjectRoot, outputRoot));
            string folder = Path.Combine(root, name);
            for (int n = 2; Directory.Exists(folder); n++) folder = Path.Combine(root, name + "-" + n);      // OUT-01: always its own folder
            Directory.CreateDirectory(folder);
            RunFolder = folder;                                             // OUT-01
            events = new StreamWriter(Path.Combine(folder, "events.jsonl"), false, new UTF8Encoding(false)) { NewLine = "\n" };
            BuildColumns();
            OpenStats();                                                    // the header even if the run ends before the first row
            WriteRunInfo();
        }

        // ---- Events (OUT-02, OUT-03) ----

        public void OnEvent(SimEvent e, string line)
        {
            events?.WriteLine(compatibility ? CompatLine(e) : line);
        }

        string CompatLine(SimEvent e)
        {
            var fields = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in e.Fields) fields[kv.Key] = kv.Value;
            bool first = IsFirstSpecies(e.Species);
            if (first) fields.Remove("species");
            if (e.Kind == "death" && "killed".Equals(fields["cause"])) fields["cause"] = PrototypeFormat.KillCause;
            if (fields.TryGetValue("genome", out var g) && g is List<string> ids) fields["genome"] = ids.ConvertAll(CompatId);
            if (fields.TryGetValue("mutations", out var m) && m is List<object> list)
            {
                var copy = new List<object>();
                foreach (var item in list)
                {
                    if (!(item is SortedDictionary<string, object> d)) { copy.Add(item); continue; }
                    var c = new SortedDictionary<string, object>(d, StringComparer.Ordinal);
                    foreach (var key in new[] { "locus", "parent", "child" })
                        if (c.TryGetValue(key, out var v) && v is string s) c[key] = CompatId(s);
                    copy.Add(c);
                }
                fields["mutations"] = copy;
            }
            return CanonicalJson.Write(fields);
        }

        bool IsFirstSpecies(string speciesId) => World.AllSpecies.Count > 0 && World.AllSpecies[0].Id == speciesId;

        /// <summary>In the compatibility mode the first species' loci have no prefix: "prey.eat:3" → "eat:3" (OUT-05).</summary>
        public string CompatId(string id)
        {
            if (!compatibility || World.AllSpecies.Count == 0 || id == null) return id;
            string prefix = World.AllSpecies[0].Id + ".";
            return id.StartsWith(prefix, StringComparison.Ordinal) ? id.Substring(prefix.Length) : id;
        }

        // ---- Statistics rows (13 §4) ----

        /// <summary>One statistics row; the record phase calls it every statsEvery ticks.</summary>
        public void WriteStats(int tick)
        {
            if (stats == null) return;
            var row = StatRow(tick);
            var columns = Columns();
            if (!SameAs(columns, statColumns))                              // a species added (SPEC-30), a new death cause or module column
            {
                statColumns.Clear();
                statColumns.AddRange(columns);
                OpenStats();
            }
            statRows.Add(row);
            stats.WriteLine(StatLine(row));
        }

        /// <summary>(Re)writes stats.csv: the header, then the rows so far (empty cells for species that didn't exist yet).</summary>
        void OpenStats()
        {
            if (!writeFiles || RunFolder == null) return;
            stats?.Dispose();
            stats = new StreamWriter(Path.Combine(RunFolder, "stats.csv"), false, new UTF8Encoding(false)) { NewLine = "\n" };
            stats.WriteLine(string.Join(",", statColumns));
            foreach (var row in statRows) stats.WriteLine(StatLine(row));
        }

        string StatLine(Dictionary<string, string> row)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < statColumns.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(row.TryGetValue(statColumns[i], out var v) ? v : "");
            }
            return sb.ToString();
        }

        string Prefix(Species s, int index) => compatibility ? (index == 0 ? "" : index == 1 ? PrototypeFormat.SecondSpeciesPrefix : s.Id + "_") : s.Id + "_";

        void BuildColumns()
        {
            statColumns.Clear();
            statColumns.AddRange(Columns());
        }

        /// <summary>The columns now: t, then each species' block (its modules' columns last), then the World's modules' columns.</summary>
        List<string> Columns()
        {
            var columns = new List<string> { "t" };
            void Add(string c) { if (!columns.Contains(c)) columns.Add(c); }
            for (int i = 0; i < World.AllSpecies.Count; i++)
            {
                var s = World.AllSpecies[i];
                string p = Prefix(s, i);
                foreach (var c in new[] { "pop", "mean_energy", "mean_stamina", "exhausted", "mean_gen", "max_gen", "births", "immigrants",
                                          "deaths_starve", "deaths_pred", "deaths_age", "deaths_migrated" })
                    Add(p + c);
                foreach (var cause in OtherCauses(s)) Add(p + "deaths_" + cause);
                foreach (var c in new[] { "decisions", "backend_queries", "invalid", "portions" }) Add(p + c);
                foreach (var a in s.Actions) Add(p + "act_" + a.Name);
                foreach (var m in s.ModulesOf<IStatsColumns>())
                    foreach (var c in m.StatColumns) Add(p + c);
                if (i == 0) Add("alleles");
            }
            foreach (var m in WorldColumns())
                foreach (var c in m.StatColumns) Add(c);
            return columns;
        }

        static bool SameAs(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        IEnumerable<IStatsColumns> WorldColumns()
        {
            foreach (var ph in World.Phases) if (ph is IStatsColumns c) yield return c;
            foreach (var svc in World.Services) if (svc is IStatsColumns c) yield return c;
        }

        /// <summary>Causes beyond the four fixed columns: the death rules' (in rule order), then any other cause recorded so far (sorted).</summary>
        IEnumerable<string> OtherCauses(Species s)
        {
            var seen = new List<string> { "starvation", "killed", "old_age", "migrated" };
            foreach (var rule in s.ModulesOf<DeathRule>())
                foreach (var cause in rule.Causes)
                    if (!seen.Contains(cause)) { seen.Add(cause); yield return cause; }
            foreach (var kv in s.Counters.Deaths)                           // World.RecordDeath from a student's own phase or module
                if (!seen.Contains(kv.Key)) { seen.Add(kv.Key); yield return kv.Key; }
        }

        Dictionary<string, string> StatRow(int tick)
        {
            var row = new Dictionary<string, string> { ["t"] = tick.ToString(CultureInfo.InvariantCulture), ["alleles"] = Num(World.Alleles.Count) };
            for (int i = 0; i < World.AllSpecies.Count; i++)
            {
                var s = World.AllSpecies[i];
                var c = s.Counters;
                string p = Prefix(s, i);
                var energy = s.Declarations.FindStat("energy");
                var stamina = s.Declarations.FindStat("stamina");
                double e = 0, st = 0, gen = 0;
                int n = 0, maxGen = 0;
                foreach (var a in s.Animals)
                {
                    if (a.IsGone) continue;
                    n++;
                    if (energy.IsValid) e += a[energy];
                    if (stamina.IsValid) st += a[stamina];
                    gen += a.Generation;
                    maxGen = Math.Max(maxGen, a.Generation);
                }
                row[p + "pop"] = Num(n);
                row[p + "mean_energy"] = Num(n > 0 ? e / n : 0, 2);
                row[p + "mean_stamina"] = Num(n > 0 ? st / n : 0, 2);
                row[p + "exhausted"] = Num(c.Exhausted);
                row[p + "mean_gen"] = Num(n > 0 ? gen / n : 0, 2);
                row[p + "max_gen"] = Num(maxGen);
                row[p + "births"] = Num(c.Births);
                row[p + "immigrants"] = Num(c.Immigrants);
                row[p + "deaths_starve"] = Num(c.DeathsBy("starvation"));
                row[p + "deaths_pred"] = Num(c.DeathsBy("killed"));
                row[p + "deaths_age"] = Num(c.DeathsBy("old_age"));
                row[p + "deaths_migrated"] = Num(c.DeathsBy("migrated"));
                foreach (var cause in OtherCauses(s)) row[p + "deaths_" + cause] = Num(c.DeathsBy(cause));
                row[p + "decisions"] = Num(c.Decisions);
                row[p + "backend_queries"] = Num(c.BrainQueries);
                row[p + "invalid"] = Num(c.Invalid);
                row[p + "portions"] = Num(c.Portions);
                for (int k = 0; k < s.Actions.Count; k++) row[p + "act_" + s.Actions[k].Name] = Num(k < c.ActionCounts.Length ? c.ActionCounts[k] : 0);
                foreach (var m in s.ModulesOf<IStatsColumns>())
                    foreach (var col in m.StatColumns)
                        if (!row.ContainsKey(p + col)) row[p + col] = Value(m.StatValue(col));
            }
            foreach (var m in WorldColumns())
                foreach (var col in m.StatColumns)
                    if (!row.ContainsKey(col)) row[col] = Value(m.StatValue(col));
            return row;
        }

        static string Value(double v) => Math.Round(v, 4).ToString("0.####", CultureInfo.InvariantCulture);

        static string Num(double v, int decimals = 0) =>
            decimals == 0 ? Math.Round(v).ToString(CultureInfo.InvariantCulture) : Math.Round(v, decimals).ToString("0.##", CultureInfo.InvariantCulture);

        // ---- Flushes (RAND-22) ----

        /// <summary>Called by the record phase every tick: flushes events and stats, and the allele list, on their periods.</summary>
        public void AfterTick(int tick)
        {
            if (tick > 0 && tick % flushEvery == 0) { events?.Flush(); stats?.Flush(); }
            if (tick > 0 && tick % allelesEvery == 0) WriteAlleles();
        }

        // ---- Files at the end (13 §2, RAND-20) ----

        public override void OnRunStopped(string reason)
        {
            if (!started || finished) return;
            finished = true;
            clock.Stop();
            if (writeFiles && RunFolder != null)
            {
                WriteAlleles();
                WriteJson("final_population.json", FinalPopulation());
                WriteJson("summary.json", Summary(reason));
                WriteRunInfo();
            }
            Close();
            World.Events.RemoveSink(this);
        }

        /// <summary>Finishes the files of a world that is being destroyed without a stop (tests, editor): RAND-20, OUT-03.</summary>
        void OnDestroy()
        {
            if (started && !finished && World != null) OnRunStopped(World.StopReason ?? "world destroyed");
            Close();
        }

        void Close()
        {
            events?.Dispose();
            stats?.Dispose();
            events = stats = null;
        }

        void WriteAlleles()
        {
            if (!writeFiles || RunFolder == null) return;
            var sb = new StringBuilder();
            foreach (var a in World.Alleles.All)
            {
                var d = new SortedDictionary<string, object>(StringComparer.Ordinal);
                d["id"] = CompatId(a.Id);
                d["locus"] = CompatId(a.Locus);
                if (a.Kind == AlleleKind.Text) d["text"] = a.Text; else d["value"] = a.Number;
                d["origin"] = a.Origin;
                d["parent_id"] = CompatId(a.ParentId);
                d["operator"] = a.Operator;
                d["model"] = a.Model;
                d["seed"] = a.Seed;
                sb.Append(CanonicalJson.Write(d)).Append('\n');
            }
            File.WriteAllText(Path.Combine(RunFolder, "alleles.jsonl"), sb.ToString(), new UTF8Encoding(false));
        }

        List<object> FinalPopulation()
        {
            var list = new List<object>();
            for (int i = 0; i < World.AllSpecies.Count; i++)
            {
                var s = World.AllSpecies[i];
                foreach (var a in s.Animals)
                {
                    if (a.IsGone) continue;
                    var d = new SortedDictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["id"] = a.Id, ["gen"] = a.Generation,
                        ["genome"] = World.GenomeIds(a.Genome).ConvertAll(CompatId),
                    };
                    if (!(compatibility && i == 0)) d["species"] = s.Id;
                    list.Add(d);
                }
            }
            return list;
        }

        SortedDictionary<string, object> SpeciesSummary(Species s)
        {
            var c = s.Counters;
            int pop = 0;
            foreach (var a in s.Animals) if (!a.IsGone) pop++;
            int decided = Math.Max(1, c.Decisions);
            var deaths = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in c.Deaths) deaths[compatibility && kv.Key == "killed" ? PrototypeFormat.KillCause : kv.Key] = kv.Value;
            var shares = new SortedDictionary<string, object>(StringComparer.Ordinal);
            for (int k = 0; k < s.Actions.Count && k < c.ActionCounts.Length; k++) shares[s.Actions[k].Name] = Math.Round((double)c.ActionCounts[k] / decided, 3);
            var d = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["pop_final"] = pop, ["births"] = c.Births, ["immigrants"] = c.Immigrants, ["hatched"] = c.Hatched,
                ["deaths"] = deaths, ["mean_lifespan"] = c.DeathsTotal > 0 ? Math.Round((double)c.LifespanSum / c.DeathsTotal, 1) : 0.0,
                ["max_gen"] = c.MaxGeneration, ["decisions"] = c.Decisions, ["backend_queries"] = c.BrainQueries,
                ["memo_hit_rate"] = Math.Round((double)c.MemoHits / decided, 3), ["backend_s"] = Math.Round(c.BrainMilliseconds / 1000.0, 2),
                ["invalid_rate"] = Math.Round((double)c.Invalid / decided, 3),
                ["exhausted_share"] = c.AnimalTicks > 0 ? Math.Round((double)c.Exhausted / c.AnimalTicks, 3) : 0.0,
                ["action_share"] = shares, ["founders"] = c.Founders, ["eggs_lost"] = c.EggsLost, ["failures"] = c.Failures,
            };
            if (c.Kills > 0 || s.Module<Diet>()?.Strikes == true) { d["kills"] = c.Kills; d["portions"] = c.Portions; }
            else if (c.Portions > 0) d["portions"] = c.Portions;
            return d;
        }

        SortedDictionary<string, object> Summary(string reason)
        {
            var mutations = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["tried"] = World.Mutations.Attempts, ["ok"] = World.Mutations.Successes, ["calls"] = World.Mutations.ModelCalls,
                ["failed"] = World.Mutations.Failures,
            };
            var rejected = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in World.Mutations.Rejections) rejected[kv.Key] = kv.Value;
            mutations["rejected"] = rejected;
            var brains = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var b in World.ServicesOf<Brain>()) brains[b.Id] = b.ModelIdentity;
            int failures = 0;
            foreach (var s in World.AllSpecies) failures += s.Counters.Failures;
            var d = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["ticks"] = World.Tick, ["seed"] = World.Seed, ["world_seed"] = World.WorldSeed,
                ["backend"] = World.DefaultBrain != null ? World.DefaultBrain.Id : "", ["brains"] = brains,
                ["mutations"] = mutations, ["alleles"] = World.Alleles.Count,
                ["events_sha"] = World.Events.ShortHash, ["events_hash"] = World.Events.Hash, ["events"] = World.Events.Count,
                ["stopped"] = reason, ["minutes"] = Math.Round(clock.Elapsed.TotalMinutes, 1),
                ["llm_calls"] = World.Decisions.ModelCalls, ["cache_hits"] = World.Decisions.CacheHits,
                ["failures"] = failures, ["mutation_calls"] = World.Mutations.ModelCalls,
            };
            if (compatibility && World.AllSpecies.Count > 0)
            {
                foreach (var kv in SpeciesSummary(World.AllSpecies[0]))
                    if (!d.ContainsKey(kv.Key)) d[kv.Key] = kv.Value;               // the run's own keys (failures) win
                for (int i = 1; i < World.AllSpecies.Count; i++)
                    d[PrototypeFormat.SummaryKey(World.AllSpecies[i].Id)] = SpeciesSummary(World.AllSpecies[i]);
            }
            else
            {
                var species = new SortedDictionary<string, object>(StringComparer.Ordinal);
                foreach (var s in World.AllSpecies) species[s.Id] = SpeciesSummary(s);
                d["species"] = species;
            }
            return d;
        }

        void WriteRunInfo()
        {
            if (!writeFiles || RunFolder == null) return;
            var info = RunInfo.Collect(World);
            foreach (var kv in ExtraInfo) info[kv.Key] = kv.Value;
            WriteJson("run_info.json", info);
        }

        void WriteJson(string file, object value) =>
            File.WriteAllText(Path.Combine(RunFolder, file), CanonicalJson.Write(value) + "\n", new UTF8Encoding(false));

        static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            return sb.ToString();
        }

        public void Configure(string root, string name, bool compat = false, bool files = true, int statsInterval = 100)
        {
            outputRoot = root;
            runName = name;
            compatibility = compat;
            writeFiles = files;
            statsEvery = Mathf.Max(1, statsInterval);
        }
    }
}
