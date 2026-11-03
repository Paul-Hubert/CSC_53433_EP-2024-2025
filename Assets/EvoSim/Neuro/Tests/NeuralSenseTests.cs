using System;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// The neural species' senses (plan M16 §5.4): the ray fan and the world ray caster (T-NEURO-05), interoception and the
    /// hunger clock (T-NEURO-06).
    /// </summary>
    public class NeuralSenseTests : WorldFixture
    {
        /// <summary>
        /// A 30 m world with an empty food layer, cover without cells and carcasses, a neural-style prey (fan, interoception,
        /// rest and eat) and a predator, no founders: place animals, items and cover by hand.
        /// </summary>
        World FanWorld(string name, Func<DecisionQuery, float[]> policy = null, int prey = 0, int predators = 0, float size = 30f,
                       float food = 0f, float cover = 0f)
        {
            return New(5, name: name).Flat(size, size).Food(food, 0f).Cover(cover).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => { b.TakesAttachments = true; b.ReadsText = false; b.Policy = policy; })
                .Species("prey", s => s.PreyBody()
                    .Sense<RayFanSense>(f => f.Configure(12, 360f, RayFanSense.PreyKinds))
                    .Sense<InteroceptionSense>().Module<HungerClock>()
                    .Action<RestAction>("rest", withGene: false).Action<EatAction>("eat", withGene: false)
                    .Population(prey))
                .Species("predator", s => s.PredatorBody("prey")
                    .Sense<RayFanSense>(f => f.Configure(12, 360f, RayFanSense.PredatorKinds))
                    .Sense<InteroceptionSense>().Module<HungerClock>()
                    .Action<RestAction>("rest", withGene: false)
                    .Population(predators))
                .Build();
        }

        static RayFanSense Fan(Species s) => s.Module<RayFanSense>();

        static float[] Inputs(Animal a)
        {
            var fan = Fan(a.Species);
            return ((NeuralInput)fan.Attachment(a, new SenseContext(a.Species.World))).Values;
        }

        static float Channel(float[] values, RayFanSense fan, int ray, RayHitKind kind) =>
            values[ray * fan.Kinds.Count + fan.ChannelOf(kind)];

        static RayHit Ray0(Animal a)
        {
            var hits = new RayHit[Fan(a.Species).Rays];
            Fan(a.Species).Cast(a, hits);
            return hits[0];
        }

        // ---- T-NEURO-05 ----

        [Test, Description("T-NEURO-05 (plan M16 §5.4): a 360° fan of 12 rays lies at 0°, 30° … 330° from the heading, none doubled at ±180°; partial fans of 1, 2, 5 and 7 rays are symmetric")]
        public void FanAngles()
        {
            var full = WorldRayCaster.FanAngles(12, 360f);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12).Select(i => i * 30f).ToArray(), full);
            Assert.AreEqual(12, full.Select(x => Mathf.Repeat(x, 360f)).Distinct().Count(), "no direction twice");
            CollectionAssert.AreEqual(new[] { 0f }, WorldRayCaster.FanAngles(1, 90f));
            foreach (int n in new[] { 2, 5, 7 })
            {
                var a = WorldRayCaster.FanAngles(n, 90f);
                Assert.AreEqual(-45f, a[0], 1e-4f);
                Assert.AreEqual(45f, a[n - 1], 1e-4f);
                for (int i = 0; i < n; i++) Assert.AreEqual(-a[n - 1 - i], a[i], 1e-4f, $"{n} rays: symmetric");
            }
            Assert.AreEqual(0f, WorldRayCaster.FanAngles(5, 90f)[2], 1e-4f, "an odd fan has a ray straight ahead");
        }

        [Test, Description("T-NEURO-05 (SENSE-40): food 3 m ahead → ray 0 Food proximity 0.85, every other channel of ray 0 at 0; the attachment is rays × kinds wide")]
        public void FoodAhead()
        {
            var w = FanWorld("food ahead");
            var prey = w.FindSpecies("prey");
            var a = Place.Animal(prey, Place.At(15.5f, 15.5f));
            Place.Food(w.Service<FoodGrid>(), Place.At(15.5f, 18.5f));
            var fan = Fan(prey);
            Assert.AreEqual(20f, fan.RangeOf(a), 1e-4f, "range: the vision, 20 m without the trait");
            var v = Inputs(a);
            Assert.AreEqual(12 * 7, v.Length);
            Assert.AreEqual(0.85f, Channel(v, fan, 0, RayHitKind.Food), 1e-4f);
            foreach (var k in fan.Kinds.Where(k => k != RayHitKind.Food)) Assert.AreEqual(0f, Channel(v, fan, 0, k), k.ToString());
            Assert.AreEqual(1 + fan.ChannelOf(RayHitKind.Food), fan.Read(a, new SenseContext(w)), "the token: the nearest thing is food");
        }

        [Test, Description("T-NEURO-05 (plan D3): a kin 2 m ahead hides food 3 m behind it (first object); killed, it lets the ray pass")]
        public void FirstObjectStopsTheRay()
        {
            var w = FanWorld("first object");
            var prey = w.FindSpecies("prey");
            var a = Place.Animal(prey, Place.At(15.5f, 15.5f));
            var kin = Place.Animal(prey, Place.At(15.5f, 17.5f));
            Place.Food(w.Service<FoodGrid>(), Place.At(15.5f, 18.5f));
            var fan = Fan(prey);
            var v = Inputs(a);
            Assert.AreEqual(0.9f, Channel(v, fan, 0, RayHitKind.Kin), 1e-4f);
            Assert.AreEqual(0f, Channel(v, fan, 0, RayHitKind.Food), "the food behind the kin is not seen");

            kin.Killed = true;
            var hit = Ray0(a);
            Assert.AreEqual(RayHitKind.Food, hit.Kind, "a killed animal lets the ray pass");
            Assert.AreEqual(3f, hit.Distance, 1e-4f);
        }

        [Test, Description("T-NEURO-05 (SENSE-06, ENV-11): a prey hidden in cover → the predator's ray stops at the cover cell; in the open, the ray finds the prey")]
        public void CoverStopsTheRay()
        {
            var w = FanWorld("cover");
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var hunter = Place.Animal(predator, Place.At(15.5f, 10.5f));
            Place.Animal(prey, Place.At(15.5f, 15.5f));
            var hit = Ray0(hunter);
            Assert.AreEqual(RayHitKind.Prey, hit.Kind);
            Assert.AreEqual(5f, hit.Distance, 1e-4f);

            var cover = w.Service<CoverLayer>();
            for (int z = 14; z <= 16; z++) Place.Cover(cover, Place.At(15.5f, z + 0.5f));
            hit = Ray0(hunter);
            Assert.AreEqual(RayHitKind.Cover, hit.Kind, "the prey is hidden: the ray stops at the cover");
            Assert.AreEqual(3.5f, hit.Distance, 1e-4f, "where the ray enters the cover cell");
        }

        [Test, Description("T-NEURO-05: the world's edge stops a ray as Obstacle")]
        public void EdgeIsObstacle()
        {
            var w = FanWorld("edge");
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(1.5f, 15.5f), heading: 270f);
            var hit = Ray0(a);
            Assert.AreEqual(RayHitKind.Obstacle, hit.Kind);
            Assert.AreEqual(1.5f, hit.Distance, 1e-4f);
        }

        [Test, Description("T-NEURO-05 (SPACE-08): at equal distance the lower kind wins: a predator standing on food 3 m ahead reads Threat")]
        public void EqualDistanceKindOrder()
        {
            var w = FanWorld("tie");
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(15.5f, 15.5f));
            Place.Food(w.Service<FoodGrid>(), Place.At(15.5f, 18.5f));
            Place.Animal(w.FindSpecies("predator"), Place.At(15.5f, 18.5f));
            var hit = Ray0(a);
            Assert.AreEqual(RayHitKind.Threat, hit.Kind);
            Assert.AreEqual(3f, hit.Distance, 1e-4f);
        }

        [Test, Description("T-NEURO-05 (plan D8): in a 4-species world where both predators eat both preys, both predators read as Threat to both preys, and the other prey as OtherAnimal")]
        public void FourSpeciesFoodWeb()
        {
            var w = New(5, name: "2x2").Flat(30, 30).Food(0f, 0f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => { b.TakesAttachments = true; b.ReadsText = false; })
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest", withGene: false).Population(0))
                .Species("prey-nn", s => s.PreyBody().Action<RestAction>("rest", withGene: false).Population(0))
                .Species("predator", s => { s.PredatorBody(); s.Get<Diet>().Set("prey", "prey-nn", "carcass:prey", "carcass:prey-nn"); s.Action<RestAction>("rest", withGene: false).Population(0); })
                .Species("predator-nn", s => { s.PredatorBody(); s.Get<Diet>().Set("prey", "prey-nn", "carcass:prey", "carcass:prey-nn"); s.Action<RestAction>("rest", withGene: false).Population(0); })
                .Build();
            var caster = new WorldRayCaster(w);
            var angles = new[] { 0f, 90f, 180f };
            var hits = new RayHit[3];
            foreach (var (id, x, z) in new[] { ("prey", 15.5f, 15.5f), ("prey-nn", 25.5f, 5.5f) })
            {
                // Each viewer has its own cluster: a predator ahead, the other predator to the right, the other prey behind.
                var viewer = Place.Animal(w.FindSpecies(id), Place.At(x, z));
                Place.Animal(w.FindSpecies("predator"), Place.At(x, z + 3f));
                Place.Animal(w.FindSpecies("predator-nn"), Place.At(x + 3f, z));
                Place.Animal(w.FindSpecies(id == "prey" ? "prey-nn" : "prey"), Place.At(x, z - 3f));
                caster.Cast(viewer, 20f, angles, hits);
                Assert.AreEqual(RayHitKind.Threat, hits[0].Kind, id + " sees predator");
                Assert.AreEqual(RayHitKind.Threat, hits[1].Kind, id + " sees predator-nn");
                Assert.AreEqual(RayHitKind.OtherAnimal, hits[2].Kind, id + " sees the other prey");
            }
            var hunter = Place.Animal(w.FindSpecies("predator-nn"), Place.At(5.5f, 25.5f));
            Place.Animal(w.FindSpecies("prey-nn"), Place.At(5.5f, 28.5f));
            caster.Cast(hunter, 20f, angles, hits);
            Assert.AreEqual(RayHitKind.Prey, hits[0].Kind, "predator-nn sees prey-nn as prey");
        }

        // ---- T-NEURO-06 ----

        [Test, Description("T-NEURO-06 (SENSE-31, T-SENSE-09, SENSE-03): over 500 animals, ReadAll gives Read's tokens, and a fan's attachment is the same when read twice")]
        public void ReadAllEqualsRead()
        {
            var w = FanWorld("500 animals", prey: 400, predators: 100, size: 60f, food: 0.1f, cover: 0.2f);
            w.Advance(3);
            var ctx = new SenseContext(w);
            int checkedAnimals = 0;
            foreach (var s in w.AllSpecies)
            {
                var group = s.Animals.Where(a => a.IsAlive).ToList();
                foreach (var sense in s.Senses)
                {
                    var tokens = new int[group.Count];
                    sense.ReadAll(group, ctx, tokens);
                    for (int i = 0; i < group.Count; i++) Assert.AreEqual(sense.Read(group[i], ctx), tokens[i], $"{sense.Label} of {group[i]}");
                }
                var fan = Fan(s);
                foreach (var a in group)
                {
                    var one = ((NeuralInput)fan.Attachment(a, ctx)).Values;
                    var two = ((NeuralInput)fan.Attachment(a, ctx)).Values;
                    CollectionAssert.AreEqual(one, two);
                }
                checkedAnimals += group.Count;
            }
            Assert.Greater(checkedAnimals, 450);
        }

        [Test, Description("T-NEURO-06 (plan M16 §5.4): interoception is 7 + actions wide; the in-cover flag is set only where a cover hides the animal from a threat; with no threat, where it stands in cover")]
        public void InteroceptionInCover()
        {
            var w = FanWorld("in cover");
            var prey = w.FindSpecies("prey");
            var body = prey.Module<InteroceptionSense>();
            Assert.AreEqual(7 + prey.Actions.Count, body.NeuralWidth);
            var cover = w.Service<CoverLayer>();
            Place.Cover(cover, Place.At(5.5f, 5.5f));
            var hidden = Place.Animal(prey, Place.At(5.5f, 5.5f));
            var open = Place.Animal(prey, Place.At(20.5f, 20.5f));
            var v = new float[body.NeuralWidth];
            body.Fill(hidden, v);
            Assert.AreEqual(1f, v[5], "hidden from the predator");
            body.Fill(open, v);
            Assert.AreEqual(0f, v[5], "in the open");

            var predatorBody = w.FindSpecies("predator").Module<InteroceptionSense>();
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(5.5f, 5.5f));
            var pv = new float[predatorBody.NeuralWidth];
            predatorBody.Fill(hunter, pv);
            Assert.AreEqual(1f, pv[5], "no threat: standing in cover");

            cover.SetHidingSpecies(new[] { "nobody" });
            body.Fill(hidden, v);
            Assert.AreEqual(0f, v[5], "in cover, but this cover hides it from no threat");
        }

        [Test, Description("T-NEURO-06 (CORE-05): the previous action is a one-hot of the animal's last action, all zeros before its first decision")]
        public void InteroceptionPreviousAction()
        {
            var w = FanWorld("previous action", policy: q => new[] { 0f, 1f });
            var prey = w.FindSpecies("prey");
            var body = prey.Module<InteroceptionSense>();
            var a = Place.Animal(prey, Place.At(15.5f, 15.5f));
            var v = new float[body.NeuralWidth];
            body.Fill(a, v);
            Assert.AreEqual(0f, v[6] + v[7], "no decision yet");
            w.Advance(1);
            body.Fill(a, v);
            Assert.AreEqual(1, a.Action, "it chose eat");
            Assert.AreEqual(0f, v[6]);
            Assert.AreEqual(1f, v[7]);
        }

        [Test, Description("T-NEURO-06 (plan M16 §5.4): ticks since the last meal grow by 1 per tick, reset on a graze or any other meal, and the input is clamped at hungerHorizon")]
        public void HungerClockCounts()
        {
            int choice = 0;                                                           // 0 rest, 1 eat
            var w = FanWorld("hunger", policy: q => choice == 0 ? new[] { 1f, 0f } : new[] { 0f, 1f });
            var prey = w.FindSpecies("prey");
            var clock = prey.Module<HungerClock>();
            var body = prey.Module<InteroceptionSense>();
            var a = Place.Animal(prey, Place.At(15.5f, 15.5f));
            w.Advance(3);
            Assert.AreEqual(3f, a[clock.SinceMeal]);

            Place.Food(w.Service<FoodGrid>(), Place.At(15.5f, 15.5f));
            choice = 1;
            for (int k = 0; k < 3 && a.Meals == 0; k++) w.Advance(1);               // the brain may answer for the next tick
            Assert.AreEqual(1, a.Meals, "it grazed");
            Assert.AreEqual(0f, a[clock.SinceMeal], "a graze resets the clock");
            choice = 0;
            w.Advance(2);
            Assert.AreEqual(2f, a[clock.SinceMeal]);

            a.Meals++;                                                                // a strike or a scavenge feeds the same way
            w.Advance(1);
            Assert.AreEqual(0f, a[clock.SinceMeal]);

            var v = new float[body.NeuralWidth];
            a[clock.SinceMeal] = 50f;
            body.Fill(a, v);
            Assert.AreEqual(50f / body.HungerHorizon, v[v.Length - 1], 1e-6f);
            a[clock.SinceMeal] = 5000f;
            body.Fill(a, v);
            Assert.AreEqual(1f, v[v.Length - 1], "clamped at the horizon");
        }

        [Test, Description("T-NEURO-06 (plan M16 §5.1): with the default prey kinds and 12 rays the fan gives 84 inputs, interoception 7 + the action count (96 in all with 5 actions)")]
        public void LayoutWidths()
        {
            var w = FanWorld("widths");
            var prey = w.FindSpecies("prey");
            Assert.AreEqual(84, Fan(prey).NeuralWidth);
            var layout = NeuralLayout.For(prey, new[] { 12 });
            Assert.AreEqual(84 + 7 + prey.Actions.Count, layout.Inputs, "with 5 actions: 84 + 12 = 96");
        }
    }
}
