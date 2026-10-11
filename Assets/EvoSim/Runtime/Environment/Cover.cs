using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Places where an animal is hidden from the species that hunt it (03 §2). Optional: a world without one
    /// behaves as if no point were in cover (ENV-12).
    /// </summary>
    /// <remarks>
    /// <para>Derive from it, implement <see cref="InCover"/> and <see cref="NearestCover"/>, and place the component under
    /// the World (the reference: <see cref="CoverLayer"/> on "Thickets", under Environment). Several covers combine: an
    /// animal is hidden when any of them hides it (<see cref="WorldQueries.IsHiddenFrom"/>).</para>
    /// <para>A hidden animal is left out of its hunters' senses, can't be targeted by their actions and is never struck
    /// (ENV-11). Override <see cref="Hides"/> to change who hides from whom. Cover only answers questions: it draws no
    /// random number after <see cref="WorldModule.Initialize"/>, which may build a map from a "world/…" stream.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Every point with x in [MinX, MaxX] is in cover (a test probe of EvoSim).
    /// public class SideCover : Cover
    /// {
    ///     public float MinX = float.NegativeInfinity, MaxX = float.PositiveInfinity;
    ///
    ///     public override bool InCover(Vector3 p) =&gt; p.x &gt;= MinX &amp;&amp; p.x &lt;= MaxX;
    ///
    ///     public override bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance)
    ///     {
    ///         point = new Vector3(Mathf.Clamp(p.x, MinX, MaxX), p.y, p.z);
    ///         distance = Mathf.Abs(point.x - p.x);
    ///         return distance &lt;= radius;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Cover : WorldService
    {
        [SerializeField, Tooltip("Species that can hide here, by id; empty = every species (ENV-11). They hide from their threats.")]
        List<string> hidingSpecies = new List<string>();

        /// <summary>Checks whether a point is in cover (ENV-10).</summary>
        /// <param name="p">A world point; its height is ignored by the reference.</param>
        /// <returns>True when the point is in cover.</returns>
        public abstract bool InCover(Vector3 p);

        /// <summary>Finds the nearest point of cover within a radius (ENV-10): the hide action runs to it, the cover sense reports it.</summary>
        /// <param name="p">The point searched from, usually the animal's position.</param>
        /// <param name="radius">The largest distance, in meters.</param>
        /// <param name="point">The nearest point in cover, on the ground; <paramref name="p"/> itself when it is in cover already.</param>
        /// <param name="distance">The world distance to <paramref name="point"/>, in meters (0 when already in cover).</param>
        /// <returns>True when cover was found within the radius; false if none.</returns>
        public abstract bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance);

        /// <summary>
        /// Whether an animal of <paramref name="hidden"/> standing in cover is hidden from <paramref name="from"/>:
        /// the reference hides a species from its threats; kin and its own prey still see it (ENV-11).
        /// </summary>
        /// <remarks>
        /// The default also requires <paramref name="hidden"/> to be one of the hiding species when that list is set.
        /// An override must give the same answer for the same pair of species during a run: senses rely on it.
        /// </remarks>
        /// <param name="hidden">The species of the animal that may be hidden.</param>
        /// <param name="from">The species looking for it.</param>
        /// <returns>True when cover hides <paramref name="hidden"/> from <paramref name="from"/>; it says nothing about where the animal stands.</returns>
        public virtual bool Hides(Species hidden, Species from)
        {
            if (hidingSpecies.Count > 0 && !hidingSpecies.Contains(hidden.Id)) return false;
            return World.IsThreat(from, hidden) && from != hidden;
        }

        /// <summary>Whether this animal is hidden from that species right now: its species hides from them and it stands in cover.</summary>
        /// <param name="a">The animal that may be hidden.</param>
        /// <param name="from">The species looking for it.</param>
        /// <returns>True when <paramref name="a"/> is hidden from <paramref name="from"/> at its current position.</returns>
        public virtual bool IsHiddenFrom(Animal a, Species from) => Hides(a.Species, from) && InCover(a.Position);

        /// <summary>Sets the species that can hide here from code (scene builders, tests); empty = every species.</summary>
        /// <param name="ids">Species ids.</param>
        public void SetHidingSpecies(IEnumerable<string> ids) => hidingSpecies = new List<string>(ids);
    }
}
