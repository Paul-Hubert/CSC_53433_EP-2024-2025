using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Adds modules to one species of a world being built: what <see cref="WorldBuilder.Species"/> hands to its
    /// <c>configure</c> callback.
    /// </summary>
    /// <remarks>
    /// <para>Every module goes on its own child GameObject, as in a scene: actions under <c>Actions</c>, senses under
    /// <c>Senses</c>, everything else under <c>Life</c>. Order matters (SPEC-03): actions and senses take their order
    /// from the calls, which is the order of the probability rows, of the prompt and of the situation text.</para>
    /// <para>The <see cref="Lab"/> extension methods add whole sets at once: <see cref="Lab.PreyBody"/>,
    /// <see cref="Lab.PreySenses"/>, <see cref="Lab.PreyActionSet"/> and the predator's.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// New().Flat(20, 20).Species("prey", s =&gt; s.Population(2).Action&lt;ProbeAction&gt;().Sense&lt;ProbeSense&gt;())
    /// </code>
    /// </example>
    public sealed class SpeciesSetup
    {
        readonly Transform root;            // the species' GameObject
        Transform actions, senses, life;    // the Actions, Senses and Life children, made on first use

        /// <summary>The Species component being set up.</summary>
        public Species Species { get; }

        /// <summary>The species' own GameObject, to add components by hand.</summary>
        public GameObject GameObject => root.gameObject;

        /// <summary>Puts a Species component on the GameObject, with no founders yet.</summary>
        /// <param name="root">The species' GameObject, under the world's root.</param>
        /// <param name="name">The display name.</param>
        /// <param name="id">The id; null = the name.</param>
        internal SpeciesSetup(Transform root, string name, string id)
        {
            this.root = root;
            Species = root.gameObject.AddComponent<Species>();
            Species.SetNames(id ?? name, name);
            Species.SetInitialPopulation(0);
        }

        /// <summary>Sets how many founders the World creates at initialisation (POP-04); 0 by default.</summary>
        /// <param name="founders">The number of founders, placed at random walkable positions.</param>
        /// <returns>This setup, to chain more calls.</returns>
        public SpeciesSetup Population(int founders)
        {
            Species.SetInitialPopulation(founders);
            return this;
        }

        /// <summary>
        /// Adds an action on its own GameObject, after the actions added so far, with a bound text gene on the same object
        /// (GENE-05) unless <paramref name="withGene"/> is false.
        /// </summary>
        /// <typeparam name="T">The action class.</typeparam>
        /// <param name="name">
        /// The action's name (its GameObject's name), which <see cref="Lab.Choose"/> and the prompt use; null = the class name
        /// without "Action", in lower case (<c>EatAction</c> gives "eat").
        /// </param>
        /// <param name="configure">Called at once with the new action, to set it up; null = defaults.</param>
        /// <param name="founders">The gene's founder sentences, before the neutral "No preference."; null = the neutral one only.</param>
        /// <param name="withGene">False: no gene, so validation warns (V-05) unless the test wants that.</param>
        /// <returns>This setup, to chain more calls.</returns>
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

        /// <summary>Adds a sense on its own GameObject, after the senses added so far: its place in the situation text (SPEC-03).</summary>
        /// <typeparam name="T">The sense class.</typeparam>
        /// <param name="configure">Called at once with the new sense, to set it up; null = defaults.</param>
        /// <param name="name">The GameObject's name; null = the class name without "Sense", in lower case.</param>
        /// <returns>This setup, to chain more calls.</returns>
        public SpeciesSetup Sense<T>(Action<T> configure = null, string name = null) where T : Sense
        {
            senses ??= WorldBuilder.Child(root, "Senses");
            var s = WorldBuilder.Child(senses, name ?? DefaultName(typeof(T).Name, "Sense")).gameObject.AddComponent<T>();
            configure?.Invoke(s);
            return this;
        }

        /// <summary>
        /// Adds any other species module (stat, rule, locomotion, gene, crossover, mutation…) on its own GameObject under <c>Life</c>.
        /// </summary>
        /// <typeparam name="T">The module class.</typeparam>
        /// <param name="configure">Called at once with the new module, to set it up; null = defaults.</param>
        /// <param name="name">The GameObject's name; null = the class name.</param>
        /// <returns>This setup, to chain more calls.</returns>
        public SpeciesSetup Module<T>(Action<T> configure = null, string name = null) where T : SpeciesModule
        {
            life ??= WorldBuilder.Child(root, "Life");
            var m = WorldBuilder.Child(life, name ?? typeof(T).Name).gameObject.AddComponent<T>();
            configure?.Invoke(m);
            return this;
        }

        /// <summary>Adds a component on the species' own GameObject: an <see cref="Edible"/>, a <see cref="SearchRule"/>, a body.</summary>
        /// <typeparam name="T">The component class.</typeparam>
        /// <param name="configure">Called at once with the new component, to set it up; null = defaults.</param>
        /// <returns>This setup, to chain more calls.</returns>
        public SpeciesSetup On<T>(Action<T> configure = null) where T : Component
        {
            var c = root.gameObject.AddComponent<T>();
            configure?.Invoke(c);
            return this;
        }

        /// <summary>Finds the first module of a type added so far, to wire one module to another in a test.</summary>
        /// <typeparam name="T">The component class to find.</typeparam>
        /// <returns>The first one in hierarchy order, or null.</returns>
        public T Get<T>() where T : Component => root.GetComponentInChildren<T>(true);

        /// <summary>Finds a module of a type on the child GameObject with a given name (e.g. the gene of the action "eat").</summary>
        /// <typeparam name="T">The component class to find.</typeparam>
        /// <param name="gameObjectName">The GameObject's name: an action's name, a sense's or module's name.</param>
        /// <returns>The component, or null when no such GameObject carries one.</returns>
        public T Find<T>(string gameObjectName) where T : Component
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
                if (c.gameObject.name == gameObjectName) return c;
            return null;
        }

        /// <summary>The class name without its suffix, in lower case: "EatAction" gives "eat".</summary>
        static string DefaultName(string typeName, string suffix)
        {
            string n = typeName.EndsWith(suffix) ? typeName.Substring(0, typeName.Length - suffix.Length) : typeName;
            return n.ToLowerInvariant();
        }
    }
}
