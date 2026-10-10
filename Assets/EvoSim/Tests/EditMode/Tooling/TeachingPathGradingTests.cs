using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EvoSim.Editor;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EvoSim.Tests
{
    /// <summary>
    /// The grading of the teaching path (22 §0, 30 §4): every exercise class a student wrote (My&lt;Reference&gt; in
    /// Assets/Student), and every reference module as a check of the grading itself, runs the Lab scene in place of its
    /// reference. Deterministic modules must give the reference's events hash; modules that draw at random (crossover,
    /// litter, LLM mutation) pass property checks. An unwritten stub throws, so its cases fail until written.
    /// </summary>
    public class TeachingPathGradingTests : WorldFixture
    {
        const string Scene = "Assets/EvoSim/Scenes/Lab1_Small.unity";
        const int Seed = 7, Ticks = 300;
        static readonly Type[] RandomDraws = { typeof(UniformCrossover), typeof(Litter), typeof(LlmMutation) };
        static readonly Dictionary<Type, string> referenceHash = new Dictionary<Type, string>();

        /// <summary>The reference modules, then the students' exercise classes (assemblies outside EvoSim).</summary>
        static IEnumerable<Type> Graded() => Exercises.Path.Concat(TypeCache.GetTypesWithAttribute<ExerciseOfAttribute>()
            .Where(t => !t.IsAbstract && !t.Assembly.GetName().Name.StartsWith("EvoSim", StringComparison.Ordinal))
            .OrderBy(t => t.FullName, StringComparer.Ordinal));

        static IEnumerable<Type> Deterministic() => Graded().Where(t => !RandomDraws.Contains(ReferenceOf(t)));
        static IEnumerable<Type> Crossovers() => Graded().Where(t => ReferenceOf(t) == typeof(UniformCrossover));
        static IEnumerable<Type> Litters() => Graded().Where(t => ReferenceOf(t) == typeof(Litter));
        static IEnumerable<Type> Mutations() => Graded().Where(t => ReferenceOf(t) == typeof(LlmMutation));

        static Type ReferenceOf(Type t) => t.GetCustomAttribute<ExerciseOfAttribute>()?.Reference ?? t;

        /// <summary>The scene's mutator client replaced by the fake one (as R-01), then every reference module swapped for the graded class.</summary>
        static void Configure(World w, Type graded, ref int swapped)
        {
            var client = w.GetComponentInChildren<MutatorClient>(true);
            var go = client != null ? client.gameObject : w.GetComponentInChildren<MutatorService>(true)?.gameObject;
            if (client != null) Object.DestroyImmediate(client);
            if (go != null) go.AddComponent<FakeMutator>();
            var reference = ReferenceOf(graded);
            var targets = new List<Component>();
            foreach (var c in w.GetComponentsInChildren(reference, true)) if (c.GetType() == reference) targets.Add(c);
            foreach (var c in targets)
            {
                var made = Exercises.Swap(c, graded);
                if (made is Brain brain && (w.DefaultBrain == null || w.DefaultBrain == (Object)c)) w.DefaultBrain = brain;
                swapped++;
            }
            if (reference == typeof(RandomBrain) && w.DefaultBrain == null)
                w.DefaultBrain = (Brain)w.GetComponentsInChildren(graded, true).FirstOrDefault();
        }

        static string Run(Type graded, out int swapped)
        {
            int n = 0;
            string hash = Batch.HashOf(Scene, Seed, Ticks, w => Configure(w, graded, ref n));
            swapped = n;
            return hash;
        }

        [Test, Description("22 §0: a deterministic module in place of its reference gives the reference's run (same events hash, Lab1_Small, 300 ticks)")]
        public void SameRunAsTheReference([ValueSource(nameof(Deterministic))] Type graded)
        {
            var reference = ReferenceOf(graded);
            if (!referenceHash.TryGetValue(reference, out var expected)) referenceHash[reference] = expected = Run(reference, out _);
            string actual = Run(graded, out int swapped);
            Assert.Greater(swapped, 0, $"Lab1_Small uses {reference.Name}");
            Assert.AreEqual(expected, actual, $"{graded.Name} doesn't do what {reference.Name} does (same seed, different events)");
        }

        [Test, Description("22 §0: a crossover takes each locus from one parent or the other, each about half the time (GENE-30), and the world repeats with it")]
        public void CrossoverChecks([ValueSource(nameof(Crossovers))] Type graded)
        {
            Assert.AreEqual(Run(graded, out int swapped), Run(graded, out _), "same seed, same run (RAND-11)");
            Assert.Greater(swapped, 0);
            Batch.WithScene(Scene, w =>
            {
                int unused = 0;
                Configure(w, graded, ref unused);
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                w.DefaultBrain = w.GetComponentInChildren<RandomBrain>(true);
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                var s = w.FindSpecies("prey");
                var crossover = (Crossover)s.GetComponentInChildren(graded, true);
                var a = s.Animals[0].Genome;
                var b = s.Animals.Select(x => x.Genome).First(g => Enumerable.Range(0, g.Count).Any(i => g[i] != a[i]));
                var rng = new RandomStream(5, "grading");
                int fromA = 0, differing = 0;
                for (int k = 0; k < 2000; k++)
                {
                    var child = crossover.Cross(a, b, rng);
                    Assert.AreEqual(a.Count, child.Length);
                    for (int i = 0; i < child.Length; i++)
                    {
                        Assert.IsTrue(ReferenceEquals(child[i], a[i]) || ReferenceEquals(child[i], b[i]), "each allele comes from a parent");
                        if (a[i] == b[i]) continue;
                        differing++;
                        if (ReferenceEquals(child[i], a[i])) fromA++;
                    }
                }
                Assert.AreEqual(0.5, (double)fromA / differing, 0.03, "probability one half");
                return "";
            });
        }

        [Test, Description("22 §0: a litter is uniform in [min, max], cut to what the parents can pay, never below min (REPRO-10)")]
        public void LitterChecks([ValueSource(nameof(Litters))] Type graded)
        {
            Assert.AreEqual(Run(graded, out int swapped), Run(graded, out _), "same seed, same run (RAND-11)");
            Assert.Greater(swapped, 0);
            var go = new GameObject("Litter grading");
            try
            {
                var litter = (Litter)go.AddComponent(graded);
                litter.Configure(2, 4, 40f);
                var rng = new RandomStream(9, "grading");
                var seen = new int[5];
                for (int k = 0; k < 3000; k++) seen[litter.Size(rng, new[] { 1000f, 1000f })]++;
                Assert.AreEqual(0, seen[0] + seen[1], "never below min");
                Assert.That(seen.Skip(2).Min(), Is.GreaterThan(850), "uniform in [2, 4]: " + string.Join(", ", seen));
                for (int k = 0; k < 200; k++) Assert.LessOrEqual(litter.Size(rng, new[] { 60f, 1000f }), 3, "a parent with 60 pays 20 per baby: at most 3");
                Assert.AreEqual(2, litter.Size(rng, new[] { 1f, 1f }), "never below min, even when the parents can't pay");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test, Description("22 §0: an LLM mutation runs with the offline mutator, repeats with the same seed (MUT-30) and makes mutated alleles")]
        public void MutationChecks([ValueSource(nameof(Mutations))] Type graded)
        {
            Assert.AreEqual(Run(graded, out int swapped), Run(graded, out _), "same seed, same run (MUT-30, RAND-11)");
            Assert.Greater(swapped, 0);
            Batch.WithScene(Scene, w =>
            {
                int unused = 0;
                w.Seed = Seed;
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                Configure(w, graded, ref unused);
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                w.DefaultBrain = w.GetComponentInChildren<RandomBrain>(true);
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                w.Advance(1500);
                int births = w.AllSpecies.Sum(s => s.Counters.Births);
                int mutated = w.AllSpecies.SelectMany(s => s.Animals).SelectMany(a => a.Genome.Alleles).Count(x => x.ParentId != null);
                Assume.That(births, Is.GreaterThan(0), "no birth in 1 500 ticks");
                Assert.Greater(mutated, 0, "babies carry mutated alleles");
                return "";
            });
        }
    }
}
