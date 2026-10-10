using EvoSim.Samples;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>The recipes of 22 built as samples (Assets/EvoSim/Samples): each does what its recipe says.</summary>
    public class SampleTests : WorldFixture
    {
        [Test, Description("22 §1: thirst grows every tick in the death phase and kills at its deadly level; the cause is \"thirst\"")]
        public void ThirstKills()
        {
            var w = New().Flat(20, 20).Food(0f, 0f).Phase<DeathPhase>()
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest").Module<Thirst>(t => t.Configure(10f, 30f)).Module<DiesOfThirst>())
                .Build();
            var sp = w.FindSpecies("prey");
            var a = Place.Animal(sp, Place.At(5, 5));
            a.Choose("rest");
            var thirst = sp.Module<Thirst>().Value;
            w.Advance(2);
            Assert.AreEqual(20f, a[thirst], 1e-4f);
            w.Advance(1);
            Assert.IsTrue(a.IsGone, "dies at 30");
            Assert.AreEqual(1, sp.Counters.Deaths["thirst"]);
            StringAssert.Contains("dies of thirst at 30", sp.Prompt.Text);
        }

        [Test, Description("22 §2: the water sense says here at the shore, a band when water is in sight, none beyond")]
        public void WaterSense()
        {
            var w = New().Ground<PondGround>(20, 20).Food(0f, 0f)
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest").Sense<NearestWaterSense>()).Build();
            var sp = w.FindSpecies("prey");
            var sense = sp.Module<NearestWaterSense>();
            string Token(float x, float z) => sense.Tokens[sense.Read(Place.Animal(sp, Place.At(x, z)), new SenseContext(w))];
            Assert.AreEqual("here", Token(10f, 13.2f));
            Assert.AreEqual("close", Token(10f, 15f));
            Assert.AreEqual("medium", Token(10f, 19.5f));
            Assert.AreEqual("Water: 1-4 meters away.", sense.Write(System.Linq.Enumerable.ToList(sense.Tokens).IndexOf("close"), TextStyle.V1));
        }

        [Test, Description("22 §3: drink walks to the shore and resets thirst on arrival")]
        public void DrinkResetsThirst()
        {
            var w = New().Ground<PondGround>(20, 20).Food(0f, 0f).Phase<ActPhase>()
                .Species("prey", s => s.PreyBody().Action<DrinkAction>("drink").Module<Thirst>()).Build();
            var sp = w.FindSpecies("prey");
            var a = Place.Animal(sp, Place.At(10f, 15f));
            var thirst = sp.Module<Thirst>().Value;
            a[thirst] = 50f;
            a.Choose("drink");
            w.Advance(3);
            Assert.AreEqual(0f, a[thirst], "drank");
            Assert.IsFalse(w.Ground.IsWater(a.Position), "on the shore, not in the water");
        }

        [Test, Description("22 §8 (MUT-21): the intensity ladder moves the intensity word one step, keeps case and punctuation, and fails without one")]
        public void IntensityLadderSteps()
        {
            var op = new GameObject("ladder").AddComponent<IntensityLadder>();
            try
            {
                var seen = new System.Collections.Generic.HashSet<string>();
                for (int seed = 0; seed < 20; seed++)
                {
                    var job = op.Start(null, new Allele("prey.rest", 0, AlleleValue.OfText("Rest often when tired."), "founder"), new RandomStream(seed, "mutation"));
                    Assert.IsTrue(job.Succeeded);
                    seen.Add(job.Result.Text);
                }
                CollectionAssert.AreEquivalent(new[] { "Rest always when tired.", "Rest sometimes when tired." }, seen);
                var top = op.Start(null, new Allele("prey.rest", 1, AlleleValue.OfText("Always rest."), "founder"), new RandomStream(1, "mutation"));
                Assert.IsTrue(top.Result.Text == "Always rest." || top.Result.Text == "Often rest.", top.Result.Text);
                Assert.IsFalse(op.Start(null, new Allele("prey.rest", 2, AlleleValue.OfText("Rest when tired."), "founder"), new RandomStream(1, "m")).Succeeded);
            }
            finally { Object.DestroyImmediate(op.gameObject); }
        }

        [Test, Description("22 §9: seasons slow the grass's regrowth in the second half of each year")]
        public void Seasons()
        {
            var w = New().Flat(20, 20).Food(0.1f, 0.01f).Phase<SeasonsPhase>(p => p.Configure(10, 0.2f)).Phase<EnvironmentPhase>()
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest")).Build();
            var grass = w.Service<FoodGrid>();
            w.Advance(3);
            Assert.AreEqual(1f, grass.RegrowFactor, "summer");
            w.Advance(4);
            Assert.AreEqual(0.2f, grass.RegrowFactor, "winter");
            w.Advance(5);
            Assert.AreEqual(1f, grass.RegrowFactor, "the next summer");
        }

        [Test, Description("22 §13: the chased sense says yes when the nearest threat's target at the start of the tick is this animal")]
        public void ChasedSense()
        {
            var w = New().Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.Sense<ChasedSense>()).Phase<ActPhase>().Build();
            var prey = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            var other = Place.Animal(w.FindSpecies("prey"), Place.At(30, 30));
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(15, 10));
            prey.Choose("rest");
            other.Choose("rest");
            hunter.Choose("hunt");
            var sense = w.FindSpecies("prey").Module<ChasedSense>();
            Assert.AreEqual("no", sense.Tokens[sense.Read(prey, new SenseContext(w))], "nobody hunted yet");
            w.Advance(1);
            w.Space.Rebuild();
            Assert.AreEqual("yes", sense.Tokens[sense.Read(prey, new SenseContext(w))]);
            Assert.AreEqual("no", sense.Tokens[sense.Read(other, new SenseContext(w))]);
            Assert.AreEqual("Something is chasing me.", sense.Write(1, TextStyle.V2));
        }

        [Test, Description("22 §14: slope locomotion refuses ground steeper than its limit and charges energy per meter climbed")]
        public void SlopeLocomotion()
        {
            var w = New().Ground<RampGround>(40, 20).Phase<ActPhase>()
                .Species("walker", s => s.On<SearchRule>()
                    .Module<SlopeLocomotion>(l => { l.SetSpeeds(1f, 1f); l.Configure(35f, 0.3f); }, "Locomotion")
                    .Module<Energy>().Module<Stamina>().Module<Metabolism>()
                    .Action<GoToAction>("go", g => g.Target = new Vector3(30f, 0f, 10f)))
                .Build();
            var a = Place.Animal(w.FindSpecies("walker"), Place.At(5, 10));
            a.Choose("go");
            w.Advance(20);
            Assert.Less(a.Position.x, 11.5f, "the 45° ramp is refused");
            Assert.Greater(a.Position.x, 9.5f, "the flat part is walked");
            var loco = w.FindSpecies("walker").Module<SlopeLocomotion>();
            Assert.AreEqual(0.3f, loco.ExtraCost(a, Vector3.zero, new Vector3(1f, 1f, 0f)), 1e-5f);
            Assert.AreEqual(0f, loco.ExtraCost(a, new Vector3(1f, 1f, 0f), Vector3.zero), "downhill is free");
        }
    }
}
