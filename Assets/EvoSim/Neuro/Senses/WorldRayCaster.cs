using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// Casts rays through the world model, not through Physics (plan M16 D3, §5.4): animals, food and carcasses are data, so
    /// each ray stops at the first EvoSim object along it.
    /// </summary>
    /// <remarks>
    /// <para>What a ray can meet, nearest first: a living animal not hidden from the viewer (SENSE-06; a circle of
    /// <see cref="BodyRadius"/>), an entity not used up (same circle), an item of a resource layer (a walk over the
    /// <see cref="FoodGrid"/> cells, a march with <see cref="ResourceLayer.WithinReach"/> for other layers), the edge of a
    /// cover patch seen from outside (a walk over the <see cref="CoverLayer"/> cells, a march for other covers), and the
    /// ground: the world's edge, water, ground that can't be walked on.</para>
    /// <para>An object's distance is where its centre projects on the ray, so food 3 m ahead reads 3 m. Rays start at
    /// <see cref="BodyRadius"/>: what lies under or beside the animal is not seen. At equal distance the lower
    /// <see cref="RayHitKind"/> wins, then the lower id (SPACE-08).</para>
    /// <para>It reads only and draws no random number (SENSE-03). It keeps scratch lists, so use one caster from one thread.</para>
    /// </remarks>
    public sealed class WorldRayCaster
    {
        /// <summary>Distances closer than this are equal (meters).</summary>
        public const float Epsilon = 1e-4f;

        /// <summary>The radius of an animal's or entity's circle, and where rays start, in meters (plan: 0.5).</summary>
        public float BodyRadius = 0.5f;
        /// <summary>The step of a march (other layers, other covers, the ground), in meters (plan: 0.25).</summary>
        public float MarchStep = 0.25f;

        /// <summary>True (default): animals are binned per sense context and sorted into per-ray lists; false: every ray tests
        /// every animal. Both give the same hits (tests compare them).</summary>
        public bool UseBuckets = true;
        /// <summary>True: a kin ready to mate reads <see cref="RayHitKind.ReadyKin"/>.</summary>
        public bool ReadyKin;
        /// <summary>True: water reads <see cref="RayHitKind.Water"/> instead of <see cref="RayHitKind.Obstacle"/>.</summary>
        public bool SplitWater;

        /// <summary>An object near the viewer that a ray may meet: its place, its kind and its tie-break id.</summary>
        struct Candidate { public float X, Z; public RayHitKind Kind; public int Id; }

        readonly World world;
        readonly List<Candidate> near = new List<Candidate>();          // this cast's animals and entities within range
        readonly List<ResourceLayer> layers = new List<ResourceLayer>();
        readonly List<Cover> covers = new List<Cover>();
        readonly List<EntityKind> entityKinds = new List<EntityKind>();
        bool bound;                                                     // the services were listed

        /// <summary>On a plain flat ground, how far inside the edges the march may skip its tests (meters, M16.8).</summary>
        const float SureMargin = 0.01f;
        /// <summary>The side of a cell of the animal bins (meters).</summary>
        const float BinSize = 4f;

        // The animal bins of one sense context in one tick (M16.8; the sense phase reuses its context): per species, per cell, the indices into its animal list. Made at
        // the first cast with a new context, so a tick's fans share one binning and a tool's fresh context gets a fresh one.
        SenseContext binned;
        int binnedTick;
        readonly List<List<int>[]> bins = new List<List<int>[]>();     // per species, in AllSpecies order
        readonly List<int> found = new List<int>();                     // one species' indices near this cast, sorted
        readonly List<sbyte[]> hidden = new List<sbyte[]>();            // per species: per animal and viewer species, -1 unknown, 0, 1
        Rect binBounds;
        readonly List<List<int>> buckets = new List<List<int>>();       // per ray: the candidates it may meet, in gathered order
        /// <summary>The bearing margin of a ray's candidate list, in degrees (far above float error).</summary>
        const double BucketMargin = 0.5;
        readonly List<float> marchTs = new List<float>();               // the ground march's distances, shared by every ray
        float marchFrom = float.NaN, marchStep = float.NaN;
        int binWidth, binHeight;

        /// <summary>Makes a caster for a world; it lists the world's layers, covers and entity kinds at its first cast.</summary>
        /// <param name="world">The world to cast through.</param>
        public WorldRayCaster(World world) { this.world = world; }

        /// <summary>The ray angles of a fan, in degrees from the heading (clockwise), as <see cref="RayFanSense"/> lays them out.</summary>
        /// <param name="rays">The number of rays, at least 1.</param>
        /// <param name="fov">The fan's width in degrees: at 360 the rays are evenly spaced, ray i at i·360/rays (ray 0 straight
        /// ahead, none doubled at ±180°); below it the fan is symmetric, ray i at −fov/2 + i·fov/(rays−1), one ray straight ahead.</param>
        /// <returns>One angle per ray.</returns>
        public static float[] FanAngles(int rays, float fov)
        {
            rays = Math.Max(1, rays);
            var angles = new float[rays];
            for (int i = 0; i < rays; i++)
            {
                if (fov >= 360f) angles[i] = i * 360f / rays;
                else angles[i] = rays == 1 ? 0f : -fov / 2f + i * fov / (rays - 1);
            }
            return angles;
        }

        /// <summary>Casts one fan from an animal: one hit, or miss, per angle.</summary>
        /// <param name="a">The animal that looks; never seen itself.</param>
        /// <param name="range">How far the rays go, in meters.</param>
        /// <param name="angles">The rays' angles, in degrees from the animal's heading, clockwise.</param>
        /// <param name="hits">Where to write the hits, one per angle.</param>
        /// <param name="context">The tick's sense context, or null. With one, the animals are binned once for every cast
        /// with that context (the world must not change in between: one tick's sense and ask phases); without, every animal
        /// is checked. Both give the same hits.</param>
        public void Cast(Animal a, float range, IReadOnlyList<float> angles, Span<RayHit> hits, SenseContext context = null)
        {
            Bind();
            Gather(a, range, context);
            bool bucketed = UseBuckets && Bucket(a, angles);
            for (int k = 0; k < angles.Count; k++)
            {
                float r = (a.Heading + angles[k]) * Mathf.Deg2Rad;
                hits[k] = CastOne(a, a.Position.x, a.Position.z, Mathf.Sin(r), Mathf.Cos(r), range, bucketed ? buckets[k] : null);
            }
        }

        /// <summary>Lists the world's resource layers, covers and entity kinds once.</summary>
        void Bind()
        {
            if (bound) return;
            bound = true;
            layers.AddRange(world.ServicesOf<ResourceLayer>());
            covers.AddRange(world.ServicesOf<Cover>());
            entityKinds.AddRange(world.ServicesOf<EntityKind>());
        }

        /// <summary>Collects the animals and entities a ray of this cast may meet: within range plus a body radius.</summary>
        void Gather(Animal a, float range, SenseContext context)
        {
            near.Clear();
            float reach = range + BodyRadius, reach2 = reach * reach;
            var me = a.Species;
            var diet = me.Module<Diet>();
            bool useBins = UseBuckets && context != null && Bin(context);
            var all = world.AllSpecies;
            for (int si = 0; si < all.Count; si++)
            {
                var s = all[si];
                RayHitKind kind = s == me ? RayHitKind.Kin
                                : diet != null && diet.Striking(s) != null ? RayHitKind.Prey
                                : world.IsThreat(s, me) ? RayHitKind.Threat
                                : RayHitKind.OtherAnimal;
                var animals = s.Animals;
                if (useBins) Near(si, a.Position, reach);
                int count = useBins ? found.Count : animals.Count;
                for (int j = 0; j < count; j++)
                {
                    var o = animals[useBins ? found[j] : j];
                    if (o == a || !o.IsAlive) continue;                                    // killed: the rays pass through
                    float dx = o.Position.x - a.Position.x, dz = o.Position.z - a.Position.z;
                    if (dx * dx + dz * dz > reach2) continue;
                    if (useBins ? Hidden(si, found[j], o, me, all) : world.Queries.IsHiddenFrom(o, me)) continue;   // SENSE-06
                    var k = kind == RayHitKind.Kin && ReadyKin && world.Queries.IsReady(o) ? RayHitKind.ReadyKin : kind;
                    near.Add(new Candidate { X = o.Position.x, Z = o.Position.z, Kind = k, Id = o.Id });
                }
            }
            foreach (var ek in entityKinds)
            {
                int n = ek.EntityCount;
                for (int i = 0; i < n; i++)
                {
                    var e = ek.EntityAt(i);
                    if (e == null || e.UsedUp) continue;
                    float dx = e.Position.x - a.Position.x, dz = e.Position.z - a.Position.z;
                    if (dx * dx + dz * dz > reach2) continue;
                    near.Add(new Candidate { X = e.Position.x, Z = e.Position.z, Kind = e is Carcass ? RayHitKind.Carcass : RayHitKind.Entity, Id = e.Id });
                }
            }
        }

        /// <summary>
        /// Sorts the gathered candidates into per-ray lists, in their gathered order (M16.8): a candidate goes to every ray
        /// whose angle lies within asin(r / d) of its bearing, plus a margin, which is every ray its exact test can pass. Only
        /// for evenly spaced fans of two rays or more; false (every ray tests every candidate) otherwise.
        /// </summary>
        bool Bucket(Animal a, IReadOnlyList<float> angles)
        {
            int n = angles.Count;
            if (n < 2) return false;
            float step = angles[1] - angles[0];
            if (step <= 0f) return false;
            for (int k = 2; k < n; k++)
                if (Mathf.Abs(angles[k] - angles[k - 1] - step) > 1e-3f) return false;
            bool full = Mathf.Abs(step * n - 360f) < 1e-3f;
            while (buckets.Count < n) buckets.Add(new List<int>());
            for (int k = 0; k < n; k++) buckets[k].Clear();
            double first = a.Heading + angles[0];
            double gapMiddle = step * (n - 1) + (360.0 - step * (n - 1)) / 2.0;   // a partial fan: the middle of the angles it skips
            for (int i = 0; i < near.Count; i++)
            {
                var c = near[i];
                double rx = c.X - a.Position.x, rz = c.Z - a.Position.z;
                double d = Math.Sqrt(rx * rx + rz * rz);
                int lo, hi;
                if (d <= BodyRadius * 1.01) { lo = 0; hi = n - 1; }
                else
                {
                    double half = Math.Asin(Math.Min(1.0, BodyRadius / d)) * (180.0 / Math.PI) + BucketMargin;
                    double rel = Math.Atan2(rx, rz) * (180.0 / Math.PI) - first;     // the ray direction is (sin, cos) of its angle
                    rel -= 360.0 * Math.Floor(rel / 360.0);                             // into [0, 360)
                    if (full)
                    {
                        lo = (int)Math.Ceiling((rel - half) / step);
                        hi = (int)Math.Floor((rel + half) / step);
                    }
                    else
                    {
                        if (rel > gapMiddle) rel -= 360.0;                              // behind the fan's gap: before its first ray
                        lo = Math.Max(0, (int)Math.Ceiling((rel - half) / step));
                        hi = Math.Min(n - 1, (int)Math.Floor((rel + half) / step));
                    }
                }
                if (hi - lo + 1 >= n) { lo = 0; hi = n - 1; }
                for (int k = lo; k <= hi; k++) buckets[((k % n) + n) % n].Add(i);
            }
            return true;
        }

        /// <summary>Bins every animal by cell for this context, once; false when the world has no ground to bin over.</summary>
        bool Bin(SenseContext context)
        {
            if (context == binned && world.Tick == binnedTick) return true;
            var ground = world.Ground;
            if (ground == null) return false;
            var all = world.AllSpecies;
            var b = ground.Bounds;
            int w = Math.Max(1, Mathf.CeilToInt(b.width / BinSize)), h = Math.Max(1, Mathf.CeilToInt(b.height / BinSize));
            if (b != binBounds || w != binWidth || h != binHeight) bins.Clear();
            binBounds = b;
            binWidth = w;
            binHeight = h;
            while (bins.Count < all.Count)
            {
                var cells = new List<int>[w * h];
                for (int c = 0; c < cells.Length; c++) cells[c] = new List<int>(8);
                bins.Add(cells);
            }
            while (hidden.Count < all.Count) hidden.Add(new sbyte[0]);
            for (int si = 0; si < all.Count; si++)
            {
                var cells = bins[si];
                for (int c = 0; c < cells.Length; c++) cells[c].Clear();
                var animals = all[si].Animals;
                for (int i = 0; i < animals.Count; i++) cells[BinOf(animals[i].Position.x, animals[i].Position.z)].Add(i);
                int size = animals.Count * all.Count;
                if (hidden[si].Length < size) hidden[si] = new sbyte[Math.Max(size, hidden[si].Length * 2)];
                for (int k = 0; k < size; k++) hidden[si][k] = -1;
            }
            binned = context;
            binnedTick = world.Tick;
            return true;
        }

        /// <summary>Whether an animal is hidden from a viewer species (SENSE-06), asked once per pair in this context.</summary>
        bool Hidden(int speciesIndex, int animalIndex, Animal o, Species viewer, IReadOnlyList<Species> all)
        {
            int v = 0;
            while (all[v] != viewer) v++;
            int k = animalIndex * all.Count + v;
            var flags = hidden[speciesIndex];
            if (flags[k] < 0) flags[k] = world.Queries.IsHiddenFrom(o, viewer) ? (sbyte)1 : (sbyte)0;
            return flags[k] == 1;
        }

        /// <summary>The bin of a point, clamped to the grid (an animal outside the ground lands in an edge bin).</summary>
        int BinOf(float x, float z)
        {
            int ix = Mathf.Clamp(Mathf.FloorToInt((x - binBounds.xMin) / BinSize), 0, binWidth - 1);
            int iz = Mathf.Clamp(Mathf.FloorToInt((z - binBounds.yMin) / BinSize), 0, binHeight - 1);
            return iz * binWidth + ix;
        }

        /// <summary>
        /// Lists the indices of one species' animals in the bins a circle of radius <paramref name="reach"/> around p
        /// touches, ascending: the order of the full scan, so ties resolve as they do without bins.
        /// </summary>
        void Near(int speciesIndex, Vector3 p, float reach)
        {
            found.Clear();
            var cells = bins[speciesIndex];
            int x0 = BinOf(p.x - reach, p.z - reach), x1 = BinOf(p.x + reach, p.z + reach);
            int ix0 = x0 % binWidth, iz0 = x0 / binWidth, ix1 = x1 % binWidth, iz1 = x1 / binWidth;
            for (int iz = iz0; iz <= iz1; iz++)
                for (int ix = ix0; ix <= ix1; ix++)
                    found.AddRange(cells[iz * binWidth + ix]);
            found.Sort();
        }

        /// <summary>The first object along one ray from (ox, oz), direction (dx, dz) of length 1.</summary>
        RayHit CastOne(Animal a, float ox, float oz, float dx, float dz, float range, List<int> only)
        {
            float start = BodyRadius;
            var best = RayHit.Miss(range);

            // Animals and entities: circles, at the projection of their centre.
            float r2 = BodyRadius * BodyRadius;
            int count = only != null ? only.Count : near.Count;
            for (int j = 0; j < count; j++)
            {
                var c = near[only != null ? only[j] : j];
                float rx = c.X - ox, rz = c.Z - oz;
                float t = rx * dx + rz * dz;
                if (t < start || t > range) continue;
                float perp2 = rx * rx + rz * rz - t * t;
                if (perp2 > r2) continue;
                Offer(ref best, new RayHit(c.Kind, t, c.Id));
            }

            // Resource items.
            var diet = a.Species.Module<Diet>();
            foreach (var layer in layers)
            {
                var kind = diet != null && diet.Grazing(layer) != null ? RayHitKind.Food : RayHitKind.Item;
                float limit = best.Distance + Epsilon;
                if (layer is FoodGrid grid) Offer(ref best, FoodAlong(grid, kind, ox, oz, dx, dz, start, limit));
                else Offer(ref best, MarchLayer(layer, kind, ox, oz, dx, dz, start, limit));
            }

            // Cover patches, seen from outside.
            foreach (var cover in covers)
            {
                float limit = best.Distance + Epsilon;
                if (cover is CoverLayer cl) Offer(ref best, CoverAlong(cl, ox, oz, dx, dz, start, limit));
                else Offer(ref best, MarchCover(cover, ox, oz, dx, dz, start, limit));
            }

            // The ground: its edge exactly, water and unwalkable ground by marching.
            var ground = world.Ground;
            if (ground != null) Offer(ref best, GroundAlong(ground, ox, oz, dx, dz, start, best.Distance + Epsilon));
            return best;
        }

        static void Offer(ref RayHit best, in RayHit candidate)
        {
            if (candidate.Before(best)) best = candidate;
        }

        /// <summary>The first cell holding an item along the ray, at its centre's projection (never under the animal).</summary>
        static RayHit FoodAlong(FoodGrid layer, RayHitKind kind, float ox, float oz, float dx, float dz, float start, float limit)
        {
            var g = layer.Grid;
            if (g == null) return RayHit.Miss(limit);
            var walk = new GridWalk(g, ox, oz, dx, dz, start);
            while (walk.Next(out int cell, out float enter, out float exit))
            {
                if (enter > limit) break;
                if (!layer.HasItem(cell)) continue;
                var c = g.Center(cell);
                float t = (c.x - ox) * dx + (c.z - oz) * dz;
                if (t < start) continue;                               // the item it stands on, or one behind
                t = Mathf.Clamp(t, enter, exit);
                if (t <= limit) return new RayHit(kind, t, cell);
            }
            return RayHit.Miss(limit);
        }

        /// <summary>The first item of a non-grid layer along the ray, by marching.</summary>
        RayHit MarchLayer(ResourceLayer layer, RayHitKind kind, float ox, float oz, float dx, float dz, float start, float limit)
        {
            for (float t = start; t <= limit; t += MarchStep)
            {
                var p = new Vector3(ox + dx * t, 0f, oz + dz * t);
                if (layer.WithinReach(p, MarchStep, out var item)) return new RayHit(kind, t, item.Cell);
            }
            return RayHit.Miss(limit);
        }

        /// <summary>The first cover cell the ray enters from outside cover (cells it starts in are not seen).</summary>
        static RayHit CoverAlong(CoverLayer cover, float ox, float oz, float dx, float dz, float start, float limit)
        {
            var g = cover.Grid;
            if (g == null) return RayHit.Miss(limit);
            var walk = new GridWalk(g, ox, oz, dx, dz, start);
            bool inside = true;                                        // still in the patch it starts in
            bool first = true;
            while (walk.Next(out int cell, out float enter, out _))
            {
                if (enter > limit) break;
                bool covered = cover.InCover(g.Center(cell));
                if (first) { inside = covered; first = false; continue; }
                if (!covered) { inside = false; continue; }
                if (!inside) return new RayHit(RayHitKind.Cover, enter, -1);
            }
            return RayHit.Miss(limit);
        }

        /// <summary>The first point in cover after a point outside it, by marching, for covers that are not cell layers.</summary>
        RayHit MarchCover(Cover cover, float ox, float oz, float dx, float dz, float start, float limit)
        {
            bool inside = cover.InCover(new Vector3(ox + dx * start, 0f, oz + dz * start));
            for (float t = start + MarchStep; t <= limit; t += MarchStep)
            {
                bool covered = cover.InCover(new Vector3(ox + dx * t, 0f, oz + dz * t));
                if (covered && !inside) return new RayHit(RayHitKind.Cover, t, -1);
                if (!covered) inside = false;
            }
            return RayHit.Miss(limit);
        }

        /// <summary>The world's edge (exact) or the first water or unwalkable point (marched) along the ray.</summary>
        RayHit GroundAlong(Ground ground, float ox, float oz, float dx, float dz, float start, float limit)
        {
            var b = ground.Bounds;
            float edge = ExitDistance(b, ox, oz, dx, dz);
            float end = Mathf.Min(edge, limit);
            // A plain flat ground has no water, and every point inside is walkable: the steps that stay well inside on both
            // axes can't stop the ray. They are still stepped (the same float sums), so the hits stay exactly those of the
            // march (M16.8). The margin is across each axis, not along the ray: a ray almost along an edge runs a hair
            // from it, where rounding decides.
            float sure = ground.GetType() == typeof(FlatGround)
                ? Mathf.Min(SureAlong(ox, dx, b.xMin, b.xMax), SureAlong(oz, dz, b.yMin, b.yMax))
                : float.NegativeInfinity;
            var steps = MarchSteps(start, end);
            int k = sure > start ? FirstStepFrom(steps, sure) : 0;
            for (; k < steps.Count && steps[k] < end; k++)
            {
                float t = steps[k];
                var p = new Vector3(ox + dx * t, 0f, oz + dz * t);
                if (ground.IsWater(p)) return new RayHit(SplitWater ? RayHitKind.Water : RayHitKind.Obstacle, t, -1);
                if (!ground.IsWalkable(p)) return new RayHit(RayHitKind.Obstacle, t, -1);
            }
            return edge <= limit ? new RayHit(RayHitKind.Obstacle, Mathf.Max(edge, start), -1) : RayHit.Miss(limit);
        }

        /// <summary>
        /// The march's distances from <paramref name="start"/>, each the float sum of the one before and the step (as the
        /// loop <c>t += MarchStep</c> makes them), at least up to <paramref name="end"/>; computed once and extended.
        /// </summary>
        List<float> MarchSteps(float start, float end)
        {
            if (marchFrom != start || marchStep != MarchStep)
            {
                marchTs.Clear();
                marchFrom = start;
                marchStep = MarchStep;
            }
            if (marchTs.Count == 0) marchTs.Add(start);
            while (marchTs[marchTs.Count - 1] < end) marchTs.Add(marchTs[marchTs.Count - 1] + marchStep);
            return marchTs;
        }

        /// <summary>The index of the first step at or beyond a distance (the steps grow).</summary>
        static int FirstStepFrom(List<float> steps, float from)
        {
            int lo = 0, hi = steps.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (steps[mid] < from) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        /// <summary>
        /// On one axis, the distance along the ray before which the point stays at least <see cref="SureMargin"/> inside
        /// [min, max); negative infinity when the origin itself is not that far inside.
        /// </summary>
        static float SureAlong(float o, float d, float min, float max)
        {
            float lo = min + SureMargin, hi = max - SureMargin;
            if (o < lo || o >= hi) return float.NegativeInfinity;
            if (d > 0f) return (hi - o) / d;
            if (d < 0f) return (lo - o) / d;
            return float.PositiveInfinity;
        }

        /// <summary>Where a ray from inside a rectangle leaves it (0 when it starts outside).</summary>
        static float ExitDistance(Rect b, float ox, float oz, float dx, float dz)
        {
            if (ox < b.xMin || ox > b.xMax || oz < b.yMin || oz > b.yMax) return 0f;
            float t = float.PositiveInfinity;
            if (dx > 1e-9f) t = Mathf.Min(t, (b.xMax - ox) / dx);
            else if (dx < -1e-9f) t = Mathf.Min(t, (b.xMin - ox) / dx);
            if (dz > 1e-9f) t = Mathf.Min(t, (b.yMax - oz) / dz);
            else if (dz < -1e-9f) t = Mathf.Min(t, (b.yMin - oz) / dz);
            return t;
        }

        /// <summary>
        /// Walks the cells of a grid along a ray, in order, from distance <c>start</c> (Amanatides–Woo): each cell with the
        /// distances where the ray enters and leaves it. Stops at the grid's edge.
        /// </summary>
        struct GridWalk
        {
            readonly CellGrid g;
            int ix, iz;
            readonly int stepX, stepZ;
            float tMaxX, tMaxZ, tEnter;
            readonly float tDeltaX, tDeltaZ;
            bool done;

            public GridWalk(CellGrid grid, float ox, float oz, float dx, float dz, float start)
            {
                g = grid;
                float cs = grid.CellSize;
                float px = ox + dx * start, pz = oz + dz * start;
                ix = Mathf.FloorToInt((px - grid.X0) / cs);
                iz = Mathf.FloorToInt((pz - grid.Z0) / cs);
                stepX = dx > 0f ? 1 : dx < 0f ? -1 : 0;
                stepZ = dz > 0f ? 1 : dz < 0f ? -1 : 0;
                tDeltaX = stepX != 0 ? cs / Mathf.Abs(dx) : float.PositiveInfinity;
                tDeltaZ = stepZ != 0 ? cs / Mathf.Abs(dz) : float.PositiveInfinity;
                tMaxX = stepX > 0 ? (grid.X0 + (ix + 1) * cs - ox) / dx : stepX < 0 ? (grid.X0 + ix * cs - ox) / dx : float.PositiveInfinity;
                tMaxZ = stepZ > 0 ? (grid.Z0 + (iz + 1) * cs - oz) / dz : stepZ < 0 ? (grid.Z0 + iz * cs - oz) / dz : float.PositiveInfinity;
                tEnter = start;
                done = false;
            }

            public bool Next(out int cell, out float enter, out float exit)
            {
                cell = -1; enter = exit = 0f;
                if (done || ix < 0 || iz < 0 || ix >= g.Width || iz >= g.Height) { done = true; return false; }
                cell = g.Index(ix, iz);
                enter = tEnter;
                if (tMaxX < tMaxZ) { exit = tMaxX; ix += stepX; tMaxX += tDeltaX; }
                else { exit = tMaxZ; iz += stepZ; tMaxZ += tDeltaZ; }
                if (float.IsPositiveInfinity(exit)) done = true;
                tEnter = exit;
                return true;
            }
        }
    }
}
