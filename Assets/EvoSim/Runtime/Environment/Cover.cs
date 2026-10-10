using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Places where an animal is hidden from the species that hunt it (03 §2). Optional: a world without one
    /// behaves as if no point were in cover (ENV-12).
    /// </summary>
    public abstract class Cover : WorldService
    {
        [SerializeField, Tooltip("Species that can hide here, by id; empty = every species (ENV-11). They hide from their threats.")]
        List<string> hidingSpecies = new List<string>();

        /// <summary>Whether a point is in cover (ENV-10).</summary>
        public abstract bool InCover(Vector3 p);

        /// <summary>The nearest point of cover within radius (ENV-10); false if none.</summary>
        public abstract bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance);

        /// <summary>
        /// Whether an animal of <paramref name="hidden"/> standing in cover is hidden from <paramref name="from"/>:
        /// the reference hides a species from its threats; kin and its own prey still see it (ENV-11).
        /// </summary>
        public virtual bool Hides(Species hidden, Species from)
        {
            if (hidingSpecies.Count > 0 && !hidingSpecies.Contains(hidden.Id)) return false;
            return World.IsThreat(from, hidden) && from != hidden;
        }

        /// <summary>Whether this animal is hidden from that species right now.</summary>
        public virtual bool IsHiddenFrom(Animal a, Species from) => Hides(a.Species, from) && InCover(a.Position);

        public void SetHidingSpecies(IEnumerable<string> ids) => hidingSpecies = new List<string>(ids);
    }
}
