using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A grid of square cells over the world rectangle, used inside layers (food cells, cover cells) — never to
    /// place animals (02 intro). Finds the nearest marked cell by expanding rings (20 §9).
    /// </summary>
    public sealed class CellGrid
    {
        public readonly float X0, Z0, CellSize;
        public readonly int Width, Height;
        public int Count => Width * Height;

        public CellGrid(Rect bounds, float cellSize)
        {
            CellSize = Mathf.Max(0.01f, cellSize);
            X0 = bounds.xMin;
            Z0 = bounds.yMin;
            Width = Mathf.Max(1, Mathf.CeilToInt(bounds.width / CellSize - 1e-4f));
            Height = Mathf.Max(1, Mathf.CeilToInt(bounds.height / CellSize - 1e-4f));
        }

        /// <summary>The cell containing a point, or -1 outside.</summary>
        public int CellOf(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - X0) / CellSize), z = Mathf.FloorToInt((p.z - Z0) / CellSize);
            return x < 0 || z < 0 || x >= Width || z >= Height ? -1 : z * Width + x;
        }

        public int X(int cell) => cell % Width;
        public int Z(int cell) => cell / Width;
        public int Index(int x, int z) => z * Width + x;

        /// <summary>The centre of a cell, at height 0.</summary>
        public Vector3 Center(int cell) => new Vector3(X0 + (X(cell) + 0.5f) * CellSize, 0f, Z0 + (Z(cell) + 0.5f) * CellSize);

        /// <summary>
        /// The marked cell whose centre is nearest to p within radius, by the ground's distance (SPACE-02);
        /// ties go to the lower cell index (SPACE-08). Returns -1 if none.
        /// </summary>
        public int Nearest(bool[] mask, Vector3 p, float radius, Ground ground, out float distance)
        {
            distance = float.PositiveInfinity;
            int best = -1;
            int cx = Mathf.Clamp(Mathf.FloorToInt((p.x - X0) / CellSize), 0, Width - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((p.z - Z0) / CellSize), 0, Height - 1);
            int maxRing = Mathf.CeilToInt(radius / CellSize) + 1;
            for (int k = 0; k <= maxRing; k++)
            {
                float ringMin = (k - 0.5f) * CellSize;
                if (k > 0 && (ringMin > radius + 1e-5f || ringMin > distance + 1e-5f)) break;
                for (int dz = -k; dz <= k; dz++)
                {
                    int z = cz + dz;
                    if (z < 0 || z >= Height) continue;
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

        static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
