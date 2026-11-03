using System.Linq;
using NUnit.Framework;

namespace EvoSim.Neuro.Tests
{
    /// <summary>The layout of the neural species' assembly (plan M16 §5).</summary>
    public class NeuroAssemblyTests
    {
        [Test, Description("T-EDIT-02 (EDIT-03; plan M16 §5): EvoSim.Neuro has no reference to UnityEditor")]
        public void NeuroDoesNotReferenceUnityEditor()
        {
            var assembly = typeof(NeuralNet).Assembly;
            Assert.AreEqual("EvoSim.Neuro", assembly.GetName().Name);
            var names = assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();
            Assert.That(names, Has.None.StartsWith("UnityEditor"), string.Join(", ", names));
        }
    }
}
