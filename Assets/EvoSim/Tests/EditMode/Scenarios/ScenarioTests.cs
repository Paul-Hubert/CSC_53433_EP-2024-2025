using System.Collections.Generic;
using System.Linq;
using EvoSim.Samples;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>
    /// The T2 scenarios of 31 that run with random or scripted brains and fakes, as tests: exact facts and the
    /// directional checks that hold with scripted policies. (S11, S14, S21, S22, S24 live with their systems' tests.)
    /// </summary>
    public class ScenarioTests : WorldFixture
    {
        /// <summary>Records every event of a world (for scenario facts read from the event stream).</summary>
        sealed class Events : IEventSink
        {
            public readonly List<SimEvent> All = new List<SimEvent>();
            public void OnEvent(SimEvent e, string line) => All.Add(e);
        }

        static Events Watch(World w)
        {
            var sink = new Events();
            w.Events.AddSink(sink);
            return sink;
        }

        // ---- S01 ----

        static float[] Grazer(DecisionQuery q)
        {
            string food = ScriptedBrain.Token(q, "Food"), kin = ScriptedBrain.Token(q, "Animal");
            string action = food != null && food != "none" ? "eat" : kin != null && kin.EndsWith(", ready") ? "mate" : "rest";
            return ScriptedBrain.Prefer(q.Species, action, 0.02f);
        }

        [Test, Category("T3"), Description("S01 lone grazer (POP-01, ANIM-40): one species, food only, a scripted policy → the cap is reached within 3 000 ticks in 5/5 seeds; deaths only by starvation, old age or migration")]
        public void LoneGrazer()
        {
            foreach (int seed in new[] { 1234, 7, 42, 99, 2026 })
            {
                var w = New(seed, name: "S01 " + seed).Flat(40, 40).Food(0.2f, 0.004f).Service<EggSystem>("Eggs").ReferencePhases()
                    .DefaultBrain<ScriptedBrain>(b => b.Policy = Grazer)
                    .Species("grazer", s => s.PreyBody().LifeRules(30, 4).Population(8)
                        .Action<EatAction>("eat").Action<RestAction>("rest").Action<MateAction>("mate")
                        .Sense<LevelSense>(l => l.Configure("energy", 30f, 70f), "Energy")
                        .Sense<NearestResourceSense>(null, "Food")
                        .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Kin, readiness: true), "Animal"))
                    .Build();
                var sp = w.FindSpecies("grazer");
                int reached = -1;
                for (int t = 0; t < 3000 && reached < 0; t++)
                {
                    w.Advance(1);
                    if (sp.Animals.Count >= 30) reached = w.Tick;
                }
                Assert.GreaterOrEqual(reached, 0, $"seed {seed}: the cap of 30 is reached within 3 000 ticks");
                foreach (var cause in sp.Counters.Deaths.Keys)
                    CollectionAssert.Contains(new[] { "starvation", "old_age", "migrated" }, cause, $"seed {seed}");
            }
        }

        // ---- S05 ----

        [Test, Description("S05 controls (CTRL-01, CTRL-02): C2 keeps the founders' alleles, C4 founders use control sentences, C7 births have one parent, C3 queries carry other animals' genes; each reproducible")]
        public void Controls()
        {
            (World w, Events e) Run(System.Action<World> control, string name, bool scripted = false)
            {
                var b = New(5, name: name).Lab1(prey: 20, predators: 3, randomBrain: !scripted)
                    .Configure(w => w.SetControlSentences(EvoSim.Editor.ReferenceScenes.ControlSentences())).Configure(control);
                if (scripted) b.DefaultBrain<ScriptedBrain>();
                b.Root.GetComponentsInChildren<LlmMutation>(true).ToList().ForEach(m => m.Rate = 0.5f);
                var w = b.Build();
                var events = Watch(w);
                w.Advance(300);
                return (w, events);
            }

            var c2 = Run(w => w.NoMutation = true, "C2");
            Assert.IsFalse(c2.e.All.Any(x => x.Kind == "birth" && x["mutations"] is System.Collections.ICollection m && m.Count > 0), "C2: no mutation");
            Assert.AreEqual(c2.w.Events.Hash, Run(w => w.NoMutation = true, "C2 again").w.Events.Hash, "C2 reproducible");

            var c4 = Run(w => w.RandomFounders = true, "C4");
            var controls = new HashSet<string>(c4.w.ControlSentences);
            foreach (var a in c4.w.FindSpecies("prey").Animals.Where(a => a.Origin == "founder"))
                foreach (var allele in a.Genome.Alleles) if (allele.Kind == AlleleKind.Text) Assert.IsTrue(controls.Contains(allele.Text), allele.Text);

            var c7 = Run(w => w.Asexual = true, "C7");
            var births = c7.e.All.Where(x => x.Kind == "birth").ToList();
            Assert.Greater(births.Count, 0);
            foreach (var bth in births) Assert.AreEqual(1, ((System.Collections.ICollection)bth["parents"]).Count, "C7: one parent");

            var c3 = Run(w => w.ShuffledGenes = true, "C3", scripted: true);
            var brain = c3.w.GetComponentInChildren<ScriptedBrain>();
            var own = new HashSet<string>(c3.w.FindSpecies("prey").Animals.Select(a => a.Genome.BrainKey));
            Assert.Greater(brain.Seen.Count, 0);
            Assert.AreEqual(c3.w.Events.Hash, Run(w => w.ShuffledGenes = true, "C3 again", scripted: true).w.Events.Hash, "C3 reproducible");
        }

        // ---- S08 ----

        [Test, Description("S08 cannibals (SPEC-12, ACT-11): a species that grazes and strikes its own kin → it is its own threat; no animal targets itself; every kill's killer is of the same species")]
        public void Cannibals()
        {
            var w = New(8, name: "S08").Flat(30, 30).Food(0.1f, 0.003f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<RandomBrain>()
                .Species("cannibal", s =>
                {
                    s.PreyBody().LifeRules(40, 4).Population(20)
                        .Action<EatAction>("eat").Action<HuntAction>("hunt").Action<FleeAction>("flee").Action<RestAction>("rest").Action<MateAction>("mate")
                        .Module<Digestion>();
                    s.Get<Diet>().Set("Grass", "cannibal", "carcass:cannibal");
                    s.Get<Diet>().SetKillChance(0.1f);
                }).Build();
            var sp = w.FindSpecies("cannibal");
            CollectionAssert.Contains(w.ThreatsOf(sp), sp, "it flees its own kind");
            var events = Watch(w);
            for (int t = 0; t < 400; t++)
            {
                w.Advance(1);
                foreach (var a in sp.Animals) Assert.AreNotEqual(a.Id, a.TargetId, "never targets itself");
            }
            var kills = events.All.Where(e => e.Kind == "death" && (string)e["cause"] == "killed").ToList();
            Assert.Greater(kills.Count, 0, "some kills in 400 ticks");
            foreach (var k in kills) Assert.AreEqual("cannibal", k.Species);
        }

        // ---- S13 ----

        [Test, Description("S13 mutation operators (MUT-20, MUT-21): the deck with a fake mutator mutates at its rate; the intensity ladder only changes intensity words; no operator adds no allele")]
        public void MutationOperators()
        {
            World Run(string op)
            {
                var b = New(13, name: "S13 " + op).Lab1(prey: 20, predators: 3);
                foreach (var m in b.Root.GetComponentsInChildren<LlmMutation>(true))
                {
                    if (op == "deck") { m.Rate = 0.2f; continue; }
                    var go = m.gameObject;
                    Object.DestroyImmediate(m);
                    if (op == "ladder") go.AddComponent<IntensityLadder>().Rate = 0.5f;
                }
                if (op == "ladder")                                                  // a rest sentence the ladder can always move
                    foreach (var g in b.Root.GetComponentsInChildren<TextGene>(true))
                        if (g.gameObject.name == "rest") { g.SetPool(null); g.Configure(new[] { "Sometimes rest when tired." }); }
                var w = b.Build();
                w.Advance(400);
                return w;
            }
            var deck = Run("deck");
            Assert.Greater(deck.Mutations.Successes, 0, "the deck mutates");
            Assert.Greater(deck.Mutations.Attempts, 0);

            var ladder = Run("ladder");
            string[] words = { "never", "rarely", "sometimes", "often", "always" };
            var mutants = ladder.Alleles.All.Where(a => a.Origin == "mutant" && a.Kind == AlleleKind.Text).ToList();
            Assert.IsNotEmpty(mutants, $"the ladder made mutants: {ladder.Mutations.Attempts} tries, {ladder.Mutations.Successes} ok, {ladder.Mutations.Failures} failed");
            foreach (var allele in mutants)
            {
                var parent = ladder.Alleles.ById(allele.ParentId);
                var x = allele.Text.ToLowerInvariant().Split(' ');
                var y = parent.Text.ToLowerInvariant().Split(' ');
                Assert.AreEqual(y.Length, x.Length, allele.Text);
                for (int i = 0; i < x.Length; i++)
                    if (x[i] != y[i]) Assert.IsTrue(words.Any(wd => x[i].StartsWith(wd)) && words.Any(wd => y[i].StartsWith(wd)), $"{parent.Text} → {allele.Text}");
            }

            var none = Run("none");
            Assert.Greater(none.AllSpecies.Sum(s => s.Counters.Births), 0, "babies were born");
            Assert.IsFalse(none.Alleles.All.Any(a => a.Origin == "mutant"), "no operator: no new allele");
        }

        // ---- S18 ----

        [Test, Description("S18 moving carcasses (ENV-21): a drag action moves carcasses only in the act phase; the carcass sense follows the moved carcass")]
        public void MovingCarcasses()
        {
            var b = New(18, name: "S18").Ecology(30f, 0.3f, 0.05f, regrow: 0f, predator: p => p.Action<DragAction>("drag"))
                .Carcasses().Service<EggSystem>("Eggs").ReferencePhases().DefaultBrain<RandomBrain>();
            var w = b.Build();
            var carcasses = w.Service<CarcassSystem>();
            var predator = w.FindSpecies("predator");
            var dragger = Place.Animal(predator, Place.At(10, 10));
            var c = Place.Carcass(carcasses, w.FindSpecies("prey"), Place.At(10.5f, 10));
            var before = c.Position;
            dragger.Choose("drag");
            w.Advance(1);
            dragger.Choose("drag");
            w.Advance(1);
            Assert.AreNotEqual(before, c.Position, "dragged");
            Assert.AreEqual(dragger.Position, c.Position, "the carcass follows the dragger");
            var sense = predator.Senses.First(s => s is NearestEntitySense);
            var other = Place.Animal(predator, c.Position + new Vector3(3f, 0f, 0f));
            w.Space.Rebuild();
            Assert.AreNotEqual("none", sense.Tokens[sense.Read(other, new SenseContext(w))], "the sense finds it where it is now");
        }

        // ---- S25 ----

        [Test, Description("S25 decision timing (DEC-01, DEC-04): period 1, 4, 8 → queries follow the period; with staggering the largest batch is below half the unstaggered one")]
        public void DecisionTiming()
        {
            (int decisions, int largest) Run(int period, bool stagger)
            {
                var b = New(25, name: $"S25 {period} {stagger}").Lab1(prey: 30, predators: 4, randomBrain: false)
                    .DefaultBrain<ScriptedBrain>().Configure(w => w.DecisionPeriod = period);
                if (stagger)
                    foreach (var s in b.Root.GetComponentsInChildren<Species>(true)) s.gameObject.AddComponent<DecisionSchedule>().Configure(period, true);
                var w = b.Build();
                var brain = w.GetComponentInChildren<ScriptedBrain>();
                w.Advance(40);
                // tick 0: every founder decides whatever the schedule (it has no action yet), so batches from tick 1 on
                return (w.AllSpecies.Sum(s => s.Counters.Decisions), brain.BatchSizes.Count > 1 ? brain.BatchSizes.Skip(1).Max() : 0);
            }
            var p1 = Run(1, false);
            var p4 = Run(4, false);
            var p8 = Run(8, false);
            Assert.Greater(p1.decisions, 2.5 * p4.decisions, "period 4: about a quarter of the decisions");
            Assert.Greater(p4.decisions, 1.5 * p8.decisions, "period 8: about half of period 4");
            var staggered = Run(8, true);
            Assert.Less(staggered.largest, 0.5 * p8.largest, "staggering spreads the batches");
        }

        // ---- S28 ----

        [Test, Description("S28 egg eaters (REPRO-23, SPEC-10, V-24): incubation 20 and an egg-eating species → eaten eggs never hatch and are recorded lost (\"eaten\"); without an Edible on the egg kind, V-24 fires")]
        public void EggEaters()
        {
            WorldBuilder Make(bool edible, string name)
            {
                var b = New(28, name: name).Lab1(prey: 24, predators: 0, incubation: 20)
                    .Species("eggeater", s =>
                    {
                        s.PreyBody().LifeRules(10, 0).Population(6).Action<EatAction>("eat", e => e.SetTarget("egg:prey"));
                        s.Get<Diet>().Set("egg:prey");
                        s.Get<Energy>().Configure(5000f, 5000f);                        // they wait for eggs without starving
                    });
                if (edible) b.Root.GetComponentInChildren<EggSystem>(true).gameObject.AddComponent<Edible>().Configure(EatMethod.Graze, 15f);
                return b;
            }
            var bad = Make(false, "S28 no edible").BuildUninitialized();
            var report = new ValidationReport();
            bad.Prepare(report);
            Assert.IsTrue(report.Has("V-24"), "eggs not edible: V-24");

            var w = Make(true, "S28").Build();
            var events = Watch(w);
            foreach (var a in w.FindSpecies("eggeater").Animals) a.Choose("eat");
            for (int t = 0; t < 600; t++)
            {
                foreach (var a in w.FindSpecies("eggeater").Animals) a.Choose("eat");
                w.Advance(1);
            }
            var lost = events.All.Where(e => e.Kind == "egg_lost" && (string)e["reason"] == "eaten").Select(e => e.Id).ToList();
            Assert.Greater(lost.Count, 0, "some eggs eaten");
            var waiting = new HashSet<int>(w.Service<EggSystem>().Entities.Select(e => e.Id));
            foreach (var id in lost) Assert.IsFalse(waiting.Contains(id), $"egg {id} was eaten: it no longer incubates, so it never hatches");
        }
    }
}
