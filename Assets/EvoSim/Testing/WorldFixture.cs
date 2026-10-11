using System.Collections.Generic;
using NUnit.Framework;

namespace EvoSim.Testing
{
    /// <summary>
    /// The base class of tests that build worlds: every world started with <see cref="New"/> is destroyed after each test,
    /// so tests leave nothing in the scene.
    /// </summary>
    /// <remarks>
    /// Derive your test class from it, start each world with <see cref="New"/>, add what the test needs with the
    /// <see cref="WorldBuilder"/> methods (and the <see cref="Lab"/> recipes), then call <see cref="WorldBuilder.Build"/>.
    /// Advance the world tick by tick with <see cref="World.Advance"/> and assert on its animals, counters and events.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ThirstTests : WorldFixture
    /// {
    ///     [Test, Description("22 §1: thirst kills at its deadly level")]
    ///     public void ThirstKills()
    ///     {
    ///         var w = New().Flat(20, 20).Food(0f, 0f).Phase&lt;DeathPhase&gt;()
    ///             .Species("prey", s =&gt; s.PreyBody().Action&lt;RestAction&gt;("rest")
    ///                 .Module&lt;Thirst&gt;(t =&gt; t.Configure(10f, 30f)).Module&lt;DiesOfThirst&gt;())
    ///             .Build();
    ///         var sp = w.FindSpecies("prey");
    ///         var a = Place.Animal(sp, Place.At(5, 5));
    ///         a.Choose("rest");
    ///         w.Advance(3);
    ///         Assert.IsTrue(a.IsGone, "dies at 30");
    ///         Assert.AreEqual(1, sp.Counters.Deaths["thirst"]);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class WorldFixture
    {
        readonly List<WorldBuilder> builders = new List<WorldBuilder>();   // every builder made by New, disposed in DestroyWorlds

        /// <summary>A new world builder, whose world is destroyed when the test ends.</summary>
        /// <param name="seed">The world's seed: the same seed gives the same run (RAND-10).</param>
        /// <param name="waitMode">How a tick waits for late answers; Freeze (the default) blocks, as tests need.</param>
        /// <param name="name">The name of the world's root GameObject; give each world of a test its own.</param>
        /// <returns>An empty builder: a World with its Environment, Brains and Phases objects, not yet initialised.</returns>
        protected WorldBuilder New(int seed = 1234, WaitMode waitMode = WaitMode.Freeze, string name = "Test World")
        {
            var b = new WorldBuilder(seed, waitMode, name);
            builders.Add(b);
            return b;
        }

        /// <summary>Destroys every world the test built with <see cref="New"/>; NUnit calls it after each test.</summary>
        [TearDown]
        public void DestroyWorlds()
        {
            foreach (var b in builders) b.Dispose();
            builders.Clear();
        }
    }
}
