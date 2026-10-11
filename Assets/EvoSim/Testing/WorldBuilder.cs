using System;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Builds a world in code, with no scene (30 §2): ground, food, cover, services, phases and species, each added by one
    /// chained call, then <see cref="Build"/>.
    /// </summary>
    /// <remarks>
    /// <para>The builder makes the same GameObject tree as a scene: a root with the <see cref="EvoSim.World"/>, then
    /// <c>Environment</c> (layers, entities), <c>Brains</c> (brains, caches, mutators), <c>Phases</c> and one object per
    /// species. The tree stays inactive until <see cref="Build"/>, so no module runs before the World initialises it.</para>
    /// <para>A world has only what you add: no phase runs unless you add it (<see cref="Phase{T}"/>, or all of them with
    /// <see cref="Lab.ReferencePhases"/>), and no species is populated unless you ask (<see cref="SpeciesSetup.Population"/>,
    /// or <see cref="Place.Animal"/> after Build). The <see cref="Lab"/> extension methods add the reference species and
    /// the Lab 1 world in one call.</para>
    /// <para>In a test, start builders with <see cref="WorldFixture.New"/>, which destroys the world afterwards; a builder
    /// made with <c>new</c> must be disposed.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var w = New().Flat(20, 20).Food(0.1f, 0.01f)
    ///     .Phase&lt;SeasonsPhase&gt;(p =&gt; p.Configure(10, 0.2f)).Phase&lt;EnvironmentPhase&gt;()
    ///     .Species("prey", s =&gt; s.PreyBody().Action&lt;RestAction&gt;("rest"))
    ///     .Build();
    /// var grass = w.Service&lt;FoodGrid&gt;();
    /// w.Advance(7);
    /// Assert.AreEqual(0.2f, grass.RegrowFactor, "winter");
    /// </code>
    /// </example>
    public sealed class WorldBuilder : IDisposable
    {
        readonly Transform phases, brains, environment;   // the three standard children of the root, made by the constructor

        /// <summary>The root GameObject of the world being built (inactive until <see cref="Build"/>).</summary>
        public GameObject Root { get; }

        /// <summary>The World component on <see cref="Root"/>: set its options here or with <see cref="Configure"/>.</summary>
        public World World { get; }

        /// <summary>Starts an empty world: a root with its World and the Environment, Brains and Phases objects.</summary>
        /// <param name="seed">The world's seed: the same seed gives the same run (RAND-10).</param>
        /// <param name="waitMode">How a tick waits for late answers; Freeze (the default) blocks, as tests need.</param>
        /// <param name="name">The name of the root GameObject; give each world of a test its own.</param>
        /// <remarks>
        /// The World doesn't start on Play (<c>StartOnPlay</c> is off): the test drives it with <see cref="World.Advance"/>.
        /// </remarks>
        public WorldBuilder(int seed = 1234, WaitMode waitMode = WaitMode.Freeze, string name = "Test World")
        {
            Root = new GameObject(name);
            Root.SetActive(false);
            World = Root.AddComponent<World>();
            World.StartOnPlay = false;
            World.Seed = seed;
            World.WaitMode = waitMode;
            environment = Child(Root.transform, "Environment");
            brains = Child(Root.transform, "Brains");
            phases = Child(Root.transform, "Phases");
        }

        /// <summary>Adds a flat ground, a rectangle starting at the origin (x from 0 to width, z from 0 to depth).</summary>
        /// <param name="width">The size along x, in meters (at least 1).</param>
        /// <param name="depth">The size along z, in meters (at least 1).</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Flat(float width, float depth)
        {
            var g = Child(Root.transform, "Ground").gameObject.AddComponent<FlatGround>();
            g.SetSize(width, depth);
            g.transform.SetSiblingIndex(0);     // the ground comes first: layers are laid on it
            return this;
        }

        /// <summary>
        /// Adds a flat ground of a subclass of your own: one that replaces the distance function, adds a pond or a ramp
        /// (<see cref="DoubleDistanceGround"/>, <see cref="PondGround"/>, <see cref="RampGround"/>).
        /// </summary>
        /// <typeparam name="T">The ground class to add.</typeparam>
        /// <param name="width">The size along x, in meters (at least 1).</param>
        /// <param name="depth">The size along z, in meters (at least 1).</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Ground<T>(float width, float depth) where T : FlatGround
        {
            var g = Child(Root.transform, "Ground").gameObject.AddComponent<T>();
            g.SetSize(width, depth);
            g.transform.SetSiblingIndex(0);
            return this;
        }

        /// <summary>
        /// Adds a food layer (a <see cref="FoodGrid"/> under Environment) whose items are grazed for <paramref name="energy"/>
        /// points each.
        /// </summary>
        /// <param name="initialFraction">The share of eligible cells holding an item at the start, 0 to 1 (reference 0.1).</param>
        /// <param name="regrowP">
        /// The chance per empty eligible cell per tick of growing an item (reference 0.0015); 0 = no regrowth.
        /// </param>
        /// <param name="name">The layer's name, which diets and senses use (reference "Grass").</param>
        /// <param name="energy">The energy one item gives, in points (reference 25).</param>
        /// <param name="hungryCover">True: no item in cover, at the start or later (ENV-04).</param>
        /// <returns>This builder, to chain more calls.</returns>
        /// <remarks><c>Food(0f, 0f)</c> makes an empty layer that never regrows: place items with <see cref="Place.Food"/>.</remarks>
        public WorldBuilder Food(float initialFraction = 0.1f, float regrowP = 0.0015f, string name = "Grass", float energy = 25f,
                                 bool hungryCover = true)
        {
            return Service<FoodGrid>(name, g =>
            {
                g.Configure(initialFraction, regrowP, hungryCover);
                g.gameObject.AddComponent<Edible>().Configure(EatMethod.Graze, energy);
            });
        }

        /// <summary>Adds a cover layer (a <see cref="CoverLayer"/>, thickets that hide animals from their hunters, ENV-11).</summary>
        /// <param name="fraction">The share of walkable ground in cover, 0 to 1 (reference 0.2).</param>
        /// <param name="name">The layer's name.</param>
        /// <returns>This builder, to chain more calls.</returns>
        /// <remarks>Put cover on an exact cell with <see cref="Place.Cover"/>.</remarks>
        public WorldBuilder Cover(float fraction = 0.2f, string name = "Thickets") =>
            Service<CoverLayer>(name, c => c.SetFraction(fraction));

        /// <summary>Adds the carcass system: killed animals leave carcasses that hunters can scavenge (ENV-20).</summary>
        /// <param name="name">The service's name.</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Carcasses(string name = "Carcasses") => Service<CarcassSystem>(name);

        /// <summary>Changes the World's settings now: text style, decision period, tick limit…</summary>
        /// <param name="configure">Called at once with the World, e.g. <c>w =&gt; w.TextStyle = TextStyle.V2</c>.</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Configure(Action<World> configure)
        {
            configure(World);
            return this;
        }

        /// <summary>
        /// Adds a world service on its own GameObject: under Brains for brains, mutators and caches (by type name), under
        /// Environment for everything else (layers, entity systems, covers, recorders).
        /// </summary>
        /// <typeparam name="T">The service class to add.</typeparam>
        /// <param name="name">The GameObject's name, which is also the service's name; null = the class name.</param>
        /// <param name="configure">Called at once with the new component, to set it up; null = defaults.</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Service<T>(string name = null, Action<T> configure = null) where T : WorldService
        {
            var parent = typeof(Brain).IsAssignableFrom(typeof(T)) || typeof(T).Name.Contains("Mutator") || typeof(T).Name.Contains("Cache")
                ? brains : environment;
            var c = Child(parent, name ?? typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>Adds a brain under Brains and makes it the World's default brain (DEC-13).</summary>
        /// <typeparam name="T">The brain class: <see cref="RandomBrain"/>, <see cref="ScriptedBrain"/>, an HTTP brain, yours.</typeparam>
        /// <param name="configure">
        /// Called at once with the new brain, e.g. to set a <see cref="ScriptedBrain.Policy"/> or keep a reference to it.
        /// </param>
        /// <param name="name">The GameObject's name; null = the class name.</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder DefaultBrain<T>(Action<T> configure = null, string name = null) where T : Brain
        {
            return Service<T>(name ?? typeof(T).Name, b =>
            {
                configure?.Invoke(b);
                World.DefaultBrain = b;
            });
        }

        /// <summary>Finds the first component of a type anywhere in the world being built, inactive objects included.</summary>
        /// <typeparam name="T">The component class to find (a brain, a service, a module…).</typeparam>
        /// <returns>The first one in hierarchy order, or null.</returns>
        public T Get<T>() where T : Component => Root.GetComponentInChildren<T>(true);

        /// <summary>Adds a phase under Phases, after the phases added so far: phases run in hierarchy order (TICK-01).</summary>
        /// <typeparam name="T">The phase class to add.</typeparam>
        /// <param name="configure">Called at once with the new phase, to set it up; null = defaults.</param>
        /// <returns>This builder, to chain more calls.</returns>
        public WorldBuilder Phase<T>(Action<T> configure = null) where T : TickPhase
        {
            var c = Child(phases, typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>Adds a species, on its own GameObject under the root, with the modules <paramref name="configure"/> adds.</summary>
        /// <param name="name">The species' display name, and its id unless <paramref name="id"/> is given.</param>
        /// <param name="configure">Adds the species' modules through a <see cref="SpeciesSetup"/>; null = an empty species.</param>
        /// <param name="id">The species' id when it differs from its name; null = the name.</param>
        /// <returns>This builder, to chain more calls.</returns>
        /// <remarks>The species starts with no founders: set some with <see cref="SpeciesSetup.Population"/>.</remarks>
        public WorldBuilder Species(string name, Action<SpeciesSetup> configure = null, string id = null)
        {
            var setup = new SpeciesSetup(Child(Root.transform, name), name, id);
            configure?.Invoke(setup);
            return this;
        }

        /// <summary>Activates the tree and initialises the world: discovery, validation, founders.</summary>
        /// <returns>The initialised World, ready for <see cref="World.Advance"/>.</returns>
        /// <exception cref="InvalidOperationException">The world failed validation; the message holds the validation report.</exception>
        public World Build()
        {
            Root.SetActive(true);
            if (!World.Initialize())
                throw new InvalidOperationException("The test world failed validation:\n" + World.LastReport);
            return World;
        }

        /// <summary>Activates the tree without initialising the world, to test validation or change the tree first.</summary>
        /// <returns>The World, not initialised: call <see cref="World.Initialize"/> or <c>Prepare</c> yourself.</returns>
        public World BuildUninitialized()
        {
            Root.SetActive(true);
            return World;
        }

        /// <summary>Destroys the world's GameObjects. <see cref="WorldFixture"/> calls it after each test.</summary>
        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
        }

        /// <summary>Makes an empty child GameObject.</summary>
        /// <param name="parent">Where the child goes.</param>
        /// <param name="name">The child's name.</param>
        /// <returns>The child's transform.</returns>
        internal static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
