using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Value noise on a lattice: random values at lattice points, smoothly interpolated (the prototype's cover and
    /// terrain maps). A noise map is built once from a stream; sampling it is a pure function.
    /// </summary>
    /// <remarks>
    /// Each octave is a lattice of random values in [0, 1) with its own spacing; a sample is the weighted sum of the
    /// octaves, each interpolated with a smoothstep. With the default weights (1, 0.5, 0.25…) values lie in [0, 2).
    /// </remarks>
    public sealed class ValueNoise
    {
        readonly float[][] lattices;    // per octave: random values at the lattice points, row by row
        readonly int[] latticeWidth;    // per octave: lattice points per row
        readonly float[] cellSizes, weights;    // per octave: lattice spacing in meters, and weight in the sum

        /// <summary>Builds a noise map over a rectangle, drawing every lattice value now, octave by octave.</summary>
        /// <param name="bounds">The rectangle sampled later (x is world x, y is world z), usually <see cref="Ground.Bounds"/>.</param>
        /// <param name="octaveCellSizes">Lattice spacing per octave, in meters (cover: 8, 4; terrain: 16, 8, 4).</param>
        /// <param name="rng">The stream the values come from, e.g. "world/Thickets" (RAND-03); the same stream gives the same map.</param>
        /// <param name="octaveWeights">The weight of each octave in the sum; null = 1, 0.5, 0.25…</param>
        public ValueNoise(Rect bounds, float[] octaveCellSizes, RandomStream rng, float[] octaveWeights = null)
        {
            int n = octaveCellSizes.Length;
            lattices = new float[n][];
            latticeWidth = new int[n];
            cellSizes = (float[])octaveCellSizes.Clone();
            weights = new float[n];
            for (int o = 0; o < n; o++)
            {
                weights[o] = octaveWeights != null ? octaveWeights[o] : Mathf.Pow(0.5f, o);
                // One spare lattice point past each far edge, so a sample at the edge can interpolate.
                int w = Mathf.CeilToInt(bounds.width / cellSizes[o]) + 2;
                int h = Mathf.CeilToInt(bounds.height / cellSizes[o]) + 2;
                latticeWidth[o] = w;
                lattices[o] = new float[w * h];
                for (int i = 0; i < lattices[o].Length; i++) lattices[o][i] = rng.NextFloat();
            }
            Origin = new Vector2(bounds.xMin, bounds.yMin);
        }

        /// <summary>The rectangle's minimum corner (x is world x, y is world z), where every lattice starts.</summary>
        public Vector2 Origin { get; }

        /// <summary>The noise value at a point (weighted sum of octaves).</summary>
        /// <param name="x">World x, inside the rectangle given to the constructor.</param>
        /// <param name="z">World z, inside the rectangle given to the constructor.</param>
        /// <returns>The noise value; points outside the rectangle are not supported.</returns>
        public float Sample(float x, float z)
        {
            float sum = 0f;
            for (int o = 0; o < lattices.Length; o++)
            {
                float fx = (x - Origin.x) / cellSizes[o], fz = (z - Origin.y) / cellSizes[o];
                int ix = Mathf.FloorToInt(fx), iz = Mathf.FloorToInt(fz);
                float tx = Smooth(fx - ix), tz = Smooth(fz - iz);
                int w = latticeWidth[o];
                var l = lattices[o];
                float a = l[iz * w + ix], b = l[iz * w + ix + 1], c = l[(iz + 1) * w + ix], d = l[(iz + 1) * w + ix + 1];
                sum += weights[o] * Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
            }
            return sum;
        }

        /// <summary>The smoothstep curve 3t² − 2t³: eases the interpolation between lattice points.</summary>
        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
