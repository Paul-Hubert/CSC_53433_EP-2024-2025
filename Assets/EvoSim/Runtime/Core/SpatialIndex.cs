using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Animals by species in a uniform hash grid (20 §3.11, SENSE-32): rebuilt by the sense and act phases, updated
    /// as animals move in a sequential act order. Nearest queries search expanding rings and break ties by id
    /// (SPACE-07, SPACE-08). The ring bound assumes the world distance is at least the horizontal Euclidean one.
    /// </summary>
    public sealed class SpatialIndex
    {
        public const float CellSize = 4f;

        readonly World world;
        readonly CellGrid grid;
        readonly Dictionary<Species, List<Animal>[]> cellsOf = new Dictionary<Species, List<Animal>[]>();

        public SpatialIndex(World world, Rect bounds)
        {
            this.world = world;
            grid = new CellGrid(bounds, CellSize);
        }

        /// <summary>Every living animal of every species, placed again (once per sense and act phase).</summary>
        public void Rebuild()
        {
            foreach (var s in world.AllSpecies)
            {
                var cells = CellsOf(s);
                for (int c = 0; c < cells.Length; c++) cells[c]?.Clear();
                var animals = s.Animals;
                for (int i = 0; i < animals.Count; i++)
                {
                    var a = animals[i];
                    a.SpatialCell = -1;
                    if (!a.IsGone) Put(cells, a);
                }
            }
        }

        /// <summary>A new animal (born, hatched, newcomer) enters the index.</summary>
        public void Added(Animal a)
        {
            if (!a.IsGone) Put(CellsOf(a.Species), a);
        }

        /// <summary>An animal moved: its cell is updated, so later actors see it where it is (ACT-31).</summary>
        public void Moved(Animal a)
        {
            int c = Cell(a.Position);
            if (c == a.SpatialCell) return;
            var cells = CellsOf(a.Species);
            if (a.SpatialCell >= 0) cells[a.SpatialCell]?.Remove(a);
            a.SpatialCell = -1;
            Put(cells, a);
        }

        /// <summary>
        /// The nearest animal of the given species within radius that the filter accepts. The viewer itself,
        /// killed animals and gone animals are never candidates (SPACE-07, ANIM-41). Ties go to the lower id.
        /// </summary>
        public bool Nearest(Animal viewer, Vector3 p, float radius, IReadOnlyList<Species> among,
                            Func<Animal, Animal, bool> accept, out AnimalHit hit)
        {
            Animal best = null;
            float bestD = float.PositiveInfinity;
            int cx = grid.X(Cell(p)), cz = grid.Z(Cell(p));
            int maxRing = Mathf.CeilToInt(radius / CellSize) + 1;
            for (int k = 0; k <= maxRing; k++)
            {
                float ringMin = (k - 1) * CellSize;                   // p may sit anywhere in its own cell
                if (k > 1 && (ringMin > radius + 1e-5f || ringMin > bestD + 1e-5f)) break;
                for (int dz = -k; dz <= k; dz++)
                {
                    int z = cz + dz;
                    if (z < 0 || z >= grid.Height) continue;
                    bool edgeRow = dz == -k || dz == k;
                    for (int dx = -k; dx <= k; dx += edgeRow ? 1 : 2 * k)
                    {
                        int x = cx + dx;
                        if (x >= 0 && x < grid.Width)
                        {
                            int c = z * grid.Width + x;
                            for (int si = 0; si < among.Count; si++)
                            {
                                var list = CellsOf(among[si])[c];
                                if (list == null) continue;
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var a = list[i];
                                    if (a == viewer || a.IsGone || a.Killed) continue;
                                    float d = world.Distance(p, a.Position);
                                    if (d > radius) continue;
                                    if (best != null && (d > bestD + 1e-6f || (Mathf.Abs(d - bestD) <= 1e-6f && a.Id > best.Id))) continue;
                                    if (accept != null && !accept(a, viewer)) continue;
                                    best = a;
                                    bestD = d;
                                }
                            }
                        }
                        if (k == 0) break;
                    }
                }
            }
            hit = new AnimalHit(best, best != null ? bestD : float.PositiveInfinity);
            return best != null;
        }

        /// <summary>Every animal of the given species within radius, in no particular order (for counts and tests).</summary>
        public void Within(Vector3 p, float radius, IReadOnlyList<Species> among, List<Animal> result)
        {
            result.Clear();
            foreach (var s in among)
                foreach (var a in s.Animals)
                    if (!a.IsGone && world.Distance(p, a.Position) <= radius) result.Add(a);
        }

        int Cell(Vector3 p)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((p.x - grid.X0) / CellSize), 0, grid.Width - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((p.z - grid.Z0) / CellSize), 0, grid.Height - 1);
            return z * grid.Width + x;
        }

        void Put(List<Animal>[] cells, Animal a)
        {
            int c = Cell(a.Position);
            (cells[c] ??= new List<Animal>()).Add(a);
            a.SpatialCell = c;
        }

        List<Animal>[] CellsOf(Species s)
        {
            if (!cellsOf.TryGetValue(s, out var cells))
            {
                cells = new List<Animal>[grid.Count];
                cellsOf.Add(s, cells);
            }
            return cells;
        }
    }
}
