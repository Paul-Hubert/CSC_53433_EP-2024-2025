using System;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>The neural brain (plan M16 D1, §5.6): rows, the Greedy and Sample policies, and failures (T-NEURO-07).</summary>
    public class NeuralBrainTests : WorldFixture
    {
        /// <summary>A 30 m world whose prey and predator run their own networks (fan, interoception, a hidden layer of 4).</summary>
        World NeuralWorld(string name, int prey = 10, int predators = 3, Action<NeuralBrain> brain = null, float tau = 1f)
        {
            return New(9, name: name).Flat(30, 30).Food(0.1f, 0.001f).Cover(0.1f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<NeuralBrain>(brain)
                .Configure(w => w.SamplingTemperature = tau)
                .Species("prey", s => s.PreyBody()
                    .Sense<RayFanSense>(f => f.Configure(12, 360f, RayFanSense.PreyKinds))
                    .Sense<InteroceptionSense>().Module<HungerClock>()
                    .Action<RestAction>("rest", withGene: false).Action<EatAction>("eat", withGene: false)
                    .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { 4 }, 8); }, "Brain")
                    .Population(prey))
                .Species("predator", s => s.PredatorBody("prey")
                    .Sense<RayFanSense>(f => f.Configure(12, 360f, RayFanSense.PredatorKinds))
                    .Sense<InteroceptionSense>().Module<HungerClock>()
                    .Action<RestAction>("rest", withGene: false).Action<HuntAction>("hunt", withGene: false)
                    .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { 4 }, 8); }, "Brain")
                    .Population(predators))
                .Build();
        }

        /// <summary>The query the ask-brains phase would build for this animal now: tokens, attachments and genome.</summary>
        static DecisionQuery QueryOf(Animal a, bool withGenome = true)
        {
            var s = a.Species;
            var ctx = new SenseContext(s.World);
            var o = s.Observe(a, ctx);
            var attachments = s.Senses.Where(x => x.HasAttachments).Select(x => x.Attachment(a, ctx)).Where(x => x != null).ToList();
            return new DecisionQuery(s, new List<KeyValuePair<string, string>>(), o, s.Describe(o, TextStyle.V1), TextStyle.V1,
                                     a.Genome.BrainKey, attachments, withGenome ? a.Genome : null);
        }

        /// <summary>The network's outputs for a query, computed here from the layout, the weights and the inputs.</summary>
        static float[] Outputs(DecisionQuery q)
        {
            var gene = q.Species.Module<NeuralGenomeGene>();
            var layout = gene.Layout;
            var input = new float[layout.Inputs];
            foreach (var slot in layout.Slots)
            {
                if (slot.Attached)
                {
                    var n = q.Attachments.OfType<NeuralInput>().Single(x => x.SenseIndex == slot.SenseIndex);
                    Array.Copy(n.Values, 0, input, slot.Offset, slot.Width);
                }
                else input[slot.Offset + q.Observation[slot.SenseIndex]] = 1f;
            }
            var output = new float[layout.Outputs];
            new NeuralNet(layout, gene.Activation).Forward(q.Genome[gene.Locus].Value.Vector, input, output);
            return output;
        }

        [Test, Description("T-NEURO-07 (T-DEC-03, DEC-11, DEC-12): on both neural species, rows have the species' length, sum to 1 and repeat for the same query; Greedy is a one-hot on the largest output")]
        public void GreedyRows()
        {
            var w = NeuralWorld("greedy rows");
            w.Advance(2);
            var brain = w.GetComponentInChildren<NeuralBrain>();
            Assert.AreEqual("neural-greedy", brain.Id);
            Assert.IsFalse(brain.Memoizable);
            Assert.IsFalse(brain.ReadsGeneText);
            Assert.IsTrue(brain.AcceptsAttachments);
            var queries = w.AllSpecies.SelectMany(s => s.Animals.Where(a => a.IsAlive)).Select(a => QueryOf(a)).ToList();
            Assert.Greater(queries.Count, 8);
            var answer = brain.Ask(queries);
            var again = brain.Ask(queries);
            Assert.IsTrue(answer.IsDone);
            for (int i = 0; i < queries.Count; i++)
            {
                var row = answer.Row(i);
                Assert.IsNull(BrainAnswer.Check(row, queries[i].Species.Actions.Count), $"row {i}");
                CollectionAssert.AreEqual(row, again.Row(i), "deterministic");
                int best = NeuralNet.ArgMax(Outputs(queries[i]));
                for (int k = 0; k < row.Length; k++) Assert.AreEqual(k == best ? 1f : 0f, row[k], $"row {i}: one-hot on the largest output");
            }
        }

        [Test, Description("T-NEURO-07 (plan D1): Greedy ties go to the lower index; Sample is a softmax at the brain's τ that sums to 1")]
        public void Policies()
        {
            CollectionAssert.AreEqual(new[] { 0f, 1f, 0f, 0f }, NeuralBrain.ToRow(new[] { 0.1f, 2f, 2f, -1f }, NeuralPolicy.Greedy, 1f));
            CollectionAssert.AreEqual(new[] { 1f, 0f, 0f }, NeuralBrain.ToRow(new[] { 0f, 0f, 0f }, NeuralPolicy.Greedy, 1f));
            var outputs = new[] { 1f, 2f, 0.5f };
            foreach (var tau in new[] { 1f, 0.5f, 2f })
            {
                var row = NeuralBrain.ToRow(outputs, NeuralPolicy.Sample, tau);
                double z = outputs.Sum(o => Math.Exp(o / tau));
                for (int k = 0; k < 3; k++) Assert.AreEqual(Math.Exp(outputs[k] / tau) / z, row[k], 1e-6, $"τ {tau}");
                Assert.AreEqual(1.0, row.Sum(), 1e-6);
            }
            var w = NeuralWorld("sample id", prey: 2, predators: 1, brain: b => b.Configure(NeuralPolicy.Sample, 0.5f));
            Assert.AreEqual("neural-sample-0.5", w.GetComponentInChildren<NeuralBrain>().Id);
        }

        [Test, Description("T-NEURO-07 (DEC-20): with Greedy the drawn action is always the one-hot's, whatever the World's τ")]
        public void GreedyIgnoresTemperature()
        {
            foreach (var tau in new[] { 0.3f, 1f, 4f })
            {
                var w = NeuralWorld("tau " + tau, tau: tau);
                int decisions = 0;
                for (int t = 0; t < 10; t++)
                {
                    w.Advance(1);
                    foreach (var a in w.AllSpecies.SelectMany(s => s.Animals))
                    {
                        if (a.LastDecisionTick != w.Tick - 1 || a.LastProbabilities == null) continue;
                        Assert.AreEqual(Array.IndexOf(a.LastProbabilities, 1f), a.Action, $"τ {tau}: {a}");
                        decisions++;
                    }
                }
                Assert.Greater(decisions, 20, $"τ {tau}: decisions seen");
            }
        }

        [Test, Description("T-NEURO-07 (DEC-40): a query without a genome, or with weights of the wrong length, fails; it never gets a made-up row")]
        public void FailuresAreNotRows()
        {
            var w = NeuralWorld("failures", prey: 3, predators: 1);
            var brain = w.GetComponentInChildren<NeuralBrain>();
            var a = w.FindSpecies("prey").Animals[0];
            var answer = brain.Ask(new[] { QueryOf(a, withGenome: false), QueryOf(a) });
            Assert.IsTrue(answer.Failed(0));
            StringAssert.Contains("genome", answer.Error(0));
            Assert.IsFalse(answer.Failed(1));

            var gene = a.Species.Module<NeuralGenomeGene>();
            var shortWeights = w.Alleles.Register(gene, AlleleValue.OfVector(new float[5]), "custom");
            var alleles = Enumerable.Range(0, a.Genome.Count).Select(i => i == gene.Locus ? shortWeights : a.Genome[i]).ToArray();
            var b = Place.Animal(a.Species, Place.At(3f, 3f), new Genome(a.Species, alleles));
            var bad = brain.Ask(new[] { QueryOf(b) });
            Assert.IsTrue(bad.Failed(0));
            StringAssert.Contains("V-71", bad.Error(0));
        }

        [Test, Description("T-NEURO-07 (V-70): a species with the neural brain and no NeuralGenomeGene is an error before the run")]
        public void BrainNeedsTheGene()
        {
            var b = New(2, name: "no gene").Flat(20, 20).ReferencePhases().DefaultBrain<NeuralBrain>()
                .Species("prey", s => s.Module<KinematicLocomotion>().Action<RestAction>("rest", withGene: false).Population(2));
            var r = new ValidationReport();
            Assert.IsFalse(b.BuildUninitialized().Prepare(r));
            Assert.IsTrue(r.Has("V-70"), r.ToString());
        }
    }
}
