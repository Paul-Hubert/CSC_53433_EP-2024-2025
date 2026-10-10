using System.Linq;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>The assembly layout of Docs/Interface/20 §7.</summary>
    public class AssemblyLayoutTests
    {
        [Test, Description("M0 skeleton: the runtime assembly exists under its name")]
        public void RuntimeAssemblyIsEvoSimRuntime()
        {
            Assert.AreEqual("EvoSim.Runtime", typeof(Units).Assembly.GetName().Name);
            Assert.AreEqual(0.001f, Units.Epsilon);
        }

        [Test, Description("T-EDIT-02 (EDIT-03): the runtime assemblies have no reference to UnityEditor")]
        public void RuntimeAssembliesDoNotReferenceUnityEditor()
        {
            var runtime = typeof(Units).Assembly;
            var names = runtime.GetReferencedAssemblies().Select(r => r.Name).ToList();
            Assert.That(names, Has.None.StartsWith("UnityEditor"), string.Join(", ", names));
        }
    }
}
