using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What ran (13 §2, CFG-03): the scene, the full effective configuration by path, git commit, Unity version,
    /// brains and mutator (models, hosts), seeds, wait mode, act order and controls. Never any secret (OUT-04).
    /// </summary>
    /// <remarks>
    /// The <see cref="RunRecorder"/> writes it to <c>run_info.json</c> when the run starts and again when it stops, with
    /// its own <see cref="RunRecorder.ExtraInfo"/> (the scenario and its overrides) added.
    /// </remarks>
    public static class RunInfo
    {
        /// <summary>Gathers what ran into one sorted dictionary, ready for canonical JSON.</summary>
        /// <remarks>
        /// Keys: <c>world</c>, <c>scene</c>, <c>unity</c>, <c>git</c>, <c>seed</c>, <c>world_seed</c>, <c>wait_mode</c>,
        /// <c>act_order</c>, <c>decision_period</c>, <c>sampling_temperature</c>, <c>text_style</c>, <c>brain</c> (the
        /// default), <c>command</c> (masked), <c>started</c>, <c>controls</c>, <c>brains</c>, <c>mutator</c> (when there is
        /// one), <c>settings</c> (every serialized setting by path, <see cref="FieldPath.Snapshot"/>) and <c>config</c> (the
        /// mutation deck path the prototype's tools read).
        /// </remarks>
        /// <param name="w">The World, initialised.</param>
        /// <returns>The run information, keys in ordinal order.</returns>
        public static SortedDictionary<string, object> Collect(World w)
        {
            var info = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["world"] = w.name,
                ["scene"] = w.gameObject.scene.path,
                ["unity"] = Application.unityVersion,
                ["git"] = GitCommit(),
                ["seed"] = w.Seed,
                ["world_seed"] = w.WorldSeed,
                ["wait_mode"] = w.WaitMode.ToString(),
                ["act_order"] = w.Phase<ActPhase>() != null ? w.Phase<ActPhase>().Order.ToString() : "",
                ["decision_period"] = w.DecisionPeriod,
                ["sampling_temperature"] = w.SamplingTemperature,
                ["text_style"] = w.TextStyle.ToString(),
                ["brain"] = w.DefaultBrain != null ? w.DefaultBrain.Id : "",
                ["command"] = MaskedCommandLine(),
                ["started"] = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture),   // a date, not randomness (RAND-01)
            };
            var controls = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["no_mutation"] = w.NoMutation, ["shuffled"] = w.ShuffledGenes, ["random_founders"] = w.RandomFounders,
                ["asexual"] = w.Asexual, ["null_brain"] = NullBrain(w),
            };
            info["controls"] = controls;                                                          // CTRL-01
            var brains = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var b in w.ServicesOf<Brain>())
                brains[b.Id] = new SortedDictionary<string, object>(StringComparer.Ordinal) { ["model"] = b.ModelIdentity, ["mode"] = b.Mode, ["strict"] = b.Strict };
            info["brains"] = brains;
            var mutator = w.Service<MutatorService>();
            var client = w.Service<MutatorClient>();
            if (mutator != null)
                info["mutator"] = new SortedDictionary<string, object>(StringComparer.Ordinal)
                {
                    ["model"] = mutator.Model, ["temperature"] = mutator.Temperature, ["client"] = client != null ? client.ModelIdentity : "",
                };
            var config = FieldPath.Snapshot(w);
            info["settings"] = config;                                                             // CFG-03
            // The prototype's analysis tools read the deck path here (OUT-05, R-08).
            info["config"] = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["evolution"] = new SortedDictionary<string, object>(StringComparer.Ordinal) { ["mutation_prompts"] = "prompts/mutate_v4.txt" },
            };
            return info;
        }

        /// <summary>The NULL control (15 §1): every species is decided by the random brain, whatever the default.</summary>
        static bool NullBrain(World w)
        {
            if (w.AllSpecies.Count == 0) return w.DefaultBrain != null && w.DefaultBrain.Id == "random";
            foreach (var s in w.AllSpecies)
                if (s.Brain == null || s.Brain.Id != "random") return false;
            return true;
        }

        /// <summary>The command line, with keys and the values of options naming one masked (OUT-04).</summary>
        /// <returns>The process's arguments joined by spaces, secrets replaced (<see cref="Secrets.Mask"/>).</returns>
        public static string MaskedCommandLine() => Secrets.Mask(Environment.GetCommandLineArgs());

        /// <summary>The commit checked out in the project's repository, read from .git (no process started).</summary>
        /// <remarks>Looks for <c>.git</c> in the project folder and up to two folders above it; follows a branch reference,
        /// loose or packed.</remarks>
        /// <returns>The commit hash, the raw HEAD text when the reference can't be resolved, or "" when no repository is found.</returns>
        public static string GitCommit()
        {
            try
            {
                string dir = Path.GetDirectoryName(Application.dataPath);
                for (int up = 0; up < 3 && dir != null; up++, dir = Path.GetDirectoryName(dir))
                {
                    string head = Path.Combine(dir, ".git", "HEAD");
                    if (!File.Exists(head)) continue;
                    string text = File.ReadAllText(head).Trim();
                    if (!text.StartsWith("ref: ")) return text;
                    string refPath = Path.Combine(dir, ".git", text.Substring(5).Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(refPath)) return File.ReadAllText(refPath).Trim();
                    string packed = Path.Combine(dir, ".git", "packed-refs");
                    if (File.Exists(packed))
                        foreach (var line in File.ReadAllLines(packed))
                            if (line.EndsWith(text.Substring(5))) return line.Split(' ')[0];
                    return text;
                }
            }
            catch (Exception) { }
            return "";
        }
    }
}
