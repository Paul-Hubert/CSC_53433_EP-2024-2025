using System;

namespace EvoSim
{
    /// <summary>Sampling temperature (DEC-20): p^(1/τ), renormalised; τ &lt; 1 sharpens, τ &gt; 1 flattens. Pure functions.</summary>
    public static class Sampling
    {
        /// <summary>The tempered row (a new array), or the row itself when τ = 1.</summary>
        public static float[] Temper(float[] p, float tau)
        {
            if (Math.Abs(tau - 1f) < 1e-6f) return p;
            var q = new float[p.Length];
            double sum = 0, inv = 1.0 / tau;
            for (int i = 0; i < p.Length; i++)
            {
                q[i] = p[i] <= 0f ? 0f : (float)Math.Pow(p[i], inv);
                sum += q[i];
            }
            if (sum <= 0) return p;
            for (int i = 0; i < q.Length; i++) q[i] = (float)(q[i] / sum);
            return q;
        }
    }
}
