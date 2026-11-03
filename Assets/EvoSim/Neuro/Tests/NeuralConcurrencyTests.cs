using System.IO;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// T-NEURO-08 (plan M16 D4, M16.8; SPACE-11, SPACE-14): the neural brain on its worker task beside a slow JEV, and the
    /// per-tick pools of the neural senses.
    /// </summary>
    /// <remarks>
    /// S32 (<c>Brains_2x2</c>) runs with every HTTP brain on a <see cref="FakeHttpTransport"/> that answers after a delay, and
    /// the answer cache in an empty temporary folder: no server is called and the real caches are untouched. The scene is
    /// opened additively and closed unsaved: close it in the editor first.
    /// </remarks>
    public class NeuralConcurrencyTests : WorldFixture
    {
        const int Ticks = 40;

        /// <summary>The ceiling of a tick without the brain in <see cref="TickTimeOnLab1Full"/>, in ms: the M16.8 measurement
        /// (6.7 ms) plus a margin. It guards against regressions; the 20 §9 budget of 2 ms is not met (status file).</summary>
        const double TickCeilingMs = 10.0;

        [Test, Category("T3"), Description("T-NEURO-08 (20 §9, M16.8): S32 L0 (Lab1_Full, the neural species at reference sizes beside the JEV species on the random brain) → the tick time without the brain, the brain's and the senses' reported; under the M16.8 ceiling")]
        public void TickTimeOnLab1Full()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/S32_Brains2x2.asset");
            string result = EvoSim.Batch.WithScene(scenario.scene, w =>
            {
                var problems = new System.Collections.Generic.List<string>();
                scenario.Apply(w, "L0-default", 1234, problems);
                Assert.IsEmpty(problems);
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                // Callbacks around ask-brains … choose-actions measure the brain's time, which the budget leaves out; the
                // sense phase is timed too, for the report.
                var brainClock = Clock(w.GetComponentInChildren<AskBrainsPhase>(true).transform, w.GetComponentInChildren<ChooseActionsPhase>(true).transform);
                var senseTransform = w.GetComponentInChildren<SensePhase>(true).transform;
                var senseClock = Clock(senseTransform, senseTransform);
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                w.Advance(300);                                                 // warm-up: pools and lists reach their size
                int animals = 0, neural = 0;
                foreach (var s in w.AllSpecies)
                {
                    animals += s.Animals.Count;
                    if (s.Brain is NeuralBrain) neural += s.Animals.Count;
                }
                brainClock.Reset();
                senseClock.Reset();
                const int measured = 300;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                w.Advance(measured);
                double all = clock.Elapsed.TotalMilliseconds / measured, brain = brainClock.Elapsed.TotalMilliseconds / measured;
                double sense = senseClock.Elapsed.TotalMilliseconds / measured;
                return $"{neural} neural of {animals} animals, {all:0.000} ms per tick: brain {brain:0.000} ms, senses {sense:0.000} ms, without the brain {all - brain:0.000} ms";
            });
            TestContext.WriteLine("T-NEURO-08 tick time: " + result);
            Debug.Log("T-NEURO-08 tick time: " + result);
            double without = double.Parse(result.Substring(result.LastIndexOf("brain ") + 6).Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Less(without, TickCeilingMs, "tick time without the brain");
        }

        /// <summary>A stopwatch that runs from just before the first phase to just after the last (two callback phases).</summary>
        static System.Diagnostics.Stopwatch Clock(Transform first, Transform last)
        {
            var watch = new System.Diagnostics.Stopwatch();
            var start = new GameObject("clock start").AddComponent<CallbackPhase>();
            start.transform.SetParent(first.parent, false);
            start.transform.SetSiblingIndex(first.GetSiblingIndex());
            var stop = new GameObject("clock stop").AddComponent<CallbackPhase>();
            stop.transform.SetParent(last.parent, false);
            stop.transform.SetSiblingIndex(last.GetSiblingIndex() + 1);
            start.OnRun = _ => watch.Start();
            stop.OnRun = _ => watch.Stop();
            return watch;
        }

        /// <summary>One S32 run on fake servers: the result line of <see cref="EvoSim.Batch.Run"/>.</summary>
        static string Run(WaitMode mode, int syncBelow, RunSpeed speed, string name)
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/S32_Brains2x2.asset");
            string cache = Path.Combine(Application.temporaryCachePath, "t-neuro-08-" + name);
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
            return EvoSim.Batch.Run(scenario, "default", 1, Ticks, w =>
            {
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                foreach (var c in w.GetComponentsInChildren<AnswerCache>(true)) c.SetFolder(cache);
                foreach (var h in w.GetComponentsInChildren<HttpBrain>(true))
                {
                    var fake = FakeHttpTransport.For(h);
                    Assert.IsNotNull(fake, h.GetType().Name + " has no fake server");
                    fake.DelayMs = 15;
                    h.Transport = fake;
                }
                foreach (var n in w.GetComponentsInChildren<NeuralBrain>(true)) n.SyncBelow = syncBelow;
                w.RunSpeed = speed;
            }, mode);
        }

        /// <summary>The 16 hex digits after <c>"hash "</c> in a result line.</summary>
        static string Hash(string line) => line.Substring(line.IndexOf("hash ") + 5, 16);

        [Test, Description("T-NEURO-08 (D4, SPACE-11, SPACE-14): S32 with a slow fake JEV → the tick completes; the neural brain inline or on its worker, Freeze or Responsive, Fast or RealTime → the same hash")]
        public void WorkerBesideSlowJev()
        {
            string inline = Run(WaitMode.Freeze, int.MaxValue, RunSpeed.Fast, "inline");
            Debug.Log("T-NEURO-08 " + inline);
            StringAssert.DoesNotStartWith("FAILED", inline);
            StringAssert.Contains($"{Ticks} ticks", inline, "every tick completed");
            StringAssert.DoesNotContain("model calls 0 decisions", inline, "the fake JEV answered");
            foreach (var (mode, speed, name) in new[]
                     {
                         (WaitMode.Freeze, RunSpeed.Fast, "worker"),
                         (WaitMode.Responsive, RunSpeed.Fast, "responsive"),
                         (WaitMode.Freeze, RunSpeed.RealTime, "realtime"),
                     })
            {
                string line = Run(mode, 0, speed, name);
                Debug.Log("T-NEURO-08 " + line);
                StringAssert.Contains($"{Ticks} ticks", line, name);
                Assert.AreEqual(Hash(inline), Hash(line), name);
            }
        }

        [Test, Description("T-NEURO-08 (20 §9): a tick pool keeps one item per animal per tick and hands the last tick's items out again")]
        public void TickPoolReuses()
        {
            var pool = new TickPool<float[]>(() => new float[3]);
            Assert.IsTrue(pool.Get(1, 7, out var a));
            Assert.IsFalse(pool.Get(1, 7, out var again), "the same animal in the same tick");
            Assert.AreSame(a, again);
            Assert.IsTrue(pool.Get(1, 8, out var b));
            Assert.AreNotSame(a, b);
            Assert.IsTrue(pool.Get(2, 9, out var c), "a new tick");
            Assert.IsTrue(pool.Get(2, 7, out var d));
            Assert.AreEqual(2, pool.Made, "the second tick reused the first tick's items");
            Assert.IsTrue((c == a || c == b) && (d == a || d == b) && c != d);
        }
    }
}
