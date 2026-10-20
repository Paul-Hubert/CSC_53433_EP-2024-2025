using System;
using System.IO;
using System.Linq;
using EvoSim;
using EvoSim.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the example scene <c>Assets/Student/Scenes/AlarmCalls.unity</c> (a copy of the reference HideVsFlee with alarm
/// calls added to the prey) and runs it headless to check that calls are made and heard.
/// </summary>
/// <remarks>
/// <para>Menu: <c>EvoSim ▸ Student ▸ Build Alarm Calls Scene</c> and <c>EvoSim ▸ Student ▸ Run Alarm Calls Headless (L0)</c>.
/// From the command line, with the editor closed:</para>
/// <para><c>Unity.exe -batchmode -nographics -projectPath . -executeMethod AlarmCallsScene.BuildFromCommandLine</c></para>
/// <para><c>Unity.exe -batchmode -nographics -projectPath . -executeMethod AlarmCallsScene.RunFromCommandLine [-seed n] [-ticks n]</c></para>
/// <para>Building the scene in code makes it reproducible: running the builder again gives the same scene.</para>
/// </remarks>
public static class AlarmCallsScene
{
    /// <summary>The reference scene the example starts from.</summary>
    public const string SourceScene = "Assets/EvoSim/Scenes/HideVsFlee.unity";
    /// <summary>The example scene this script writes.</summary>
    public const string ScenePath = "Assets/Student/Scenes/AlarmCalls.unity";

    /// <summary>The founder sentences of the <c>alarm</c> gene, before the neutral "No preference.".</summary>
    static readonly string[] Founders =
    {
        "Call out whenever you see a predator.",
        "Stay silent, save your energy.",
        "Warn your kin when danger is near.",
        "Call only when you are safe in cover.",
    };

    /// <summary>
    /// <c>EvoSim ▸ Student ▸ Build Alarm Calls Scene</c>: copies HideVsFlee to <see cref="ScenePath"/> and adds the alarm
    /// calls: the service and the fading phase under the World, the action with its gene and the sense under the prey.
    /// </summary>
    /// <returns>Null when the scene was written, otherwise what went wrong.</returns>
    [MenuItem("EvoSim/Student/Build Alarm Calls Scene")]
    public static string Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "cancelled";
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        AssetDatabase.DeleteAsset(ScenePath);                                    // rebuild from scratch: the same scene every time
        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath)) return "could not copy " + SourceScene;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var world = UnityEngine.Object.FindFirstObjectByType<World>();
        if (world == null) return "no World in " + ScenePath;
        var prey = world.GetComponentsInChildren<Species>(true).FirstOrDefault(s => s.gameObject.name == "Prey");
        if (prey == null) return "no Prey species";
        var environment = world.transform.Find("Environment");
        var phases = world.transform.Find("Phases");
        var act = phases != null ? phases.Find("Act") : null;
        var actions = prey.transform.Find("Actions");
        var senses = prey.transform.Find("Senses");
        if (environment == null || act == null || actions == null || senses == null) return "the scene is not laid out like HideVsFlee";

        // The world: the calls themselves, and a phase after Act that forgets the old ones.
        Child(environment, "Alarm calls").AddComponent<AlarmCalls>();
        var fade = Child(phases, "Fade alarm calls");
        fade.AddComponent<FadeAlarmCalls>();
        fade.transform.SetSiblingIndex(act.GetSiblingIndex() + 1);

        // The prey: the action with its gene, last of the actions, and the sense, last of the senses.
        var alarm = Child(actions, "alarm");
        alarm.AddComponent<AlarmCallAction>();
        alarm.AddComponent<TextGene>().Configure(Founders, pro: "Always give an alarm call.", anti: "Never give an alarm call.");
        Child(senses, "Alarm").AddComponent<AlarmSense>();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) return "could not save " + ScenePath;
        Debug.Log("AlarmCallsScene: wrote " + ScenePath);
        return null;
    }

    /// <summary>
    /// Runs the example scene headless at L0 (random brain, no mutation, no files), and counts what the alarm calls did.
    /// </summary>
    /// <param name="seed">The world seed.</param>
    /// <param name="ticks">The run length in ticks.</param>
    /// <returns>A summary line: ticks, prey decisions, how many chose to call, calls made, decisions that heard a call and
    /// what those chose, and the events hash; or a line starting with <c>FAILED:</c>.</returns>
    public static string RunHeadless(int seed, int ticks)
    {
        return Batch.WithScene(ScenePath, w =>
        {
            w.Seed = seed;
            w.TickLimit = ticks;
            w.WaitMode = WaitMode.Freeze;
            w.StartOnPlay = false;
            string problem = RealTimeRuns.SetLevel(w, "L0");
            if (problem != null) return "FAILED: " + problem;
            foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
            if (!w.Initialize()) return "FAILED: validation: " + w.LastReport;

            var prey = w.FindSpecies("prey");
            int alarm = prey.FindAction("alarm")?.Index ?? -2;            // -2: no such action, never chosen
            int flee = prey.FindAction("flee")?.Index ?? -2;
            int hide = prey.FindAction("hide")?.Index ?? -2;
            int sense = prey.Senses.ToList().FindIndex(s => s is AlarmSense);
            int decisions = 0, heard = 0, heardFledOrHid = 0;
            while (w.State != RunState.Stopped)
            {
                w.Advance(1);
                foreach (var a in prey.Animals)
                {
                    if (a.LastObservation == null || a.LastDecisionTick != w.Tick - 1) continue;   // decided in the tick just run
                    decisions++;
                    if (a.LastObservation[sense] == 0) continue;
                    heard++;
                    if (a.Action == flee || a.Action == hide) heardFledOrHid++;            // the carrier, if it also called
                }
            }
            var calls = w.Service<AlarmCalls>();
            // The call is an overlay: it acts on the tick of its decision and has ended by now; the counters kept it.
            int chose = alarm >= 0 ? prey.Counters.ActionCounts[alarm] : 0;
            return $"AlarmCalls L0 seed {seed}: {w.Tick} ticks, {decisions} prey decisions, {chose} chose alarm, {calls.CallsMade} calls made, " +
                   $"{heard} decisions heard a call ({heardFledOrHid} of them fled or hid), hash {w.Events.Hash}";
        });
    }

    /// <summary><c>EvoSim ▸ Student ▸ Run Alarm Calls Headless (L0)</c>: 400 ticks, seed 1234, twice, and logs both lines.</summary>
    [MenuItem("EvoSim/Student/Run Alarm Calls Headless (L0)")]
    static void RunFromMenu()
    {
        Debug.Log(RunHeadless(1234, 400));
        Debug.Log(RunHeadless(1234, 400));
    }

    /// <summary>Command-line entry point: builds the scene, then exits with 0, or 1 when it failed.</summary>
    public static void BuildFromCommandLine()
    {
        int code = 1;
        try
        {
            string problem = Build();
            if (problem != null) Debug.LogError("AlarmCallsScene: " + problem);
            else code = 0;
        }
        catch (Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    /// <summary>
    /// Command-line entry point: runs the scene headless twice with the same seed (<c>-seed</c>, default 1234; <c>-ticks</c>,
    /// default 400), logs both lines and whether the hashes match, then exits with 0, or 1 when a run failed or the hashes
    /// differ.
    /// </summary>
    public static void RunFromCommandLine()
    {
        int code = 1;
        try
        {
            int seed = int.Parse(Arg("-seed") ?? "1234");
            int ticks = int.Parse(Arg("-ticks") ?? "400");
            string first = RunHeadless(seed, ticks), second = RunHeadless(seed, ticks);
            Debug.Log("AlarmCallsScene run 1: " + first);
            Debug.Log("AlarmCallsScene run 2: " + second);
            bool same = !first.StartsWith("FAILED") && first == second;
            Debug.Log("AlarmCallsScene: " + (same ? "SAME HASH" : "DIFFERENT RUNS"));
            if (same) code = 0;
        }
        catch (Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    /// <summary>The value after a command-line flag.</summary>
    /// <param name="flag">The flag, such as <c>-seed</c>.</param>
    /// <returns>The next argument, or null when the flag is absent or last.</returns>
    static string Arg(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Makes an empty GameObject under a parent, to hold one module.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="name">The new object's name: an action's name, a sense's label.</param>
    /// <returns>The new GameObject.</returns>
    static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
