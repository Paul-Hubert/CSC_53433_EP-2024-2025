namespace EvoSim.Neuro
{
    /// <summary>The first object one ray met (plan M16 §5.4): its kind, its distance and its id, or a miss.</summary>
    public readonly struct RayHit
    {
        /// <summary>True when the ray stopped on something within its range.</summary>
        public readonly bool Any;
        /// <summary>What stopped it; meaningless on a miss.</summary>
        public readonly RayHitKind Kind;
        /// <summary>How far along the ray, in meters; the range on a miss.</summary>
        public readonly float Distance;
        /// <summary>The animal's or entity's id, the item's cell, or -1 for cover and the ground: the tie-break (SPACE-08).</summary>
        public readonly int Id;

        /// <summary>Makes a hit.</summary>
        /// <param name="kind">What stopped the ray.</param>
        /// <param name="distance">How far along the ray, in meters.</param>
        /// <param name="id">The object's id, or -1.</param>
        public RayHit(RayHitKind kind, float distance, int id) { Any = true; Kind = kind; Distance = distance; Id = id; }

        /// <summary>A miss: nothing within <paramref name="range"/>.</summary>
        /// <param name="range">The ray's length, in meters.</param>
        /// <returns>The miss.</returns>
        public static RayHit Miss(float range) => new RayHit(false, range);

        RayHit(bool any, float range) { Any = any; Kind = RayHitKind.Obstacle; Distance = range; Id = -1; }

        /// <summary>True when this hit comes before <paramref name="other"/>: nearer, then the lower kind, then the lower id.</summary>
        /// <param name="other">The hit to compare with.</param>
        /// <returns>True when this one wins.</returns>
        public bool Before(in RayHit other)
        {
            if (!Any) return false;
            if (!other.Any) return true;
            if (Distance < other.Distance - WorldRayCaster.Epsilon) return true;
            if (Distance > other.Distance + WorldRayCaster.Epsilon) return false;
            if (Kind != other.Kind) return Kind < other.Kind;
            return Id < other.Id;
        }

        /// <inheritdoc/>
        public override string ToString() => Any ? $"{Kind} {Distance:0.##} m" : "miss";
    }
}
