using System.Collections.Generic;
using System.Linq;
using EvoSim.Samples;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>The T3 scenarios of 31 with scripted or random brains: mechanics over thousands of ticks (nightly).</summary>
    [Category("T3")]
    public class LongScenarioTests : WorldFixture
    {
        static readonly int[] Seeds = { 1234, 7, 42, 99, 2026 };

        static bool Near(string token) => token != null && (token == "here" || token.StartsWith("adjacent") || token.StartsWith("close"));
        static bool InRange(string token) => Near(token) || (token != null && token.StartsWith("medium"));
        static bool Seen(string token) => token != null && token != "none" && !token.StartsWith("none");

        // ---- S06 ----

        static float[] Prey(DecisionQuery q)
        {
            var s = q.Species;
            if (s.Id == "predator")
                return ScriptedBrain.Prefer(s, Seen(ScriptedBrain.Token(q, "Prey")) ? "hunt" : "rest", 0.05f);
            string threat = ScriptedBrain.Token(q, "Predator"), cover = ScriptedBrain.Token(q, "Cover"), food = ScriptedBrain.Token(q, "Food");
            // React while the threat is still at medium range: from "close", a hunter at 2 m per tick arrives first.
            string action = InRange(threat) && s.FindAction("hide") != null && Near(cover) ? "hide"
                          : InRange(threat) ? "flee"
                          : Seen(food) ? "eat" : "rest";
            return ScriptedBrain.Prefer(s, action, 0.05f);
        }

        [Test, Description("S06 hide versus flee, mechanics (ACT-31, ENV-12): a scripted prey that hides when a threat is close and cover is near is killed less than a flee-only prey in >= 4/5 seeds")]
        public void HideVersusFlee()
        {
            int Kills(int seed, bool hide)
            {
                var b = New(seed, name: $"S06 {seed} {hide}").Lab1(prey: 30, predators: 5, randomBrain: false).DefaultBrain<ScriptedBrain>(x => x.Policy = Prey);
                b.Configure(w => w.NoMutation = true);
                if (!hide)
                {
                    var prey = b.Root.GetComponentsInChildren<Species>(true).First(s => s.gameObject.name == "prey");
                    Object.DestroyImmediate(prey.GetComponentInChildren<HideAction>(true).gameObject);
                    Object.DestroyImmediate(prey.GetComponentInChildren<NearestCoverSense>(true).gameObject);
                }
                var w = b.Build();
                w.Advance(1500);
                return w.FindSpecies("predator").Counters.Kills;
            }
            int fewer = 0;
            var lines = new List<string>();
            foreach (int seed in Seeds)
            {
                int withHide = Kills(seed, true), fleeOnly = Kills(seed, false);
                lines.Add($"seed {seed}: kills {withHide} with hide, {fleeOnly} flee-only");
                if (withHide < fleeOnly) fewer++;
            }
            Assert.GreaterOrEqual(fewer, 4, string.Join("; ", lines));
        }

        // ---- S10 ----

        [Test, Description("S10 six-species web (SPEC-10…13, DEC-15): two grazers, two mid hunters, an apex hunter and a pure scavenger → one batch per brain per tick; the scavenger eats only carcass portions; the derived threats follow the diets")]
        public void SixSpeciesWeb()
        {
            SpeciesSetup Grazer(SpeciesSetup s, int n) => s.PreyBody().LifeRules(30, 3).Population(n)
                .Action<EatAction>("eat").Action<FleeAction>("flee").Action<RestAction>("rest").Action<MateAction>("mate")
                .Sense<NearestResourceSense>(null, "Food").Sense<NearestAnimalSense>(x => x.Configure(AnimalSet.Threats), "Predator");
            SpeciesSetup Hunter(SpeciesSetup s, int n, params string[] diet)
            {
                s.PreyBody().LifeRules(10, 1).Population(n).Module<Digestion>()
                    .Action<HuntAction>("hunt").Action<RestAction>("rest").Action<MateAction>("mate")
                    .Sense<NearestAnimalSense>(x => x.Configure(AnimalSet.Prey), "Prey");
                s.Get<Diet>().Set(diet);
                s.Get<Diet>().SetKillChance(0.5f);
                return s;
            }
            var w = New(10, name: "S10").Flat(60, 60).Food(0.15f, 0.003f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<ScriptedBrain>()
                .Species("deer", s => Grazer(s, 12))
                .Species("rabbit", s => Grazer(s, 12))
                .Species("fox", s => Hunter(s, 4, "rabbit", "carcass:rabbit"))
                .Species("lynx", s => Hunter(s, 4, "deer", "rabbit", "carcass:deer"))
                .Species("wolf", s => Hunter(s, 2, "deer", "fox", "lynx", "carcass:deer"))
                .Species("vulture", s =>
                {
                    s.PreyBody().LifeRules(10, 2).Population(4).Module<Digestion>()
                        .Action<HuntAction>("scavenge").Action<RestAction>("rest").Action<MateAction>("mate")
                        .Sense<NearestEntitySense>(null, "Carcass");
                    s.Get<Diet>().Set("carcass:deer", "carcass:rabbit", "carcass:fox", "carcass:lynx");
                })
                .Build();
            string Threats(string id) => string.Join(",", w.ThreatsOf(w.FindSpecies(id)).Select(x => x.Id).OrderBy(x => x));
            Assert.AreEqual("lynx,wolf", Threats("deer"));
            Assert.AreEqual("fox,lynx", Threats("rabbit"));
            Assert.AreEqual("wolf", Threats("fox"));
            Assert.AreEqual("", Threats("vulture"), "nobody strikes the scavenger");
            var brain = w.GetComponentInChildren<ScriptedBrain>();
            w.Advance(1);
            Assert.AreEqual(1, brain.Batches, "one batch per brain per tick (DEC-15)");
            Assert.AreEqual(6, brain.Seen.Select(q => q.Species).Distinct().Count(), "the first batch mixes all six species (identical queries are sent once)");
            w.Advance(599);
            Assert.LessOrEqual(brain.Batches, 600, "never more than one batch per tick (the memo answers the rest)");
            var v = w.FindSpecies("vulture").Counters;
            Assert.AreEqual(v.Meals, v.Portions, "the scavenger's meals are all carcass portions");
        }

        // ---- S16 ----

        static float[] Thirsty(DecisionQuery q)
        {
            var s = q.Species;
            string thirst = ScriptedBrain.Token(q, "Thirst"), water = ScriptedBrain.Token(q, "Water"), food = ScriptedBrain.Token(q, "Food");
            string action = thirst == "high" && s.FindAction("drink") != null && Seen(water) ? "drink" : Seen(food) ? "eat" : "rest";
            return ScriptedBrain.Prefer(s, action, 0.02f);
        }

        [Test, Description("S16 water and thirst (samples 22 §1–§3): with a scripted policy, deaths by thirst occur without the drink action and are under 10 % of deaths with it")]
        public void WaterAndThirst()
        {
            Species Run(bool drink)
            {
                var w = New(16, name: "S16 " + drink).Ground<PondGround>(24, 24).Food(0.2f, 0.005f).Service<EggSystem>("Eggs").ReferencePhases()
                    .DefaultBrain<ScriptedBrain>(b => b.Policy = Thirsty)
                    .Species("prey", s =>
                    {
                        s.PreyBody().LifeRules(30, 4).Population(16).Module<Thirst>(t => t.Configure(1f, 120f)).Module<DiesOfThirst>()
                            .Action<EatAction>("eat").Action<RestAction>("rest")
                            .Sense<NearestResourceSense>(null, "Food").Sense<NearestWaterSense>(null, "Water")
                            .Sense<LevelSense>(l => l.Configure("thirst", 30f, 70f), "Thirst");
                        if (drink) s.Action<DrinkAction>("drink");
                    }).Build();
                w.Advance(1500);
                return w.FindSpecies("prey");
            }
            var without = Run(false).Counters;
            Assert.Greater(without.Deaths.TryGetValue("thirst", out int t0) ? t0 : 0, 0, "without drink, animals die of thirst");
            var with = Run(true).Counters;
            int thirst = with.Deaths.TryGetValue("thirst", out int t1) ? t1 : 0;
            Assert.Less(thirst, 0.1 * Mathf.Max(1, with.DeathsTotal), $"with drink: {thirst} of {with.DeathsTotal} deaths by thirst");
        }

        // ---- S17 ----

        [Test, Description("S17 terrain and locomotion (MOVE-02, SPACE-05): on the terrain scene, no animal stands on water or unwalkable ground; a predator with the slope locomotion never climbs steeper than its limit")]
        public void SlopesOnTerrain()
        {
            string result = Batch.WithScene("Assets/EvoSim/Scenes/Terrain_Locomotion.unity", w =>
            {
                var predatorGo = w.GetComponentsInChildren<Species>(true).First(s => s.gameObject.name == "Predator");
                var kinematic = predatorGo.GetComponentInChildren<KinematicLocomotion>(true);
                var holder = kinematic.gameObject;
                Object.DestroyImmediate(kinematic);
                holder.AddComponent<SlopeLocomotion>().Configure(30f, 0.3f);
                w.DefaultBrain = w.GetComponentInChildren<RandomBrain>(true);
                w.NoMutation = true;
                w.WaitMode = WaitMode.Freeze;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                var loco = w.FindSpecies("predator").Module<SlopeLocomotion>();
                Assert.IsNotNull(loco);
                for (int t = 0; t < 800; t++)
                {
                    w.Advance(1);
                    foreach (var s in w.AllSpecies)
                        foreach (var a in s.Animals)
                        {
                            Assert.IsTrue(w.Ground.IsWalkable(a.Position), $"{a} on water or a mountain at {a.Position}");
                            if (s.Id != "predator" || a.MetersMoved < 0.5f) continue;
                            float rise = a.Position.y - a.PreviousPosition.y;
                            float run = new Vector2(a.Position.x - a.PreviousPosition.x, a.Position.z - a.PreviousPosition.z).magnitude;
                            if (rise > 0f && run > 0.5f)
                                Assert.LessOrEqual(Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 30f + 8f, $"{a} climbed too steeply");
                        }
                }
                return "ok";
            });
            Assert.AreEqual("ok", result);
        }

        // ---- S23 ----

        [Test, Description("S23 scale (20 §9): the full world with caps × 6 (about 2 000 animals), random brain → a mean tick under 12 ms after warm-up; memo hit rate reported")]
        public void Scale()
        {
            var scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/S23_Scale.asset");
            string result = Batch.WithScene(scenario.scene, w =>
            {
                scenario.Apply(w, "x6", 1234, new List<string>());
                w.WaitMode = WaitMode.Freeze;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                int start = w.AllSpecies.Sum(s => s.Animals.Count);
                w.Advance(100);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                w.Advance(100);
                double ms = clock.Elapsed.TotalMilliseconds / 100.0;
                int animals = w.AllSpecies.Sum(s => s.Animals.Count);
                long decisions = w.AllSpecies.Sum(s => (long)s.Counters.Decisions), memo = w.AllSpecies.Sum(s => (long)s.Counters.MemoHits);
                TestContext.WriteLine($"S23: {start} animals at the start, {animals} after 200 ticks, {ms:0.00} ms per tick, memo hit rate {100.0 * memo / Mathf.Max(1, decisions):0}%");
                Assert.Greater(start, 900, "about 1 000 animals at the start, caps for 2 000");
                Assert.Less(ms, 12.0, "tick time");
                return "ok";
            });
            Assert.AreEqual("ok", result);
        }
    }
}
