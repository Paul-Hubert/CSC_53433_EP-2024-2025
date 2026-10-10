using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;

namespace EvoSim.Tests
{
    /// <summary>The scenario assets (31, CFG-02): every variant applies to its scene and validates; T2 variants run twice to the same hash.</summary>
    public class ScenarioAssetTests
    {
        static IEnumerable<string> Variants()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScenarioAsset", new[] { "Assets/EvoSim/Scenarios" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (s == null) continue;
                foreach (var v in s.variants) yield return s.name + "|" + v.name;
            }
        }

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

        static string Hash(string line) => line.Substring(line.IndexOf("hash ") + 5, 16);
    }
}
