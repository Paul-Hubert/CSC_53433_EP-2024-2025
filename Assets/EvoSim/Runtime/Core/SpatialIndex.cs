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
    /// <remarks>
    /// Reach it with <see cref="World.Space"/>. Senses and actions usually go through <see cref="WorldQueries"/>,
    /// which adds vision, cover and diet checks. A phase that moves animals must call <see cref="Moved"/> after each
    /// move, or later queries in the same phase see the animal where it was (ACT-31).
    /// </remarks>
    public sealed class SpatialIndex
    {
        /// <summary>The side of a grid cell, in meters.</summary>
        public const float CellSize = 4f;

        readonly World world;                                                  // whose species and distance it uses
        readonly CellGrid grid;                                                // the cells over the ground's rectangle
        // Per species: the animals in each cell.
        readonly Dictionary<Species, List<Animal>[]> cellsOf = new Dictionary<Species, List<Animal>[]>();

        /// <summary>Makes an empty index over a rectangle; <see cref="World.Space"/> makes the World's.</summary>
        /// <param name="world">The World whose animals it indexes and whose distance it measures with.</param>
        /// <param name="bounds">The world rectangle on the horizontal plane (x is world x, y is world z), in meters.</param>
        public SpatialIndex(World world, Rect bounds)
        {
            this.world = world;
            grid = new CellGrid(bounds, CellSize);
        }

        /// <summary>Every living animal of every species, placed again (once per sense and act phase).</summary>
        public void Rebuild()
        {
            var all = world.AllSpecies;
            for (int si = 0; si < all.Count; si++)
            {
                var s = all[si];
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
        /// <param name="a">The animal; nothing happens when it is gone.</param>
        public void Added(Animal a)
        {
            if (!a.IsGone) Put(CellsOf(a.Species), a);
        }

        /// <summary>An animal moved: its cell is updated, so later actors see it where it is (ACT-31).</summary>
        /// <param name="a">The animal, at its new position.</param>
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
        /// <param name="viewer">The animal that asks: never found itself, and passed to the filter.</param>
        /// <param name="p">The point to measure from, usually the viewer's position.</param>
        /// <param name="radius">The farthest distance, in meters (world distance, <see cref="World.Distance"/>).</param>
        /// <param name="among">The species to search.</param>
        /// <param name="accept">A filter given the candidate and the viewer (visibility, readiness…), or null for none.</param>
        /// <param name="hit">The animal found and its distance; an empty hit when none.</param>
        /// <returns>True when an animal was found.</returns>
        public bool Nearest(Animal viewer, Vector3 p, float radius, IReadOnlyList<Species> among,
                            Func<Animal, Animal, bool> accept, out AnimalHit hit)
        {
            Animal best = null;
            float bestD = float.PositiveInfinity;
            int cx = grid.X(Cell(p)), cz = grid.Z(Cell(p));
            int maxRing = Mathf.CeilToInt(radius / CellSize) + 1;
            for (int k = 0; k <= maxRing; k++)
            {
                // Ring k holds the cells k steps away; nothing in it is nearer than (k - 1) cells.
                float ringMin = (k - 1) * CellSize;                   // p may sit anywhere in its own cell
                if (k > 1 && (ringMin > radius + 1e-5f || ringMin > bestD + 1e-5f)) break;
                for (int dz = -k; dz <= k; dz++)
                {
                    int z = cz + dz;
                    if (z < 0 || z >= grid.Height) continue;
                    bool edgeRow = dz == -k || dz == k;
                    for (int dx = -k; dx <= k; dx += edgeRow ? 1 : 2 * k)   // inner rows: only the two edge cells
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
                                    // Keep the nearer; at an equal distance the lower id (SPACE-08).
                                    // The filter runs last: it may cost more.
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
        /// <param name="p">The point to measure from.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <param name="among">The species to search.</param>
        /// <param name="result">Cleared, then filled with the animals found (gone ones left out; killed ones and any
        /// viewer included).</param>
        /// <remarks>It checks every animal of those species: use it for counts and tests, not every tick for every animal.</remarks>
        public void Within(Vector3 p, float radius, IReadOnlyList<Species> among, List<Animal> result)
        {
            result.Clear();
            for (int si = 0; si < among.Count; si++)
            {
                var animals = among[si].Animals;
                for (int i = 0; i < animals.Count; i++)
                    if (!animals[i].IsGone && world.Distance(p, animals[i].Position) <= radius) result.Add(animals[i]);
            }
        }

        /// <summary>The cell of a point; points outside the rectangle go to the nearest border cell.</summary>
        int Cell(Vector3 p)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((p.x - grid.X0) / CellSize), 0, grid.Width - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((p.z - grid.Z0) / CellSize), 0, grid.Height - 1);
            return z * grid.Width + x;
        }

        /// <summary>Puts an animal in the cell of its position and remembers the cell on it.</summary>
        void Put(List<Animal>[] cells, Animal a)
        {
            int c = Cell(a.Position);
            cells[c].Add(a);
            a.SpatialCell = c;
        }

        /// <summary>The cells of one species, made on first use.</summary>
        List<Animal>[] CellsOf(Species s)
        {
            if (!cellsOf.TryGetValue(s, out var cells))
            {
                cells = new List<Animal>[grid.Count];
                for (int c = 0; c < cells.Length; c++) cells[c] = new List<Animal>(16);   // made once and roomy: moving never allocates (R-07)
                cellsOf.Add(s, cells);
            }
            return cells;
        }
    }
}
