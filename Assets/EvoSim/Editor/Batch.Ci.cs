using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EvoSim
{
    // Batch, part two: the CI entry points of 32 §4. RunRegression (the T3 tests: pinned hashes, equivalences, scene
    // checks), RunScenarios -tier T2|T3|T4 (every scenario variant of that tier, each seed twice: same hash), RunIntegrity
    // (the brain and mutator checks). Each writes a report under Logs/EvoSim/Reports and exits non-zero on failure.
    public static partial class Batch
    {
        /// <summary>-executeMethod EvoSim.Batch.RunRegression: runs the EditMode tests of category T3 and exits with their result.</summary>
        /// <remarks>
        /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunRegression</c>. Each failed test
        /// is logged as an error; the totals go to the log and to <c>Logs/EvoSim/Reports/regression-&lt;yyyyMMdd-HHmmss&gt;.txt</c>.
        /// Exit code 1 when a test failed, else 0. In the open editor it runs the tests without exiting.
        /// </remarks>
        public static void RunRegression()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RegressionCallbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, categoryNames = new[] { "T3" } }) { runSynchronously = true });
        }

        /// <summary>Listens to the regression test run: logs each failed test, then writes the report and exits.</summary>
        sealed class RegressionCallbacks : ICallbacks
        {
            /// <summary>Does nothing: nothing is needed before the run.</summary>
            public void RunStarted(ITestAdaptor testsToRun) { }
            /// <summary>Does nothing: only results are reported.</summary>
            public void TestStarted(ITestAdaptor test) { }
            /// <summary>Logs a failed test (a leaf, not a fixture) with its full name and message.</summary>
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"EvoSim regression FAILED {result.FullName}: {result.Message}");
            }
            /// <summary>Logs and writes the totals; in batch mode, exits with 1 when a test failed.</summary>
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
        /// <remarks>
        /// <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunScenarios -tier T2 [-ticks n]</c>
        /// (the tier defaults to T2). The result lines go to the log and to
        /// <c>Logs/EvoSim/Reports/scenarios-&lt;tier&gt;-&lt;yyyyMMdd-HHmmss&gt;.txt</c>; each run also writes its run folder under
        /// <c>Logs/EvoSim/runs</c>. Exit code 1 when a run failed, else 0.
        /// </remarks>
        public static void RunScenarios()
        {
            string tier = Arg("-tier") ?? "T2";
            int? ticks = Arg("-ticks") != null ? int.Parse(Arg("-ticks")) : (int?)null;
            string summary = RunTier(tier, ticks, out bool failed);
            Debug.Log(summary);
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }

        /// <summary>The same from the open editor: one line per run, and a report file.</summary>
        /// <param name="tier">The tier to run: <c>T2</c> (fast), <c>T3</c> (regression) or <c>T4</c> (real models).</param>
        /// <param name="ticks">The run length in ticks, or null for each scenario's.</param>
        /// <param name="failed">Set to true when a run failed or its two hashes differed.</param>
        /// <returns>One line per seed, <c>PASS …</c> or <c>FAIL … | again: …</c>, or a line saying the tier has no variant.</returns>
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
        /// <remarks>The same as <c>-executeMethod EvoSim.Integrity.RunIntegrity</c>, with the same options: see
        /// <see cref="Integrity.RunIntegrity"/>.</remarks>
        public static void RunIntegrity() => Integrity.RunIntegrity();

        /// <summary>The event hash in a run's summary line (up to 16 characters), or empty when the line has none.</summary>
        static string HashIn(string line)
        {
            int i = line.IndexOf("hash ", StringComparison.Ordinal);
            return i < 0 ? "" : line.Substring(i + 5, Math.Min(16, line.Length - i - 5));
        }

        /// <summary>
        /// Writes a text report <c>&lt;name&gt;-&lt;yyyyMMdd-HHmmss&gt;.txt</c> under <see cref="IntegrityReport.Folder"/>.
        /// </summary>
        static void WriteReport(string name, string text)
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), IntegrityReport.Folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.txt"), text + "\n", new UTF8Encoding(false));
        }
    }
}
