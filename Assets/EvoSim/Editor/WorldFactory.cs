using System.IO;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// Creating things from the menus (21 §6): a new world with the default tree, a new species with the reference
    /// body modules, and actions, senses and genes from the module prefabs in Modules/. The static methods are what
    /// the menus call, so tests build worlds exactly as a student does.
    /// </summary>
    public static class WorldFactory
    {
        public const string ModulesFolder = ReferenceScenes.ModulesFolder;
        public static readonly string[] Actions = { "Eat", "Flee", "Hide", "Follow", "Rest", "Mate", "Hunt" };
        public static readonly string[] Senses = { "Energy", "Stamina", "Food", "Predator", "Prey", "Cover", "Animal", "Carcass", "Age" };
        public static readonly string[] Genes = { "StaminaGene" };

        /// <summary>
        /// The default tree (21 §6): ground, environment, brains, recorder and the eleven phases. The random brain is the
        /// default, so a new world runs without a model server; JEV is there to switch to.
        /// </summary>
        public static GameObject NewWorld(string name = "World", float size = 48f)
        {
            var go = ReferenceScenes.DefaultWorld(name, size);
            go.SetActive(true);
            return go;
        }

        /// <summary>A species with a kinematic locomotion, energy, stamina, metabolism, diet, mating, litter, crossover, starvation, old age, cap, floor and staggered decisions.</summary>
        public static GameObject NewSpecies(GameObject world, string name = "grazer")
        {
            var go = new GameObject(name);
            go.transform.SetParent(world.transform, false);
            var s = go.AddComponent<Species>();
            s.SetNames(name.ToLowerInvariant().Replace(' ', '_'), name);
            s.SetInitialPopulation(10);
            go.AddComponent<SearchRule>();
            Child(go, "Locomotion").AddComponent<KinematicLocomotion>();
            var stats = Child(go, "Stats");
            stats.AddComponent<Energy>();
            stats.AddComponent<Stamina>();
            stats.AddComponent<Metabolism>();
            Child(go, "Senses");
            Child(go, "Actions");
            Child(go, "Food").AddComponent<Diet>().Set("Grass");
            var life = Child(go, "Life");
            life.AddComponent<MatingRule>();
            life.AddComponent<Litter>();
            life.AddComponent<UniformCrossover>();
            life.AddComponent<Starvation>();
            life.AddComponent<OldAge>();
            life.AddComponent<CapRule>();
            life.AddComponent<FloorRule>();
            life.AddComponent<DecisionSchedule>().Configure(0, true);                 // staggered, like the reference species (DEC-04)
            go.AddComponent<Body>();
            return go;
        }

        /// <summary>Instantiates a module prefab (an action, a sense or a gene) under the species' folder of that kind.</summary>
        public static GameObject AddModule(GameObject species, string kind, string module)
        {
            string path = kind == "Actions" ? $"{ModulesFolder}/{module}.prefab" : $"{ModulesFolder}/{kind}/{module}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("No module prefab at " + path + " (EvoSim ▸ Build Reference Scenes makes them).");
            var folder = species.transform.Find(kind) != null ? species.transform.Find(kind).gameObject : Child(species, kind);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, folder.transform);
            if (kind == "Actions") go.name = module.ToLowerInvariant();            // an action's name is lower case (prompt, locus id)
            return go;
        }

        // ---- Menus ----

        [MenuItem("GameObject/EvoSim/New World", false, 10)]
        static void NewWorldMenu(MenuCommand command)
        {
            var go = NewWorld();
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "New EvoSim World");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/EvoSim/New Species", false, 11)]
        static void NewSpeciesMenu()
        {
            var world = Selected<World>();
            var go = NewSpecies(world.gameObject, "Species " + (world.GetComponentsInChildren<Species>(true).Length + 1));
            Undo.RegisterCreatedObjectUndo(go, "New EvoSim Species");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/EvoSim/New Species", true)]
        static bool CanAddSpecies() => Selected<World>() != null;

        static void Add(string kind, string module)
        {
            var species = Selected<Species>();
            var go = AddModule(species.gameObject, kind, module);
            Undo.RegisterCreatedObjectUndo(go, "Add " + module);
            Selection.activeObject = go;
        }

        static bool CanAdd() => Selected<Species>() != null;

        [MenuItem("GameObject/EvoSim/Add Action/Eat", false, 20)] static void AddEat() => Add("Actions", "Eat");
        [MenuItem("GameObject/EvoSim/Add Action/Flee", false, 20)] static void AddFlee() => Add("Actions", "Flee");
        [MenuItem("GameObject/EvoSim/Add Action/Hide", false, 20)] static void AddHide() => Add("Actions", "Hide");
        [MenuItem("GameObject/EvoSim/Add Action/Follow", false, 20)] static void AddFollow() => Add("Actions", "Follow");
        [MenuItem("GameObject/EvoSim/Add Action/Rest", false, 20)] static void AddRest() => Add("Actions", "Rest");
        [MenuItem("GameObject/EvoSim/Add Action/Mate", false, 20)] static void AddMate() => Add("Actions", "Mate");
        [MenuItem("GameObject/EvoSim/Add Action/Hunt", false, 20)] static void AddHunt() => Add("Actions", "Hunt");
        [MenuItem("GameObject/EvoSim/Add Sense/Energy", false, 21)] static void AddEnergy() => Add("Senses", "Energy");
        [MenuItem("GameObject/EvoSim/Add Sense/Stamina", false, 21)] static void AddStamina() => Add("Senses", "Stamina");
        [MenuItem("GameObject/EvoSim/Add Sense/Food", false, 21)] static void AddFood() => Add("Senses", "Food");
        [MenuItem("GameObject/EvoSim/Add Sense/Predator", false, 21)] static void AddPredator() => Add("Senses", "Predator");
        [MenuItem("GameObject/EvoSim/Add Sense/Prey", false, 21)] static void AddPrey() => Add("Senses", "Prey");
        [MenuItem("GameObject/EvoSim/Add Sense/Cover", false, 21)] static void AddCover() => Add("Senses", "Cover");
        [MenuItem("GameObject/EvoSim/Add Sense/Animal", false, 21)] static void AddAnimal() => Add("Senses", "Animal");
        [MenuItem("GameObject/EvoSim/Add Sense/Carcass", false, 21)] static void AddCarcass() => Add("Senses", "Carcass");
        [MenuItem("GameObject/EvoSim/Add Sense/Age", false, 21)] static void AddAge() => Add("Senses", "Age");
        [MenuItem("GameObject/EvoSim/Add Gene/Stamina gene", false, 22)] static void AddStaminaGene() => Add("Genes", "StaminaGene");

        [MenuItem("GameObject/EvoSim/Add Action/Eat", true)] static bool CanAddEat() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Flee", true)] static bool CanAddFlee() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Hide", true)] static bool CanAddHide() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Follow", true)] static bool CanAddFollow() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Rest", true)] static bool CanAddRest() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Mate", true)] static bool CanAddMate() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Action/Hunt", true)] static bool CanAddHunt() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Energy", true)] static bool CanAddEnergy() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Stamina", true)] static bool CanAddStamina() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Food", true)] static bool CanAddFood() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Predator", true)] static bool CanAddPredator() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Prey", true)] static bool CanAddPrey() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Cover", true)] static bool CanAddCover() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Animal", true)] static bool CanAddAnimal() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Carcass", true)] static bool CanAddCarcass() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Sense/Age", true)] static bool CanAddAge() => CanAdd();
        [MenuItem("GameObject/EvoSim/Add Gene/Stamina gene", true)] static bool CanAddStaminaGene() => CanAdd();

        /// <summary>The component of that type on the selection or above it.</summary>
        static T Selected<T>() where T : Component =>
            Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<T>(true) : null;

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
