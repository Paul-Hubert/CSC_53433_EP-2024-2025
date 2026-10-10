using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// Builds a Windows player that draws a run (07 real-time test): Windows 64-bit, Mono, the reference scenes
    /// (HideVsFlee first) given here, not in the Build Settings, into Builds/EvoSim/&lt;date&gt;/EvoSim.exe. Project and
    /// Player settings are left as they are. Batch: -executeMethod EvoSim.Editor.VisualPlayerBuild.BuildFromCommandLine
    /// [-scenes HideVsFlee,Lab1_Full] [-out folder]. Run it: EvoSim.exe -scene HideVsFlee -seed 1234 -ticks 600
    /// -speed 5 -brain JEV -capture 10 -quitAtEnd -screen-fullscreen 0 -logFile player.log (PlayerArgs).
    /// </summary>
    public static class VisualPlayerBuild
    {
        public static readonly string[] DefaultScenes = { "HideVsFlee", "Lab1_Small", "Lab1_Full", "ThreeSpecies", "Terrain_Locomotion", "Sandbox" };

        [MenuItem("EvoSim/Build/Visual Player")]
        public static void BuildFromMenu()
        {
            string result = Build(DefaultScenes, DefaultFolder());
            EditorUtility.DisplayDialog("EvoSim Visual Player", result, "OK");
        }

        /// <summary>The -executeMethod entry point; exits with 0 when the build succeeded.</summary>
        public static void BuildFromCommandLine()
        {
            string scenes = Arg("-scenes"), folder = Arg("-out") ?? DefaultFolder();
            string result = Build(scenes != null ? scenes.Split(',') : DefaultScenes, folder);
            Debug.Log("EvoSim visual player: " + result);
            if (Application.isBatchMode) EditorApplication.Exit(result.StartsWith("Succeeded") ? 0 : 1);
        }

        public static string DefaultFolder() => "Builds/EvoSim/" + DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// For the unity CLI's eval in the open editor: starts the build after the call returns (a long synchronous call
        /// outlives the pipeline's timer) and writes the result to Logs/EvoSim/visual-player-build.txt.
        /// </summary>
        public static string BuildLater(string sceneList = null, string folder = null)
        {
            const string resultFile = "Logs/EvoSim/visual-player-build.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(resultFile));
            File.WriteAllText(resultFile, "building\n");
            EditorApplication.delayCall += () =>
            {
                string result;
                try { result = Build(sceneList != null ? sceneList.Split(',') : DefaultScenes, folder ?? DefaultFolder()); }
                catch (Exception e) { result = "Failed: " + e; }
                File.WriteAllText(resultFile, result + "\n");
                Debug.Log("EvoSim visual player: " + result);
            };
            return "build scheduled; result in " + resultFile;
        }

        /// <summary>Builds the player; returns "Succeeded: …" or "Failed: …".</summary>
        public static string Build(IEnumerable<string> sceneNames, string folder)
        {
            var scenes = new List<string>();
            foreach (var n in sceneNames)
            {
                string path = ReferenceScenes.ScenesFolder + "/" + n.Trim() + ".unity";
                if (!File.Exists(path)) return "Failed: no scene " + path;
                scenes.Add(path);
            }
            if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x)
                return "Failed: the Standalone scripting backend isn't Mono; this build leaves Player settings alone (RAND-12 compares Mono runs)";
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(folder, "EvoSim.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            return $"{s.result}: {Path.GetFullPath(options.locationPathName)}, {scenes.Count} scenes, {s.totalSize / 1048576f:0.0} MB, " +
                   $"{s.totalTime.TotalSeconds:0} s, {s.totalErrors} errors, {s.totalWarnings} warnings";
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
