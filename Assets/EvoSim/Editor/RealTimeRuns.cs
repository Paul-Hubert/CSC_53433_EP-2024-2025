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
    /// <remarks>
    /// <para>A real-time test compares what a watcher sees in Play mode with a headless run of the same seed:</para>
    /// <list type="bullet">
    /// <item><description><see cref="Headless"/> (blocks until the end) or <see cref="HeadlessStart"/> and
    /// <see cref="HeadlessStatus"/> (stepped, for long runs with model calls): the reference run, with its files and
    /// hash.</description></item>
    /// <item><description><see cref="PreparePlay"/>, then Play (the unity CLI's <c>editor_play</c>), then <see cref="Status"/>
    /// while it plays: the watched run, with the same settings as the Windows player.</description></item>
    /// <item><description><see cref="Behaviour"/>: counts, tick by tick, what the watcher should see (hiding, kills,
    /// births).</description></item>
    /// </list>
    /// <para>With the editor closed: <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod
    /// EvoSim.Editor.RealTimeRuns.FromCommandLine -do headless|behaviour -scene HideVsFlee -seed 1234 -ticks 600 -level L2
    /// [-run NAME] -quit</c> (see <see cref="FromCommandLine"/>). Headless runs write their run folder under
    /// <c>Logs/EvoSim/runs</c>. The scene must not be open in the editor.</para>
    /// </remarks>
    public static class RealTimeRuns
    {
        /// <summary>The path of a reference scene from its name.</summary>
        /// <param name="scene">A scene name of <see cref="ReferenceScenes.ScenesFolder"/>, such as <c>HideVsFlee</c>.</param>
        /// <returns><c>Assets/EvoSim/Scenes/&lt;scene&gt;.unity</c>.</returns>
        public static string ScenePath(string scene) => ReferenceScenes.ScenesFolder + "/" + scene + ".unity";

        /// <summary>Sets a level on a World: L0 random brain and no mutation; L1 the scene's own default brain, no mutation; L2 both on.</summary>
        /// <param name="w">A World that has not initialised yet.</param>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c>.</param>
        /// <returns>Null when the level was set; otherwise the problem (an unknown level, or no random brain for L0).</returns>
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
        /// <remarks>It blocks the editor until the run ends; for a long run with model calls use <see cref="HeadlessStart"/>.</remarks>
        /// <param name="scene">A reference scene name; it must not be open in the editor.</param>
        /// <param name="seed">The world seed.</param>
        /// <param name="ticks">The run length in ticks.</param>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c> (<see cref="SetLevel"/>).</param>
        /// <param name="runName">The run folder's name under <c>Logs/EvoSim/runs</c>.</param>
        /// <returns>A summary line (ticks, full events hash, brain, brain and mutator calls, cache hits, mutations, seconds, run
        /// folder), or a line starting with <c>FAILED:</c>.</returns>
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

        static World stepping;                                     // the World of the stepped headless run, or null when none runs
        static UnityEngine.SceneManagement.Scene steppingScene;    // its scene, opened additively and closed at the end
        static System.Diagnostics.Stopwatch steppingClock;         // wall-clock time since it started
        static string steppingInfo, lastResult;                    // "scene level seed n"; the last run's result line, or null
        /// <summary>The file that holds the stepped run's state, then its result line.</summary>
        const string ResultFile = "Logs/EvoSim/realtime-headless.txt";

        /// <summary>
        /// The same headless run, advanced from the editor's update loop a few ticks at a time, so a long run with model calls
        /// never blocks a CLI call (the pipeline's timer); poll <see cref="HeadlessStatus"/>. The result also goes to Logs/EvoSim/realtime-headless.txt.
        /// </summary>
        /// <remarks>One stepped run at a time. A script reload during the run loses it (the static state is reset).</remarks>
        /// <param name="scene">A reference scene name; it must not be open in the editor.</param>
        /// <param name="seed">The world seed.</param>
        /// <param name="ticks">The run length in ticks.</param>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c> (<see cref="SetLevel"/>).</param>
        /// <param name="runName">The run folder's name under <c>Logs/EvoSim/runs</c>.</param>
        /// <returns><c>started …</c> with the run folder, the status of the run already going, or a line starting with
        /// <c>FAILED:</c>.</returns>
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

        /// <summary>
        /// Called by the editor's update loop: advances the stepped run one tick at a time for up to 50 ms, then, once it has
        /// stopped (or failed), writes the result, unhooks itself and closes the scene.
        /// </summary>
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
        /// <returns><c>running …</c> with the tick, calls and seconds; or the last result line; or <c>no headless run</c>.</returns>
        public static string HeadlessStatus()
        {
            var w = stepping;
            if (w == null) return lastResult ?? "no headless run";
            return $"running {steppingInfo}: tick {w.Tick}, brain calls {w.Decisions.ModelCalls}, cache hits {w.Decisions.CacheHits}, mutator calls " +
                   $"{w.Mutations.ModelCalls}, {steppingClock.Elapsed.TotalSeconds:0} s";
        }

        /// <summary>The session key that holds the prepared player arguments until Play mode ends.</summary>
        const string PlayArgsKey = "EvoSim.RealTimeRuns.PlayArgs";

        /// <summary>The player's arguments of a level (PlayerArgs): L0 the random brain and no mutation, L1 no mutation, L2 the scene as it is.</summary>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c>; anything else counts as L2.</param>
        /// <returns><c>-brain Random -noMutation</c>, <c>-noMutation</c>, or an empty string.</returns>
        public static string LevelArgs(string level) => level == "L0" ? "-brain Random -noMutation" : level == "L1" ? "-noMutation" : "";

        /// <summary>
        /// Opens a scene for a watched Play-mode run (step 3) without changing it: the settings are kept as a player command
        /// line (PlayerArgs) and applied to the World when Play mode wakes it (World.Configuring), as the Windows player does.
        /// Enter Play mode afterwards (editor_play); leaving Play mode forgets them.
        /// </summary>
        /// <remarks>
        /// For example <c>unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.PreparePlay("HideVsFlee",
        /// 1234, 600, 5f, "L2", "RUN");'</c>. The scene replaces the open scenes, so they must have no unsaved change.
        /// </remarks>
        /// <param name="scene">A reference scene name.</param>
        /// <param name="seed">The world seed (<c>-seed</c>).</param>
        /// <param name="ticks">The run length in ticks (<c>-ticks</c>).</param>
        /// <param name="ticksPerSecond">The pace of the watched run, in ticks per second (<c>-speed</c>).</param>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c> (<see cref="LevelArgs"/>).</param>
        /// <param name="runName">The run folder's name (<c>-run</c>).</param>
        /// <param name="responsive">True to wait for the brains while Unity keeps rendering; false to freeze (<c>-wait</c>).</param>
        /// <param name="captureSeconds">Seconds between two screen captures (<c>-capture</c>; 0 = start and end only).</param>
        /// <param name="extra">More player arguments, such as <c>-stagger</c> or <c>-minutes 10</c>; empty for none.</param>
        /// <returns><c>&lt;scene&gt; open, unchanged; on Play: &lt;arguments&gt;</c>, or why nothing was prepared.</returns>
        public static string PreparePlay(string scene, int seed, int ticks, float ticksPerSecond, string level, string runName, bool responsive = true,
                                         float captureSeconds = 10f, string extra = "")
        {
            if (EditorApplication.isPlaying) return "leave Play mode first";
            if (level != "L0" && level != "L1" && level != "L2") return "level " + level + ": L0, L1 or L2";
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "an open scene has unsaved changes: save or discard them first";
            EditorSceneManager.OpenScene(ScenePath(scene), OpenSceneMode.Single);
            string args = string.Join(" ", "-scene", scene, "-seed", seed, "-ticks", ticks, "-speed", ticksPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                      "-wait", responsive ? "responsive" : "freeze", "-capture", captureSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                      "-run", runName, LevelArgs(level), extra ?? "").Trim();
            SessionState.SetString(PlayArgsKey, args);
            return $"{scene} open, unchanged; on Play: {args}";
        }

        /// <summary>
        /// Hooks <see cref="ApplyPrepared"/> on <see cref="World.Configuring"/> and <see cref="Forget"/> on Play mode changes;
        /// Unity runs it when the editor loads or recompiles. The prepared arguments are then applied to the World that wakes
        /// in Play mode, like the player's launcher.
        /// </summary>
        [InitializeOnLoadMethod]
        static void HookPlayMode()
        {
            World.Configuring -= ApplyPrepared;
            World.Configuring += ApplyPrepared;
            EditorApplication.playModeStateChanged -= Forget;
            EditorApplication.playModeStateChanged += Forget;
        }

        /// <summary>
        /// Applies the prepared player arguments to a World of the prepared scene as it configures, and to the scene's run
        /// capture; logs what was set and any problem. Does nothing when nothing is prepared.
        /// </summary>
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

        /// <summary>Forgets the prepared arguments once the editor is back in Edit mode.</summary>
        static void Forget(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode) SessionState.EraseString(PlayArgsKey);
        }

        /// <summary>The run in Play mode now: tick, state, pace, numbers, hash, folders (step 3, polled).</summary>
        /// <returns>One line about the first World found: tick, state and what it waits for, ticks per second and frames per
        /// second, brain and mutator calls, events hash, run and capture folders, memory (MB); or <c>not playing</c> /
        /// <c>no World</c>.</returns>
        public static string Status()
        {
            var w = Object.FindAnyObjectByType<World>();
            if (w == null) return EditorApplication.isPlaying ? "no World" : "not playing";
            var overlay = Object.FindAnyObjectByType<RunOverlay>();
            var capture = Object.FindAnyObjectByType<RunCapture>();
            var recorder = w.Service<RunRecorder>();
            string pace = overlay != null ? $"{overlay.TicksPerSecond:0.0} ticks/s, {overlay.FramesPerSecond:0} fps, waiting {overlay.WaitingSeconds:0.0} s; run: {overlay.PaceSummary()}" : "no overlay";
            return $"playing {EditorApplication.isPlaying}, focused {UnityEditorInternal.InternalEditorUtility.isApplicationActive}, tick {w.Tick}, {w.State}" +
                   (w.State == RunState.Stopped ? $" ({w.StopReason})" : "") + (w.WaitingFor != null ? $" for {RunNumbers.Waiting(w)}" : "") +
                   $", {pace}, waits {w.WaitCount}, views {w.ViewCount}, brain calls {w.Decisions.ModelCalls}, " +
                   $"cache {w.Decisions.CacheHits}, mutator calls {w.Mutations.ModelCalls}, mutations {w.Mutations.Successes}/{w.Mutations.Failures}, hash {w.Events?.Hash}, " +
                   $"run {recorder?.RunFolder}, captures {capture?.Captures} in {capture?.RunFolder}, sheet {capture?.ContactSheetPath}, " +
                   $"memory {System.GC.GetTotalMemory(false) / 1048576.0:0} MB managed, {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576.0:0} MB native";
        }

        /// <summary>
        /// What a watcher should see, counted tick by tick on a headless replay (step 5): hunting predators whose nearest prey
        /// in sight is hidden in cover (they lose it), hunters targeting a hidden prey (never), prey hiding in cover, kills,
        /// carcass portions, prey meals and food regrown, babies farther than 2 m from both parents.
        /// </summary>
        /// <remarks>No run files are written. The scene needs species with the ids <c>prey</c> and <c>predator</c> (the Lab 1
        /// scenes, HideVsFlee, Terrain_Locomotion).</remarks>
        /// <param name="scene">A reference scene name; it must not be open in the editor.</param>
        /// <param name="seed">The world seed.</param>
        /// <param name="ticks">The run length in ticks.</param>
        /// <param name="level"><c>L0</c>, <c>L1</c> or <c>L2</c> (<see cref="SetLevel"/>).</param>
        /// <returns>One line of counts (in animal-ticks unless said), or a line starting with <c>FAILED:</c>.</returns>
        public static string Behaviour(string scene, int seed, int ticks, string level)
        {
            return Batch.WithScene(ScenePath(scene), w =>
            {
                w.Seed = seed;
                w.TickLimit = ticks;
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                string problem = SetLevel(w, level);
                if (problem != null) return "FAILED: " + problem;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                if (!w.Initialize()) return "FAILED: validation: " + w.LastReport;
                var prey = w.FindSpecies("prey");
                var predator = w.FindSpecies("predator");
                var food = w.Service<FoodGrid>();
                int hunting = 0, lost = 0, targetedHidden = 0, hiding = 0, hidingInCover = 0, babies = 0, farBabies = 0, outside = 0;
                int foodStart = food != null ? food.Count : 0;
                var where = new System.Collections.Generic.Dictionary<int, Vector3>();
                while (w.State != RunState.Stopped)
                {
                    w.Advance(1);
                    where.Clear();
                    foreach (var s in w.AllSpecies)
                        foreach (var a in s.Animals)
                        {
                            if (a.IsGone) continue;
                            where[a.Id] = a.Position;
                            if (!w.Ground.Inside(a.Position) || Mathf.Abs(a.Position.y - w.Ground.Height(a.Position)) > 1e-3f) outside++;
                        }
                    foreach (var p in predator.Animals)
                    {
                        if (p.IsGone || p.CurrentAction == null || p.CurrentAction.Name != "hunt") continue;
                        hunting++;
                        float vision = w.Queries.Vision(p), best = float.PositiveInfinity;
                        Animal nearest = null;
                        foreach (var q in prey.Animals)
                        {
                            if (q.IsGone) continue;
                            float d = w.Distance(p.Position, q.Position);
                            if (d <= vision && d < best) { best = d; nearest = q; }
                        }
                        if (nearest != null && w.Queries.IsHiddenFrom(nearest, predator)) lost++;
                        var target = p.TargetId >= 0 ? prey.Animals.FirstOrDefault(q => q.Id == p.TargetId && !q.IsGone) : null;
                        if (target != null && w.Queries.IsHiddenFrom(target, predator)) targetedHidden++;
                    }
                    foreach (var q in prey.Animals)
                    {
                        if (q.IsGone || q.CurrentAction == null || q.CurrentAction.Name != "hide") continue;
                        hiding++;
                        if (w.Queries.InCover(q)) hidingInCover++;
                    }
                    foreach (var s in w.AllSpecies)
                        foreach (var a in s.Animals)
                        {
                            if (a.IsGone || a.Origin != "birth" || a.BornTick != w.Tick - 1) continue;
                            babies++;
                            float near = float.PositiveInfinity;
                            foreach (int id in a.Parents) if (where.TryGetValue(id, out var pp)) near = Mathf.Min(near, w.Distance(pp, a.Position));
                            if (near > 2f) farBabies++;
                        }
                }
                int foodEnd = food != null ? food.Count : 0;
                return $"{scene} {level} seed {seed}, {w.Tick} ticks, hash {w.Events.ShortHash}: hunting predator-ticks {hunting}, nearest prey in sight hidden {lost}, " +
                       $"hunters targeting a hidden prey {targetedHidden}; prey hide-ticks {hiding} ({hidingInCover} in cover); kills {predator.Counters.Kills}, " +
                       $"carcass portions {predator.Counters.Portions}; prey meals {prey.Counters.Meals}, food {foodStart} then {foodEnd} (regrown {foodEnd - foodStart + prey.Counters.Meals}); " +
                       $"babies {babies}, more than 2 m from both living parents {farBabies}; animal-ticks off the ground or outside it {outside}";
            });
        }

        /// <summary>
        /// Batch entry point (the editor closed): -do headless|behaviour -scene S -seed n -ticks n -level L0|L1|L2 [-run name];
        /// the result line goes to the log and to Logs/EvoSim/realtime-batch.txt (appended).
        /// </summary>
        /// <remarks>
        /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Editor.RealTimeRuns.FromCommandLine -do
        /// headless -scene HideVsFlee -seed 1234 -ticks 600 -level L2 [-run NAME] -quit</c>. Defaults: <c>-do headless</c>,
        /// <c>-scene HideVsFlee</c>, <c>-seed 1234</c>, <c>-ticks 600</c>, <c>-level L0</c>, <c>-run
        /// &lt;scene&gt;-&lt;level&gt;-s&lt;seed&gt;-&lt;ticks&gt;</c>. It does not exit Unity by itself: pass <c>-quit</c>.
        /// </remarks>
        public static void FromCommandLine()
        {
            // The value after an option on the command line, or the fallback.
            string Arg(string name, string fallback)
            {
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
                return fallback;
            }
            string what = Arg("-do", "headless"), scene = Arg("-scene", "HideVsFlee"), level = Arg("-level", "L0");
            int seed = int.Parse(Arg("-seed", "1234")), ticks = int.Parse(Arg("-ticks", "600"));
            string result;
            try
            {
                result = what == "behaviour" ? Behaviour(scene, seed, ticks, level)
                       : Headless(scene, seed, ticks, level, Arg("-run", $"{scene}-{level}-s{seed}-{ticks}"));
            }
            catch (System.Exception e) { result = "FAILED: " + e; }
            Debug.Log("EvoSim real-time batch: " + result);
            Directory.CreateDirectory("Logs/EvoSim");
            File.AppendAllText("Logs/EvoSim/realtime-batch.txt", $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} {what}: {result}\n");
        }

        /// <summary>The events hash recorded in a run folder's summary.json.</summary>
        /// <param name="runFolder">A run folder, such as one under <c>Logs/EvoSim/runs</c>.</param>
        /// <returns>The <c>events_hash</c> of its <c>summary.json</c>, or a sentence saying the file is missing.</returns>
        public static string HashIn(string runFolder)
        {
            string path = Path.Combine(runFolder, "summary.json");
            if (!File.Exists(path)) return "no summary.json in " + runFolder;
            var o = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            return (string)o["events_hash"];
        }
    }
}
