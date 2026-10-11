using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim
{
    /// <summary>
    /// In a player (not the editor), reads the command line (<see cref="PlayerArgs"/>) and sets each World before it
    /// starts (<see cref="World.Configuring"/>). With -scene, a World of another scene doesn't start and that scene
    /// loads instead. With -quitAtEnd, the player quits once the run stopped and its captures are written.
    /// Run files, captures and the answer cache land next to the build (paths are relative to its folder).
    /// </summary>
    /// <remarks>
    /// It installs itself before the first scene loads; nothing needs to be placed in a scene. Settings from the command
    /// line never go into the scene file: they are applied to the World in memory, run after run.
    /// </remarks>
    public static class PlayerLauncher
    {
        /// <summary>The parsed command line, or null in the editor.</summary>
        public static PlayerArgs Args { get; private set; }
        static bool loading;    // the -scene scene is loading: load it only once

        /// <summary>Parses the command line, sets the window size, and listens for Worlds being configured (players only).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Application.isEditor) return;
            Args = PlayerArgs.Parse(Environment.GetCommandLineArgs());
            foreach (var p in Args.Problems) Debug.LogWarning("EvoSim player: " + p);
            if (Args.WindowWidth > 0) Screen.SetResolution(Args.WindowWidth, Args.WindowHeight, FullScreenMode.Windowed);
            World.Configuring += Configure;
        }

        /// <summary>
        /// Sets a World from the arguments, or, when -scene names another scene, keeps it from starting and loads that scene;
        /// then sets the capture and -quitAtEnd, and logs what was set.
        /// </summary>
        static void Configure(World w)
        {
            if (!string.IsNullOrEmpty(Args.Scene) && !string.Equals(w.gameObject.scene.name, Args.Scene, StringComparison.OrdinalIgnoreCase))
            {
                w.StartOnPlay = false;
                if (!loading)
                {
                    loading = true;
                    Debug.Log($"EvoSim player: loading scene {Args.Scene}");
                    SceneManager.LoadScene(Args.Scene);
                }
                return;
            }
            string said = Args.ApplyTo(w);
            foreach (var p in Args.Problems) Debug.LogWarning("EvoSim player: " + p);
            Args.Problems.Clear();
            Args.ApplyTo(UnityEngine.Object.FindAnyObjectByType<RunCapture>());
            if (Args.QuitAtEnd) w.gameObject.AddComponent<QuitWhenStopped>().World = w;
            Debug.Log($"EvoSim player: scene {w.gameObject.scene.name}, {(said.Length > 0 ? said : "the scene's settings")}" +
                      $"{(Args.QuitAtEnd ? ", quit at the end" : "")}; files under {RunCapture.Resolve(".")}; " +
                      $"screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}, dpi {Screen.dpi:0}");
        }
    }

    /// <summary>Quits the player once its World stopped and the captures are written (-quitAtEnd).</summary>
    /// <remarks>
    /// Added to the World's GameObject by <see cref="PlayerLauncher"/>. It waits at most 30 seconds for the captures, then
    /// logs the stop reason, the tick, the events hash and the pace before quitting.
    /// </remarks>
    public sealed class QuitWhenStopped : MonoBehaviour
    {
        /// <summary>The World to watch.</summary>
        public World World;
        float stoppedAt = -1f;      // real time when the stop was first seen, in seconds; -1 before

        /// <summary>Each frame: once the World stopped and the captures are written (or 30 s passed), logs and quits.</summary>
        void Update()
        {
            if (World == null || World.State != RunState.Stopped) return;
            if (stoppedAt < 0f) stoppedAt = Time.unscaledTime;
            var capture = FindAnyObjectByType<RunCapture>();
            bool written = capture == null || !capture.isActiveAndEnabled || capture.Finished;
            if (!written && Time.unscaledTime - stoppedAt < 30f) return;
            var overlay = FindAnyObjectByType<RunOverlay>();
            Debug.Log($"EvoSim player: run stopped ({World.StopReason}) at tick {World.Tick}, events hash {World.Events.Hash}; " +
                      $"pace {(overlay != null ? overlay.PaceSummary() : "not measured (no overlay)")}; screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}; quitting");
            Application.Quit();
        }
    }
}
