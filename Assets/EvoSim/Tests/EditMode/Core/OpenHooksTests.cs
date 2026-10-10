using System.IO;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>
    /// The seams that let students build anything (owner 2026-10-10: no criteria, the system is open): births and deaths
    /// reach every module, modules add statistics columns and name their companions, diets and custom interactions decide
    /// what a meal is, brains with memory see every animal, covers combine, and entities of one's own kind are found.
    /// </summary>
    public class OpenHooksTests : WorldFixture
    {
        static string Root => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "EvoSim", "test-runs");

        [Test, Description("ANIM-42, open: species modules and world modules hear every birth (founders, babies, immigrants) and every death")]
        public void ModulesHearBirthsAndDeaths()
        {
            var b = New(5, name: "hooks").Lab1(prey: 20, predators: 3).Service<WorldHookProbe>("Hooks");
            var probe = b.Root.transform.Find("prey").gameObject.AddComponent<HookProbe>();
            var w = b.Build();
            w.Advance(800);
            var c = w.FindSpecies("prey").Counters;
            Assert.Greater(c.Births + c.DeathsTotal, 0, "something happened");
            Assert.AreEqual(c.Founders + c.Births + c.Immigrants, probe.Born);
            Assert.AreEqual(c.DeathsTotal, probe.Died);
            var hooks = w.Service<WorldHookProbe>();
            Assert.AreEqual(w.AllSpecies.Sum(s => s.Counters.Founders + s.Counters.Births + s.Counters.Immigrants), hooks.Born);
            Assert.AreEqual(w.AllSpecies.Sum(s => s.Counters.DeathsTotal), hooks.Died);
        }

        [Test, Description("13 §4, open: modules add stats.csv columns (a species module's with its prefix, a world module's at the end); a death cause recorded by any code gets its column, the rows before it empty")]
        public void ModulesAddStatsColumns()
        {
            const string name = "module-columns";
            string folder = Path.Combine(Root, name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            var b = New(19, name: name).Lab1(prey: 20, predators: 3).Recorder(true, "Logs/EvoSim/test-runs", name)
                .Service<WorldHookProbe>("Hooks")
                .Phase<CallbackPhase>(p => p.OnRun = t =>
                {
                    if (t.Tick != 150) return;
                    var prey = p.World.FindSpecies("prey");
                    p.World.RecordDeath(prey.Animals.First(x => !x.IsGone && !x.Killed), "lightning");   // a student's own death
                    prey.RemoveGone();
                });
            b.Root.transform.Find("prey").gameObject.AddComponent<HookProbe>();
            var w = b.Build();
            w.Advance(300);
            w.Stop("test");
            var lines = File.ReadAllLines(Path.Combine(w.Service<RunRecorder>().RunFolder, "stats.csv"));
            var header = lines[0].Split(',');
            Assert.AreEqual(4, lines.Length, "the header and three rows");
            foreach (var l in lines) Assert.AreEqual(header.Length, l.Split(',').Length, "every row has every column");
            int born = System.Array.IndexOf(header, "prey_probe_born"), lightning = System.Array.IndexOf(header, "prey_deaths_lightning");
            Assert.GreaterOrEqual(born, 0, lines[0]);
            Assert.GreaterOrEqual(lightning, 0, lines[0]);
            Assert.AreEqual("world_born", header[header.Length - 1], "a world module's columns come last");
            Assert.AreEqual("", lines[1].Split(',')[lightning], "no such death yet at tick 100");
            Assert.AreEqual("1", lines[2].Split(',')[lightning]);
            Assert.AreEqual(w.FindSpecies("prey").Module<HookProbe>().Born.ToString(), lines[3].Split(',')[born]);
        }

        [Test, Description("V-26, open: a module names the modules it needs with [RequiresModule]; a missing one is a warning whose fix adds it")]
        public void RequiredModulesAreReported()
        {
            var w = New(name: "requires").Flat(20, 20).DefaultBrain<RandomBrain>()
                .Species("prey", s => s.Population(0).Action<RestAction>("rest").Module<KinematicLocomotion>(name: "Locomotion").Module<NeedsEnergyProbe>())
                .BuildUninitialized();
            var report = new ValidationReport();
            Assert.IsTrue(w.Prepare(report), report.ToString());
            var m = report.Messages.Single(x => x.Id == "V-26");
            Assert.AreEqual(Severity.Warning, m.Severity);
            Assert.AreEqual("Add Energy", m.Fix.Label);
            m.Fix.Apply();
            var again = new ValidationReport();
            Assert.IsTrue(w.Prepare(again), again.ToString());
            Assert.IsFalse(again.Has("V-26"), again.ToString());
            Assert.IsNotNull(w.FindSpecies("prey").Module<Energy>());
        }

        [Test, Description("PROMPT-03, open: a placeholder reads a property as well as a field; {{ and }} write a brace")]
        public void PlaceholdersReadPropertiesAndEscapeBraces()
        {
            var w = New(name: "placeholders").Lab1(prey: 2, predators: 1).Build();
            var s = w.FindSpecies("prey");
            var writer = new PromptWriter(s);
            string text = writer.Fill("At most {CapRule.Cap} animals; answer {{\"action\": \"rest\"}}.");
            Assert.AreEqual($"At most {s.Module<CapRule>().Cap} animals; answer {{\"action\": \"rest\"}}.", text);
            Assert.IsEmpty(writer.Problems);
        }

        [Test, Description("ACT-11, open: the diet decides a strike's kill chance and a meal's energy (KillChanceAgainst, EnergyFrom)")]
        public void DietsDecideKillsAndMeals()
        {
            var b = New(3, name: "diet hooks").Ecology(40f, 0f, 0f, prey: s => s.Population(0), predator: s => s.Population(0), regrow: 0f)
                .Phase<ActPhase>().Phase<DeathPhase>().Phase<EnvironmentPhase>();
            var old = b.Root.transform.Find("predator").GetComponentInChildren<Diet>(true);
            var go = old.gameObject;
            Object.DestroyImmediate(old);
            go.AddComponent<SureKillDiet>().Set("prey", "carcass:prey");
            var w = b.Build();
            for (int i = 0; i < 5; i++)                                                    // the reference chance would miss some
            {
                var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(5 + 6 * i, 10)); hunter.Choose("hunt");
                hunter.SetStat("energy", 10f);
                var prey = Place.Animal(w.FindSpecies("prey"), Place.At(5.5f + 6 * i, 10)); prey.Choose("rest");
                w.Advance(1);
                Assert.IsTrue(prey.IsGone, $"strike {i} kills");
                Assert.AreEqual(1, hunter.Meals);
                Assert.Less(hunter.StatOf("energy"), 12f, "the meal gave 1, not the prey's 60");
                hunter.SetStat("energy", 0f);
                w.Advance(1);                                                              // the hunter starves out of the way
            }
        }

        [Test, Description("ACT-10, open: a custom interaction feeds through InteractionContext.Feed: energy gained, the meal counted")]
        public void CustomInteractionsFeed()
        {
            var w = New(name: "feed").Ecology(40f, 0f, 0f, prey: s => s.Population(0).Action<FeedAction>("snack"), predator: s => s.Population(0), regrow: 0f)
                .Phase<ActPhase>().Build();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            a.Choose("snack");
            a.SetStat("energy", 50f);
            w.Advance(1);
            Assert.AreEqual(1, a.Meals);
            Assert.AreEqual(1, a.Species.Counters.Meals);
            Assert.Greater(a.StatOf("energy"), 52f, "5 gained, less than that spent");
        }

        [Test, Description("DEC-30, DEC-32, open: a brain that isn't memoizable gets every deciding animal's query, with its id; nothing is memoised or shared")]
        public void BrainsWithMemorySeeEveryAnimal()
        {
            var w = New(9, name: "memory").Lab1(prey: 12, predators: 2, randomBrain: false).DefaultBrain<MemoryBrain>().Build();
            w.Advance(40);
            var brain = w.Service<MemoryBrain>();
            int decisions = w.AllSpecies.Sum(s => s.Counters.Decisions);
            Assert.Greater(decisions, 0);
            Assert.AreEqual(decisions, brain.AnimalIds.Count, "every decision reached the brain");
            Assert.IsTrue(brain.AnimalIds.All(id => id >= 0), "with its animal's id");
            Assert.AreEqual(0, w.AllSpecies.Sum(s => s.Counters.MemoHits));
        }

        [Test, Description("ENV-10, ENV-11, open: several Cover services combine: in cover in any, hidden by any, the nearest cover of all")]
        public void CoversCombine()
        {
            var w = New(name: "covers").Ecology(20f, 0f, 0f, prey: s => s.Population(0), predator: s => s.Population(0), regrow: 0f)
                .Service<SideCover>("Left", c => c.MaxX = 5f).Service<SideCover>("Right", c => c.MinX = 15f).Build();
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            var left = Place.Animal(prey, Place.At(2, 10));
            var right = Place.Animal(prey, Place.At(17, 10));
            var open = Place.Animal(prey, Place.At(12, 10));
            Assert.IsTrue(w.Queries.IsHiddenFrom(left, predator));
            Assert.IsTrue(w.Queries.IsHiddenFrom(right, predator), "the second cover hides too");
            Assert.IsFalse(w.Queries.IsHiddenFrom(open, predator));
            Assert.IsFalse(w.Queries.IsHiddenFrom(right, prey), "kin still see it (ENV-11)");
            Assert.IsTrue(w.Queries.InCover(right));
            var hit = w.Queries.NearestCover(open, 20f);
            Assert.IsTrue(hit.HasValue);
            Assert.AreEqual(3f, hit.Value.Distance, 1e-4f, "the right cover, 3 m away, not the left one at 7 m");
        }

        [Test, Description("ENV-20, SPACE-08, open: the nearest entity of a kind of one's own, across every system of that kind, with a filter; ties by id")]
        public void NearestEntityOfAnyKind()
        {
            var w = New(name: "fruit").Ecology(40f, 0f, 0f, prey: s => s.Population(0), predator: s => s.Population(0), regrow: 0f)
                .Service<FruitSystem>("Apples").Service<FruitSystem>("Pears").Build();
            var systems = w.ServicesOf<FruitSystem>().ToList();
            var apple = systems[0].Add(new Fruit { Position = Place.At(16, 10) });
            var pear = systems[1].Add(new Fruit { Position = Place.At(14, 10) });
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            var senses = new SenseContext(w);
            Assert.AreSame(pear, senses.NearestEntity<Fruit>(a, 20f).Value.Entity);
            Assert.AreSame(apple, senses.NearestEntity<Fruit>(a, 20f, (f, viewer) => f != pear).Value.Entity, "the filter");
            Assert.IsFalse(senses.NearestEntity<Fruit>(a, 3f).HasValue, "out of range");
            systems[0].Add(new Fruit { Position = Place.At(6, 10) });                       // as far as the pear, a later id
            Assert.AreSame(pear, senses.NearestEntity<Fruit>(a, 20f).Value.Entity, "a tie goes to the lower id");
            Assert.AreSame(pear, new ActContext(w).NearestEntity<Fruit>(a).Value.Entity, "within the animal's vision");
        }

        [Test, Description("SENSE-04, open: an action says whether it hunts; the sample ChasedSense reads that, not the class")]
        public void ActionsSayWhetherTheyHunt()
        {
            var w = New(name: "hunts").Lab1(prey: 2, predators: 1).Build();
            Assert.IsTrue(w.FindSpecies("predator").FindAction("hunt").Hunts);
            Assert.IsFalse(w.FindSpecies("predator").FindAction("rest").Hunts);
            Assert.IsFalse(w.FindSpecies("prey").Actions.Any(x => x.Hunts));
        }
    }
}
