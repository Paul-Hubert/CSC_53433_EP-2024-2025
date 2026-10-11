using UnityEngine;

namespace EvoSim
{
    /// <summary>The Lab 1 ground: a flat rectangle at height 0, walkable everywhere inside (02 §1).</summary>
    /// <remarks>
    /// The rectangle starts at the world origin and spans <c>width</c> × <c>depth</c> meters. Derive from it for a flat
    /// test ground with something more: a pond (<c>PondGround</c>), a ramp (<c>RampGround</c>) or another distance
    /// (<c>DoubleDistanceGround</c>), in EvoSim's test probes.
    /// </remarks>
    public class FlatGround : Ground
    {
        [SerializeField, Min(1f), Tooltip("Size along x, in meters (reference 192, small 48).")]
        float width = 192f;
        [SerializeField, Min(1f), Tooltip("Size along z, in meters (reference 192, small 48).")]
        float depth = 192f;

        /// <summary>From (0, 0) to (width, depth).</summary>
        /// <inheritdoc/>
        public override Rect Bounds => new Rect(0f, 0f, width, depth);
        /// <summary>Always 0.</summary>
        /// <inheritdoc/>
        public override float Height(Vector3 p) => 0f;
        /// <summary>True everywhere inside the rectangle.</summary>
        /// <inheritdoc/>
        public override bool IsWalkable(Vector3 p) => Inside(p);

        /// <summary>Sets the size from code (WorldBuilder, scenarios).</summary>
        /// <remarks>Call it before the World initialises: layers lay their grids over the ground's rectangle then.</remarks>
        /// <param name="x">The size along x, in meters (at least 1).</param>
        /// <param name="z">The size along z, in meters (at least 1).</param>
        public void SetSize(float x, float z)
        {
            width = Mathf.Max(1f, x);
            depth = Mathf.Max(1f, z);
        }

        /// <summary>Reports a size below 1 × 1 m (V-37), with a fix that clamps it.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (!(width >= 1f && depth >= 1f))
                report.Error("V-37", this, "The ground must be at least 1 × 1 m.", new ValidationFix("Clamp", () => { width = float.IsNaN(width) ? 1f : Mathf.Max(1f, width); depth = float.IsNaN(depth) ? 1f : Mathf.Max(1f, depth); }));
        }
    }
}
