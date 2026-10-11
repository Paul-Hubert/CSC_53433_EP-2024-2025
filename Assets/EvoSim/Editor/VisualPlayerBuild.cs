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
    /// <remarks>
    /// <para>From the menu: <c>EvoSim ▸ Build ▸ Visual Player</c> (a dialog shows the result). Batch, editor closed:
    /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Editor.VisualPlayerBuild.BuildFromCommandLine
    /// [-scenes HideVsFlee,Lab1_Full] [-out Builds/EvoSim/2026-10-11]</c>. From the open editor with the unity CLI:
    /// <see cref="BuildLater"/>, or <see cref="CompilePlayerScriptsLater"/> to check that the player scripts compile.</para>
    /// <para>The Standalone scripting backend must be Mono: the build refuses otherwise rather than change Player settings
    /// (RAND-12 compares Mono runs). The player writes its run files and captures next to the exe, under
    /// <c>Logs/EvoSim/</c>, and keeps its answer cache in <c>Library/EvoSim/AnswerCache</c>.</para>
    /// </remarks>
    public static class VisualPlayerBuild
    {
        /// <summary>
        /// The reference scenes in the build, by name in <see cref="ReferenceScenes.ScenesFolder"/>; the first is the player's
        /// start scene.
        /// </summary>
        public static readonly string[] DefaultScenes = { "HideVsFlee", "Lab1_Small", "Lab1_Full", "ThreeSpecies", "Terrain_Locomotion", "Sandbox" };

        /// <summary>
        /// <c>EvoSim ▸ Build ▸ Visual Player</c>: builds the player with the six default scenes into today's folder,
        /// <c>Builds/EvoSim/&lt;yyyy-MM-dd&gt;/EvoSim.exe</c>, and shows the result in a dialog.
        /// </summary>
        [MenuItem("EvoSim/Build/Visual Player")]
        public static void BuildFromMenu()
        {
            string result = Build(DefaultScenes, DefaultFolder());
            EditorUtility.DisplayDialog("EvoSim Visual Player", result, "OK");
        }

        /// <summary>The -executeMethod entry point; exits with 0 when the build succeeded.</summary>
        /// <remarks>
        /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Editor.VisualPlayerBuild.BuildFromCommandLine
        /// [-scenes A,B] [-out folder]</c>: <c>-scenes</c> lists scene names of <see cref="ReferenceScenes.ScenesFolder"/>
        /// (default <see cref="DefaultScenes"/>), <c>-out</c> the output folder (default <see cref="DefaultFolder"/>). The
        /// result line is logged as <c>EvoSim visual player: …</c>; exit code 1 on failure.
        /// </remarks>
        public static void BuildFromCommandLine()
        {
            string scenes = Arg("-scenes"), folder = Arg("-out") ?? DefaultFolder();
            string result = Build(scenes != null ? scenes.Split(',') : DefaultScenes, folder);
            Debug.Log("EvoSim visual player: " + result);
            if (Application.isBatchMode) EditorApplication.Exit(result.StartsWith("Succeeded") ? 0 : 1);
        }

        /// <summary>Today's build folder, relative to the project: <c>Builds/EvoSim/&lt;yyyy-MM-dd&gt;</c>.</summary>
        /// <returns>The folder path; a second build on the same day replaces the first.</returns>
        public static string DefaultFolder() => "Builds/EvoSim/" + DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// For the unity CLI's eval in the open editor: starts the build after the call returns (a long synchronous call
        /// outlives the pipeline's timer) and writes the result to Logs/EvoSim/visual-player-build.txt.
        /// </summary>
        /// <remarks>
        /// <c>unity command --result-only eval --code 'return EvoSim.Editor.VisualPlayerBuild.BuildLater();'</c>, then read the
        /// result file: it says <c>building</c> until the build ends.
        /// </remarks>
        /// <param name="sceneList">Comma-separated scene names, or null for <see cref="DefaultScenes"/>.</param>
        /// <param name="folder">The output folder, or null for <see cref="DefaultFolder"/>.</param>
        /// <returns>A line saying the build is scheduled and where its result will be written.</returns>
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

        /// <summary>
        /// For the unity CLI: compiles every script of the project for the Windows player, without building, after the call
        /// returns; the result goes to Logs/EvoSim/player-scripts.txt, the compiler's errors to the console.
        /// </summary>
        /// <remarks>A quick check that no editor-only code leaked into the runtime assemblies (EDIT-03); the compiled
        /// assemblies go to <c>Temp/EvoSimPlayerScripts</c>.</remarks>
        /// <returns>A line saying the compilation is scheduled and where its result will be written.</returns>
        public static string CompilePlayerScriptsLater()
        {
            const string resultFile = "Logs/EvoSim/player-scripts.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(resultFile));
            File.WriteAllText(resultFile, "compiling\n");
            EditorApplication.delayCall += () =>
            {
                string result;
                try
                {
                    var settings = new UnityEditor.Build.Player.ScriptCompilationSettings
                    {
                        target = BuildTarget.StandaloneWindows64, group = BuildTargetGroup.Standalone,
                    };
                    var compiled = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/EvoSimPlayerScripts");
                    IReadOnlyCollection<string> assemblies = compiled.assemblies;
                    result = assemblies != null && assemblies.Count > 0 ? $"Succeeded: {assemblies.Count} assemblies: {string.Join(", ", assemblies)}" : "Failed: see the console";
                }
                catch (Exception e) { result = "Failed: " + e.Message; }
                File.WriteAllText(resultFile, result + "\n");
            };
            return "scheduled; result in " + resultFile;
        }

        /// <summary>Builds the player; returns "Succeeded: …" or "Failed: …".</summary>
        /// <param name="sceneNames">Scene names in <see cref="ReferenceScenes.ScenesFolder"/>, without <c>.unity</c>; the first
        /// is the player's default scene.</param>
        /// <param name="folder">The output folder, relative to the project or absolute; <c>EvoSim.exe</c> is written in it.</param>
        /// <returns>The build result with the exe's full path, scene count, size (MB), time (s), errors and warnings; or
        /// <c>Failed: …</c> when a scene is missing or the scripting backend isn't Mono.</returns>
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

        /// <summary>The value after a command-line option, or null when it is absent.</summary>
        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
