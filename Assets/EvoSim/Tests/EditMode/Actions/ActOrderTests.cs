using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks what each act order lets an animal see (ACT-31, 07 §3), that the act phase never lays eggs (ACT-13, 11), and
    /// that babies are aged on their birth tick (ANIM-35).
    /// </summary>
    /// <remarks>
    /// Each test builds a small Lab ecology (<see cref="Lab.Ecology"/>: 40 m, no food, no cover) with only the phases it
    /// needs. Animals are placed by hand with <see cref="Place.Animal"/> and given an action with <see cref="Lab.Choose"/>;
    /// without a decision phase they keep it. Only <see cref="BabiesAgeOnTheirBirthTick"/> asks a brain: a
    /// <see cref="ScriptedBrain"/> that always prefers <c>mate</c>.
    /// </remarks>
    public class ActOrderTests : WorldFixture
    {
        [Test, Description("T-MOVE-05 (ACT-31): in sequential orders an animal acts on the state left by the animals before it; when simultaneous, every intent comes from the start state")]
        public void LaterActorsSeeEarlierMoves([Values(ActOrder.SpeciesInTurn, ActOrder.AllMixed, ActOrder.Simultaneous)] ActOrder order)
        {
            var w = New(name: "visibility " + order).Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.Action<GoToAction>("go", a => a.Target = Place.At(0, 10)))
                .Phase<ActPhase>(p => p.Order = order).Build();
            var runner = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            runner.Choose("go");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(20, 10));
            hunter.Choose("hunt");
            bool runnerFirst = order == ActOrder.SpeciesInTurn;                       // prey act before predators
            for (int t = 0; t < 6; t++)
            {
                var start = runner.Position;
                w.Advance(1);
                Assert.Less(runner.Position.x, start.x, "the runner moved away");
                if (order == ActOrder.Simultaneous)
                    Assert.AreEqual(start.x, hunter.TargetPoint.x, 1e-4, "simultaneous: the hunter aimed at where the runner stood at the start");
                else if (runnerFirst)
                    Assert.AreEqual(runner.Position.x, hunter.TargetPoint.x, 1e-4, "sequential: the hunter, acting later, aimed at the runner's new place");
                else
                    Assert.IsTrue(Mathf.Abs(hunter.TargetPoint.x - runner.Position.x) < 1e-4 || Mathf.Abs(hunter.TargetPoint.x - start.x) < 1e-4,
                                  "all mixed: wherever the runner was when the hunter's turn came");
            }
        }

        [Test, Description("T-ACT-08 (ACT-13): breeding is no interaction of the mate action: after the act phase no egg exists; the breed phase then lays them")]
        public void EggsComeFromTheBreedPhase()
        {
            int laidAfterAct = -1;
            // A callback phase between the act and breed phases reads the egg count the act phase left behind.
            var w = New(name: "eggs").Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.LifeRules(40, 0))
                .Phase<ActPhase>().Phase<CallbackPhase>(p => p.OnRun = t => laidAfterAct = t.World.FindSpecies("prey").Counters.EggsLaid)
                .Phase<BreedPhase>().Phase<HatchPhase>().Build();
            var prey = w.FindSpecies("prey");
            foreach (var x in new[] { 10f, 10.5f })
            {
                var a = Place.Animal(prey, Place.At(x, 10));
                a.Age = 200; a.SetStat("energy", 90f); a.Choose("mate");
            }
            w.Advance(1);
            Assert.AreEqual(0, laidAfterAct, "the act phase lays no egg");
            Assert.Greater(prey.Counters.EggsLaid, 0, "the breed phase does (positive control)");
            Assert.AreEqual(prey.Counters.EggsLaid, prey.Counters.Births, "and the hatch phase hatches them");
        }

        [Test, Description("ANIM-35 (ANIM-36): babies born in a tick are aged by that tick's death phase: age 1 at the end of their birth tick")]
        public void BabiesAgeOnTheirBirthTick()
        {
            var w = New(name: "baby age").Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.LifeRules(40, 0)).ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => b.Policy = q => ScriptedBrain.Prefer(q.Species, "mate")).Build();
            var prey = w.FindSpecies("prey");
            foreach (var x in new[] { 10f, 10.5f })
            {
                var a = Place.Animal(prey, Place.At(x, 10));
                a.Age = 200; a.SetStat("energy", 90f);
            }
            w.Advance(1);
            var babies = prey.Animals.Where(a => a.Origin == "birth").ToList();
            Assert.IsNotEmpty(babies);
            Assert.IsTrue(babies.All(b => b.Age == 1 && b.BornTick == 0), string.Join(",", babies.Select(b => b.Age)));
        }
    }
}
