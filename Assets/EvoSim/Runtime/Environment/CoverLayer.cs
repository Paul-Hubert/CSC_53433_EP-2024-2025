using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Cover in clustered patches (thickets): the top <see cref="fraction"/> of walkable cells of a value-noise
    /// map, drawn from the world stream (03 §2 reference: 20 %, octaves 8 and 4 m).
    /// </summary>
    public class CoverLayer : Cover
    {
        [SerializeField, Range(0f, 1f), Tooltip("Share of walkable ground in cover (reference 0.2; 0 = none).")]
        float fraction = 0.2f;
        [SerializeField, Tooltip("Noise octave cell sizes, in meters (reference 8, 4).")]
        float[] octaves = { 8f, 4f };
        [SerializeField, Min(0.25f), Tooltip("Cover cell size, in meters.")]
        float cellSize = 1f;

        CellGrid grid;
        bool[] cover;
        int coverCells;

        public CellGrid Grid => grid;
        public int CoverCells => coverCells;
        public float Fraction => fraction;

        public override void Initialize()
        {
            var ground = World.Ground ?? World.Service<Ground>();
            var bounds = ground != null ? ground.Bounds : new Rect(0, 0, 1, 1);
            grid = new CellGrid(bounds, cellSize);
            cover = new bool[grid.Count];
            coverCells = 0;
            if (fraction <= 0f) return;

            var noise = new ValueNoise(bounds, octaves, World.Random.Get("world/" + ServiceName));
            var walkable = new System.Collections.Generic.List<int>();
            var values = new float[grid.Count];
            for (int c = 0; c < grid.Count; c++)
            {
                var p = grid.Center(c);
                if (ground != null && !ground.IsWalkable(p)) continue;
                values[c] = noise.Sample(p.x, p.z);
                walkable.Add(c);
            }
            // The top `fraction` of walkable cells by noise value; ties by cell index (deterministic).
            walkable.Sort((a, b) => values[b] != values[a] ? values[b].CompareTo(values[a]) : a.CompareTo(b));
            int n = Mathf.RoundToInt(fraction * walkable.Count);
            for (int i = 0; i < n; i++) cover[walkable[i]] = true;
            coverCells = n;
        }

        public override bool InCover(Vector3 p)
        {
            int c = grid.CellOf(p);
            return c >= 0 && cover[c];
        }

        public override bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance)
        {
            if (InCover(p)) { point = p; distance = 0f; return true; }
            int c = grid.Nearest(cover, p, radius, World.Ground, out distance);
            point = c >= 0 ? grid.Center(c) : p;
            if (c >= 0 && World.Ground != null) point = World.Ground.OnGround(point);
            return c >= 0;
        }

        /// <summary>Marks cells by hand (Place.Cover in tests, hand-made maps).</summary>
        public void SetCover(Vector3 p, bool value)
        {
            int c = grid.CellOf(p);
            if (c < 0 || cover[c] == value) return;
            cover[c] = value;
            coverCells += value ? 1 : -1;
        }

        /// <summary>Removes all cover (tests).</summary>
        public void Clear()
        {
            System.Array.Clear(cover, 0, cover.Length);
            coverCells = 0;
        }

        public void SetFraction(float f) => fraction = Mathf.Clamp01(f);

        public override void Validate(ValidationReport report)
        {
            if (fraction < 0f || fraction > 1f) report.Error("V-35", this, "Cover fraction must be in [0, 1].");
            if (octaves == null || octaves.Length == 0) report.Error("V-35", this, "Cover needs at least one noise octave.");
        }
    }
}
