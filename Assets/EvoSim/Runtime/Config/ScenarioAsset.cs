using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A scenario (31 §1, CFG-02): a scene plus overrides by path, variants, seeds, a length, a brain and the expected
    /// facts. The scenario runner window and EvoSim.Batch.RunScenario read it.
    /// </summary>
    /// <remarks>
    /// <para>Create one with <c>Assets ▸ Create ▸ EvoSim ▸ Scenario</c>. A run is one scenario, one variant and one seed:
    /// the runner opens the scene, calls <see cref="Apply"/> on its World before it initialises, and records what was
    /// applied in <c>run_info.json</c> (CFG-03). The scene file itself is never changed.</para>
    /// <para>Paths follow <see cref="FieldPath"/>: <c>Prey/Litter/max</c>, <c>World/decisionPeriod</c>. Values are written
    /// as text, in the invariant culture (<c>0.5</c>, <c>true</c>, an enum name, <c>[8, 4]</c> for a float array).</para>
    /// </remarks>
    [CreateAssetMenu(menuName = "EvoSim/Scenario", fileName = "Scenario")]
    public class ScenarioAsset : ScriptableObject
    {
        /// <summary>One setting to override: a path and its new value, as text.</summary>
        [Serializable]
        public class Override
        {
            /// <summary>The setting's path under the World (see <see cref="FieldPath"/>).</summary>
            [Tooltip("<GameObject path>/<Component>/<field>, e.g. Prey/Litter/max or World/decisionPeriod.")]
            public string path = "";
            /// <summary>The new value as text, e.g. <c>"2"</c>, <c>"0.5"</c>, <c>"true"</c>, <c>"Simultaneous"</c>.</summary>
            public string value = "";

            /// <summary>An empty override (for Unity's serialisation).</summary>
            public Override() { }
            /// <summary>An override of one setting.</summary>
            /// <param name="path">The setting's path under the World.</param>
            /// <param name="value">The new value as text.</param>
            public Override(string path, string value) { this.path = path; this.value = value; }
        }

        /// <summary>A variant of the scenario: its own brain, tier, overrides and removed GameObjects, on top of the scenario's.</summary>
        [Serializable]
        public class Variant
        {
            /// <summary>The variant's name, chosen in the runner or on the command line.</summary>
            public string name = "default";
            /// <summary>This variant's brain (GameObject name or brain id); empty = the scenario's.</summary>
            [Tooltip("This variant's brain (GameObject name or brain id); empty = the scenario's.")]
            public string brain = "";
            /// <summary>The tier this variant runs in (T2 fast … T4 real models); empty = the scenario's.</summary>
            [Tooltip("The tier this variant runs in (T2 fast … T4 real models); empty = the scenario's.")]
            public string tier = "";
            /// <summary>Overrides applied after the scenario's own, so they win on the same path.</summary>
            public List<Override> set = new List<Override>();
            /// <summary>GameObject paths under the World to switch off, e.g. <c>prey/Actions/hide</c> (S06).</summary>
            [Tooltip("GameObject paths under the World to switch off, e.g. prey/Actions/hide.")]
            public List<string> remove = new List<string>();
        }

        /// <summary>The scenario's id and title, e.g. "S03 Lab 1 small".</summary>
        [Tooltip("The scenario's id and title, e.g. \"S03 Lab 1 small\".")]
        public string title = "";
        /// <summary>The scene, relative to the project (Assets/EvoSim/Scenes/Lab1_Small.unity).</summary>
        [Tooltip("The scene, relative to the project (Assets/EvoSim/Scenes/Lab1_Small.unity).")]
        public string scene = "";
        /// <summary>Overrides of every variant, applied first.</summary>
        [Tooltip("Overrides of every variant.")]
        public List<Override> set = new List<Override>();
        /// <summary>The variants; the first is used when a name matches none.</summary>
        public List<Variant> variants = new List<Variant> { new Variant() };
        /// <summary>The seeds to run, one run each (unless one seed is asked for).</summary>
        public List<int> seeds = new List<int> { 1234 };
        /// <summary>Ticks per run: the World's tick limit.</summary>
        [Min(1), Tooltip("Ticks per run.")]
        public int ticks = 1000;
        /// <summary>The brain service to use as the world's default (its GameObject's name or brain id); empty = the scene's.</summary>
        [Tooltip("The brain service to use as the world's default (its GameObject's name); empty = the scene's.")]
        public string brain = "";
        /// <summary>Expected facts: invariants, deterministic, and directional checks (31 §1); shown by the scenario runner.</summary>
        [Tooltip("Expected facts: invariants, deterministic, and directional checks (31 §1).")]
        public List<string> expect = new List<string> { "invariants", "deterministic" };
        /// <summary>The tier the scenario runs in: T2 (fast), T3 (regression), T4 (real models).</summary>
        [Tooltip("T2 (fast), T3 (regression), T4 (real models).")]
        public string tier = "T2";

        /// <summary>A variant by name.</summary>
        /// <param name="name">The variant's name.</param>
        /// <returns>The variant of that name, else the first variant, else a new default variant.</returns>
        public Variant FindVariant(string name)
        {
            foreach (var v in variants) if (v.name == name) return v;
            return variants.Count > 0 ? variants[0] : new Variant();
        }

        /// <summary>
        /// Applies the scenario to a World before it initialises: the seed, the length, the brain, the overrides and the
        /// removals of a variant. Returns what was applied, for run_info.json (CFG-03); errors are listed in problems.
        /// </summary>
        /// <remarks>
        /// Order: seed and tick limit; the default brain (the variant's, else the scenario's), found under the World by
        /// GameObject name or brain id; the scenario's overrides, then the variant's; then the removals. A problem doesn't
        /// stop the rest. Overrides skip the inspector's limits: the World's validation catches values out of range (V-35).
        /// </remarks>
        /// <param name="world">The World of the opened scene, not initialised yet.</param>
        /// <param name="variantName">The variant to apply (see <see cref="FindVariant"/>).</param>
        /// <param name="seed">The run seed.</param>
        /// <param name="problems">Receives one line per override, removal or brain that could not be applied.</param>
        /// <returns>What was applied: <c>brain</c>, each override's path with its value, and <c>remove:&lt;path&gt;</c> for each removal.</returns>
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

        /// <summary>The scenario's overrides, then the variant's.</summary>
        static IEnumerable<Override> Concat(List<Override> a, List<Override> b)
        {
            foreach (var o in a) yield return o;
            foreach (var o in b) yield return o;
        }
    }
}
