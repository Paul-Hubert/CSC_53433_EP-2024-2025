using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A scenario (31 §1, CFG-02): a scene plus overrides by path, variants, seeds, a length, a brain and the expected
    /// facts. The scenario runner window and EvoSim.Batch.RunScenario read it.
    /// </summary>
    [CreateAssetMenu(menuName = "EvoSim/Scenario", fileName = "Scenario")]
    public class ScenarioAsset : ScriptableObject
    {
        [Serializable]
        public class Override
        {
            [Tooltip("<GameObject path>/<Component>/<field>, e.g. Prey/Litter/max or World/decisionPeriod.")]
            public string path = "";
            public string value = "";

            public Override() { }
            public Override(string path, string value) { this.path = path; this.value = value; }
        }

        [Serializable]
        public class Variant
        {
            public string name = "default";
            [Tooltip("This variant's brain (GameObject name or brain id); empty = the scenario's.")]
            public string brain = "";
            [Tooltip("The tier this variant runs in (T2 fast … T4 real models); empty = the scenario's.")]
            public string tier = "";
            public List<Override> set = new List<Override>();
            [Tooltip("GameObject paths under the World to switch off, e.g. prey/Actions/hide.")]
            public List<string> remove = new List<string>();
        }

        [Tooltip("The scenario's id and title, e.g. \"S03 Lab 1 small\".")]
        public string title = "";
        [Tooltip("The scene, relative to the project (Assets/EvoSim/Scenes/Lab1_Small.unity).")]
        public string scene = "";
        [Tooltip("Overrides of every variant.")]
        public List<Override> set = new List<Override>();
        public List<Variant> variants = new List<Variant> { new Variant() };
        public List<int> seeds = new List<int> { 1234 };
        [Min(1), Tooltip("Ticks per run.")]
        public int ticks = 1000;
        [Tooltip("The brain service to use as the world's default (its GameObject's name); empty = the scene's.")]
        public string brain = "";
        [Tooltip("Expected facts: invariants, deterministic, and directional checks (31 §1).")]
        public List<string> expect = new List<string> { "invariants", "deterministic" };
        [Tooltip("T2 (fast), T3 (regression), T4 (real models).")]
        public string tier = "T2";

        public Variant FindVariant(string name)
        {
            foreach (var v in variants) if (v.name == name) return v;
            return variants.Count > 0 ? variants[0] : new Variant();
        }

        /// <summary>
        /// Applies the scenario to a World before it initialises: the seed, the length, the brain, the overrides and the
        /// removals of a variant. Returns what was applied, for run_info.json (CFG-03); errors are listed in problems.
        /// </summary>
        public SortedDictionary<string, object> Apply(World world, string variantName, int seed, List<string> problems)
        {
            var applied = new SortedDictionary<string, object>(StringComparer.Ordinal);
            var v = FindVariant(variantName);
            world.Seed = seed;
            world.TickLimit = ticks;
            string brainName = !string.IsNullOrEmpty(v.brain) ? v.brain : brain;
            if (!string.IsNullOrEmpty(brainName))
            {
                Brain b = null;
                foreach (var candidate in world.GetComponentsInChildren<Brain>(true))
                    if (candidate.gameObject.name == brainName || candidate.Id == brainName) { b = candidate; break; }
                if (b != null && !Ownership.IsEnabled(b)) problems.Add($"the brain {brainName} is disabled (ARCH-06)");
                else if (b != null) { world.DefaultBrain = b; applied["brain"] = brainName; }
                else problems.Add($"no brain named {brainName}");
            }
            foreach (var o in Concat(set, v.set))
            {
                if (FieldPath.Set(world, o.path, o.value, out var error)) applied[o.path] = o.value;
                else problems.Add(error);
            }
            foreach (var path in v.remove)
            {
                if (FieldPath.Remove(world, path, out var error)) applied["remove:" + path] = true;
                else problems.Add(error);
            }
            return applied;
        }

        static IEnumerable<Override> Concat(List<Override> a, List<Override> b)
        {
            foreach (var o in a) yield return o;
            foreach (var o in b) yield return o;
        }
    }
}
