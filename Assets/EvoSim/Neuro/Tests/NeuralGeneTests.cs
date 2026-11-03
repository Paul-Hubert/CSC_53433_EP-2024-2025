using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// The network's genes (plan M16 §5.1–§5.3): the layout and the vector gene (T-NEURO-02), random float mutation
    /// (T-NEURO-03) and the per-neuron crossover (T-NEURO-04).
    /// </summary>
    public class NeuralGeneTests : WorldFixture
    {
        readonly List<GameObject> loose = new List<GameObject>();            // modules made outside a world

        /// <summary>Destroys the modules a test made outside a world.</summary>
        [TearDown]
        public void DestroyLoose()
        {
            foreach (var go in loose) if (go != null) Object.DestroyImmediate(go);
            loose.Clear();
        }

        /// <summary>
        /// A small world whose prey has the reference senses, rest and eat, and a network of one hidden layer of
        /// <paramref name="hidden"/>, with the neural mutation and crossover.
        /// </summary>
        World NeuralWorld(string name, int seed = 3, int hidden = 3, int founders = 8, int prey = 6)
        {
            return New(seed, name: name).Flat(30, 30).Food(0.1f, 0f).Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>()
                .Species("prey", s => s.PreyBody().PreySenses()
                    .Action<RestAction>("rest", withGene: false).Action<EatAction>("eat", withGene: false)
                    .Module<Litter>(l => l.Configure(1, 1, 40f)).Module<CapRule>(c => c.Configure(20))
                    .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { hidden }, founders); }, "Brain")
                    .Module<NeuralMutation>().Module<NeuronCrossover>()
                    .Population(prey)).Build();
        }

        /// <summary>A free vector gene of <paramref name="length"/> values, outside any world.</summary>
        VectorProbeGene Probe(int length)
        {
            var go = new GameObject("probe");
            loose.Add(go);
            var g = go.AddComponent<VectorProbeGene>();
            g.Length = length;
            g.SetLabel("weights");
            return g;
        }

        static NeuralGenomeGene GeneOf(World w) => (NeuralGenomeGene)w.FindSpecies("prey").Genes.Single(g => g is NeuralGenomeGene);

        // ---- T-NEURO-02 ----

        [Test, Description("T-NEURO-02 (T-GENE-01, GENE-01…04, GENE-07): the network gene has an id, a label, the vector kind and a founder pool; it is read by the brain, sets no trait and writes one summary line")]
        public void GeneConformance()
        {
            var w = NeuralWorld("gene conformance");
            var s = w.FindSpecies("prey");
            var gene = GeneOf(w);
            Assert.AreEqual("prey.brain", gene.LocusId);
            Assert.AreEqual(AlleleKind.Vector, gene.Kind);
            Assert.IsTrue(gene.ReadByBrain, "the weights are in the brain-visible key (DEC-31)");
            Assert.AreEqual(8, gene.FounderPool.Count);
            Assert.IsTrue(gene.FounderPool.All(f => f.Origin == "founder" && f.Value.VectorLength == gene.Layout.VectorLength));
            Assert.IsTrue(gene.FounderPool.All(f => gene.Check(f.Value) == null), "founders pass the gene's checks");
            Assert.AreEqual(8, gene.FounderPool.Select(f => f.Value.Canonical()).Distinct().Count(), "random founders differ");

            var lines = new List<KeyValuePair<string, string>>();
            var a = s.Animals[0];
            var e = new Expression(null, s.Declarations, lines);
            gene.Express(a.Genome[gene.Locus].Value, e);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("brain", lines[0].Key);
            StringAssert.IsMatch(@"^vec\[\d+\]#[0-9a-f]{8}$", lines[0].Value);

            Assert.IsNotNull(gene.Check(AlleleValue.OfVector(new float[3])), "wrong length");
            var bad = gene.FounderPool[0].Value.Vector.ToArray();
            bad[0] = float.NaN;
            Assert.IsNotNull(gene.Check(AlleleValue.OfVector(bad)), "NaN");
            bad[0] = gene.Clamp + 1f;
            Assert.IsNotNull(gene.Check(AlleleValue.OfVector(bad)), "beyond the clamp");
        }

        [Test, Description("T-NEURO-02 (plan M16 D2, §5.1): the vector length is Σ (fan-in + 1) × fan-out for 1–3 senses, 1–8 actions and 0–2 hidden layers; a species' inputs are its senses' widths")]
        public void VectorLengthFollowsTheLayout()
        {
            foreach (var widths in new[] { new[] { 4 }, new[] { 3, 7 }, new[] { 2, 5, 9 } })
                for (int actions = 1; actions <= 8; actions++)
                    foreach (var hidden in new[] { new int[0], new[] { 12 }, new[] { 6, 4 } })
                    {
                        int inputs = widths.Sum();
                        var layout = new NeuralLayout(inputs, hidden, actions);
                        int expected = 0, previous = inputs;
                        foreach (int h in hidden.Concat(new[] { actions })) { expected += (previous + 1) * h; previous = h; }
                        Assert.AreEqual(expected, layout.VectorLength, $"{inputs} → {string.Join(",", hidden)} → {actions}");
                        Assert.AreEqual(expected, NeuralLayout.LengthOf(inputs, hidden, actions));
                        // Every weight belongs to exactly one neuron slice.
                        var covered = new bool[expected];
                        for (int l = 0; l < layout.LayerCount; l++)
                            for (int j = 0; j < layout.Sizes[l + 1]; j++)
                                for (int k = 0; k < layout.SliceLength(l); k++)
                                {
                                    int at = layout.NeuronOffset(l, j) + k;
                                    Assert.IsFalse(covered[at], "slices overlap");
                                    covered[at] = true;
                                    Assert.AreEqual(l, layout.LayerOf(at));
                                }
                        Assert.IsTrue(covered.All(c => c), "slices cover the vector");
                    }
            Assert.AreEqual(1229, NeuralLayout.LengthOf(96, new[] { 12 }, 5), "the measured runs' templates (plan §5.1)");
            Assert.AreEqual(1373, NeuralLayout.LengthOf(108, new[] { 12 }, 5));

            var w = NeuralWorld("layout of a species", hidden: 12);
            var s = w.FindSpecies("prey");
            var layoutOfSpecies = GeneOf(w).Layout;
            Assert.AreEqual(s.Senses.Sum(x => x.Tokens.Count), layoutOfSpecies.Inputs, "a sense without attachment: a one-hot of its tokens");
            Assert.AreEqual(s.Actions.Count, layoutOfSpecies.Outputs, "one output per action");
            Assert.AreEqual(NeuralLayout.LengthOf(layoutOfSpecies.Inputs, new[] { 12 }, s.Actions.Count), layoutOfSpecies.VectorLength);
        }

        [Test, Description("T-NEURO-02 (GENE-21): the random founder pool is the same in two worlds with different run seeds")]
        public void FoundersIgnoreTheRunSeed()
        {
            var one = GeneOf(NeuralWorld("founders 1", seed: 1)).FounderPool.Select(f => f.Value.Canonical()).ToList();
            var two = GeneOf(NeuralWorld("founders 2", seed: 2)).FounderPool.Select(f => f.Value.Canonical()).ToList();
            CollectionAssert.AreEqual(one, two);
        }

        [Test, Description("T-NEURO-02 (V-71): adding an action after the founders were drawn → V-71 with a fix that rebuilds them")]
        public void AddingAnActionReportsV71()
        {
            var w = NeuralWorld("v71");
            var gene = GeneOf(w);
            int before = gene.FounderPool[0].Value.VectorLength;
            var actions = w.FindSpecies("prey").GetComponentsInChildren<RestAction>(true)[0].transform.parent;
            var go = new GameObject("walk");
            go.transform.SetParent(actions, false);
            go.AddComponent<RestAction>();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("V-71"));
            Assert.IsFalse(w.Initialize(), "the founders no longer fit");
            Assert.IsTrue(w.LastReport.Has("V-71"), w.LastReport.ToString());
            var fix = w.LastReport.Messages.First(m => m.Id == "V-71").Fix;
            Assert.IsNotNull(fix, "a fix: rebuild founders");
            fix.Apply();
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.AreEqual(before + 4, gene.FounderPool[0].Value.VectorLength, "one more output: its bias and 3 hidden weights");
            Assert.AreEqual(NeuralLayout.LengthOf(gene.Layout.Inputs, new[] { 3 }, 3), gene.FounderPool[0].Value.VectorLength, "rebuilt for three actions");
        }

        // ---- T-NEURO-03 ----

        [Test, Description("T-NEURO-03 (T-MUT template, MUT-20, MUT-30): the same stream gives the same mutant; it differs from its parent, passes the gene's checks and stays clamped")]
        public void MutationIsDeterministicAndClamped()
        {
            var w = NeuralWorld("mutation");
            var gene = GeneOf(w);
            var op = w.FindSpecies("prey").Module<NeuralMutation>();
            Assert.AreEqual(0.5f, op.Rate, "the Gentle rate");
            Assert.IsTrue(op.Accepts(gene));
            var parent = w.Alleles.Register(gene, gene.FounderPool[0].Value, "founder");
            for (int k = 0; k < 50; k++)
            {
                var a = op.StartMutation(gene, parent, new RandomStream(k, "mutation"));
                var b = op.StartMutation(gene, parent, new RandomStream(k, "mutation"));
                Assert.IsTrue(a.IsDone && a.Succeeded, "finished at once");
                Assert.AreEqual(a.Result, b.Result, "same stream, same mutant");
                Assert.AreNotEqual(parent.Value, a.Result, "never its parent (MUT-04)");
                Assert.IsNull(gene.Check(a.Result));
                StringAssert.StartsWith("nn-", a.Operator);
            }
            // A network at the clamp, pushed hard: every weight stays within ±clamp.
            var edge = Enumerable.Repeat(gene.Clamp, gene.Layout.VectorLength).ToArray();
            var high = w.Alleles.Register(gene, AlleleValue.OfVector(edge), "custom");
            op.AddChance = 1f;
            op.AddStrength = 5f;
            var pushed = op.StartMutation(gene, high, new RandomStream(1, "mutation")).Result;
            foreach (float v in pushed.Vector.ToArray()) Assert.LessOrEqual(Mathf.Abs(v), gene.Clamp);
            // The same number of draws whatever happens (MUT-30).
            var r1 = new RandomStream(5, "m");
            var r2 = new RandomStream(5, "m");
            op.AddChance = 0f; op.ReplaceChance = 0f;
            op.StartMutation(gene, parent, r1);
            op.AddChance = 1f; op.ReplaceChance = 0f;
            op.StartMutation(gene, parent, r2);
            Assert.AreEqual(r1.Draws, r2.Draws);
        }

        [Test, Description("T-NEURO-03: over 10 000 mutations of 200 weights, the shares of added and replaced weights match pAdd and pReplace within 4 s.e.")]
        public void MutationSharesMatchTheSettings()
        {
            var gene = Probe(200);
            gene.Clamp = 100f;
            var go = new GameObject("op");
            loose.Add(go);
            var op = go.AddComponent<NeuralMutation>();
            op.AddStrength = 0.01f;                     // added: |v| ≤ 0.01
            op.ReplaceStrength = 10f;                   // replaced: |v| up to 10 (below 0.01 with chance 0.1 %)
            var parent = new Allele("probe.weights", 0, AlleleValue.OfVector(new float[200]), "custom");
            var rng = new RandomStream(7, "mutation");
            long added = 0, replaced = 0, total = 0;
            for (int k = 0; k < 10000; k++)
            {
                var v = op.StartMutation(gene, parent, rng).Result.Vector;
                for (int i = 0; i < v.Length; i++)
                {
                    float x = Mathf.Abs(v[i]);
                    if (x == 0f) continue;
                    if (x <= 0.01f) added++; else replaced++;
                }
                total += v.Length;
            }
            void Near(double p, long count, string what)
            {
                double share = (double)count / total, se = System.Math.Sqrt(p * (1 - p) / total);
                Assert.That(share, Is.InRange(p - 4 * se - 0.001, p + 4 * se + 0.001), $"{what}: {share:0.0000} for {p}");
            }
            Near(op.AddChance, added, "added");
            Near(op.ReplaceChance, replaced, "replaced");
        }

        // ---- T-NEURO-04 ----

        [Test, Description("T-NEURO-04 (GENE-30, GENE-32, GENE-12): each neuron slice comes whole from one parent; the same stream gives the same baby; a recombinant is registered once with both parents; a child equal to a parent reuses its allele")]
        public void NeuronCrossoverMixesWholeNeurons()
        {
            var w = NeuralWorld("crossover", hidden: 4);
            var s = w.FindSpecies("prey");
            var gene = GeneOf(w);
            var cross = s.Module<NeuronCrossover>();
            int locus = gene.Locus;
            var first = s.Animals[0];
            var second = s.Animals.First(x => x.Genome[locus] != first.Genome[locus]);
            var pa = first.Genome[locus].Value.Vector.ToArray();
            var pb = second.Genome[locus].Value.Vector.ToArray();
            var layout = gene.Layout;

            int before = w.Alleles.CountAt(gene.LocusId);
            var child = cross.Cross(first.Genome, second.Genome, new RandomStream(3, "mutation"))[locus];
            var again = cross.Cross(first.Genome, second.Genome, new RandomStream(3, "mutation"))[locus];
            Assert.AreSame(child, again, "same stream, same baby, registered once");
            Assert.AreEqual("recombinant", child.Origin);
            Assert.AreEqual(first.Genome[locus].Id, child.ParentId);
            Assert.AreEqual(second.Genome[locus].Id, child.SecondParentId);
            Assert.AreEqual(before + 1, w.Alleles.CountAt(gene.LocusId));

            var c = child.Value.Vector.ToArray();
            for (int l = 0; l < layout.LayerCount; l++)
                for (int j = 0; j < layout.Sizes[l + 1]; j++)
                {
                    int at = layout.NeuronOffset(l, j), n = layout.SliceLength(l);
                    bool fromA = Enumerable.Range(at, n).All(i => c[i] == pa[i]);
                    bool fromB = Enumerable.Range(at, n).All(i => c[i] == pb[i]);
                    Assert.IsTrue(fromA || fromB, $"neuron {j} of layer {l} comes whole from one parent");
                }

            Assert.AreSame(first.Genome[locus], cross.Cross(first.Genome, first.Genome, new RandomStream(4, "mutation"))[locus], "equal parents: the parent's allele");
            var babies = Enumerable.Range(0, 20).Select(k => cross.Cross(first.Genome, second.Genome, new RandomStream(100 + k, "mutation"))[locus]).ToList();
            Assert.Greater(babies.Distinct().Count(), 1, "siblings differ (GENE-32)");
        }
    }
}
