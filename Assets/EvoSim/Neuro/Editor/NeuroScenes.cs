using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EvoSim.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Neuro.Editor
{
    /// <summary>
    /// Builds the neural species' content of M16.6 (plan §5, §6.1): the two neural bodies and species prefabs, the scenes
    /// <c>Neuro_Lab</c> (S29), <c>Neuro_JevPrey</c> (S30) and <c>Neuro_JevPredator</c> (S31) on Lab1_Small's map, and the
    /// scenario assets S29–S31 with their asexual, sexual and L0 variants.
    /// </summary>
    /// <remarks>
    /// <para><c>EvoSim ▸ Neuro ▸ Build Neuro Scenes</c> overwrites them. Each scene starts as a copy of
    /// <c>Lab1_Small</c>: the species to replace are matched by <see cref="NeuralTemplates"/>, then removed, and every
    /// placeholder that named them (<c>{predator.Stamina.max}</c>) names their neural match instead. The predator's diet
    /// follows the prey it now hunts. With one side of each pair gone, the scenes have no <see cref="MatchedPairs"/>: the
    /// pairs are checked where both sides live (the 2×2 of M16.7, and T-NEURO-12).</para>
    /// <para>Scenes are opened additively, so the editor's open scene is never touched; a scene open in the editor is
    /// skipped. Like the reference scenes, they are then the configuration (ARCH-01).</para>
    /// </remarks>
    public static class NeuroScenes
    {
        /// <summary>The scene the neural scenes copy: Lab1_Small's map, populations and services (plan §6.1).</summary>
        public const string Source = ReferenceScenes.ScenesFolder + "/Lab1_Small.unity";
        /// <summary>S29: neural prey and neural predators.</summary>
        public const string NeuroLab = ReferenceScenes.ScenesFolder + "/Neuro_Lab.unity";
        /// <summary>S30: JEV prey and neural predators.</summary>
        public const string JevPrey = ReferenceScenes.ScenesFolder + "/Neuro_JevPrey.unity";
        /// <summary>S31: neural prey and JEV predators.</summary>
        public const string JevPredator = ReferenceScenes.ScenesFolder + "/Neuro_JevPredator.unity";
        /// <summary>The scene the comparison scenes copy: Lab1_Full's map, populations and services (plan §6.2, owner).</summary>
        public const string FullSource = ReferenceScenes.ScenesFolder + "/Lab1_Full.unity";
        /// <summary>S32: JEV and neural prey, JEV and neural predators, in one world.</summary>
        public const string Brains2x2 = ReferenceScenes.ScenesFolder + "/Brains_2x2.unity";
        /// <summary>S33: JEV and neural prey, no predators.</summary>
        public const string PreyVsPrey = ReferenceScenes.ScenesFolder + "/Prey_vs_Prey.unity";
        /// <summary>The grass regrowth of the comparison scenarios: twice the reference 0.0015, as two species graze (owner).</summary>
        public const string DoubleRegrowth = "0.003";
        /// <summary>The prompt line of a JEV species' mating rule when every species breeds alone (plan §6.3).</summary>
        public const string AsexualMatingRule = "An adult that chooses mate breeds alone as soon as it is ready.";

        /// <summary>
        /// <c>EvoSim ▸ Neuro ▸ Build Neuro Scenes</c>: the bodies, the three scenes, the species prefabs and the scenarios.
        /// </summary>
        /// <returns>One line per scene: built, or why it was skipped.</returns>
        [MenuItem("EvoSim/Neuro/Build Neuro Scenes")]
        public static string BuildAll()
        {
            Directory.CreateDirectory(NeuralSpeciesMenu.ModulesFolder);
            var preyBody = BodyPrefab("NeuroPreyBody", "PreyBody", new Color(0.3f, 0.75f, 0.85f));
            var predatorBody = BodyPrefab("NeuroPredatorBody", "PredatorBody", new Color(0.85f, 0.35f, 0.75f));
            var lines = new List<string>
            {
                Scene(NeuroLab, true, true, preyBody, predatorBody, true),
                Scene(JevPrey, false, true, preyBody, predatorBody, false),
                Scene(JevPredator, true, false, preyBody, predatorBody, false),
            };
            lines.Add(ComparisonScene(Brains2x2, true, preyBody, predatorBody));
            lines.Add(ComparisonScene(PreyVsPrey, false, preyBody, predatorBody));
            BuildScenarios();
            AssetDatabase.SaveAssets();
            string result = string.Join("\n", lines);
            Debug.Log("EvoSim: neural content built\n" + result);
            return result;
        }

        /// <summary>
        /// <c>EvoSim ▸ Neuro ▸ Build Comparison Scenes</c>: only <c>Brains_2x2</c>, <c>Prey_vs_Prey</c> and the scenarios,
        /// leaving the bodies and the M16.6 scenes as they are.
        /// </summary>
        /// <returns>One line per scene.</returns>
        [MenuItem("EvoSim/Neuro/Build Comparison Scenes")]
        public static string BuildComparison()
        {
            var preyBody = NeuralSpeciesMenu.Body("NeuroPreyBody");
            var predatorBody = NeuralSpeciesMenu.Body("NeuroPredatorBody");
            var lines = new List<string>
            {
                ComparisonScene(Brains2x2, true, preyBody, predatorBody),
                ComparisonScene(PreyVsPrey, false, preyBody, predatorBody),
            };
            BuildScenarios();
            AssetDatabase.SaveAssets();
            string result = string.Join("\n", lines);
            Debug.Log("EvoSim: comparison content built\n" + result);
            return result;
        }

        /// <summary>
        /// A comparison scene (plan §6.2): a copy of Lab1_Full with the neural prey (and, with predators, the neural
        /// predator) beside the JEV species, both predators striking and scavenging both preys, the matched pairs,
        /// <see cref="SurvivalStats"/>, and <see cref="PlateauStop"/> after the Record phase. Without predators (S33) both
        /// predators are removed, and the placeholders that named the JEV predator are replaced by their values, so the
        /// JEV prey's prompt reads exactly as in Lab1_Full (its answer cache stays valid).
        /// </summary>
        static string ComparisonScene(string path, bool predators, GameObject preyBody, GameObject predatorBody)
        {
            if (SceneManager.GetSceneByPath(path).isLoaded) return path + ": open in the editor, skipped";
            File.Copy(FullSource, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                World world = null;
                foreach (var root in scene.GetRootGameObjects())
                    if ((world = root.GetComponentInChildren<World>(true)) != null) break;
                world.gameObject.name = predators ? "Brains 2x2 World" : "Prey vs Prey World";
                var prey = Find(world, "prey");
                var predator = Find(world, "predator");
                var nnPrey = NeuralTemplates.Prey(prey, body: preyBody);
                if (predators)
                {
                    var nnPredator = NeuralTemplates.Predator(predator, body: predatorBody);
                    NeuralTemplates.Retarget(predator.GetComponentInChildren<Diet>(true), prey.RequestedId, nnPrey.RequestedId, keep: true);
                    NeuralTemplates.Retarget(nnPredator.GetComponentInChildren<Diet>(true), prey.RequestedId, nnPrey.RequestedId, keep: true);
                }
                else
                {
                    Freeze(world, predator.DisplayName);
                    Object.DestroyImmediate(predator.gameObject);
                }
                NeuralTemplates.PairsOf(world).gameObject.AddComponent<SurvivalStats>();
                var plateau = new GameObject("Plateau stop");
                plateau.transform.SetParent(world.transform.Find("Phases"), false);
                plateau.AddComponent<PlateauStop>();
                EditorSceneManager.SaveScene(scene, path);
                return path + ": built";
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Replaces every placeholder under the World that names a species (<c>{predator.Stamina.max}</c>) by the value it
        /// reads now (<c>30</c>), before that species is removed.
        /// </summary>
        static void Freeze(World world, string speciesName)
        {
            var placeholder = new Regex(@"\{" + Regex.Escape(speciesName) + @"\.([A-Za-z_][\w ]*)\.([A-Za-z_][\w ]*)\}");
            foreach (var c in world.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;
                var so = new SerializedObject(c);
                var p = so.GetIterator();
                bool changed = false;
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.String && placeholder.IsMatch(p.stringValue))
                    {
                        p.stringValue = placeholder.Replace(p.stringValue, m =>
                            FieldPath.Get(world, $"{speciesName}/{m.Groups[1].Value}/{m.Groups[2].Value}") ?? m.Value);
                        changed = true;
                    }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// One scene: a copy of Lab1_Small whose prey, predator or both are replaced by their neural match; with
        /// <paramref name="savePrefabs"/>, the neural species are also saved as the <c>NeuroPrey</c> and
        /// <c>NeuroPredator</c> prefabs.
        /// </summary>
        static string Scene(string path, bool neuralPrey, bool neuralPredator, GameObject preyBody, GameObject predatorBody, bool savePrefabs)
        {
            if (SceneManager.GetSceneByPath(path).isLoaded) return path + ": open in the editor, skipped";
            File.Copy(Source, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                World world = null;
                foreach (var root in scene.GetRootGameObjects())
                    if ((world = root.GetComponentInChildren<World>(true)) != null) break;
                world.gameObject.name = "Neuro World";
                var prey = Find(world, "prey");
                var predator = Find(world, "predator");
                Species nnPrey = null, nnPredator = null;
                if (neuralPrey) nnPrey = NeuralTemplates.Prey(prey, body: preyBody);
                if (neuralPredator) nnPredator = NeuralTemplates.Predator(predator, body: predatorBody);
                if (neuralPrey)
                {
                    NeuralTemplates.Retarget((nnPredator != null ? nnPredator : predator).GetComponentInChildren<Diet>(true), prey.RequestedId, nnPrey.RequestedId);
                    Rename(world, prey.DisplayName, nnPrey.DisplayName);
                    Object.DestroyImmediate(prey.gameObject);
                }
                if (neuralPredator)
                {
                    Rename(world, predator.DisplayName, nnPredator.DisplayName);
                    Object.DestroyImmediate(predator.gameObject);
                }
                var pairs = world.GetComponentInChildren<MatchedPairs>(true);
                if (pairs != null) Object.DestroyImmediate(pairs.gameObject);
                if (savePrefabs)
                {
                    PrefabUtility.SaveAsPrefabAsset(nnPrey.gameObject, NeuralSpeciesMenu.ModulesFolder + "/NeuroPrey.prefab");
                    PrefabUtility.SaveAsPrefabAsset(nnPredator.gameObject, NeuralSpeciesMenu.ModulesFolder + "/NeuroPredator.prefab");
                }
                EditorSceneManager.SaveScene(scene, path);
                return path + ": built";
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>The World's species with that id.</summary>
        static Species Find(World world, string id)
        {
            foreach (var s in world.GetComponentsInChildren<Species>(true)) if (s.RequestedId == id) return s;
            throw new System.InvalidOperationException($"{Source} has no species '{id}'");
        }

        /// <summary>
        /// Makes every prompt placeholder under the World that names a species (<c>{prey.…}</c>) name another one
        /// (<c>{neural prey.…}</c>): every text field of every component, through serialized properties.
        /// </summary>
        static void Rename(World world, string from, string to)
        {
            foreach (var c in world.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;
                var so = new SerializedObject(c);
                var p = so.GetIterator();
                bool changed = false;
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.String && p.stringValue.Contains("{" + from + "."))
                    {
                        p.stringValue = p.stringValue.Replace("{" + from + ".", "{" + to + ".");
                        changed = true;
                    }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// A neural body: a copy of a reference body (<c>PreyBody</c>, <c>PredatorBody</c>) with its own colour and a
        /// <see cref="RayFanView"/> instead of its <see cref="ActionView"/>, so the fan shows in the Scene and Game views.
        /// </summary>
        static GameObject BodyPrefab(string file, string from, Color color)
        {
            string path = NeuralSpeciesMenu.ModulesFolder + "/" + file + ".prefab";
            AssetDatabase.CopyAsset(ReferenceScenes.BodiesFolder + "/" + from + ".prefab", path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var old = root.GetComponent<ActionView>();
                var so = new SerializedObject(old);
                var body = (Renderer)so.FindProperty("body").objectReferenceValue;
                var marker = (Renderer)so.FindProperty("marker").objectReferenceValue;
                Object.DestroyImmediate(old);
                root.AddComponent<RayFanView>().Configure(body, marker);
                body.sharedMaterial = Material(NeuralSpeciesMenu.ModulesFolder + "/" + file + ".mat", body.sharedMaterial, color);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>A copy of a material in a colour, at a path (replaced when it exists).</summary>
        static Material Material(string path, Material source, Color color)
        {
            var m = new Material(source) { color = color };
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>
        /// S29 NeuroLab (T2: asexual, sexual, and the no-mutation control), S30 JevPreyNeuroPredator and S31
        /// NeuroPreyJevPredator (L0 asexual and sexual on the random brain in T2, and JEV asexual and sexual in T4), plan §6.1.
        /// </summary>
        /// <remarks>
        /// The L0 variants switch the JEV species' LLM mutation off (its <c>Mutation</c> object), so they make no model call;
        /// the neural species still mutate. The asexual JEV variants change the JEV mating rule's prompt line to match
        /// the world (plan §6.3).
        /// </remarks>
        public static void BuildScenarios()
        {
            var seeds = new List<int> { 1234, 7, 42 };
            Scenario("S29_NeuroLab", "S29 Neural prey and neural predators", NeuroLab, 5000, "T2", seeds,
                new[] { "invariants", "deterministic", "evolving neural prey eat more per animal-tick than in no-mutation (nn_meals_per_tick)" },
                Variant("asexual", "", "T2", null, ("World/asexual", "true")),
                Variant("sexual", "", "T2", null),
                Variant("no-mutation", "", "T2", null, ("World/asexual", "true"), ("World/noMutation", "true")));
            Scenario("S30_JevPreyNeuroPredator", "S30 JEV prey and neural predators", JevPrey, 2000, "T4", seeds,
                new[] { "invariants", "deterministic" },
                Variant("L0-asexual", "Random", "T2", "Prey/Mutation", ("World/asexual", "true")),
                Variant("L0-sexual", "Random", "T2", "Prey/Mutation"),
                Variant("jev-asexual", "JEV", "T4", null, ("World/asexual", "true"), ("Prey/MatingRule/promptRule", AsexualMatingRule)),
                Variant("jev-sexual", "JEV", "T4", null));
            Scenario("S31_NeuroPreyJevPredator", "S31 Neural prey and JEV predators", JevPredator, 2000, "T4", seeds,
                new[] { "invariants", "deterministic" },
                Variant("L0-asexual", "Random", "T2", "Predator/Mutation", ("World/asexual", "true")),
                Variant("L0-sexual", "Random", "T2", "Predator/Mutation"),
                Variant("jev-asexual", "JEV", "T4", null, ("World/asexual", "true"), ("Predator/MatingRule/promptRule", AsexualMatingRule)),
                Variant("jev-sexual", "JEV", "T4", null));
            BuildComparisonScenarios();
        }

        /// <summary>
        /// S32 Brains2x2 and S33 PreyVsPrey (plan §6.2, §6.3): seeds 1 and 2, asexual with the JEV mating prompt override,
        /// grass regrowth x2, a 60 000-tick cap (the plateau rule stops earlier); arms <c>default</c> (neural species decide
        /// every tick) and <c>matched</c> (every 4 ticks, staggered, like JEV), each in T4 with JEV and in T2 as L0 (the
        /// random brain, the JEV species' LLM mutation off).
        /// </summary>
        public static void BuildComparisonScenarios()
        {
            var seeds = new List<int> { 1, 2 };
            var expect = new[] { "invariants", "deterministic", "survival.json written", "stops at the plateau or the cap" };
            var matched = new[] { ("Neuro Prey/DecisionSchedule/period", "4"), ("Neuro Prey/DecisionSchedule/staggered", "true"),
                                  ("Neuro Predator/DecisionSchedule/period", "4"), ("Neuro Predator/DecisionSchedule/staggered", "true") };
            var matchedPrey = new[] { matched[0], matched[1] };
            Scenario("S32_Brains2x2", "S32 Brains 2x2: JEV and neural prey, JEV and neural predators", Brains2x2, 60000, "T4", seeds, expect,
                new[] { ("World/asexual", "true"), ("Environment/Grass/FoodGrid/regrowP", DoubleRegrowth),
                        ("Prey/MatingRule/promptRule", AsexualMatingRule), ("Predator/MatingRule/promptRule", AsexualMatingRule) },
                Variant("default", "JEV", "T4", null),
                Variant("matched", "JEV", "T4", null, matched),
                Removing("L0-default", "Random", "T2", new[] { "Prey/Mutation", "Predator/Mutation" }),
                Removing("L0-matched", "Random", "T2", new[] { "Prey/Mutation", "Predator/Mutation" }, matched));
            Scenario("S33_PreyVsPrey", "S33 Prey vs prey: JEV and neural prey, no predators", PreyVsPrey, 60000, "T4", seeds, expect,
                new[] { ("World/asexual", "true"), ("Environment/Grass/FoodGrid/regrowP", DoubleRegrowth),
                        ("Prey/MatingRule/promptRule", AsexualMatingRule) },
                Variant("default", "JEV", "T4", null),
                Variant("matched", "JEV", "T4", null, matchedPrey),
                Removing("L0-default", "Random", "T2", new[] { "Prey/Mutation" }),
                Removing("L0-matched", "Random", "T2", new[] { "Prey/Mutation" }, matchedPrey));
        }

        /// <summary>A variant: its brain, tier, one GameObject to remove (or null) and its overrides.</summary>
        static ScenarioAsset.Variant Variant(string name, string brain, string tier, string remove, params (string path, string value)[] set) =>
            Removing(name, brain, tier, remove != null ? new[] { remove } : null, set);

        /// <summary>A variant: its brain, tier, the GameObjects to remove (or null) and its overrides.</summary>
        static ScenarioAsset.Variant Removing(string name, string brain, string tier, string[] remove, params (string path, string value)[] set)
        {
            var v = new ScenarioAsset.Variant { name = name, brain = brain, tier = tier };
            foreach (var (path, value) in set) v.set.Add(new ScenarioAsset.Override(path, value));
            if (remove != null) v.remove.AddRange(remove);
            return v;
        }

        /// <summary>Writes one scenario asset to the scenarios folder, replacing any of that name.</summary>
        static void Scenario(string file, string title, string scene, int ticks, string tier, List<int> seeds, string[] expect,
                             params ScenarioAsset.Variant[] variants) =>
            Scenario(file, title, scene, ticks, tier, seeds, expect, new (string, string)[0], variants);

        /// <summary>Writes one scenario asset with overrides for every variant; an existing asset is overwritten in place (same GUID).</summary>
        static void Scenario(string file, string title, string scene, int ticks, string tier, List<int> seeds, string[] expect,
                             (string path, string value)[] set, params ScenarioAsset.Variant[] variants)
        {
            var s = ScriptableObject.CreateInstance<ScenarioAsset>();
            s.title = title;
            s.scene = scene;
            s.ticks = ticks;
            s.tier = tier;
            s.brain = "Random";                                                         // the JEV species' brain unless a variant says JEV
            s.seeds = new List<int>(seeds);
            s.expect = new List<string>(expect);
            s.variants = new List<ScenarioAsset.Variant>(variants);
            foreach (var (path, value) in set) s.set.Add(new ScenarioAsset.Override(path, value));
            string asset = ReferenceScenes.ScenariosFolder + "/" + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(asset);
            if (existing != null)
            {
                s.name = existing.name;
                EditorUtility.CopySerialized(s, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(s);
            }
            else AssetDatabase.CreateAsset(s, asset);
        }
    }
}
