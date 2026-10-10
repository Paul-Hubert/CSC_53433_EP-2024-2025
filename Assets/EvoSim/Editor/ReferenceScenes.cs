using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Editor
{
    /// <summary>
    /// Builds the reference worlds of 20 §2 as scenes (Lab1_Full, Lab1_Small) and the allele pools of 09 §3 as assets,
    /// from the prototype's pool files copied to Data/Pools. The scenes are then the configuration (ARCH-01): edit them,
    /// not this builder, unless the reference itself changes.
    /// </summary>
    public static class ReferenceScenes
    {
        public const string Root = "Assets/EvoSim";
        public const string ScenesFolder = Root + "/Scenes";
        public const string PoolsFolder = Root + "/Data/AllelePools";
        const string Deck = Root + "/Data/Decks/mutate_v4.txt";
        const string StaminaWordsLow = "I am out of breath.", StaminaWordsMedium = "I am getting tired.", StaminaWordsHigh = "I am rested.";

        // 08 §7 prompt texts (line breaks as in the prototype's prompts, which JEV was trained on).
        const string PreyLocomotionRule = "Animals move {walkSpeed} meter per step. Predators run {predator.Locomotion.runSpeed} meters per step when they hunt,\n" +
                                          "but they have {predator.Stamina.max} stamina against an animal's {Stamina.max}, so a long chase tires them first.";
        const string PredatorLocomotionRule = "Predators run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                                              "Prey animals move {prey.Locomotion.walkSpeed} meter per step but have {prey.Stamina.max} stamina against a predator's {Stamina.max}.";
        const string PredatorSearchRule = "If the chosen action has nothing to act on in sight (no prey, carcass, other\n" +
                                          "predator or ready partner), the predator searches the surroundings instead.";
        const string PredatorStaminaRule = "Every meter moved costs stamina. Standing still brings it back, which costs some\n" +
                                           "energy until stamina is full. Without stamina a predator cannot move.";
        const string PredatorHeader = "You decide what a predator does next in a simple world.";
        const string PredatorGenesIntro = "This predator's instincts (its genes). They define its personality: follow them\n" +
                                          "even when they seem unwise. Instincts that are meaningless have no effect.";

        [MenuItem("EvoSim/Build Reference Scenes")]
        public static void BuildAll()
        {
            var pools = BuildPools();
            Build("Lab1_Small", 48f, 24, 40, 10, 4, 6, 3, pools);
            Build("Lab1_Full", 192f, 136, 270, 10, 28, 68, 3, pools);
            BuildScenarios();
            AssetDatabase.SaveAssets();
            Debug.Log("EvoSim: reference scenes built in " + ScenesFolder);
        }

        public const string ScenariosFolder = Root + "/Scenarios";
        const string EmptyScene = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";

        /// <summary>The scenario assets whose scenes exist (31 §2); more come with the samples (M11).</summary>
        public static void BuildScenarios()
        {
            Directory.CreateDirectory(ScenariosFolder);
            Scenario("S03_Lab1Small", "S03 Lab 1 small", ScenesFolder + "/Lab1_Small.unity", 1000, "Random", "T2",
                     "invariants", "deterministic");
            Scenario("S02_Lab1Reference", "S02 Lab 1 reference", ScenesFolder + "/Lab1_Full.unity", 2000, "Random", "T4",
                     "invariants", "deterministic", "no newcomer in 10 000 ticks (R-03)");
        }

        static ScenarioAsset Scenario(string file, string title, string scene, int ticks, string brain, string tier, params string[] expect)
        {
            string path = ScenariosFolder + "/" + file + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<ScenarioAsset>();
                AssetDatabase.CreateAsset(s, path);
            }
            s.title = title;
            s.scene = scene;
            s.ticks = ticks;
            s.brain = brain;
            s.tier = tier;
            s.seeds = new List<int> { 1234, 7, 42 };
            s.expect = new List<string>(expect);
            EditorUtility.SetDirty(s);
            return s;
        }

        /// <summary>The allele pools of 09 §3, one asset per locus, from Data/Pools (the prototype's v2 pools) plus the Unity hide draft.</summary>
        public static Dictionary<string, AllelePool> BuildPools()
        {
            Directory.CreateDirectory(PoolsFolder);
            var pools = new Dictionary<string, AllelePool>();
            void FromFiles(string species, string founderFile, string contrastFile)
            {
                var founders = JObject.Parse(File.ReadAllText(Root + "/Data/Pools/" + founderFile))["loci"];
                var contrasts = JObject.Parse(File.ReadAllText(Root + "/Data/Pools/" + contrastFile));
                foreach (var locus in ((JObject)founders).Properties())
                {
                    var list = new List<string>();
                    foreach (var a in locus.Value["alleles"]) list.Add((string)a);
                    var c = contrasts[locus.Name];
                    pools[species + "." + locus.Name] = Pool(species + "_" + locus.Name, list, c != null ? (string)c["pro"] : "", c != null ? (string)c["anti"] : "");
                }
            }
            FromFiles("prey", "founder_pool_v2.json", "contrast_alleles_v2.json");
            FromFiles("predator", "predator_founder_pool_v2.json", "predator_contrast_alleles_v2.json");
            pools["prey.hide"] = Pool("prey_hide", new List<string>
            {
                "Hide when a predator is close.", "Stay in cover when danger is near.", "Hide only when you are tired.", "Leave cover to find food when hungry.",
            }, "Always hide, whatever happens.", "Never hide in cover.");                            // 09 §3, Unity draft (40 §2 #1)
            return pools;
        }

        static AllelePool Pool(string name, List<string> founders, string pro, string anti)
        {
            string path = PoolsFolder + "/" + name + ".asset";
            var pool = AssetDatabase.LoadAssetAtPath<AllelePool>(path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<AllelePool>();
                AssetDatabase.CreateAsset(pool, path);
            }
            pool.founders = founders;
            pool.contrastPro = pro;
            pool.contrastAnti = anti;
            EditorUtility.SetDirty(pool);
            return pool;
        }

        /// <summary>The control sentences of GENE-23: 20 shuffled-word and 20 irrelevant ones (Data/Pools/control_alleles_v1.json).</summary>
        public static List<string> ControlSentences()
        {
            var o = JObject.Parse(File.ReadAllText(Root + "/Data/Pools/control_alleles_v1.json"));
            var list = new List<string>();
            foreach (var s in o["shuffled"]) list.Add((string)s);
            foreach (var s in o["irrelevant"]) list.Add((string)s);
            return list;
        }

        /// <summary>One Lab 1 scene: the tree of 20 §2 with the reference values of 14 §2.</summary>
        public static string Build(string sceneName, float size, int prey, int preyCap, int preyFloor, int predators, int predatorCap, int predatorFloor,
                                   Dictionary<string, AllelePool> pools)
        {
            // Opened additively from an empty file, so the editor's own open scene is never touched.
            Directory.CreateDirectory(ScenesFolder);
            string path = ScenesFolder + "/" + sceneName + ".unity";
            File.WriteAllText(path, EmptyScene);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cameraGo.transform.position = new Vector3(size / 2f, size * 0.9f, -size * 0.1f);
            cameraGo.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var worldGo = new GameObject("Lab1 World");
            worldGo.SetActive(false);
            var world = worldGo.AddComponent<World>();
            world.SetControlSentences(ControlSentences());

            var ground = Child(worldGo, "Ground").AddComponent<FlatGround>();
            ground.SetSize(size, size);

            var env = Child(worldGo, "Environment");
            var grass = Child(env, "Grass");
            grass.AddComponent<FoodGrid>().Configure(0.1f, 0.0015f, true);
            grass.AddComponent<Edible>().Configure(EatMethod.Graze, 25f);
            Child(env, "Thickets").AddComponent<CoverLayer>().SetFraction(0.2f);
            Child(env, "Carcasses").AddComponent<CarcassSystem>();
            Child(env, "Eggs").AddComponent<EggSystem>();

            var brains = Child(worldGo, "Brains");
            var random = Child(brains, "Random").AddComponent<RandomBrain>();
            Child(brains, "Answer cache").AddComponent<AnswerCache>();
            Child(brains, "Mutator").AddComponent<MutatorService>().Configure("qwen3.5:0.8b", 1.2f);
            world.DefaultBrain = random;

            var recording = Child(worldGo, "Recording");
            recording.AddComponent<RunRecorder>().Configure("Logs/EvoSim/runs", "", false, true);
            recording.AddComponent<LiveStatistics>();

            var phases = Child(worldGo, "Phases");
            Child(phases, "Sense").AddComponent<SensePhase>();
            Child(phases, "Ask brains").AddComponent<AskBrainsPhase>();
            Child(phases, "Choose actions").AddComponent<ChooseActionsPhase>();
            Child(phases, "Act").AddComponent<ActPhase>();
            Child(phases, "Breed").AddComponent<BreedPhase>();
            Child(phases, "Hatch").AddComponent<HatchPhase>();
            Child(phases, "Deaths").AddComponent<DeathPhase>();
            Child(phases, "Migration").AddComponent<MigrationPhase>();
            Child(phases, "Environment").AddComponent<EnvironmentPhase>();
            Child(phases, "Floor").AddComponent<FloorPhase>();
            Child(phases, "Record").AddComponent<RecordPhase>();

            var deck = AssetDatabase.LoadAssetAtPath<TextAsset>(Deck);
            BuildPrey(worldGo, prey, preyCap, preyFloor, pools, deck);
            BuildPredator(worldGo, predators, predatorCap, predatorFloor, pools, deck);

            worldGo.SetActive(true);
            EditorSceneManager.SaveScene(scene, path);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            return path;
        }

        static void BuildPrey(GameObject world, int population, int cap, int floor, Dictionary<string, AllelePool> pools, TextAsset deck)
        {
            var go = Child(world, "Prey");
            var s = go.AddComponent<Species>();
            s.SetNames("prey", "prey");
            s.SetInitialPopulation(population);
            go.AddComponent<SearchRule>();
            go.AddComponent<Edible>().Configure(EatMethod.Strike, 60f, 2, 30f, 100);

            var loco = Child(go, "Locomotion").AddComponent<KinematicLocomotion>();
            loco.SetSpeeds(1f, 1f);
            loco.SetPromptRule(PreyLocomotionRule);
            var stats = Child(go, "Stats");
            stats.AddComponent<Energy>().Configure(100f, 60f);
            stats.AddComponent<Stamina>().Configure(60f, 2f, 0.3f);
            stats.AddComponent<Metabolism>().Configure(0.7f, 0.5f, 0.2f);

            var senses = Child(go, "Senses");
            Child(senses, "Energy").AddComponent<LevelSense>().Configure("energy", 30f, 70f);
            Child(senses, "Stamina").AddComponent<LevelSense>().Configure("stamina", 20f, 40f, new[] { StaminaWordsLow, StaminaWordsMedium, StaminaWordsHigh });
            Child(senses, "Food").AddComponent<NearestResourceSense>();
            Child(senses, "Predator").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Threats);
            Child(senses, "Cover").AddComponent<NearestCoverSense>();
            Child(senses, "Animal").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Kin, readiness: true, partner: 20f);
            Child(senses, "Age").AddComponent<AgeSense>();

            var actions = Child(go, "Actions");
            Action<EatAction>(actions, "eat", pools["prey.eat"]);
            Action<FleeAction>(actions, "flee", pools["prey.flee"]);
            Action<HideAction>(actions, "hide", pools["prey.hide"]);
            Action<FollowAction>(actions, "follow", pools["prey.follow"]);
            Action<RestAction>(actions, "rest", pools["prey.rest"]);
            Action<MateAction>(actions, "mate", pools["prey.mate"]);

            Child(go, "Food").AddComponent<Diet>().Set("Grass");
            Life(go, cap, floor);
            Child(go, "Mutation").AddComponent<LlmMutation>().Configure(deck, "The sentence below is a rule that a wild animal follows.");
        }

        static void BuildPredator(GameObject world, int population, int cap, int floor, Dictionary<string, AllelePool> pools, TextAsset deck)
        {
            var go = Child(world, "Predator");
            var s = go.AddComponent<Species>();
            s.SetNames("predator", "predator");
            s.SetInitialPopulation(population);
            s.SetPromptHeader(PredatorHeader);
            s.SetGenesIntro(PredatorGenesIntro);
            go.AddComponent<SearchRule>().SetPromptRule(PredatorSearchRule);

            var loco = Child(go, "Locomotion").AddComponent<KinematicLocomotion>();
            loco.SetSpeeds(1f, 2f);
            loco.SetPromptRule(PredatorLocomotionRule);
            var stats = Child(go, "Stats");
            stats.AddComponent<Energy>().Configure(100f, 60f);
            var stamina = stats.AddComponent<Stamina>();
            stamina.Configure(30f, 2f, 0.3f);
            stamina.SetPromptRule(PredatorStaminaRule);
            stats.AddComponent<Metabolism>().Configure(0.7f, 0.5f, 0.2f);
            stats.AddComponent<Digestion>().Configure(50, 60f);

            var senses = Child(go, "Senses");
            Child(senses, "Energy").AddComponent<LevelSense>().Configure("energy", 30f, 70f);
            Child(senses, "Stamina").AddComponent<LevelSense>().Configure("stamina", 10f, 20f, new[] { StaminaWordsLow, StaminaWordsMedium, StaminaWordsHigh });
            Child(senses, "Prey").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Prey);
            Child(senses, "Carcass").AddComponent<NearestEntitySense>();
            var kin = Child(senses, "Other predator").AddComponent<NearestAnimalSense>();
            kin.Configure(AnimalSet.Kin, readiness: true, partner: 20f);
            kin.SetLabel("Other predator");
            Child(senses, "Age").AddComponent<AgeSense>();

            var actions = Child(go, "Actions");
            Action<HuntAction>(actions, "hunt", pools["predator.hunt"]);
            Action<FollowAction>(actions, "follow", pools["predator.follow"]).SetDescription("move toward the nearest other predator");
            Action<RestAction>(actions, "rest", pools["predator.rest"]);
            Action<MateAction>(actions, "mate", pools["predator.mate"]).SetDescription("walk to the nearest ready partner (another predator) in sight and breed with it");

            var diet = Child(go, "Food").AddComponent<Diet>();
            diet.Set("prey", "carcass:prey");
            diet.SetKillChance(0.5f);
            Life(go, cap, floor);
            Child(go, "Mutation").AddComponent<LlmMutation>().Configure(deck, "The sentence below is a rule that a wild animal follows.");
        }

        static void Life(GameObject species, int cap, int floor)
        {
            var life = Child(species, "Life");
            life.AddComponent<MatingRule>().Configure(50f, 150f);
            life.AddComponent<Litter>().Configure(2, 4, 40f);
            life.AddComponent<UniformCrossover>();
            life.AddComponent<Incubation>().Ticks = 0;
            life.AddComponent<Starvation>();
            life.AddComponent<OldAge>().SetMaxAge(1500);
            life.AddComponent<CapRule>().Configure(cap);
            life.AddComponent<FloorRule>().Floor = floor;
        }

        static T Action<T>(GameObject parent, string name, AllelePool pool) where T : AnimalAction
        {
            var go = Child(parent, name);
            var a = go.AddComponent<T>();
            var gene = go.AddComponent<TextGene>();
            gene.SetPool(pool);
            return a;
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
