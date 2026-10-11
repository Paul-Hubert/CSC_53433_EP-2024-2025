using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test action that walks or runs toward a fixed point, stopping at a distance (T-SPACE-01, T-MOVE-01).</summary>
    /// <remarks>Set its fields from the test after <c>Build</c>; they take effect at the next act phase.</remarks>
    /// <example>
    /// <code>
    /// var w = New().Flat(40, 40).Food(0f, 0f).Phase&lt;ActPhase&gt;()
    ///     .Species("prey", s =&gt; s.PreyBody().Action&lt;GoToAction&gt;("goto")).Build();
    /// var a = Place.Animal(w.FindSpecies("prey"), Place.At(10.123f, 7.77f));
    /// a.Choose("goto");
    /// var target = a.Position + new Vector3(0.3f, 0f, 0.2158703f);          // 0.3695 m away
    /// ((GoToAction)a.Species.FindAction("goto")).Target = target;
    /// w.Advance(1);
    /// Assert.AreEqual(target.x, a.Position.x, 1e-5f);
    /// </code>
    /// </example>
    public class GoToAction : AnimalAction
    {
        /// <summary>"go to a fixed point (a test probe)".</summary>
        protected override string DefaultDescription => "go to a fixed point (a test probe)";

        /// <summary>The point to go to.</summary>
        public Vector3 Target;
        /// <summary>Stop this far from <see cref="Target"/>, in meters.</summary>
        public float StopAt;
        /// <summary>True: run (the running speed), else walk.</summary>
        public bool Run;
        /// <summary>When set, the action gives this direction with no distance limit (T-SPACE-03).</summary>
        public Vector3 Direction;

        /// <summary>
        /// Walks 1 000 m along <see cref="Direction"/> when it is set; else walks or runs to <see cref="Target"/>, stopping
        /// at <see cref="StopAt"/>.
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            if (Direction != Vector3.zero) { c.WalkTo(a.Position + Direction * 1000f); return; }
            if (Run) c.RunTo(Target, StopAt);
            else c.WalkTo(Target, StopAt);
        }
    }
}
