using System;

namespace EvoSim.Neuro
{
    /// <summary>
    /// A ratio of two death rates (plan M16 §6.2, the headline hazard ratio): (d₁/T₁) / (d₂/T₂) with a 95 % interval from
    /// the log rate ratio, SE = √(1/d₁ + 1/d₂), a constant hazard per window. Below 1: the first group dies less often.
    /// </summary>
    public readonly struct RateRatio
    {
        /// <summary>Deaths and animal-ticks of the first group (the neural species in the report).</summary>
        public readonly double D1, T1;
        /// <summary>Deaths and animal-ticks of the second group (the JEV species).</summary>
        public readonly double D2, T2;

        /// <summary>A ratio from deaths and animal-ticks.</summary>
        /// <param name="d1">Deaths, first group.</param>
        /// <param name="t1">Animal-ticks, first group.</param>
        /// <param name="d2">Deaths, second group.</param>
        /// <param name="t2">Animal-ticks, second group.</param>
        public RateRatio(double d1, double t1, double d2, double t2) { D1 = d1; T1 = t1; D2 = d2; T2 = t2; }

        /// <summary>True when both groups have a death and time at risk, so the ratio and its interval exist.</summary>
        public bool Defined => D1 > 0 && D2 > 0 && T1 > 0 && T2 > 0;
        /// <summary>The ratio of the rates; NaN when not <see cref="Defined"/>.</summary>
        public double Ratio => Defined ? (D1 / T1) / (D2 / T2) : double.NaN;
        /// <summary>The standard error of the log ratio, √(1/d₁ + 1/d₂).</summary>
        public double LogSe => Defined ? Math.Sqrt(1.0 / D1 + 1.0 / D2) : double.NaN;
        /// <summary>The lower end of the 95 % interval, ratio · e^(−1.96 SE).</summary>
        public double Lower => Ratio * Math.Exp(-1.96 * LogSe);
        /// <summary>The upper end of the 95 % interval, ratio · e^(1.96 SE).</summary>
        public double Upper => Ratio * Math.Exp(1.96 * LogSe);
        /// <summary>True when the interval excludes 1 (an effect, in either direction).</summary>
        public bool ExcludesOne => Defined && (Upper < 1.0 || Lower > 1.0);
    }
}
