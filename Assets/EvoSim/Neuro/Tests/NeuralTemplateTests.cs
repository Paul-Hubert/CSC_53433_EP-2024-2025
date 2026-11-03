using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// T-NEURO-12 (plan M16 D7, §5.7, V-78, V-79): the neural templates built against the reference species of Lab1_Small,
    /// in a 2×2 world where both predators eat both preys, and <see cref="NeuroStats"/>.
    /// </summary>
    /// <remarks>The scene is opened additively and closed unsaved (<see cref="EvoSim.Batch.WithScene"/>): close it in the editor first.</remarks>
    public class NeuralTemplateTests : WorldFixture
    {
        const string Lab1Small = "Assets/EvoSim/Scenes/Lab1_Small.unity";

        /// <summary>
        /// Opens Lab1_Small, adds the two neural species, lets <paramref name="edit"/> change the world, validates, and
        /// returns <paramref name="check"/>'s result (or the validation's errors).
        /// </summary>
        static string With2x2(System.Action<World, Species, Species> edit, System.Func<World, ValidationReport, string> check, bool feedBoth = true)
        {
            return EvoSim.Batch.WithScene(Lab1Small, w =>
            {
                var prey = w.GetComponentsInChildren<Species>(true).Single(s => s.RequestedId == "prey");
                var predator = w.GetComponentsInChildren<Species>(true).Single(s => s.RequestedId == "predator");
                var nnPrey = NeuralTemplates.Prey(prey);
                var nnPredator = NeuralTemplates.Predator(predator);
                if (feedBoth) NeuralTemplates.Retarget(predator.GetComponentInChildren<Diet>(true), "prey", NeuralTemplates.PreyId, keep: true);
                NeuralTemplates.Retarget(nnPredator.GetComponentInChildren<Diet>(true), "prey", NeuralTemplates.PreyId, keep: true);
                edit?.Invoke(w, nnPrey, nnPredator);
                var report = new ValidationReport();
                w.Prepare(report);
                return check(w, report);
            });
        }

        static string Lines(ValidationReport r, string id) =>
            string.Join("\n", r.Messages.Where(m => m.Id == id).Select(m => m.Text));

        [Test, Description("T-NEURO-12 (D7): NeuroPrey and NeuroPredator built against the reference species, both predators eating both preys → no V-78, no V-79, no error")]
        public void TemplatesMatchTheReference()
        {
            string result = With2x2(null, (w, r) =>
            {
                if (r.HasErrors) return r.ToString();
                if (r.Has("V-78") || r.Has("V-79")) return Lines(r, "V-78") + Lines(r, "V-79");
                var s = w.FindSpecies(NeuralTemplates.PreyId);
                var p = w.FindSpecies(NeuralTemplates.PredatorId);
                Assert.AreEqual(NeuralTemplates.ActionNames, s.Actions.Select(a => a.Name).ToArray(), "left, right, walk, run, stop");
                Assert.IsInstanceOf<NeuralBrain>(s.Brain);
                Assert.AreSame(s.Brain, p.Brain, "one neural brain service");
                Assert.AreEqual(1, s.Module<DecisionSchedule>().Period(w), "a decision every tick");
                Assert.AreEqual(1, s.Genes.Count, "one gene: the network");
                Assert.IsNotNull(s.Module<NeuralMutation>());
                Assert.IsNotNull(s.Module<NeuronCrossover>());
                Assert.IsNull(s.Module<SearchRule>(), "no search rule: the neural actions have no target");
                Assert.AreEqual(NeuralContacts.Graze, ((NeuralAction)s.Actions[2]).Contacts);
                Assert.AreEqual(NeuralContacts.Strike | NeuralContacts.Scavenge, ((NeuralAction)p.Actions[2]).Contacts);
                Assert.AreEqual(RayFanSense.PredatorKinds, p.Module<RayFanSense>().Kinds.ToArray());
                CollectionAssert.AreEquivalent(new[] { "prey:prey-nn", "predator:predator-nn" },
                                               w.GetComponentInChildren<MatchedPairs>().Pairs.Select(x => x.jev + ":" + x.neural));
                return "ok";
            });
            Assert.AreEqual("ok", result);
        }

        [Test, Description("T-NEURO-12 (V-78): the neural prey's run speed changed → V-78 names it")]
        public void RunSpeedIsNamed()
        {
            string result = With2x2((w, prey, predator) => prey.GetComponentInChildren<KinematicLocomotion>(true).SetSpeeds(1f, 2f),
                                    (w, r) => Lines(r, "V-78"));
            StringAssert.Contains("runSpeed 1 ≠ 2", result);
            StringAssert.Contains("'neural prey' differs from its match 'prey'", result);
        }

        [Test, Description("T-NEURO-12 (V-79): the JEV predator's diet without prey-nn → V-79 names the diet; the cover hides the two alike")]
        public void DietWithoutTheNeuralPrey()
        {
            string result = With2x2(null, (w, r) => Lines(r, "V-79"), feedBoth: false);
            StringAssert.Contains("the diet of 'predator' eats 'prey' but not 'neural prey'", result);
            StringAssert.Contains("the diet of 'predator' scavenges 'prey' but not 'neural prey'", result);
            StringAssert.Contains("hides 'prey' from 'predator' but not 'neural prey'", result);
        }

        [Test, Description("V-78: a pair that names no species of the world is reported")]
        public void PairWithoutSpecies()
        {
            string result = With2x2((w, prey, predator) => NeuralTemplates.PairsOf(w).Add("wolf", "wolf-nn"), (w, r) => Lines(r, "V-78"));
            StringAssert.Contains("'wolf' names no species", result);
        }

        [Test, Description("V-73, V-74: a sexual neural species without NeuronCrossover warns; one deciding every 4 ticks is reported as info")]
        public void CrossoverAndRate()
        {
            string result = With2x2((w, prey, predator) =>
            {
                Object.DestroyImmediate(prey.GetComponentInChildren<NeuronCrossover>(true));
                predator.GetComponentInChildren<DecisionSchedule>(true).Configure(4, true);
            }, (w, r) => Lines(r, "V-73") + "|" + Lines(r, "V-74"));
            StringAssert.Contains("'neural prey' mates in pairs without a NeuronCrossover", result);
            StringAssert.Contains("'neural predator' decides every 4 ticks", result);
        }

        // ---- NeuroStats ----

        [Test, Description("NeuroStats (plan §5.6): a prey walking straight for 52 ticks → speed 1 m/tick, the walk share 1, and a loop after 50 ticks")]
        public void NeuroStatsColumns()
        {
            var w = New(4, name: "stats").Flat(80, 80).Food(0f, 0f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => { b.ReadsText = false; b.TakesAttachments = true; b.Policy = q => new[] { 0f, 0f, 1f, 0f, 0f }; })
                .Species("prey", s =>
                {
                    s.PreyBody().Sense<InteroceptionSense>().Module<HungerClock>().Module<NeuroStats>();
                    s.Get<MatingRule>().Configure(500f, 0f);
                    s.Action<TurnAction>("left", t => t.Configure(false), withGene: false)
                     .Action<TurnAction>("right", t => t.Configure(true), withGene: false)
                     .Action<ForwardAction>("walk", f => f.Configure(false), withGene: false)
                     .Action<ForwardAction>("run", f => f.Configure(true), withGene: false)
                     .Action<StopAction>("stop", withGene: false)
                     .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { 4 }, 8); }, "Brain")
                     .Population(0);
                })
                .Build();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(5f, 40f), heading: 90f);
            var stats = w.FindSpecies("prey").Module<NeuroStats>();
            a.SetStat("energy", 100f);                                                  // enough for 52 ticks of walking
            w.Advance(49);
            Assert.AreEqual(0.0, stats.StatValue("nn_loops"), "not yet 50 ticks of walk");
            w.Advance(3);
            Assert.AreEqual(1.0, stats.StatValue("nn_loops"), "50 ticks of the same action");
            Assert.AreEqual(1.0, stats.StatValue("nn_share_walk"));
            Assert.AreEqual(0.0, stats.StatValue("nn_share_left"));
            Assert.AreEqual(1.0, stats.StatValue("nn_speed"), 0.03, "1 m a tick, the first tick (no previous position) aside");
            Assert.AreEqual(0.0, stats.StatValue("nn_meals_per_tick"), "no food");
            Assert.That(stats.StatColumns.ToList(), Is.EqualTo(new List<string>
                { "nn_speed", "nn_meals_per_tick", "nn_meals_per_m", "nn_loops", "nn_share_left", "nn_share_right", "nn_share_walk", "nn_share_run", "nn_share_stop" }));
            Assert.IsFalse(a.IsGone);
        }
    }
}
