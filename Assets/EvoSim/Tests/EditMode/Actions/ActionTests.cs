using System;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Actions, intents and interactions (07 §1, §3, §4; 04 §2).</summary>
    public class ActionTests : WorldFixture
    {
        World Eco(int seed = 1, ActOrder order = ActOrder.SpeciesInTurn, float killChance = 1f, bool cover = false)
        {
            return New(seed).Ecology(40f, cover ? 0.2f : 0f, 0f, predator: p => p.Get<Diet>().SetKillChance(killChance), regrow: 0f)
                .Phase<ActPhase>(p => p.Order = order).Phase<DeathPhase>().Phase<EnvironmentPhase>()
                .Build();
        }

        static IEnumerable<Type> AllActionTypes() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name.StartsWith("EvoSim"))
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(AnimalAction).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        [Test, Description("T-ACT-01 template (ACT-01, ACT-04, ACT-06): every action class: a name and a description; alone in the world it searches (or stays), flagged once per decision; it never changes another animal")]
        public void ActionConformance([ValueSource(nameof(AllActionTypes))] Type type)
        {
            var b = New().Flat(100, 100).Food(0f, 0f).Carcasses().Service<EggSystem>("Eggs").Phase<ActPhase>()
                .Species("prey", s => s.PreyBody().Action<RestAction>("rest"))
                .Species("test", s =>
                {
                    s.PreyBody();
                    s.Get<Diet>().Set("Grass", "prey", "carcass:prey");
                    var go = new GameObject(type.Name.ToLowerInvariant());
                    go.transform.SetParent(s.GameObject.transform, false);
                    go.AddComponent(type);
                    go.AddComponent<TextGene>();
                });
            var w = b.Build();
            var sp = w.FindSpecies("test");
            var action = sp.Actions.First(a => a.GetType() == type);
            Assert.IsFalse(string.IsNullOrEmpty(action.Name), "a name");
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.Description), $"{type.Name} has a description for the prompt (ACT-01)");
            Assert.AreEqual(1, sp.Actions.Count(a => a.Name == action.Name), "unique in its species");

            var a = Place.Animal(sp, Place.At(10, 10));
            var other = Place.Animal(w.FindSpecies("prey"), Place.At(90, 90));                       // out of sight
            a.Action = action.Index;
            var c = new ActContext(w);
            var otherBefore = (other.Position, other.StatOf("energy"), other.StatOf("stamina"), other.Killed);
            for (int t = 0; t < 4; t++)
            {
                c.Begin(a);
                action.Act(a, c);
                Assert.AreEqual(otherBefore, (other.Position, other.StatOf("energy"), other.StatOf("stamina"), other.Killed), "ACT-06");
                Assert.AreEqual(Place.At(10, 10), a.Position, "an action never moves the animal itself (ACT-06)");
                Assert.IsTrue(c.Intent.IsStay || c.Intent.Wandering || type.Assembly != typeof(AnimalAction).Assembly,
                              $"{type.Name} alone: searches or stays (ACT-04)");
            }
            Assert.LessOrEqual(sp.Counters.Invalid, 1, "searching is counted once per decision");
            Assert.AreEqual(a.Searching ? 1 : 0, sp.Counters.Invalid);
        }

        [Test, Description("T-ACT-02 (ACT-02): a chosen action over 4 ticks with a moving target looks the target up again each tick")]
        public void TargetsAreLookedUpEachTick()
        {
            var w = Eco();
            var prey = w.FindSpecies("prey");
            var leader = Place.Animal(prey, Place.At(10, 10)); leader.Choose("flee");
            var follower = Place.Animal(prey, Place.At(5, 10)); follower.Choose("follow");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(12, 12)); hunter.Choose("rest");
            for (int t = 0; t < 4; t++)
            {
                var c = new ActContext(w);
                c.Begin(follower);
                follower.CurrentAction.Act(follower, c);
                Assert.AreSame(leader, c.TargetAnimal);
                Assert.AreEqual(leader.Position, c.Intent.Target, $"tick {t}: toward where the leader is now");
                w.Advance(1);
                Assert.AreEqual(leader.Id, follower.TargetId);
            }
        }

        [Test, Description("T-ACT-03 (ACT-03): each reference action's intent: target, walk or run, stop distance (07 §4)")]
        public void ReferenceIntents()
        {
            var w = Eco(cover: true);
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var cover = w.Service<CoverLayer>();
            cover.Clear();
            Place.Cover(cover, Place.At(14.5f, 10.5f));
            var food = w.Service<FoodGrid>();
            Place.Food(food, Place.At(10.5f, 13.5f));
            var a = Place.Animal(prey, Place.At(10.5f, 10.5f));
            var kin = Place.Animal(prey, Place.At(10.5f, 4.5f));
            kin.SetStat("energy", 80); kin.Age = 200;
            var hunter = Place.Animal(predator, Place.At(6.5f, 10.5f));
            var c = new ActContext(w);
            Intent Plan(Animal x, string action) { c.Begin(x); x.Species.FindAction(action).Act(x, c); return c.Intent; }

            var eat = Plan(a, "eat");
            Assert.IsFalse(eat.Run); Assert.AreEqual(Place.At(10.5f, 13.5f), eat.Target); Assert.AreEqual(3f, eat.MaxDistance, 1e-4f, "stop 0");
            Assert.AreEqual(InteractionKind.Graze, c.Interaction.Kind);
            var flee = Plan(a, "flee");
            Assert.IsTrue(flee.Run); Assert.Greater(flee.Direction.x, 0.99f, "away from the hunter");
            var hide = Plan(a, "hide");
            Assert.IsFalse(hide.Run); Assert.AreEqual(4f, hide.MaxDistance, 1e-4f, "into cover, stop 0");
            var follow = Plan(a, "follow");
            Assert.IsFalse(follow.Run); Assert.AreEqual(5f, follow.MaxDistance, 1e-4f, "stop at 1 m");
            Assert.IsTrue(Plan(a, "rest").IsStay);
            var mate = Plan(a, "mate");
            Assert.AreEqual(5f, mate.MaxDistance, 1e-4f, "to the ready partner, stop at 1 m");
            var hunt = Plan(hunter, "hunt");
            Assert.IsTrue(hunt.Run); Assert.AreEqual(3f, hunt.MaxDistance, 1e-4f, "run, stop at 1 m");
            Assert.AreEqual(InteractionKind.Strike, c.Interaction.Kind);
            Assert.AreSame(a, c.Interaction.Target);
        }

        [Test, Description("T-ACT-04 (ACT-10, MOVE-04): eat 0.8 m from an item → walks to it and grazes in the same tick")]
        public void EatOnArrival()
        {
            var w = Eco();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 9.7f));
            a.Choose("eat");
            var food = w.Service<FoodGrid>();
            Place.Food(food, Place.At(10.5f, 10.5f));
            w.Advance(1);
            Assert.AreEqual(0, food.Count, "grazed on the tick of arrival");
            Assert.AreEqual(1, a.Meals);
        }

        [Test, Description("T-ACT-05 (ACT-10): strike, scavenge at 1.01 m and 0.99 m, graze at 0.51 m and 0.49 m → no interaction, interaction")]
        public void Reaches()
        {
            void Check(string what, float distance, bool expect, Func<World, Animal> setup)
            {
                var w = Eco();
                var actor = setup(w);
                actor.SetStat("stamina", 0f);                               // can't move: the reach alone decides
                int meals = actor.Meals;
                w.Advance(1);
                Assert.AreEqual(expect, actor.Meals > meals, $"{what} at {distance} m");
            }
            foreach (var (d, ok) in new[] { (1.01f, false), (0.99f, true) })
            {
                Check("strike", d, ok, w =>
                {
                    var h = Place.Animal(w.FindSpecies("predator"), Place.At(10, 10)); h.Choose("hunt");
                    Place.Animal(w.FindSpecies("prey"), Place.At(10 + d, 10)).Choose("rest");
                    return h;
                });
                Check("scavenge", d, ok, w =>
                {
                    var h = Place.Animal(w.FindSpecies("predator"), Place.At(10, 10)); h.Choose("hunt");
                    Place.Carcass(w.Service<CarcassSystem>(), w.FindSpecies("prey"), Place.At(10 + d, 10));
                    return h;
                });
            }
            foreach (var (d, ok) in new[] { (0.51f, false), (0.49f, true) })
                Check("graze", d, ok, w =>
                {
                    var e = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f - d, 10.5f)); e.Choose("eat");
                    Place.Food(w.Service<FoodGrid>(), Place.At(10.5f, 10.5f));
                    return e;
                });
        }

        [Test, Description("T-ACT-06 (ACT-11): 10 000 strikes with kill chance 0.5 → 50 % within 4 s.e.; a kill leaves a carcass, makes the hunter busy, adds the gain")]
        public void StrikeOutcomes()
        {
            var w = Eco(killChance: 0.5f);
            var resolver = new InteractionResolver(w);
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(10, 10));
            var cs = w.Service<CarcassSystem>();
            int kills = 0, n = 10000;
            for (int i = 0; i < n; i++)
            {
                var prey = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10));
                hunter.StruckThisTick = false;
                hunter.BusyTicks = 0;
                hunter.SetStat("energy", 20f);
                int carcasses = cs.EntityCount;
                resolver.Apply(hunter, Interaction.Strike(prey), prey.Position);
                if (prey.Killed)
                {
                    kills++;
                    Assert.AreEqual(carcasses + 1, cs.EntityCount, "a kill leaves a carcass");
                    Assert.AreEqual(50, hunter.BusyTicks, "the hunter digests");
                    Assert.AreEqual(80f, hunter.StatOf("energy"), 1e-4f, "+60");
                    Assert.AreEqual(hunter.Id, prey.KilledBy);
                }
                prey.IsGone = true;
            }
            Stat.Binomial(kills, n, 0.5, 4, "kills");
        }

        [Test, Description("T-ACT-07 (ACT-12): a hunter strikes at most once per tick; a prey that survives one strike can be killed by another hunter the same tick")]
        public void OneStrikePerTick()
        {
            var w = Eco(killChance: 1f);
            var resolver = new InteractionResolver(w);
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(10, 10));
            hunter.SetTrait(w.FindSpecies("predator").Module<Diet>().KillChance, 0f);
            var p1 = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10));
            var p2 = Place.Animal(w.FindSpecies("prey"), Place.At(9.5f, 10));
            Assert.IsTrue(resolver.Apply(hunter, Interaction.Strike(p1), p1.Position));
            Assert.IsFalse(resolver.Apply(hunter, Interaction.Strike(p2), p2.Position), "a second strike in the same tick is refused");
            var second = Place.Animal(w.FindSpecies("predator"), Place.At(11, 10));
            Assert.IsFalse(p1.Killed);
            resolver.Apply(second, Interaction.Strike(p1), p1.Position);
            Assert.IsTrue(p1.Killed, "struck again by another hunter, it dies");
        }

        [Test, Description("T-ACT-08 (ACT-13): mate next to a ready partner → no birth in the act phase")]
        public void MatingIsNotAnInteraction()
        {
            var w = Eco();
            var prey = w.FindSpecies("prey");
            var a = Place.Animal(prey, Place.At(10, 10)); a.Choose("mate"); a.Age = 200; a.SetStat("energy", 90);
            var b = Place.Animal(prey, Place.At(10.5f, 10)); b.Choose("rest"); b.Age = 200; b.SetStat("energy", 90);
            int before = prey.Animals.Count;
            w.Advance(3);
            Assert.AreEqual(before, prey.Animals.Count);
            Assert.AreEqual(0, prey.Counters.Births);
        }

        [Test, Description("T-ACT-09 (ACT-20): static check: no action class reads text gene values or allele texts")]
        public void ActionsDontReadGenes()
        {
            var files = SourceScan.Files("Runtime/Actions").Where(f => !f.EndsWith("AnimalAction.cs"));
            var hits = SourceScan.Find(files, @"\.Gene\b|Genome|Allele|FounderPool|Founders|\.Neutral\b|ContrastPro|ContrastAnti");
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }

        [Test, Description("T-ACT-10 (07 §4): hunt with a carcass and a prey at the same distance → goes for the carcass")]
        public void CarcassWinsATie()
        {
            var w = Eco();
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(10, 10));
            Place.Animal(w.FindSpecies("prey"), Place.At(13, 10));
            Place.Carcass(w.Service<CarcassSystem>(), w.FindSpecies("prey"), Place.At(7, 10));
            var c = new ActContext(w);
            c.Begin(hunter);
            hunter.Species.FindAction("hunt").Act(hunter, c);
            Assert.AreEqual(InteractionKind.Scavenge, c.Interaction.Kind);
        }

        [Test, Description("T-SPEC-04 (SPEC-11): a cannibal hunts kin, never itself; its threats include itself, so flee runs from kin")]
        public void Cannibals()
        {
            var w = New().Flat(40, 40).Food(0f, 0f).Phase<ActPhase>().Phase<DeathPhase>()
                .Species("cannibal", s =>
                {
                    s.PreyBody().Module<Digestion>().Action<HuntAction>("hunt").Action<FleeAction>("flee");
                    s.Get<Diet>().Set("Grass", "cannibal");
                    s.Get<Diet>().SetKillChance(1f);
                })
                .Build();
            var sp = w.AllSpecies[0];
            var a = Place.Animal(sp, Place.At(10, 10));
            var b = Place.Animal(sp, Place.At(14, 10));
            var c = new ActContext(w);
            c.Begin(a);
            sp.FindAction("hunt").Act(a, c);
            Assert.AreSame(b, c.Interaction.Target, "hunts kin, not itself");
            c.Begin(a);
            sp.FindAction("flee").Act(a, c);
            Assert.Less(c.Intent.Direction.x, -0.99f, "flees from its own kind");
            CollectionAssert.Contains(w.ThreatsOf(sp).ToArray(), sp);
        }

        [Test, Description("T-SPEC-05 (SPEC-13): a grazer next to a carcass and a hunter next to food → no interaction")]
        public void InteractionsNeedTheDiet()
        {
            var w = Eco();
            var resolver = new InteractionResolver(w);
            var grazer = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            var carcass = Place.Carcass(w.Service<CarcassSystem>(), w.FindSpecies("prey"), Place.At(10.2f, 10));
            Assert.IsFalse(resolver.Apply(grazer, Interaction.Scavenge(carcass), carcass.Position));
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(20.5f, 20.5f));
            var food = w.Service<FoodGrid>();
            Place.Food(food, hunter.Position);
            food.Nearest(hunter.Position, 1f, out var item);
            Assert.IsFalse(resolver.Apply(hunter, Interaction.Graze(item), item.Position));
            Assert.AreEqual(1, food.Count);
            Assert.AreEqual(2, carcass.PortionsLeft);
        }

        [Test, Description("T-SPEC-08 (SPEC-10, SPEC-15, ENV-23, REPRO-23): eggs without an Edible → V-24; with an Edible and an egg eater → eaten, recorded as lost, energy = edible × scale")]
        public void EggEaters()
        {
            var bad = New(name: "No edible eggs").Ecology(20f).Species("eggeater", s =>
                {
                    s.PreyBody().Action<EatAction>("eat", e => e.SetTarget("egg:prey"));
                    s.Get<Diet>().Set("egg:prey");
                })
                .BuildUninitialized();
            var r = new ValidationReport();
            Assert.IsFalse(bad.Prepare(r));
            Assert.IsTrue(r.Has("V-24"), r.ToString());

            var good = New(name: "Edible eggs").Ecology(20f).Phase<ActPhase>().Phase<EnvironmentPhase>()
                .Species("eggeater", s =>
                {
                    s.PreyBody().Action<EatAction>("eat", e => e.SetTarget("egg:prey"));
                    s.Get<Diet>().Set(new Diet.Entry("egg:prey", 2f));
                });
            good.Root.transform.Find("Environment/Eggs").gameObject.AddComponent<Edible>().Configure(EatMethod.Graze, 15f);
            var w = good.Build();
            Assert.IsFalse(w.LastReport.Has("V-24"), "the egg kind is edible now");
            var eggs = w.Service<EggSystem>();
            w.Events.Lines = new List<string>();
            var prey = w.FindSpecies("prey");
            var egg = eggs.Lay(new Egg { Species = prey, Parents = new[] { 1, 2 }, Position = Place.At(10, 10), HatchTick = 999, Alleles = new Allele[0] });
            var eater = Place.Animal(w.FindSpecies("eggeater"), Place.At(10f, 10f));
            eater.Choose("eat");
            eater.SetStat("energy", 50f);
            w.Advance(1);
            Assert.IsTrue(egg.UsedUp);
            Assert.AreEqual("eaten", egg.LostReason);
            Assert.AreEqual(50f + 15f * 2f - 0.7f, eater.StatOf("energy"), 1e-4f, "edible energy × diet scale");
            Assert.IsTrue(w.Events.Lines.Any(l => l.Contains("\"kind\": \"egg_lost\"") && l.Contains("\"reason\": \"eaten\"")));
            Assert.AreEqual(0, eggs.EntityCount, "removed in the environment phase");
        }

        [Test, Description("T-ENV-02 (ENV-02, ACT-30): two animals reaching for one item in one tick, under each act order → exactly one eats it")]
        public void ContestedItems([Values] ActOrder order)
        {
            int firstWins = 0, secondWins = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var w = Eco(seed, order);
                var food = w.Service<FoodGrid>();
                Place.Food(food, Place.At(10.5f, 10.5f));
                var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.1f, 10.5f)); a.Choose("eat");
                var b = Place.Animal(w.FindSpecies("prey"), Place.At(10.9f, 10.5f)); b.Choose("eat");
                w.Advance(1);
                Assert.AreEqual(1, a.Meals + b.Meals, $"{order}, seed {seed}: exactly one eats");
                if (a.Meals == 1) firstWins++; else secondWins++;
            }
            Assert.Greater(firstWins, 0, "the winner follows the random order");
            Assert.Greater(secondWins, 0);
        }

        [Test, Description("T-ENV-05, act part (ENV-11, ACT-05): a prey animal in cover next to a hunter is not its target and never struck; its kin still see it")]
        public void CoverHidesFromThreats()
        {
            var w = Eco(cover: true);
            var cover = w.Service<CoverLayer>();
            cover.Clear();
            Place.Cover(cover, Place.At(10.5f, 10.5f));
            var hidden = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10.5f)); hidden.Choose("rest");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(11.2f, 10.5f)); hunter.Choose("hunt");
            var kin = Place.Animal(w.FindSpecies("prey"), Place.At(13.5f, 10.5f));
            w.Space.Rebuild();
            Assert.AreSame(kin, w.Queries.NearestAnimal(hunter, AnimalSet.Prey)?.Animal, "the hidden one is not in the hunter's queries");
            Assert.AreSame(hidden, w.Queries.NearestAnimal(kin, AnimalSet.Kin)?.Animal, "its kin still see it");
            Assert.IsFalse(new InteractionResolver(w).Apply(hunter, Interaction.Strike(hidden), hidden.Position), "never struck");
            kin.IsGone = true;
            w.Advance(20);
            Assert.IsFalse(hidden.Killed, "a hunter next to it for 20 ticks never strikes it");
        }

        [Test, Description("T-ENV-06, act part (ENV-12): without a cover module, hide searches and nothing is hidden")]
        public void NoCoverModule()
        {
            var w = Eco(cover: false);
            Assert.IsNull(w.Service<Cover>());
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            a.Choose("hide");
            w.Advance(1);
            Assert.IsTrue(a.Searching);
            Assert.AreEqual(1, a.Species.Counters.Invalid);
            Assert.IsFalse(w.Queries.InCover(a));
        }

        [Test, Description("T-ENV-07 (ENV-20, ENV-22): a kill's carcass: 2 portions, never the killer, one per eater, removed when eaten up")]
        public void CarcassesFromKills()
        {
            var w = Eco(killChance: 1f);
            var prey = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10)); prey.Choose("rest");
            var killer = Place.Animal(w.FindSpecies("predator"), Place.At(10.5f, 10)); killer.Choose("hunt");
            w.Advance(1);
            var cs = w.Service<CarcassSystem>();
            Assert.AreEqual(1, cs.EntityCount);
            var carcass = cs.Entities[0];
            Assert.AreEqual(2, carcass.PortionsLeft);
            Assert.AreEqual(killer.Id, carcass.KillerId);
            killer.BusyTicks = 0;
            killer.Choose("hunt");
            var eaters = new List<Animal>();
            for (int i = 0; i < 3; i++) { var e = Place.Animal(w.FindSpecies("predator"), carcass.Position + new Vector3(0, 0, 0.3f * (i + 1))); e.Choose("hunt"); eaters.Add(e); }
            w.Advance(1);
            Assert.AreEqual(2, killer.Species.Counters.Portions);
            Assert.AreEqual(2, eaters.Count(e => e.Meals == 1), "two portions, one per eater");
            Assert.AreEqual(1, killer.Meals, "never to the killer");
            Assert.AreEqual(0, cs.EntityCount, "removed once eaten up");
        }
    }
}
