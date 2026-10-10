using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Eggs, from conception to birth (11 §2–§4): what an egg holds, the hatch order, what a birth records.</summary>
    public class EggTests : WorldFixture
    {
        [Test, Description("REPRO-20, REPRO-22, REPRO-24, GENE-32: an egg holds its parents, generation and ticks, at its first parent's place; eggs hatch in conception order; each birth records its litter and its laid tick; one crossover per baby")]
        public void FromConceptionToBirth()
        {
            var w = New(name: "eggs").Ecology(60f, 0f, 0f, regrow: 0f, prey: s =>
                {
                    s.LifeRules(40, 0, incubation: 3);
                    var x = s.Get<UniformCrossover>();
                    var go = x.gameObject;
                    Object.DestroyImmediate(x);
                    go.AddComponent<CountingCrossover>();
                })
                .Phase<ActPhase>().Phase<BreedPhase>().Phase<HatchPhase>().Build();
            var prey = w.FindSpecies("prey");
            var animals = new List<Animal>();
            foreach (var x in new[] { 10f, 10.5f, 40f, 40.5f })
            {
                var a = Place.Animal(prey, Place.At(x, 10));
                a.Age = 200; a.SetStat("energy", 90f); a.Choose("mate");
                animals.Add(a);
            }
            w.Events.Lines = new List<string>();
            w.Advance(1);

            var eggs = w.Service<EggSystem>().Entities.ToList();
            Assert.Greater(eggs.Count, 0);
            Assert.AreEqual(eggs.Count, prey.GetComponentInChildren<CountingCrossover>().Calls, "one crossover per baby (GENE-32)");
            foreach (var e in eggs)
            {
                Assert.AreEqual(prey, e.Species);
                var parents = e.Parents.Select(id => animals.Single(a => a.Id == id)).ToList();
                Assert.AreEqual(2, parents.Count);
                Assert.AreEqual(1, e.Generation);
                Assert.AreEqual(0, e.ConceivedTick);
                Assert.AreEqual(3, e.HatchTick);
                Assert.AreEqual(parents[0].Position.x, e.Position.x, 1e-4, "at the first parent's place (REPRO-20)");
                Assert.AreEqual(prey.Genes.Count, e.Alleles.Length, "a full genome waits in the egg");
            }
            Assert.AreEqual(0, prey.Counters.Births, "nothing hatches before the incubation ends");

            w.Advance(3);
            var births = w.Events.Lines.Select(JObject.Parse).Where(e => (string)e["kind"] == "birth").ToList();
            Assert.AreEqual(eggs.Count, births.Count);
            var order = births.Select(b => string.Join("+", b["parents"])).ToList();
            CollectionAssert.AreEqual(eggs.OrderBy(e => e.ConceptionOrder).Select(e => string.Join("+", e.Parents)).ToList(), order, "in conception order (REPRO-22)");
            foreach (var b in births)
            {
                Assert.AreEqual(3, (int)b["t"]);
                Assert.AreEqual(0, (int)b["laid"], "the laid tick (REPRO-24)");
                string parents = string.Join("+", b["parents"]);
                Assert.AreEqual(order.Count(p => p == parents), (int)b["litter"], "the litter of that conception (REPRO-24)");
            }
        }
    }
}
