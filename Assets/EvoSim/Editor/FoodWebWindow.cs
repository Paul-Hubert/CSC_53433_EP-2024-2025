using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The food web (21 §4): species and layers as a graph with graze, strike, scavenge and egg edges, the derived
    /// threats, and the food-web warnings of validation.
    /// </summary>
    public class FoodWebWindow : EditorWindow
    {
        ValidationReport report;
        World shown;

        [MenuItem("Window/EvoSim/Food Web")]
        public static void Open() => GetWindow<FoodWebWindow>("Food Web").Show();

        void OnGUI()
        {
            var w = EditorWorlds.Current();
            if (w == null) { EditorGUILayout.HelpBox("No World in the open scenes.", MessageType.Info); return; }
            if (GUILayout.Button(w.IsInitialized ? "Refresh" : "Build the food web (validates the world)") || (shown != w && w.IsInitialized))
            {
                report = ValidationPanel.Validate(w);
                shown = w;
            }
            if (shown != w) return;

            var nodes = new List<string>();
            var layers = new List<string>();
            foreach (var s in w.AllSpecies) nodes.Add(s.Id);
            foreach (var service in w.Services) if (service is ResourceLayer l) layers.Add(l.LayerName);
            var rect = GUILayoutUtility.GetRect(300, 260, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));
            var pos = new Dictionary<string, Vector2>();
            for (int i = 0; i < nodes.Count; i++)
                pos[nodes[i]] = new Vector2(rect.x + rect.width * (i + 1) / (nodes.Count + 1), rect.y + 40 + (i % 2) * 70);
            for (int i = 0; i < layers.Count; i++)
                pos["layer:" + layers[i]] = new Vector2(rect.x + rect.width * (i + 1) / (layers.Count + 1), rect.yMax - 30);

            foreach (var s in w.AllSpecies)
            {
                var diet = s.Module<Diet>();
                if (diet == null) continue;
                foreach (var t in diet.Resolved)
                {
                    string to = t.Kind == EdibleTargetKind.Layer ? "layer:" + t.Layer.LayerName : t.Species != null ? t.Species.Id : null;
                    if (to == null || !pos.ContainsKey(to)) continue;
                    Handles.color = t.Kind == EdibleTargetKind.Layer ? Color.green : t.Kind == EdibleTargetKind.Species ? Color.red
                                  : t.Kind == EdibleTargetKind.Carcass ? new Color(0.6f, 0.4f, 0.2f) : Color.yellow;
                    Handles.DrawAAPolyLine(t.Kind == EdibleTargetKind.Species ? 3f : 2f, pos[s.Id], pos[to]);
                    GUI.Label(new Rect((pos[s.Id] + pos[to]) / 2f, new Vector2(80, 16)), t.Kind.ToString().ToLowerInvariant(), EditorStyles.miniLabel);
                }
            }
            foreach (var kv in pos)
            {
                var r = new Rect(kv.Value.x - 50, kv.Value.y - 10, 100, 20);
                EditorGUI.DrawRect(r, kv.Key.StartsWith("layer:") ? new Color(0.2f, 0.35f, 0.2f) : new Color(0.3f, 0.3f, 0.45f));
                GUI.Label(r, kv.Key.StartsWith("layer:") ? kv.Key.Substring(6) : kv.Key, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
            }

            EditorGUILayout.LabelField("Threats (derived)", EditorStyles.boldLabel);
            foreach (var s in w.AllSpecies)
            {
                var names = new List<string>();
                foreach (var t in w.ThreatsOf(s)) names.Add(t.DisplayName);
                EditorGUILayout.LabelField(s.DisplayName, names.Count > 0 ? string.Join(", ", names) : "none");
            }
            if (report == null) return;
            EditorGUILayout.LabelField("Food-web messages", EditorStyles.boldLabel);
            foreach (var m in report.Messages)
                if (m.Id == "V-20" || m.Id == "V-22" || m.Id == "V-24" || m.Id == "V-25")
                    EditorGUILayout.HelpBox($"{m.Id} {m.Text}", m.Severity == Severity.Error ? MessageType.Error : MessageType.Warning);
        }
    }
}
