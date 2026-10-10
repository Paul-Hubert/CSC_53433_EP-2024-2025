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
    /// The set-up and run integrity found by the P1 audit (32 §3, phase 2): brains that aren't set up, settings an
    /// override pushes out of range, species added during a run, strict mutator stops, mutations that change nothing,
    /// signatures, the batch entry points, genome keys, clones' edibles and the mate action's range.
    /// </summary>
    public class WorldIntegrityTests : WorldFixture
    {
        [Test, Description("ARCH-06, DEC-13 (V-10): a disabled, inactive or other World's brain is an error, never used without being set up")]
        public void BrainsMustBeServicesOfThisWorld([Values("disabled", "inactive", "foreign")] string how)
        {
            var w = New(name: "brain " + how).Lab1(prey: 4, predators: 1).BuildUninitialized();
            var brain = w.DefaultBrain;
            if (how == "disabled") brain.enabled = false;
            else if (how == "inactive") brain.gameObject.SetActive(false);
            else w.DefaultBrain = New(name: "other").Lab1(prey: 2, predators: 1).BuildUninitialized().DefaultBrain;
            var report = new ValidationReport();
            Assert.IsFalse(w.Prepare(report));
            Assert.IsTrue(report.Messages.Any(m => m.Id == "V-10" && m.Severity == Severity.Error), report.ToString());
        }

        [Test, Description("V-35 (DEC-01): a scenario override that sets the decision period to 0 is a validation error, not a division by zero at tick 0")]
        public void DecisionPeriodOverride()
        {
            var w = New(name: "period").Lab1(prey: 4, predators: 1).BuildUninitialized();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;                // Initialize logs the error
            Assert.IsTrue(FieldPath.Set(w, "World/decisionPeriod", "0", out var error), error);
            Assert.IsFalse(w.Initialize());
            var m = w.LastReport.Messages.Single(x => x.Id == "V-35");
            Assert.AreEqual(Severity.Error, m.Severity);
            m.Fix.Apply();
            Assert.AreEqual(1, w.DecisionPeriod, "the fix clamps it");
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            w.Advance(3);
        }

        [Test, Description("SPEC-30, CORE-08 (V-07, EDIT-01): a species added during a run gets a new id and a new display name; a template with errors is refused before anything is recorded")]
        public void AddedSpeciesAreValidated()
        {
            var w = New(name: "add").Lab1(prey: 6, predators: 2).Build();
            var again = w.AddSpecies(w.FindSpecies("predator"), "predator");
            Assert.AreEqual("predator-2", again.Id);
            Assert.AreEqual("predator-2", again.DisplayName, "a new name too, never one in use (SPEC-30)");
            Assert.AreSame(w.FindSpecies("predator"), w.FindSpeciesByName("predator"));

            var templates = New(name: "templates").Lab1(prey: 2, predators: 1).BuildUninitialized();
            var broken = templates.GetComponentsInChildren<Species>(true).First(s => s.gameObject.name == "prey");
            Object.DestroyImmediate(broken.GetComponentInChildren<KinematicLocomotion>(true));
            long events = w.Events.Count;
            int count = w.AllSpecies.Count, alleles = w.Alleles.Count;
            var e = Assert.Throws<InvalidOperationException>(() => w.AddSpecies(broken, "broken"));
            StringAssert.Contains("V-13", e.Message);
            Assert.AreEqual(count, w.AllSpecies.Count, "not added");
            Assert.AreEqual(events, w.Events.Count, "nothing recorded");
            Assert.AreEqual(alleles, w.Alleles.Count, "no allele registered");
            Assert.IsNull(w.FindSpeciesByName("broken"));
            Assert.AreEqual(5, w.Advance(5), "the world runs on");
        }

        [Test, Description("RAND-20, TICK-04 (strict mutator): a mutator failure stops the run at the end of that tick, with consistent outputs (kills = killed deaths, every egg hatched or lost)")]
        public void StrictMutatorStopsAtTheBoundary()
        {
            var b = New(31, name: "strict").Lab1(prey: 24, predators: 4);
            b.Get<FakeMutator>().FailWhen = (sentence, seed) => "the mutator is down";
            b.Get<MutatorService>().Strict = true;
            foreach (var m in b.Root.GetComponentsInChildren<LlmMutation>(true)) m.Rate = 1f;
            var w = b.Build();
            while (w.State != RunState.Stopped && w.Tick < 3000) w.Advance(1);
            Assert.AreEqual(RunState.Stopped, w.State, "a birth happened and the mutator failed");
            StringAssert.StartsWith("mutator failure", w.StopReason);
            Assert.AreEqual(0, w.NextPhaseIndex, "stopped between two ticks");
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            Assert.AreEqual(predator.Counters.Kills, prey.Counters.DeathsBy("killed"), "every kill of the last tick has its death");
            Assert.IsFalse(w.AllSpecies.SelectMany(s => s.Animals).Any(a => a.Killed), "killed animals were removed");
            foreach (var s in w.AllSpecies)
                Assert.AreEqual(s.Counters.EggsLaid, s.Counters.Hatched + s.Counters.EggsLost, $"{s.Id}: no egg left half-done");
            Assert.Greater(w.Mutations.Failures, 0);
        }

        [Test, Description("MUT-03, MUT-04 (MUT-20): a Gaussian mutation of a gene at its range's edge that returns the parent's value is a failure (\"unchanged\"), never a mutation event")]
        public void MutationsThatChangeNothingFail()
        {
            var w = New(41, name: "gauss edge").Flat(30, 30).Food(0.3f, 0.01f).Service<EggSystem>("Eggs")
                .Species("prey", s => s.Population(20).PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." })
                    .Action<EatAction>("eat", founders: new[] { "Eat often." }).LifeRules(60, 0)
                    .Module<NumberGene>(g => g.Configure("stamina.max", 20f, 120f, new[] { 120f }, geneLabel: "endurance"))
                    .Module<GaussianMutation>(m => { m.Rate = 1f; m.Sigma = 5f; }))
                .ReferencePhases().DefaultBrain<RandomBrain>().Build();
            w.Events.Lines = new List<string>();
            int births = 0;
            for (int t = 0; t < 1500 && births < 40; t++) { w.Advance(1); births = w.AllSpecies[0].Counters.Births; }
            Assert.Greater(births, 0);
            var mutations = w.Events.Lines.Select(Newtonsoft.Json.Linq.JObject.Parse).Where(e => (string)e["kind"] == "birth")
                .SelectMany(e => e["mutations"]).Where(m => (string)m["operator"] == "gauss").ToList();
            Assert.IsFalse(mutations.Any(m => (string)m["parent"] == (string)m["child"]), "no mutation event from a value to itself");
            Assert.Greater(w.Mutations.Rejections.TryGetValue("unchanged", out var n) ? n : 0, 0, "draws past the edge are counted as unchanged");
        }

        [Test, Description("SPEC-03 (V-12): the signature is computed after the food web, so a label derived from it (a threat sense naming the predator) is the same at every Prepare")]
        public void SignaturesAreStable()
        {
            string Signature(bool twice)
            {
                var w = New(name: "signature " + twice).Lab1(prey: 4, predators: 1).BuildUninitialized();
                var species = w.GetComponentsInChildren<Species>(true);
                species.First(s => s.gameObject.name == "predator").SetNames("predator", "wolf");
                var prey = species.First(s => s.gameObject.name == "prey");
                foreach (var n in prey.GetComponentsInChildren<NearestAnimalSense>(true)) if (n.Targets == AnimalSet.Threats) n.SetLabel("");
                Assert.IsTrue(w.Prepare(new ValidationReport()));
                if (twice) Assert.IsTrue(w.Prepare(new ValidationReport()));
                StringAssert.AreEqualIgnoringCase("wolf", prey.Senses.First(x => x is NearestAnimalSense n && n.Targets == AnimalSet.Threats).Label);
                return prey.Signature;
            }
            Assert.AreEqual(Signature(false), Signature(true));
        }

        [Test, Description("ARCH-12 (20 §10): the phases go through the batch entry points: Sense.ReadAll once per species, AnimalAction.ActAll and Locomotion.MoveAll per group (one animal in sequential orders, the whole group when simultaneous)")]
        public void BatchEntryPoints([Values(ActOrder.SpeciesInTurn, ActOrder.Simultaneous)] ActOrder order)
        {
            var w = New(name: "batch " + order).Flat(30, 30)
                .Species("grazer", s => s.Population(6).Action<BatchProbeAction>("wander", founders: new[] { "Wander." })
                    .Sense<BatchProbeSense>(name: "parity").Module<BatchProbeLocomotion>(name: "Locomotion"))
                .Phase<SensePhase>().Phase<AskBrainsPhase>().Phase<ChooseActionsPhase>().Phase<ActPhase>(p => p.Order = order)
                .DefaultBrain<RandomBrain>().Build();
            var s = w.AllSpecies[0];
            w.Advance(5);
            var sense = s.GetComponentInChildren<BatchProbeSense>();
            var action = s.GetComponentInChildren<BatchProbeAction>();
            var loco = s.GetComponentInChildren<BatchProbeLocomotion>();
            CollectionAssert.AreEqual(new[] { 6, 6 }, sense.Groups, "ticks 0 and 4: every animal of the species in one call");
            Assert.AreEqual(6 * 5, action.Groups.Sum(), "every animal planned through ActAll");
            Assert.AreEqual(6 * 5, loco.Groups.Sum(), "every wandering animal moved through MoveAll");
            int expected = order == ActOrder.Simultaneous ? 6 : 1;
            Assert.IsTrue(action.Groups.All(g => g == expected), string.Join(",", action.Groups));
            Assert.IsTrue(loco.Groups.All(g => g == expected), string.Join(",", loco.Groups));
        }

        [Test, Description("GENE-13 (GENE-10): genome keys hash number alleles at the gene's precision, so two runs that hold the same allele give the same key")]
        public void GenomeKeysUseTheGenesPrecision()
        {
            string Key(float first)
            {
                var w = New(name: "decimals " + first).Flat(20, 20).Food(0f, 0f)
                    .Species("prey", s => s.PreyBody().Action<RestAction>("rest", founders: new[] { "Rest." })
                        .Module<NumberGene>(g => { g.Configure("stamina.max", 20f, 120f, new[] { 60f }, geneLabel: "endurance"); g.SetDecimals(1); }))
                    .Build();
                var s = w.AllSpecies[0];
                var alleles = new Allele[s.Genes.Count];
                for (int i = 0; i < alleles.Length; i++)
                {
                    var g = s.Genes[i];
                    alleles[i] = g is NumberGene ? w.Alleles.Register(g, AlleleValue.OfNumber(first), "custom")
                                                 : w.Alleles.Register(g, g.FounderPool[0].Value, g.FounderPool[0].Origin);
                }
                if (s.Genes.First(g => g is NumberGene) is NumberGene n)
                    Assert.AreSame(alleles[n.Locus], w.Alleles.Register(n, AlleleValue.OfNumber(first == 61.04f ? 61.01f : 61.04f), "custom"), "one allele at 1 decimal");
                return new Genome(s, alleles).Key;
            }
            Assert.AreEqual(Key(61.04f), Key(61.01f));
        }

        [Test, Description("SPEC-10, SPEC-13, ACT-11: a species added during the run is struck through its own Edible: disabled, it can't be eaten; its energy is what the hunter gains")]
        public void ClonesAreEatenThroughTheirOwnEdible()
        {
            var w = New(name: "clone edible").Lab1(prey: 0, predators: 0).Build();
            var predator = w.FindSpecies("predator");
            var deer = w.AddSpecies(w.FindSpecies("prey"), "deer", w.FindSpecies("prey"));
            var killChance = predator.Module<Diet>().KillChance;
            var resolver = new InteractionResolver(w);
            Animal Hunter(float x) => Place.Animal(predator, Place.At(x, 10), atCreation: a => a.SetTrait(killChance, 1f));

            var edible = deer.GetComponent<Edible>();
            edible.enabled = false;
            var h1 = Hunter(10);
            var d1 = Place.Animal(deer, Place.At(10.5f, 10));
            resolver.Apply(h1, Interaction.Strike(d1), d1.Position);
            Assert.IsFalse(d1.Killed, "without an enabled edible nothing can eat it (SPEC-10)");

            edible.enabled = true;
            edible.energy = 10f;
            var h2 = Hunter(20);
            var d2 = Place.Animal(deer, Place.At(20.5f, 10));
            float before = h2.StatOf("energy");
            Assert.IsTrue(resolver.Apply(h2, Interaction.Strike(d2), d2.Position));
            Assert.IsTrue(d2.Killed);
            Assert.AreEqual(before + 10f, h2.StatOf("energy"), 1e-4, "the deer's own energy (ACT-11)");
        }

        [Test, Description("ACT-05 (06 §4): the mate action walks only to a ready partner its kin sense can report, within the partner range")]
        public void MateStaysWithinThePartnerRange()
        {
            var w = New(name: "partner range").Flat(40, 40).Food(0f, 0f)
                .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." })
                    .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Kin, readiness: true, partner: 4f), "Animal"))
                .Build();
            var s = w.AllSpecies[0];
            Animal Adult(float x) { var a = Place.Animal(s, Place.At(x, 10)); a.Age = 200; a.SetStat("energy", 100f); return a; }
            var seeker = Adult(10);
            var far = Adult(25);
            var c = new ActContext(w);
            c.Begin(seeker);
            Assert.IsNull(c.NearestReadyPartner(seeker), "15 m: beyond the partner range of 4 m, its readiness can't be sensed");
            var near = Adult(13);
            Assert.AreSame(near, c.NearestReadyPartner(seeker)?.Animal, "3 m: within the range");
            Assert.IsNotNull(far);
        }

        [Test, Description("ANIM-17: a trait not declared changeable is set at creation only; setting it later throws, a changeable one may change")]
        public void TraitsAreFixedAfterCreation()
        {
            var w = New(name: "traits").Lab1(prey: 0, predators: 0).Build();
            var predator = w.FindSpecies("predator");
            var killChance = predator.Module<Diet>().KillChance;
            Assert.IsFalse(killChance.Changeable);
            var a = Place.Animal(predator, Place.At(10, 10), atCreation: x => x.SetTrait(killChance, 0.25f));
            Assert.AreEqual(0.25f, a.Trait(killChance), "at creation it may be set");
            Assert.Throws<InvalidOperationException>(() => a.SetTrait(killChance, 0.75f));
            Assert.AreEqual(0.25f, a.Trait(killChance));
            var changeable = new TraitId(killChance.Index, killChance.Name, killChance.Default, 0f, 1f, true);
            a.SetTrait(changeable, 0.5f);
            Assert.AreEqual(0.5f, a.Trait(killChance), "a changeable trait changes during life");
        }

        [Test, Description("GENE-06, SPEC-04: an animal can't be made from another species' genome")]
        public void GenomesMatchTheirSpecies()
        {
            var w = New(name: "genome species").Lab1(prey: 1, predators: 0).Build();
            var preyGenome = w.FindSpecies("prey").Animals[0].Genome;
            Assert.Throws<ArgumentException>(() => Place.Animal(w.FindSpecies("predator"), Place.At(5, 5), preyGenome));
        }
    }
}
