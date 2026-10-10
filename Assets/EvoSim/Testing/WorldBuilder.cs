using System;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Builds a world in code, no scene needed (30 §2):
    /// <c>new WorldBuilder(seed).Flat(20, 20).Species("prey", s => s.Action&lt;RestAction&gt;()).Build()</c>.
    /// The tree is built inactive and activated at Build, so no module runs before the World initialises it.
    /// </summary>
    public sealed class WorldBuilder : IDisposable
    {
        readonly Transform phases, brains, environment;

        public GameObject Root { get; }
        public World World { get; }

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

        /// <summary>A flat ground of this size, in meters.</summary>
        public WorldBuilder Flat(float width, float depth)
        {
            var g = Child(Root.transform, "Ground").gameObject.AddComponent<FlatGround>();
            g.SetSize(width, depth);
            g.transform.SetSiblingIndex(0);
            return this;
        }

        /// <summary>A flat ground of a given subclass (e.g. one that replaces the distance function).</summary>
        public WorldBuilder Ground<T>(float width, float depth) where T : FlatGround
        {
            var g = Child(Root.transform, "Ground").gameObject.AddComponent<T>();
            g.SetSize(width, depth);
            g.transform.SetSiblingIndex(0);
            return this;
        }

        /// <summary>A food layer with an Edible (grazed, <paramref name="energy"/> per item).</summary>
        public WorldBuilder Food(float initialFraction = 0.1f, float regrowP = 0.0015f, string name = "Grass", float energy = 25f,
                                 bool hungryCover = true)
        {
            return Service<FoodGrid>(name, g =>
            {
                g.Configure(initialFraction, regrowP, hungryCover);
                g.gameObject.AddComponent<Edible>().Configure(EatMethod.Graze, energy);
            });
        }

        /// <summary>A cover layer covering this share of walkable ground.</summary>
        public WorldBuilder Cover(float fraction = 0.2f, string name = "Thickets") =>
            Service<CoverLayer>(name, c => c.SetFraction(fraction));

        /// <summary>The carcass system.</summary>
        public WorldBuilder Carcasses(string name = "Carcasses") => Service<CarcassSystem>(name);

        /// <summary>Changes the World's settings.</summary>
        public WorldBuilder Configure(Action<World> configure)
        {
            configure(World);
            return this;
        }

        /// <summary>A service under Environment (layers, entities) or Brains (brains, caches, mutators).</summary>
        public WorldBuilder Service<T>(string name = null, Action<T> configure = null) where T : WorldService
        {
            var parent = typeof(Brain).IsAssignableFrom(typeof(T)) || typeof(T).Name.Contains("Mutator") || typeof(T).Name.Contains("Cache")
                ? brains : environment;
            var c = Child(parent, name ?? typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>A brain under Brains, made the World's default brain.</summary>
        public WorldBuilder DefaultBrain<T>(Action<T> configure = null, string name = null) where T : Brain
        {
            return Service<T>(name ?? typeof(T).Name, b =>
            {
                configure?.Invoke(b);
                World.DefaultBrain = b;
            });
        }

        /// <summary>The brain of this name (under Brains), or the first of its type.</summary>
        public T Get<T>() where T : Component => Root.GetComponentInChildren<T>(true);

        /// <summary>A phase, appended after the existing ones (TICK-01).</summary>
        public WorldBuilder Phase<T>(Action<T> configure = null) where T : TickPhase
        {
            var c = Child(phases, typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>A species with its modules.</summary>
        public WorldBuilder Species(string name, Action<SpeciesSetup> configure = null, string id = null)
        {
            var setup = new SpeciesSetup(Child(Root.transform, name), name, id);
            configure?.Invoke(setup);
            return this;
        }

        /// <summary>Activates the tree and initialises the world; throws with the validation report if it fails.</summary>
        public World Build()
        {
            Root.SetActive(true);
            if (!World.Initialize())
                throw new InvalidOperationException("The test world failed validation:\n" + World.LastReport);
            return World;
        }

        /// <summary>Activates the tree without initialising (to test validation).</summary>
        public World BuildUninitialized()
        {
            Root.SetActive(true);
            return World;
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
        }

        internal static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
