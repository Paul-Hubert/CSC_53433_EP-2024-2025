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
    public static class RunInfo
    {
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

        /// <summary>The command line, with keys and the values of options naming one masked (OUT-04).</summary>
        /// <summary>The NULL control (15 §1): every species is decided by the random brain, whatever the default.</summary>
        static bool NullBrain(World w)
        {
            if (w.AllSpecies.Count == 0) return w.DefaultBrain != null && w.DefaultBrain.Id == "random";
            foreach (var s in w.AllSpecies)
                if (s.Brain == null || s.Brain.Id != "random") return false;
            return true;
        }

        public static string MaskedCommandLine() => Secrets.Mask(Environment.GetCommandLineArgs());

        /// <summary>The commit checked out in the project's repository, read from .git (no process started).</summary>
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
