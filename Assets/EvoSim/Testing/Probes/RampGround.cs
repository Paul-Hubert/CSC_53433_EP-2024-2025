using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test ground: flat for x &lt; RampStart, then a ramp rising at RampSlope (height per meter) (T-MOVE-07).</summary>
    /// <example>
    /// <code>
    /// var w = New().Ground&lt;RampGround&gt;(40, 40).Phase&lt;ActPhase&gt;()
    ///     .Species("careful", s =&gt; s.Module&lt;SlopeLimitLocomotion&gt;(l =&gt; l.MaxSlopeDegrees = 20f)
    ///                                .Action&lt;GoToAction&gt;("goto"))
    ///     .Build();
    /// </code>
    /// </example>
    public class RampGround : FlatGround
    {
        /// <summary>Where the ramp starts along x, in meters.</summary>
        public float RampStart = 10f;
        /// <summary>The ramp's rise per meter along x (1 = 45°).</summary>
        public float RampSlope = 1f;

        /// <summary>0 before <see cref="RampStart"/>, then rising by <see cref="RampSlope"/> meters per meter.</summary>
        /// <inheritdoc/>
        public override float Height(Vector3 p) => p.x < RampStart ? 0f : (p.x - RampStart) * RampSlope;
    }
}
