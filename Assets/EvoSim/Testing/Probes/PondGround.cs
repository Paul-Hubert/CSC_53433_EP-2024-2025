using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test ground: flat, with a round pond (not walkable) whose shore animals can reach (samples 22 §2–§3).</summary>
    public class PondGround : FlatGround
    {
        public Vector3 PondCenter = new Vector3(10f, 0f, 10f);
        public float PondRadius = 3f;

        public override bool IsWater(Vector3 p) => Flat(p, PondCenter) < PondRadius;

        public override bool IsWalkable(Vector3 p) => base.IsWalkable(p) && !IsWater(p);

        public override WaterHit? NearestWater(Vector3 p, float radius)
        {
            float d = Mathf.Max(0f, Flat(p, PondCenter) - PondRadius);
            if (d > radius) return null;
            var dir = new Vector3(p.x - PondCenter.x, 0f, p.z - PondCenter.z);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
            dir.Normalize();
            var water = PondCenter + dir * (PondRadius - 0.01f);
            var shore = PondCenter + dir * (PondRadius + 0.01f);
            return new WaterHit(water, shore, d);
        }

        static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
