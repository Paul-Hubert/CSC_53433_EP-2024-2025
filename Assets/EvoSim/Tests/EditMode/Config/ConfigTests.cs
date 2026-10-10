using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Configuration: fields, scenario overrides, traits (14).</summary>
    public class ConfigTests : WorldFixture
    {
        [Test, Description("T-CFG-01 (CFG-01): every module setting is visible in the inspector with a tooltip; numbers carry a range (Range or Min)")]
        public void EveryFieldIsDocumented()
        {
            var problems = new List<string>();
            var types = new[] { typeof(World).Assembly, AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "EvoSim.Http") }
                .Where(a => a != null).SelectMany(a => a.GetTypes())
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && (typeof(WorldModule).IsAssignableFrom(t) || typeof(SpeciesModule).IsAssignableFrom(t)
                            || t == typeof(World) || t == typeof(Species) || t == typeof(Edible)));
            foreach (var t in types)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!FieldPath.IsSerialized(f)) continue;
                    var tip = f.GetCustomAttribute<TooltipAttribute>();
                    if (tip == null || string.IsNullOrWhiteSpace(tip.tooltip)) { problems.Add($"{t.Name}.{f.Name}: no tooltip"); continue; }
                    bool number = f.FieldType == typeof(float) || f.FieldType == typeof(int);
                    if (number && f.GetCustomAttribute<UnityEngine.RangeAttribute>() == null && f.GetCustomAttribute<MinAttribute>() == null
                        && !tip.tooltip.Any(char.IsDigit))
                        problems.Add($"{t.Name}.{f.Name}: a number without a range or a reference value");
                }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test, Description("T-CFG-02 (CFG-02, CFG-03): a scenario overriding Prey/Litter/max and World/decisionPeriod → applied, and saved in run_info.json")]
        public void ScenarioOverrides()
        {
            var b = New().Lab1();
            var w = b.BuildUninitialized();
            var scenario = ScriptableObject.CreateInstance<ScenarioAsset>();
            scenario.set.Add(new ScenarioAsset.Override("Prey/Litter/max", "7"));
            scenario.set.Add(new ScenarioAsset.Override("World/decisionPeriod", "2"));
            scenario.variants[0].remove.Add("prey/Actions/hide");
            scenario.ticks = 50;
            var problems = new List<string>();
            var applied = scenario.Apply(w, "default", 99, problems);
            Assert.IsEmpty(problems, string.Join("; ", problems));
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.AreEqual(7, w.FindSpecies("prey").Module<Litter>().Max);
            Assert.AreEqual(2, w.DecisionPeriod);
            Assert.AreEqual(99, w.Seed);
            Assert.AreEqual(50, w.TickLimit);
            Assert.IsNull(w.FindSpecies("prey").FindAction("hide"), "removed");
            Assert.AreEqual("7", applied["Prey/Litter/max"]);
            var info = RunInfo.Collect(w);
            var settings = (SortedDictionary<string, object>)info["settings"];
            Assert.AreEqual(7, settings["prey/Life/Litter/Litter/max"]);
            Assert.AreEqual(2, settings["World/decisionPeriod"]);
            Assert.AreEqual("2", FieldPath.Get(w, "World/decisionPeriod"));
            Assert.IsFalse(FieldPath.Set(w, "prey/Litter/nothing", "1", out var error));
            StringAssert.Contains("no field", error);
            UnityEngine.Object.DestroyImmediate(scenario);
        }

        [Test, Description("T-CFG-03 (CFG-04, ANIM-16): every parameter a number gene may target is read by the reference modules through its trait")]
        public void TraitsDriveBehaviour()
        {
            void With(string trait, float value, out World w)
            {
                bool preyHasIt = trait != "kill.chance";                       // only hunters have a kill chance
                w = New(name: trait + value).Ecology(40f, 0f, 0f, regrow: 0f,
                        prey: s => { if (preyHasIt) s.Module<NumberGene>(g => g.Configure(trait, 0f, 1000f, new[] { value }, false, "g")); },
                        predator: p => p.Module<NumberGene>(g => g.Configure(trait, 0f, 1000f, new[] { value }, false, "g")))
                    .Phase<ActPhase>().Phase<DeathPhase>().Build();
            }

            With("speed.walk", 2f, out var w1);
            var a = Place.Animal(w1.FindSpecies("prey"), Place.At(10, 10));
            a.Choose("follow");
            Place.Animal(w1.FindSpecies("prey"), Place.At(20, 10)).Choose("rest");
            w1.Advance(1);
            Assert.AreEqual(12f, a.Position.x, 1e-4f, "speed.walk");

            With("speed.run", 3f, out var w2);
            var h = Place.Animal(w2.FindSpecies("predator"), Place.At(10, 10));
            h.Choose("hunt");
            Place.Animal(w2.FindSpecies("prey"), Place.At(20, 10)).Choose("rest");
            w2.Advance(1);
            Assert.AreEqual(13f, h.Position.x, 1e-4f, "speed.run");

            With("stamina.max", 10f, out var w3);
            var b = Place.Animal(w3.FindSpecies("prey"), Place.At(10, 10));
            Assert.AreEqual(10f, b.StatOf("stamina"), "stamina.max: starts full at the gene's value");
            b.SetStat("stamina", 50f);
            Assert.AreEqual(10f, b.StatOf("stamina"), "and caps there");

            With("stamina.regen", 5f, out var w4);
            var c = Place.Animal(w4.FindSpecies("prey"), Place.At(10, 10));
            c.Choose("rest");
            c.SetStat("stamina", 10f);
            w4.Advance(1);
            Assert.AreEqual(15f, c.StatOf("stamina"), 1e-4f, "stamina.regen");

            With("vision", 5f, out var w5);
            var d = Place.Animal(w5.FindSpecies("prey"), Place.At(10, 10));
            Place.Animal(w5.FindSpecies("predator"), Place.At(18, 10));
            w5.Space.Rebuild();
            var sense = d.Species.FindSense("Predator");
            Assert.AreEqual("none", sense.Tokens[sense.Read(d, new SenseContext(w5))], "vision: a predator at 8 m is beyond 5 m");

            With("maturity", 10f, out var w6);
            var e = Place.Animal(w6.FindSpecies("prey"), Place.At(10, 10));
            e.Age = 10;
            Assert.IsTrue(e.Species.Module<MatingRule>().IsAdult(e), "maturity");
            var age = e.Species.FindSense("Age");
            Assert.AreEqual("adult", age.Tokens[age.Read(e, new SenseContext(w6))]);

            With("kill.chance", 0f, out var w7);
            var k = Place.Animal(w7.FindSpecies("predator"), Place.At(10, 10));
            var victim = Place.Animal(w7.FindSpecies("prey"), Place.At(10.5f, 10));
            victim.Choose("rest");
            k.Choose("hunt");
            w7.Advance(20);
            Assert.IsFalse(victim.Killed, "kill.chance 0: never kills");
        }
    }
}
