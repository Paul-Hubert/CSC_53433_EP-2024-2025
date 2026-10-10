using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Value noise on a lattice: random values at lattice points, smoothly interpolated (the prototype's cover and
    /// terrain maps). A noise map is built once from a stream; sampling it is a pure function.
    /// </summary>
    public sealed class ValueNoise
    {
        readonly float[][] lattices;
        readonly int[] latticeWidth;
        readonly float[] cellSizes, weights;

        /// <param name="octaveCellSizes">Lattice spacing per octave, in meters (cover: 8, 4; terrain: 16, 8, 4).</param>
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
                int w = Mathf.CeilToInt(bounds.width / cellSizes[o]) + 2;
                int h = Mathf.CeilToInt(bounds.height / cellSizes[o]) + 2;
                latticeWidth[o] = w;
                lattices[o] = new float[w * h];
                for (int i = 0; i < lattices[o].Length; i++) lattices[o][i] = rng.NextFloat();
            }
            Origin = new Vector2(bounds.xMin, bounds.yMin);
        }

        public Vector2 Origin { get; }

        /// <summary>The noise value at a point (weighted sum of octaves).</summary>
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

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
