using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim
{
    /// <summary>
    /// Headless runs (30 §6, 31 §1, 32 §4): Unity -batchmode -executeMethod EvoSim.Batch.RunScenario -scenario S03
    /// [-variant name] [-seed n] [-ticks n] [-responsive]. In the open editor the same runs go through RunScenarioNamed (the unity CLI's
    /// eval). Worlds run in Freeze mode, outside Play mode: the World initialises itself (ARCH-04).
    /// </summary>
    /// <remarks>
    /// <para>The command-line entry points, run from the project folder with the editor closed (or on a copy of the
    /// project), each exit Unity with 0 on success, 1 on a failure and, for <see cref="RunScenario"/> and
    /// <see cref="RunIntegrity"/>, 2 on an exception:</para>
    /// <list type="bullet">
    /// <item><description><c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunScenario
    /// -scenario S03 [-variant name] [-seed n] [-ticks n] [-responsive]</c>: one scenario (31 §1); each run's files go to
    /// <c>Logs/EvoSim/runs/&lt;scenario&gt;-&lt;variant&gt;-s&lt;seed&gt;-&lt;time&gt;/</c>.</description></item>
    /// <item><description><c>… -executeMethod EvoSim.Batch.RunRegression</c>: the EditMode tests of category T3 (pinned
    /// hashes, equivalences, scene checks); a report <c>Logs/EvoSim/Reports/regression-&lt;time&gt;.txt</c>.</description></item>
    /// <item><description><c>… -executeMethod EvoSim.Batch.RunScenarios -tier T2|T3|T4 [-ticks n]</c>: every scenario
    /// variant of the tier, each seed twice (same hash); a report
    /// <c>Logs/EvoSim/Reports/scenarios-&lt;tier&gt;-&lt;time&gt;.txt</c>.</description></item>
    /// <item><description><c>… -executeMethod EvoSim.Batch.RunIntegrity [-checks B-02,B-03] [-brain JEV] [-cpu] [-judge model]</c>:
    /// the brain and mutator checks of 32 §2 (<see cref="Integrity"/>); one JSON and one Markdown report per check under
    /// <c>Logs/EvoSim/Reports</c>.</description></item>
    /// </list>
    /// <para>From the open editor, the same code runs through <see cref="RunScenarioNamed"/> and <see cref="RunTier"/>
    /// (for instance with the unity CLI's <c>eval</c>), or the Scenario Runner window. A scene that is open in the editor
    /// cannot be run: <see cref="WithScene"/> refuses it.</para>
    /// </remarks>
    public static partial class Batch
    {
        /// <summary>The folder searched for scenario assets by name.</summary>
        const string ScenariosFolder = "Assets/EvoSim/Scenarios";

        /// <summary>Command-line entry point: runs one scenario (every seed of it unless -seed is given) and exits.</summary>
        /// <remarks>
        /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunScenario -scenario S03
        /// [-variant name] [-seed n] [-ticks n] [-responsive]</c>. <c>-scenario</c> is an asset name or the id that starts its
        /// title (<see cref="FindScenario"/>); without <c>-variant</c> the first variant runs; <c>-responsive</c> waits for
        /// the brains one tick at a time instead of freezing. Each run logs one summary line (<c>EvoSim scenario: …</c>) and
        /// writes its run folder under <c>Logs/EvoSim/runs</c>. Exit code: 0, 1 when a run failed, 2 on an exception.
        /// </remarks>
        public static void RunScenario()
        {
            int code = 0;
            try
            {
                string name = Arg("-scenario") ?? throw new ArgumentException("-scenario is required");
                string variant = Arg("-variant") ?? "";
                int? seed = Arg("-seed") != null ? int.Parse(Arg("-seed")) : (int?)null;
                int? ticks = Arg("-ticks") != null ? int.Parse(Arg("-ticks")) : (int?)null;
                var mode = Environment.GetCommandLineArgs().Contains("-responsive") ? WaitMode.Responsive : WaitMode.Freeze;
                var scenario = FindScenario(name) ?? throw new ArgumentException("no scenario " + name);
                foreach (var s in seed.HasValue ? new List<int> { seed.Value } : scenario.seeds)
                {
                    var result = Run(scenario, variant, s, ticks, null, mode);
                    Debug.Log("EvoSim scenario: " + result);
                    if (result.StartsWith("FAILED")) code = 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 2;
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        /// <summary>The same run from the open editor (unity command eval): returns a one-line summary.</summary>
        /// <remarks>
        /// For example <c>unity command --timeout 900 --result-only eval --code 'return EvoSim.Batch.RunScenarioNamed("S03");'</c>.
        /// Open another scene first: the scenario's own scene must not be open.
        /// </remarks>
        /// <param name="name">The scenario's asset name or id (<c>S03</c>).</param>
        /// <param name="variant">The variant's name; empty = the first variant.</param>
        /// <param name="seed">One seed, or -1 for every seed of the scenario.</param>
        /// <param name="ticks">The run length in ticks, or -1 for the scenario's.</param>
        /// <param name="responsive">True to run in Responsive wait mode instead of Freeze.</param>
        /// <returns>One summary line per seed (see <see cref="Run"/>), or <c>FAILED: no scenario …</c>.</returns>
        public static string RunScenarioNamed(string name, string variant = "", int seed = -1, int ticks = -1, bool responsive = false)
        {
            var scenario = FindScenario(name);
            if (scenario == null) return "FAILED: no scenario " + name;
            var sb = new StringBuilder();
            foreach (var s in seed >= 0 ? new List<int> { seed } : scenario.seeds)
                sb.AppendLine(Run(scenario, variant, s, ticks >= 0 ? ticks : (int?)null, null, responsive ? WaitMode.Responsive : WaitMode.Freeze));
            return sb.ToString().TrimEnd();
        }

        /// <summary>Runs one scenario variant and seed in Freeze mode; the recorder writes the run's files.</summary>
        /// <remarks>
        /// The scene is opened additively, the scenario applied (seed, length, brain, overrides, removals), the world
        /// initialised and advanced until it stops, then the scene is closed unsaved. The run folder
        /// (<c>Logs/EvoSim/runs/&lt;scenario&gt;-&lt;variant&gt;-s&lt;seed&gt;-&lt;time&gt;/</c>) holds <c>run_info.json</c>
        /// (with the scenario, variant and applied overrides), <c>events.jsonl</c>, <c>stats.csv</c>, <c>alleles.jsonl</c>,
        /// <c>final_population.json</c> and <c>summary.json</c>.
        /// </remarks>
        /// <param name="scenario">The scenario asset.</param>
        /// <param name="variant">The variant's name; empty or unknown = the first variant.</param>
        /// <param name="seed">The world seed.</param>
        /// <param name="ticks">The run length in ticks, or null for the scenario's.</param>
        /// <param name="beforeInitialize">Called on the world after the scenario is applied and before it initialises (tests use
        /// it to swap a brain), or null.</param>
        /// <param name="mode">Freeze (one call advances up to 100 ticks and blocks on the brains) or Responsive (one tick per
        /// call, yielding while the brains answer).</param>
        /// <returns>
        /// A summary line: <c>&lt;scenario&gt; &lt;variant&gt; seed &lt;n&gt; (&lt;mode&gt;): &lt;ticks&gt; ticks, hash
        /// &lt;16 hex&gt;, stopped: &lt;reason&gt;, &lt;s&gt; s, model calls …, folder &lt;path&gt;</c>; or a line starting with
        /// <c>FAILED:</c> when the scenario can't be applied, the world doesn't validate or the scene is open.
        /// </returns>
        public static string Run(ScenarioAsset scenario, string variant, int seed, int? ticks = null, Action<World> beforeInitialize = null,
                                 WaitMode mode = WaitMode.Freeze)
        {
            return WithScene(scenario.scene, world =>
            {
                var problems = new List<string>();
                var applied = scenario.Apply(world, variant, seed, problems);
                if (ticks.HasValue) world.TickLimit = ticks.Value;
                world.WaitMode = mode;
                world.StartOnPlay = false;
                if (problems.Count > 0) return "FAILED: " + string.Join("; ", problems);
                var recorder = world.Service<RunRecorder>() ?? world.GetComponentInChildren<RunRecorder>();
                if (recorder != null)
                {
                    recorder.RunName = $"{Safe(scenario.name)}-{Safe(variant.Length > 0 ? variant : "default")}-s{seed}-{DateTime.Now:yyyyMMdd-HHmmss}";
                    recorder.ExtraInfo["scenario"] = scenario.name;
                    recorder.ExtraInfo["variant"] = variant;
                    recorder.ExtraInfo["overrides"] = applied;
                }
                beforeInitialize?.Invoke(world);
                if (!world.Initialize()) return "FAILED: validation: " + world.LastReport;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var call = new System.Diagnostics.Stopwatch();
                double longest = 0;
                int calls = 0;
                while (world.State != RunState.Stopped)
                {
                    call.Restart();
                    world.Advance(mode == WaitMode.Responsive ? 1 : 100);              // Responsive: one tick per frame at most
                    calls++;
                    longest = Math.Max(longest, call.Elapsed.TotalMilliseconds);
                    if (mode == WaitMode.Responsive && world.State == RunState.Waiting) System.Threading.Thread.Sleep(1);   // a frame's worth of yielding
                }
                return $"{scenario.name} {(variant.Length > 0 ? variant : "default")} seed {seed} ({mode}): {world.Tick} ticks, hash {world.Events.ShortHash}, " +
                       $"stopped: {world.StopReason}, {clock.Elapsed.TotalSeconds:0.0} s, model calls {world.Decisions.ModelCalls} decisions + " +
                       $"{world.Mutations.ModelCalls} mutations, longest Advance call {longest:0} ms over {calls}, folder {recorder?.RunFolder}";
            });
        }

        /// <summary>Opens a scene additively, runs the action on its World, then closes it unsaved.</summary>
        /// <remarks>A scene already open in the editor is refused: running on it would change it, then close it.</remarks>
        /// <param name="scenePath">The scene, relative to the project (<c>Assets/EvoSim/Scenes/….unity</c>).</param>
        /// <param name="action">What to do with the scene's first World; its string is returned.</param>
        /// <returns>The action's result, or a line starting with <c>FAILED:</c> when the scene is open or has no World.</returns>
        public static string WithScene(string scenePath, Func<World, string> action)
        {
            if (SceneManager.GetSceneByPath(scenePath).isLoaded)                       // it would run on the open scene, change it and close it
                return "FAILED: " + scenePath + " is open in the editor: open another scene first";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                World world = null;
                foreach (var root in scene.GetRootGameObjects())
                    if ((world = root.GetComponentInChildren<World>(true)) != null) break;
                if (world == null) return "FAILED: no World in " + scenePath;
                return action(world);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>The events hash of a scene after a number of ticks with the random brain (R-01); configure may swap more (the mutator).</summary>
        /// <remarks>No run files are written. The regression tests pin these hashes.</remarks>
        /// <param name="scenePath">The scene, relative to the project; it must not be open in the editor.</param>
        /// <param name="seed">The world seed.</param>
        /// <param name="ticks">The run length in ticks.</param>
        /// <param name="configure">Called on the world before it initialises, after the random brain is made the default,
        /// or null.</param>
        /// <returns>The full hash of the run's events.</returns>
        /// <exception cref="InvalidOperationException">The world doesn't validate, or the scene can't be run.</exception>
        public static string HashOf(string scenePath, int seed, int ticks, Action<World> configure = null)
        {
            string hash = null;
            string result = WithScene(scenePath, world =>
            {
                world.Seed = seed;
                world.TickLimit = ticks;
                world.WaitMode = WaitMode.Freeze;
                world.StartOnPlay = false;
                var random = world.GetComponentInChildren<RandomBrain>(true);
                if (random != null) world.DefaultBrain = random;                         // R-01: the random-brain twin
                foreach (var r in world.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                configure?.Invoke(world);
                if (!world.Initialize()) throw new InvalidOperationException("validation: " + world.LastReport);
                while (world.State != RunState.Stopped) world.Advance(200);
                hash = world.Events.Hash;
                return hash;
            });
            return hash ?? throw new InvalidOperationException(result);
        }

        /// <summary>A scenario asset by asset name or by the start of its title ("S03").</summary>
        /// <param name="name">An asset name (<c>S03_Lab1Small</c>), the part before its first underscore, or the first word of
        /// its title.</param>
        /// <returns>The first matching asset under <c>Assets/EvoSim/Scenarios</c>, or null.</returns>
        public static ScenarioAsset FindScenario(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScenarioAsset", new[] { ScenariosFolder }))
            {
                var s = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (s == null) continue;
                if (s.name == name || s.name.StartsWith(name + "_") || s.title.StartsWith(name + " ")) return s;
            }
            return null;
        }

        /// <summary>The value after a command-line option (<c>-seed 1234</c> gives <c>"1234"</c>), or null when it is absent.</summary>
        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        /// <summary>A name safe for a folder: letters, digits, '-' and '_' kept, anything else replaced by '_'.</summary>
        static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
