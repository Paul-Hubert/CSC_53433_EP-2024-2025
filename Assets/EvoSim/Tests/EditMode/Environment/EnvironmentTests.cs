using System.Collections.Generic;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Space, layers, cover and entities (02, 03).</summary>
    public class EnvironmentTests : WorldFixture
    {
        [Test, Description("T-SPACE-02 (SPACE-02, SPACE-06): distances are horizontal Euclidean; a replaced distance function is used by every query")]
        public void DistancesAreHorizontalAndReplaceable()
        {
            var w = New().Flat(20, 20).Build();
            Assert.AreEqual(5f, w.Distance(new Vector3(0, 0, 0), new Vector3(3, 100, 4)), 1e-5f, "height doesn't count");

            var w2 = New(name: "Doubled").Ground<DoubleDistanceGround>(20, 20).Food(0f, 0f).Carcasses()
                .Species("prey", s => s.Action<ProbeAction>())
                .Build();
            Assert.AreEqual(10f, w2.Distance(new Vector3(0, 0, 0), new Vector3(3, 0, 4)), 1e-5f);
            var food = w2.Service<FoodGrid>();
            Place.Food(food, Place.At(5.5f, 5.5f));
            Assert.IsTrue(food.Nearest(Place.At(5.5f, 8.5f), 10f, out var item));
            Assert.AreEqual(6f, item.Distance, 1e-4f, "layers measure with the world's function");
            Assert.IsFalse(food.Nearest(Place.At(5.5f, 8.5f), 5f, out _), "and compare it with the radius");

            var prey = w2.AllSpecies[0];
            var a = Place.Animal(prey, Place.At(2, 2));
            var b = Place.Animal(prey, Place.At(2, 5));
            w2.Space.Rebuild();
            Assert.IsTrue(w2.Space.Nearest(a, a.Position, 10f, new[] { prey }, null, out var hit));
            Assert.AreSame(b, hit.Animal);
            Assert.AreEqual(6f, hit.Distance, 1e-4f, "the spatial index measures with the world's function");
            var cs = w2.Service<CarcassSystem>();
            Place.Carcass(cs, prey, Place.At(2, 3));
            Assert.IsTrue(cs.Nearest(a.Position, 10f, a, null, out _, out float cd));
            Assert.AreEqual(2f, cd, 1e-4f, "entity queries too");
        }

        [Test, Description("T-SPACE-04 (SPACE-07, SPACE-08): two candidates at the same distance → the same one in every run, whatever the insertion order")]
        public void TiesAreBrokenByAFixedKey()
        {
            for (int run = 0; run < 100; run++)
            {
                var w = New(run).Flat(20, 20).Food(0f, 0f).Carcasses().Species("prey", s => s.Action<ProbeAction>()).Build();
                var prey = w.AllSpecies[0];
                bool reverse = run % 2 == 1;
                var viewer = Place.Animal(prey, Place.At(10, 10));
                var first = Place.Animal(prey, Place.At(reverse ? 13 : 7, 10));
                var second = Place.Animal(prey, Place.At(reverse ? 7 : 13, 10));
                w.Space.Rebuild();
                Assert.IsTrue(w.Space.Nearest(viewer, viewer.Position, 5f, new[] { prey }, null, out var hit));
                Assert.AreSame(first, hit.Animal, "the lower id wins a tie");

                var food = w.Service<FoodGrid>();
                Place.Food(food, Place.At(reverse ? 13.5f : 7.5f, 10.5f));
                Place.Food(food, Place.At(reverse ? 7.5f : 13.5f, 10.5f));
                Assert.IsTrue(food.Nearest(Place.At(10.5f, 10.5f), 5f, out var item));
                Assert.AreEqual(food.Grid.CellOf(Place.At(7.5f, 10.5f)), item.Cell, "the lower cell index wins a tie");

                var cs = w.Service<CarcassSystem>();
                var c1 = Place.Carcass(cs, prey, Place.At(reverse ? 13 : 7, 10));
                Place.Carcass(cs, prey, Place.At(reverse ? 7 : 13, 10));
                Assert.IsTrue(cs.Nearest(viewer.Position, 5f, viewer, null, out var found, out _));
                Assert.AreSame(c1, found, "the lower entity id wins a tie");
            }
        }

        [Test, Description("T-ENV-01 (ENV-01): nearest item, within reach and count match a brute-force search")]
        public void FoodQueriesMatchBruteForce()
        {
            var w = New(5).Flat(48, 48).Food(0.05f, 0f).Build();
            var food = w.Service<FoodGrid>();
            var rng = new RandomStream(99, "test");
            int brute = 0;
            for (int c = 0; c < food.Grid.Count; c++) if (food.HasItem(c)) brute++;
            Assert.AreEqual(brute, food.Count);
            Assert.Greater(brute, 50);
            for (int i = 0; i < 500; i++)
            {
                var p = Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f));
                float radius = rng.Range(0.3f, 25f);
                int bestCell = -1;
                float bestD = float.PositiveInfinity;
                for (int c = 0; c < food.Grid.Count; c++)
                {
                    if (!food.HasItem(c)) continue;
                    float d = w.Distance(p, food.Grid.Center(c));
                    if (d <= radius && (d < bestD - 1e-6f || (Mathf.Abs(d - bestD) <= 1e-6f && c < bestCell))) { bestD = d; bestCell = c; }
                }
                bool found = food.Nearest(p, radius, out var item);
                Assert.AreEqual(bestCell >= 0, found, $"query {i}");
                if (found) { Assert.AreEqual(bestCell, item.Cell, $"query {i}"); Assert.AreEqual(bestD, item.Distance, 1e-4f); }
                bool reach = food.WithinReach(p, 0.5f, out var r);
                Assert.AreEqual(bestCell >= 0 && bestD <= 0.5f, reach);
            }
            Assert.IsTrue(food.Nearest(Place.At(24, 24), 100f, out var any));
            Assert.IsTrue(food.Consume(any));
            Assert.IsFalse(food.Consume(any), "ENV-02: an item can be eaten once");
            Assert.AreEqual(brute - 1, food.Count);
        }

        [Test, Description("T-ENV-03 (ENV-03, RAND-02): regrowth on an empty layer follows its probability; draws on another stream change nothing")]
        public void RegrowthFollowsItsProbability()
        {
            const float p = 0.00002f;
            const int ticks = 2000;
            int Run(bool extraDraws, out int cells)
            {
                var w = New(3).Flat(48, 48).Food(0f, p).Phase<EnvironmentPhase>().Phase<RandomEventPhase>(r => r.DrawsPerTick = extraDraws ? 7 : 0).Build();
                var food = w.Service<FoodGrid>();
                cells = food.EligibleCount;
                w.Advance(ticks);
                return food.Count;
            }
            int count = Run(false, out int eligible);
            Assert.AreEqual(48 * 48, eligible);
            double pFilled = 1 - System.Math.Pow(1 - p, ticks);
            Stat.Binomial(count, eligible, pFilled, 4, "regrown cells");
            Assert.AreEqual(count, Run(true, out _), "extra draws on another stream leave the regrowth identical");
        }

        [Test, Description("T-ENV-04 (ENV-04): hungry cover: no item ever in cover, at the start or after 5 000 ticks")]
        public void NoFoodInCover()
        {
            var w = New(8).Flat(48, 48).Cover(0.3f).Food(0.5f, 0.01f).Phase<EnvironmentPhase>().Build();
            var food = w.Service<FoodGrid>();
            var cover = w.Service<CoverLayer>();
            Assert.Greater(cover.CoverCells, 100);
            void Check(string when)
            {
                for (int c = 0; c < food.Grid.Count; c++)
                    if (food.HasItem(c)) Assert.IsFalse(cover.InCover(food.Grid.Center(c)), $"{when}: item in cover at cell {c}");
            }
            Check("start");
            w.Advance(5000);
            Check("after 5000 ticks");
            Assert.Greater(food.Count, 1000, "the rest of the ground filled up");
        }

        [Test, Description("T-ENV-08 (ENV-10): in-cover test and nearest cover within a radius match a brute-force search")]
        public void CoverQueriesMatchBruteForce()
        {
            var w = New(4).Flat(48, 48).Cover(0.2f).Build();
            var cover = w.Service<CoverLayer>();
            var g = cover.Grid;
            Assert.AreEqual(Mathf.RoundToInt(0.2f * 48 * 48), cover.CoverCells, "20 % of walkable ground");
            var rng = new RandomStream(1, "cover-test");
            for (int i = 0; i < 300; i++)
            {
                var p = Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f));
                float radius = rng.Range(0.5f, 15f);
                bool inCover = cover.InCover(p);
                int bestCell = -1;
                float bestD = float.PositiveInfinity;
                for (int c = 0; c < g.Count; c++)
                {
                    if (!cover.InCover(g.Center(c))) continue;
                    float d = w.Distance(p, g.Center(c));
                    if (d <= radius && (d < bestD - 1e-6f || (Mathf.Abs(d - bestD) <= 1e-6f && c < bestCell))) { bestD = d; bestCell = c; }
                }
                bool found = cover.NearestCover(p, radius, out var point, out float dist);
                if (inCover) { Assert.IsTrue(found); Assert.AreEqual(0f, dist); continue; }
                Assert.AreEqual(bestCell >= 0, found, $"query {i}");
                if (found) Assert.AreEqual(bestD, dist, 1e-4f, $"query {i}");
            }
        }

        [Test, Description("T-ENV-07, entity part (ENV-20, ENV-22): carcass portions, never to the killer, one per eater, removed when eaten up or after its lifetime")]
        public void CarcassPortionsAndLifetime()
        {
            var w = New().Flat(20, 20).Carcasses().Phase<EnvironmentPhase>()
                .Species("prey", s => s.Action<ProbeAction>().On<Edible>(e => e.Configure(EatMethod.Strike, 60, 2, 30, 100)))
                .Species("predator", s => s.Action<ProbeAction>())
                .Build();
            var prey = w.AllSpecies[0];
            var predator = w.AllSpecies[1];
            var cs = w.Service<CarcassSystem>();
            var victim = Place.Animal(prey, Place.At(5, 5));
            var killer = Place.Animal(predator, Place.At(5, 6));
            var other1 = Place.Animal(predator, Place.At(6, 5));
            var other2 = Place.Animal(predator, Place.At(4, 5));
            var other3 = Place.Animal(predator, Place.At(5, 4));

            var c = cs.Create(victim, killer.Id);
            Assert.AreEqual(2, c.PortionsLeft);
            Assert.AreEqual(30f, c.EnergyPerPortion);
            Assert.AreEqual(victim.Position, c.Position);
            Assert.IsFalse(cs.EatPortion(c, killer), "never to the killer");
            Assert.IsTrue(cs.EatPortion(c, other1));
            Assert.IsFalse(cs.EatPortion(c, other1), "at most one per eater");
            Assert.IsTrue(cs.EatPortion(c, other2));
            Assert.IsFalse(cs.EatPortion(c, other3), "at most 2 portions");
            Assert.IsTrue(c.UsedUp);
            w.Advance(1);
            Assert.AreEqual(0, cs.EntityCount, "removed in the environment phase when eaten up");

            var rotting = cs.Create(victim, killer.Id);
            w.Advance(99);
            Assert.AreEqual(1, cs.EntityCount, "still there after 99 environment phases");
            w.Advance(1);
            Assert.AreEqual(0, cs.EntityCount, "rotted after 100 ticks");
            Assert.AreEqual(1, rotting.Id, "entity ids are never reused");
        }

        [Test, Description("T-RAND-03 (RAND-04): a fixed world seed and a varied run seed → the same map and initial food")]
        public void WorldSeedFixesTheMap()
        {
            (bool[] cover, bool[] food) Map(int seed)
            {
                var w = New(seed).Flat(48, 48).Cover(0.2f).Food(0.1f, 0.0015f)
                    .Configure(x => { var so = x; so.Seed = seed; })
                    .Build();
                return Snapshot(w);
            }
            (bool[] cover, bool[] food) MapWithWorldSeed(int seed)
            {
                var b = New(seed).Flat(48, 48).Cover(0.2f).Food(0.1f, 0.0015f);
                b.Configure(x => x.SetWorldSeed(777));
                return Snapshot(b.Build());
            }
            (bool[], bool[]) Snapshot(World w)
            {
                var cover = w.Service<CoverLayer>();
                var food = w.Service<FoodGrid>();
                var c = new bool[cover.Grid.Count];
                var f = new bool[food.Grid.Count];
                for (int i = 0; i < c.Length; i++) c[i] = cover.InCover(cover.Grid.Center(i));
                for (int i = 0; i < f.Length; i++) f[i] = food.HasItem(i);
                return (c, f);
            }
            var a = MapWithWorldSeed(1);
            var b2 = MapWithWorldSeed(2);
            CollectionAssert.AreEqual(a.cover, b2.cover, "same cover");
            CollectionAssert.AreEqual(a.food, b2.food, "same initial food");
            var c1 = Map(1);
            var c2 = Map(2);
            CollectionAssert.AreNotEqual(c1.cover, c2.cover, "without a world seed, the map follows the run seed");
        }
    }
}
