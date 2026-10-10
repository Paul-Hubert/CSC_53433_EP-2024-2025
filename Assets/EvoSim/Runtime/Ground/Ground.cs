using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Answers, for any point, its height and whether it is walkable (SPACE-04), inside a bounded rectangle with hard
    /// borders (SPACE-03). It also owns the world's distance function (SPACE-02): every module measures through it.
    /// </summary>
    public abstract class Ground : WorldService
    {
        /// <summary>Samples per meter when checking that a straight move doesn't cross non-walkable ground.</summary>
        const float SampleStep = 0.25f;

        /// <summary>The world rectangle on the horizontal plane: x is world x, y is world z. Half-open [min, max).</summary>
        public abstract Rect Bounds { get; }

        /// <summary>The ground's height under a point (SPACE-06).</summary>
        public abstract float Height(Vector3 p);

        /// <summary>Whether an animal may stand here; false outside the rectangle (SPACE-04).</summary>
        public abstract bool IsWalkable(Vector3 p);

        /// <summary>The world distance: horizontal Euclidean in the reference. Override for path length (SPACE-02).</summary>
        public virtual float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>The slope in degrees between two points (0 on flat ground).</summary>
        public virtual float SlopeDegrees(Vector3 from, Vector3 to)
        {
            float run = Distance(from, to);
            if (run < Units.Epsilon) return 0f;
            return Mathf.Atan2(Mathf.Abs(Height(to) - Height(from)), run) * Mathf.Rad2Deg;
        }

        /// <summary>Whether a point is water (terrain grounds; never on flat ground).</summary>
        public virtual bool IsWater(Vector3 p) => false;

        /// <summary>The nearest water within radius, with a walkable shore point next to it (22 §2); null if none.</summary>
        public virtual WaterHit? NearestWater(Vector3 p, float radius) => null;

        public bool Inside(Vector3 p)
        {
            var r = Bounds;
            return p.x >= r.xMin && p.x < r.xMax && p.z >= r.yMin && p.z < r.yMax;
        }

        /// <summary>The point clamped into the rectangle, its height set from the ground.</summary>
        public Vector3 Clamp(Vector3 p)
        {
            var r = Bounds;
            p.x = Mathf.Clamp(p.x, r.xMin, r.xMax - 1e-4f);
            p.z = Mathf.Clamp(p.z, r.yMin, r.yMax - 1e-4f);
            p.y = Height(p);
            return p;
        }

        /// <summary>The point with its height set from the ground (SPACE-06).</summary>
        public Vector3 OnGround(Vector3 p)
        {
            p.y = Height(p);
            return p;
        }

        /// <summary>
        /// Moves in a straight line toward <paramref name="to"/> as far as walkable ground allows. Blocked by a
        /// border or an obstacle, it slides along it (MOVE-04): the best of the clamped point and the two
        /// one-axis moves, else the farthest walkable point of the straight segment.
        /// </summary>
        public virtual Vector3 SlideTo(Vector3 from, Vector3 to)
        {
            if (Clear(from, to)) return OnGround(to);
            Vector3 dir = new Vector3(to.x - from.x, 0f, to.z - from.z);
            Vector3 best = from;
            float bestProgress = 0f;
            Try(Clamp(to));
            Try(new Vector3(to.x, from.y, from.z));
            Try(new Vector3(from.x, from.y, to.z));
            if (bestProgress <= Units.Epsilon) best = FarthestAlong(from, to);
            return OnGround(best);

            void Try(Vector3 c)
            {
                if (!Clear(from, c)) return;
                float progress = Vector3.Dot(new Vector3(c.x - from.x, 0f, c.z - from.z), dir);
                if (progress > bestProgress + 1e-6f) { best = c; bestProgress = progress; }
            }
        }

        /// <summary>A random step of length d to walkable ground, for an animal that is stuck (MOVE-04).</summary>
        public virtual Vector3 SideStep(Vector3 from, float d, RandomStream rng)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = rng.Range(0f, 2f * Mathf.PI);
                var c = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * d;
                if (Clear(from, c)) return OnGround(c);
            }
            return from;
        }

        /// <summary>A uniformly random walkable point (founders, newcomers; POP-03, POP-04).</summary>
        public virtual Vector3 RandomWalkable(RandomStream rng)
        {
            var r = Bounds;
            for (int i = 0; i < 10000; i++)
            {
                var p = new Vector3(rng.Range(r.xMin, r.xMax), 0f, rng.Range(r.yMin, r.yMax));
                if (IsWalkable(p)) return OnGround(p);
            }
            throw new System.InvalidOperationException($"{name}: no walkable point found in 10 000 tries.");
        }

        /// <summary>True if every sample of the straight segment is walkable (inside, not water, not an obstacle).</summary>
        public bool Clear(Vector3 from, Vector3 to)
        {
            if (!IsWalkable(to)) return false;
            float d = Distance(from, to);
            int n = Mathf.CeilToInt(d / SampleStep);
            for (int i = 1; i < n; i++)
                if (!IsWalkable(Vector3.Lerp(from, to, (float)i / n))) return false;
            return true;
        }

        /// <summary>The farthest walkable sample of the straight segment, before the first blocked one.</summary>
        Vector3 FarthestAlong(Vector3 from, Vector3 to)
        {
            float d = Distance(from, to);
            int n = Mathf.Max(1, Mathf.CeilToInt(d / SampleStep));
            Vector3 last = from;
            for (int i = 1; i <= n; i++)
            {
                var p = Vector3.Lerp(from, to, (float)i / n);
                if (!IsWalkable(p)) break;
                last = p;
            }
            return last;
        }
    }
}
