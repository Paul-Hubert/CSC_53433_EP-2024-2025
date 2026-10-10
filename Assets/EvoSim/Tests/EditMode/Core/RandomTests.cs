using System.Collections.Generic;
using EvoSim.Testing;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>Named random streams (RAND-02…05) and the events hash (RAND-10, RAND-13).</summary>
    public class RandomTests : WorldFixture
    {
        [Test, Description("RAND-02: a stream is a function of the seed and its name only")]
        public void StreamsAreReproducible()
        {
            var a = new RandomStream(1234, "prey/sampling");
            var b = new RandomStream(1234, "prey/sampling");
            var c = new RandomStream(1234, "prey/actions");
            var d = new RandomStream(7, "prey/sampling");
            bool differC = false, differD = false;
            for (int i = 0; i < 100; i++)
            {
                uint x = a.NextUInt();
                Assert.AreEqual(x, b.NextUInt());
                differC |= c.NextUInt() != x;
                differD |= d.NextUInt() != x;
            }
            Assert.IsTrue(differC, "another name, another sequence");
            Assert.IsTrue(differD, "another seed, another sequence");
        }

        [Test, Description("T-RAND-02, stream part (RAND-02, RAND-03): extra draws in one stream never shift another")]
        public void StreamsAreIndependent()
        {
            var s1 = new RandomStreams(1234);
            var s2 = new RandomStreams(1234);
            for (int i = 0; i < 500; i++) s2.Get("Grass").NextUInt();         // extra draws in the food stream
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(s1.Get("prey/litter").NextUInt(), s2.Get("prey/litter").NextUInt());
            Assert.AreSame(s1.Get("x"), s1.Get("x"), "one stream per name");
        }

        [Test, Description("RAND-04: the world stream uses the world seed; the others the run seed")]
        public void WorldSeedIsSeparate()
        {
            var a = new RandomStreams(1, 99);
            var b = new RandomStreams(2, 99);
            Assert.AreEqual(a.World.NextUInt(), b.World.NextUInt(), "same map");
            Assert.AreNotEqual(a.Get("act-order").NextUInt(), b.Get("act-order").NextUInt(), "different run");
        }

        [Test, Description("RandomStream: ranges stay in bounds and are uniform (χ²)")]
        public void RangesAreUniform()
        {
            var r = new RandomStream(5, "test");
            var counts = new long[7];
            for (int i = 0; i < 70000; i++)
            {
                int v = r.Range(0, 7);
                Assert.That(v, Is.InRange(0, 6));
                counts[v]++;
            }
            Stat.Uniform(counts, 0.001, "Range(0, 7)");
            for (int i = 0; i < 10000; i++)
            {
                float f = r.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                double d = r.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test, Description("RandomStream: Choose follows the weights; Gaussian has mean 0 and variance 1")]
        public void ChooseAndGaussian()
        {
            var r = new RandomStream(11, "test");
            var w = new[] { 0.1f, 0.0f, 0.6f, 0.3f };
            long n = 100000, hits0 = 0, hits1 = 0, hits2 = 0;
            for (int i = 0; i < n; i++)
            {
                int k = r.Choose(w);
                if (k == 0) hits0++;
                if (k == 1) hits1++;
                if (k == 2) hits2++;
            }
            Stat.Binomial(hits0, n, 0.1, 4, "p0");
            Assert.AreEqual(0, hits1, "a zero weight is never drawn");
            Stat.Binomial(hits2, n, 0.6, 4, "p2");

            var g = new List<double>();
            for (int i = 0; i < 20000; i++) g.Add(r.NextGaussian());
            Stat.Mean(g, 0.0, 4, "gaussian mean");
            var sq = new List<double>();
            foreach (var x in g) sq.Add(x * x);
            Stat.Mean(sq, 1.0, 4, "gaussian variance");
        }

        [Test, Description("T-RAND-05, EditMode part (RAND-10, RAND-11, RAND-13, CORE-09): same seed same hash, another seed another hash")]
        public void SameSeedSameHash()
        {
            string Run(int seed)
            {
                var w = New(seed).Flat(20, 20).Phase<RandomEventPhase>().Species("prey", s => s.Population(5).Action<ProbeAction>(founders: new[] { "Eat.", "Rest." })).Build();
                w.Advance(50);
                return w.Events.Hash;
            }
            Assert.AreEqual(Run(1234), Run(1234));
            Assert.AreNotEqual(Run(1234), Run(7));
        }

        [Test, Description("RAND-10: events are canonical JSON with sorted keys; the hash is chained over the lines")]
        public void EventsAreCanonical()
        {
            var e = new SimEvent("death", 12, 7, "prey").With("cause", "starvation").With("age", 300).With("food", 2.5f)
                .With("genome", new List<string> { "prey.eat:0", "prey.flee:1" });
            Assert.AreEqual("{\"age\": 300, \"cause\": \"starvation\", \"food\": 2.5, \"genome\": [\"prey.eat:0\", \"prey.flee:1\"], " +
                            "\"id\": 7, \"kind\": \"death\", \"species\": \"prey\", \"t\": 12}", e.ToJson());
            Assert.AreEqual("40.0", CanonicalJson.Number(40.0));
            Assert.AreEqual("0", CanonicalJson.Number(-0.0));
            Assert.AreEqual("\"caf\\u00e9\"", CanonicalJson.Write("café"));

            var a = new EventLog(1, 1);
            var b = new EventLog(1, 1);
            Assert.AreEqual(a.Hash, b.Hash);
            a.Record(e);
            Assert.AreNotEqual(a.Hash, b.Hash);
            b.Record(new SimEvent("death", 12, 7, "prey").With("food", 2.5f).With("age", 300).With("cause", "starvation")
                .With("genome", new List<string> { "prey.eat:0", "prey.flee:1" }));
            Assert.AreEqual(a.Hash, b.Hash, "insertion order of the fields doesn't matter (RAND-05)");
            Assert.AreNotEqual(new EventLog(1, 1).Hash, new EventLog(2, 1).Hash, "RAND-13 even for an empty world");
        }

        [Test, Description("T-CORE-09 (CORE-05, ARCH-07): two worlds run interleaved tick by tick give the hashes they give alone")]
        public void WorldsDontShareState()
        {
            World Make(int seed, string name) => New(seed, name: name).Flat(20, 20).Phase<RandomEventPhase>()
                .Species("prey", s => s.Population(4).Action<ProbeAction>(founders: new[] { "Eat.", "Rest.", "Run." })).Build();

            var aloneA = Make(1, "A alone"); aloneA.Advance(100);
            var aloneB = Make(2, "B alone"); aloneB.Advance(100);
            var a = Make(1, "A");
            var b = Make(2, "B");
            for (int t = 0; t < 100; t++) { a.Advance(1); b.Advance(1); }
            Assert.AreEqual(aloneA.Events.Hash, a.Events.Hash);
            Assert.AreEqual(aloneB.Events.Hash, b.Events.Hash);
            Assert.AreNotEqual(a.AllSpecies[0].Animals[0].Id, -1);
            Assert.AreEqual(0, a.AllSpecies[0].Animals[0].Id, "animal ids are per world");
            Assert.AreEqual(0, b.AllSpecies[0].Animals[0].Id);
        }
    }
}
