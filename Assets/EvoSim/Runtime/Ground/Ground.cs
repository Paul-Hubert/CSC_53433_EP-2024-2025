using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Answers, for any point, its height and whether it is walkable (SPACE-04), inside a bounded rectangle with hard
    /// borders (SPACE-03). It also owns the world's distance function (SPACE-02): every module measures through it.
    /// </summary>
    /// <remarks>
    /// <para>One ground per World, under it (reference: the "Ground" GameObject). The World initialises it before every
    /// other service, since layers and cover are laid on it, and finds it as <see cref="World.Ground"/>.</para>
    /// <para>Derive from it and implement <see cref="Bounds"/>, <see cref="Height"/> and <see cref="IsWalkable"/>; override
    /// <see cref="IsWater"/> and <see cref="NearestWater"/> for water, <see cref="Distance"/> for path length. The answers
    /// must not change during a run, and <see cref="IsWalkable"/> is false outside <see cref="Bounds"/>. All walkable ground
    /// should be connected (SPACE-05). The movement helpers (<see cref="SlideTo"/>, <see cref="SideStep"/>) keep every
    /// animal on walkable ground (MOVE-02).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // The Lab 1 ground (FlatGround): the three questions every ground answers.
    /// public class FlatGround : Ground
    /// {
    ///     [SerializeField, Min(1f)] float width = 192f;
    ///     [SerializeField, Min(1f)] float depth = 192f;
    ///
    ///     public override Rect Bounds =&gt; new Rect(0f, 0f, width, depth);
    ///     public override float Height(Vector3 p) =&gt; 0f;
    ///     public override bool IsWalkable(Vector3 p) =&gt; Inside(p);
    /// }
    ///
    /// // A flat ground with a round pond that animals can't enter (from EvoSim's test probes).
    /// public class PondGround : FlatGround
    /// {
    ///     public Vector3 PondCenter = new Vector3(10f, 0f, 10f);
    ///     public float PondRadius = 3f;
    ///
    ///     public override bool IsWater(Vector3 p) =&gt; Flat(p, PondCenter) &lt; PondRadius;
    ///     public override bool IsWalkable(Vector3 p) =&gt; base.IsWalkable(p) &amp;&amp; !IsWater(p);
    ///
    ///     static float Flat(Vector3 a, Vector3 b) =&gt; new Vector2(a.x - b.x, a.z - b.z).magnitude;
    /// }
    /// </code>
    /// </example>
    public abstract class Ground : WorldService
    {
        /// <summary>The distance between two samples, in meters, when checking that a straight move doesn't cross non-walkable ground.</summary>
        const float SampleStep = 0.25f;

        /// <summary>The world rectangle on the horizontal plane: x is world x, y is world z. Half-open [min, max).</summary>
        public abstract Rect Bounds { get; }

        /// <summary>The ground's height under a point (SPACE-06).</summary>
        /// <param name="p">A world point; only x and z count.</param>
        /// <returns>The world height of the ground there, in meters.</returns>
        public abstract float Height(Vector3 p);

        /// <summary>Whether an animal may stand here; false outside the rectangle (SPACE-04).</summary>
        /// <param name="p">A world point; only x and z count.</param>
        /// <returns>True when the point is walkable: inside, and not water, too steep or an obstacle.</returns>
        public abstract bool IsWalkable(Vector3 p);

        /// <summary>The world distance: horizontal Euclidean in the reference. Override for path length (SPACE-02).</summary>
        /// <remarks>
        /// Every module measures through <see cref="World.Distance"/>, which calls this. A replacement must never be shorter
        /// than the straight horizontal distance: <see cref="CellGrid.Nearest"/> prunes its search with it.
        /// </remarks>
        /// <param name="a">One point.</param>
        /// <param name="b">The other point.</param>
        /// <returns>The distance, in meters; height doesn't count in the reference (SPACE-06).</returns>
        public virtual float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>The slope in degrees between two points (0 on flat ground).</summary>
        /// <remarks>Locomotions use it for slope costs or limits (22 §14).</remarks>
        /// <param name="from">The start point.</param>
        /// <param name="to">The end point.</param>
        /// <returns>The slope's steepness, from 0 to 90 degrees, up or down alike; 0 when the points are closer than <see cref="Units.Epsilon"/>.</returns>
        public virtual float SlopeDegrees(Vector3 from, Vector3 to)
        {
            float run = Distance(from, to);
            if (run < Units.Epsilon) return 0f;
            return Mathf.Atan2(Mathf.Abs(Height(to) - Height(from)), run) * Mathf.Rad2Deg;
        }

        /// <summary>Whether a point is water (terrain grounds; never on flat ground).</summary>
        /// <param name="p">A world point.</param>
        /// <returns>True when the point is water; false by default.</returns>
        public virtual bool IsWater(Vector3 p) => false;

        /// <summary>The nearest water within radius, with a walkable shore point next to it (22 §2); null if none.</summary>
        /// <remarks>The drink action walks to the shore point; the water sense reports the distance.</remarks>
        /// <param name="p">The point searched from, usually an animal's position.</param>
        /// <param name="radius">The largest distance, in meters.</param>
        /// <returns>The water point, its shore and its distance, or null when no water is within the radius (always, by default).</returns>
        public virtual WaterHit? NearestWater(Vector3 p, float radius) => null;

        /// <summary>Checks whether a point lies inside the rectangle (half-open: the far edges are outside).</summary>
        /// <param name="p">A world point; only x and z count.</param>
        /// <returns>True inside <see cref="Bounds"/>.</returns>
        public bool Inside(Vector3 p)
        {
            var r = Bounds;
            return p.x >= r.xMin && p.x < r.xMax && p.z >= r.yMin && p.z < r.yMax;
        }

        /// <summary>The point clamped into the rectangle, its height set from the ground.</summary>
        /// <param name="p">A world point, maybe outside.</param>
        /// <returns>The nearest point inside <see cref="Bounds"/>, on the ground; it may still be non-walkable.</returns>
        public Vector3 Clamp(Vector3 p)
        {
            var r = Bounds;
            p.x = Mathf.Clamp(p.x, r.xMin, r.xMax - 1e-4f);
            p.z = Mathf.Clamp(p.z, r.yMin, r.yMax - 1e-4f);
            p.y = Height(p);
            return p;
        }

        /// <summary>The point with its height set from the ground (SPACE-06).</summary>
        /// <param name="p">A world point.</param>
        /// <returns>The same x and z, with y = <see cref="Height"/>.</returns>
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
        /// <remarks>"Best" is the candidate that gets farthest along the wanted direction. The kinematic locomotion calls it once per move.</remarks>
        /// <param name="from">The start, on walkable ground.</param>
        /// <param name="to">The wanted end point.</param>
        /// <returns>The point reached, on the ground and walkable; <paramref name="from"/> when no move is possible.</returns>
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

            // Keeps a candidate whose straight path is clear and that gets farther along dir than the best so far.
            void Try(Vector3 c)
            {
                if (!Clear(from, c)) return;
                float progress = Vector3.Dot(new Vector3(c.x - from.x, 0f, c.z - from.z), dir);
                if (progress > bestProgress + 1e-6f) { best = c; bestProgress = progress; }
            }
        }

        /// <summary>A random step of length d to walkable ground, for an animal that is stuck (MOVE-04).</summary>
        /// <remarks>Tries up to 8 random directions and takes the first whose straight path is clear.</remarks>
        /// <param name="from">The stuck animal's position.</param>
        /// <param name="d">The step length, in meters.</param>
        /// <param name="rng">The stream to draw directions from: the species' <c>actions</c> stream (RAND-03).</param>
        /// <returns>The new point, on the ground; <paramref name="from"/> when all 8 tries are blocked.</returns>
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
        /// <param name="rng">The stream to draw from: the species' <c>founders</c> stream (RAND-03).</param>
        /// <returns>A walkable point on the ground.</returns>
        /// <exception cref="System.InvalidOperationException">No walkable point was found in 10 000 tries.</exception>
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
        /// <remarks>Samples are <see cref="SampleStep"/> apart; the start point itself is not checked.</remarks>
        /// <param name="from">The start of the segment.</param>
        /// <param name="to">The end of the segment, which must be walkable.</param>
        /// <returns>True when an animal can walk the segment in a straight line.</returns>
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
