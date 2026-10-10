using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>Genes, alleles, the registry and founders (09).</summary>
    public class GeneTests : WorldFixture
    {
        [Test, Description("T-GENE-01 (GENE-01…04): every gene kind has an id, a label, a kind and a founder pool; text expression only adds a prompt line, number expression only sets its trait")]
        public void GeneKinds()
        {
            var w = New().Flat(10, 10).Species("prey", s => s
                .Action<RestAction>("rest", founders: new[] { "Rest when you are tired." })
                .Module<TraitProbe>()
                .Module<NumberGene>(g => g.Configure("probe.trait", 1f, 120f, new[] { 45f, 75f }, false, "level"))).Build();
            var sp = w.AllSpecies[0];
            var text = (TextGene)sp.Genes[0];
            var number = (NumberGene)sp.Genes[1];
            Assert.AreEqual("prey.rest", text.LocusId);
            Assert.AreEqual("rest", text.Label);
            Assert.AreEqual(AlleleKind.Text, text.Kind);
            CollectionAssert.AreEqual(new[] { "Rest when you are tired.", "No preference." }, text.FounderPool.Select(f => f.Value.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "founder", "neutral" }, text.FounderPool.Select(f => f.Origin).ToArray());
            Assert.AreEqual("prey.level", number.LocusId);
            Assert.AreEqual(AlleleKind.Number, number.Kind);

            var a = Place.Animal(sp, Place.At(1, 1));
            var lines = new List<KeyValuePair<string, string>>();
            var e = new Expression(a, sp.Declarations, lines);
            float before = a.Trait(sp.Module<TraitProbe>().Trait);
            text.Express(AlleleValue.OfText("Never rest."), e);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(before, a.Trait(sp.Module<TraitProbe>().Trait), "a text gene changes no trait");
            number.Express(AlleleValue.OfNumber(99f), e);
            Assert.AreEqual(1, lines.Count, "a number gene adds no prompt line");
            Assert.AreEqual(99f, a.Trait(sp.Module<TraitProbe>().Trait));
        }

        [Test, Description("T-GENE-02 (GENE-10): \"Eat  when hungry.\" and \"Eat when hungry.\" → one allele, first origin kept; 0.30001 and 0.30004 at 4 decimals → one allele")]
        public void RegistryCollapsesEqualValues()
        {
            var w = New().Flat(10, 10).Species("prey", s => s.Action<EatAction>("eat").Module<TraitProbe>()
                .Module<NumberGene>(g => g.Configure("probe.trait", 0f, 1f, new[] { 0.5f }))).Build();
            var sp = w.AllSpecies[0];
            var r = w.Alleles;
            var a = r.Register(sp.Genes[0], AlleleValue.OfText("Eat  when hungry."), "custom");
            var b = r.Register(sp.Genes[0], AlleleValue.OfText("Eat when hungry."), "mutant");
            Assert.AreSame(a, b);
            Assert.AreEqual("custom", b.Origin, "the first origin is kept");
            var x = r.Register(sp.Genes[1], AlleleValue.OfNumber(0.30001f), "mutant");
            var y = r.Register(sp.Genes[1], AlleleValue.OfNumber(0.30004f), "mutant");
            Assert.AreSame(x, y);
            Assert.AreNotSame(x, r.Register(sp.Genes[1], AlleleValue.OfNumber(0.3002f), "mutant"));
        }

        [Test, Description("T-GENE-03 (GENE-11): allele ids per locus → prey.eat:0, prey.eat:1…")]
        public void AlleleIds()
        {
            var w = New().Flat(10, 10).Species("prey", s => s.Action<EatAction>("eat", founders: new[] { "Eat.", "Eat now." })).Build();
            var ids = w.Alleles.All.Select(a => a.Id).ToArray();
            CollectionAssert.AreEqual(new[] { "prey.eat:0", "prey.eat:1", "prey.eat:2" }, ids);
            var m = w.Alleles.Register(w.AllSpecies[0].Genes[0], AlleleValue.OfText("Eat later."), "mutant", "prey.eat:1", "llm#3", "qwen", 42);
            Assert.AreEqual("prey.eat:3", m.Id);
        }

        [Test, Description("T-GENE-04 (GENE-12): a mutant allele records parent, operator, model, seed; a 5-step lineage is rebuilt from the records")]
        public void Lineage()
        {
            var w = New().Flat(10, 10).Species("prey", s => s.Action<EatAction>("eat", founders: new[] { "Eat." })).Build();
            var gene = w.AllSpecies[0].Genes[0];
            var parent = w.Alleles.All[0];
            var chain = new List<Allele> { parent };
            for (int i = 1; i <= 5; i++)
            {
                parent = w.Alleles.Register(gene, AlleleValue.OfText("Eat " + new string('a', i) + "."), "mutant", parent.Id, "llm#" + i, "qwen3.5:0.8b", 100 + i);
                chain.Add(parent);
            }
            Assert.AreEqual("llm#5", parent.Operator);
            Assert.AreEqual("qwen3.5:0.8b", parent.Model);
            Assert.AreEqual(105, parent.Seed);
            var rebuilt = new List<Allele>();
            for (var a = parent; a != null; a = w.Alleles.ById(a.ParentId)) rebuilt.Insert(0, a);
            CollectionAssert.AreEqual(chain, rebuilt);
        }

        [Test, Description("T-GENE-05 (GENE-13): the same genes in two runs with different registration orders → the same genome key")]
        public void GenomeKeysDependOnValues()
        {
            string Key(bool reversed)
            {
                var w = New(name: reversed ? "reversed" : "straight").Flat(10, 10)
                    .Species("prey", s => s.Action<EatAction>("eat", founders: reversed ? new[] { "B.", "A." } : new[] { "A.", "B." })
                                           .Action<RestAction>("rest", founders: new[] { "C." })).Build();
                var sp = w.AllSpecies[0];
                var g = new Genome(sp, new[] { w.Alleles.Register(sp.Genes[0], AlleleValue.OfText("A."), "x"), w.Alleles.Register(sp.Genes[1], AlleleValue.OfText("C."), "x") });
                return g.Key + "/" + g.BrainKey;
            }
            Assert.AreEqual(Key(false), Key(true));
        }

        [Test, Description("T-GENE-06 (GENE-20): 10 000 founders → each locus uniform over its pool (χ²), neutral included")]
        public void FoundersAreUniform()
        {
            var w = New().Ecology(48f, 0f).Build();
            var prey = w.FindSpecies("prey");
            var rng = new RandomStream(5, "founders-test");
            var counts = prey.Genes.Select(g => new long[g.FounderPool.Count]).ToArray();
            for (int i = 0; i < 10000; i++)
            {
                var genome = w.FounderGenome(prey, rng);
                for (int l = 0; l < prey.Genes.Count; l++) counts[l][genome[l].Index]++;
            }
            for (int l = 0; l < prey.Genes.Count; l++)
            {
                Assert.AreEqual(5, counts[l].Length, "4 founders + the neutral allele");
                Stat.Uniform(counts[l], 0.001, prey.Genes[l].Label);
            }
        }

        [Test, Description("T-GENE-07 (GENE-22): a founder of 13 words, or with \"é\" → V-40")]
        public void FounderGuards()
        {
            foreach (var bad in new[] { "One two three four five six seven eight nine ten eleven twelve thirteen.", "Eat the café food." })
            {
                var w = New(name: bad).Flat(10, 10).Species("prey", s => s.Action<EatAction>("eat", founders: new[] { bad })).BuildUninitialized();
                var r = new ValidationReport();
                Assert.IsFalse(w.Prepare(r));
                Assert.IsTrue(r.Has("V-40"), r.ToString());
            }
            var ok = New(name: "ok").Flat(10, 10).Species("prey", s => s.Action<EatAction>("eat", founders: new[] { "Eat the 12 seeds, quickly; then rest!" })).BuildUninitialized();
            var r2 = new ValidationReport();
            Assert.IsTrue(ok.Prepare(r2), r2.ToString());
        }

        [Test, Description("T-GENE-10 (GENE-21): founder pools are frozen: two seeds → the same pool contents and allele ids for founders")]
        public void FounderPoolsAreFrozen()
        {
            string Pool(int seed)
            {
                var w = New(seed, name: "seed " + seed).Ecology(48f, 0f, prey: s => s.Population(20)).Build();
                return string.Join("|", w.Alleles.All.Select(a => a.Id + "=" + a.Text));
            }
            Assert.AreEqual(Pool(1), Pool(2));
        }

        [Test, Description("T-GENE-11, pool part (GENE-24): no neutral → none in the pool; a changed neutral text → founders use it")]
        public void NeutralSettings()
        {
            var w = New().Flat(10, 10).Species("prey", s => s
                .Action<EatAction>("eat", g => { }, new[] { "Eat." })
                .Action<RestAction>("rest", founders: new[] { "Rest." })).BuildUninitialized();
            var sp = w.GetComponentInChildren<Species>();
            sp.transform.Find("Actions/eat").GetComponent<TextGene>().Configure(new[] { "Eat." }, withNeutral: false);
            sp.transform.Find("Actions/rest").GetComponent<TextGene>().Configure(new[] { "Rest." }, true, "Whatever.");
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.AreEqual(1, sp.Genes[0].FounderPool.Count);
            CollectionAssert.Contains(sp.Genes[1].FounderPool.Select(f => f.Value.Text).ToArray(), "Whatever.");
            Assert.IsTrue(w.LastReport.Has("V-43"), "no neutral: a warning");
        }
    }
}
