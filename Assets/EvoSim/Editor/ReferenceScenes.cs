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
    /// Builds the reference content (20 §2, §7; 09 §3): the allele pools from the prototype's pool files, the module
    /// prefabs (Modules/), placeholder bodies, and the scenes Lab1_Small, Lab1_Full, HideVsFlee, ThreeSpecies,
    /// Terrain_Locomotion and Sandbox. The scenes are then the configuration (ARCH-01): edit them, not this builder,
    /// unless the reference itself changes. Scenes are opened additively from an empty file, so the editor's own open
    /// scene is never touched.
    /// </summary>
    public static class ReferenceScenes
    {
        public const string Root = "Assets/EvoSim";
        public const string ScenesFolder = Root + "/Scenes";
        public const string PoolsFolder = Root + "/Data/AllelePools";
        public const string ModulesFolder = Root + "/Modules";
        public const string BodiesFolder = ModulesFolder + "/Bodies";
        public const string ScenariosFolder = Root + "/Scenarios";
        const string Deck = Root + "/Data/Decks/mutate_v4.txt";
        const string ModelsFolder = Root + "/Data/Models/JEV-9B";
        const string EmptyScene = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";
        const string ContextLine = "The sentence below is a rule that a wild animal follows.";
        /// <summary>Ticks between two decisions in the reference worlds (DEC-01; owner 2026-10-11, 5 × the former 4).</summary>
        public const int ReferenceDecisionPeriod = 20;
        static readonly string[] StaminaWords = { "I am out of breath.", "I am getting tired.", "I am rested." };

        // 08 §7 prompt texts (line breaks as in the prototype's prompts, which JEV was trained on).
        const string PreyLocomotionRule = "Animals move {walkSpeed} meter per step. Predators run {predator.Locomotion.runSpeed} meters per step when they hunt,\n" +
                                          "but they have {predator.Stamina.max} stamina against an animal's {Stamina.max}, so a long chase tires them first.";
        const string PredatorLocomotionRule = "Predators run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                                              "Prey animals move {prey.Locomotion.walkSpeed} meter per step but have {prey.Stamina.max} stamina against a predator's {Stamina.max}.";

        /// <summary>The module prefabs and bodies made by BuildModules, by name.</summary>
        sealed class Kit
        {
            public Dictionary<string, AllelePool> Pools;
            public Dictionary<string, GameObject> Modules = new Dictionary<string, GameObject>();
            public GameObject PreyBody, PredatorBody;
            public TextAsset Deck;
        }

        [MenuItem("EvoSim/Build Reference Scenes")]
        public static void BuildAll()
        {
            var kit = new Kit { Pools = BuildPools(), Deck = AssetDatabase.LoadAssetAtPath<TextAsset>(Deck) };
            var scene = Begin("Lab1_Small", 48f, out string path, out var previous);
            BuildModules(kit);                                             // temporary objects live in this scene, never the owner's
            Lab1(kit, 48f, 24, 40, 10, 4, 6, 3, 0.2f);
            End(scene, path, previous);
            Build("Lab1_Full", 192f, () => Lab1(kit, 192f, 136, 270, 10, 28, 68, 3, 0.2f));
            Build("HideVsFlee", 64f, () => Lab1(kit, 64f, 40, 70, 10, 6, 12, 3, 0.3f));
            Build("ThreeSpecies", 96f, () => ThreeSpecies(kit));
            Build("Terrain_Locomotion", 96f, () => TerrainLocomotion(kit));
            Build("Sandbox", 32f, () => Sandbox(kit));
            BuildScenarios();
            AssetDatabase.SaveAssets();
            Debug.Log("EvoSim: reference content built in " + Root);
        }

        // ---- Scenes ----

        static void Build(string name, float size, System.Action content)
        {
            var scene = Begin(name, size, out string path, out var previous);
            content();
            End(scene, path, previous);
        }

        static Scene Begin(string name, float size, out string path, out Scene previous)
        {
            Directory.CreateDirectory(ScenesFolder);
            path = ScenesFolder + "/" + name + ".unity";
            File.WriteAllText(path, EmptyScene);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.AddComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            cameraGo.transform.position = new Vector3(size / 2f, size * 0.9f, -size * 0.1f);
            cameraGo.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            var lightGo = new GameObject("Directional Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return scene;
        }

        static void End(Scene scene, string path, Scene previous)
        {
            foreach (var root in scene.GetRootGameObjects()) root.SetActive(true);
            RunViews.AddTo(scene);                                                     // ground, markers, overlay, capture, camera
            EditorSceneManager.SaveScene(scene, path);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>A Lab 1 world (04 §5, 14 §2): grass, thickets, carcasses, eggs; prey and predators.</summary>
        static void Lab1(Kit kit, float size, int prey, int preyCap, int preyFloor, int predators, int predatorCap, int predatorFloor, float cover)
        {
            var world = World("Lab1 World", Flat(size), cover, true);
            Prey(kit, world, prey, preyCap, preyFloor);
            Predator(kit, world, predators, predatorCap, predatorFloor);
        }

        /// <summary>S09: rabbits graze, foxes strike rabbits, wolves strike foxes and rabbits; every name in the texts.</summary>
        static void ThreeSpecies(Kit kit)
        {
            var world = World("Three Species World", Flat(96f), 0.2f, true);

            var rabbit = Grazer(kit, world, "Rabbit", "rabbit", 40, 80, 10, true,
                "Rabbits move {walkSpeed} meter per step. Foxes run {fox.Locomotion.runSpeed} and wolves {wolf.Locomotion.runSpeed}\n" +
                "meters per step when they hunt, but they tire long before a rabbit does.");
            rabbit.GetComponent<Species>().SetPromptHeader("You decide what a rabbit does next in a simple world.");
            rabbit.GetComponent<Species>().SetGenesIntro(Intro("rabbit"));
            rabbit.AddComponent<Body>().Configure(null, new Color(0.75f, 0.65f, 0.5f), 0.5f);

            var fox = Hunter(kit, world, "Fox", "fox", 8, 16, 3, 2f, 40f, new[] { "rabbit", "carcass:rabbit" }, true,
                "Foxes run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                "Wolves run {wolf.Locomotion.runSpeed} meters per step; rabbits move {rabbit.Locomotion.walkSpeed} meter per step.");
            fox.AddComponent<Edible>().Configure(EatMethod.Strike, 80f, 2, 40f, 100);
            fox.AddComponent<Body>().Configure(null, new Color(0.95f, 0.5f, 0.1f), 0.7f);

            var wolf = Hunter(kit, world, "Wolf", "wolf", 3, 6, 2, 2f, 30f, new[] { "fox", "rabbit", "carcass:fox", "carcass:rabbit" }, false,
                "Wolves run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                "Foxes run {fox.Locomotion.runSpeed} meters per step; rabbits move {rabbit.Locomotion.walkSpeed} meter per step.");
            wolf.AddComponent<Body>().Configure(null, new Color(0.45f, 0.45f, 0.5f), 1f);
        }

        /// <summary>S17: a generated terrain (about 15 % water, 10 % mountains) with the kinematic locomotion for both species.</summary>
        static void TerrainLocomotion(Kit kit)
        {
            const float size = 96f, height = 20f;
            var data = TerrainAsset(size, height, out float waterLevel);
            var worldGo = new GameObject("Terrain World");
            worldGo.SetActive(false);
            var groundGo = Child(worldGo, "Ground");
            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.transform.SetParent(groundGo.transform, false);
            groundGo.AddComponent<TerrainGround>().Configure(terrainGo.GetComponent<Terrain>(), waterLevel, 35f);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);                      // the water surface: visual only
            water.name = "Water (visual)";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(groundGo.transform, false);
            water.transform.position = new Vector3(size / 2f, waterLevel, size / 2f);
            water.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            water.GetComponent<Renderer>().sharedMaterial = BodyMaterial("Water", new Color(0.2f, 0.45f, 0.8f));

            var world = World(worldGo, 0.2f, true);
            Prey(kit, world, 40, 70, 10);
            Predator(kit, world, 6, 10, 3);
        }

        /// <summary>A small world for trying things: grass and one grazing species, the random brain, no mutation.</summary>
        static void Sandbox(Kit kit)
        {
            var world = World("Sandbox World", Flat(32f), 0f, false);
            var grazer = Grazer(kit, world, "Grazer", "grazer", 12, 30, 4, false, "Animals move {walkSpeed} meter per step.");
            grazer.GetComponent<Species>().SetPromptHeader("You decide what a grazing animal does next in a simple world.");
            grazer.AddComponent<Body>().Configure(kit.PreyBody, Color.white, 0.6f);
        }

        static GameObject Flat(float size)
        {
            var worldGo = new GameObject("World");
            worldGo.SetActive(false);
            Child(worldGo, "Ground").AddComponent<FlatGround>().SetSize(size, size);
            return worldGo;
        }

        static GameObject World(string name, GameObject worldGo, float cover, bool full)
        {
            worldGo.name = name;
            return World(worldGo, cover, full);
        }

        /// <summary>The menu's new world (21 §6): the full tree with the random brain as default, so it runs without a server.</summary>
        internal static GameObject DefaultWorld(string name, float size)
        {
            var go = World(name, Flat(size), 0.2f, true);
            var w = go.GetComponent<World>();
            w.DefaultBrain = go.GetComponentInChildren<RandomBrain>(true);
            return go;
        }

        /// <summary>The world-level modules of 20 §2: environment, brains, recording, phases. Full = JEV and the mutator too.</summary>
        static GameObject World(GameObject worldGo, float cover, bool full)
        {
            var world = worldGo.AddComponent<World>();
            world.SetControlSentences(ControlSentences());
            world.DecisionPeriod = ReferenceDecisionPeriod;

            var env = Child(worldGo, "Environment");
            var grass = Child(env, "Grass");
            grass.AddComponent<FoodGrid>().Configure(0.1f, 0.0015f, true);
            grass.AddComponent<Edible>().Configure(EatMethod.Graze, 25f);
            if (cover > 0f) Child(env, "Thickets").AddComponent<CoverLayer>().SetFraction(cover);
            Child(env, "Carcasses").AddComponent<CarcassSystem>();
            Child(env, "Eggs").AddComponent<EggSystem>();

            var brains = Child(worldGo, "Brains");
            Brain defaultBrain;
            if (full)
            {
                var jev = Child(brains, "JEV").AddComponent<JevBrain>();                   // the reference brain (08 §6)
                jev.SetModelFiles(AssetDatabase.LoadAssetAtPath<TextAsset>(ModelsFolder + "/decision_head.json"),
                                  AssetDatabase.LoadAssetAtPath<TextAsset>(ModelsFolder + "/calibration.json"));
                jev.SetQuestion("predator", "Which action does this predator take now?"); // as the prototype asked predators
                defaultBrain = jev;
                Child(brains, "Random").AddComponent<RandomBrain>();
                Child(brains, "Ollama points").AddComponent<OllamaPointsBrain>();
            }
            else defaultBrain = Child(brains, "Random").AddComponent<RandomBrain>();
            Child(brains, "Answer cache").AddComponent<AnswerCache>();
            if (full)
            {
                Child(brains, "Mutator").AddComponent<MutatorService>().Configure("gemma4:26b", 1.2f);
                Child(brains, "Mutator client").AddComponent<OllamaMutatorClient>();
            }
            world.DefaultBrain = defaultBrain;

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
            return worldGo;
        }

        // ---- Species ----

        /// <summary>The reference prey (20 §2): eat, flee, hide, follow, rest, mate.</summary>
        static void Prey(Kit kit, GameObject world, int population, int cap, int floor)
        {
            var go = Grazer(kit, world, "Prey", "prey", population, cap, floor, true, PreyLocomotionRule);
            go.AddComponent<Body>().Configure(kit.PreyBody, new Color(0.45f, 0.8f, 0.35f), 0.6f);
        }

        /// <summary>The reference predator (20 §2): hunt, follow, rest, mate.</summary>
        static void Predator(Kit kit, GameObject world, int population, int cap, int floor)
        {
            var go = Hunter(kit, world, "Predator", "predator", population, cap, floor, 2f, 30f, new[] { "prey", "carcass:prey" }, false, PredatorLocomotionRule);
            go.AddComponent<Body>().Configure(kit.PredatorBody, new Color(0.85f, 0.25f, 0.2f), 0.9f);
        }

        /// <summary>A grazing species: the prey's modules; with threats, also flee, hide and the predator and cover senses.</summary>
        static GameObject Grazer(Kit kit, GameObject world, string goName, string id, int population, int cap, int floor, bool threats, string locomotionRule)
        {
            var go = Child(world, goName);
            var s = go.AddComponent<Species>();
            s.SetNames(id, id);
            s.SetInitialPopulation(population);
            var search = go.AddComponent<SearchRule>();
            if (!threats) search.SetPromptRule("If the chosen action has nothing to act on in sight (no food, animal or ready\npartner), the animal searches the surroundings instead.");
            if (threats) go.AddComponent<Edible>().Configure(EatMethod.Strike, 60f, 2, 30f, 100);

            var loco = Child(go, "Locomotion").AddComponent<KinematicLocomotion>();
            loco.SetSpeeds(1f, 1f);
            loco.SetPromptRule(locomotionRule);
            var stats = Child(go, "Stats");
            stats.AddComponent<Energy>().Configure(100f, 60f);
            stats.AddComponent<Stamina>().Configure(60f, 2f, 0.3f);
            stats.AddComponent<Metabolism>().Configure(0.7f, 0.5f, 0.2f);

            var senses = Child(go, "Senses");
            Child(senses, "Energy").AddComponent<LevelSense>().Configure("energy", 30f, 70f);
            Child(senses, "Stamina").AddComponent<LevelSense>().Configure("stamina", 20f, 40f, StaminaWords);
            Child(senses, "Food").AddComponent<NearestResourceSense>();
            if (threats)
            {
                Child(senses, "Predator").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Threats);
                Child(senses, "Cover").AddComponent<NearestCoverSense>();
            }
            Child(senses, "Animal").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Kin, readiness: true, partner: 20f);
            Child(senses, "Age").AddComponent<AgeSense>();

            var actions = Child(go, "Actions");
            Module(kit, actions, "Eat", "prey.eat");
            if (threats)
            {
                Module(kit, actions, "Flee", "prey.flee");
                Module(kit, actions, "Hide", "prey.hide");
            }
            Module(kit, actions, "Follow", "prey.follow");
            Module(kit, actions, "Rest", "prey.rest");
            Module(kit, actions, "Mate", "prey.mate");

            Child(go, "Food").AddComponent<Diet>().Set("Grass");
            Life(go, cap, floor);
            if (threats) Child(go, "Mutation").AddComponent<LlmMutation>().Configure(kit.Deck, ContextLine);
            return go;
        }

        /// <summary>A hunting species: hunt (strike and scavenge by its diet), maybe flee, follow, rest, mate.</summary>
        static GameObject Hunter(Kit kit, GameObject world, string goName, string id, int population, int cap, int floor, float runSpeed,
                                 float stamina, string[] diet, bool flees, string locomotionRule)
        {
            var go = Child(world, goName);
            var s = go.AddComponent<Species>();
            s.SetNames(id, id);
            s.SetInitialPopulation(population);
            s.SetPromptHeader($"You decide what a {id} does next in a simple world.");
            s.SetGenesIntro(Intro(id));
            go.AddComponent<SearchRule>().SetPromptRule(
                $"If the chosen action has nothing to act on in sight (no prey, carcass, other\n{id} or ready partner), the {id} searches the surroundings instead.");

            var loco = Child(go, "Locomotion").AddComponent<KinematicLocomotion>();
            loco.SetSpeeds(1f, runSpeed);
            loco.SetPromptRule(locomotionRule);
            var stats = Child(go, "Stats");
            stats.AddComponent<Energy>().Configure(100f, 60f);
            var st = stats.AddComponent<Stamina>();
            st.Configure(stamina, 2f, 0.3f);
            st.SetPromptRule($"Every meter moved costs stamina. Standing still brings it back, which costs some\nenergy until stamina is full. Without stamina a {id} cannot move.");
            stats.AddComponent<Metabolism>().Configure(0.7f, 0.5f, 0.2f);
            stats.AddComponent<Digestion>().Configure(50, 60f);

            var senses = Child(go, "Senses");
            Child(senses, "Energy").AddComponent<LevelSense>().Configure("energy", 30f, 70f);
            Child(senses, "Stamina").AddComponent<LevelSense>().Configure("stamina", 10f, 20f, StaminaWords);
            Child(senses, "Prey").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Prey);
            if (flees) Child(senses, "Predator").AddComponent<NearestAnimalSense>().Configure(AnimalSet.Threats);
            Child(senses, "Carcass").AddComponent<NearestEntitySense>();
            var kin = Child(senses, "Other " + id).AddComponent<NearestAnimalSense>();
            kin.Configure(AnimalSet.Kin, readiness: true, partner: 20f);
            kin.SetLabel("Other " + id);
            Child(senses, "Age").AddComponent<AgeSense>();

            var actions = Child(go, "Actions");
            Module(kit, actions, "Hunt", "predator.hunt");
            if (flees) Module(kit, actions, "Flee", "prey.flee");
            Module(kit, actions, "Follow", "predator.follow").GetComponent<AnimalAction>().SetDescription($"move toward the nearest other {id}");
            Module(kit, actions, "Rest", "predator.rest");
            Module(kit, actions, "Mate", "predator.mate").GetComponent<AnimalAction>()
                .SetDescription($"walk to the nearest ready partner (another {id}) in sight and breed with it");

            var food = Child(go, "Food").AddComponent<Diet>();
            food.Set(diet);
            food.SetKillChance(0.5f);
            Life(go, cap, floor);
            Child(go, "Mutation").AddComponent<LlmMutation>().Configure(kit.Deck, ContextLine);
            return go;
        }

        static string Intro(string noun) =>
            $"This {noun}'s instincts (its genes). They define its personality: follow them\neven when they seem unwise. Instincts that are meaningless have no effect.";

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
            life.AddComponent<DecisionSchedule>().Configure(0, true);                 // staggered decisions: the reference (DEC-04, owner 2026-10-10)
        }

        /// <summary>
        /// Brings the saved reference scenes to the builder's reference without rebuilding them: the reference decision period
        /// (DEC-01, owner 2026-10-11) and a staggered DecisionSchedule in every species' Life group (DEC-04, owner 2026-10-10);
        /// a species that has a schedule keeps it.
        /// </summary>
        [MenuItem("EvoSim/Reference/Update Reference Scenes")]
        public static string UpdateReferenceScenes()
        {
            var lines = new List<string>();
            foreach (var file in Directory.GetFiles(ScenesFolder, "*.unity"))
            {
                string path = file.Replace('\\', '/');
                if (SceneManager.GetSceneByPath(path).isLoaded) { lines.Add(path + ": open in the editor, skipped"); continue; }
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var added = new List<string>();
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var w in root.GetComponentsInChildren<World>(true))
                            if (w.DecisionPeriod != ReferenceDecisionPeriod)
                            {
                                added.Add($"period {w.DecisionPeriod} → {ReferenceDecisionPeriod}");
                                w.DecisionPeriod = ReferenceDecisionPeriod;
                                EditorUtility.SetDirty(w);
                            }
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var s in root.GetComponentsInChildren<Species>(true))
                        {
                            if (s.GetComponentInChildren<DecisionSchedule>(true) != null) continue;
                            var life = s.transform.Find("Life");
                            var go = life != null ? life.gameObject : s.gameObject;
                            go.AddComponent<DecisionSchedule>().Configure(0, true);
                            added.Add(s.name + " staggered");
                        }
                    if (added.Count > 0) EditorSceneManager.SaveScene(scene);
                    lines.Add($"{Path.GetFileNameWithoutExtension(path)}: {(added.Count > 0 ? string.Join(", ", added) : "nothing")}");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            string result = string.Join("\n", lines);
            Debug.Log("EvoSim reference scenes updated:\n" + result);
            return result;
        }

        /// <summary>An instance of a module prefab with the pool of the given locus (prefab override when it differs).</summary>
        static GameObject Module(Kit kit, GameObject parent, string module, string poolKey)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(kit.Modules[module], parent.transform);
            go.name = module.ToLowerInvariant();                                    // the action's name (prompt, locus id) is lower case
            var gene = go.GetComponent<TextGene>();
            if (gene != null && kit.Pools.TryGetValue(poolKey, out var pool)) gene.SetPool(pool);
            return go;
        }

        // ---- Module prefabs and bodies ----

        /// <summary>The action prefabs of 20 §7 (an action and its text gene, named after the action) and the placeholder bodies.</summary>
        static void BuildModules(Kit kit)
        {
            Directory.CreateDirectory(ModulesFolder);
            Directory.CreateDirectory(BodiesFolder);
            kit.Modules["Eat"] = ActionPrefab<EatAction>("Eat", "eat", kit.Pools["prey.eat"]);
            kit.Modules["Flee"] = ActionPrefab<FleeAction>("Flee", "flee", kit.Pools["prey.flee"]);
            kit.Modules["Hide"] = ActionPrefab<HideAction>("Hide", "hide", kit.Pools["prey.hide"]);
            kit.Modules["Follow"] = ActionPrefab<FollowAction>("Follow", "follow", kit.Pools["prey.follow"]);
            kit.Modules["Rest"] = ActionPrefab<RestAction>("Rest", "rest", kit.Pools["prey.rest"]);
            kit.Modules["Mate"] = ActionPrefab<MateAction>("Mate", "mate", kit.Pools["prey.mate"]);
            kit.Modules["Hunt"] = ActionPrefab<HuntAction>("Hunt", "hunt", kit.Pools["predator.hunt"]);
            kit.PreyBody = BodyPrefab("PreyBody", new Color(0.45f, 0.8f, 0.35f));
            kit.PredatorBody = BodyPrefab("PredatorBody", new Color(0.85f, 0.25f, 0.2f));

            Directory.CreateDirectory(ModulesFolder + "/Senses");                         // Add Sense (21 §6)
            SensePrefab<LevelSense>("Energy", x => x.Configure("energy", 30f, 70f));
            SensePrefab<LevelSense>("Stamina", x => x.Configure("stamina", 20f, 40f, StaminaWords));
            SensePrefab<NearestResourceSense>("Food", null);
            SensePrefab<NearestAnimalSense>("Predator", x => x.Configure(AnimalSet.Threats));
            SensePrefab<NearestAnimalSense>("Prey", x => x.Configure(AnimalSet.Prey));
            SensePrefab<NearestCoverSense>("Cover", null);
            SensePrefab<NearestAnimalSense>("Animal", x => x.Configure(AnimalSet.Kin, readiness: true, partner: 20f));
            SensePrefab<NearestEntitySense>("Carcass", null);
            SensePrefab<AgeSense>("Age", null);

            Directory.CreateDirectory(ModulesFolder + "/Genes");                          // Add Gene: the stamina gene of 09 §5
            var gene = new GameObject("stamina");
            gene.AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 45f, 60f, 75f }, false, "stamina");
            gene.AddComponent<GaussianMutation>().Sigma = 5f;
            PrefabUtility.SaveAsPrefabAsset(gene, ModulesFolder + "/Genes/StaminaGene.prefab");
            Object.DestroyImmediate(gene);
        }

        static void SensePrefab<T>(string name, System.Action<T> configure) where T : Sense
        {
            var go = new GameObject(name);                                         // the GameObject's name is the sense's label
            var sense = go.AddComponent<T>();
            configure?.Invoke(sense);
            PrefabUtility.SaveAsPrefabAsset(go, ModulesFolder + "/Senses/" + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        static GameObject ActionPrefab<T>(string file, string actionName, AllelePool pool) where T : AnimalAction
        {
            var go = new GameObject(actionName);                                  // the GameObject's name is the action's name
            go.AddComponent<T>();
            go.AddComponent<TextGene>().SetPool(pool);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ModulesFolder + "/" + file + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>A placeholder body: a capsule as tall as it is wide standing on the ground, with a nose and an action marker (RunViews).</summary>
        static GameObject BodyPrefab(string file, Color color) => RunViews.BodyPrefab(file, BodyMaterial(file, color));

        static Material BodyMaterial(string name, Color color)
        {
            Directory.CreateDirectory(BodiesFolder);
            string path = BodiesFolder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// The terrain of S17: value noise from a fixed seed, the top tenth raised into steep mountains; the water level is
        /// the 15th percentile of the heights.
        /// </summary>
        static TerrainData TerrainAsset(float size, float height, out float waterLevel)
        {
            const int resolution = 129;
            string path = ScenesFolder + "/Terrain_Locomotion_TerrainData.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, path);
            }
            data.heightmapResolution = resolution;
            data.size = new Vector3(size, height, size);
            var noise = new ValueNoise(new Rect(0, 0, size, size), new[] { 32f, 12f, 5f }, new RandomStream(1234, "terrain"), new[] { 0.6f, 0.3f, 0.1f });
            var h = new float[resolution, resolution];
            var all = new List<float>(resolution * resolution);
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    h[z, x] = noise.Sample(x * size / (resolution - 1), z * size / (resolution - 1));
                    all.Add(h[z, x]);
                }
            all.Sort();
            float p15 = all[all.Count * 15 / 100], p90 = all[all.Count * 90 / 100], top = all[all.Count - 1];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    float v = h[z, x];
                    if (v > p90) v = p90 + (v - p90) * 6f;                                       // mountains: steep
                    h[z, x] = v;
                }
            float max = p90 + (top - p90) * 6f;
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++) h[z, x] = Mathf.Clamp01(h[z, x] / max);
            data.SetHeights(0, 0, h);
            EditorUtility.SetDirty(data);
            waterLevel = p15 / max * height;
            return data;
        }

        // ---- Pools, controls, scenarios ----

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

        /// <summary>The scenario assets whose scenes exist (31 §2); more come with the samples (M11).</summary>
        public static void BuildScenarios()
        {
            Directory.CreateDirectory(ScenariosFolder);
            // T2 runs use the random brain without model calls (C2: no mutation); the "jev" variants are the T4 runs.
            var s03 = Scenario("S03_Lab1Small", "S03 Lab 1 small", ScenesFolder + "/Lab1_Small.unity", 1000, "", "T2",
                               "invariants", "deterministic");
            s03.variants = new List<ScenarioAsset.Variant> { Offline("default"), Online("jev", "JEV") };
            var s02 = Scenario("S02_Lab1Reference", "S02 Lab 1 reference", ScenesFolder + "/Lab1_Full.unity", 2000, "", "T4",
                               "invariants", "deterministic", "no newcomer in 10 000 ticks (R-03)");
            s02.variants = new List<ScenarioAsset.Variant> { Online("default", "JEV"), Offline("random") };

            // The other scene-based scenarios of 31 §2; the ones that need code (S01, S08, S10–S14, S16, S18, S21,
            // S22, S24, S28) are tests (Tests/EditMode/Scenarios and the systems' tests).
            string small = ScenesFolder + "/Lab1_Small.unity", full = ScenesFolder + "/Lab1_Full.unity";
            Scenario("S00_EmptyWorld", "S00 Empty world", ScenesFolder + "/Sandbox.unity", 1000, "Random", "T2", "invariants", "deterministic")
                .variants = new List<ScenarioAsset.Variant> { Offline("default", "Grazer") };
            Scenario("S04_NullBrain", "S04 Null brain", full, 2000, "Random", "T4", "invariants", "predators need newcomers in 5/5 seeds")
                .variants = new List<ScenarioAsset.Variant> { Online("default", "Random") };
            Scenario("S05_Controls", "S05 Controls", small, 1000, "Random", "T4", "invariants", "deterministic", "each control's defining fact")
                .variants = new List<ScenarioAsset.Variant>
                {
                    Offline("C2"), Variant("C3", "Random", "T4", ("World/shuffledGenes", "true")), Variant("C4", "Random", "T4", ("World/randomFounders", "true")),
                    Variant("C7", "Random", "T4", ("World/asexual", "true")),
                };
            var s06 = Scenario("S06_HideVsFlee", "S06 Hide versus flee", full, 5000, "JEV", "T4", "invariants", "deterministic",
                               "kills(hide-and-flee) < kills(flee-only) in >= 4/5 seeds", "share(prey, hide) > 0.02 in hide-and-flee");
            s06.seeds = new List<int> { 1234, 7, 42, 99, 2026 };
            s06.variants = new List<ScenarioAsset.Variant>
            {
                Online("hide-and-flee", "JEV"),
                Online("flee-only", "JEV", "Prey/Actions/hide", "Prey/Senses/Cover"),
                With(Online("prototype", "JEV", "Prey/Actions/hide", "Prey/Senses/Cover"), ("Prey/Actions/flee/FleeAction/fleeIntoCover", "true")),
            };
            Scenario("S07_NoCover", "S07 No cover", full, 5000, "JEV", "T4", "invariants", "validation has no error", "more prey killed than S02 in >= 4/5 seeds")
                .variants = new List<ScenarioAsset.Variant> { Online("default", "JEV", "Environment/Thickets", "Prey/Actions/hide", "Prey/Senses/Cover") };
            Scenario("S09_FoodChain", "S09 Food chain of three", ScenesFolder + "/ThreeSpecies.unity", 5000, "Random", "T3", "invariants", "deterministic")
                .variants = new List<ScenarioAsset.Variant> { Offline("default"), Online("jev", "JEV") };
            Scenario("S15_ObservationWording", "S15 Observation wording", small, 1000, "JEV", "T4", "invariants", "G1 holds in V1 and V2")
                .variants = new List<ScenarioAsset.Variant> { Variant("V1", "JEV", "T4", ("World/textStyle", "V1")), Variant("V2", "JEV", "T4", ("World/textStyle", "V2")) };
            Scenario("S17_TerrainLocomotion", "S17 Terrain and locomotion", ScenesFolder + "/Terrain_Locomotion.unity", 2000, "Random", "T3",
                     "invariants", "deterministic", "no animal on water or mountain")
                .variants = new List<ScenarioAsset.Variant> { Offline("kinematic") };
            Scenario("S19_ActOrders", "S19 Act orders", full, 2000, "Random", "T3", "invariants", "deterministic")
                .variants = new List<ScenarioAsset.Variant>
                {
                    With(Offline("species-in-turn"), ("Phases/Act/ActPhase/order", "SpeciesInTurn")),
                    With(Offline("all-mixed"), ("Phases/Act/ActPhase/order", "AllMixed")),
                    With(Offline("simultaneous"), ("Phases/Act/ActPhase/order", "Simultaneous")),
                };
            Scenario("S20_CapRules", "S20 Cap rules", full, 5000, "JEV", "T4", "invariants", "generations(block) < generations(migrate) in >= 4/5 seeds")
                .variants = new List<ScenarioAsset.Variant>
                {
                    Variant("migrate", "JEV", "T4", ("Prey/CapRule/mode", "Migrate"), ("Predator/CapRule/mode", "Migrate")),
                    Variant("block", "JEV", "T4", ("Prey/CapRule/mode", "Block"), ("Predator/CapRule/mode", "Block")),
                };
            Scenario("S23_Scale", "S23 Scale", full, 500, "Random", "T3", "invariants", "tick time under 12 ms")
                .variants = new List<ScenarioAsset.Variant>
                {
                    With(Offline("x6"), ("Prey/CapRule/cap", "1620"), ("Predator/CapRule/cap", "408"),
                         ("Prey/Species/initialPopulation", "816"), ("Predator/Species/initialPopulation", "168")),
                };
            Scenario("S25_DecisionTiming", "S25 Decision timing", small, 400, "Random", "T2", "invariants", "deterministic")
                .variants = new List<ScenarioAsset.Variant>
                {
                    With(Offline("period-1"), ("World/decisionPeriod", "1")), With(Offline("period-4"), ("World/decisionPeriod", "4")),
                    With(Offline("period-8"), ("World/decisionPeriod", "8")),
                };
            Scenario("S26_NamesInGenes", "S26 Names in genes", small, 1000, "JEV", "T4", "P(flee | shadow close) < P(flee | wolf close)", "V-23 flags the gene")
                .variants = new List<ScenarioAsset.Variant>
                {
                    Variant("wolf", "JEV", "T4", ("Predator/Species/displayName", "wolf")), Variant("shadow", "JEV", "T4", ("Predator/Species/displayName", "shadow")),
                };
            Scenario("S27_Lab1WithLLM", "S27 Lab 1 with an LLM", full, 500, "JEV", "T4", "no failed call", "no species needs newcomers")
                .variants = new List<ScenarioAsset.Variant> { Online("jev", "JEV"), Online("gemma", "Ollama points") };
        }

        static ScenarioAsset.Variant Variant(string name, string brain, string tier, params (string path, string value)[] set)
        {
            var v = new ScenarioAsset.Variant { name = name, brain = brain, tier = tier };
            foreach (var (path, value) in set) v.set.Add(new ScenarioAsset.Override(path, value));
            return v;
        }

        static ScenarioAsset.Variant With(ScenarioAsset.Variant v, params (string path, string value)[] set)
        {
            foreach (var (path, value) in set) v.set.Add(new ScenarioAsset.Override(path, value));
            return v;
        }

        /// <summary>A variant that makes no model call: the random brain and C2 (no mutation).</summary>
        static ScenarioAsset.Variant Offline(string name, params string[] remove) => new ScenarioAsset.Variant
        {
            name = name, brain = "Random", tier = "T2", remove = new List<string>(remove),
            set = new List<ScenarioAsset.Override> { new ScenarioAsset.Override("World/noMutation", "true") },
        };

        static ScenarioAsset.Variant Online(string name, string brain, params string[] remove) =>
            new ScenarioAsset.Variant { name = name, brain = brain, tier = "T4", remove = new List<string>(remove) };

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

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
