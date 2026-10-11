using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// A flat ground whose distance function is twice the Euclidean one: a replaced distance, which every sense and action
    /// must then use (T-SPACE-02, SPACE-06).
    /// </summary>
    /// <example>
    /// <code>
    /// var w = New().Ground&lt;DoubleDistanceGround&gt;(20, 20).Species("prey", s =&gt; s.Action&lt;RestAction&gt;()).Build();
    /// </code>
    /// </example>
    public class DoubleDistanceGround : FlatGround
    {
        /// <summary>Twice the flat ground's distance.</summary>
        /// <inheritdoc/>
        public override float Distance(Vector3 a, Vector3 b) => 2f * base.Distance(a, b);
    }
}
