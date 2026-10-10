using System.Collections.Generic;
using NUnit.Framework;

namespace EvoSim.Testing
{
    /// <summary>Base class of tests that build worlds: every world built through <see cref="New"/> is destroyed after the test.</summary>
    public abstract class WorldFixture
    {
        readonly List<WorldBuilder> builders = new List<WorldBuilder>();

        /// <summary>A new builder, destroyed in TearDown.</summary>
        protected WorldBuilder New(int seed = 1234, WaitMode waitMode = WaitMode.Freeze, string name = "Test World")
        {
            var b = new WorldBuilder(seed, waitMode, name);
            builders.Add(b);
            return b;
        }

        [TearDown]
        public void DestroyWorlds()
        {
            foreach (var b in builders) b.Dispose();
            builders.Clear();
        }
    }
}
