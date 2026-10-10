using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>The reference scenes of M9 (20 §7, 31): what each one is for, offline (random brain, no mutation).</summary>
    public class SceneTests : WorldFixture
    {
        const string Scenes = "Assets/EvoSim/Scenes/";

        /// <summary>Runs a scene offline: the random brain, no mutation, no files; returns what the check returns.</summary>
        static string Offline(string scene, System.Func<World, string> check, int ticks = 0)
        {
            return Batch.WithScene(Scenes + scene + ".unity", w =>
            {
                var random = w.GetComponentInChildren<RandomBrain>(true);
                if (random != null) w.DefaultBrain = random;
                w.NoMutation = true;
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                w.Seed = 5;
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
                if (ticks > 0) w.Advance(ticks);
                return check(w);
            });
        }

        static string Names(World w, Species s) => string.Join(",", w.ThreatsOf(s).Select(x => x.Id).OrderBy(x => x));

        [Test, Description("S09 (SPEC-10, SPEC-20): ThreeSpecies derives threats rabbit {fox, wolf}, fox {wolf}, wolf {}; prompts and situation texts name the species; 300 ticks without error")]
        public void ThreeSpecies()
        {
            string result = Offline("ThreeSpecies", w =>
            {
                var rabbit = w.FindSpecies("rabbit");
                var fox = w.FindSpecies("fox");
                var wolf = w.FindSpecies("wolf");
                Assert.AreEqual("fox,wolf", Names(w, rabbit));
                Assert.AreEqual("wolf", Names(w, fox));
                Assert.AreEqual("", Names(w, wolf));
                StringAssert.Contains("Foxes run 2 and wolves 2", rabbit.Prompt.Text);
                StringAssert.Contains("You decide what a fox does next", fox.Prompt.Text);
                StringAssert.Contains("- flee:", fox.Prompt.Text, "foxes flee wolves");
                Assert.IsNotNull(fox.FindSense("Wolf"), "a single threat is named after its species");
                Assert.IsNotNull(fox.FindSense("Rabbit"), "a single prey too");
                Assert.IsNotNull(rabbit.FindSense("Predator"), "two threats: the generic word");
                return "ok";
            }, 300);
            Assert.AreEqual("ok", result);
        }

        [Test, Description("S17 (SPACE-05): Terrain_Locomotion has about 15 % water and some unwalkable mountains; after 300 ticks no animal stands on water or on unwalkable ground")]
        public void TerrainLocomotion()
        {
            string result = Offline("Terrain_Locomotion", w =>
            {
                var ground = (TerrainGround)w.Ground;
                int water = 0, blocked = 0, n = 0;
                var b = ground.Bounds;
                for (float z = b.yMin + 0.5f; z < b.yMax; z += 1f)
                    for (float x = b.xMin + 0.5f; x < b.xMax; x += 1f)
                    {
                        var p = new Vector3(x, 0f, z);
                        n++;
                        if (ground.IsWater(p)) water++;
                        else if (!ground.IsWalkable(p)) blocked++;
                    }
                float waterShare = (float)water / n, blockedShare = (float)blocked / n;
                Assert.That(waterShare, Is.InRange(0.10f, 0.20f), "water share");
                Assert.That(blockedShare, Is.InRange(0.02f, 0.25f), "mountains and cut-off pockets");
                foreach (var s in w.AllSpecies)
                    foreach (var a in s.Animals)
                        if (!a.IsGone) Assert.IsTrue(ground.IsWalkable(a.Position), $"{s.Id} {a.Id} at {a.Position}");
                return $"water {waterShare:P0}, blocked {blockedShare:P0}";
            }, 300);
            StringAssert.StartsWith("water", result);
        }

        [Test, Description("Sandbox (20 §7): one grazing species, the random brain, no mutator; 500 ticks without error or model call")]
        public void Sandbox()
        {
            string result = Offline("Sandbox", w =>
            {
                Assert.AreEqual(1, w.AllSpecies.Count);
                Assert.IsNull(w.Service<MutatorService>());
                Assert.AreEqual(0, w.Decisions.ModelCalls);
                Assert.Greater(w.AllSpecies[0].Counters.Decisions, 0);
                return "ok";
            }, 500);
            Assert.AreEqual("ok", result);
        }

        [Test, Description("20 §7: the action module prefabs exist, each an action named after itself with its text gene, and the reference scenes use them")]
        public void ModulePrefabs()
        {
            foreach (var name in new[] { "Eat", "Flee", "Hide", "Follow", "Rest", "Mate", "Hunt" })
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/EvoSim/Modules/{name}.prefab");
                Assert.IsNotNull(prefab, name);
                Assert.IsNotNull(prefab.GetComponent<AnimalAction>(), name);
                Assert.IsNotNull(prefab.GetComponent<TextGene>(), name);
                Assert.AreEqual(name.ToLowerInvariant(), prefab.name.ToLowerInvariant());
            }
            Offline("Lab1_Small", w =>
            {
                foreach (var a in w.FindSpecies("prey").Actions)
                    Assert.IsTrue(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(a), a.Name + " is a module prefab instance");
                Assert.IsNotNull(w.FindSpecies("prey").Module<Body>());
                return "";
            });
        }
    }
}
