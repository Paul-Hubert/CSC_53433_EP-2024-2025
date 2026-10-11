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
    /// <remarks>
    /// <para>The menus, in the Hierarchy's right-click menu and in <c>GameObject ▸ EvoSim</c>:</para>
    /// <list type="bullet">
    /// <item><description><c>New World</c>: a world under the clicked object (or at the root), selected, with Undo.</description></item>
    /// <item><description><c>New Species</c>: with a World (or one of its children) selected, a species named
    /// <c>Species &lt;n&gt;</c> under it. It has no sense and no action yet: validation asks for one (V-04).</description></item>
    /// <item><description><c>Add Action ▸ Eat … Hunt</c>, <c>Add Sense ▸ Energy … Age</c>, <c>Add Gene ▸ Stamina gene</c>:
    /// with a Species (or one of its children) selected, an instance of the module prefab under its <c>Actions</c>,
    /// <c>Senses</c> or <c>Genes</c> group.</description></item>
    /// </list>
    /// <para>The module prefabs are made by <c>EvoSim ▸ Build Reference Scenes</c>. A student's own module can be added the
    /// same way: save it as a prefab in <see cref="ModulesFolder"/> and call <see cref="AddModule"/>, or drag the prefab.</para>
    /// </remarks>
    public static class WorldFactory
    {
        /// <summary>The module prefabs' folder: <c>Assets/EvoSim/Modules</c>.</summary>
        public const string ModulesFolder = ReferenceScenes.ModulesFolder;
        /// <summary>The action prefabs offered by <c>Add Action</c>, by file name in <see cref="ModulesFolder"/>.</summary>
        public static readonly string[] Actions = { "Eat", "Flee", "Hide", "Follow", "Rest", "Mate", "Hunt" };
        /// <summary>
        /// The sense prefabs offered by <c>Add Sense</c>, by file name in <c>Modules/Senses</c>; the name is the sense's label.
        /// </summary>
        public static readonly string[] Senses = { "Energy", "Stamina", "Food", "Predator", "Prey", "Cover", "Animal", "Carcass", "Age" };
        /// <summary>The gene prefabs offered by <c>Add Gene</c>, by file name in <c>Modules/Genes</c>.</summary>
        public static readonly string[] Genes = { "StaminaGene" };

        /// <summary>
        /// The default tree (21 §6): ground, environment, brains, recorder and the eleven phases. The random brain is the
        /// default, so a new world runs without a model server; JEV is there to switch to.
        /// </summary>
        /// <remarks>It has no species: add one with <see cref="NewSpecies"/>. Its decision period is
        /// <see cref="ReferenceScenes.ReferenceDecisionPeriod"/>.</remarks>
        /// <param name="name">The World object's name.</param>
        /// <param name="size">The side of the flat square ground, in meters.</param>
        /// <returns>The active World object, at the root of the active scene.</returns>
        public static GameObject NewWorld(string name = "World", float size = 48f)
        {
            var go = ReferenceScenes.DefaultWorld(name, size);
            go.SetActive(true);
            return go;
        }

        /// <summary>A species with a kinematic locomotion, energy, stamina, metabolism, diet, mating, litter, crossover, starvation, old age, cap, floor and staggered decisions.</summary>
        /// <remarks>Its groups are <c>Locomotion</c>, <c>Stats</c>, <c>Senses</c> and <c>Actions</c> (empty), <c>Food</c> (a
        /// diet of <c>Grass</c>) and <c>Life</c>; it starts with 10 animals and a body. Add senses, actions and genes with
        /// <see cref="AddModule"/>.</remarks>
        /// <param name="world">The World object to put the species under.</param>
        /// <param name="name">The display name; the id is the same in lower case with '_' for spaces.</param>
        /// <returns>The new species object.</returns>
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
        /// <param name="species">The species object.</param>
        /// <param name="kind"><c>Actions</c> (prefabs at the root of <see cref="ModulesFolder"/>), <c>Senses</c> or <c>Genes</c>
        /// (prefabs in that subfolder); also the name of the group the module goes under, created if missing.</param>
        /// <param name="module">The prefab's file name without <c>.prefab</c>, such as <c>Eat</c> or <c>StaminaGene</c>.</param>
        /// <returns>The prefab instance; an action is renamed in lower case, since its name is its name in the prompt.</returns>
        /// <exception cref="FileNotFoundException">No such prefab: build the reference content first.</exception>
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

        /// <summary><c>GameObject ▸ EvoSim ▸ New World</c>: a default world under the right-clicked object, selected, with Undo.</summary>
        [MenuItem("GameObject/EvoSim/New World", false, 10)]
        static void NewWorldMenu(MenuCommand command)
        {
            var go = NewWorld();
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "New EvoSim World");
            Selection.activeObject = go;
        }

        /// <summary>
        /// <c>GameObject ▸ EvoSim ▸ New Species</c>: a species <c>Species &lt;n&gt;</c> under the selected World, selected, with Undo.
        /// </summary>
        [MenuItem("GameObject/EvoSim/New Species", false, 11)]
        static void NewSpeciesMenu()
        {
            var world = Selected<World>();
            var go = NewSpecies(world.gameObject, "Species " + (world.GetComponentsInChildren<Species>(true).Length + 1));
            Undo.RegisterCreatedObjectUndo(go, "New EvoSim Species");
            Selection.activeObject = go;
        }

        /// <summary>Enables New Species only when a World, or an object under one, is selected.</summary>
        [MenuItem("GameObject/EvoSim/New Species", true)]
        static bool CanAddSpecies() => Selected<World>() != null;

        /// <summary>Adds a module prefab to the selected species (see <see cref="AddModule"/>), selects it, with Undo.</summary>
        static void Add(string kind, string module)
        {
            var species = Selected<Species>();
            var go = AddModule(species.gameObject, kind, module);
            Undo.RegisterCreatedObjectUndo(go, "Add " + module);
            Selection.activeObject = go;
        }

        /// <summary>True when a Species, or an object under one, is selected: the Add menus are enabled only then.</summary>
        static bool CanAdd() => Selected<Species>() != null;

        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Eat</c>: walk to food the diet allows and eat it.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Eat", false, 20)] static void AddEat() => Add("Actions", "Eat");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Flee</c>: run away from the nearest threat.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Flee", false, 20)] static void AddFlee() => Add("Actions", "Flee");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Hide</c>: go into the nearest cover.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Hide", false, 20)] static void AddHide() => Add("Actions", "Hide");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Follow</c>: move toward the nearest animal of the same species.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Follow", false, 20)] static void AddFollow() => Add("Actions", "Follow");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Rest</c>: stay still to recover stamina.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Rest", false, 20)] static void AddRest() => Add("Actions", "Rest");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Mate</c>: walk to a ready partner and breed.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Mate", false, 20)] static void AddMate() => Add("Actions", "Mate");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Action ▸ Hunt</c>: chase and strike prey, or scavenge, as the diet says.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Hunt", false, 20)] static void AddHunt() => Add("Actions", "Hunt");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Energy</c>: energy as low, medium or high.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Energy", false, 21)] static void AddEnergy() => Add("Senses", "Energy");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Stamina</c>: stamina as low, medium or high.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Stamina", false, 21)] static void AddStamina() => Add("Senses", "Stamina");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Food</c>: the distance to the nearest food item.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Food", false, 21)] static void AddFood() => Add("Senses", "Food");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Predator</c>: the distance to the nearest threat.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Predator", false, 21)] static void AddPredator() => Add("Senses", "Predator");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Prey</c>: the distance to the nearest prey.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Prey", false, 21)] static void AddPrey() => Add("Senses", "Prey");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Cover</c>: the distance to the nearest cover.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Cover", false, 21)] static void AddCover() => Add("Senses", "Cover");
        /// <summary>
        /// <c>GameObject ▸ EvoSim ▸ Add Sense ▸ Animal</c>: the nearest animal of the same species, and whether it is ready to mate.
        /// </summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Animal", false, 21)] static void AddAnimal() => Add("Senses", "Animal");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Carcass</c>: the distance to the nearest carcass.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Carcass", false, 21)] static void AddCarcass() => Add("Senses", "Carcass");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Sense ▸ Age</c>: young or adult.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Age", false, 21)] static void AddAge() => Add("Senses", "Age");
        /// <summary><c>GameObject ▸ EvoSim ▸ Add Gene ▸ Stamina gene</c>: a number gene for the maximum stamina (09 §5).</summary>
        [MenuItem("GameObject/EvoSim/Add Gene/Stamina gene", false, 22)] static void AddStaminaGene() => Add("Genes", "StaminaGene");

        /// <summary>Enables Add Action ▸ Eat when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Eat", true)] static bool CanAddEat() => CanAdd();
        /// <summary>Enables Add Action ▸ Flee when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Flee", true)] static bool CanAddFlee() => CanAdd();
        /// <summary>Enables Add Action ▸ Hide when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Hide", true)] static bool CanAddHide() => CanAdd();
        /// <summary>Enables Add Action ▸ Follow when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Follow", true)] static bool CanAddFollow() => CanAdd();
        /// <summary>Enables Add Action ▸ Rest when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Rest", true)] static bool CanAddRest() => CanAdd();
        /// <summary>Enables Add Action ▸ Mate when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Mate", true)] static bool CanAddMate() => CanAdd();
        /// <summary>Enables Add Action ▸ Hunt when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Action/Hunt", true)] static bool CanAddHunt() => CanAdd();
        /// <summary>Enables Add Sense ▸ Energy when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Energy", true)] static bool CanAddEnergy() => CanAdd();
        /// <summary>Enables Add Sense ▸ Stamina when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Stamina", true)] static bool CanAddStamina() => CanAdd();
        /// <summary>Enables Add Sense ▸ Food when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Food", true)] static bool CanAddFood() => CanAdd();
        /// <summary>Enables Add Sense ▸ Predator when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Predator", true)] static bool CanAddPredator() => CanAdd();
        /// <summary>Enables Add Sense ▸ Prey when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Prey", true)] static bool CanAddPrey() => CanAdd();
        /// <summary>Enables Add Sense ▸ Cover when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Cover", true)] static bool CanAddCover() => CanAdd();
        /// <summary>Enables Add Sense ▸ Animal when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Animal", true)] static bool CanAddAnimal() => CanAdd();
        /// <summary>Enables Add Sense ▸ Carcass when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Carcass", true)] static bool CanAddCarcass() => CanAdd();
        /// <summary>Enables Add Sense ▸ Age when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Sense/Age", true)] static bool CanAddAge() => CanAdd();
        /// <summary>Enables Add Gene ▸ Stamina gene when a species is selected.</summary>
        [MenuItem("GameObject/EvoSim/Add Gene/Stamina gene", true)] static bool CanAddStaminaGene() => CanAdd();

        /// <summary>The component of that type on the selection or above it.</summary>
        static T Selected<T>() where T : Component =>
            Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<T>(true) : null;

        /// <summary>A new empty child object of <paramref name="parent"/>.</summary>
        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
