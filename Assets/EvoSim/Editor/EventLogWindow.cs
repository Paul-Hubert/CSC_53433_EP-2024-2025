using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The event log (21 §4, lab D): the running world's events as they are recorded, filtered by kind, species and
    /// animal; following an animal shows its line and its descendants' births.
    /// </summary>
    /// <remarks>
    /// Open it with <c>Window ▸ EvoSim ▸ Event Log</c>. It listens to the running world as an <see cref="IEventSink"/>, so
    /// it only shows events recorded since it started watching. The filters are exact: <c>Kind</c> is an event kind
    /// (<c>birth</c>, <c>death</c>…), <c>Species</c> a species id, <c>Animal</c> an animal id (0 = every animal).
    /// </remarks>
    public class EventLogWindow : EditorWindow, IEventSink
    {
        /// <summary>The number of most recent events kept in memory; older ones are dropped.</summary>
        const int Keep = 5000;
        readonly Queue<(SimEvent e, string line)> events = new Queue<(SimEvent, string)>();   // the last Keep events with their lines
        World watched;                        // the running world this window listens to, or null
        string kind = "", species = "";       // filters: an event kind and a species id; empty = all
        int animal;                           // filter: an animal id whose events and descendants' events are shown; 0 = all
        bool follow = true;                   // keeps the list scrolled to the newest event
        Vector2 scroll;                       // the list's scroll position

        /// <summary>Opens the window (<c>Window ▸ EvoSim ▸ Event Log</c>).</summary>
        [MenuItem("Window/EvoSim/Event Log")]
        public static void Open() => GetWindow<EventLogWindow>("Events").Show();

        /// <summary>Keeps one recorded event with its canonical line; called by the world's event log as it records.</summary>
        /// <param name="e">The event.</param>
        /// <param name="line">Its canonical text line, as written to the run's event file.</param>
        public void OnEvent(SimEvent e, string line)
        {
            events.Enqueue((e, line));
            while (events.Count > Keep) events.Dequeue();
        }

        /// <summary>Follows the current world (registers as its event sink when it runs) and redraws in Play mode.</summary>
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

        /// <summary>Stops listening to the world when the window closes or scripts reload.</summary>
        void OnDisable() => watched?.Events?.RemoveSink(this);

        /// <summary>Draws the filters and the last 1 000 matching events, with the run's event hash.</summary>
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
                // Events are in time order, so a birth whose parent is already in the family adds the child before its own events.
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
