using System.Collections.Generic;
using System.Linq;
using System.Text;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Senses, observations and situation texts (06).</summary>
    public class SenseTests : WorldFixture
    {
        World Eco(int seed = 1, float cover = 0.2f) =>
            New(seed).Ecology(48f, cover, 0f, regrow: 0f).Phase<ActPhase>().Phase<DeathPhase>().Build();

        static Observation Read(World w, Animal a) => a.Species.Observe(a, new SenseContext(w));

        static string Token(World w, Animal a, string label)
        {
            var sense = a.Species.FindSense(label);
            return sense.Tokens[sense.Read(a, new SenseContext(w))];
        }

        [Test, Description("T-SENSE-01 (SENSE-01, SENSE-06): every reference sense in 1 000 random world states returns a declared token; the \"nothing\" token when nothing is there")]
        public void SensesReturnDeclaredTokens()
        {
            var w = Eco(5);
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var food = w.Service<FoodGrid>();
            var rng = new RandomStream(7, "sense-test");
            var animals = new List<Animal>();
            for (int i = 0; i < 12; i++) animals.Add(Place.Animal(prey, Place.At(1, 1)));
            for (int i = 0; i < 4; i++) animals.Add(Place.Animal(predator, Place.At(1, 1)));
            for (int state = 0; state < 1000; state++)
            {
                food.Clear();
                for (int k = 0; k < 20; k++) Place.Food(food, Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f)));
                foreach (var a in animals)
                {
                    a.Position = Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f));
                    a.SetStat("energy", rng.Range(-5f, 100f));
                    a.SetStat("stamina", rng.Range(0f, 60f));
                    a.Age = rng.Range(0, 400);
                    a.Killed = rng.Chance(0.05);
                }
                w.Space.Rebuild();
                var s = new SenseContext(w);
                foreach (var a in animals)
                {
                    var senses = a.Species.Senses;
                    for (int i = 0; i < senses.Count; i++)
                    {
                        int t = senses[i].Read(a, s);
                        Assert.That(t, Is.InRange(0, senses[i].Tokens.Count - 1), $"{senses[i].Label}");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(senses[i].Write(t, TextStyle.V1)));
                    }
                }
            }
            foreach (var a in animals) { a.Killed = false; a.IsGone = true; }
            food.Clear();
            var lone = Place.Animal(prey, Place.At(40, 40));
            w.Space.Rebuild();
            w.Service<CoverLayer>().Clear();
            foreach (var label in new[] { "Food", "Predator", "Cover", "Animal" })
                Assert.AreEqual("none", Token(w, lone, label), $"{label}: nothing there");
        }

        [Test, Description("T-SENSE-02 (SENSE-02): two animals in different places with the same tokens → equal observations, one key")]
        public void ObservationsAreTuples()
        {
            var w = Eco(cover: 0f);
            var prey = w.FindSpecies("prey");
            var a = Place.Animal(prey, Place.At(5, 5));
            var b = Place.Animal(prey, Place.At(40, 40));
            w.Space.Rebuild();
            var oa = Read(w, a);
            var ob = Read(w, b);
            Assert.AreEqual(oa, ob);
            Assert.AreEqual(oa.Key, ob.Key);
            Assert.AreEqual(prey.Senses.Count, oa.Count);
        }

        [Test, Description("T-SENSE-03 (SENSE-03, SENSE-04): the same state read twice, and by animals in another order → the same tokens")]
        public void SensesArePureFunctionsOfTheState()
        {
            var w = Eco(9);
            var prey = w.FindSpecies("prey");
            var rng = new RandomStream(2, "positions");
            var animals = new List<Animal>();
            for (int i = 0; i < 30; i++) animals.Add(Place.Animal(prey, Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f))));
            for (int i = 0; i < 6; i++) Place.Animal(w.FindSpecies("predator"), Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f)));
            w.Space.Rebuild();
            var first = animals.Select(a => Read(w, a).Key).ToList();
            var again = animals.Select(a => Read(w, a).Key).ToList();
            var reversed = Enumerable.Reverse(animals).Select(a => Read(w, a).Key).Reverse().ToList();
            CollectionAssert.AreEqual(first, again);
            CollectionAssert.AreEqual(first, reversed);
        }

        [Test, Description("T-SENSE-04 (SENSE-20): 0.99, 1.0, 1.01, 4.0, 4.01, 10.0, 20.0, 20.01 m → adjacent, adjacent, close, close, medium, medium, far, none; an item within reach → here")]
        public void BandBoundaries()
        {
            var expected = new[] { "adjacent", "adjacent", "close", "close", "medium", "medium", "far", "none" };
            var distances = new[] { 0.99f, 1.0f, 1.01f, 4.0f, 4.01f, 10.0f, 20.0f, 20.01f };
            var w = Eco(cover: 0f);
            var me = Place.Animal(w.FindSpecies("prey"), Place.At(2, 24));
            var threat = Place.Animal(w.FindSpecies("predator"), Place.At(2, 24));
            for (int i = 0; i < distances.Length; i++)
            {
                threat.Position = Place.At(2 + distances[i], 24);
                w.Space.Rebuild();
                Assert.AreEqual(expected[i], Token(w, me, "Predator"), $"{distances[i]} m");
                Assert.AreEqual(expected[i] == "none" ? -1 : System.Array.IndexOf(new[] { "adjacent", "close", "medium", "far" }, expected[i]),
                                Bands.Reference.IndexOf(distances[i], 20f));
            }
            var food = w.Service<FoodGrid>();
            Place.Food(food, Place.At(30.5f, 30.5f));
            me.Position = Place.At(30.1f, 30.5f);
            Assert.AreEqual("here", Token(w, me, "Food"), "within reach");
            me.Position = Place.At(29.5f, 30.5f);
            Assert.AreEqual("adjacent", Token(w, me, "Food"));
        }

        [Test, Description("T-SENSE-05 (SENSE-21): band edges [4, 1, 10] and [1, 4, 25] with vision 20 → validation errors")]
        public void BandEdgesAreValidated()
        {
            foreach (var edges in new[] { new[] { 4f, 1f, 10f }, new[] { 1f, 4f, 25f } })
            {
                var w = New(name: string.Join(",", edges)).Flat(20, 20)
                    .Species("prey", s => s.Action<RestAction>().Sense<NearestCoverSense>(c => c.SetBands(edges, 20f)))
                    .BuildUninitialized();
                var r = new ValidationReport();
                Assert.IsFalse(w.Prepare(r));
                Assert.IsTrue(r.Has("V-30"), r.ToString());
            }
        }

        [Test, Description("T-SENSE-06 (06 §4): a ready partner at 15 m with partner range 20 and 4 → \"ready to mate\" written, then not written")]
        public void ReadinessWithinPartnerRange()
        {
            foreach (var (range, written) in new[] { (20f, true), (4f, false) })
            {
                var w = New(name: "range " + range).Ecology(48f, 0f, 0f, prey: s => s.Find<NearestAnimalSense>("Animal").Configure(AnimalSet.Kin, true, range))
                    .Build();
                var prey = w.FindSpecies("prey");
                var me = Place.Animal(prey, Place.At(10, 10));
                var partner = Place.Animal(prey, Place.At(25, 10));
                partner.Age = 200;
                partner.SetStat("energy", 80);
                w.Space.Rebuild();
                var sense = prey.FindSense("Animal");
                int t = sense.Read(me, new SenseContext(w));
                string text = sense.Write(t, TextStyle.V1);
                Assert.AreEqual(written, text.Contains("ready to mate"), $"range {range}: {text}");
                StringAssert.StartsWith("Animal: 10-20 meters away", text);
            }
        }

        [Test, Description("T-SENSE-07 (SENSE-05): observation-space sizes of the reference species → 29 160 and 4 050; above the threshold → V-50")]
        public void ObservationSpace()
        {
            var w = Eco();
            Assert.AreEqual(29160, w.FindSpecies("prey").ObservationSpace);
            Assert.AreEqual(4050, w.FindSpecies("predator").ObservationSpace);
            Assert.IsFalse(w.LastReport.Has("V-50"));
            var big = New(name: "big").Flat(20, 20).Species("big", s =>
            {
                s.Action<RestAction>().Module<Energy>();
                for (int i = 0; i < 11; i++) s.Sense<LevelSense>(l => { l.Configure("energy", 30, 70); l.SetLabel("E" + i); }, "e" + i);
            }).BuildUninitialized();
            var r = new ValidationReport();
            big.Prepare(r);
            Assert.IsTrue(r.Has("V-50"), "3^11 = 177 147 situations: warning");
        }

        [Test, Description("T-SENSE-08 (SENSE-10…13): situation texts (V1 and V2) of the 48 reference situations → equal to the pinned snapshot; distances in meters, never cells")]
        public void SituationTextsSnapshot()
        {
            var w = Eco();
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var set = ObservationSet.FromJsonLines(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "EvoSim/Data/Observations/prey_observations_v2.jsonl")));
            var preyObs = set.For(prey);
            Assert.AreEqual(48, preyObs.Count, "every prototype situation maps to the Unity prey");
            var predatorSet = new ObservationSet();
            foreach (var e in new[] { "low", "medium", "high" })
                foreach (var p in new[] { "none", "adjacent", "close", "far" })
                    foreach (var c in new[] { "none", "close" })
                        foreach (var k in new[] { "none", "close, ready" })
                            predatorSet.Add(new Dictionary<string, string> { { "energy", e }, { "prey", p }, { "carcass", c }, { "other predator", k }, { "stamina", "medium" } });
            var predatorObs = predatorSet.For(predator);
            Assert.AreEqual(48, predatorObs.Count);

            var sb = new StringBuilder();
            foreach (var (species, list) in new[] { (prey, preyObs), (predator, predatorObs) })
                foreach (var o in list)
                {
                    string v1 = species.Describe(o, TextStyle.V1), v2 = species.Describe(o, TextStyle.V2);
                    Assert.IsFalse(v1.Contains("cell") || v2.Contains("cell"), v1);
                    sb.Append(species.Id).Append('\t').Append(o.Key).Append("\tV1\t").Append(v1).Append('\n');
                    sb.Append(species.Id).Append('\t').Append(o.Key).Append("\tV2\t").Append(v2).Append('\n');
                }
            StringAssert.Contains("prey\t", sb.ToString());
            Golden.Check("situations.tsv", sb.ToString());
        }

        [Test, Description("06 §2 reference texts: the prey and predator examples of the table, word for word")]
        public void ReferenceTexts()
        {
            var w = Eco();
            var prey = w.FindSpecies("prey");
            var set = new ObservationSet();
            set.Add(new Dictionary<string, string> { { "energy", "low" }, { "stamina", "high" }, { "food", "close" }, { "predator", "medium" }, { "cover", "here" }, { "animal", "far, ready" }, { "age", "adult" } });
            var o = set.ToObservation(set.Rows[0], prey);
            Assert.AreEqual("Energy: low. Stamina: high. Food: 1-4 meters away. Predator: 4-10 meters away. Cover: here. Animal: 10-20 meters away, ready to mate. Age: adult.",
                            prey.Describe(o, TextStyle.V1));
            Assert.AreEqual("I am hungry and weak. I am rested. The nearest food is 1-4 meters away. The nearest predator is 4-10 meters away. I am in cover. The nearest other animal is 10-20 meters away. It is ready to mate. I am an adult.",
                            prey.Describe(o, TextStyle.V2));
            var predator = w.FindSpecies("predator");
            var ps = new ObservationSet();
            ps.Add(new Dictionary<string, string> { { "energy", "medium" }, { "stamina", "low" }, { "prey", "close" }, { "carcass", "none" }, { "other predator", "none" }, { "age", "adult" } });
            var po = ps.ToObservation(ps.Rows[0], predator);
            Assert.AreEqual("Energy: medium. Stamina: low. Prey: 1-4 meters away. Carcass: none within 20 meters. Other predator: none within 20 meters. Age: adult.",
                            predator.Describe(po, TextStyle.V1));
            Assert.AreEqual("I have some energy. I am out of breath. The nearest prey is 1-4 meters away. No carcass within 20 meters. No other predator within 20 meters. I am an adult.",
                            predator.Describe(po, TextStyle.V2));
        }

        [Test, Description("T-SENSE-09 (SENSE-30, SENSE-31): a batched raycast sense gives the same tokens as one animal at a time, for 500 animals")]
        public void BatchedEqualsSequential()
        {
            var w = New().Flat(100, 100).Species("prey", s => s.Action<RestAction>().Sense<ObstacleSense>()).Build();
            var sp = w.AllSpecies[0];
            var rng = new RandomStream(11, "obstacles");
            var walls = new List<GameObject>();
            for (int i = 0; i < 60; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(rng.Range(0f, 100f), 0.5f, rng.Range(0f, 100f));
                cube.transform.localScale = new Vector3(rng.Range(1f, 6f), 2f, rng.Range(1f, 6f));
                walls.Add(cube);
            }
            Physics.SyncTransforms();
            try
            {
                var animals = new List<Animal>();
                for (int i = 0; i < 500; i++) animals.Add(Place.Animal(sp, Place.At(rng.Range(0f, 100f), rng.Range(0f, 100f)), heading: rng.Range(0f, 360f)));
                var batched = new SenseContext(w);
                batched.CastRays(animals);
                var tokens = animals.Select(a => sp.Senses[0].Read(a, batched)).ToList();
                Assert.Greater(tokens.Count(t => t == 1), 10, "some rays hit");
                Assert.Greater(tokens.Count(t => t == 0), 10, "some don't");
                var one = new SenseContext(w);
                one.Rays.Immediate = true;
                for (int i = 0; i < animals.Count; i++)
                {
                    one.CastRays(new[] { animals[i] });
                    Assert.AreEqual(tokens[i], sp.Senses[0].Read(animals[i], one), $"animal {i}");
                }
            }
            finally { foreach (var c in walls) Object.DestroyImmediate(c); }
        }

        [Test, Description("SENSE-06, T-ENV-05 and T-ENV-06 sense parts: hidden and killed animals are never sensed; without cover the cover sense says none")]
        public void HiddenThingsAreNeverSensed()
        {
            var w = Eco();
            var cover = w.Service<CoverLayer>();
            cover.Clear();
            Place.Cover(cover, Place.At(10.5f, 10.5f));
            var hidden = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10.5f));
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(11.5f, 10.5f));
            var kin = Place.Animal(w.FindSpecies("prey"), Place.At(13.5f, 10.5f));
            kin.Killed = true;
            w.Space.Rebuild();
            Assert.AreEqual("none", Token(w, hunter, "Prey"), "the hidden prey and the killed one are not sensed");
            Assert.AreEqual("here", Token(w, hidden, "Cover"));
            Assert.AreEqual("adjacent", Token(w, hidden, "Predator"), "the hidden prey still sees its threat");

            var bare = Eco(cover: 0f);
            Assert.IsNull(bare.Service<Cover>());
            var a = Place.Animal(bare.FindSpecies("prey"), Place.At(10, 10));
            Assert.AreEqual("none", Token(bare, a, "Cover"), "no cover module: none (ENV-12)");
        }
    }
}
