using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A Unity Terrain as ground (02 §1): heights from the heightmap; walkable above the water level, below a
    /// maximum steepness, outside tree footprints; only the largest connected walkable region is kept (SPACE-05).
    /// Walkability is computed once, on a grid of cells.
    /// </summary>
    public class TerrainGround : Ground
    {
        [SerializeField, Tooltip("The terrain, on this GameObject or below it (ARCH-08). Empty = the first one found there.")]
        Terrain terrain;
        [SerializeField, Tooltip("World height of the water surface, in meters: lower ground is water (terrain preview: the lowest 15 %).")]
        float waterLevel = 0f;
        [SerializeField, Range(0f, 90f), Tooltip("Steeper ground is not walkable, in degrees.")]
        float maxSteepness = 35f;
        [SerializeField, Min(0f), Tooltip("Radius around each tree that animals can't enter, in meters (0 = trees don't block).")]
        float treeFootprint = 0.5f;
        [SerializeField, Min(0.25f), Tooltip("Walkability grid cell size, in meters.")]
        float cellSize = 1f;
        [SerializeField, Tooltip("Keep only the largest connected walkable region (SPACE-05).")]
        bool keepLargestRegion = true;

        CellGrid grid;
        bool[] walkable, water;
        Rect bounds;
        Vector3 terrainOrigin;

        public Terrain Terrain => terrain;
        public CellGrid Grid => grid;
        public float WaterLevel => waterLevel;
        public float MaxSteepness => maxSteepness;
        public override Rect Bounds => bounds;

        public override void Initialize()
        {
            if (terrain == null) terrain = GetComponentInChildren<Terrain>();
            if (terrain == null || terrain.terrainData == null) { bounds = new Rect(0, 0, 1, 1); grid = new CellGrid(bounds, 1f); walkable = water = new bool[1]; return; }
            terrainOrigin = terrain.GetPosition();
            var size = terrain.terrainData.size;
            bounds = new Rect(terrainOrigin.x, terrainOrigin.z, size.x, size.z);
            grid = new CellGrid(bounds, cellSize);
            walkable = new bool[grid.Count];
            water = new bool[grid.Count];
            var data = terrain.terrainData;
            for (int c = 0; c < grid.Count; c++)
            {
                var p = grid.Center(c);
                float h = SampleHeight(p);
                if (h <= waterLevel) { water[c] = true; continue; }
                float nx = (p.x - bounds.xMin) / bounds.width, nz = (p.z - bounds.yMin) / bounds.height;
                if (data.GetSteepness(nx, nz) > maxSteepness) continue;
                walkable[c] = true;
            }
            if (treeFootprint > 0f) BlockTrees(data);
            if (keepLargestRegion) KeepLargestRegion();
        }

        void BlockTrees(TerrainData data)
        {
            foreach (var tree in data.treeInstances)
            {
                var p = new Vector3(bounds.xMin + tree.position.x * bounds.width, 0f, bounds.yMin + tree.position.z * bounds.height);
                int r = Mathf.CeilToInt(treeFootprint / cellSize);
                int cx = grid.X(Mathf.Max(0, grid.CellOf(p))), cz = grid.Z(Mathf.Max(0, grid.CellOf(p)));
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (x < 0 || z < 0 || x >= grid.Width || z >= grid.Height) continue;
                        int c = grid.Index(x, z);
                        if (Distance(grid.Center(c), p) <= treeFootprint + cellSize * 0.5f) walkable[c] = false;
                    }
            }
        }

        /// <summary>Flood fill (4-neighbour); cells outside the largest region become non-walkable (SPACE-05).</summary>
        void KeepLargestRegion()
        {
            var region = new int[grid.Count];
            int regions = 0, bestRegion = -1, bestSize = 0;
            var stack = new Stack<int>();
            for (int start = 0; start < grid.Count; start++)
            {
                if (!walkable[start] || region[start] != 0) continue;
                int id = ++regions, size = 0;
                region[start] = id;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int c = stack.Pop();
                    size++;
                    int x = grid.X(c), z = grid.Z(c);
                    Visit(x + 1, z); Visit(x - 1, z); Visit(x, z + 1); Visit(x, z - 1);
                    void Visit(int vx, int vz)
                    {
                        if (vx < 0 || vz < 0 || vx >= grid.Width || vz >= grid.Height) return;
                        int n = grid.Index(vx, vz);
                        if (!walkable[n] || region[n] != 0) return;
                        region[n] = id;
                        stack.Push(n);
                    }
                }
                if (size > bestSize) { bestSize = size; bestRegion = id; }
            }
            for (int c = 0; c < grid.Count; c++)
                if (walkable[c] && region[c] != bestRegion) walkable[c] = false;
        }

        float SampleHeight(Vector3 p) => terrain != null ? terrain.SampleHeight(p) + terrainOrigin.y : 0f;

        public override float Height(Vector3 p) => SampleHeight(p);

        public override bool IsWalkable(Vector3 p)
        {
            int c = grid.CellOf(p);
            return c >= 0 && walkable[c] && Inside(p);
        }

        public override bool IsWater(Vector3 p)
        {
            int c = grid.CellOf(p);
            return c >= 0 && water[c];
        }

        public override WaterHit? NearestWater(Vector3 p, float radius)
        {
            int c = grid.Nearest(water, p, radius, this, out float d);
            if (c < 0) return null;
            var w = grid.Center(c);
            int shore = grid.Nearest(walkable, w, cellSize * 3f, this, out _);
            var s = shore >= 0 ? OnGround(grid.Center(shore)) : p;
            return new WaterHit(OnGround(w), s, d);
        }

        /// <summary>Share of cells that are walkable, for checks and the inspector.</summary>
        public float WalkableShare
        {
            get
            {
                int n = 0;
                foreach (var w in walkable) if (w) n++;
                return grid.Count == 0 ? 0f : (float)n / grid.Count;
            }
        }

        public void Configure(Terrain t, float water, float steepness, float footprint = 0.5f)
        {
            terrain = t;
            waterLevel = water;
            maxSteepness = steepness;
            treeFootprint = footprint;
        }

        public override void Validate(ValidationReport report)
        {
            if (terrain == null) report.Error("V-20", this, "TerrainGround needs a Terrain on its GameObject or below it.");
        }
    }
}
