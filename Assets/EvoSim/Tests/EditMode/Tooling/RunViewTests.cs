using System.Linq;
using EvoSim.Editor;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Watching a run (07 real-time test): the player's command line, and reference scenes that can be watched.</summary>
    public class RunViewTests : WorldFixture
    {
        [Test, Description("07 §4: the player's command line is read: scene, seed, ticks, speed, wait, brain, capture, cache, no mutation, quit at the end; wrong values are reported, not guessed")]
        public void PlayerArgumentsAreRead()
        {
            var p = PlayerArgs.Parse(new[] { "EvoSim.exe", "-scene", "HideVsFlee", "-seed", "1234", "-ticks", "600", "-speed", "20", "-wait", "freeze",
                                             "-brain", "JEV", "-capture", "10", "-cache", "cache", "-noMutation", "-quitAtEnd", "-logFile", "p.log",
                                             "-window", "1920x1080", "-run", "r1" });
            Assert.AreEqual("HideVsFlee", p.Scene);
            Assert.AreEqual(1234, p.Seed);
            Assert.AreEqual(600, p.Ticks);
            Assert.AreEqual(RunSpeed.RealTime, p.Speed);
            Assert.AreEqual(20f, p.TicksPerSecond);
            Assert.AreEqual(WaitMode.Freeze, p.Wait);
            Assert.AreEqual("JEV", p.Brain);
            Assert.AreEqual(10f, p.Capture);
            Assert.AreEqual("cache", p.Cache);
            Assert.IsTrue(p.NoMutation && p.QuitAtEnd);
            Assert.AreEqual((1920, 1080), (p.WindowWidth, p.WindowHeight));
            Assert.AreEqual("r1", p.Run);
            Assert.IsEmpty(p.Problems);

            Assert.AreEqual(RunSpeed.Fast, PlayerArgs.Parse(new[] { "-speed", "fast" }).Speed);
            Assert.AreEqual(RunSpeed.PerFixedUpdate, PlayerArgs.Parse(new[] { "-speed", "FIXED" }).Speed);
            Assert.AreEqual(-1f, PlayerArgs.Parse(new[] { "-capture", "off" }).Capture);
            var bad = PlayerArgs.Parse(new[] { "-seed", "x", "-speed", "0", "-wait", "maybe", "-capture", "-3", "-window", "big" });
            Assert.AreEqual(5, bad.Problems.Count, string.Join("; ", bad.Problems));
            Assert.IsNull(bad.Seed);
        }

        [Test, Description("07 §4: the arguments set the World before it starts: seed, tick limit, RealTime speed, wait mode, the default brain by name, no mutation, the cache folder; an unknown brain is reported")]
        public void PlayerArgumentsSetTheWorld()
        {
            var b = New(1).Lab1(prey: 4, predators: 1).Service<AnswerCache>("Answer cache");
            var w = b.BuildUninitialized();
            var random = w.GetComponentInChildren<RandomBrain>(true);
            var p = PlayerArgs.Parse(new[] { "-seed", "99", "-ticks", "30", "-speed", "12.5", "-wait", "responsive", "-brain", random.gameObject.name.ToUpperInvariant(),
                                             "-noMutation", "-cache", "Temp/EvoSimCacheTest" });
            string said = p.ApplyTo(w);
            Assert.IsEmpty(p.Problems, string.Join("; ", p.Problems));
            Assert.AreEqual(99, w.Seed);
            Assert.AreEqual(30, w.TickLimit);
            Assert.AreEqual(RunSpeed.RealTime, w.RunSpeed);
            Assert.AreEqual(12.5f, w.RealTimeTicksPerSecond);
            Assert.AreEqual(WaitMode.Responsive, w.WaitMode);
            Assert.AreSame(random, w.DefaultBrain);
            Assert.IsTrue(w.NoMutation);
            StringAssert.EndsWith(System.IO.Path.Combine("Temp", "EvoSimCacheTest"), w.GetComponentInChildren<AnswerCache>(true).Folder);
            StringAssert.Contains("seed 99", said);

            var unknown = PlayerArgs.Parse(new[] { "-brain", "nobody" });
            unknown.ApplyTo(w);
            Assert.AreEqual(1, unknown.Problems.Count);
            Assert.AreSame(random, w.DefaultBrain, "an unknown brain changes nothing");
        }

        [Test, Description("07 §1 (SPACE-12): every reference scene can be watched: run views (ground, entity markers, overlay, capture) outside the World, a WorldCamera on its camera")]
        public void ReferenceScenesCanBeWatched()
        {
            foreach (var path in System.IO.Directory.GetFiles(ReferenceScenes.ScenesFolder, "*.unity"))
            {
                string result = Batch.WithScene(path.Replace('\\', '/'), w =>
                {
                    var roots = w.gameObject.scene.GetRootGameObjects();
                    var views = roots.FirstOrDefault(r => r.name == RunViews.ObjectName);
                    Assert.IsNotNull(views, path + ": run views");
                    Assert.IsNotNull(views.GetComponent<GroundView>(), path);
                    Assert.IsNotNull(views.GetComponent<EntityMarkers>(), path);
                    Assert.IsNotNull(views.GetComponent<RunOverlay>(), path);
                    Assert.IsNotNull(views.GetComponent<RunCapture>(), path);
                    Assert.IsNull(views.GetComponentInParent<World>(), path + ": the run views are outside the World");
                    Assert.IsTrue(roots.Any(r => r.GetComponentInChildren<WorldCamera>(true) != null), path + ": a WorldCamera");
                    return "ok";
                });
                Assert.AreEqual("ok", result);
            }
        }

        [Test, Description("07 §5 (SPACE-12, RAND-11): the reference bodies stand on the ground, show where they go (a nose in front) and their action (an ActionView marker), and have no collider")]
        public void ReferenceBodiesShowActionAndHeading()
        {
            foreach (var name in new[] { "PreyBody", "PredatorBody" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReferenceScenes.BodiesFolder + "/" + name + ".prefab");
                Assert.IsNotNull(prefab, name);
                var view = prefab.GetComponent<ActionView>();
                Assert.IsNotNull(view, name + ": an ActionView at the root (the pool finds it there)");
                Assert.IsNotNull(view.Body);
                Assert.IsNotNull(view.Marker);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), name + ": no collider");
                var bounds = view.Body.GetComponent<MeshFilter>().sharedMesh.bounds;
                float bottom = view.Body.transform.localPosition.y + bounds.min.y * view.Body.transform.localScale.y;
                Assert.AreEqual(0f, bottom, 1e-3f, name + ": the body stands on the ground");
                Assert.Greater(prefab.transform.Find("Nose").localPosition.z, 0.3f, name + ": the nose is in front");
            }
        }
    }
}
