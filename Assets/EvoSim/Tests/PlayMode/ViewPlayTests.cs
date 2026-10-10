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
        World Make(bool show, bool interpolate, string name)
        {
            var b = New(11, WaitMode.Freeze, name).Lab1(prey: 20, predators: 4)
                .Configure(x => { x.TickLimit = 150; x.ShowViews = show; x.InterpolateViews = interpolate; });
            b.Root.transform.Find("prey").gameObject.AddComponent<Body>().Configure(null, Color.green, 0.6f);
            b.Root.transform.Find("predator").gameObject.AddComponent<Body>().Configure(null, Color.red, 0.9f);
            var w = b.Build();
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
            }
        }

        [UnityTest, Description("T-SPACE-06 (SPACE-12): views shown or hidden, interpolation on or off → identical hashes; one view per living animal, between its tick positions")]
        public IEnumerator ViewsNeverChangeTheRun()
        {
            var hashes = new List<string>();
            var problems = new List<string>();
            foreach (var (show, interpolate) in new[] { (true, true), (true, false), (false, true) })
            {
                var w = Make(show, interpolate, $"views-{show}-{interpolate}");
                yield return RunToEnd(w, problems);
                Assert.AreEqual(150, w.Tick);
                hashes.Add(w.Events.Hash);
                if (show) Assert.Greater(w.transform.Find("Views (runtime)").childCount, 0, "views were made");
            }
            Assert.Greater(checks, 20, "frames where the views were compared with the animals");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(10)));
            Assert.AreEqual(hashes[0], hashes[1], "interpolation off");
            Assert.AreEqual(hashes[0], hashes[2], "views hidden");
        }
    }
}
