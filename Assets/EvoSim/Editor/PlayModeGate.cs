using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Editor
{
    /// <summary>
    /// EDIT-01: before Play, every enabled World in the open scenes is validated with the same engine as
    /// World.Initialize; errors cancel Play and are logged with the object at fault (EDIT-02).
    /// </summary>
    /// <remarks>
    /// When Play is blocked, the Console shows one line per error, <c>EvoSim: Play blocked — V-xx …</c>; clicking it
    /// selects the object at fault. The fix buttons are in the World and Species inspectors (<see cref="ValidationPanel"/>).
    /// Warnings and information never block Play.
    /// </remarks>
    [InitializeOnLoad]
    public static class PlayModeGate
    {
        /// <summary>Hooks the gate on Play mode changes; Unity runs it when the editor loads or recompiles.</summary>
        static PlayModeGate() => EditorApplication.playModeStateChanged += OnChange;

        /// <summary>When the editor leaves Edit mode for Play, validates the open worlds and cancels Play on an error.</summary>
        static void OnChange(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            var errors = Errors(OpenWorlds());
            if (errors.Count == 0) return;
            EditorApplication.isPlaying = false;
            foreach (var m in errors) Debug.LogError("EvoSim: Play blocked — " + m, m.Object);
        }

        /// <summary>The enabled Worlds of the open scenes.</summary>
        /// <returns>Every active and enabled World of the loaded scenes, possibly none.</returns>
        public static List<World> OpenWorlds()
        {
            var worlds = new List<World>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var w in root.GetComponentsInChildren<World>(false))
                        if (w.isActiveAndEnabled) worlds.Add(w);
            }
            return worlds;
        }

        /// <summary>The validation errors of these worlds (warnings and information don't block).</summary>
        /// <param name="worlds">The worlds to validate; each is prepared with <see cref="World.Prepare"/>.</param>
        /// <returns>The error messages of all the worlds, or an empty list when Play may start.</returns>
        public static List<ValidationMessage> Errors(IEnumerable<World> worlds)
        {
            var errors = new List<ValidationMessage>();
            foreach (var w in worlds)
            {
                var report = new ValidationReport();
                w.Prepare(report);
                foreach (var m in report.Messages)
                    if (m.Severity == Severity.Error) errors.Add(m);
            }
            return errors;
        }
    }
}
