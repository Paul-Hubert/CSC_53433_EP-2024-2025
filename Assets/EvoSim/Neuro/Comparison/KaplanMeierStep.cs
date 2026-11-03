namespace EvoSim.Neuro
{
    /// <summary>
    /// One step of a Kaplan–Meier curve (plan M16 §6.2): at an age where deaths happened, the animals still at risk, the
    /// deaths, the survival estimate and its 95 % Greenwood band.
    /// </summary>
    public readonly struct KaplanMeierStep
    {
        /// <summary>The age, in ticks.</summary>
        public readonly int Time;
        /// <summary>Animals at risk just before that age: neither dead nor censored before it.</summary>
        public readonly int AtRisk;
        /// <summary>Deaths at that age.</summary>
        public readonly int Deaths;
        /// <summary>The survival estimate S(t) just after that age.</summary>
        public readonly double Survival;
        /// <summary>Greenwood's variance of S(t): S² Σ d / (n (n − d)).</summary>
        public readonly double Variance;

        /// <summary>A step.</summary>
        /// <param name="time">The age.</param>
        /// <param name="atRisk">Animals at risk.</param>
        /// <param name="deaths">Deaths at that age.</param>
        /// <param name="survival">S(t).</param>
        /// <param name="variance">Greenwood's variance.</param>
        public KaplanMeierStep(int time, int atRisk, int deaths, double survival, double variance)
        {
            Time = time; AtRisk = atRisk; Deaths = deaths; Survival = survival; Variance = variance;
        }

        /// <summary>The lower end of the 95 % band, S − 1.96 √Var, at least 0.</summary>
        public double Lower => System.Math.Max(0.0, Survival - 1.96 * System.Math.Sqrt(Variance));
        /// <summary>The upper end of the 95 % band, S + 1.96 √Var, at most 1.</summary>
        public double Upper => System.Math.Min(1.0, Survival + 1.96 * System.Math.Sqrt(Variance));
    }
}
