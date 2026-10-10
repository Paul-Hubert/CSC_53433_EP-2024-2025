using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A flat ground whose distance function is twice the Euclidean one: a replaced distance (T-SPACE-02).</summary>
    public class DoubleDistanceGround : FlatGround
    {
        public override float Distance(Vector3 a, Vector3 b) => 2f * base.Distance(a, b);
    }
}
