using System;
using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Distance bands (06 §3, SENSE-20): increasing edges e1 &lt; … &lt; ek and a vision v &gt; ek. A distance falls in
    /// the first band with d ≤ ei, in "far" if ek &lt; d ≤ v, and is "none" beyond v. Reference: 1, 4, 10 m and 20 m.
    /// </summary>
    [Serializable]
    public class Bands
    {
        [Tooltip("Increasing band edges, in meters (reference 1, 4, 10).")]
        public float[] edges = { 1f, 4f, 10f };
        [Tooltip("Names of the bands up to each edge (reference adjacent, close, medium); then come far and none.")]
        public string[] names = { "adjacent", "close", "medium" };
        [Min(0.01f), Tooltip("Default of the species' vision trait, in meters (reference 20).")]
        public float vision = 20f;

        public const string Far = "far", None = "none", Here = "here";

        /// <summary>The reference bands: 1, 4, 10 m, vision 20 m.</summary>
        public static Bands Reference => new Bands();

        /// <summary>Number of distance bands: one per edge, plus far.</summary>
        public int Count => edges.Length + 1;

        public string NameOf(int band) => band < edges.Length ? (band < names.Length ? names[band] : "band" + (band + 1)) : Far;

        /// <summary>The band of a distance for this vision, or -1 for none (SENSE-20).</summary>
        public int IndexOf(float distance, float visionMeters)
        {
            if (distance > visionMeters) return -1;
            for (int i = 0; i < edges.Length; i++)
                if (distance <= edges[i]) return i;
            return edges.Length;
        }

        /// <summary>The lower and upper limits of a band, in meters (far ends at the vision).</summary>
        public (float lower, float upper) Range(int band, float visionMeters) =>
            (band == 0 ? 0f : edges[band - 1], band < edges.Length ? edges[band] : visionMeters);

        /// <summary>A band's range as text: "within 1 meter", "1-4 meters away" (SENSE-12, SENSE-13).</summary>
        public string Describe(int band, float visionMeters)
        {
            var (lower, upper) = Range(band, visionMeters);
            if (band == 0) return "within " + Meters(upper);
            return Number(lower) + "-" + Number(upper) + " meters away";
        }

        /// <summary>"none within 20 meters": what "none" states (SENSE-12).</summary>
        public string DescribeNone(float visionMeters) => "none within " + Meters(visionMeters);

        /// <summary>A problem with the edges (SENSE-21), or null.</summary>
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

        /// <summary>"1 meter", "20 meters".</summary>
        public static string Meters(float m) => Number(m) + (Mathf.Approximately(m, 1f) ? " meter" : " meters");

        /// <summary>A number as the brain reads it: "4", "2.5".</summary>
        public static string Number(float v) =>
            Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture)
                                                   : v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
