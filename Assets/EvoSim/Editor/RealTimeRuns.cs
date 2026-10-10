using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The real-time visual test from the unity CLI (eval): a headless reference run, a scene set up for Play mode, and the
    /// run's status while it plays. Levels: L0 the random brain and no mutation (offline), L1 the scene's brain (JEV) and
    /// no mutation, L2 the scene's brain and mutator. Example:
    /// <code>unity command eval --code 'return EvoSim.Editor.RealTimeRuns.Headless("HideVsFlee", 1234, 600, "L2", "RT-L2-headless");'</code>
    /// </summary>
    public static class RealTimeRuns
    {
        public static string ScenePath(string scene) => ReferenceScenes.ScenesFolder + "/" + scene + ".unity";

        /// <summary>Sets a level on a World: L0 random brain and no mutation; L1 the scene's own default brain, no mutation; L2 both on.</summary>
        public static string SetLevel(World w, string level)
        {
            switch (level)
            {
                case "L0":
                    var random = w.GetComponentInChildren<RandomBrain>(true);
                    if (random == null) return "no random brain";
                    w.DefaultBrain = random;
                    w.NoMutation = true;
                    return null;
                case "L1": w.NoMutation = true; return null;
                case "L2": w.NoMutation = false; return null;
                default: return "level " + level + ": L0, L1 or L2";
            }
        }

        /// <summary>A headless Freeze run with files (step 2): returns the events hash, the run folder and the model calls.</summary>
        public static string Headless(string scene, int seed, int ticks, string level, string runName)
        {
            return Batch.WithScene(ScenePath(scene), w =>                              // as Batch.HashOf, with the level's brain and the files
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                w.Seed = seed;
                w.TickLimit = ticks;
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                string problem = SetLevel(w, level);
                if (problem != null) return "FAILED: " + problem;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) { r.WriteFiles = true; r.RunName = runName; }
                if (!w.Initialize()) return "FAILED: validation: " + w.LastReport;
                while (w.State != RunState.Stopped) w.Advance(50);
                var recorder = w.Service<RunRecorder>();
                return $"{scene} {level} seed {seed}: {w.Tick} ticks, hash {w.Events.Hash}, brain {w.DefaultBrain.name}, brain calls {w.Decisions.ModelCalls}, " +
                       $"cache hits {w.Decisions.CacheHits}, mutator calls {w.Mutations.ModelCalls} (cache {w.Mutations.CacheHits}), mutations " +
                       $"{w.Mutations.Successes} ok {w.Mutations.Failures} failed, {clock.Elapsed.TotalSeconds:0.0} s, folder {recorder?.RunFolder}";
            });
        }

        static World stepping;
        static UnityEngine.SceneManagement.Scene steppingScene;
        static System.Diagnostics.Stopwatch steppingClock;
        static string steppingInfo, lastResult;
        const string ResultFile = "Logs/EvoSim/realtime-headless.txt";

        /// <summary>
        /// The same headless run, advanced from the editor's update loop a few ticks at a time, so a long run with model calls
        /// never blocks a CLI call (the pipeline's timer); poll <see cref="HeadlessStatus"/>. The result also goes to Logs/EvoSim/realtime-headless.txt.
        /// </summary>
        public static string HeadlessStart(string scene, int seed, int ticks, string level, string runName)
        {
            if (stepping != null) return "a headless run is going: " + HeadlessStatus();
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath(scene)).isLoaded)
                return "FAILED: " + scene + " is open in the editor: open another scene first (the run would change it)";
            steppingScene = EditorSceneManager.OpenScene(ScenePath(scene), OpenSceneMode.Additive);
            var w = steppingScene.GetRootGameObjects().Select(r => r.GetComponentInChildren<World>(true)).First(x => x != null);
            w.Seed = seed;
            w.TickLimit = ticks;
            w.WaitMode = WaitMode.Freeze;
            w.StartOnPlay = false;
            string problem = SetLevel(w, level);
            foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) { r.WriteFiles = true; r.RunName = runName; }
            if (problem != null || !w.Initialize())
            {
                EditorSceneManager.CloseScene(steppingScene, true);
                return "FAILED: " + (problem ?? "validation: " + w.LastReport);
            }
            stepping = w;
            steppingInfo = $"{scene} {level} seed {seed}";
            steppingClock = System.Diagnostics.Stopwatch.StartNew();
            lastResult = null;
            Directory.CreateDirectory(Path.GetDirectoryName(ResultFile));
            File.WriteAllText(ResultFile, "running " + steppingInfo + "\n");
            EditorApplication.update += Step;
            return "started " + steppingInfo + ", folder " + w.Service<RunRecorder>()?.RunFolder;
        }

        static void Step()
        {
            var w = stepping;
            if (w == null) { EditorApplication.update -= Step; return; }
            try
            {
                var budget = System.Diagnostics.Stopwatch.StartNew();
                while (w.State != RunState.Stopped && budget.ElapsedMilliseconds < 50) w.Advance(1);
            }
            catch (System.Exception e) { lastResult = "FAILED: " + e.Message; }
            if (w.State != RunState.Stopped && lastResult == null) return;
            lastResult ??= $"{steppingInfo}: {w.Tick} ticks ({w.StopReason}), hash {w.Events.Hash}, brain {w.DefaultBrain.name}, brain calls {w.Decisions.ModelCalls}, " +
                           $"cache hits {w.Decisions.CacheHits}, mutator calls {w.Mutations.ModelCalls} (cache {w.Mutations.CacheHits}), mutations " +
                           $"{w.Mutations.Successes} ok {w.Mutations.Failures} failed, {steppingClock.Elapsed.TotalSeconds:0.0} s, folder {w.Service<RunRecorder>()?.RunFolder}";
            File.WriteAllText(ResultFile, lastResult + "\n");
            EditorApplication.update -= Step;
            stepping = null;
            EditorSceneManager.CloseScene(steppingScene, true);
        }

        /// <summary>The stepped headless run's progress, or its result once it stopped.</summary>
        public static string HeadlessStatus()
        {
            var w = stepping;
            if (w == null) return lastResult ?? "no headless run";
            return $"running {steppingInfo}: tick {w.Tick}, brain calls {w.Decisions.ModelCalls}, cache hits {w.Decisions.CacheHits}, mutator calls " +
                   $"{w.Mutations.ModelCalls}, {steppingClock.Elapsed.TotalSeconds:0} s";
        }

        const string PlayArgsKey = "EvoSim.RealTimeRuns.PlayArgs";

        /// <summary>The player's arguments of a level (PlayerArgs): L0 the random brain and no mutation, L1 no mutation, L2 the scene as it is.</summary>
        public static string LevelArgs(string level) => level == "L0" ? "-brain Random -noMutation" : level == "L1" ? "-noMutation" : "";

        /// <summary>
        /// Opens a scene for a watched Play-mode run (step 3) without changing it: the settings are kept as a player command
        /// line (PlayerArgs) and applied to the World when Play mode wakes it (World.Configuring), as the Windows player does.
        /// Enter Play mode afterwards (editor_play); leaving Play mode forgets them.
        /// </summary>
        public static string PreparePlay(string scene, int seed, int ticks, float ticksPerSecond, string level, string runName, bool responsive = true,
                                         float captureSeconds = 10f)
        {
            if (EditorApplication.isPlaying) return "leave Play mode first";
            if (level != "L0" && level != "L1" && level != "L2") return "level " + level + ": L0, L1 or L2";
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "an open scene has unsaved changes: save or discard them first";
            EditorSceneManager.OpenScene(ScenePath(scene), OpenSceneMode.Single);
            string args = string.Join(" ", "-scene", scene, "-seed", seed, "-ticks", ticks, "-speed", ticksPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                      "-wait", responsive ? "responsive" : "freeze", "-capture", captureSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                      "-run", runName, LevelArgs(level)).Trim();
            SessionState.SetString(PlayArgsKey, args);
            return $"{scene} open, unchanged; on Play: {args}";
        }

        /// <summary>Applies the prepared arguments to the World that wakes in Play mode (once), like the player's launcher.</summary>
        [InitializeOnLoadMethod]
        static void HookPlayMode()
        {
            World.Configuring -= ApplyPrepared;
            World.Configuring += ApplyPrepared;
            EditorApplication.playModeStateChanged -= Forget;
            EditorApplication.playModeStateChanged += Forget;
        }

        static void ApplyPrepared(World w)
        {
            string args = SessionState.GetString(PlayArgsKey, "");
            if (args.Length == 0) return;
            var p = PlayerArgs.Parse(args.Split(' '));
            if (p.Scene != null && w.gameObject.scene.name != p.Scene) return;      // another scene's World (a test, say)
            string said = p.ApplyTo(w);
            p.ApplyTo(Object.FindAnyObjectByType<RunCapture>());
            foreach (var problem in p.Problems) Debug.LogWarning("EvoSim real-time run: " + problem);
            Debug.Log($"EvoSim real-time run: {w.gameObject.scene.name}, {said}");
        }

        static void Forget(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode) SessionState.EraseString(PlayArgsKey);
        }

        /// <summary>The run in Play mode now: tick, state, pace, numbers, hash, folders (step 3, polled).</summary>
        public static string Status()
        {
            var w = Object.FindAnyObjectByType<World>();
            if (w == null) return EditorApplication.isPlaying ? "no World" : "not playing";
            var overlay = Object.FindAnyObjectByType<RunOverlay>();
            var capture = Object.FindAnyObjectByType<RunCapture>();
            var recorder = w.Service<RunRecorder>();
            string pace = overlay != null ? $"{overlay.TicksPerSecond:0.0} ticks/s, {overlay.FramesPerSecond:0} fps, waiting {overlay.WaitingSeconds:0.0} s" : "no overlay";
            return $"playing {EditorApplication.isPlaying}, focused {UnityEditorInternal.InternalEditorUtility.isApplicationActive}, tick {w.Tick}, {w.State}" +
                   (w.State == RunState.Stopped ? $" ({w.StopReason})" : "") + $", {pace}, views {w.ViewCount}, brain calls {w.Decisions.ModelCalls}, " +
                   $"cache {w.Decisions.CacheHits}, mutator calls {w.Mutations.ModelCalls}, mutations {w.Mutations.Successes}/{w.Mutations.Failures}, hash {w.Events?.Hash}, " +
                   $"run {recorder?.RunFolder}, captures {capture?.Captures} in {capture?.RunFolder}, sheet {capture?.ContactSheetPath}, " +
                   $"memory {System.GC.GetTotalMemory(false) / 1048576.0:0} MB managed, {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576.0:0} MB native";
        }

        /// <summary>The events hash recorded in a run folder's summary.json.</summary>
        public static string HashIn(string runFolder)
        {
            string path = Path.Combine(runFolder, "summary.json");
            if (!File.Exists(path)) return "no summary.json in " + runFolder;
            var o = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            return (string)o["events_hash"];
        }
    }
}
