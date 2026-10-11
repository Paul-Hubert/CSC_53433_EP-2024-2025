using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The World inspector (21 §3): state and tick, run controls in Play mode, the settings, the phases in order with
    /// their enable toggles, the services, and the validation panel.
    /// </summary>
    /// <remarks>
    /// <para>Unity shows it in place of the default inspector when a World is selected. In Play mode, once the world runs,
    /// the buttons Play, Pause, Step, Run N and Stop drive it. The phase list follows the children of the World's
    /// <c>Phases</c> object: a toggle switches a phase on or off, ▲ moves it up one place (Edit mode only, with Undo).</para>
    /// <para><c>Validate</c> runs the validation (<see cref="ValidationPanel"/>); in Play mode the button shows the report of
    /// the world's initialisation. A fix button applies its fix and validates again.</para>
    /// <para>A student who wants an inspector for a module of their own writes the same kind of class:
    /// <c>[CustomEditor(typeof(MyModule))]</c> on a class deriving from <c>UnityEditor.Editor</c>, in an Editor folder or
    /// editor assembly.</para>
    /// </remarks>
    [CustomEditor(typeof(World))]
    public class WorldInspector : UnityEditor.Editor
    {
        ValidationReport report;          // the last validation shown, or null before the first
        int runFor = 100;                 // the N of Run N, in ticks
        bool showPhases = true, showServices;   // foldouts
        double waitingSince = -1;         // editor time (seconds) when the world started waiting for the brains, or -1

        /// <summary>True in Play mode, so the tick and state stay current.</summary>
        /// <inheritdoc/>
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        /// <summary>
        /// Draws the state, the run controls (Play mode), the World's fields, the phases, the services and the validation.
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>
        /// Draws the state line: not initialised, or the run state and tick, with what the world waits for (the brain, the
        /// number of queries, seconds waited) and the stop reason.
        /// </summary>
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

        /// <summary>Draws the run controls of a running world: Play, Pause, Step, Run N (ticks) and Stop.</summary>
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

        /// <summary>
        /// Draws the phases under the <c>Phases</c> child in tick order, with a toggle to switch each on or off and a ▲
        /// button to move it up; changes are refused in Play mode.
        /// </summary>
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

        /// <summary>Lists every world service below the World by its service name and class, marking the switched-off ones.</summary>
        void DrawServices(World w)
        {
            showServices = EditorGUILayout.Foldout(showServices, "Services", true);
            if (!showServices) return;
            foreach (var s in w.GetComponentsInChildren<WorldService>(true))
                EditorGUILayout.LabelField(s.ServiceName, s.GetType().Name + (s.isActiveAndEnabled ? "" : " (off)"));
        }
    }
}
