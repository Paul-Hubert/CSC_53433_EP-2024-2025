using System.Collections;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks the tick loop driven by <c>FixedUpdate</c> in Play mode (tier T2; SPACE-10…14, TICK-03, RAND-10…13;
    /// T-SPACE-05, T-SPACE-07, T-RAND-05): the hash does not depend on ticks per call, on the fixed time step or on the wait
    /// mode, and follows the seed.
    /// </summary>
    /// <remarks>
    /// The worlds hold one species with a <see cref="ProbeAction"/>, a <see cref="RandomEventPhase"/> for the hash, and a
    /// <see cref="DelayedPhase"/> that waits two calls every 4 ticks, as a slow brain would. The tests change
    /// <c>Time.fixedDeltaTime</c>; <see cref="SaveTime"/> and <see cref="RestoreTime"/> put it back.
    /// </remarks>
    public class TickLoopPlayTests : WorldFixture
    {
        /// <summary>The fixed time step before the test, in seconds.</summary>
        float savedFixedDelta;

        /// <summary>Remembers the fixed time step before each test.</summary>
        [SetUp] public void SaveTime() => savedFixedDelta = Time.fixedDeltaTime;
        /// <summary>Puts the fixed time step back after each test.</summary>
        [TearDown] public void RestoreTime() => Time.fixedDeltaTime = savedFixedDelta;

        /// <summary>
        /// A small world started with <c>Play()</c>: one species of 6 with a probe action, random events every tick, and a
        /// phase that waits 2 calls every 4 ticks.
        /// </summary>
        /// <param name="seed">The world seed.</param>
        /// <param name="mode">How the world waits for the delayed phase.</param>
        /// <param name="ticksPerCall">Ticks run per <c>FixedUpdate</c>.</param>
        /// <param name="limit">The tick limit.</param>
        /// <returns>The running world.</returns>
        World Make(int seed, WaitMode mode, int ticksPerCall, int limit)
        {
            var w = New(seed, mode).Flat(20, 20)
                .Phase<RandomEventPhase>()
                .Phase<DelayedPhase>(p => { p.DelayCalls = 2; p.Every = 4; })
                .Species("prey", s => s.Population(6).Action<ProbeAction>(founders: new[] { "Eat.", "Rest.", "Run." }))
                .Configure(x => { x.TicksPerFixedUpdate = ticksPerCall; x.TickLimit = limit; })
                .Build();
            w.Play();
            return w;
        }

        /// <summary>Waits fixed updates until the world stops at its tick limit (5 000 at most), then checks that it did.</summary>
        IEnumerator RunToEnd(World w)
        {
            int guard = 0;
            while (w.State != RunState.Stopped && guard++ < 5000) yield return new WaitForFixedUpdate();
            Assert.AreEqual(RunState.Stopped, w.State, "the world reached its tick limit");
        }

        [UnityTest, Description("T-SPACE-05 (SPACE-10, SPACE-11): 1, 3 and 10 ticks per call, fixedDeltaTime 0.02 and 0.005 → identical hashes")]
        public IEnumerator TicksPerCallAndFrameRateDontMatter()
        {
            var hashes = new System.Collections.Generic.List<string>();
            foreach (float dt in new[] { 0.02f, 0.005f })
            {
                Time.fixedDeltaTime = dt;
                foreach (int perCall in new[] { 1, 3, 10 })
                {
                    var w = Make(1234, WaitMode.Freeze, perCall, 60);
                    yield return RunToEnd(w);
                    Assert.AreEqual(60, w.Tick);
                    hashes.Add(w.Events.Hash);
                }
            }
            foreach (var h in hashes) Assert.AreEqual(hashes[0], h);
        }

        [UnityTest, Description("T-SPACE-07, Play mode part (SPACE-13, SPACE-14, TICK-03): answers 2 FixedUpdates late, Responsive and Freeze → same hash")]
        public IEnumerator WaitModesInPlayMode()
        {
            var responsive = Make(7, WaitMode.Responsive, 1, 40);
            var freeze = Make(7, WaitMode.Freeze, 1, 40);
            yield return RunToEnd(responsive);
            yield return RunToEnd(freeze);
            Assert.AreEqual(freeze.Events.Hash, responsive.Events.Hash);
            Assert.Greater(responsive.WaitCount, 0, "the Responsive world did wait");
        }

        [UnityTest, Description("T-RAND-05 (RAND-10, RAND-11, RAND-13, CORE-09): same seed twice → same hash; another seed → another hash")]
        public IEnumerator SameSeedSameHashInPlayMode()
        {
            var a = Make(1234, WaitMode.Responsive, 2, 50);
            var b = Make(1234, WaitMode.Responsive, 2, 50);
            var c = Make(42, WaitMode.Responsive, 2, 50);
            yield return RunToEnd(a);
            yield return RunToEnd(b);
            yield return RunToEnd(c);
            Assert.AreEqual(a.Events.Hash, b.Events.Hash);
            Assert.AreNotEqual(a.Events.Hash, c.Events.Hash);
        }
    }
}
