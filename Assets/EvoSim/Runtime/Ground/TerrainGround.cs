using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A Unity Terrain as ground (02 §1): heights from the heightmap; walkable above the water level, below a
    /// maximum steepness, outside tree footprints; only the largest connected walkable region is kept (SPACE-05).
    /// Walkability is computed once, on a grid of cells.
    /// </summary>
    /// <remarks>
    /// <para>Put it on the World's "Ground" GameObject with a Terrain on it or below it. Each cell is judged at its centre:
    /// at or below the water level it is water; steeper than the maximum it is blocked; otherwise walkable. Then tree
    /// footprints are blocked, and pockets cut off from the largest region are dropped. Water and steep cells are never
    /// walkable.</para>
    /// <para>The terrain must not change during a run: the grid is built once, in <see cref="Initialize"/>.</para>
    /// </remarks>
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
        bool[] walkable, water;     // per cell of grid: walkable; water (at or below the water level)
        Rect bounds;
        Vector3 terrainOrigin;      // the terrain's world position: heights are relative to its y

        /// <summary>The terrain used, once found (after <see cref="Initialize"/> when the field was empty).</summary>
        public Terrain Terrain => terrain;
        /// <summary>The walkability grid over the terrain's rectangle.</summary>
        public CellGrid Grid => grid;
        /// <summary>The world height of the water surface, in meters.</summary>
        public float WaterLevel => waterLevel;
        /// <summary>The steepest walkable ground, in degrees.</summary>
        public float MaxSteepness => maxSteepness;
        /// <summary>The terrain's rectangle, from its position and size; 1 × 1 m at the origin without a terrain.</summary>
        /// <inheritdoc/>
        public override Rect Bounds => bounds;

        /// <summary>Finds the terrain and computes, once, which cells are water and which are walkable.</summary>
        /// <inheritdoc/>
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

        /// <summary>Makes the cells within the tree footprint of every tree instance non-walkable.</summary>
        void BlockTrees(TerrainData data)
        {
            foreach (var tree in data.treeInstances)
            {
                // Tree positions are normalised to the terrain (0 to 1 on each axis).
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

        /// <summary>The terrain's world height under a point, in meters; 0 without a terrain.</summary>
        float SampleHeight(Vector3 p) => terrain != null ? terrain.SampleHeight(p) + terrainOrigin.y : 0f;

        /// <summary>The terrain's height under the point, from the heightmap (exact, not per cell).</summary>
        /// <inheritdoc/>
        public override float Height(Vector3 p) => SampleHeight(p);

        /// <summary>True when the point is inside and its cell is walkable.</summary>
        /// <inheritdoc/>
        public override bool IsWalkable(Vector3 p)
        {
            int c = grid.CellOf(p);
            return c >= 0 && walkable[c] && Inside(p);
        }

        /// <summary>True when the point's cell lies at or below the water level.</summary>
        /// <inheritdoc/>
        public override bool IsWater(Vector3 p)
        {
            int c = grid.CellOf(p);
            return c >= 0 && water[c];
        }

        /// <summary>
        /// The centre of the nearest water cell within the radius, and the nearest walkable cell centre within 3 cells of it
        /// as the shore (the query point when there is none).
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>Sets the terrain and its limits from code (scene builders, tests), before the World initialises.</summary>
        /// <param name="t">The terrain.</param>
        /// <param name="water">The world height of the water surface, in meters.</param>
        /// <param name="steepness">The steepest walkable ground, in degrees.</param>
        /// <param name="footprint">The radius around each tree that animals can't enter, in meters (0 = trees don't block).</param>
        public void Configure(Terrain t, float water, float steepness, float footprint = 0.5f)
        {
            terrain = t;
            waterLevel = water;
            maxSteepness = steepness;
            treeFootprint = footprint;
        }

        /// <summary>Reports a missing terrain (V-20).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (terrain == null) report.Error("V-20", this, "TerrainGround needs a Terrain on its GameObject or below it.");
        }
    }
}
