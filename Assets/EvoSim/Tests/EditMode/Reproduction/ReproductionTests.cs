using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Mating, litters, eggs, crossover (11 §1–§3, 09 §4).</summary>
    public class ReproductionTests : WorldFixture
    {
        /// <summary>Prey only, with the act, breed, hatch, death and migration phases, no decisions (actions are set by hand).</summary>
        World Breeders(int incubation = 0, bool asexual = false, int cap = 400, CapRule.Mode mode = CapRule.Mode.Migrate, int seed = 1234)
        {
            return New(seed).Flat(40, 40).Food(0f, 0f).Service<EggSystem>("Eggs").Configure(w => w.Asexual = asexual)
                .Phase<ActPhase>().Phase<BreedPhase>().Phase<HatchPhase>().Phase<DeathPhase>().Phase<MigrationPhase>().Phase<EnvironmentPhase>()
                .Species("prey", s =>
                {
                    s.PreyBody().PreySenses().PreyActionSet().LifeRules(cap, 0, incubation);
                    s.Get<CapRule>().Configure(cap, mode);
                })
                .Build();
        }

        static Animal Adult(Species s, Vector3 p, float energy = 90f, string action = "rest")
        {
            var a = Place.Animal(s, p);
            a.Age = 200;
            a.SetStat("energy", energy);
            a.SetStat("stamina", 0f);                                   // nobody moves: only the reach matters
            a.Choose(action);
            return a;
        }

        [Test, Description("T-REPRO-01 (REPRO-01): age 149 / 150 and energy 49.9 / 50 → not ready, ready")]
        public void Readiness()
        {
            var w = Breeders();
            var s = w.AllSpecies[0];
            var rule = s.Module<MatingRule>();
            var a = Place.Animal(s, Place.At(1, 1));
            a.SetStat("energy", 60); a.Age = 149;
            Assert.IsFalse(rule.IsReady(a));
            a.Age = 150;
            Assert.IsTrue(rule.IsReady(a));
            a.SetStat("energy", 49.9f);
            Assert.IsFalse(rule.IsReady(a));
            a.SetStat("energy", 50f);
            Assert.IsTrue(rule.IsReady(a));
        }

        [Test, Description("T-REPRO-02 (REPRO-02): mate next to a ready partner choosing rest → a litter; two choosing mate out of reach → none")]
        public void OnePartnersChoiceIsEnough()
        {
            var w = Breeders();
            var s = w.AllSpecies[0];
            var seeker = Adult(s, Place.At(10, 10), action: "mate");
            var resting = Adult(s, Place.At(10.8f, 10), action: "rest");
            var far1 = Adult(s, Place.At(30, 30), action: "mate");
            var far2 = Adult(s, Place.At(30, 35), action: "mate");
            w.Advance(1);
            Assert.That(seeker.Offspring, Is.InRange(2, 4), "one partner's choice is enough");
            Assert.AreEqual(seeker.Offspring, resting.Offspring);
            Assert.AreEqual(0, far1.Offspring + far2.Offspring, "out of reach: none");
            Assert.AreEqual(seeker.Offspring, s.Counters.Births);
        }

        [Test, Description("T-REPRO-03 (REPRO-03): three ready seekers in contact → each breeds at most once per decision period; the same pairing in every run")]
        public void OncePerPeriodAndDeterministicPairing()
        {
            string Pairing()
            {
                var w = Breeders();
                var s = w.AllSpecies[0];
                var a = Adult(s, Place.At(10, 10), action: "mate");
                var b = Adult(s, Place.At(10.4f, 10), action: "mate");
                var c = Adult(s, Place.At(10.8f, 10), action: "mate");
                w.Phase<BreedPhase>().Run(w.Context);
                w.Phase<BreedPhase>().Run(w.Context);                               // again in the same period
                Assert.AreEqual(1, new[] { a, b, c }.Count(x => !x.BredThisPeriod), "two bred once, one found no free partner");
                return string.Join(",", new[] { a, b, c }.Select(x => x.Offspring > 0));
            }
            string first = Pairing();
            Assert.AreEqual("True,True,False", first, "by id: a breeds with its nearest, b");
            for (int i = 0; i < 5; i++) Assert.AreEqual(first, Pairing());
        }

        [Test, Description("T-REPRO-04 (REPRO-04, GENE-31, T-GENE-09): asexual → one parent pays the whole cost; the baby copies its genome")]
        public void Asexual()
        {
            var w = Breeders(asexual: true);
            var s = w.AllSpecies[0];
            var a = Adult(s, Place.At(10, 10), energy: 100, action: "mate");
            w.Phase<BreedPhase>().Run(w.Context);
            int k = a.Offspring;
            Assert.That(k, Is.InRange(2, 2), "100 energy pays for 2 babies of 40 alone");
            Assert.AreEqual(100f - 40f * k, a.StatOf("energy"), 1e-4f, "the whole cost");
            w.Phase<HatchPhase>().Run(w.Context);
            foreach (var baby in s.Animals.Where(x => x != a))
            {
                CollectionAssert.AreEqual(new[] { a.Id }, baby.Parents);
                CollectionAssert.AreEqual(a.Genome.Alleles, baby.Genome.Alleles, "a copy of the parent (no mutation operator here)");
            }
        }

        [Test, Description("T-REPRO-05 (REPRO-10): litters [2, 4] uniform; parents at 50 energy → cut to 2; at 30 → 2 anyway, energy below zero")]
        public void LitterSizes()
        {
            var w = Breeders();
            var litter = w.AllSpecies[0].Module<Litter>();
            var rng = new RandomStream(4, "litters");
            var counts = new long[3];
            for (int i = 0; i < 10000; i++) counts[litter.Size(rng, new[] { 100f, 100f }) - 2]++;
            Stat.Uniform(counts, 0.001, "litter sizes");
            for (int i = 0; i < 100; i++) Assert.AreEqual(2, litter.Size(rng, new[] { 50f, 50f }));
            for (int i = 0; i < 100; i++) Assert.AreEqual(2, litter.Size(rng, new[] { 30f, 30f }), "never below the minimum");

            var s = w.AllSpecies[0];
            var a = Adult(s, Place.At(10, 10), energy: 50, action: "mate");
            Adult(s, Place.At(10.5f, 10), energy: 50);
            w.Phase<BreedPhase>().Run(w.Context);
            Assert.AreEqual(2, a.Offspring);
            Assert.AreEqual(10f, a.StatOf("energy"), 1e-4f);

            var asexual = Breeders(asexual: true);
            var b = Adult(asexual.AllSpecies[0], Place.At(10, 10), energy: 50, action: "mate");
            asexual.Phase<BreedPhase>().Run(asexual.Context);
            Assert.AreEqual(2, b.Offspring, "can afford 1, gets the minimum 2");
            Assert.AreEqual(-30f, b.StatOf("energy"), 1e-4f, "below zero");
        }

        [Test, Description("T-REPRO-06 (REPRO-11): a litter of 3 → each parent pays 60, offspring +3, flagged as bred")]
        public void ParentsPay()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var w = Breeders(seed: seed);
                var s = w.AllSpecies[0];
                var a = Adult(s, Place.At(10, 10), 100, "mate");
                var b = Adult(s, Place.At(10.5f, 10), 100);
                w.Phase<BreedPhase>().Run(w.Context);
                if (a.Offspring != 3) continue;
                Assert.AreEqual(40f, a.StatOf("energy"), 1e-4f);
                Assert.AreEqual(40f, b.StatOf("energy"), 1e-4f);
                Assert.AreEqual(3, b.Offspring);
                Assert.IsTrue(a.BredThisPeriod && b.BredThisPeriod);
                return;
            }
            Assert.Fail("no litter of 3 in 50 seeds");
        }

        [Test, Description("T-REPRO-07 (REPRO-12, REPRO-13): babies → energy 40, full stamina, age 0, generation max + 1, at the first parent's position, no action")]
        public void Babies()
        {
            var w = Breeders();
            var s = w.AllSpecies[0];
            var a = Adult(s, Place.At(10, 10), 100, "mate");
            var b = Adult(s, Place.At(10.5f, 10), 100);
            a.Generation = 3; b.Generation = 7;
            w.Phase<BreedPhase>().Run(w.Context);
            w.Phase<HatchPhase>().Run(w.Context);
            var babies = s.Animals.Where(x => x.Origin == "birth").ToList();
            Assert.That(babies.Count, Is.InRange(2, 4));
            foreach (var baby in babies)
            {
                Assert.AreEqual(40f, baby.StatOf("energy"));
                Assert.AreEqual(60f, baby.StatOf("stamina"));
                Assert.AreEqual(0, baby.Age);
                Assert.AreEqual(8, baby.Generation);
                Assert.AreEqual(a.Position, baby.Position);
                Assert.AreEqual(-1, baby.Action, "decides at the next tick");
                CollectionAssert.AreEqual(new[] { a.Id, b.Id }, baby.Parents);
            }
        }

        [Test, Description("T-REPRO-08 (REPRO-20…22): incubation 0 and 5 → hatch the same tick / 5 ticks later, in conception order; eggs are not sensed, don't move, don't count toward the cap")]
        public void EggsAndIncubation()
        {
            var w0 = Breeders(0);
            var s0 = w0.AllSpecies[0];
            Adult(s0, Place.At(10, 10), 100, "mate");
            Adult(s0, Place.At(10.5f, 10), 100);
            w0.Advance(1);
            Assert.Greater(s0.Counters.Hatched, 0, "incubation 0: hatched in the same tick");

            var w = Breeders(5, cap: 2);
            var s = w.AllSpecies[0];
            var a = Adult(s, Place.At(10, 10), 100, "mate");
            var b = Adult(s, Place.At(10.5f, 10), 100);
            var eggs = w.Service<EggSystem>();
            w.Advance(1);
            int laid = eggs.EntityCount;
            Assert.Greater(laid, 0);
            Assert.AreEqual(0, s.Counters.Hatched);
            var positions = eggs.Entities.Select(e => e.Position).ToList();
            a.Choose("rest");
            b.Position = Place.At(35, 35);
            w.Space.Rebuild();
            var kin = s.FindSense("Animal");
            Assert.AreEqual("none", kin.Tokens[kin.Read(a, new SenseContext(w))], "eggs at its feet are not sensed as animals");
            w.Advance(3);
            Assert.AreEqual(0, s.Counters.Hatched, "not yet");
            Assert.AreEqual(2, s.Animals.Count, "eggs don't count toward the cap of 2: nobody migrated");
            CollectionAssert.AreEqual(positions, eggs.Entities.Select(e => e.Position).ToList(), "eggs don't move");
            w.Advance(1);
            Assert.AreEqual(0, s.Counters.Hatched, "tick 4: not yet");
            w.Advance(1);
            Assert.AreEqual(laid, s.Counters.Hatched, "hatched at tick 5, 5 ticks after conception");
            Assert.AreEqual(0, eggs.EntityCount);
        }

        [Test, Description("T-TICK-04 (TICK-06, REPRO-05): a prey animal that chose mate next to a ready partner and is killed later in the act phase → no litter")]
        public void BreedingAfterActing()
        {
            var w = New().Ecology(40f, 0f, 0f, prey: s => s.LifeRules(400, 0), predator: p => p.Get<Diet>().SetKillChance(1f), regrow: 0f)
                .Phase<ActPhase>().Phase<BreedPhase>().Phase<HatchPhase>().Phase<DeathPhase>().Build();
            var prey = w.FindSpecies("prey");
            var a = Adult(prey, Place.At(10, 10), 100, "mate");
            Adult(prey, Place.At(10.8f, 10), 100, "rest");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(9.5f, 10));
            hunter.Choose("hunt");
            w.Advance(1);
            Assert.IsTrue(a.Killed, "the seeker was killed in the act phase, after it chose mate");
            Assert.AreEqual(0, prey.Counters.EggsLaid, "breeding runs after all animals acted: no litter");
        }

        [Test, Description("T-GENE-08 (GENE-30, GENE-32): 10 000 crossovers → each locus from parent A in 50 % (± 4 s.e.), independent; siblings differ")]
        public void UniformCrossoverIsFair()
        {
            var w = Breeders();
            var s = w.AllSpecies[0];
            var x = new Genome(s, s.Genes.Select(g => w.Alleles.Register(g, g.FounderPool[0].Value, "founder")).ToArray());
            var y = new Genome(s, s.Genes.Select(g => w.Alleles.Register(g, g.FounderPool[1].Value, "founder")).ToArray());
            var cross = s.Module<Crossover>();
            var rng = new RandomStream(8, "cross");
            var fromA = new long[s.Genes.Count];
            long both = 0;
            int n = 10000;
            var siblings = new HashSet<string>();
            for (int i = 0; i < n; i++)
            {
                var child = cross.Cross(x, y, rng);
                for (int l = 0; l < child.Length; l++) if (child[l] == x[l]) fromA[l]++;
                if (child[0] == x[0] && child[1] == x[1]) both++;
                if (i < 4) siblings.Add(string.Join(",", child.Select(c => c.Id)));
            }
            for (int l = 0; l < fromA.Length; l++) Stat.Binomial(fromA[l], n, 0.5, 4, $"locus {l}");
            Stat.Binomial(both, n, 0.25, 4, "independent loci");
            Assert.Greater(siblings.Count, 1, "siblings differ");
        }

        [Test, Description("T-SPEC-02 (SPEC-04, GENE-06): a genome of one species offered to another is rejected; crossover across species throws")]
        public void GenomesNeverCrossSpecies()
        {
            var w = New().Ecology(40f, prey: s => s.LifeRules(400, 0)).Build();
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var preyGenome = w.FounderGenome(prey, new RandomStream(1, "a"));
            var predatorGenome = w.FounderGenome(predator, new RandomStream(1, "b"));
            Assert.Throws<System.ArgumentException>(() => new Genome(predator, preyGenome.ToArray()));
            Assert.Throws<System.ArgumentException>(() => new Genome(prey, preyGenome.ToArray().Take(3).ToArray()), "wrong length");
            var crossover = prey.Module<Crossover>();
            Assert.Throws<System.InvalidOperationException>(() => crossover.Cross(preyGenome, predatorGenome, new RandomStream(1, "z")));
        }

        [Test, Description("T-ANIM-01 (ANIM-01…03): founders, newcomers and births across species → ids unique and increasing; the record has every field")]
        public void AnimalIds()
        {
            var w = New().Lab1(prey: 20, predators: 4).Build();
            w.Advance(400);
            Assert.Greater(w.AllSpecies.Sum(s => s.Counters.Births), 0, "births happened");
            var everyone = w.AllSpecies.SelectMany(s => s.Animals).ToList();
            foreach (var a in everyone)
            {
                Assert.IsNotNull(a.Species); Assert.IsNotNull(a.Genome); Assert.IsNotNull(a.Parents); Assert.IsNotNull(a.Origin);
                Assert.GreaterOrEqual(a.Age, 0);
                Assert.GreaterOrEqual(a.Generation, 0);
                Assert.AreSame(a.Species, a.Genome.Species, "one species for life (ANIM-03)");
            }
            CollectionAssert.AllItemsAreUnique(everyone.Select(a => a.Id));
            foreach (var s in w.AllSpecies)
                CollectionAssert.IsOrdered(s.Animals.Select(a => a.Id), "creation order is id order");
        }
    }
}
