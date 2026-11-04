using System.IO;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>
    /// The core changes of the neural species (plan M16 §4, C1–C5, C7): vector alleles (T-NEURO-01), their recording, the
    /// genome on a query, brains that read no sentence, and recombinant alleles. R-01 and the other goldens are checked by
    /// the regression tests, unchanged.
    /// </summary>
    public class VectorAlleleTests : WorldFixture
    {
        /// <summary>The weights of the vector gene under test.</summary>
        static readonly float[] Weights = { 0.1f, -0.25f, 1.5f };

        /// <summary>A small world whose prey carries a vector gene labelled <c>weights</c>, optionally recorded.</summary>
        World VectorWorld(string name, bool record = false)
        {
            var b = New(4, name: name).Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>()
                .Species("prey", s =>
                {
                    s.PreyBody().LifeRules(4, 0).Action<RestAction>("rest", founders: new[] { "Rest often when tired." });
                    s.Module<VectorProbeGene>(v => { v.SetLabel("weights"); v.Length = 3; });
                });
            if (record) b.Recorder(true, "Logs/EvoSim/test-runs", name);
            return b.Build();
        }

        [Test, Description("T-NEURO-01 (GENE-07, GENE-10; plan M16 C1): a vector's canonical form is its length and a hash of its values at the decimals, stable across runs; copied on creation")]
        public void VectorCanonicalForm()
        {
            var source = (float[])Weights.Clone();
            var v = AlleleValue.OfVector(source);
            source[0] = 9f;                                                         // the value owns a copy
            Assert.AreEqual(AlleleKind.Vector, v.Kind);
            Assert.AreEqual(3, v.VectorLength);
            Assert.AreEqual(0.1f, v.Vector[0]);
            Assert.AreEqual("vec[3]#9d0f0f5248b39c19", v.Canonical(), "pinned: the same in every run and on every machine");
            Assert.AreEqual("vec[3]#36c89d619da9bb75", v.Canonical(2));
            Assert.AreEqual("vec[3]#9d0f0f52", v.ToString());
            Assert.AreEqual("60.0000", AlleleValue.OfNumber(60f).Canonical(), "numbers unchanged");
            Assert.AreEqual("Eat now.", AlleleValue.OfText(" Eat  now. ").Canonical(), "sentences unchanged");
        }

        [Test, Description("T-NEURO-01 (GENE-10; plan M16 C1): vectors equal at 4 decimals are one allele; a change in the 3rd decimal is another")]
        public void VectorsCompareAtTheGenesDecimals()
        {
            var w = VectorWorld("vector registry");
            var gene = w.FindSpecies("prey").Genes.First(g => g.Kind == AlleleKind.Vector);
            var a = w.Alleles.Register(gene, AlleleValue.OfVector(Weights), "custom");
            var same = w.Alleles.Register(gene, AlleleValue.OfVector(new[] { 0.10004f, -0.25003f, 1.49996f }), "custom");
            var other = w.Alleles.Register(gene, AlleleValue.OfVector(new[] { 0.101f, -0.25f, 1.5f }), "custom");
            Assert.AreSame(a, same, "equal at 4 decimals: one allele");
            Assert.AreNotSame(a, other, "differ in the 3rd decimal: two alleles");
            Assert.AreEqual(AlleleValue.OfVector(Weights), a.Value);
            Assert.AreNotEqual(AlleleValue.OfVector(Weights), other.Value);
            Assert.AreNotEqual(AlleleValue.OfVector(new[] { 0.1f, -0.25f }), a.Value, "the length is part of the value");
        }

        [Test, Description("T-NEURO-01 (13, GENE-12; plan M16 C2, C7): alleles.jsonl names a vector by vector_sha and length, never its values; vectors.jsonl holds the values; parent2_id only on a recombinant")]
        public void VectorsAndRecombinantsAreRecorded()
        {
            var w = VectorWorld("vector record", record: true);
            var prey = w.FindSpecies("prey");
            var gene = prey.Genes.First(g => g.Kind == AlleleKind.Vector);
            var a = w.Alleles.Register(gene, AlleleValue.OfVector(Weights), "custom");
            var b = w.Alleles.Register(gene, AlleleValue.OfVector(new[] { 1f, 2f, 3f }), "custom");
            var child = w.Alleles.Register(gene, AlleleValue.OfVector(new[] { 0.1f, 2f, 3f }), "recombinant", a.Id, secondParent: b.Id);
            Assert.AreEqual(b.Id, child.SecondParentId);
            Assert.AreEqual("recombinant", child.Origin);
            w.Advance(3);
            w.Stop("test");

            string folder = w.Service<RunRecorder>().RunFolder;
            var alleles = File.ReadAllLines(Path.Combine(folder, "alleles.jsonl"));
            var vectorLines = alleles.Where(l => l.Contains("\"locus\": \"prey.weights\"")).ToList();
            Assert.AreEqual(w.Alleles.CountAt("prey.weights"), vectorLines.Count);
            foreach (var line in vectorLines)
            {
                StringAssert.Contains("\"vector_sha\": ", line);
                StringAssert.Contains("\"length\": 3", line);
                StringAssert.DoesNotContain("\"values\"", line, "never the values");
                StringAssert.DoesNotContain("\"value\"", line);
            }
            StringAssert.Contains("\"vector_sha\": \"9d0f0f5248b39c19\"", vectorLines.Single(l => l.Contains("\"id\": \"" + a.Id + "\"")));
            Assert.AreEqual(1, alleles.Count(l => l.Contains("\"parent2_id\"")), "parent2_id only on the recombinant");
            StringAssert.Contains("\"parent2_id\": \"" + b.Id + "\"", alleles.Single(l => l.Contains("\"id\": \"" + child.Id + "\"")));

            var vectors = File.ReadAllLines(Path.Combine(folder, "vectors.jsonl"));
            Assert.AreEqual(vectorLines.Count, vectors.Length, "one line per vector allele");
            Assert.IsTrue(vectors.Any(l => l == "{\"id\": \"" + a.Id + "\", \"values\": [0.1, -0.25, 1.5]}"), string.Join("\n", vectors));
        }

        [Test, Description("13 (plan M16 C2): a run without a vector gene writes no vectors.jsonl")]
        public void NoVectorsFileWithoutVectorGenes()
        {
            var w = New(4, name: "no vectors").Lab1(prey: 4, predators: 1).Recorder(true, "Logs/EvoSim/test-runs", "no vectors").Build();
            w.Advance(3);
            w.Stop("test");
            Assert.IsFalse(File.Exists(Path.Combine(w.Service<RunRecorder>().RunFolder, "vectors.jsonl")));
        }

        [Test, Description("DEC-10, CTRL-01 (plan M16 C3): a query carries the genome the brain reads, the animal's own or the shuffled one, and its key is that genome's brain key")]
        public void QueriesCarryTheGenomeTheBrainReads()
        {
            foreach (bool shuffled in new[] { false, true })
            {
                ScriptedBrain brain = null;
                var w = New(5, name: "genome " + shuffled).Lab1(prey: 6, predators: 1, randomBrain: false)
                    .DefaultBrain<ScriptedBrain>(x => brain = x).Configure(x => x.ShuffledGenes = shuffled).Build();
                w.Advance(1);
                Assert.Greater(brain.Seen.Count, 0);
                int lent = 0;
                foreach (var q in brain.Seen)
                {
                    Assert.IsNotNull(q.Genome);
                    Assert.AreEqual(q.Genome.BrainKey, q.GenomeKey, "the key is the read genome's (DEC-31)");
                    var own = q.Species.Animals.First(a => a.Id == q.AnimalId).Genome;
                    if (!ReferenceEquals(own, q.Genome)) lent++;
                }
                if (shuffled) Assert.Greater(lent, 0, "the shuffled control lends other genomes");
                else Assert.AreEqual(0, lent, "the animal's own genome");
            }
        }

        [Test, Description("V-05 (plan M16 C5): an action without a text gene is reported unless the species' brain reads no sentence")]
        public void BrainsThatReadNoTextNeedNoTextGenes()
        {
            foreach (bool reads in new[] { true, false })
            {
                var w = New(2, name: "reads " + reads).Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<ScriptedBrain>(x => x.ReadsText = reads)
                    .Species("prey", s => s.PreyBody().LifeRules(4, 0).Action<RestAction>("rest", withGene: false)).Build();
                Assert.AreEqual(reads, w.LastReport.Has("V-05"), w.LastReport.ToString());
            }
        }
    }
}
