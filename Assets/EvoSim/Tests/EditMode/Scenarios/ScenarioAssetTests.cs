using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks the scenario assets of <c>Assets/EvoSim/Scenarios</c> (31 §1, CFG-02): every variant applies to its scene and
    /// validates, and every T2 variant runs twice to the same hash without a model call.
    /// </summary>
    /// <remarks>
    /// The tests open each scenario's scene additively through <see cref="Batch.WithScene"/> and <see cref="Batch.Run"/>:
    /// close the scene in the editor first. A new scenario asset, or a new variant, is picked up automatically.
    /// </remarks>
    public class ScenarioAssetTests
    {
        /// <summary>
        /// Every variant of every scenario asset, as <c>"asset name|variant name"</c>: the cases of <see cref="VariantApplies"/>.
        /// </summary>
        static IEnumerable<string> Variants()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScenarioAsset", new[] { "Assets/EvoSim/Scenarios" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (s == null) continue;
                foreach (var v in s.variants) yield return s.name + "|" + v.name;
            }
        }

        /// <summary>Loads a scenario asset of <c>Assets/EvoSim/Scenarios</c> by its file name, without the extension.</summary>
        static ScenarioAsset Load(string name) =>
            AssetDatabase.LoadAssetAtPath<ScenarioAsset>("Assets/EvoSim/Scenarios/" + name + ".asset");

        [Test, Description("CFG-02 (31 §1): every variant of every scenario asset applies to its scene without a problem and validates without error")]
        public void VariantApplies([ValueSource(nameof(Variants))] string id)
        {
            var parts = id.Split('|');
            var scenario = Load(parts[0]);
            string result = Batch.WithScene(scenario.scene, w =>
            {
                var problems = new List<string>();
                scenario.Apply(w, parts[1], scenario.seeds[0], problems);
                if (problems.Count > 0) return string.Join("; ", problems);
                var report = new ValidationReport();
                w.Prepare(report);
                return report.HasErrors ? report.ToString() : "ok";
            });
            Assert.AreEqual("ok", result);
        }

        [Test, Description("31 §1 exact facts, T2: every T2 variant runs 200 ticks without a model call, twice to the same hash")]
        public void T2VariantsAreReproducible()
        {
            int ran = 0;
            foreach (var id in Variants())
            {
                var parts = id.Split('|');
                var scenario = Load(parts[0]);
                var v = scenario.FindVariant(parts[1]);
                if ((string.IsNullOrEmpty(v.tier) ? scenario.tier : v.tier) != "T2") continue;
                string Once() => Batch.Run(scenario, v.name, scenario.seeds[0], 200, w =>
                {
                    foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
                });
                string a = Once(), b = Once();
                StringAssert.DoesNotStartWith("FAILED", a, id);
                StringAssert.Contains("model calls 0 decisions + 0 mutations", a, id);
                Assert.AreEqual(Hash(a), Hash(b), id);
                ran++;
            }
            Assert.Greater(ran, 2, "T2 variants found");
        }

        /// <summary>The first 16 characters of the hash in a <see cref="Batch.Run"/> result line, after <c>"hash "</c>.</summary>
        static string Hash(string line) => line.Substring(line.IndexOf("hash ") + 5, 16);
    }
}
