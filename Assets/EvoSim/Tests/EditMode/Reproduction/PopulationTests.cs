using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Caps, migration, floors, newcomers, founders (11 §4).</summary>
    public class PopulationTests : WorldFixture
    {
        [Test, Description("T-POP-01 (POP-01): migrate: 7 above the cap with 3 babies born this tick → 7 older animals leave, the babies stay; block → no conception at the cap")]
        public void CapRules()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<MigrationPhase>()
                .Species("prey", s => s.PreyBody().PreyActionSet().LifeRules(10, 0)).Build();
            var s = w.AllSpecies[0];
            for (int i = 0; i < 14; i++) Place.Animal(s, Place.At(1 + i, 1));
            var babies = Enumerable.Range(0, 3).Select(i => Place.Animal(s, Place.At(5, 5 + i), origin: "birth")).ToList();
            w.Events.Lines = new System.Collections.Generic.List<string>();
            w.Advance(1);
            Assert.AreEqual(10, s.Animals.Count);
            Assert.IsTrue(babies.All(b => s.Animals.Contains(b)), "babies of this tick stay");
            Assert.AreEqual(7, w.Events.Lines.Count(l => l.Contains("\"cause\": \"migrated\"")));

            var blocked = New(name: "block").Flat(40, 40).Food(0f, 0f).Service<EggSystem>("Eggs").Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", x => { x.PreyBody().PreyActionSet().LifeRules(4, 0); x.Get<CapRule>().Configure(4, CapRule.Mode.Block); }).Build();
            var b = blocked.AllSpecies[0];
            for (int i = 0; i < 4; i++)
            {
                var a = Place.Animal(b, Place.At(10 + 0.3f * i, 10));
                a.Age = 200; a.SetStat("energy", 100); a.Choose("mate");
            }
            blocked.Advance(1);
            Assert.AreEqual(0, b.Counters.EggsLaid, "no conception at the cap");
            b.Animals[3].IsGone = true;
            b.Animals[2].IsGone = true;
            foreach (var a in b.Animals) a.BredThisPeriod = false;
            blocked.Advance(1);
            Assert.AreEqual(2, b.Counters.EggsLaid, "the litter is cut to the 2 places left");
        }

        [Test, Description("T-POP-02 (POP-02, CORE-01): 1 000 migrations where half carry allele X → X leaves in 50 % (± 4 s.e.), with no link to energy or position")]
        public void MigrationIgnoresGenes()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<MigrationPhase>()
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest", founders: new[] { "Rest.", "Run." }).LifeRules(1, 0)).Build();
            var s = w.AllSpecies[0];
            var gene = s.Genes[0];
            var x = w.Alleles.Register(gene, AlleleValue.OfText("Rest."), "founder");
            var other = w.Alleles.Register(gene, AlleleValue.OfText("Run."), "founder");
            int xLeft = 0, n = 1000, highEnergyLeft = 0;
            for (int i = 0; i < n; i++)
            {
                foreach (var a in s.Animals.ToList()) a.IsGone = true;
                var a1 = Place.Animal(s, Place.At(2, 2), new Genome(s, new[] { x }));
                var a2 = Place.Animal(s, Place.At(30, 30), new Genome(s, new[] { other }));
                bool xRich = (i % 2) == 0;
                a1.SetStat("energy", xRich ? 90 : 10);
                a2.SetStat("energy", xRich ? 10 : 90);
                w.Phase<MigrationPhase>().Run(w.Context);
                if (a1.IsGone) xLeft++;
                if ((a1.IsGone && xRich) || (a2.IsGone && !xRich)) highEnergyLeft++;
            }
            Stat.Binomial(xLeft, n, 0.5, 4, "X leaves");
            Stat.Binomial(highEnergyLeft, n, 0.5, 4, "energy plays no part");
        }

        [Test, Description("T-POP-03 (POP-03): a species below its floor → newcomers with founder genomes, generation 0, immigrant events, up to the floor")]
        public void Newcomers()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<FloorPhase>()
                .Species("prey", s => s.PreyBody().PreyActionSet().LifeRules(40, 10)).Build();
            w.Events.Lines = new System.Collections.Generic.List<string>();
            var s = w.AllSpecies[0];
            w.Advance(1);
            Assert.AreEqual(10, s.Animals.Count);
            Assert.AreEqual(10, w.Events.Lines.Count(l => l.Contains("\"kind\": \"immigrant\"")));
            foreach (var a in s.Animals)
            {
                Assert.AreEqual(0, a.Generation);
                Assert.AreEqual("immigrant", a.Origin);
                Assert.AreEqual(60f, a.StatOf("energy"), "the founder energy");
                Assert.IsTrue(a.Genome.Alleles.All(al => al.Origin == "founder" || al.Origin == "neutral"));
                Assert.IsTrue(w.Ground.IsWalkable(a.Position));
            }
            Assert.AreEqual(10, s.Counters.Immigrants);
        }

        [Test, Description("T-POP-04 (POP-04): at tick 0 the initial populations are founders at walkable positions, with founder events")]
        public void Founders()
        {
            var w = New().Lab1(prey: 24, predators: 4).Build();
            Assert.AreEqual(24, w.FindSpecies("prey").Animals.Count);
            Assert.AreEqual(4, w.FindSpecies("predator").Animals.Count);
            Assert.AreEqual(28, w.Events.Count, "one founder event each");
            Assert.IsTrue(w.AllSpecies.SelectMany(s => s.Animals).All(a => a.Origin == "founder" && w.Ground.IsWalkable(a.Position)));
        }

        [Test, Description("T-TICK-03 (TICK-05): a newcomer added at the floor phase decides at the start of the next tick")]
        public void NewcomersDecideNextTick()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>()
                .Species("prey", s => s.PreyBody().PreySenses().PreyActionSet().LifeRules(40, 3)).Build();
            var s = w.AllSpecies[0];
            w.Advance(1);
            Assert.AreEqual(3, s.Animals.Count, "added at the floor phase of tick 0");
            Assert.IsTrue(s.Animals.All(a => a.Action == -1 && a.LastDecisionTick < 0), "no decision yet");
            w.Advance(1);
            Assert.IsTrue(s.Animals.All(a => a.LastDecisionTick == 1), "decided at the start of tick 1");
        }

        [Test, Description("T-ANIM-09, migration part (ANIM-40): a migration is recorded once with cause migrated")]
        public void MigrationIsADeath()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<MigrationPhase>().Species("prey", s => s.PreyBody().PreyActionSet().LifeRules(1, 0)).Build();
            var s = w.AllSpecies[0];
            Place.Animal(s, Place.At(1, 1));
            Place.Animal(s, Place.At(2, 2));
            w.Advance(2);
            Assert.AreEqual(1, s.Counters.DeathsBy("migrated"));
            Assert.AreEqual(1, s.Animals.Count);
        }
    }
}
