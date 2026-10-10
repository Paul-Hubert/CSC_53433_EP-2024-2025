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
    public static class PlayerLauncher
    {
        public static PlayerArgs Args { get; private set; }
        static bool loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Application.isEditor) return;
            Args = PlayerArgs.Parse(Environment.GetCommandLineArgs());
            foreach (var p in Args.Problems) Debug.LogWarning("EvoSim player: " + p);
            World.Configuring += Configure;
        }

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
                      $"{(Args.QuitAtEnd ? ", quit at the end" : "")}; files under {RunCapture.Resolve(".")}");
        }
    }

    /// <summary>Quits the player once its World stopped and the captures are written (-quitAtEnd).</summary>
    public sealed class QuitWhenStopped : MonoBehaviour
    {
        public World World;
        float stoppedAt = -1f;

        void Update()
        {
            if (World == null || World.State != RunState.Stopped) return;
            if (stoppedAt < 0f) stoppedAt = Time.unscaledTime;
            var capture = FindAnyObjectByType<RunCapture>();
            bool written = capture == null || !capture.isActiveAndEnabled || capture.Finished;
            if (!written && Time.unscaledTime - stoppedAt < 30f) return;
            Debug.Log($"EvoSim player: run stopped ({World.StopReason}) at tick {World.Tick}, events hash {World.Events.Hash}; quitting");
            Application.Quit();
        }
    }
}
