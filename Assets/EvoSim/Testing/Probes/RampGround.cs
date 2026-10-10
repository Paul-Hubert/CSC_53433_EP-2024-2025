using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test ground: flat for x &lt; RampStart, then a ramp rising at RampSlope (height per meter) (T-MOVE-07).</summary>
    public class RampGround : FlatGround
    {
        public float RampStart = 10f;
        public float RampSlope = 1f;

        public override float Height(Vector3 p) => p.x < RampStart ? 0f : (p.x - RampStart) * RampSlope;
    }
}
