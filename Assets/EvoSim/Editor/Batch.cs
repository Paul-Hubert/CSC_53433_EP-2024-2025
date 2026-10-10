using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim
{
    /// <summary>
    /// Headless runs (30 §6, 31 §1, 32 §4): Unity -batchmode -executeMethod EvoSim.Batch.RunScenario -scenario S03
    /// [-variant name] [-seed n] [-ticks n]. In the open editor the same runs go through RunScenarioNamed (the unity CLI's
    /// eval). Worlds run in Freeze mode, outside Play mode: the World initialises itself (ARCH-04).
    /// </summary>
    public static class Batch
    {
        const string ScenariosFolder = "Assets/EvoSim/Scenarios";

        /// <summary>Command-line entry point: runs one scenario (every seed of it unless -seed is given) and exits.</summary>
        public static void RunScenario()
        {
            int code = 0;
            try
            {
                string name = Arg("-scenario") ?? throw new ArgumentException("-scenario is required");
                string variant = Arg("-variant") ?? "";
                int? seed = Arg("-seed") != null ? int.Parse(Arg("-seed")) : (int?)null;
                int? ticks = Arg("-ticks") != null ? int.Parse(Arg("-ticks")) : (int?)null;
                var scenario = FindScenario(name) ?? throw new ArgumentException("no scenario " + name);
                foreach (var s in seed.HasValue ? new List<int> { seed.Value } : scenario.seeds)
                {
                    var result = Run(scenario, variant, s, ticks);
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
        public static string RunScenarioNamed(string name, string variant = "", int seed = -1, int ticks = -1)
        {
            var scenario = FindScenario(name);
            if (scenario == null) return "FAILED: no scenario " + name;
            var sb = new StringBuilder();
            foreach (var s in seed >= 0 ? new List<int> { seed } : scenario.seeds)
                sb.AppendLine(Run(scenario, variant, s, ticks >= 0 ? ticks : (int?)null));
            return sb.ToString().TrimEnd();
        }

        /// <summary>Runs one scenario variant and seed in Freeze mode; the recorder writes the run's files.</summary>
        public static string Run(ScenarioAsset scenario, string variant, int seed, int? ticks = null, Action<World> beforeInitialize = null)
        {
            return WithScene(scenario.scene, world =>
            {
                var problems = new List<string>();
                var applied = scenario.Apply(world, variant, seed, problems);
                if (ticks.HasValue) world.TickLimit = ticks.Value;
                world.WaitMode = WaitMode.Freeze;
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
                while (world.State != RunState.Stopped) world.Advance(100);
                return $"{scenario.name} {(variant.Length > 0 ? variant : "default")} seed {seed}: {world.Tick} ticks, hash {world.Events.ShortHash}, stopped: {world.StopReason}, " +
                       $"{clock.Elapsed.TotalSeconds:0.0} s, folder {recorder?.RunFolder}";
            });
        }

        /// <summary>Opens a scene additively (or uses the open one), runs the action on its World, then closes it unsaved.</summary>
        public static string WithScene(string scenePath, Func<World, string> action)
        {
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

        /// <summary>The events hash of a scene after a number of ticks with the random brain (R-01).</summary>
        public static string HashOf(string scenePath, int seed, int ticks, Action<World> configure = null)
        {
            string hash = null;
            WithScene(scenePath, world =>
            {
                world.Seed = seed;
                world.TickLimit = ticks;
                world.WaitMode = WaitMode.Freeze;
                world.StartOnPlay = false;
                foreach (var r in world.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                configure?.Invoke(world);
                if (!world.Initialize()) throw new InvalidOperationException("validation: " + world.LastReport);
                while (world.State != RunState.Stopped) world.Advance(200);
                hash = world.Events.Hash;
                return hash;
            });
            return hash;
        }

        /// <summary>A scenario asset by asset name or by the start of its title ("S03").</summary>
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

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
