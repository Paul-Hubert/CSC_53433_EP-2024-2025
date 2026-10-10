using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The ecology monitor (21 §4, labs A and B): population per species over time, deaths by cause, births, mean
    /// energy and stamina, action shares, memo hit rate, ticks per second — from LiveStatistics and the species counters.
    /// </summary>
    public class EcologyMonitorWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Window/EvoSim/Ecology Monitor")]
        public static void Open() => GetWindow<EcologyMonitorWindow>("Ecology").Show();

        void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }

        void OnGUI()
        {
            var w = EditorWorlds.Current();
            if (w == null || !w.IsInitialized) { EditorGUILayout.HelpBox("No running world. Enter Play mode in a scene with a World.", MessageType.Info); return; }
            var live = w.Service<LiveStatistics>();
            EditorGUILayout.LabelField(w.name, $"tick {w.Tick}, {w.State}" + (live != null ? $", {live.TicksPerSecond:0.0} ticks/s" : ""));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (live != null) Chart(w, live);
            else EditorGUILayout.HelpBox("Add a LiveStatistics service (under Recording) for the graphs.", MessageType.None);
            foreach (var s in w.AllSpecies) Species(s);
            EditorGUILayout.EndScrollView();
        }

        static void Chart(World w, LiveStatistics live)
        {
            var rect = GUILayoutUtility.GetRect(200, 140, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            int maxPop = 1, maxTick = 1;
            foreach (var s in w.AllSpecies)
                foreach (var p in live.Of(s.Id)) { maxPop = Mathf.Max(maxPop, p.Population); maxTick = Mathf.Max(maxTick, p.Tick); }
            int k = 0;
            foreach (var s in w.AllSpecies)
            {
                var series = live.Of(s.Id);
                if (series.Count < 2) { k++; continue; }
                var points = new List<Vector3>(series.Count);
                foreach (var p in series)
                    points.Add(new Vector3(rect.x + rect.width * p.Tick / maxTick, rect.yMax - rect.height * p.Population / maxPop, 0f));
                Handles.color = EditorWorlds.ActionColor(k * 3 + 1);
                Handles.DrawAAPolyLine(2f, points.ToArray());
                GUI.Label(new Rect(rect.x + 4, rect.y + 2 + 16 * k, 200, 16), s.DisplayName, new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Handles.color } });
                k++;
            }
            GUI.Label(new Rect(rect.xMax - 80, rect.y + 2, 80, 16), $"max {maxPop}", EditorStyles.miniLabel);
        }

        static void Species(Species s)
        {
            var c = s.Counters;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{s.DisplayName}: {s.Animals.Count} living", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Births, founders", $"{c.Births}, {c.Founders}");
            var deaths = new List<string>();
            foreach (var kv in c.Deaths) deaths.Add($"{kv.Key} {kv.Value}");
            deaths.Sort(System.StringComparer.Ordinal);
            EditorGUILayout.LabelField("Deaths", deaths.Count > 0 ? string.Join(", ", deaths) : "none");
            EditorGUILayout.LabelField("Decisions, memo hits", c.Decisions > 0 ? $"{c.Decisions}, {100f * c.MemoHits / c.Decisions:0}%" : "0");
            long total = 0;
            foreach (var n in c.ActionCounts) total += n;
            if (total == 0) return;
            for (int i = 0; i < s.Actions.Count && i < c.ActionCounts.Length; i++)
            {
                var r = EditorGUILayout.GetControlRect();
                float share = (float)c.ActionCounts[i] / total;
                GUI.Label(new Rect(r.x, r.y, 80, r.height), s.Actions[i].Name);
                EditorGUI.DrawRect(new Rect(r.x + 84, r.y + 2, (r.width - 140) * share, r.height - 4), EditorWorlds.ActionColor(i));
                GUI.Label(new Rect(r.xMax - 50, r.y, 50, r.height), share.ToString("P0"));
            }
        }
    }
}
