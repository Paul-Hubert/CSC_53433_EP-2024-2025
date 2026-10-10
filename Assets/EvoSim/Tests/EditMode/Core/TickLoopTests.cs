using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>The tick loop, phases and wait modes (TICK-01…07, SPACE-10…14, RAND-11, RAND-20).</summary>
    public class TickLoopTests : WorldFixture
    {
        [Test, Description("T-TICK-01 (TICK-01, TICK-07): phases run in hierarchy order every tick; a phase added or removed runs or vanishes without other changes")]
        public void PhasesRunInHierarchyOrder()
        {
            var b = New().Flat(10, 10).Phase<TracePhase>(p => p.Label = "A").Phase<TracePhase>(p => p.Label = "B").Phase<TracePhase>(p => p.Label = "C");
            var phases = b.Root.GetComponentsInChildren<TracePhase>(true);
            var log = phases[0].Log;
            foreach (var p in phases) p.Log = log;
            var w = b.Build();
            w.Advance(2);
            CollectionAssert.AreEqual(new[] { "0:A", "0:B", "0:C", "1:A", "1:B", "1:C" }, log);

            log.Clear();
            phases[1].enabled = false;                                   // removed: vanishes
            Assert.IsTrue(w.Initialize());
            w.Advance(1);
            CollectionAssert.AreEqual(new[] { "0:A", "0:C" }, log);

            log.Clear();
            phases[1].enabled = true;
            phases[2].transform.SetSiblingIndex(0);                      // reordered: the order follows the hierarchy
            Assert.IsTrue(w.Initialize());
            w.Advance(1);
            CollectionAssert.AreEqual(new[] { "0:C", "0:A", "0:B" }, log);
        }

        [Test, Description("T-TICK-02 (TICK-02): a phase that changes the world is seen only by the phases after it in the same tick")]
        public void ChangesAreSeenByLaterPhasesOnly()
        {
            var board = new Blackboard();
            var w = New().Flat(10, 10)
                .Phase<BlackboardPhase>(p => { p.Board = board; p.name = "before"; })
                .Phase<BlackboardPhase>(p => { p.Board = board; p.Writes = true; p.name = "writer"; })
                .Phase<BlackboardPhase>(p => { p.Board = board; p.name = "after"; })
                .Build();
            w.Advance(2);
            CollectionAssert.AreEqual(new[] { "0:before=0", "0:writer=0", "0:after=1", "1:before=1", "1:writer=1", "1:after=2" }, board.Seen);
        }

        [Test, Description("T-SPACE-07, EditMode part (SPACE-13, SPACE-14, TICK-03): a tick doesn't pass a waiting phase; Responsive and Freeze give the same hash")]
        public void WaitModesGiveTheSameHash()
        {
            string Run(WaitMode mode, out World w)
            {
                w = New(42, mode).Flat(10, 10)
                    .Phase<RandomEventPhase>()
                    .Phase<DelayedPhase>(p => { p.DelayCalls = 3; p.Every = 2; })
                    .Phase<TracePhase>()
                    .Configure(x => x.TickLimit = 20)
                    .Build();
                int guard = 0;
                while (w.State != RunState.Stopped && guard++ < 1000) w.Advance(5);
                Assert.AreEqual(20, w.Tick);
                return w.Events.Hash;
            }

            string responsive = Run(WaitMode.Responsive, out var wr);
            string freeze = Run(WaitMode.Freeze, out var wf);
            Assert.AreEqual(freeze, responsive, "RAND-11: the same hash whatever the wait mode");
            Assert.Greater(wr.AdvanceCalls, wf.AdvanceCalls, "Responsive returned early while waiting");
            Assert.Greater(wr.WaitCount, 0);

            // The tick never moved past the waiting phase before its answer: the trace after it ran once per tick, in order.
            var trace = wr.Phase<TracePhase>().Log;
            CollectionAssert.AreEqual(Enumerable.Range(0, 20).Select(t => t + ":TracePhase").ToArray(), trace.Take(20).ToArray());
            var delayed = wr.Phase<DelayedPhase>();
            CollectionAssert.AreEqual(Enumerable.Range(0, 20).ToArray(), delayed.RanAtTick.Take(20).ToArray(), "decisions use their own tick");
        }

        [Test, Description("Responsive waiting: Advance returns early and the same tick resumes at the waiting phase (SPACE-14, 20 §3.2)")]
        public void ResponsiveResumesTheSameTick()
        {
            var w = New(1, WaitMode.Responsive).Flat(10, 10)
                .Phase<TracePhase>(p => p.Label = "before")
                .Phase<DelayedPhase>(p => p.DelayCalls = 2)
                .Phase<TracePhase>(p => p.Label = "after")
                .Build();
            var before = w.Phases[0] as TracePhase;
            var after = w.Phases[2] as TracePhase;
            Assert.AreEqual(0, w.Advance(1));
            Assert.AreEqual(RunState.Waiting, w.State);
            Assert.AreSame(w.Phases[1], w.WaitingFor);
            CollectionAssert.AreEqual(new[] { "0:before" }, before.Log);
            Assert.AreEqual(0, w.Advance(1), "still waiting");
            Assert.AreEqual(1, w.Advance(1), "answers in: the tick completes");
            CollectionAssert.AreEqual(new[] { "0:before" }, before.Log, "the phase before is not run again");
            CollectionAssert.AreEqual(new[] { "0:after" }, after.Log);
            Assert.AreEqual(1, w.Tick);
        }

        [Test, Description("S00 (M1 done-when): an empty world runs 1 000 ticks in both wait modes with identical hashes")]
        public void EmptyWorldRunsAThousandTicks()
        {
            string Run(WaitMode mode)
            {
                var w = New(1234, mode).Flat(48, 48).Phase<RandomEventPhase>().Phase<DelayedPhase>(p => { p.DelayCalls = 1; p.Every = 4; })
                    .Configure(x => x.TickLimit = 1000).Build();
                int guard = 0;
                while (w.State != RunState.Stopped && guard++ < 10000) w.Advance(10);
                Assert.AreEqual(1000, w.Tick);
                return w.Events.Hash;
            }
            Assert.AreEqual(Run(WaitMode.Freeze), Run(WaitMode.Responsive));
        }

        [Test, Description("SPACE-11: the hash doesn't depend on how many ticks run per call")]
        public void TicksPerCallDontMatter()
        {
            string Run(int perCall)
            {
                var w = New(9).Flat(10, 10).Phase<RandomEventPhase>().Phase<DelayedPhase>(p => p.Every = 3)
                    .Configure(x => x.TickLimit = 60).Build();
                while (w.State != RunState.Stopped) w.Advance(perCall);
                return w.Events.Hash;
            }
            string one = Run(1);
            Assert.AreEqual(one, Run(3));
            Assert.AreEqual(one, Run(10));
        }

        [Test, Description("RAND-20: stop conditions stop at a tick boundary and call OnRunStopped")]
        public void StopConditions()
        {
            var w = New().Flat(10, 10).Phase<TracePhase>().Configure(x => x.TickLimit = 5).Build();
            Assert.AreEqual(5, w.Advance(100));
            Assert.AreEqual(RunState.Stopped, w.State);
            Assert.AreEqual("tick limit", w.StopReason);
            Assert.AreEqual(0, w.Advance(1), "a stopped world doesn't advance");

            var w2 = New(waitMode: WaitMode.Responsive).Flat(10, 10).Phase<DelayedPhase>(p => p.DelayCalls = 5).Build();
            w2.Advance(1);
            w2.RequestStop("button");
            Assert.AreEqual(RunState.Waiting, w2.State, "a stop asked inside a tick waits for the boundary");
            while (w2.State != RunState.Stopped) w2.Advance(1);
            Assert.AreEqual(1, w2.Tick);
            Assert.AreEqual("button", w2.StopReason);

            var dir = System.IO.Path.Combine(Application.temporaryCachePath, "evosim-stop-test");
            System.IO.Directory.CreateDirectory(dir);
            var stop = System.IO.Path.Combine(dir, "STOP");
            System.IO.File.WriteAllText(stop, "");
            try
            {
                var w3 = New().Flat(10, 10).Phase<TracePhase>().Configure(x => x.StopFile = stop).Build();
                Assert.AreEqual(0, w3.Advance(10));
                Assert.AreEqual("stop file", w3.StopReason);
            }
            finally { System.IO.File.Delete(stop); }
        }

        [Test, Description("RAND-20: an exception in a phase stops the run with its reason and surfaces")]
        public void ExceptionsStopTheRun()
        {
            var w = New().Flat(10, 10).Phase<ThrowingPhase>().Build();
            Assert.Throws<System.InvalidOperationException>(() => w.Advance(1));
            Assert.AreEqual(RunState.Stopped, w.State);
            StringAssert.Contains("exception in ThrowingPhase", w.StopReason);
        }

        [Test, Description("Run controls (02 §4): step, run N, pause")]
        public void RunControls()
        {
            var w = New().Flat(10, 10).Phase<TracePhase>().Build();
            Assert.AreEqual(RunState.Paused, w.State);
            w.Step();
            Assert.AreEqual(RunState.Stepping, w.State);
            w.RunFor(3);
            Assert.AreEqual(RunState.Running, w.State);
            w.Pause();
            Assert.AreEqual(RunState.Paused, w.State);
        }
    }
}
