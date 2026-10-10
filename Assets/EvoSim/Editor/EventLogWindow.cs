using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The event log (21 §4, lab D): the running world's events as they are recorded, filtered by kind, species and
    /// animal; following an animal shows its line and its descendants' births.
    /// </summary>
    public class EventLogWindow : EditorWindow, IEventSink
    {
        const int Keep = 5000;
        readonly Queue<(SimEvent e, string line)> events = new Queue<(SimEvent, string)>();
        World watched;
        string kind = "", species = "";
        int animal;
        bool follow = true;
        Vector2 scroll;

        [MenuItem("Window/EvoSim/Event Log")]
        public static void Open() => GetWindow<EventLogWindow>("Events").Show();

        public void OnEvent(SimEvent e, string line)
        {
            events.Enqueue((e, line));
            while (events.Count > Keep) events.Dequeue();
        }

        void OnInspectorUpdate()
        {
            var w = EditorWorlds.Current();
            if (w != watched)
            {
                watched?.Events?.RemoveSink(this);
                events.Clear();
                watched = w != null && w.IsInitialized ? w : null;
                watched?.Events?.AddSink(this);
            }
            if (Application.isPlaying) Repaint();
        }

        void OnDisable() => watched?.Events?.RemoveSink(this);

        void OnGUI()
        {
            if (watched == null) { EditorGUILayout.HelpBox("No running world.", MessageType.Info); return; }
            EditorGUILayout.BeginHorizontal();
            kind = EditorGUILayout.TextField("Kind", kind);
            species = EditorGUILayout.TextField("Species", species);
            animal = EditorGUILayout.IntField("Animal (and offspring)", animal);
            follow = EditorGUILayout.ToggleLeft("Follow", follow, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            var family = new HashSet<int>();
            if (animal > 0) family.Add(animal);
            var shown = new List<string>();
            foreach (var (e, line) in events)
            {
                if (animal > 0 && e.Kind == "birth" && e["parents"] is System.Collections.IEnumerable parents)
                    foreach (var p in parents) if (p is int pid && family.Contains(pid)) family.Add(e.Id);
                if (kind.Length > 0 && e.Kind != kind) continue;
                if (species.Length > 0 && e.Species != species) continue;
                if (animal > 0 && !family.Contains(e.Id)) continue;
                shown.Add(line);
            }
            if (follow) scroll.y = float.MaxValue;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            int from = Mathf.Max(0, shown.Count - 1000);
            for (int i = from; i < shown.Count; i++) EditorGUILayout.SelectableLabel(shown[i], EditorStyles.miniLabel, GUILayout.Height(14));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField($"{shown.Count} of {events.Count} kept events (last {Keep}); hash {watched.Events.ShortHash}", EditorStyles.miniLabel);
        }
    }
}
