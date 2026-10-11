using System.Linq;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks the assembly layout of 20 §7 by reflection, without building a world: the runtime code compiles into
    /// <c>EvoSim.Runtime</c>, and no runtime assembly (Runtime, Http, Samples) references <c>UnityEditor</c> (EDIT-03), so a
    /// player build never needs the editor.
    /// </summary>
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
