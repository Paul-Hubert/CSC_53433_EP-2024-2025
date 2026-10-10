using UnityEngine;

namespace EvoSim
{
    /// <summary>The Lab 1 ground: a flat rectangle at height 0, walkable everywhere inside (02 §1).</summary>
    public class FlatGround : Ground
    {
        [SerializeField, Min(1f), Tooltip("Size along x, in meters (reference 192, small 48).")]
        float width = 192f;
        [SerializeField, Min(1f), Tooltip("Size along z, in meters (reference 192, small 48).")]
        float depth = 192f;

        public override Rect Bounds => new Rect(0f, 0f, width, depth);
        public override float Height(Vector3 p) => 0f;
        public override bool IsWalkable(Vector3 p) => Inside(p);

        /// <summary>Sets the size from code (WorldBuilder, scenarios).</summary>
        public void SetSize(float x, float z)
        {
            width = Mathf.Max(1f, x);
            depth = Mathf.Max(1f, z);
        }

        public override void Validate(ValidationReport report)
        {
            if (width < 1f || depth < 1f) report.Error("V-35", this, "The ground must be at least 1 × 1 m.");
        }
    }
}
