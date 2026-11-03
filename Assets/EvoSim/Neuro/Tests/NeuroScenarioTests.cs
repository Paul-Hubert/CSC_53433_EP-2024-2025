using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// T-NEURO-15 (T2, plan M16 §6.1, §8): S29 asexual and sexual for 5 000 ticks and the L0 versions of S30 and S31 repeat
    /// their hash with the same seed; evolving neural prey eat more per animal-tick than under the no-mutation control.
    /// </summary>
    /// <remarks>
    /// The scenes are opened additively and closed unsaved: close them in the editor first. No model is called (the
    /// scenarios' L0 variants use the random brain and switch the JEV species' mutation off). The S30/S31 L0 runs are shorter
    /// (2 000 ticks, their scenario length).
    /// </remarks>
    public class NeuroScenarioTests
    {
        static ScenarioAsset Load(string name) =>
            AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/" + name + ".asset");

        /// <summary>One run of a scenario variant without files: the result line of <see cref="EvoSim.Batch.Run"/>.</summary>
        static string Run(string scenario, string variant, int seed, int ticks) =>
            EvoSim.Batch.Run(Load(scenario), variant, seed, ticks, w =>
            {
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
            });

        /// <summary>The 16 hex digits after <c>"hash "</c> in a result line.</summary>
        static string Hash(string line) => line.Substring(line.IndexOf("hash ") + 5, 16);

        /// <summary>Two runs of a variant, the same seed: no failure, no model call, the same hash.</summary>
        static void Twice(string scenario, string variant, int ticks)
        {
            string a = Run(scenario, variant, 1234, ticks), b = Run(scenario, variant, 1234, ticks);
            Debug.Log("T-NEURO-15 " + a);
            StringAssert.DoesNotStartWith("FAILED", a);
            StringAssert.Contains($"{ticks} ticks", a, "ran to the end");
            StringAssert.Contains("model calls 0 decisions + 0 mutations", a);
            Assert.AreEqual(Hash(a), Hash(b), $"{scenario} {variant}");
        }

        [Test, Description("T-NEURO-15 (T2): S29 asexual for 5 000 ticks, twice with the same seed → the same hash, no model call")]
        public void S29AsexualRepeats() => Twice("S29_NeuroLab", "asexual", 5000);

        [Test, Description("T-NEURO-15 (T2): S29 sexual for 5 000 ticks, twice with the same seed → the same hash, no model call")]
        public void S29SexualRepeats() => Twice("S29_NeuroLab", "sexual", 5000);

        [Test, Description("T-NEURO-15 (T2): S30 L0 (JEV prey on the random brain, neural predators), asexual and sexual → the same hash twice")]
        public void S30L0Repeats()
        {
            Twice("S30_JevPreyNeuroPredator", "L0-asexual", 2000);
            Twice("S30_JevPreyNeuroPredator", "L0-sexual", 2000);
        }

        [Test, Description("T-NEURO-15 (T2): S31 L0 (neural prey, JEV predators on the random brain), asexual and sexual → the same hash twice")]
        public void S31L0Repeats()
        {
            Twice("S31_NeuroPreyJevPredator", "L0-asexual", 2000);
            Twice("S31_NeuroPreyJevPredator", "L0-sexual", 2000);
        }

        /// <summary>
        /// The neural prey's meals per animal-tick over the last <paramref name="window"/> ticks of a 5 000-tick S29 run
        /// (<see cref="NeuroStats"/>' <c>nn_meals_per_tick</c>, its window widened before the run).
        /// </summary>
        static double MealsPerTick(string variant, int seed, int window)
        {
            var scenario = Load("S29_NeuroLab");
            string line = EvoSim.Batch.WithScene(scenario.scene, w =>
            {
                var problems = new System.Collections.Generic.List<string>();
                scenario.Apply(w, variant, seed, problems);
                if (problems.Count > 0) return "FAILED: " + string.Join("; ", problems);
                w.TickLimit = 5000;
                w.StartOnPlay = false;
                w.WaitMode = WaitMode.Freeze;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                foreach (var s in w.GetComponentsInChildren<NeuroStats>(true)) s.Configure(s.LoopTicks, window);
                if (!w.Initialize()) return "FAILED: " + w.LastReport;
                while (w.State != RunState.Stopped) w.Advance(100);
                var prey = w.FindSpecies(NeuralTemplates.PreyId);
                return prey.Module<NeuroStats>().StatValue("nn_meals_per_tick").ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                       + $" at tick {w.Tick}, {prey.Animals.Count} alive";
            });
            StringAssert.DoesNotStartWith("FAILED", line);
            Debug.Log($"T-NEURO-15 S29 {variant} seed {seed}: nn_meals_per_tick {line}");
            return double.Parse(line.Substring(0, line.IndexOf(' ')), System.Globalization.CultureInfo.InvariantCulture);
        }

        [Test, Description("T-NEURO-15 (T2, plan §8): evolving neural prey (S29 asexual) eat more per animal-tick over the last 1 000 of 5 000 ticks than under the no-mutation control, in each of 2 seeds")]
        public void EvolvedPreyEatMore()
        {
            foreach (int seed in new[] { 1234, 7 })
            {
                double evolving = MealsPerTick("asexual", seed, 1000);
                double frozen = MealsPerTick("no-mutation", seed, 1000);
                Debug.Log($"T-NEURO-15 seed {seed}: meals per animal-tick, evolving {evolving:0.0000}, no mutation {frozen:0.0000}");
                Assert.Greater(evolving, frozen, $"seed {seed}");
            }
        }
    }
}
