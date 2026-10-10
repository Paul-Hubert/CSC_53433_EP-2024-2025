using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>Views (20 §6): pools from each species' Body, placed in LateUpdate, never writing to the run (T2).</summary>
    public class ViewPlayTests : WorldFixture
    {
        /// <summary>The run views of the reference scenes (RunViews.AddTo): ground, entity markers, overlay, capture, a camera.</summary>
        sealed class RunViewSet
        {
            public GroundView Ground;
            public EntityMarkers Markers;
            public RunOverlay Overlay;
            public RunCapture Capture;
            public WorldCamera Camera;
        }

        readonly List<GameObject> extras = new List<GameObject>();
        RunViewSet set;
        int groundChecks, markerChecks;

        [TearDown]
        public void DestroyRunViews()
        {
            foreach (var go in extras) if (go != null) Object.DestroyImmediate(go);
            extras.Clear();
        }

        RunViewSet AddRunViews(World w, string captures)
        {
            var go = new GameObject("Run views (test)");
            var cam = new GameObject("Camera (test)", typeof(Camera));
            extras.Add(go);
            extras.Add(cam);
            var views = new RunViewSet
            {
                Ground = go.AddComponent<GroundView>(), Markers = go.AddComponent<EntityMarkers>(), Overlay = go.AddComponent<RunOverlay>(),
                Capture = go.AddComponent<RunCapture>(), Camera = cam.AddComponent<WorldCamera>(),
            };
            views.Ground.World = views.Markers.World = views.Overlay.World = views.Capture.World = views.Camera.World = w;
            views.Capture.Folder = captures;
            views.Capture.EverySeconds = 0.5f;
            return views;
        }

        World Make(bool show, bool interpolate, string name, bool runViews = false)
        {
            var b = New(11, WaitMode.Freeze, name).Lab1(prey: 20, predators: 4)
                .Configure(x => { x.TickLimit = 150; x.ShowViews = show; x.InterpolateViews = interpolate; });
            b.Root.transform.Find("prey").gameObject.AddComponent<Body>().Configure(null, Color.green, 0.6f);
            b.Root.transform.Find("predator").gameObject.AddComponent<Body>().Configure(null, Color.red, 0.9f);
            var w = b.Build();
            if (runViews) set = AddRunViews(w, System.IO.Path.Combine(Application.temporaryCachePath, "evosim-captures-" + System.DateTime.Now.Ticks));
            w.Play();
            return w;
        }

        static int Living(World w) => w.AllSpecies.Sum(s => s.Animals.Count(a => !a.IsGone));

        int checks;

        IEnumerator RunToEnd(World w, List<string> problems)
        {
            float until = Time.realtimeSinceStartup + 120f;                            // frames can be very short in batch mode
            while (w.State != RunState.Stopped && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (w.State == RunState.Stopped) break;
                if (w.ViewsTick != w.Tick) continue;                                 // a tick ran since the views were placed
                checks++;
                int expected = w.ShowViews ? Living(w) : 0;
                if (w.ViewCount != expected) problems.Add($"tick {w.Tick}: {w.ViewCount} views for {expected} animals");
                foreach (var s in w.AllSpecies)
                    foreach (var a in s.Animals)
                    {
                        var v = w.ViewOf(a);
                        if (v == null) continue;
                        if (v.AnimalId != a.Id) problems.Add($"view of {a.Id} shows {v.AnimalId}");
                        if (v.gameObject.layer != 2) problems.Add("a view outside the Ignore Raycast layer");
                        var flat = new Vector2(v.transform.position.x, v.transform.position.z);
                        var from = new Vector2(a.PreviousPosition.x, a.PreviousPosition.z);
                        var to = new Vector2(a.Position.x, a.Position.z);
                        if (Vector2.Distance(flat, from) + Vector2.Distance(flat, to) > Vector2.Distance(from, to) + 1e-3f)
                            problems.Add($"view of {a.Id} off the segment between its tick positions");
                    }
                if (set != null) CheckRunViews(w, problems);
            }
        }

        /// <summary>The ground shows each food item and cover cell of the tick it painted; one marker per carcass and egg, where it lies.</summary>
        void CheckRunViews(World w, List<string> problems)
        {
            if (set.Ground.PaintedTick == w.Tick)
            {
                groundChecks++;
                var food = w.Service<FoodGrid>();
                var cover = w.Service<CoverLayer>();
                for (int c = 0; c < food.Grid.Count; c++)
                {
                    var p = food.Grid.Center(c);
                    var shown = set.Ground.ColorAt(p);
                    var expected = food.HasItem(c) ? set.Ground.FoodColor : cover.InCover(p) ? set.Ground.CoverColor : set.Ground.SoilColor;
                    if (!shown.Equals(expected)) { problems.Add($"tick {w.Tick}: ground cell {c} shows {shown}, not {expected}"); break; }
                }
            }
            if (set.Markers.ShownTick == w.Tick)
            {
                markerChecks++;
                foreach (var kind in w.ServicesOf<EntityKind>())
                {
                    int n = 0;
                    for (int i = 0; i < kind.EntityCount; i++)
                    {
                        var e = kind.EntityAt(i);
                        if (e.UsedUp) continue;
                        var m = set.Markers.Marker(kind.Kind, n++).position;
                        if (Mathf.Abs(m.x - e.Position.x) > 1e-4f || Mathf.Abs(m.z - e.Position.z) > 1e-4f)
                            problems.Add($"tick {w.Tick}: the marker of {kind.Kind} {e.Id} isn't where it lies");
                    }
                    if (set.Markers.Shown(kind.Kind) != n) problems.Add($"tick {w.Tick}: {set.Markers.Shown(kind.Kind)} {kind.Kind} markers for {n}");
                }
            }
        }

        [UnityTest, Description("T-SPACE-06 (SPACE-12, RAND-11): views shown or hidden, interpolation on or off, the run views on (ground, entity markers, overlay, capture, camera) → identical hashes; one view per living animal, between its tick positions; the ground shows the tick's food and cover, one marker per carcass and egg")]
        public IEnumerator ViewsNeverChangeTheRun()
        {
            var hashes = new List<string>();
            var problems = new List<string>();
            foreach (var (show, interpolate, runViews) in new[] { (true, true, false), (true, false, false), (false, true, false), (true, true, true) })
            {
                set = null;
                var w = Make(show, interpolate, $"views-{show}-{interpolate}-{runViews}", runViews);
                yield return RunToEnd(w, problems);
                Assert.AreEqual(150, w.Tick);
                hashes.Add(w.Events.Hash);
                if (show) Assert.Greater(w.transform.Find("Views (runtime)").childCount, 0, "views were made");
                if (set != null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    float wait = Time.realtimeSinceStartup + 15f;                            // the end capture and the contact sheet
                    while (!set.Capture.Finished && Time.realtimeSinceStartup < wait) yield return null;
                    Assert.IsTrue(set.Capture.Finished, "the capture finished after the stop");
                    Assert.GreaterOrEqual(set.Capture.Captures + set.Capture.Skipped, 2, "a start and an end capture, drawn or given up (a hidden Game view)");
                    if (set.Capture.Captures > 0) Assert.IsTrue(System.IO.File.Exists(set.Capture.ContactSheetPath), "the contact sheet");
                }
            }
            Assert.Greater(checks, 20, "frames where the views were compared with the animals");
            Assert.Greater(groundChecks, 5, "frames where the ground was compared with the food and cover");
            Assert.Greater(markerChecks, 5, "frames where the markers were compared with the entities");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(10)));
            Assert.AreEqual(hashes[0], hashes[1], "interpolation off");
            Assert.AreEqual(hashes[0], hashes[2], "views hidden");
            Assert.AreEqual(hashes[0], hashes[3], "the run views on");
        }

        [UnityTest, Description("20 §6, open: a view of one's own (an AnimalView subclass on the Body's prefab) hears OnShow with its own animal every frame it is shown")]
        public IEnumerator ViewsHearTheirAnimal()
        {
            var prefab = new GameObject("probe view");
            prefab.AddComponent<ShowProbeView>();
            var b = New(11, WaitMode.Freeze, "views-probe").Lab1(prey: 20, predators: 4).Configure(x => x.TickLimit = 60);
            b.Root.transform.Find("prey").gameObject.AddComponent<Body>().Configure(prefab, Color.green, 0.6f);
            var w = b.Build();
            w.Play();
            var problems = new List<string>();
            int frames = 0;
            float until = Time.realtimeSinceStartup + 60f;
            while (w.State != RunState.Stopped && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (w.State == RunState.Stopped || w.ViewsTick != w.Tick) continue;
                frames++;
                foreach (var a in w.FindSpecies("prey").Animals)
                {
                    if (a.IsGone) continue;
                    var v = w.ViewOf(a) as ShowProbeView;
                    if (v == null) problems.Add($"no probe view for {a.Id}");
                    else if (v.Shown == 0 || v.LastShownId != a.Id) problems.Add($"view of {a.Id}: shown {v.Shown} times, last for {v.LastShownId}");
                }
            }
            Object.Destroy(prefab);
            Assert.AreEqual(60, w.Tick);
            Assert.Greater(frames, 5, "frames where the views were looked at");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(10)));
        }
    }
}
