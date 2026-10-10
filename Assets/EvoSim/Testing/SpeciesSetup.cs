using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>Adds modules to a species being built by <see cref="WorldBuilder"/>.</summary>
    public sealed class SpeciesSetup
    {
        readonly Transform root;
        Transform actions, senses, life;

        public Species Species { get; }
        public GameObject GameObject => root.gameObject;

        internal SpeciesSetup(Transform root, string name, string id)
        {
            this.root = root;
            Species = root.gameObject.AddComponent<Species>();
            Species.SetNames(id ?? name, name);
            Species.SetInitialPopulation(0);
        }

        public SpeciesSetup Population(int founders)
        {
            Species.SetInitialPopulation(founders);
            return this;
        }

        /// <summary>
        /// An action on its own GameObject named <paramref name="name"/> (default: the class name without "Action",
        /// lower case), with a bound text gene unless <paramref name="founders"/> is null.
        /// </summary>
        public SpeciesSetup Action<T>(string name = null, Action<T> configure = null, IEnumerable<string> founders = null,
                                      bool withGene = true) where T : AnimalAction
        {
            actions ??= WorldBuilder.Child(root, "Actions");
            var go = WorldBuilder.Child(actions, name ?? DefaultName(typeof(T).Name, "Action")).gameObject;
            var a = go.AddComponent<T>();
            if (withGene)
            {
                var g = go.AddComponent<TextGene>();
                g.Configure(founders ?? Array.Empty<string>());
            }
            configure?.Invoke(a);
            return this;
        }

        /// <summary>A sense on its own GameObject, in sense order.</summary>
        public SpeciesSetup Sense<T>(Action<T> configure = null, string name = null) where T : Sense
        {
            senses ??= WorldBuilder.Child(root, "Senses");
            var s = WorldBuilder.Child(senses, name ?? DefaultName(typeof(T).Name, "Sense")).gameObject.AddComponent<T>();
            configure?.Invoke(s);
            return this;
        }

        /// <summary>Any other module (stats, rules, locomotion, genes), on its own GameObject under "Life".</summary>
        public SpeciesSetup Module<T>(Action<T> configure = null, string name = null) where T : SpeciesModule
        {
            life ??= WorldBuilder.Child(root, "Life");
            var m = WorldBuilder.Child(life, name ?? typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(m);
            return this;
        }

        /// <summary>A component on the species' own GameObject (an Edible, a Body).</summary>
        public SpeciesSetup On<T>(Action<T> configure = null) where T : Component
        {
            var c = root.gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>The first module of this type built so far (to wire references in tests).</summary>
        public T Get<T>() where T : Component => root.GetComponentInChildren<T>(true);

        static string DefaultName(string typeName, string suffix)
        {
            string n = typeName.EndsWith(suffix) ? typeName.Substring(0, typeName.Length - suffix.Length) : typeName;
            return n.ToLowerInvariant();
        }
    }
}
