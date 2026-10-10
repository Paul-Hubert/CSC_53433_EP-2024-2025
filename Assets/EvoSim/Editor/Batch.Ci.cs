using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The CI entry points of 32 §4: RunRegression (the T3 tests: pinned hashes, equivalences, scene checks),
    /// RunScenarios -tier T2|T3|T4 (every scenario variant of that tier, each seed twice: same hash), RunIntegrity
    /// (the brain and mutator checks). Each writes a report under Logs/EvoSim/Reports and exits non-zero on failure.
    /// </summary>
    public static partial class Batch
    {
        /// <summary>-executeMethod EvoSim.Batch.RunRegression: runs the EditMode tests of category T3 and exits with their result.</summary>
        public static void RunRegression()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RegressionCallbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, categoryNames = new[] { "T3" } }) { runSynchronously = true });
        }

        sealed class RegressionCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"EvoSim regression FAILED {result.FullName}: {result.Message}");
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                string line = $"EvoSim regression: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped";
                Debug.Log(line);
                WriteReport("regression", line);
                if (Application.isBatchMode) EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }
        }

        /// <summary>
        /// -executeMethod EvoSim.Batch.RunScenarios -tier T2: every variant of that tier (the variant's tier, else the
        /// scenario's), each seed run twice; a run fails when it doesn't finish or the two hashes differ (31: exact facts).
        /// </summary>
        public static void RunScenarios()
        {
            string tier = Arg("-tier") ?? "T2";
            int? ticks = Arg("-ticks") != null ? int.Parse(Arg("-ticks")) : (int?)null;
            string summary = RunTier(tier, ticks, out bool failed);
            Debug.Log(summary);
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }

        /// <summary>The same from the open editor: one line per run, and a report file.</summary>
        public static string RunTier(string tier, int? ticks, out bool failed)
        {
            failed = false;
            var sb = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:ScenarioAsset", new[] { ScenariosFolder }))
            {
                var scenario = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (scenario == null) continue;
                foreach (var v in scenario.variants)
                {
                    string t = string.IsNullOrEmpty(v.tier) ? scenario.tier : v.tier;
                    if (t != tier) continue;
                    foreach (int seed in scenario.seeds)
                    {
                        string first = Run(scenario, v.name, seed, ticks);
                        string second = Run(scenario, v.name, seed, ticks);
                        bool ok = !first.StartsWith("FAILED") && HashIn(first) == HashIn(second);
                        failed |= !ok;
                        sb.AppendLine((ok ? "PASS " : "FAIL ") + first + (ok ? "" : " | again: " + second));
                    }
                }
            }
            if (sb.Length == 0) sb.AppendLine($"no scenario variant of tier {tier}");
            WriteReport("scenarios-" + tier, sb.ToString());
            return sb.ToString().TrimEnd();
        }

        /// <summary>-executeMethod EvoSim.Batch.RunIntegrity: the brain and mutator checks (Integrity).</summary>
        public static void RunIntegrity() => Integrity.RunIntegrity();

        static string HashIn(string line)
        {
            int i = line.IndexOf("hash ", StringComparison.Ordinal);
            return i < 0 ? "" : line.Substring(i + 5, Math.Min(16, line.Length - i - 5));
        }

        static void WriteReport(string name, string text)
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), IntegrityReport.Folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.txt"), text + "\n", new UTF8Encoding(false));
        }
    }
}
