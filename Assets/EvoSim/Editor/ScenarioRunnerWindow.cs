using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The scenario runner (21 §4, lab G): the scenario assets, a variant, seeds and length; runs them headless (Freeze,
    /// the same code as EvoSim.Batch.RunScenario) and lists the results; "twice" checks the reproducibility fact.
    /// </summary>
    /// <remarks>
    /// <para>Open it with <c>Window ▸ EvoSim ▸ Scenario Runner</c>, in Edit mode. Pick a scenario and a variant, edit the
    /// seeds (comma-separated) and the length, and press <c>Run</c>: each seed runs through <see cref="Batch.Run"/> with
    /// a cancellable progress bar, and its one-line summary (ticks, event hash, model calls, run folder) is listed. The
    /// run's files go to <c>Logs/EvoSim/runs/&lt;scenario&gt;-&lt;variant&gt;-s&lt;seed&gt;-&lt;time&gt;/</c>.</para>
    /// <para>The scenario's scene must not be open in the editor (<see cref="Batch.WithScene"/> refuses it). The
    /// <c>Expect</c> line is shown for reading; only the "deterministic" fact is checked, by <c>Run each seed twice</c>.</para>
    /// </remarks>
    public class ScenarioRunnerWindow : EditorWindow
    {
        int scenarioIndex, variantIndex, ticks = -1;   // the chosen scenario and variant; ticks per run, -1 = the scenario's
        string seeds = "";                              // comma-separated seeds; filled from the scenario when it changes
        bool twice;                                     // runs each seed twice and compares the event hashes
        readonly List<string> results = new List<string>();   // one summary line per run, newest last
        Vector2 scroll;                                 // the results' scroll position

        /// <summary>Opens the window (<c>Window ▸ EvoSim ▸ Scenario Runner</c>).</summary>
        [MenuItem("Window/EvoSim/Scenario Runner")]
        public static void Open() => GetWindow<ScenarioRunnerWindow>("Scenarios").Show();

        /// <summary>Draws the scenario, variant, seeds and length fields, the Run and Clear buttons, and the results.</summary>
        void OnGUI()
        {
            var assets = new List<ScenarioAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScenarioAsset"))
            {
                var s = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) assets.Add(s);
            }
            if (assets.Count == 0) { EditorGUILayout.HelpBox("No scenario assets (Assets ▸ Create ▸ EvoSim ▸ Scenario).", MessageType.Info); return; }
            assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var names = assets.ConvertAll(a => string.IsNullOrEmpty(a.title) ? a.name : a.title).ToArray();
            int before = scenarioIndex;
            scenarioIndex = Mathf.Clamp(EditorGUILayout.Popup("Scenario", scenarioIndex, names), 0, assets.Count - 1);
            var scenario = assets[scenarioIndex];
            if (before != scenarioIndex || seeds.Length == 0) { seeds = string.Join(", ", scenario.seeds); variantIndex = 0; }
            var variants = scenario.variants.ConvertAll(v => $"{v.name} ({(string.IsNullOrEmpty(v.tier) ? scenario.tier : v.tier)})").ToArray();
            if (variants.Length > 0) variantIndex = Mathf.Clamp(EditorGUILayout.Popup("Variant", variantIndex, variants), 0, variants.Length - 1);
            seeds = EditorGUILayout.TextField("Seeds", seeds);
            ticks = EditorGUILayout.IntField(new GUIContent("Ticks (-1 = the scenario's)"), ticks);
            twice = EditorGUILayout.ToggleLeft("Run each seed twice and compare the hashes", twice);
            EditorGUILayout.LabelField("Scene", scenario.scene);
            EditorGUILayout.LabelField("Expect", string.Join("; ", scenario.expect), EditorStyles.wordWrappedLabel);
            if (Application.isPlaying) { EditorGUILayout.HelpBox("Leave Play mode to run scenarios.", MessageType.Info); return; }
            if (GUILayout.Button("Run")) Run(scenario, variants.Length > 0 ? scenario.variants[variantIndex].name : "");
            if (GUILayout.Button("Clear")) results.Clear();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var r in results) EditorGUILayout.SelectableLabel(r, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(42));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Runs the scenario variant once (or twice) per seed of the Seeds field, headless, and lists the results.</summary>
        void Run(ScenarioAsset scenario, string variant)
        {
            var list = new List<int>();
            foreach (var part in seeds.Split(','))
                if (int.TryParse(part.Trim(), out int seed)) list.Add(seed);
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("EvoSim scenario", $"{scenario.name} {variant} seed {list[i]}", (float)i / list.Count)) break;
                    string first = Batch.Run(scenario, variant, list[i], ticks > 0 ? ticks : (int?)null);
                    results.Add(first);
                    if (!twice) continue;
                    string second = Batch.Run(scenario, variant, list[i], ticks > 0 ? ticks : (int?)null);
                    results.Add((Hash(first) == Hash(second) ? "PASS same hash twice: " : "FAIL hashes differ: ") + Hash(first) + " / " + Hash(second));
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        /// <summary>The 16-character event hash in a run's summary line, or empty when the line has none (a failed run).</summary>
        static string Hash(string line)
        {
            int i = line.IndexOf("hash ");
            return i < 0 ? "" : line.Substring(i + 5, 16);
        }
    }
}
