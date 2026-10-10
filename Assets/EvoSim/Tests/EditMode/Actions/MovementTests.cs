using System.Collections.Generic;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Locomotion and movement (07 §2, SPACE-01…04).</summary>
    public class MovementTests : WorldFixture
    {
        World Build(float size = 40f, System.Action<WorldBuilder> more = null, float preyStamina = 60f)
        {
            var b = New().Flat(size, size).Food(0f, 0f).Carcasses().Phase<ActPhase>()
                .Species("prey", s => s.PreyBody(stamina: preyStamina).PreyActionSet().Action<GoToAction>("goto"))
                .Species("predator", s => s.PredatorBody().PredatorActionSet().Action<GoToAction>("goto"));
            more?.Invoke(b);
            return b.Build();
        }

        [Test, Description("T-SPACE-01 (SPACE-01): an intent of 0.37 m moves the animal by exactly that vector, nothing snapped")]
        public void PositionsAreContinuous()
        {
            var w = Build();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.123f, 7.77f));
            a.Choose("goto");
            var target = a.Position + new Vector3(0.3f, 0f, 0.2158703f);          // 0.3695 m away
            ((GoToAction)a.Species.FindAction("goto")).Target = target;
            var before = a.Position;
            w.Advance(1);
            Assert.AreEqual(target.x, a.Position.x, 1e-5f);
            Assert.AreEqual(target.z, a.Position.z, 1e-5f);
            Assert.AreEqual((target - before).magnitude, a.MetersMoved, 1e-5f);
        }

        [Test, Description("T-SPACE-03 (SPACE-03, SPACE-04, MOVE-02): 10 000 random intents, running into corners and walls → never outside the rectangle")]
        public void AnimalsStayInside()
        {
            var w = Build(10f, preyStamina: 1000f);
            var sp = w.FindSpecies("prey");
            var go = (GoToAction)sp.FindAction("goto");
            var animals = new List<Animal>();
            for (int i = 0; i < 10; i++) { var a = Place.Animal(sp, Place.At(1 + i * 0.8f, 5)); a.Choose("goto"); animals.Add(a); }
            var rng = new RandomStream(3, "intents");
            for (int t = 0; t < 1000; t++)
            {
                float angle = rng.Range(0f, 2 * Mathf.PI);
                go.Direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                go.Run = rng.NextBool();
                w.Advance(1);
                foreach (var a in animals)
                {
                    Assert.IsTrue(w.Ground.IsWalkable(a.Position), $"tick {t}: {a} at {a.Position}");
                    a.SetStat("stamina", 1000f);
                }
            }
        }

        [Test, Description("T-MOVE-01 (MOVE-03, MOVE-04): walking 1 m and running 2 m in a straight line; never past the stop distance; species move at their own speeds")]
        public void SpeedsAndStopDistances()
        {
            var w = Build();
            var prey = Place.Animal(w.FindSpecies("prey"), Place.At(5, 5));
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(5, 15));
            foreach (var a in new[] { prey, hunter })
            {
                a.Choose("goto");
                var g = (GoToAction)a.Species.FindAction("goto");
                g.Target = a.Position + new Vector3(10, 0, 0);
                g.Run = true;
                g.StopAt = 1f;
            }
            w.Advance(1);
            Assert.AreEqual(6f, prey.Position.x, 1e-5f, "prey run 1 m per tick");
            Assert.AreEqual(7f, hunter.Position.x, 1e-5f, "predators run 2 m per tick");
            Assert.AreEqual(5f, prey.Position.z, 1e-6f, "straight line");
            w.Advance(10);
            Assert.AreEqual(14f, prey.Position.x, 1e-4f, "stops 1 m before the target, never past it");
            Assert.AreEqual(14f, hunter.Position.x, 1e-4f);
            ((GoToAction)w.FindSpecies("prey").FindAction("goto")).Run = false;
        }

        [Test, Description("T-MOVE-02 (MOVE-04): fleeing in open ground increases the distance each tick; in a corner the animal slides or sidesteps, never stuck more than 3 ticks")]
        public void FleeingAndCorners()
        {
            var w = Build(40f, preyStamina: 1000f);
            var prey = Place.Animal(w.FindSpecies("prey"), Place.At(20, 20));
            prey.Choose("flee");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(17, 18));
            hunter.Choose("rest");
            float last = w.Distance(prey.Position, hunter.Position);
            for (int t = 0; t < 10; t++)
            {
                w.Advance(1);
                float d = w.Distance(prey.Position, hunter.Position);
                Assert.Greater(d, last, $"tick {t}");
                last = d;
            }

            var w2 = Build(40f, preyStamina: 1000f);
            var cornered = Place.Animal(w2.FindSpecies("prey"), Place.At(0.2f, 0.2f));
            cornered.Choose("flee");
            var h2 = Place.Animal(w2.FindSpecies("predator"), Place.At(3, 3));
            h2.Choose("rest");
            int stuck = 0;
            for (int t = 0; t < 60; t++)
            {
                w2.Advance(1);
                cornered.SetStat("stamina", 1000f);
                stuck = cornered.MetersMoved > Units.Epsilon ? 0 : stuck + 1;
                Assert.LessOrEqual(stuck, 3, $"tick {t}: stuck too long at {cornered.Position}");
                Assert.IsTrue(w2.Ground.IsWalkable(cornered.Position));
            }
        }

        [Test, Description("T-MOVE-03 (MOVE-05): wandering turns on 25 % of ticks (± 4 s.e.), only by ±45° or ±90°; blocked, a new heading")]
        public void WanderingIsAPersistentRandomWalk()
        {
            var w = New().Flat(400000, 400000).Species("prey", s => s.Module<KinematicLocomotion>().Action<RestAction>()).Build();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(200000, 200000), heading: 0f);
            var c = new ActContext(w);
            int turns = 0, n = 100000;
            for (int t = 0; t < n; t++)
            {
                float before = a.Heading;
                c.Begin(a);
                c.Wander();
                float delta = Mathf.DeltaAngle(before, a.Heading);
                if (Mathf.Abs(delta) > 1e-3f)
                {
                    turns++;
                    float m = Mathf.Abs(delta);
                    Assert.IsTrue(Mathf.Abs(m - 45f) < 1e-3f || Mathf.Abs(m - 90f) < 1e-3f, $"turned by {delta}");
                }
                Assert.IsTrue(c.Intent.Wandering && !c.Intent.Run, "wandering walks");
                a.Position += c.Intent.Direction;
            }
            Stat.Binomial(turns, n, 0.25, 4, "turn rate");

            var w2 = New(name: "Walls").Flat(10, 10).Species("prey", s => s.Module<KinematicLocomotion>().Action<RestAction>()).Build();
            var b = Place.Animal(w2.FindSpecies("prey"), Place.At(5, 9.8f), heading: 0f);   // facing the +z wall
            var c2 = new ActContext(w2);
            for (int i = 0; i < 50; i++)
            {
                b.Heading = 0f;
                c2.Begin(b);
                c2.Wander();
                Assert.IsTrue(w2.Ground.Clear(b.Position, b.Position + c2.Intent.Direction * 1f), "blocked: a heading that is free");
            }
        }

        [Test, Description("T-MOVE-04 (MOVE-06, ANIM-20): the meters the locomotion reports are the distance moved and what the metabolism charges, plus its extra cost")]
        public void ReportedMetersAreCharged()
        {
            var w = New().Flat(40, 40).Phase<ActPhase>()
                .Species("prey", s => s.Module<Energy>(e => e.Configure(100, 60)).Module<Stamina>(m => m.Configure(60))
                    .Module<Metabolism>(m => m.Configure(0.7f, 0.5f, 0.2f)).Module<TurningLocomotion>().Action<GoToAction>("goto"))
                .Build();
            var a = Place.Animal(w.AllSpecies[0], Place.At(10, 10));
            a.Choose("goto");
            ((GoToAction)a.Species.FindAction("goto")).Target = Place.At(20, 10);
            var before = a.Position;
            w.Advance(1);
            float moved = Vector3.Distance(new Vector3(before.x, 0, before.z), new Vector3(a.Position.x, 0, a.Position.z));
            Assert.AreEqual(0.5f, moved, 1e-4f);
            Assert.AreEqual(moved, a.MetersMoved, 1e-5f);
            Assert.AreEqual(60f - 0.7f - 0.5f * moved - 0.1f, a.StatOf("energy"), 1e-4f, "base + per meter + the locomotion's extra cost");
            Assert.AreEqual(60f - moved, a.StatOf("stamina"), 1e-4f);
        }

        [Test, Description("T-MOVE-06 (MOVE-01, MOVE-06): a locomotion that turns the direction by 30° and halves it → the tick goes on with where the animal really is")]
        public void TheLocomotionDecides()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<ActPhase>()
                .Species("prey", s => s.Module<Energy>().Module<Diet>(d => d.Set("Grass")).Module<TurningLocomotion>().Action<EatAction>("eat"))
                .Build();
            var a = Place.Animal(w.AllSpecies[0], Place.At(10.5f, 10.5f));
            a.Choose("eat");
            var food = w.Service<FoodGrid>();
            Place.Food(food, Place.At(14.5f, 10.5f));
            w.Advance(1);
            Assert.AreEqual(10.5f + 0.5f * Mathf.Cos(30f * Mathf.Deg2Rad), a.Position.x, 1e-4f, "turned by 30°, half the distance");
            Assert.AreNotEqual(10.5f, a.Position.z, "it went where the locomotion took it, not along the intent");
            Assert.AreEqual(1, food.Count, "no interaction: the item is out of reach from where it really is");
            for (int t = 0; t < 40 && food.Count > 0; t++) w.Advance(1);
            Assert.AreEqual(0, food.Count, "it still reaches the food, from the actual positions, without errors");
        }

        [Test, Description("T-MOVE-07 (MOVE-07): a kinematic subclass that forbids steep slopes, for one species only → it never climbs past the limit, the other does")]
        public void LocomotionCanBeInherited()
        {
            var w = New().Ground<RampGround>(40, 40).Phase<ActPhase>()
                .Species("climber", s => s.Module<KinematicLocomotion>().Action<GoToAction>("goto"))
                .Species("careful", s => s.Module<SlopeLimitLocomotion>(l => l.MaxSlopeDegrees = 20f).Action<GoToAction>("goto"))
                .Build();
            var climber = Place.Animal(w.FindSpecies("climber"), Place.At(5, 5));
            var careful = Place.Animal(w.FindSpecies("careful"), Place.At(5, 15));
            foreach (var a in new[] { climber, careful })
            {
                a.Choose("goto");
                ((GoToAction)a.Species.FindAction("goto")).Target = a.Position + new Vector3(20, 0, 0);
            }
            w.Advance(15);
            Assert.Greater(climber.Position.x, 15f, "the kinematic species climbs the 45° ramp");
            Assert.Greater(climber.Position.y, 1f, "height follows the ground (SPACE-06)");
            Assert.LessOrEqual(careful.Position.x, 10.5f, "the careful species stops at the foot of the ramp");
        }
    }
}
