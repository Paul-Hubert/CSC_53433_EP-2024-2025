using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The neural prey and predator templates (plan M16 D7, §5.7): a copy of a JEV species that keeps every body and life
    /// setting and gets the neural controller instead of its own.
    /// </summary>
    /// <remarks>
    /// <para>Kept from the JEV species: the stats, locomotion, diet, edible energy, mating rule (with its sexual switch),
    /// litter, incubation, death rules, cap, floor, initial population, and its prompt texts (which the network never
    /// reads). Removed: its senses, actions, genes, mutation, crossover and search rule. Added: a <see cref="RayFanSense"/>
    /// (prey or predator kinds) and an <see cref="InteroceptionSense"/>; the actions <c>left, right, walk, run, stop</c>
    /// with the contacts the diet allows; a <see cref="NeuralGenomeGene"/> labelled <c>brain</c>; <see cref="NeuralMutation"/>,
    /// <see cref="NeuronCrossover"/>, <see cref="HungerClock"/> and <see cref="NeuroStats"/>; a decision every tick; and the
    /// World's <see cref="NeuralBrain"/>, made under <c>Brains/Neural</c> if the World has none.</para>
    /// <para>The pair is registered in the World's <see cref="MatchedPairs"/> (made under <c>Comparison</c> if missing), so
    /// validation reports any later difference (V-78). Other species' diets are not changed: <see cref="Retarget"/> does it
    /// when a world wants it (§5.7). The editor menu <c>GameObject ▸ EvoSim ▸ New Neural Species</c> calls these with undo.</para>
    /// <para>Call them on a World that is not running. In edit mode the removed components are destroyed at once.</para>
    /// </remarks>
    public static class NeuralTemplates
    {
        /// <summary>The neural prey's id (plan §5.7).</summary>
        public const string PreyId = "prey-nn";
        /// <summary>The neural predator's id (plan §5.7).</summary>
        public const string PredatorId = "predator-nn";
        /// <summary>The neural prey's display name: what prompts and placeholders call it (<c>{neural prey.Stamina.max}</c>).</summary>
        public const string PreyName = "neural prey";
        /// <summary>The neural predator's display name.</summary>
        public const string PredatorName = "neural predator";
        /// <summary>The neural actions, in the order the template builds them (§5.5).</summary>
        public static readonly string[] ActionNames = { "left", "right", "walk", "run", "stop" };

        /// <summary>
        /// <c>NeuroPrey</c>: a copy of <paramref name="jev"/> with the neural controller, the prey ray kinds and the graze
        /// contact.
        /// </summary>
        /// <param name="jev">The JEV prey to match (the reference <c>prey</c>).</param>
        /// <param name="id">The new species' id.</param>
        /// <param name="displayName">The new species' display name.</param>
        /// <param name="body">The body prefab (<c>NeuroPreyBody</c>), or null to keep the JEV species' body.</param>
        /// <returns>The new species, a sibling of <paramref name="jev"/>.</returns>
        public static Species Prey(Species jev, string id = PreyId, string displayName = PreyName, GameObject body = null) =>
            Build(jev, id, displayName, "Neuro Prey", RayFanSense.PreyKinds, NeuralContacts.Graze, body, new Color(0.3f, 0.75f, 0.85f));

        /// <summary>
        /// <c>NeuroPredator</c>: a copy of <paramref name="jev"/> with the neural controller, the predator ray kinds and the
        /// strike and scavenge contacts. Its diet is the JEV predator's: it strikes the same species.
        /// </summary>
        /// <param name="jev">The JEV predator to match (the reference <c>predator</c>).</param>
        /// <param name="id">The new species' id.</param>
        /// <param name="displayName">The new species' display name.</param>
        /// <param name="body">The body prefab (<c>NeuroPredatorBody</c>), or null to keep the JEV species' body.</param>
        /// <returns>The new species, a sibling of <paramref name="jev"/>.</returns>
        public static Species Predator(Species jev, string id = PredatorId, string displayName = PredatorName, GameObject body = null) =>
            Build(jev, id, displayName, "Neuro Predator", RayFanSense.PredatorKinds, NeuralContacts.Strike | NeuralContacts.Scavenge, body,
                  new Color(0.85f, 0.35f, 0.75f));

        /// <summary>
        /// Replaces a species name in a diet's entries, carcasses included (<c>prey</c> → <c>prey-nn</c>,
        /// <c>carcass:prey</c> → <c>carcass:prey-nn</c>), keeping each entry's energy scale. With <paramref name="keep"/>, the
        /// old entries stay and the new ones are added after them (both preys eaten, as in the 2×2).
        /// </summary>
        /// <param name="diet">The diet to change.</param>
        /// <param name="from">The species id named now.</param>
        /// <param name="to">The species id to name instead.</param>
        /// <param name="keep">True to add the new entries and keep the old ones.</param>
        public static void Retarget(Diet diet, string from, string to, bool keep = false)
        {
            var entries = new List<Diet.Entry>();
            var added = new List<Diet.Entry>();
            foreach (var e in diet.Entries)
            {
                string t = (e.target ?? "").Trim();
                int colon = t.IndexOf(':');
                string kind = colon > 0 ? t.Substring(0, colon + 1) : "", of = colon > 0 ? t.Substring(colon + 1).Trim() : t;
                if (of != from) { entries.Add(e); continue; }
                var renamed = new Diet.Entry(kind + to, e.energyScale);
                if (keep) { entries.Add(e); added.Add(renamed); }
                else entries.Add(renamed);
            }
            entries.AddRange(added);
            diet.Set(entries.ToArray());
        }

        /// <summary>The World's neural brain service, made under <c>Brains/Neural</c> when it has none.</summary>
        /// <param name="world">The World.</param>
        /// <returns>The brain the neural species use.</returns>
        public static NeuralBrain BrainOf(World world)
        {
            var b = world.GetComponentInChildren<NeuralBrain>(true);
            if (b != null) return b;
            return Child(Child(world.gameObject, "Brains"), "Neural").AddComponent<NeuralBrain>();
        }

        /// <summary>The World's <see cref="MatchedPairs"/>, made under <c>Comparison</c> when it has none.</summary>
        /// <param name="world">The World.</param>
        /// <returns>The pairs module.</returns>
        public static MatchedPairs PairsOf(World world)
        {
            var p = world.GetComponentInChildren<MatchedPairs>(true);
            return p != null ? p : Child(world.gameObject, "Comparison").AddComponent<MatchedPairs>();
        }

        /// <summary>Copies the JEV species, strips its controller, adds the neural one, and registers the pair.</summary>
        static Species Build(Species jev, string id, string displayName, string goName, RayHitKind[] kinds, NeuralContacts contacts,
                             GameObject bodyPrefab, Color color)
        {
            var world = jev.GetComponentInParent<World>(true);
            var go = Object.Instantiate(jev.gameObject, jev.transform.parent);
            go.name = goName;
            var s = go.GetComponent<Species>();
            s.SetNames(id, displayName);
            Strip(go);

            var senses = Child(go, "Senses");
            Child(senses, "Rays").AddComponent<RayFanSense>().Configure(12, 360f, kinds);
            Child(senses, "Interoception").AddComponent<InteroceptionSense>();

            var actions = Child(go, "Actions");
            Child(actions, "left").AddComponent<TurnAction>().Configure(false).SetContacts(contacts);
            Child(actions, "right").AddComponent<TurnAction>().Configure(true).SetContacts(contacts);
            Child(actions, "walk").AddComponent<ForwardAction>().Configure(false).SetContacts(contacts);
            Child(actions, "run").AddComponent<ForwardAction>().Configure(true).SetContacts(contacts);
            Child(actions, "stop").AddComponent<StopAction>().SetContacts(contacts);

            Child(go, "Brain").AddComponent<NeuralGenomeGene>().SetLabel("brain");
            Child(go, "Mutation").AddComponent<NeuralMutation>();
            var life = Child(go, "Life");
            life.AddComponent<NeuronCrossover>();
            life.AddComponent<HungerClock>();
            life.AddComponent<NeuroStats>();
            var schedule = go.GetComponentInChildren<DecisionSchedule>(true) ?? life.AddComponent<DecisionSchedule>();
            schedule.Configure(1, false);                                                // a decision every tick (§6.1)

            var b = go.GetComponentInChildren<Body>(true);
            if (b != null) b.Configure(bodyPrefab != null ? bodyPrefab : b.Prefab, color, b.Size);
            if (world != null)
            {
                s.SetBrain(BrainOf(world));
                PairsOf(world).Add(jev.RequestedId, id);
            }
            return s;
        }

        /// <summary>
        /// Removes the JEV controller from the copy: every sense, action, gene, mutation, crossover and search rule, then
        /// the GameObjects left empty (the old <c>Senses</c>, <c>Actions</c>, <c>Mutation</c>).
        /// </summary>
        static void Strip(GameObject go)
        {
            foreach (var m in go.GetComponentsInChildren<SpeciesModule>(true))
                if (m is Sense || m is AnimalAction || m is Gene || m is MutationOperator || m is Crossover || m is SearchRule)
                    Object.DestroyImmediate(m);
            var empty = new List<GameObject>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t != go.transform && t.childCount == 0 && t.GetComponents<Component>().Length == 1) empty.Add(t.gameObject);
            foreach (var e in empty)
            {
                var parent = e.transform.parent;
                Object.DestroyImmediate(e);
                while (parent != null && parent != go.transform && parent.childCount == 0 && parent.GetComponents<Component>().Length == 1)
                {
                    var up = parent.parent;
                    Object.DestroyImmediate(parent.gameObject);
                    parent = up;
                }
            }
        }

        /// <summary>The child of that name, made when missing.</summary>
        static GameObject Child(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
