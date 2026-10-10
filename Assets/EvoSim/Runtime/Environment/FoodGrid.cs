using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The Lab 1 food layer (03 §1): at most one item per cell, at the cell centre; a share of walkable cells starts
    /// with an item; empty cells regrow at random, uniformly; no food in cover ("hungry cover", ENV-04).
    /// </summary>
    public class FoodGrid : ResourceLayer
    {
        [SerializeField, Min(0.25f), Tooltip("Cell size, in meters (reference 1).")]
        float cellSize = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Share of eligible cells holding an item at the start (reference 0.1).")]
        float initialFraction = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Probability per empty eligible cell per tick of growing an item (reference 0.0015).")]
        float regrowP = 0.0015f;
        [SerializeField, Tooltip("No item at the start and no regrowth in cover (reference: on, ENV-04).")]
        bool noFoodInCover = true;
        [SerializeField, Min(1f), Tooltip("Regrowth multiplier near water, terrain only (reference 2).")]
        float nearWaterBonus = 2f;
        [SerializeField, Min(0f), Tooltip("Distance to water for the bonus, in meters (reference 3).")]
        float nearWaterRadius = 3f;

        CellGrid grid;
        bool[] items;
        bool[] eligible;
        bool[] nearWater;
        int[] eligibleCells;
        int count;

        /// <summary>A multiplier on regrowth set by other phases (seasons, 22 §9); 1 by default.</summary>
        public float RegrowFactor { get; set; } = 1f;
        public override int Count => count;
        public CellGrid Grid => grid;
        public float RegrowP => regrowP;
        public int EligibleCount => eligibleCells?.Length ?? 0;

        public override void Initialize()
        {
            var ground = World.Ground;
            grid = new CellGrid(ground != null ? ground.Bounds : new Rect(0, 0, 1, 1), cellSize);
            items = new bool[grid.Count];
            eligible = new bool[grid.Count];
            nearWater = new bool[grid.Count];
            eligibleCells = System.Array.Empty<int>();
            count = 0;
            RegrowFactor = 1f;
        }

        /// <summary>After cover exists: which cells may hold food, then the first items (world stream, RAND-03/04).</summary>
        public override void Begin()
        {
            var ground = World.Ground;
            var cover = World.Service<Cover>();
            var list = new List<int>();
            var water = new bool[grid.Count];
            bool anyWater = false;
            for (int c = 0; c < grid.Count; c++)
            {
                var p = grid.Center(c);
                if (ground != null && ground.IsWater(p)) { water[c] = true; anyWater = true; }
                if (ground != null && !ground.IsWalkable(p)) continue;
                if (noFoodInCover && cover != null && cover.InCover(p)) continue;
                eligible[c] = true;
                list.Add(c);
            }
            eligibleCells = list.ToArray();
            if (anyWater && nearWaterRadius > 0f)
                foreach (int c in eligibleCells)
                    nearWater[c] = grid.Nearest(water, grid.Center(c), nearWaterRadius, ground, out _) >= 0;

            var rng = World.Random.Get("world/" + ServiceName);
            foreach (int c in eligibleCells)
                if (rng.NextDouble() < initialFraction) { items[c] = true; count++; }
        }

        public override bool Nearest(Vector3 p, float radius, out ResourceItem item)
        {
            int c = grid.Nearest(items, p, radius, World.Ground, out float d);
            item = c >= 0 ? new ResourceItem(this, c, OnGround(grid.Center(c)), d) : default;
            return c >= 0;
        }

        public override bool Consume(ResourceItem item)
        {
            if (item.Layer != this || item.Cell < 0 || !items[item.Cell]) return false;
            items[item.Cell] = false;
            count--;
            return true;
        }

        /// <summary>Each empty eligible cell grows an item with probability regrowP (× the near-water bonus).</summary>
        public override void Regrow(RandomStream rng)
        {
            double p = regrowP * RegrowFactor;
            if (p <= 0) return;
            foreach (int c in eligibleCells)
            {
                if (items[c]) continue;
                double pc = nearWater[c] ? p * nearWaterBonus : p;
                if (rng.NextDouble() < pc) { items[c] = true; count++; }
            }
        }

        public bool HasItem(int cell) => items[cell];
        public bool IsEligible(int cell) => eligible[cell];

        /// <summary>Puts or removes an item by hand (Place in tests); returns whether the cell is eligible.</summary>
        public bool SetItem(Vector3 p, bool value)
        {
            int c = grid.CellOf(p);
            if (c < 0 || items[c] == value) return c >= 0;
            items[c] = value;
            count += value ? 1 : -1;
            return eligible[c];
        }

        /// <summary>Empties the layer (tests).</summary>
        public void Clear()
        {
            System.Array.Clear(items, 0, items.Length);
            count = 0;
        }

        /// <summary>Sets the reference values from code (WorldBuilder, scenarios).</summary>
        public void Configure(float initial, float regrow, bool hungryCover = true)
        {
            initialFraction = initial;
            regrowP = regrow;
            noFoodInCover = hungryCover;
        }

        Vector3 OnGround(Vector3 p) => World.Ground != null ? World.Ground.OnGround(p) : p;

        public override void Validate(ValidationReport report)
        {
            if (!(initialFraction >= 0 && initialFraction <= 1 && regrowP >= 0 && regrowP <= 1))
                report.Error("V-35", this, "Food fractions and probabilities must be in [0, 1].", new ValidationFix("Clamp", () =>
                {
                    initialFraction = float.IsNaN(initialFraction) ? 0f : Mathf.Clamp01(initialFraction);
                    regrowP = float.IsNaN(regrowP) ? 0f : Mathf.Clamp01(regrowP);
                }));
        }
    }
}
