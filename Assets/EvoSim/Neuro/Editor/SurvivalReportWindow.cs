using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvoSim.Editor;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Editor
{
    /// <summary>
    /// <c>Window ▸ EvoSim ▸ Survival Report</c> (plan M16 §6.2): opens one or more run folders of one scenario and arm,
    /// shows the headline ratios, the survival curves, the per-window hazards and the kill matrix, and writes the markdown
    /// report and its CSV to <c>Logs/EvoSim/Reports/</c>.
    /// </summary>
    /// <remarks>
    /// Add the runs of each seed (their folders hold <c>events.jsonl</c>, <c>summary.json</c> and <c>survival.json</c>).
    /// The numbers are those of <see cref="SurvivalReport"/>; curves are drawn with the editor's own lines, one colour per
    /// species, the Greenwood band in a lighter shade.
    /// </remarks>
    public class SurvivalReportWindow : EditorWindow
    {
        /// <summary>Where reports are written, relative to the project.</summary>
        public const string ReportsFolder = "Logs/EvoSim/Reports";

        readonly List<string> folders = new List<string>();
        SurvivalReport report;
        string error;
        Vector2 scroll;
        int seedShown;
        string notes = "";

        /// <summary>Opens the window.</summary>
        [MenuItem("Window/EvoSim/Survival Report")]
        public static void Open() => GetWindow<SurvivalReportWindow>("Survival").Show();

        /// <summary>Opens the window on these run folders (scripts, the scenario runner).</summary>
        /// <param name="runFolders">Run folders of one scenario and arm.</param>
        /// <returns>The window.</returns>
        public static SurvivalReportWindow OpenOn(IEnumerable<string> runFolders)
        {
            var w = GetWindow<SurvivalReportWindow>("Survival");
            w.folders.Clear();
            w.folders.AddRange(runFolders);
            w.Analyse();
            w.Show();
            return w;
        }

        /// <summary>
        /// Writes the report of these run folders without a window (batch): <c>&lt;scenario&gt;-&lt;variant&gt;-&lt;date&gt;.md</c>
        /// and its <c>.csv</c> in <see cref="ReportsFolder"/>.
        /// </summary>
        /// <param name="runFolders">Run folders of one scenario and arm.</param>
        /// <param name="differences">Lines for "Settings that differ from the other arm".</param>
        /// <returns>The markdown file's path.</returns>
        public static string Write(IEnumerable<string> runFolders, IEnumerable<string> differences = null)
        {
            var r = new SurvivalReport(runFolders.Select(SurvivalAnalysis.FromFolder));
            if (differences != null) r.Notes.AddRange(differences);
            return Save(r);
        }

        static string Save(SurvivalReport r)
        {
            var first = r.Runs[0];
            string name = $"{Safe(first.Scenario, "run")}-{Safe(first.Variant, "default")}-{System.DateTime.Now:yyyy-MM-dd}";
            Directory.CreateDirectory(ReportsFolder);
            string md = Path.Combine(ReportsFolder, name + ".md");
            File.WriteAllText(md, r.Markdown($"{first.Scenario} {first.Variant}".Trim()));
            File.WriteAllText(Path.Combine(ReportsFolder, name + ".csv"), r.WindowsCsv());
            return md;
        }

        static string Safe(string s, string fallback) =>
            string.IsNullOrEmpty(s) ? fallback : new string(s.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());

        void Analyse()
        {
            error = null;
            report = null;
            try
            {
                if (folders.Count > 0) report = new SurvivalReport(folders.Select(SurvivalAnalysis.FromFolder));
            }
            catch (System.Exception e) { error = e.Message; }
            seedShown = 0;
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add run folder…"))
                {
                    string f = EditorUtility.OpenFolderPanel("Run folder", "Logs/EvoSim/runs", "");
                    if (!string.IsNullOrEmpty(f) && !folders.Contains(f)) { folders.Add(f); Analyse(); }
                }
                if (GUILayout.Button("Clear")) { folders.Clear(); Analyse(); }
                using (new EditorGUI.DisabledScope(report == null))
                    if (GUILayout.Button("Write report"))
                    {
                        report.Notes.Clear();
                        report.Notes.AddRange(notes.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0));
                        string path = Save(report);
                        EditorUtility.RevealInFinder(path);
                    }
            }
            foreach (var f in folders) EditorGUILayout.LabelField(Path.GetFileName(f), EditorStyles.miniLabel);
            notes = EditorGUILayout.TextField("Differences from the other arm", notes);
            if (error != null) { EditorGUILayout.HelpBox(error, MessageType.Error); return; }
            if (report == null) { EditorGUILayout.HelpBox("Add the run folders of one scenario and arm (one per seed).", MessageType.Info); return; }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Headline: death hazard ratio, neural / JEV (late windows)", EditorStyles.boldLabel);
            foreach (var p in report.Pairs())
            {
                var hr = report.Headline(p);
                for (int i = 0; i < hr.Count; i++)
                    EditorGUILayout.LabelField($"{p.neural} / {p.jev}  {(i < report.Runs.Count ? "seed " + report.Runs[i].Seed : "pooled")}",
                        $"{hr[i].Ratio:0.###}  ({hr[i].Lower:0.###}–{hr[i].Upper:0.###})");
                EditorGUILayout.LabelField(" ", SurvivalReport.Verdict(hr), EditorStyles.boldLabel);
            }

            EditorGUILayout.Space();
            var seeds = report.Runs.Select(r => "seed " + r.Seed).ToArray();
            seedShown = Mathf.Clamp(EditorGUILayout.Popup("Curves of", seedShown, seeds), 0, report.Runs.Count - 1);
            var run = report.Runs[seedShown];
            EditorGUILayout.LabelField("Survival over age (Kaplan–Meier)", EditorStyles.boldLabel);
            DrawCurves(run);
            EditorGUILayout.LabelField("All-cause hazard per window (per 1 000 animal-ticks)", EditorStyles.boldLabel);
            DrawWindows(run);

            EditorGUILayout.LabelField("Kill matrix (whole run)", EditorStyles.boldLabel);
            foreach (var kv in run.KillMatrix()) EditorGUILayout.LabelField($"{kv.Key.killer} → {kv.Key.victim}", kv.Value.ToString());
            EditorGUILayout.EndScrollView();
        }

        /// <summary>The species' Kaplan–Meier curves in one plot, age on x (to the longest life), S on y.</summary>
        void DrawCurves(SurvivalAnalysis run)
        {
            var rect = GUILayoutUtility.GetRect(200, 220, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            int maxAge = Mathf.Max(1, run.Lives.Count > 0 ? run.Lives.Max(l => l.Age) : 1);
            for (int i = 0; i < run.Species.Count; i++)
            {
                var c = EditorWorlds.ActionColor(i);
                var curve = run.Curve(run.Species[i]);
                var line = new List<Vector3> { Point(rect, 0, 1, maxAge) };
                var lo = new List<Vector3> { Point(rect, 0, 1, maxAge) };
                var hi = new List<Vector3> { Point(rect, 0, 1, maxAge) };
                double s = 1, l = 1, h = 1;
                foreach (var step in curve)
                {
                    line.Add(Point(rect, step.Time, s, maxAge)); line.Add(Point(rect, step.Time, step.Survival, maxAge));
                    lo.Add(Point(rect, step.Time, l, maxAge)); lo.Add(Point(rect, step.Time, step.Lower, maxAge));
                    hi.Add(Point(rect, step.Time, h, maxAge)); hi.Add(Point(rect, step.Time, step.Upper, maxAge));
                    s = step.Survival; l = step.Lower; h = step.Upper;
                }
                Handles.color = new Color(c.r, c.g, c.b, 0.35f);
                Handles.DrawAAPolyLine(1f, lo.ToArray());
                Handles.DrawAAPolyLine(1f, hi.ToArray());
                Handles.color = c;
                Handles.DrawAAPolyLine(2f, line.ToArray());
                GUI.Label(new Rect(rect.xMax - 160, rect.y + 4 + 16 * i, 156, 16), run.Species[i], new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = c } });
            }
            GUI.Label(new Rect(rect.x + 4, rect.yMax - 16, 200, 16), $"age 0 … {maxAge} ticks", EditorStyles.miniLabel);
        }

        /// <summary>Each species' all-cause hazard per window, as a line.</summary>
        void DrawWindows(SurvivalAnalysis run)
        {
            var rect = GUILayoutUtility.GetRect(200, 160, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            var all = run.Species.Select(s => run.Windows(s, report.WindowTicks)).ToList();
            double top = 1e-9;
            foreach (var ws in all) foreach (var w in ws) top = System.Math.Max(top, w.Hazard(null));
            for (int i = 0; i < all.Count; i++)
            {
                var pts = all[i].Select((w, k) => new Vector3(
                    rect.x + rect.width * (k + 0.5f) / Mathf.Max(1, all[i].Count),
                    rect.yMax - rect.height * (float)(w.Hazard(null) / top), 0)).ToArray();
                Handles.color = EditorWorlds.ActionColor(i);
                if (pts.Length > 1) Handles.DrawAAPolyLine(2f, pts);
            }
            GUI.Label(new Rect(rect.x + 4, rect.y + 2, 260, 16), $"top {top:0.###} per 1 000 animal-ticks", EditorStyles.miniLabel);
        }

        static Vector3 Point(Rect r, int age, double s, int maxAge) =>
            new Vector3(r.x + r.width * age / maxAge, r.yMax - r.height * (float)s, 0);
    }
}
