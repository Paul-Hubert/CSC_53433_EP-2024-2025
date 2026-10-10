using System;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Decisions, brains, memo, cache and failures (08 §1–§6).</summary>
    public class DecisionTests : WorldFixture
    {
        WorldBuilder Lab(int seed = 1, int prey = 0, int predators = 0) =>
            New(seed).Ecology(48f, 0.2f, 0.1f, prey: s => s.Population(prey), predator: s => s.Population(predators)).ReferencePhases();

        [Test, Description("T-DEC-01 (DEC-01, DEC-03): with period 4 every non-busy animal decides on 0, 4, 8; a newcomer on tick 5 decides on tick 6; flags cleared")]
        public void DecisionTicks()
        {
            var b = Lab(prey: 6).DefaultBrain<RandomBrain>();
            var w = b.Build();
            var prey = w.FindSpecies("prey");
            var decided = new List<(int tick, int id)>();
            var busy = prey.Animals[0];
            for (int t = 0; t < 9; t++)
            {
                if (t == 1) busy.BusyTicks = 2;
                w.Advance(1);
                foreach (var d in w.Decisions.Due) decided.Add((w.Decisions.Tick, d.Animal.Id));
                if (t == 5) { var late = Place.Animal(prey, Place.At(10, 10)); late.Searching = true; late.BredThisPeriod = true; }   // born during tick 5
            }
            int[] PerTick(int tick) => decided.Where(x => x.tick == tick).Select(x => x.id).ToArray();
            Assert.AreEqual(6, PerTick(0).Length);
            Assert.AreEqual(0, PerTick(1).Length + PerTick(2).Length, "nobody without a reason");
            CollectionAssert.AreEqual(new[] { busy.Id }, PerTick(3), "its busy state ended on tick 2: it decides at the start of tick 3");
            Assert.AreEqual(1, PerTick(6).Length, "the newcomer of tick 5 decides at the start of tick 6");
            Assert.AreEqual(7, PerTick(8).Length);
            var newcomer = prey.Animals.Last();
            Assert.IsFalse(newcomer.BredThisPeriod, "flags cleared at the decision (DEC-03)");
        }

        [Test, Description("T-DEC-02 (DEC-10): a query has the species, the brain-visible genes in locus order with labels, the observation and the situation text in the world's style")]
        public void QueriesCarryWhatTheBrainReads()
        {
            var brain = (ScriptedBrain)null;
            var w = Lab(prey: 1).DefaultBrain<ScriptedBrain>(x => brain = x).Configure(x => x.TextStyle = TextStyle.V2).Build();
            w.Advance(1);
            var q = brain.Seen.Single();
            Assert.AreEqual("prey", q.Species.Id);
            CollectionAssert.AreEqual(Testing.Lab.PreyActions, q.Genes.Select(g => g.Key).ToArray(), "labels in locus order");
            var a = w.FindSpecies("prey").Animals[0];
            CollectionAssert.AreEqual(a.Genome.Alleles.Select(x => x.Text).ToArray(), q.Genes.Select(g => g.Value).ToArray());
            Assert.AreEqual(a.LastObservation, q.Observation);
            Assert.AreEqual(w.FindSpecies("prey").Describe(q.Observation, TextStyle.V2), q.Situation);
            StringAssert.StartsWith("I ", q.Situation, "V2, the world's style");
            Assert.AreEqual(0, q.Attachments.Count);
        }

        static IEnumerable<Type> BrainTypes() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.GetName().Name.StartsWith("EvoSim"))
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(Brain).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace != null && !t.FullName.Contains("Http"))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        [Test, Description("T-DEC-03 brain template (DEC-11, DEC-12): rows of the species' length, entries ≥ 0, sum 1 ± 1e-6, in query order; the same query twice → the same row")]
        public void BrainConformance([ValueSource(nameof(BrainTypes))] Type type)
        {
            var b = Lab(prey: 0);
            var go = new GameObject(type.Name);
            go.transform.SetParent(b.Root.transform.Find("Brains"), false);
            var brain = (Brain)go.AddComponent(type);
            b.World.DefaultBrain = brain;
            var w = b.Build();
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var rng = new RandomStream(3, "queries");
            var queries = new List<DecisionQuery>();
            for (int i = 0; i < 20; i++)
            {
                var s = i % 2 == 0 ? prey : predator;
                var a = Place.Animal(s, Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f)));
                w.Space.Rebuild();
                var o = s.Observe(a, new SenseContext(w));
                queries.Add(new DecisionQuery(s, new List<KeyValuePair<string, string>>(), o, s.Describe(o, TextStyle.V1), TextStyle.V1, a.Genome.BrainKey));
            }
            var answer = brain.Ask(queries);
            answer.Pending.Wait();
            var again = brain.Ask(queries);
            again.Pending.Wait();
            for (int i = 0; i < queries.Count; i++)
            {
                Assert.IsNull(BrainAnswer.Check(answer.Row(i), queries[i].Species.Actions.Count), $"{type.Name} row {i}");
                CollectionAssert.AreEqual(answer.Row(i), again.Row(i), "deterministic");
            }
        }

        [Test, Description("T-DEC-04 (DEC-13): two species with different brains, one that can't mix species → each brain gets only its species, one batch per species")]
        public void BrainsPerSpecies()
        {
            ScriptedBrain mixed = null, single = null;
            var b = Lab(prey: 5, predators: 3);
            b.Service<ScriptedBrain>("Mixed", x => { x.BrainId = "mixed"; mixed = x; });
            b.Service<ScriptedBrain>("Single", x => { x.BrainId = "single"; x.MixesSpecies = false; single = x; });
            b.Species("deer", s => s.PreyBody().PreySenses().PreyActionSet().Population(4));
            var w = b.BuildUninitialized();
            foreach (var s in w.GetComponentsInChildren<Species>())
                s.SetBrain(s.RequestedId == "predator" ? (Brain)mixed : single);
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            w.Advance(1);
            Assert.IsTrue(mixed.Seen.All(q => q.Species.Id == "predator"));
            Assert.IsTrue(single.Seen.All(q => q.Species.Id != "predator"));
            Assert.AreEqual(2, single.Batches, "one batch per species for a brain that can't mix them");
            Assert.AreEqual(1, mixed.Batches);
        }

        [Test, Description("T-DEC-05 (DEC-14, V-11): 17 actions with a 16-option brain, and a prompt over its token limit → errors before the run")]
        public void BrainLimits()
        {
            var b = New().Flat(20, 20).ReferencePhases().DefaultBrain<ScriptedBrain>(x => x.ActionLimit = 16)
                .Species("many", s => { s.Module<KinematicLocomotion>(); for (int i = 0; i < 17; i++) s.Action<RestAction>("rest" + i); });
            var r = new ValidationReport();
            Assert.IsFalse(b.BuildUninitialized().Prepare(r));
            Assert.IsTrue(r.Has("V-11"), r.ToString());

            var b2 = New(name: "tokens").Flat(20, 20).ReferencePhases()
                .DefaultBrain<ScriptedBrain>(x => x.TokenLimit = 50)
                .Species("prey", s => s.Module<KinematicLocomotion>().Action<RestAction>("rest", founders: new[] { "Rest whenever you can, it helps you live a long time." }));
            var r2 = new ValidationReport();
            Assert.IsFalse(b2.BuildUninitialized().Prepare(r2));
            Assert.IsTrue(r2.Has("V-11"), r2.ToString());
        }

        [Test, Description("T-DEC-06 (DEC-20): sampling with τ = 1, 0.5, 2 over 100 000 draws → p, p², √p normalised, within 4 s.e.")]
        public void SamplingTemperature()
        {
            var p = new[] { 0.5f, 0.3f, 0.2f };
            foreach (var tau in new[] { 1f, 0.5f, 2f })
            {
                var q = Sampling.Temper(p, tau);
                var expected = p.Select(x => Math.Pow(x, 1.0 / tau)).ToArray();
                double sum = expected.Sum();
                var rng = new RandomStream(9, "tau" + tau);
                var counts = new long[3];
                for (int i = 0; i < 100000; i++) counts[rng.Choose(q)]++;
                for (int k = 0; k < 3; k++) Stat.Binomial(counts[k], 100000, expected[k] / sum, 4, $"τ={tau} action {k}");
            }
        }

        [Test, Description("T-DEC-07 (DEC-30, DEC-32): 50 animals in 3 distinct situations with one genome → 3 brain queries; next tick, the same situations → 0")]
        public void MemoAndDuplicates()
        {
            ScriptedBrain brain = null;
            var w = New().Flat(100, 100).Phase<SensePhase>().Phase<AskBrainsPhase>().Phase<ChooseActionsPhase>()
                .DefaultBrain<ScriptedBrain>(x => brain = x)
                .Species("prey", s => s.Module<Energy>().Sense<LevelSense>(l => l.Configure("energy", 30, 70), "Energy").Action<RestAction>("rest", founders: new[] { "Rest." }))
                .Build();
            var sp = w.AllSpecies[0];
            var genome = new Genome(sp, new[] { w.Alleles.All[0] });
            for (int i = 0; i < 50; i++)
            {
                var a = Place.Animal(sp, Place.At(i, 5), genome);
                a.SetStat("energy", i % 3 == 0 ? 10 : i % 3 == 1 ? 50 : 90);
            }
            w.Advance(1);
            Assert.AreEqual(3, brain.Calls, "one query per distinct key");
            Assert.AreEqual(1, brain.Batches);
            foreach (var a in sp.Animals) a.Action = -1;
            w.Advance(1);
            Assert.AreEqual(3, brain.Calls, "next tick: all memo hits");
            Assert.AreEqual(50, sp.Counters.MemoHits);
        }

        [Test, Description("T-DEC-08 (DEC-31): two animals that differ only in a number gene the brain doesn't read → one memo key")]
        public void NumberGenesDontSplitTheMemo()
        {
            var w = New().Flat(20, 20).ReferencePhases().DefaultBrain<ScriptedBrain>()
                .Species("prey", s => s.Module<KinematicLocomotion>().Module<TraitProbe>().Action<RestAction>("rest", founders: new[] { "Rest." })
                    .Module<NumberGene>(g => g.Configure("probe.trait", 1, 120, new[] { 40f, 80f }, false, "level")))
                .Build();
            var sp = w.AllSpecies[0];
            var text = sp.Genes.First(g => g.Kind == AlleleKind.Text);
            var number = sp.Genes.First(g => g.Kind == AlleleKind.Number);
            Genome Make(float v)
            {
                var alleles = new Allele[2];
                alleles[text.Locus] = w.Alleles.Register(text, AlleleValue.OfText("Rest."), "founder");
                alleles[number.Locus] = w.Alleles.Register(number, AlleleValue.OfNumber(v), "founder");
                return new Genome(sp, alleles);
            }
            var g1 = Make(40);
            var g2 = Make(80);
            Assert.AreNotEqual(g1.Key, g2.Key);
            Assert.AreEqual(g1.BrainKey, g2.BrainKey);
            Place.Animal(sp, Place.At(1, 1), g1);
            Place.Animal(sp, Place.At(2, 2), g2);
            w.Advance(1);
            Assert.AreEqual(1, w.GetComponentInChildren<ScriptedBrain>().Calls);
        }

        [Test, Description("T-DEC-09 (DEC-34, DEC-40, RAND-20): a brain failing every call; strict → the run stops cleanly; non-strict → uniform rows, failures counted, nothing in memo or cache")]
        public void BrainFailures()
        {
            var strict = Lab(prey: 4).DefaultBrain<ScriptedBrain>(x => { x.FailWhen = q => "server down"; x.Strict = true; }).Build();
            strict.Advance(5);
            Assert.AreEqual(RunState.Stopped, strict.State);
            StringAssert.Contains("brain failure", strict.StopReason);
            Assert.AreEqual(0, strict.Tick, "stopped before the first act phase");

            var cacheDir = System.IO.Path.Combine(Application.temporaryCachePath, "evosim-cache-fail-test");
            if (System.IO.Directory.Exists(cacheDir)) System.IO.Directory.Delete(cacheDir, true);
            var loose = Lab(seed: 2, prey: 4).Service<AnswerCache>("Answer cache", c => c.SetFolder(cacheDir))
                .DefaultBrain<ScriptedBrain>(x => { x.FailWhen = q => "server down"; x.UsesCache = true; }).Build();
            loose.Advance(5);
            Assert.AreNotEqual(RunState.Stopped, loose.State);
            var sp = loose.FindSpecies("prey");
            Assert.Greater(sp.Counters.Failures, 0);
            Assert.AreEqual(0, loose.Decisions.Memo.Count, "failures are never stored (DEC-34)");
            Assert.AreEqual(0, loose.Service<AnswerCache>().Stores);
            foreach (var a in sp.Animals) CollectionAssert.AreEqual(Brain.Uniform(sp.Actions.Count), a.LastProbabilities);
        }

        [Test, Description("T-DEC-11 (08 §6): the random brain → uniform rows of each species' length, whatever the genes and the situation")]
        public void RandomBrainIsUniform()
        {
            var w = Lab(prey: 10, predators: 5).DefaultBrain<RandomBrain>().Build();
            w.Advance(1);
            foreach (var d in w.Decisions.Due)
                CollectionAssert.AreEqual(Brain.Uniform(d.Animal.Species.Actions.Count), d.Row);
        }

        [Test, Description("T-CORE-10 (CORE-06): after the Sense, Ask brains and Choose actions phases no position, stat or item has changed")]
        public void DecidingChangesNothing()
        {
            var w = Lab(prey: 20, predators: 5).DefaultBrain<RandomBrain>().Build();
            string Snapshot() => string.Join(";", w.AllSpecies.SelectMany(s => s.Animals).Select(a => $"{a.Id}:{a.Position}:{a.StatOf("energy")}:{a.StatOf("stamina")}"))
                                 + "|" + w.Service<FoodGrid>().Count + "|" + w.Service<CarcassSystem>().EntityCount;
            var before = Snapshot();
            for (int i = 0; i < 3; i++) w.Phases[i].Run(w.Context);
            Assert.AreEqual(before, Snapshot());
            Assert.IsTrue(w.AllSpecies.SelectMany(s => s.Animals).All(a => a.Action >= 0), "but every animal has an action");
        }

        [Test, Description("T-RAND-04 (RAND-05): caches filled in different insertion orders → identical hash")]
        public void InsertionOrderDoesntMatter()
        {
            string Run(bool reversedPrefill)
            {
                var w = Lab(seed: 3, prey: 15, predators: 4).DefaultBrain<RandomBrain>().Build();
                var keys = Enumerable.Range(0, 200).Select(i => "k" + i).ToList();
                if (reversedPrefill) keys.Reverse();
                foreach (var k in keys) w.Decisions.Memo.Store(k, new[] { 1f });
                w.Advance(60);
                return w.Events.Hash;
            }
            Assert.AreEqual(Run(false), Run(true));
        }

        [Test, Description("T-SENSE-10 (SENSE-41, V-51): a camera sense with a brain that rejects attachments → V-51; with one that accepts them → never memoised without a cache key")]
        public void Attachments()
        {
            var b = New().Flat(20, 20).ReferencePhases().DefaultBrain<ScriptedBrain>()
                .Species("eyes", s => s.Module<KinematicLocomotion>().Sense<CameraProbeSense>().Action<RestAction>("rest", founders: new[] { "Rest." }).Population(5));
            var r = new ValidationReport();
            Assert.IsFalse(b.BuildUninitialized().Prepare(r));
            Assert.IsTrue(r.Has("V-51"));

            ScriptedBrain brain = null;
            var w = New(name: "accepts").Flat(20, 20).ReferencePhases().DefaultBrain<ScriptedBrain>(x => { x.TakesAttachments = true; brain = x; })
                .Species("eyes", s => s.Module<KinematicLocomotion>().Sense<CameraProbeSense>().Action<RestAction>("rest", founders: new[] { "Rest." }).Population(5))
                .Build();
            w.Advance(9);
            Assert.AreEqual(15, brain.Calls, "5 animals × 3 decision ticks, never memoised");
            Assert.AreEqual(0, w.Decisions.Memo.Count);
            Assert.IsTrue(brain.Seen.All(q => q.Attachments.Count == 1));
        }
    }
}
