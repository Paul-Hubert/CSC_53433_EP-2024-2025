using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The World inspector (21 §3): state and tick, run controls in Play mode, the settings, the phases in order with
    /// their enable toggles, the services, and the validation panel.
    /// </summary>
    [CustomEditor(typeof(World))]
    public class WorldInspector : UnityEditor.Editor
    {
        ValidationReport report;
        int runFor = 100;
        bool showPhases = true, showServices;
        double waitingSince = -1;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            var w = (World)target;
            DrawState(w);
            if (Application.isPlaying && w.IsInitialized) DrawControls(w);
            EditorGUILayout.Space();
            DrawDefaultInspector();
            EditorGUILayout.Space();
            DrawPhases(w);
            DrawServices(w);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (GUILayout.Button(Application.isPlaying ? "Show the last report" : "Validate")) report = ValidationPanel.Validate(w);
            if (ValidationPanel.Draw(report)) report = ValidationPanel.Validate(w);
        }

        void DrawState(World w)
        {
            string state;
            if (!w.IsInitialized) state = "Not initialised";
            else
            {
                state = $"{w.State} — tick {w.Tick}";
                if (w.State == RunState.Waiting || w.State == RunState.Blocked)
                {
                    if (waitingSince < 0) waitingSince = EditorApplication.timeSinceStartup;
                    var brain = w.DefaultBrain != null ? w.DefaultBrain.Id : "the brains";
                    state += $" — waiting for {brain}: {w.Decisions.Due.Count} queries, {EditorApplication.timeSinceStartup - waitingSince:0.0} s";
                }
                else waitingSince = -1;
                if (w.State == RunState.Stopped && !string.IsNullOrEmpty(w.StopReason)) state += $" ({w.StopReason})";
            }
            EditorGUILayout.HelpBox(state, MessageType.None);
        }

        void DrawControls(World w)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play")) w.Play();
            if (GUILayout.Button("Pause")) w.Pause();
            if (GUILayout.Button("Step")) w.Step();
            runFor = EditorGUILayout.IntField(runFor, GUILayout.Width(60));
            if (GUILayout.Button("Run N")) w.RunFor(runFor);
            if (GUILayout.Button("Stop")) w.RequestStop("stopped from the inspector");
            EditorGUILayout.EndHorizontal();
        }

        void DrawPhases(World w)
        {
            showPhases = EditorGUILayout.Foldout(showPhases, "Phases (in this order every tick)", true);
            if (!showPhases) return;
            var folder = w.transform.Find("Phases");
            if (folder == null) { EditorGUILayout.HelpBox("No Phases child: the World finds phases anywhere below it, in hierarchy order.", MessageType.None); return; }
            for (int i = 0; i < folder.childCount; i++)
            {
                var child = folder.GetChild(i);
                foreach (var p in child.GetComponents<TickPhase>())
                {
                    EditorGUILayout.BeginHorizontal();
                    bool on = EditorGUILayout.ToggleLeft($"{i + 1}. {child.name}  ({p.GetType().Name})", p.enabled && child.gameObject.activeSelf);
                    if (on != (p.enabled && child.gameObject.activeSelf) && !Application.isPlaying)
                    {
                        Undo.RecordObject(p, "Toggle phase");
                        p.enabled = on;
                        if (on && !child.gameObject.activeSelf) { Undo.RecordObject(child.gameObject, "Toggle phase"); child.gameObject.SetActive(true); }
                    }
                    if (i > 0 && GUILayout.Button("▲", GUILayout.Width(24)) && !Application.isPlaying)
                    {
                        Undo.SetTransformParent(child, folder, "Move phase");
                        child.SetSiblingIndex(i - 1);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void DrawServices(World w)
        {
            showServices = EditorGUILayout.Foldout(showServices, "Services", true);
            if (!showServices) return;
            foreach (var s in w.GetComponentsInChildren<WorldService>(true))
                EditorGUILayout.LabelField(s.ServiceName, s.GetType().Name + (s.isActiveAndEnabled ? "" : " (off)"));
        }
    }
}
