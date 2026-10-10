using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>When senses read (SENSE-04) and what they never see (SPACE-12, RAND-11).</summary>
    public class SenseTimingTests : WorldFixture
    {
        [Test, Description("SENSE-04, TICK-04: every decider senses the state at the start of the tick, whatever moves later in that tick")]
        public void DecidersSenseTheStartOfTheTick()
        {
            var w = New(23, name: "start of tick").Lab1(prey: 16, predators: 4, randomBrain: false)
                .DefaultBrain<ScriptedBrain>().Build();
            w.Advance(4);                                                           // everyone moved; tick 4 decides again
            w.Space.Rebuild();
            var snapshot = new SenseContext(w);
            var expected = new Dictionary<int, Observation>();
            foreach (var s in w.AllSpecies)
                foreach (var a in s.Animals)
                    if (!a.IsBusy) expected[a.Id] = s.Observe(a, snapshot);
            w.Advance(1);
            Assert.AreEqual(4, w.Decisions.Tick);
            Assert.Greater(w.Decisions.Due.Count, 10);
            foreach (var d in w.Decisions.Due)
                Assert.AreEqual(expected[d.Animal.Id], d.Observation, $"{d.Animal}: what it sensed is the start-of-tick state");
            Assert.IsTrue(w.Decisions.Due.Any(d => d.Animal.MetersMoved > 0f), "and animals did move during the tick");
        }

        [Test, Description("SPACE-12, RAND-11: views have no enabled collider and sit on the ignored layer; a sense ray asking for every layer still leaves that layer out")]
        public void SensesNeverSeeTheViews()
        {
            var go = new GameObject("body");
            var container = new GameObject("views").transform;
            var walls = new List<GameObject>();
            try
            {
                var body = go.AddComponent<Body>().Configure(null, Color.gray, 1f);
                var view = new ViewPool(body, container, 1).Take(0);
                Assert.IsFalse(view.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), "no collider a ray could hit");
                Assert.IsTrue(view.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == ViewPool.IgnoreRaycastLayer));

                var w = New(name: "rays").Flat(40, 40).Species("prey", s => s.Action<RestAction>().Sense<AllLayersObstacleSense>()).Build();
                var a = Place.Animal(w.AllSpecies[0], Place.At(10, 10), heading: 90f);
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                walls.Add(wall);
                wall.transform.position = new Vector3(12f, 0.5f, 10f);
                string Token()
                {
                    Physics.SyncTransforms();
                    var c = new SenseContext(w);
                    c.CastRays(new[] { a });
                    return w.AllSpecies[0].Senses[0].Tokens[w.AllSpecies[0].Senses[0].Read(a, c)];
                }
                Assert.AreEqual("blocked", Token(), "a wall on the default layer is seen (positive control)");
                wall.layer = ViewPool.IgnoreRaycastLayer;
                Assert.AreEqual("clear", Token(), "the same wall on the views' layer is not, whatever the sense's mask");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(container.gameObject);
                foreach (var w in walls) Object.DestroyImmediate(w);
            }
        }
    }
}
