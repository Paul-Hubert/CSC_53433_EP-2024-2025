using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>Whole Lab 1 worlds over hundreds of ticks (T2): species added during a run, controls, act orders, slow mutators.</summary>
    public class WorldPlayTests : WorldFixture
    {
        static string HashAt(World w, int tick)
        {
            while (w.Tick < tick && w.State != RunState.Stopped) w.Advance(1);
            return w.Events.Hash;
        }

        [Test, Description("T-SPEC-06 (SPEC-30, SPEC-31, RAND-03): AddSpecies from a template at tick 300 → new id, own population, streams and allele namespace; relations inherited both ways; other species' events unchanged up to tick 300")]
        public void SpeciesAddedDuringARun()
        {
            var alone = New(7, name: "alone").Lab1().Build();
            string before = HashAt(alone, 300);
            var w = New(7, name: "with deer").Lab1().Build();
            Assert.AreEqual(before, HashAt(w, 300), "the same first 300 ticks");
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            w.Events.Lines = new List<string>();
            var deer = w.AddSpecies(prey, "deer", prey, founders: 6);
            Assert.AreEqual("deer", deer.Id);
            Assert.AreEqual(6, deer.Animals.Count);
            Assert.IsTrue(w.Events.Lines[0].Contains("\"kind\": \"species_created\"") && w.Events.Lines[0].Contains("\"parent_species\": \"prey\""));
            Assert.IsTrue(deer.Genes.All(g => g.LocusId.StartsWith("deer.")), "own allele namespace");
            Assert.IsTrue(w.Alleles.All.Any(a => a.Id == "deer.eat:0"));
            CollectionAssert.Contains(w.ThreatsOf(deer).ToArray(), predator, "hunted by whoever hunted the parent");
            CollectionAssert.Contains(w.PreyOf(predator).ToArray(), deer);
            Assert.IsNotNull(deer.Module<Diet>().Grazing(w.Service<FoodGrid>()), "eats what the parent eats");
            w.Advance(100);
            Assert.Greater(deer.Counters.Decisions, 0, "its own brain queries and streams");
            Assert.AreEqual(1, w.AllSpecies.Count(s => s.Id == "deer"));
        }

        [Test, Description("T-SPEC-07 (SPEC-32): a species goes extinct (no floor) → its id is never reused; a later new species gets a new id")]
        public void ExtinctIdsAreNeverReused()
        {
            var w = New(3).Lab1(prey: 24, predators: 2).Build();
            var predator = w.FindSpecies("predator");
            predator.Module<FloorRule>().Floor = 0;
            foreach (var a in predator.Animals) a.SetStat("energy", 0.1f);
            w.Advance(2);
            Assert.AreEqual(0, predator.Animals.Count, "extinct");
            var again = w.AddSpecies(predator, "predator");
            Assert.AreNotEqual("predator", again.Id);
            Assert.AreEqual("predator-2", again.Id);
            Assert.AreSame(predator, w.FindSpecies("predator"), "the extinct species keeps its id and records");
        }

        [Test, Description("T-CTRL-01 (CTRL-01, CTRL-02): shuffled control → each query carries the genes of another living animal of the species, drawn from the sampling stream; reproducible")]
        public void ShuffledControl()
        {
            string Run(out int checkedQueries)
            {
                ScriptedBrain brain = null;
                var w = New(5, name: "shuffled").Lab1(prey: 30, predators: 4, randomBrain: false).DefaultBrain<ScriptedBrain>(b => brain = b)
                    .Configure(x => x.ShuffledGenes = true).Build();
                int n = 0, other = 0;
                for (int t = 0; t < 40; t++)
                {
                    brain.Seen.Clear();
                    var keysBefore = w.AllSpecies.SelectMany(s => s.Animals).GroupBy(a => a.Species).ToDictionary(g => g.Key, g => g.Select(a => a.Genome.BrainKey).ToList());
                    w.Advance(1);
                    foreach (var d in w.Decisions.Due.Where(d => d.Query != null))
                    {
                        CollectionAssert.Contains(keysBefore[d.Animal.Species], d.Query.GenomeKey, "the genes of a living animal of the species");
                        if (d.Query.GenomeKey != d.Animal.Genome.BrainKey) other++;
                        n++;
                    }
                }
                Assert.Greater(other, n / 2, "mostly another animal's genes");
                checkedQueries = n;
                return w.Events.Hash;
            }
            string h1 = Run(out int n1);
            Assert.Greater(n1, 50);
            Assert.AreEqual(h1, Run(out _), "reproducible");
        }

        [Test, Description("T-CTRL-02 (CTRL-01): random founders → control sentences; asexual → one parent per birth")]
        public void FounderAndParentControls()
        {
            var controls = new[] { "Trains leave from the north platform.", "Needs bakery green bread records nine attic." };
            var w = New(2).Lab1().Configure(x => { x.RandomFounders = true; x.SetControlSentences(controls); }).Build();
            foreach (var a in w.AllSpecies.SelectMany(s => s.Animals))
                Assert.IsTrue(a.Genome.Alleles.All(al => controls.Contains(al.Text) && al.Origin == "control"));

            var asexual = New(2, name: "asexual").Lab1().Configure(x => x.Asexual = true).Build();
            asexual.Advance(400);
            var babies = asexual.AllSpecies.SelectMany(s => s.Animals).Where(a => a.Origin == "birth").ToList();
            Assert.Greater(babies.Count, 0);
            Assert.IsTrue(babies.All(b => b.Parents.Length == 1));
        }

        [Test, Description("T-MOVE-05 (ACT-30…32): the three act orders on the same seed → different hashes, each reproducible")]
        public void ActOrders()
        {
            var hashes = new Dictionary<ActOrder, string>();
            foreach (var order in new[] { ActOrder.SpeciesInTurn, ActOrder.AllMixed, ActOrder.Simultaneous })
            {
                var a = New(11, name: order + " a").Lab1(prey: 30, predators: 5, order: order).Build();
                var b = New(11, name: order + " b").Lab1(prey: 30, predators: 5, order: order).Build();
                string ha = HashAt(a, 300), hb = HashAt(b, 300);
                Assert.AreEqual(ha, hb, $"{order} is reproducible");
                hashes[order] = ha;
            }
            Assert.AreEqual(3, hashes.Values.Distinct().Count(), "the orders differ");
        }

        [UnityTest, Description("T-MUT-10 (MUT-30, RAND-11): mutator answers delivered later (3 Advance calls) → identical genomes and events, in Responsive mode driven by FixedUpdate")]
        public IEnumerator SlowMutatorSameEvents()
        {
            World Make(int delay, WaitMode mode, string name)
            {
                var b = New(13, mode, name).Lab1(prey: 30, predators: 4).Configure(x => x.TickLimit = 400);
                b.Root.GetComponentsInChildren<LlmMutation>(true).ToList().ForEach(m => m.Rate = 0.3f);
                b.Root.GetComponentInChildren<FakeMutator>(true).DelayCalls = delay;
                var w = b.Build();
                w.Play();
                return w;
            }
            var quick = Make(0, WaitMode.Freeze, "quick");
            var slow = Make(3, WaitMode.Responsive, "slow");
            int guard = 0;
            while ((quick.State != RunState.Stopped || slow.State != RunState.Stopped) && guard++ < 20000) yield return new WaitForFixedUpdate();
            Assert.AreEqual(400, slow.Tick);
            Assert.Greater(quick.Mutations.Successes, 0, "mutations happened");
            Assert.Greater(slow.WaitCount, 0, "the slow world waited");
            Assert.AreEqual(quick.Events.Hash, slow.Events.Hash);
        }

        [Test, Description("T-CORE-07 (CORE-04): a world with 1 species, 1 action, 0 genes, 0 senses, and one with 6 species → initialise and run 100 ticks")]
        public void ManyShapesOfWorld()
        {
            var tiny = New(name: "tiny").Flat(20, 20).ReferencePhases().DefaultBrain<RandomBrain>().Service<EggSystem>("Eggs")
                .Species("blob", s => s.Population(5).Module<KinematicLocomotion>().Action<RestAction>("rest", withGene: false))
                .Build();
            tiny.Advance(100);
            Assert.AreEqual(100, tiny.Tick);
            Assert.AreEqual(0, tiny.AllSpecies[0].Genes.Count);

            var six = New(name: "six").Flat(64, 64).Food().Carcasses().Service<EggSystem>("Eggs").ReferencePhases().FakeMutation().DefaultBrain<RandomBrain>();
            foreach (var name in new[] { "rabbit", "hare", "deer" })
                six.Species(name, s => s.Population(15).PreyBody().PreySenses().PreyActionSet().LifeRules(60, 5).LlmMutation());
            six.Species("fox", s => { s.Population(4).On<Edible>(e => e.Configure(EatMethod.Strike, 60f, 2, 30f, 100)).PredatorBody("rabbit").PredatorSenses().PredatorActionSet().LifeRules(12, 2).LlmMutation(); s.Get<Diet>().Set("rabbit", "hare", "carcass:rabbit"); });
            six.Species("lynx", s => { s.Population(3).PredatorBody("hare").PredatorSenses().PredatorActionSet().LifeRules(10, 2); s.Get<Diet>().Set("hare", "deer", "fox"); });
            six.Species("vulture", s => { s.Population(3).PredatorBody("deer").PredatorSenses().Action<HuntAction>("hunt").Action<RestAction>("rest").LifeRules(10, 2); s.Get<Diet>().Set("carcass:rabbit", "carcass:hare", "carcass:deer"); });
            var w6 = six.Build();
            w6.Advance(100);
            Assert.AreEqual(100, w6.Tick);
            Assert.AreEqual(6, w6.AllSpecies.Count);
            CollectionAssert.AreEquivalent(new[] { "lynx" }, w6.ThreatsOf(w6.FindSpecies("fox")).Select(s => s.Id).ToArray());
        }
    }
}
