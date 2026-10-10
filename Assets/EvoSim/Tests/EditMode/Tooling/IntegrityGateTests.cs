using EvoSim.Editor;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>The information measures of the brain gates (32 §2 B-03), as the prototype computes them (promptevo/metrics.py).</summary>
    public class IntegrityGateTests
    {
        static float[] R(params float[] p) => p;

        [Test, Description("B-03: MI_G is 0 when every genome answers alike and 1 bit when two genomes give opposite certain answers")]
        public void GenomeInformation()
        {
            var same = new[] { new[] { R(0.5f, 0.5f) }, new[] { R(0.5f, 0.5f) } };
            Assert.AreEqual(0.0, IntegrityGates.MiGenome(same), 1e-9);
            var opposite = new[] { new[] { R(1f, 0f) }, new[] { R(0f, 1f) } };
            Assert.AreEqual(1.0, IntegrityGates.MiGenome(opposite), 1e-9);
            // Mean over situations: one informative situation of two gives half a bit.
            var half = new[] { new[] { R(1f, 0f), R(0.5f, 0.5f) }, new[] { R(0f, 1f), R(0.5f, 0.5f) } };
            Assert.AreEqual(0.5, IntegrityGates.MiGenome(half), 1e-9);
        }

        [Test, Description("B-10: the judge prompt is the one of 32 §2, word for word")]
        public void JudgePromptMatchesTheContract()
        {
            string doc = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath),
                "Docs", "Interface", "32-integrity-prompts-and-ci.md")).Replace("\r\n", "\n");
            int start = doc.IndexOf("```text\n", doc.IndexOf("**The judge prompt**"), System.StringComparison.Ordinal) + "```text\n".Length;
            string expected = doc.Substring(start, doc.IndexOf("\n```", start, System.StringComparison.Ordinal) - start);
            Assert.AreEqual(expected, IntegrityGates.JudgePrompt);
        }

        [Test, Description("B-03: MI_O measures how much the answer depends on the situation, per genome")]
        public void SituationInformation()
        {
            var follows = new[] { new[] { R(1f, 0f), R(0f, 1f) } };
            Assert.AreEqual(1.0, IntegrityGates.MiObs(follows), 1e-9);
            var ignores = new[] { new[] { R(0.3f, 0.7f), R(0.3f, 0.7f) } };
            Assert.AreEqual(0.0, IntegrityGates.MiObs(ignores), 1e-6);
        }
    }
}
