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
