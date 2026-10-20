using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks overlay actions, done in passing (07 ACT-07, 08 DEC-20): the second draw of the carrier, the act phase's two
    /// interactions, the overlay's end, breeding under an overlay (REPRO-03), the prompt rule (PROMPT-03) and determinism
    /// with an overlay (RAND-10). The validation codes V-19 and V-27 are in <see cref="ValidationCatalogueTests"/>.
    /// </summary>
    /// <remarks>
    /// The overlay is the probe <see cref="OverlayProbeAction"/>: it plans a walk 10 m east, which must be ignored, and a
    /// custom interaction that counts its applications. The act-phase tests give actions by hand with
    /// <see cref="Lab.Choose(Animal, string, string)"/>, in a Lab ecology without decisions.
    /// </remarks>
    public class OverlayTests : WorldFixture
    {
        /// <summary>
        /// A Lab ecology 40 m square without cover or food, the prey's actions plus the overlay probe <c>mark</c>, and only
        /// the act, death and environment phases: no brain is asked, animals keep the actions given by hand.
        /// </summary>
        /// <param name="order">The act order of the act phase.</param>
        /// <param name="ticks">The probe's <see cref="AnimalAction.OverlayTicks"/>.</param>
        /// <param name="reach">The probe's interaction reach, in meters.</param>
        /// <returns>The built, initialised world.</returns>
        World Eco(ActOrder order = ActOrder.SpeciesInTurn, int ticks = 1, float reach = float.PositiveInfinity)
        {
            return New(3).Ecology(40f, 0f, 0f, regrow: 0f,
                    prey: s => s.Action<OverlayProbeAction>("mark", m => { m.Ticks = ticks; m.Reach = reach; }))
                .Phase<ActPhase>(p => p.Order = order).Phase<DeathPhase>().Phase<EnvironmentPhase>()
                .Build();
        }

        /// <summary>The overlay probe of the world's prey.</summary>
        static OverlayProbeAction Mark(World w) => (OverlayProbeAction)w.FindSpecies("prey").FindAction("mark");

        [Test, Description("T-ACT-11, ACT-07, DEC-20: with a scripted row, P(overlay) = p_overlay and the carrier follows the rest of the row, renormalised (4 s.e.)")]
        public void CarrierIsDrawnFromTheRest()
        {
            var w = New(11).Flat(50, 50).Phase<SensePhase>().Phase<AskBrainsPhase>().Phase<ChooseActionsPhase>()
                .DefaultBrain<ScriptedBrain>(b => b.Policy = q => new[] { 0.3f, 0.2f, 0.5f })
                .Species("prey", s => s.Population(200).Module<KinematicLocomotion>()
                    .Action<OverlayProbeAction>("mark").Action<RestAction>("rest").Action<RestAction>("wait"))
                .Build();
            var animals = w.FindSpecies("prey").Animals;
            long decisions = 0, overlays = 0, rest = 0, restAlone = 0;
            for (int t = 0; t < 120; t++)
            {
                w.Advance(1);
                foreach (var a in animals)
                {
                    if (a.LastDecisionTick != w.Tick - 1) continue;
                    decisions++;
                    Assert.AreNotEqual(0, a.Action, "the carrier is never the overlay when the rest of the row is above 0");
                    if (a.Overlay == 0) { overlays++; if (a.Action == 1) rest++; }
                    else
                    {
                        Assert.AreEqual(-1, a.Overlay);
                        if (a.Action == 1) restAlone++;
                    }
                }
            }
            Assert.Greater(decisions, 5000);
            Stat.Binomial(overlays, decisions, 0.3, 4, "P(overlay)");
            Stat.Binomial(rest, overlays, 0.2 / 0.7, 4, "P(rest | overlay)");
            Stat.Binomial(restAlone, decisions - overlays, 0.2 / 0.7, 4, "P(rest | no overlay)");
            var counts = w.FindSpecies("prey").Counters.ActionCounts;
            Assert.AreEqual(decisions + overlays, counts.Sum(), "a drawn overlay counts for itself and its carrier");
        }

        [Test, Description("T-ACT-11, ACT-07: a row with nothing but the overlay leaves no carrier: the action is the overlay, the animal stays and the overlay still acts")]
        public void NoCarrierStays()
        {
            var w = New(5).Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.Action<OverlayProbeAction>("mark")).ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => b.Policy = q => q.Species.Actions.Select(x => x.Overlay ? 1f : 0f).ToArray())
                .Build();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(20.5f, 20.5f));
            w.Advance(1);
            Assert.AreEqual(Mark(w).Index, a.Action, "the action stays the overlay");
            Assert.AreEqual("stay", a.ActionLabel, "the overlay ran its tick and ended");
            Assert.AreEqual(Place.At(20.5f, 20.5f), a.Position, "no carrier: it stays (its overlay's walk is ignored)");
            Assert.AreEqual(1, Mark(w).Applied);
        }

        [Test, Description("T-ACT-11, ACT-07: the overlay's intent is ignored, the carrier moves the animal, and both interactions apply, carrier first, in every act order")]
        public void BothInteractionsApply([Values] ActOrder order)
        {
            var w = Eco(order);
            var prey = w.FindSpecies("prey");
            var food = w.Service<FoodGrid>();
            Place.Food(food, Place.At(10.5f, 10.5f));
            var eater = Place.Animal(prey, Place.At(10.5f, 10.5f));
            eater.Choose("eat", "mark");
            var runner = Place.Animal(prey, Place.At(30.5f, 30.5f));
            Place.Animal(w.FindSpecies("predator"), Place.At(32.5f, 30.5f)).Choose("rest");
            runner.Choose("flee", "mark");
            Assert.AreEqual("flee + mark", runner.ActionLabel);
            w.Advance(1);
            Assert.AreEqual(1, eater.Meals, "the carrier's interaction (graze)");
            Assert.Less(runner.Position.x, 30.5f, "the carrier (flee) moved it west, not the overlay's walk east");
            Assert.AreEqual(2, Mark(w).Applied, "the overlay's interaction, for both animals");
            Assert.AreEqual(-1, eater.Overlay, "one tick, then it ends");
            Assert.AreEqual("eat", eater.ActionLabel);
            w.Advance(1);
            Assert.AreEqual(2, Mark(w).Applied, "no more after its tick");
        }

        [Test, Description("T-ACT-11, ACT-07: the overlay acts for OverlayTicks ticks; 0 = until the next decision")]
        public void OverlayTicks([Values(0, 1, 3)] int ticks)
        {
            var w = Eco(ticks: ticks);
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10.5f));
            a.Choose("rest", "mark");
            w.Advance(5);
            Assert.AreEqual(ticks == 0 ? 5 : ticks, Mark(w).Applied);
            Assert.AreEqual(ticks == 0 ? Mark(w).Index : -1, a.Overlay);
        }

        [Test, Description("T-ACT-11, ACT-07, ACT-10: the overlay's reach is measured from the point its ignored intent named")]
        public void ReachFromTheOverlaysIntent()
        {
            var w = Eco(reach: 1f);
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.5f, 10.5f));
            a.Choose("rest", "mark");
            w.Advance(1);
            Assert.AreEqual(0, Mark(w).Applied, "the probe's intent names a point 10 m east, out of its 1 m reach");
        }

        [Test, Description("T-ACT-11, ACT-07, ANIM-30: a busy animal ends its overlay; so does death")]
        public void BusyAndDeathEndTheOverlay()
        {
            var w = Eco(ticks: 0);
            var prey = w.FindSpecies("prey");
            var busy = Place.Animal(prey, Place.At(10.5f, 10.5f));
            busy.Choose("rest", "mark");
            busy.BusyTicks = 2;
            w.Advance(1);
            Assert.AreEqual(0, Mark(w).Applied, "busy animals don't act");
            Assert.AreEqual(-1, busy.Overlay);
            var dead = Place.Animal(prey, Place.At(20.5f, 20.5f));
            dead.Choose("rest", "mark");
            w.RecordDeath(dead, "test");
            Assert.AreEqual(-1, dead.Overlay);
            Assert.IsNull(dead.CurrentOverlay);
        }

        [Test, Description("T-ACT-11, REPRO-03, ACT-07: a mate carrier under an overlay breeds once per period, with the same pairing as without it")]
        public void MateCarrierBreedsOncePerPeriod()
        {
            string Pairing(bool overlay)
            {
                var w = New(1234).Flat(40, 40).Food(0f, 0f).Service<EggSystem>("Eggs")
                    .Phase<ActPhase>().Phase<BreedPhase>().Phase<HatchPhase>().Phase<DeathPhase>().Phase<EnvironmentPhase>()
                    .Species("prey", s => s.PreyBody().PreySenses().PreyActionSet()
                        .Action<OverlayProbeAction>("mark", m => m.Ticks = 0).LifeRules(400, 0, 0))
                    .Build();
                var s = w.AllSpecies[0];
                var adults = new[] { 10f, 10.4f, 10.8f }.Select(x =>
                {
                    var a = Place.Animal(s, Place.At(x, 10));
                    a.Age = 200;
                    a.SetStat("energy", 90f);
                    a.SetStat("stamina", 0f);                                   // nobody moves: only the reach matters
                    if (overlay) a.Choose("mate", "mark"); else a.Choose("mate");
                    return a;
                }).ToArray();
                w.Advance(3);                                                   // one decision period, no new decision
                Assert.AreEqual(1, adults.Count(x => x.Offspring == 0), "two bred once, one found no free partner");
                if (overlay) Assert.AreEqual(9, Mark(w).Applied, "the overlay acted every tick beside the mate carrier");
                return string.Join(",", adults.Select(x => x.Offspring > 0));
            }
            Assert.AreEqual(Pairing(false), Pairing(true));
        }

        [Test, Description("T-ACT-11, PROMPT-03, ACT-07: a species with an overlay gets one engine rule line naming it; one without gets none")]
        public void PromptRule()
        {
            var w = Eco();
            StringAssert.Contains("Some actions are done in passing (mark): the animal also does its next most likely choice.\n",
                                  w.FindSpecies("prey").Prompt.Text);
            StringAssert.DoesNotContain("done in passing", w.FindSpecies("predator").Prompt.Text);
        }

        [Test, Description("T-ACT-11, RAND-10, ACT-07: with an overlay in the reference phases, the same seed gives the same events hash, and the overlay is drawn and acts")]
        public void SameSeedSameHashWithAnOverlay()
        {
            OverlayProbeAction probe = null;
            string Run(int seed)
            {
                var w = New(seed).Ecology(40f, 0.2f, 0.1f, prey: s => s.Population(16).Action<OverlayProbeAction>("mark", m => probe = m),
                                          predator: s => s.Population(3))
                    .ReferencePhases().DefaultBrain<RandomBrain>().Build();
                w.Advance(120);
                var prey = w.FindSpecies("prey");
                Assert.Greater(prey.Counters.ActionCounts[probe.Index], 0, "the overlay was drawn");
                Assert.Greater(probe.Applied, 0, "and acted");
                return w.Events.Hash + "/" + probe.Applied;
            }
            Assert.AreEqual(Run(1234), Run(1234));
        }
    }
}
