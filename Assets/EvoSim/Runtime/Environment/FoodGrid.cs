using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The Lab 1 food layer (03 §1): at most one item per cell, at the cell centre; a share of walkable cells starts
    /// with an item; empty cells regrow at random, uniformly; no food in cover ("hungry cover", ENV-04).
    /// </summary>
    /// <remarks>
    /// <para>Place it under the World's Environment object with an <see cref="Edible"/> (reference: "Grass", grazed, 25
    /// per item); diets and senses name it by its GameObject's name.</para>
    /// <para>Lifecycle: <see cref="Initialize"/> lays the grid over the ground; <see cref="Begin"/>, once cover exists,
    /// picks the cells that may hold food and the first items from the stream "world/&lt;layer name&gt;" (world seed,
    /// RAND-04); <see cref="Regrow"/> runs once per tick in the environment phase with the layer's own stream (ENV-03).</para>
    /// <para>On a terrain, cells within <c>nearWaterRadius</c> of water regrow <c>nearWaterBonus</c> times faster.</para>
    /// </remarks>
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
        bool[] items;           // per cell: holds an item now
        bool[] eligible;        // per cell: may hold food (walkable, and outside cover when noFoodInCover)
        bool[] nearWater;       // per cell: within nearWaterRadius of water, so regrows faster
        int[] eligibleCells;    // the eligible cells, in index order: regrowth visits them in this order
        int count;

        /// <summary>A multiplier on regrowth set by other phases (seasons, 22 §9); 1 by default.</summary>
        public float RegrowFactor { get; set; } = 1f;
        /// <inheritdoc/>
        public override int Count => count;
        /// <summary>The food cells' grid over the ground's rectangle.</summary>
        public CellGrid Grid => grid;
        /// <summary>The probability that an empty eligible cell grows an item in one tick, before the factors.</summary>
        public float RegrowP => regrowP;
        /// <summary>How many cells may hold food (known after <see cref="Begin"/>; 0 before).</summary>
        public int EligibleCount => eligibleCells?.Length ?? 0;

        /// <summary>Lays an empty grid over the ground's rectangle and resets <see cref="RegrowFactor"/> to 1; no item yet.</summary>
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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

        /// <summary>The nearest cell holding an item, by the world distance to the cell centre; ties to the lower cell index.</summary>
        /// <inheritdoc/>
        public override bool Nearest(Vector3 p, float radius, out ResourceItem item)
        {
            int c = grid.Nearest(items, p, radius, World.Ground, out float d);
            item = c >= 0 ? new ResourceItem(this, c, OnGround(grid.Center(c)), d) : default;
            return c >= 0;
        }

        /// <inheritdoc/>
        public override bool Consume(ResourceItem item)
        {
            if (item.Layer != this || item.Cell < 0 || !items[item.Cell]) return false;
            items[item.Cell] = false;
            count--;
            return true;
        }

        /// <summary>Each empty eligible cell grows an item with probability regrowP (× the near-water bonus).</summary>
        /// <remarks>The probability is also multiplied by <see cref="RegrowFactor"/>; one draw per empty eligible cell, in cell order.</remarks>
        /// <inheritdoc/>
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

        /// <summary>Checks whether a cell holds an item now.</summary>
        /// <param name="cell">A cell index of <see cref="Grid"/>.</param>
        /// <returns>True when the cell holds an item.</returns>
        public bool HasItem(int cell) => items[cell];
        /// <summary>Checks whether a cell may hold food (walkable, and outside cover when food is kept out of cover).</summary>
        /// <param name="cell">A cell index of <see cref="Grid"/>.</param>
        /// <returns>True when the cell may hold food and regrows.</returns>
        public bool IsEligible(int cell) => eligible[cell];

        /// <summary>Puts or removes an item by hand (Place in tests); returns whether the cell is eligible.</summary>
        /// <remarks>An item may be put on a cell that is not eligible: it can be eaten but never regrows there.</remarks>
        /// <param name="p">A point in the cell to change.</param>
        /// <param name="value">True to put an item, false to remove it.</param>
        /// <returns>
        /// Whether the cell is eligible; false outside the grid. When the cell already was as asked, true for any cell of the
        /// grid.
        /// </returns>
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
        /// <remarks>Call it before the World initialises: the items are placed in <see cref="Begin"/>.</remarks>
        /// <param name="initial">Share of eligible cells holding an item at the start, from 0 to 1.</param>
        /// <param name="regrow">Probability per empty eligible cell per tick of growing an item, from 0 to 1.</param>
        /// <param name="hungryCover">True to keep cover free of food (the reference).</param>
        public void Configure(float initial, float regrow, bool hungryCover = true)
        {
            initialFraction = initial;
            regrowP = regrow;
            noFoodInCover = hungryCover;
        }

        /// <summary>The point with its height set from the ground, if there is one.</summary>
        Vector3 OnGround(Vector3 p) => World.Ground != null ? World.Ground.OnGround(p) : p;

        /// <summary>Reports a start fraction or regrowth probability outside [0, 1] (V-35), with a fix that clamps them.</summary>
        /// <inheritdoc/>
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
