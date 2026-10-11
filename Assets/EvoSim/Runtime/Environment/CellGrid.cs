using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A grid of square cells over the world rectangle, used inside layers (food cells, cover cells) — never to
    /// place animals (02 intro). Finds the nearest marked cell by expanding rings (20 §9).
    /// </summary>
    /// <remarks>
    /// <para>Cells are numbered row by row: cell = z × <see cref="Width"/> + x, starting at the rectangle's minimum
    /// corner. A layer keeps one value per cell in its own array (a <c>bool[]</c> of length <see cref="Count"/>).</para>
    /// </remarks>
    public sealed class CellGrid
    {
        /// <summary>
        /// <c>X0</c> and <c>Z0</c>: the world x and z of the grid's minimum corner, in meters; <c>CellSize</c>: the side of a
        /// cell, in meters (at least 0.01).
        /// </summary>
        public readonly float X0, Z0, CellSize;
        /// <summary><c>Width</c>: cells along x; <c>Height</c>: cells along z (at least 1 each).</summary>
        public readonly int Width, Height;
        /// <summary>Number of cells: <see cref="Width"/> × <see cref="Height"/>.</summary>
        public int Count => Width * Height;

        /// <summary>A grid that covers a rectangle; the last row and column may stick out past its far edges.</summary>
        /// <param name="bounds">The rectangle on the horizontal plane (x is world x, y is world z), usually <see cref="Ground.Bounds"/>.</param>
        /// <param name="cellSize">The side of a cell, in meters.</param>
        public CellGrid(Rect bounds, float cellSize)
        {
            CellSize = Mathf.Max(0.01f, cellSize);
            X0 = bounds.xMin;
            Z0 = bounds.yMin;
            Width = Mathf.Max(1, Mathf.CeilToInt(bounds.width / CellSize - 1e-4f));
            Height = Mathf.Max(1, Mathf.CeilToInt(bounds.height / CellSize - 1e-4f));
        }

        /// <summary>Finds the cell that contains a point (its height is ignored).</summary>
        /// <param name="p">A world point.</param>
        /// <returns>The cell index, or -1 when the point is outside the grid.</returns>
        public int CellOf(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - X0) / CellSize), z = Mathf.FloorToInt((p.z - Z0) / CellSize);
            return x < 0 || z < 0 || x >= Width || z >= Height ? -1 : z * Width + x;
        }

        /// <summary>The column of a cell.</summary>
        /// <param name="cell">A cell index.</param>
        /// <returns>Its x position in the grid, from 0 to <see cref="Width"/> - 1.</returns>
        public int X(int cell) => cell % Width;
        /// <summary>The row of a cell.</summary>
        /// <param name="cell">A cell index.</param>
        /// <returns>Its z position in the grid, from 0 to <see cref="Height"/> - 1.</returns>
        public int Z(int cell) => cell / Width;
        /// <summary>The index of the cell at a column and row; nothing is checked.</summary>
        /// <param name="x">The column, from 0 to <see cref="Width"/> - 1.</param>
        /// <param name="z">The row, from 0 to <see cref="Height"/> - 1.</param>
        /// <returns>The cell index.</returns>
        public int Index(int x, int z) => z * Width + x;

        /// <summary>The centre of a cell, at height 0.</summary>
        /// <param name="cell">A cell index.</param>
        /// <returns>The world point at the cell's centre, with y = 0 (use <see cref="Ground.OnGround"/> to lay it on the ground).</returns>
        public Vector3 Center(int cell) => new Vector3(X0 + (X(cell) + 0.5f) * CellSize, 0f, Z0 + (Z(cell) + 0.5f) * CellSize);

        /// <summary>
        /// The marked cell whose centre is nearest to p within radius, by the ground's distance (SPACE-02);
        /// ties go to the lower cell index (SPACE-08). Returns -1 if none.
        /// </summary>
        /// <remarks>
        /// The search visits rings of cells around p and stops once a ring is farther than the radius or the best cell
        /// found. It prunes with the straight-line distance, so it assumes the ground's distance is never shorter than that.
        /// </remarks>
        /// <param name="mask">One flag per cell (length <see cref="Count"/>): true for the cells that count (an item, cover, water).</param>
        /// <param name="p">The point searched from; a point outside the grid starts from the nearest edge cell.</param>
        /// <param name="radius">The largest distance, in meters (inclusive).</param>
        /// <param name="ground">The ground whose distance is used, or null for the horizontal Euclidean distance.</param>
        /// <param name="distance">The distance to the cell's centre, in meters, or positive infinity when none is found.</param>
        /// <returns>The nearest marked cell, or -1 when none is within the radius.</returns>
        public int Nearest(bool[] mask, Vector3 p, float radius, Ground ground, out float distance)
        {
            distance = float.PositiveInfinity;
            int best = -1;
            int cx = Mathf.Clamp(Mathf.FloorToInt((p.x - X0) / CellSize), 0, Width - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((p.z - Z0) / CellSize), 0, Height - 1);
            int maxRing = Mathf.CeilToInt(radius / CellSize) + 1;
            for (int k = 0; k <= maxRing; k++)
            {
                // The nearest centre of ring k is at least (k - 0.5) cells away: past the radius or the best, stop.
                float ringMin = (k - 0.5f) * CellSize;
                if (k > 0 && (ringMin > radius + 1e-5f || ringMin > distance + 1e-5f)) break;
                for (int dz = -k; dz <= k; dz++)
                {
                    int z = cz + dz;
                    if (z < 0 || z >= Height) continue;
                    // The top and bottom rows of the ring are walked whole; the rows between only at their two ends.
                    bool edgeRow = dz == -k || dz == k;
                    for (int dx = -k; dx <= k; dx += edgeRow ? 1 : 2 * k)
                    {
                        int x = cx + dx;
                        if (x >= 0 && x < Width)
                        {
                            int c = z * Width + x;
                            if (mask[c])
                            {
                                float d = ground != null ? ground.Distance(p, Center(c)) : Horizontal(p, Center(c));
                                if (d <= radius && (d < distance - 1e-6f || (Mathf.Abs(d - distance) <= 1e-6f && c < best)))
                                {
                                    best = c;
                                    distance = d;
                                }
                            }
                        }
                        if (k == 0) break;
                    }
                }
            }
            if (best < 0) distance = float.PositiveInfinity;
            return best;
        }

        /// <summary>The horizontal Euclidean distance between two points, in meters (used when no ground is given).</summary>
        static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
