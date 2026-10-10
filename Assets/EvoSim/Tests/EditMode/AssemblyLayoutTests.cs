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

        [Test, Description("T-EDIT-02 (EDIT-03): the runtime assemblies (Runtime, Http, Samples) have no reference to UnityEditor")]
        public void RuntimeAssembliesDoNotReferenceUnityEditor()
        {
            foreach (var runtime in new[] { typeof(Units).Assembly, typeof(HttpBrain).Assembly, typeof(EvoSim.Samples.Thirst).Assembly })
            {
                var names = runtime.GetReferencedAssemblies().Select(r => r.Name).ToList();
                Assert.That(names, Has.None.StartsWith("UnityEditor"), runtime.GetName().Name + ": " + string.Join(", ", names));
            }
        }
    }
}
