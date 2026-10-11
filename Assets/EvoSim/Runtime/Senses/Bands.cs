using System;
using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Distance bands (06 §3, SENSE-20): increasing edges e1 &lt; … &lt; ek and a vision v &gt; ek. A distance falls in
    /// the first band with d ≤ ei, in "far" if ek &lt; d ≤ v, and is "none" beyond v. Reference: 1, 4, 10 m and 20 m.
    /// </summary>
    /// <remarks>
    /// A serializable setting of every <see cref="BandedSense"/>: band 0 is "adjacent" (≤ 1 m), band 1 "close" (1–4 m), band
    /// 2 "medium" (4–10 m) and band 3 "far" (10–20 m) in the reference. It also writes distances the way the brain reads
    /// them: "within 1 meter", "1-4 meters away", "none within 20 meters" (SENSE-12, SENSE-13). Edges are validated before the
    /// run (SENSE-21, V-30).
    /// </remarks>
    [Serializable]
    public class Bands
    {
        /// <summary>The upper edge of each band but the last, increasing, in meters (reference 1, 4, 10).</summary>
        [Tooltip("Increasing band edges, in meters (reference 1, 4, 10).")]
        public float[] edges = { 1f, 4f, 10f };

        /// <summary>
        /// The token of each band up to its edge (reference adjacent, close, medium); then come "far" and "none". A band
        /// without a name is called "band" and its number from 1 ("band4").
        /// </summary>
        [Tooltip("Names of the bands up to each edge (reference adjacent, close, medium); then come far and none.")]
        public string[] names = { "adjacent", "close", "medium" };

        /// <summary>
        /// The default of the species' "vision" trait, in meters (reference 20): the end of the far band and the range
        /// "none" states. It must be above the last edge.
        /// </summary>
        [Min(0.01f), Tooltip("Default of the species' vision trait, in meters (reference 20).")]
        public float vision = 20f;

        /// <summary>
        /// The fixed tokens of banded senses: "far" (the last band, up to the vision), "none" (nothing within vision) and
        /// "here" (within reach, or standing in it).
        /// </summary>
        public const string Far = "far", None = "none", Here = "here";

        /// <summary>The reference bands: 1, 4, 10 m, vision 20 m (a new copy each time).</summary>
        public static Bands Reference => new Bands();

        /// <summary>Number of distance bands: one per edge, plus far ("here" and "none" are not bands).</summary>
        public int Count => edges.Length + 1;

        /// <summary>Gives the token of a band.</summary>
        /// <param name="band">A band index, from 0 to <see cref="Count"/> - 1.</param>
        /// <returns>Its name from <see cref="names"/>, "band" and its number when unnamed, or "far" for the last band.</returns>
        public string NameOf(int band) => band < edges.Length ? (band < names.Length ? names[band] : "band" + (band + 1)) : Far;

        /// <summary>Gives the band of a distance for this vision (SENSE-20).</summary>
        /// <param name="distance">The distance to the nearest thing, in meters.</param>
        /// <param name="visionMeters">The animal's vision, in meters: beyond it, nothing is seen.</param>
        /// <returns>The first band whose edge is ≥ the distance, the far band (<see cref="Count"/> - 1) up to the vision, or -1
        /// ("none") beyond the vision.</returns>
        public int IndexOf(float distance, float visionMeters)
        {
            if (distance > visionMeters) return -1;
            for (int i = 0; i < edges.Length; i++)
                if (distance <= edges[i]) return i;
            return edges.Length;
        }

        /// <summary>Gives the lower and upper limits of a band, in meters.</summary>
        /// <param name="band">A band index, from 0 to <see cref="Count"/> - 1.</param>
        /// <param name="visionMeters">The vision, in meters: where the far band ends.</param>
        /// <returns>(lower, upper): 0 to the first edge for band 0, the previous edge to the vision for the far band.</returns>
        public (float lower, float upper) Range(int band, float visionMeters) =>
            (band == 0 ? 0f : edges[band - 1], band < edges.Length ? edges[band] : visionMeters);

        /// <summary>Writes a band's range as the brain reads it (SENSE-12, SENSE-13).</summary>
        /// <param name="band">A band index, from 0 to <see cref="Count"/> - 1.</param>
        /// <param name="visionMeters">The vision the text states, in meters: the end of the far band.</param>
        /// <returns>"within 1 meter" for band 0, else "lower-upper meters away" ("1-4 meters away").</returns>
        public string Describe(int band, float visionMeters)
        {
            var (lower, upper) = Range(band, visionMeters);
            if (band == 0) return "within " + Meters(upper);
            return Number(lower) + "-" + Number(upper) + " meters away";
        }

        /// <summary>Writes what "none" states: how far the animal sees (SENSE-12).</summary>
        /// <param name="visionMeters">The vision the text states, in meters.</param>
        /// <returns>"none within 20 meters" for 20.</returns>
        public string DescribeNone(float visionMeters) => "none within " + Meters(visionMeters);

        /// <summary>Checks the edges and the vision (SENSE-21): positive, increasing, the last one below the vision.</summary>
        /// <returns>What is wrong, in a few words, or null when the bands are fine.</returns>
        public string Check()
        {
            if (edges == null || edges.Length == 0) return "no band edges";
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i] <= 0f) return $"edge {edges[i]} is not positive";
                if (i > 0 && edges[i] <= edges[i - 1]) return $"edges {string.Join(", ", edges)} are not increasing";
            }
            if (edges[edges.Length - 1] >= vision) return $"the last edge {edges[edges.Length - 1]} is not below the vision {vision}";
            return null;
        }

        /// <summary>Writes a length with its unit: "1 meter", "20 meters" (SENSE-13).</summary>
        /// <param name="m">The length, in meters.</param>
        /// <returns>The number as <see cref="Number"/> writes it, then "meter" or "meters".</returns>
        public static string Meters(float m) => Number(m) + (Mathf.Approximately(m, 1f) ? " meter" : " meters");

        /// <summary>Writes a number as the brain reads it: "4", "2.5"; prompt placeholders use it too.</summary>
        /// <param name="v">The number.</param>
        /// <returns>A whole number without decimals, else at most two decimals, with a dot whatever the computer's language.</returns>
        public static string Number(float v) =>
            Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture)
                                                   : v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
