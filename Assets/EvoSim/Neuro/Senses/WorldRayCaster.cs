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
        public void Cast(Animal a, float range, IReadOnlyList<float> angles, Span<RayHit> hits)
        {
            Bind();
            Gather(a, range);
            for (int k = 0; k < angles.Count; k++)
            {
                float r = (a.Heading + angles[k]) * Mathf.Deg2Rad;
                hits[k] = CastOne(a, a.Position.x, a.Position.z, Mathf.Sin(r), Mathf.Cos(r), range);
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
        void Gather(Animal a, float range)
        {
            near.Clear();
            float reach = range + BodyRadius, reach2 = reach * reach;
            var me = a.Species;
            var diet = me.Module<Diet>();
            foreach (var s in world.AllSpecies)
            {
                RayHitKind kind = s == me ? RayHitKind.Kin
                                : diet != null && diet.Striking(s) != null ? RayHitKind.Prey
                                : world.IsThreat(s, me) ? RayHitKind.Threat
                                : RayHitKind.OtherAnimal;
                var animals = s.Animals;
                for (int i = 0; i < animals.Count; i++)
                {
                    var o = animals[i];
                    if (o == a || !o.IsAlive) continue;                                    // killed: the rays pass through
                    float dx = o.Position.x - a.Position.x, dz = o.Position.z - a.Position.z;
                    if (dx * dx + dz * dz > reach2) continue;
                    if (world.Queries.IsHiddenFrom(o, me)) continue;                        // SENSE-06
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

        /// <summary>The first object along one ray from (ox, oz), direction (dx, dz) of length 1.</summary>
        RayHit CastOne(Animal a, float ox, float oz, float dx, float dz, float range)
        {
            float start = BodyRadius;
            var best = RayHit.Miss(range);

            // Animals and entities: circles, at the projection of their centre.
            float r2 = BodyRadius * BodyRadius;
            for (int i = 0; i < near.Count; i++)
            {
                var c = near[i];
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
            for (float t = start; t < end; t += MarchStep)
            {
                var p = new Vector3(ox + dx * t, 0f, oz + dz * t);
                if (ground.IsWater(p)) return new RayHit(SplitWater ? RayHitKind.Water : RayHitKind.Obstacle, t, -1);
                if (!ground.IsWalkable(p)) return new RayHit(RayHitKind.Obstacle, t, -1);
            }
            return edge <= limit ? new RayHit(RayHitKind.Obstacle, Mathf.Max(edge, start), -1) : RayHit.Miss(limit);
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
