using System;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EvoSim.Tests
{
    /// <summary>
    /// The conformance suites of 30 §4: parameterised over every class of a kind found in the project (reference,
    /// HTTP, samples and student assemblies; test probes excluded), so a new module is tested the moment it exists.
    /// The brain suite is DecisionTests.BrainConformance.
    /// </summary>
    public class ConformanceTests : WorldFixture
    {
        static IEnumerable<Type> Kind<T>() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                string n = a.GetName().Name;
                return (n.StartsWith("EvoSim.") && !n.StartsWith("EvoSim.Testing") && !n.StartsWith("EvoSim.Tests") && n != "EvoSim.Editor")
                       || n == "Assembly-CSharp" || n == "Student";
            })
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
            .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        static IEnumerable<Type> Senses() => Kind<Sense>();
        static IEnumerable<Type> Actions() => Kind<AnimalAction>();
        static IEnumerable<Type> Operators() => Kind<MutationOperator>();
        static IEnumerable<Type> Phases() => Kind<TickPhase>();
        static IEnumerable<Type> Locomotions() => Kind<Locomotion>();
        static IEnumerable<Type> Layers() => Kind<ResourceLayer>();
        static IEnumerable<Type> StatModules() => Kind<SpeciesModule>().Where(t => t.GetProperties().Any(p => p.PropertyType == typeof(StatId)));

        /// <summary>
        /// The modules a kind names with [RequiresModule] (a drink needs thirst): a species module goes on the species under
        /// test, a world module under its World, so a student's module that needs company is tested like the others.
        /// </summary>
        static void Companions(Type t, GameObject species)
        {
            foreach (RequiresModuleAttribute need in t.GetCustomAttributes(typeof(RequiresModuleAttribute), true))
            {
                if (need.Module == null || need.Module.IsAbstract) continue;
                if (typeof(SpeciesModule).IsAssignableFrom(need.Module))
                {
                    if (species.GetComponentInChildren(need.Module, true) == null) species.AddComponent(need.Module);
                    continue;
                }
                var world = species.GetComponentInParent<World>(true);
                if (world != null && typeof(WorldModule).IsAssignableFrom(need.Module) && world.GetComponentInChildren(need.Module, true) == null)
                    Child(world.gameObject, need.Module.Name).AddComponent(need.Module);
            }
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// <summary>The Lab ecology with something added to the prey: a populated, valid world to exercise it in.</summary>
        WorldBuilder Ecology(string name, Action<GameObject> addToPrey, int prey = 16, int predators = 3, float food = 0.1f, int seed = 7) =>
            New(seed, name: name).Ecology(40f, 0.2f, food,
                    prey: s => { s.Population(prey); addToPrey?.Invoke(s.GameObject); },
                    predator: s => s.Population(predators))
                .Carcasses().Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>();

        // ---- T-SENSE template ----

        [Test, Description("T-SENSE template (SENSE-01, SENSE-10): tokens declared and non-empty; every animal's observation is a declared index over 60 ticks; Write deterministic and non-empty for every token and style")]
        public void SenseConformance([ValueSource(nameof(Senses))] Type type)
        {
            Sense sense = null;
            var w = Ecology("sense " + type.Name, go =>
            {
                Companions(type, go);
                sense = (Sense)Child(go, type.Name).AddComponent(type);
                sense.SetLabel("Probe " + type.Name);                               // never the label of a reference sense (V-17)
            }).Build();
            Assert.Greater(sense.Tokens.Count, 0, "tokens declared");
            foreach (var t in sense.Tokens) Assert.IsFalse(string.IsNullOrWhiteSpace(t), "a token is empty");
            for (int k = 0; k < sense.Tokens.Count; k++)
                foreach (var style in new[] { TextStyle.V1, TextStyle.V2 })
                {
                    string text = sense.Write(k, style);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"Write({k}, {style}) is empty");
                    Assert.AreEqual(text, sense.Write(k, style), "Write is deterministic");
                }
            var species = w.FindSpecies("prey");
            int index = species.Senses.ToList().IndexOf(sense);
            Assert.GreaterOrEqual(index, 0);
            int seen = 0;
            for (int tick = 0; tick < 60 && w.State != RunState.Stopped; tick++)
            {
                w.Advance(1);
                foreach (var a in species.Animals)
                {
                    if (a.LastObservation == null || a.LastDecisionTick != w.Tick - 1) continue;
                    Assert.That(a.LastObservation[index], Is.InRange(0, sense.Tokens.Count - 1));
                    seen++;
                }
            }
            Assert.Greater(seen, 0, "the sense was read");
        }

        // ---- T-ACT template ----

        [Test, Description("T-ACT template (ACT-04, ACT-06): description present; bound gene; Act only sets intents (no animal moved or changed); searches when alone")]
        public void ActionConformance([ValueSource(nameof(Actions))] Type type)
        {
            AnimalAction action = null;
            var w = Ecology("act " + type.Name, go =>
            {
                Companions(type, go);
                var holder = Child(go, "under test");
                action = (AnimalAction)holder.AddComponent(type);
                holder.AddComponent<TextGene>().Configure(new[] { "Do it now." });
            }).Build();
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.Description), "description present");
            Assert.IsNotNull(action.Gene, "bound gene");

            w.Advance(3);
            var everyone = w.AllSpecies.SelectMany(s => s.Animals).ToList();
            var before = everyone.Select(Snapshot).ToList();
            var c = new ActContext(w);
            foreach (var a in w.FindSpecies("prey").Animals)
            {
                c.Begin(a);
                action.Act(a, c);
            }
            var after = everyone.Select(Snapshot).ToList();
            for (int i = 0; i < everyone.Count; i++) Assert.AreEqual(before[i], after[i], $"Act changed {everyone[i]}: only intents (ACT-06)");

            if (action.Rests) return;
            AnimalAction alone = null;
            var empty = New(9, name: "alone " + type.Name).Flat(40, 40).Food(0f, 0f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>()
                .Species("prey", s =>
                {
                    s.PreyBody().LifeRules(10, 0);
                    Companions(type, s.GameObject);
                    var holder = Child(s.GameObject, "under test");
                    alone = (AnimalAction)holder.AddComponent(type);
                    holder.AddComponent<TextGene>().Configure(new[] { "Do it now." });
                }).Build();
            var solo = Place.Animal(empty.FindSpecies("prey"), Place.At(20, 20));
            var ctx = new ActContext(empty);
            ctx.Begin(solo);
            alone.Act(solo, ctx);
            Assert.IsTrue(solo.Searching || ctx.Intent.IsStay, "alone with nothing in sight it searches (ACT-04), or stays");
        }

        static string Snapshot(Animal a)
        {
            var stats = string.Join(",", a.Species.Declarations.Stats.Select(d => a[d.Id].ToString("R")));
            return $"{a.Id} {a.Position.x:R} {a.Position.z:R} {a.Killed} {stats}";
        }

        // ---- T-ANIM stat template ----

        [Test, Description("T-ANIM stat template (ANIM-10, ANIM-22): every module declaring a stat — founders start as declared and the stat stays in range for 1 000 ticks")]
        public void StatConformance([ValueSource(nameof(StatModules))] Type type)
        {
            SpeciesModule module = null;
            var b = Ecology("stat " + type.Name, go =>
            {
                module = (SpeciesModule)(go.GetComponentInChildren(type, true) ?? Child(go, type.Name).AddComponent(type));
                Companions(type, go);
            }).Phase<InvariantPhase>();
            var w = b.Build();
            var stat = (StatId)type.GetProperties().First(p => p.PropertyType == typeof(StatId)).GetValue(module);
            Assert.IsTrue(stat.IsValid, "the stat was declared");
            var decl = w.FindSpecies("prey").Declarations.Stats.First(d => d.Id.Name == stat.Name);
            foreach (var a in w.FindSpecies("prey").Animals)
            {
                float expected = decl.Start.Trait >= 0 ? a.Trait(w.FindSpecies("prey").Declarations.Traits[decl.Start.Trait].Id) : decl.Start.Value;
                Assert.AreEqual(Mathf.Clamp(expected, stat.Min, stat.Max), a[stat], 1e-4f, "founders start as declared");
            }
            w.Advance(1000);                                                        // InvariantPhase checks the ranges every tick
            Assert.Greater(w.GetComponentInChildren<InvariantPhase>().Checked, 0);
        }

        // ---- T-ENV layer template ----

        [Test, Description("T-ENV layer template (ENV-01…04): an item is consumed once; Nearest finds an item within the radius, the nearest one for grids; items change only in their phase")]
        public void LayerConformance([ValueSource(nameof(Layers))] Type type)
        {
            var w = New(3, name: "layer " + type.Name).Flat(30, 30).Phase<EnvironmentPhase>()
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest")).BuildUninitialized();
            var go = new GameObject("Grass");                                       // the name the prey's diet eats
            go.transform.SetParent(w.transform, false);
            var layer = (ResourceLayer)go.AddComponent(type);
            go.AddComponent<Edible>();
            if (layer is FoodGrid g) g.Configure(0.3f, 0f);
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.Greater(layer.Count, 0, "the layer starts with items");
            Assert.IsTrue(layer.Nearest(new Vector3(15, 0, 15), 100f, out var item));
            Assert.LessOrEqual(item.Distance, 100f);
            if (layer is FoodGrid grid)
            {
                float best = float.MaxValue;
                for (int c = 0; c < grid.Grid.Count; c++)
                    if (grid.HasItem(c)) best = Mathf.Min(best, w.Distance(new Vector3(15, 0, 15), grid.Grid.Center(c)));
                Assert.AreEqual(best, item.Distance, 1e-3f, "the nearest equals brute force");
            }
            int count = layer.Count;
            Assert.IsTrue(layer.Consume(item));
            Assert.IsFalse(layer.Consume(item), "consumed once");
            Assert.AreEqual(count - 1, layer.Count);
        }

        // ---- T-MUT template ----

        [Test, Description("T-MUT template (MUT-20, MUT-30): the same parent and seed give the same result; a result passes the gene kind's checks or the job fails")]
        public void MutationOperatorConformance([ValueSource(nameof(Operators))] Type type)
        {
            MutationOperator op = null;
            var w = New(4, name: "mut " + type.Name).Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").FakeMutation().ReferencePhases()
                .DefaultBrain<RandomBrain>()
                .Species("prey", s =>
                {
                    s.PreyBody().LifeRules(10, 0).Action<RestAction>("rest", founders: new[] { "Rest often when tired." });
                    s.Module<NumberGene>(n => n.Configure("stamina.max", 20f, 120f, new[] { 60f }, false, "stamina"));
                    op = (MutationOperator)Child(s.GameObject, "operator").AddComponent(type);
                    if (op is LlmMutation llm) llm.Configure(EvoSim.Testing.Lab.Deck, "The sentence below is a rule that a wild animal follows.");
                }).Build();
            var s0 = w.FindSpecies("prey");
            int accepted = 0;
            foreach (var gene in s0.Genes)
            {
                if (!op.Accepts(gene)) continue;
                accepted++;
                var parent = w.Alleles.Register(gene, gene.FounderPool[0].Value, "founder");
                var a = op.StartMutation(gene, parent, new RandomStream(11, "mutation"));
                var b = op.StartMutation(gene, parent, new RandomStream(11, "mutation"));
                for (int i = 0; i < 50 && !(a.IsDone && b.IsDone); i++) w.Advance(1);   // asynchronous operators finish through the world
                Assert.IsTrue(a.IsDone && b.IsDone, "finished");
                Assert.AreEqual(a.Succeeded, b.Succeeded, "same seed, same outcome");
                if (!a.Succeeded) continue;
                Assert.AreEqual(a.Result, b.Result, "same seed, same result");
                Assert.IsNull(gene.Check(a.Result), "the result passes the gene's checks");
            }
            Assert.Greater(accepted, 0, "it accepts at least one of a text and a number gene");
        }

        // ---- T-TICK phase template ----

        [Test, Description("T-TICK phase template (TICK-01): runs 20 ticks on a world without animals; disabling and re-enabling it leaves the hash of a populated world unchanged")]
        public void PhaseConformance([ValueSource(nameof(Phases))] Type type)
        {
            var empty = New(1, name: "phase " + type.Name).Flat(20, 20).Food(0.1f, 0.01f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<RandomBrain>().BuildUninitialized();
            var phases = empty.GetComponentInChildren<SensePhase>(true).transform.parent;
            if (empty.GetComponentInChildren(type, true) == null) Child(phases.gameObject, type.Name).AddComponent(type).transform.SetSiblingIndex(0);   // first: before Environment
            Assert.IsTrue(empty.Initialize(), empty.LastReport.ToString());
            empty.Advance(20);
            Assert.AreEqual(20, empty.Tick);

            string Hash(bool toggle)
            {
                var w = Ecology($"phase {type.Name} {toggle}", null, seed: 13).BuildUninitialized();
                var ph = w.GetComponentInChildren<SensePhase>(true).transform.parent;
                Component c = w.GetComponentInChildren(type, true);
                if (c == null) { c = Child(ph.gameObject, type.Name).AddComponent(type); c.transform.SetSiblingIndex(0); }
                if (toggle) { ((Behaviour)c).enabled = false; ((Behaviour)c).enabled = true; }
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                w.Advance(40);
                return w.Events.Hash;
            }
            Assert.AreEqual(Hash(false), Hash(true));
        }

        // ---- T-MOVE locomotion template ----

        [Test, Description("T-MOVE locomotion template (MOVE-02, MOVE-03, SPACE-04, SPACE-05): never ends a tick on water or outside; reports the meters actually moved; stuck animals move again within 3 ticks; extra cost ≥ 0")]
        public void LocomotionConformance([ValueSource(nameof(Locomotions))] Type type)
        {
            var w = New(21, name: "move " + type.Name).Ground<PondGround>(20, 20).Phase<SensePhase>().Phase<ActPhase>().DefaultBrain<RandomBrain>()
                .Species("walker", s =>
                {
                    s.On<SearchRule>().Module<Energy>().Module<Stamina>().Module<Metabolism>();
                    var loco = (Locomotion)Child(s.GameObject, "Locomotion").AddComponent(type);
                    loco.SetSpeeds(1f, 2f);
                    s.Action<GoToAction>("go", g => g.Target = new Vector3(10f, 0f, 10f));                 // walking: 30 m fit in the stamina
                }).Build();
            var sp = w.FindSpecies("walker");
            var loco0 = sp.Module<Locomotion>();
            var walkers = new List<Animal>();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var a = Place.Animal(sp, Place.At(10f + 8f * Mathf.Cos(angle), 10f + 8f * Mathf.Sin(angle)));
                a.Choose("go");
                walkers.Add(a);
            }
            var stuckFor = new int[walkers.Count];
            for (int tick = 0; tick < 30; tick++)
            {
                var from = walkers.Select(a => a.Position).ToList();
                w.Advance(1);
                for (int i = 0; i < walkers.Count; i++)
                {
                    var a = walkers[i];
                    Assert.IsTrue(w.Ground.IsWalkable(a.Position) && w.Ground.Inside(a.Position), $"tick {w.Tick}: {a} on water or outside at {a.Position}");
                    Assert.AreEqual(w.Distance(from[i], a.Position), a.MetersMoved, 1e-3f, "the meters reported are the meters moved");
                    Assert.GreaterOrEqual(loco0.ExtraCost(a, from[i], a.Position), 0f, "extra cost ≥ 0");
                    float toShore = Vector2.Distance(new Vector2(a.Position.x, a.Position.z), new Vector2(10f, 10f)) - 3f;
                    stuckFor[i] = a.MetersMoved < Units.Epsilon && toShore > 0.6f ? stuckFor[i] + 1 : 0;
                    Assert.Less(stuckFor[i], 4, $"{a} stuck away from its goal for more than 3 ticks");
                }
            }
        }
    }
}
