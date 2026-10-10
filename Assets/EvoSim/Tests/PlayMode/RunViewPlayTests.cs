using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>Watching a run (07 real-time test): the overlay's numbers, RealTime pacing, the camera, the launcher's hook.</summary>
    public class RunViewPlayTests : WorldFixture
    {
        readonly List<GameObject> extras = new List<GameObject>();

        [TearDown]
        public void DestroyExtras()
        {
            foreach (var go in extras) if (go != null) Object.DestroyImmediate(go);
            extras.Clear();
        }

        WorldBuilder Lab(int seed, string name, WaitMode mode = WaitMode.Freeze)
        {
            var b = New(seed, mode, name).Lab1(prey: 20, predators: 4);
            b.Root.transform.Find("prey").gameObject.AddComponent<Body>().Configure(null, Color.green, 0.6f);
            b.Root.transform.Find("predator").gameObject.AddComponent<Body>().Configure(null, Color.red, 0.9f);
            return b;
        }

        T Add<T>(World w) where T : Component
        {
            var go = new GameObject(typeof(T).Name + " (test)");
            extras.Add(go);
            if (typeof(T) == typeof(WorldCamera)) go.AddComponent<Camera>();
            return go.AddComponent<T>();
        }

        [UnityTest, Description("07 §1 (OUT-02): the overlay's numbers are the run's: per species the living animals, and births and deaths equal the birth and death events; mutations ok equal the mutations the birth events list; the tick and state are the World's")]
        public IEnumerator OverlayShowsTheRunsNumbers()
        {
            var w = Lab(21, "overlay").Configure(x => { x.TickLimit = 500; x.TicksPerFixedUpdate = 5; }).Build();
            w.Events.Lines = new List<string>();
            var overlay = Add<RunOverlay>(w);
            overlay.World = w;
            w.Play();
            var problems = new List<string>();
            int compared = 0;
            float until = Time.realtimeSinceStartup + 120f;
            while (w.State != RunState.Stopped && Time.realtimeSinceStartup < until)
            {
                yield return null;
                var n = overlay.Numbers;
                if (n.Tick != w.Tick || w.State == RunState.Stopped) continue;            // a tick ran after the overlay read
                compared++;
                var events = w.Events.Lines.Select(JObject.Parse).ToList();
                foreach (var s in w.AllSpecies)
                {
                    var line = n.Species.Single(x => x.Name == s.DisplayName);
                    int living = s.Animals.Count(a => !a.IsGone);
                    int births = events.Count(e => (string)e["kind"] == "birth" && (string)e["species"] == s.Id);
                    int deaths = events.Count(e => (string)e["kind"] == "death" && (string)e["species"] == s.Id);
                    if (line.Living != living) problems.Add($"tick {w.Tick} {s.Id}: {line.Living} living shown, {living} alive");
                    if (line.Births != births) problems.Add($"tick {w.Tick} {s.Id}: {line.Births} births shown, {births} birth events");
                    if (line.Deaths != deaths) problems.Add($"tick {w.Tick} {s.Id}: {line.Deaths} deaths shown, {deaths} death events");
                }
                int mutated = events.Where(e => (string)e["kind"] == "birth").Sum(e => ((JArray)e["mutations"]).Count);
                if (n.MutationsOk != mutated) problems.Add($"tick {w.Tick}: {n.MutationsOk} mutations shown, {mutated} in the birth events");
                if (n.State != w.State) problems.Add($"tick {w.Tick}: state {n.State} shown, {w.State}");
            }
            Assert.AreEqual(500, w.Tick);
            Assert.Greater(compared, 10, "frames where the overlay was compared with the events");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(10)));
            var last = overlay.Numbers.Read(w);
            Assert.Greater(last.Species.Sum(x => x.Births), 0, "births happened, so the comparison meant something");
            Assert.Greater(last.Species.Sum(x => x.Deaths), 0, "deaths too");
            Assert.Greater(last.MutationsOk, 0, "and mutations");
            StringAssert.Contains("Tick 500", overlay.RunText());
        }

        [UnityTest, Description("07 §5 (SPACE-14, SPACE-12): at RealTime speed the views move between ticks (the interpolation takes values between 0 and 1), at most a tenth of a second of ticks runs in one frame, and the hash equals a Freeze run's")]
        public IEnumerator RealTimeViewsMoveBetweenTicks()
        {
            var w = Lab(3, "realtime", WaitMode.Responsive)
                .Configure(x => { x.TickLimit = 40; x.RunSpeed = RunSpeed.RealTime; x.RealTimeTicksPerSecond = 20f; }).Build();
            w.Play();
            int between = 0, mostPerFrame = 0, last = 0;
            float until = Time.realtimeSinceStartup + 60f;
            while (w.State != RunState.Stopped && Time.realtimeSinceStartup < until)
            {
                yield return null;
                mostPerFrame = Mathf.Max(mostPerFrame, w.Tick - last);
                last = w.Tick;
                float f = w.InterpolationFactor;
                if (f > 0.05f && f < 0.95f) between++;
            }
            Assert.AreEqual(40, w.Tick);
            Assert.Greater(between, 5, "frames that showed the animals between two ticks");
            Assert.LessOrEqual(mostPerFrame, 2, "ticks run in one frame at 20 ticks per second");

            var freeze = Lab(3, "realtime-freeze").Configure(x => x.TickLimit = 40).Build();
            freeze.Advance(40);
            Assert.AreEqual(freeze.Events.Hash, w.Events.Hash, "the pace never changes the run");
        }

        [UnityTest, Description("07 §1: the camera frames the whole ground at the start, and a click on an animal's view picks that animal; the camera only reads (SPACE-12)")]
        public IEnumerator CameraFramesTheWorldAndPicksAnimals()
        {
            var w = Lab(5, "camera").Configure(x => x.TickLimit = 20).Build();
            var camera = Add<WorldCamera>(w);
            camera.World = w;
            w.Play();
            for (int i = 0; i < 5; i++) yield return null;
            var cam = camera.GetComponent<Camera>();
            var b = w.Ground.Bounds;
            foreach (var corner in new[] { new Vector3(b.xMin, 0, b.yMin), new Vector3(b.xMax, 0, b.yMin), new Vector3(b.xMin, 0, b.yMax), new Vector3(b.xMax, 0, b.yMax) })
            {
                var v = cam.WorldToViewportPoint(corner);
                Assert.IsTrue(v.z > 0 && v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1, $"corner {corner} on screen: {v}");
            }
            var a = w.FindSpecies("predator").Animals.First(x => !x.IsGone);
            var screen = cam.WorldToScreenPoint(w.ViewOf(a).transform.position + Vector3.up * 0.3f);
            var picked = camera.Pick(screen);
            Assert.IsNotNull(picked, "an animal under the click");
            Assert.Less(Vector3.Distance(cam.WorldToScreenPoint(w.ViewOf(picked).transform.position), cam.WorldToScreenPoint(w.ViewOf(a).transform.position)), 2f,
                        "the animal picked is the one clicked (or one on the same spot)");
            camera.Follow(picked);
            yield return null;
            Assert.AreSame(picked, camera.Followed);
        }

        [UnityTest, Description("07 §4: World.Configuring is heard in Awake before the run starts: a launcher sets the seed or stops the start")]
        public IEnumerator ConfiguringRunsBeforeTheStart()
        {
            var heard = new List<World>();
            void Handler(World w) { heard.Add(w); w.Seed = 77; w.StartOnPlay = false; }
            World.Configuring += Handler;
            try
            {
                var go = new GameObject("configured world");
                extras.Add(go);
                go.SetActive(false);
                var world = go.AddComponent<World>();
                world.StartOnPlay = true;
                go.SetActive(true);
                yield return null;
                Assert.AreEqual(1, heard.Count);
                Assert.AreSame(world, heard[0]);
                Assert.AreEqual(77, world.Seed);
                Assert.IsFalse(world.IsInitialized, "the handler turned the start off");
            }
            finally { World.Configuring -= Handler; }
        }
    }
}
