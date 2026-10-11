using System;

namespace EvoSim
{
    /// <summary>Sampling temperature (DEC-20): p^(1/τ), renormalised; τ &lt; 1 sharpens, τ &gt; 1 flattens. Pure functions.</summary>
    /// <remarks>
    /// The choose-actions phase tempers each decision's row with the World's sampling temperature (reference τ = 1, at least
    /// 0.01), then draws the action from it with the species' sampling stream (RAND-03). The memo and the cache keep the
    /// brain's own rows, untempered.
    /// </remarks>
    public static class Sampling
    {
        /// <summary>Tempers a probability row: each entry p becomes p^(1/τ), then the row is renormalised (DEC-20).</summary>
        /// <param name="p">A probability row; it is not changed.</param>
        /// <param name="tau">The temperature τ, &gt; 0: below 1 sharpens towards the likeliest action, above 1 flattens
        /// towards uniform.</param>
        /// <returns>The tempered row (a new array), or the row itself when τ = 1 or every tempered entry is 0.</returns>
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
