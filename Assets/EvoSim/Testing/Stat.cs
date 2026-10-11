using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace EvoSim.Testing
{
    /// <summary>Statistical asserts: a proportion or a mean within k standard errors, a χ² uniformity test (30 §2).</summary>
    /// <remarks>
    /// Random rules are tested by counting over many draws from a fixed seed: the test then passes or fails the same way on
    /// every run. k = 4 standard errors (the default) fails a correct rule about once in 16 000 seeds.
    /// </remarks>
    /// <example>
    /// T-ACT-06: 10 000 strikes with kill chance 0.5 kill half the time.
    /// <code>
    /// Stat.Binomial(kills, n, 0.5, 4, "kills");
    /// </code>
    /// </example>
    public static class Stat
    {
        /// <summary>
        /// Asserts that the observed proportion <paramref name="successes"/> / <paramref name="trials"/> is within k standard
        /// errors of p.
        /// </summary>
        /// <param name="successes">How many trials succeeded.</param>
        /// <param name="trials">How many trials were made (above 0).</param>
        /// <param name="p">The expected probability of success, 0 to 1.</param>
        /// <param name="k">The tolerance, in standard errors.</param>
        /// <param name="what">A name for the failure message.</param>
        public static void Binomial(long successes, long trials, double p, double k = 4, string what = "")
        {
            double se = Math.Sqrt(p * (1 - p) / trials);
            double observed = (double)successes / trials;
            Assert.That(Math.Abs(observed - p), Is.LessThanOrEqualTo(k * se + 1e-12),
                $"{what}: observed {observed:F5} vs expected {p:F5} (±{k} s.e. = {k * se:F5}, {trials} trials)");
        }

        /// <summary>
        /// Asserts that a count of events, each with probability p per trial, is within k standard errors; the same test as
        /// <see cref="Binomial"/>.
        /// </summary>
        /// <param name="count">How many events happened.</param>
        /// <param name="trials">How many trials were made (above 0).</param>
        /// <param name="p">The expected probability per trial, 0 to 1.</param>
        /// <param name="k">The tolerance, in standard errors.</param>
        /// <param name="what">A name for the failure message.</param>
        public static void Count(long count, long trials, double p, double k = 4, string what = "") =>
            Binomial(count, trials, p, k, what);

        /// <summary>
        /// Asserts that the mean of the values is within k standard errors of the expected mean, the standard error taken from
        /// the values' own spread.
        /// </summary>
        /// <param name="values">The measured values (at least 2 for a spread).</param>
        /// <param name="expected">The expected mean.</param>
        /// <param name="k">The tolerance, in standard errors.</param>
        /// <param name="what">A name for the failure message.</param>
        public static void Mean(IReadOnlyList<double> values, double expected, double k = 4, string what = "")
        {
            double mean = 0;
            foreach (var v in values) mean += v;
            mean /= values.Count;
            double var = 0;
            foreach (var v in values) var += (v - mean) * (v - mean);
            var /= Math.Max(1, values.Count - 1);
            double se = Math.Sqrt(var / values.Count);
            Assert.That(Math.Abs(mean - expected), Is.LessThanOrEqualTo(k * se + 1e-9),
                $"{what}: mean {mean:F5} vs expected {expected:F5} (±{k} s.e. = {k * se:F5})");
        }

        /// <summary>χ² test that the counts are uniform; fails when p &lt; alpha (Wilson–Hilferty approximation).</summary>
        /// <param name="counts">How many times each outcome happened (e.g. each founder allele drawn).</param>
        /// <param name="alpha">The significance level: the test fails when the p-value is below it.</param>
        /// <param name="what">A name for the failure message.</param>
        public static void Uniform(IReadOnlyList<long> counts, double alpha = 0.001, string what = "")
        {
            long total = 0;
            foreach (var c in counts) total += c;
            double expected = (double)total / counts.Count, chi2 = 0;
            foreach (var c in counts) chi2 += (c - expected) * (c - expected) / expected;
            int df = counts.Count - 1;
            double p = ChiSquareUpperTail(chi2, df);
            Assert.That(p, Is.GreaterThanOrEqualTo(alpha), $"{what}: χ² = {chi2:F2} with {df} d.f., p = {p:E2} (counts {string.Join(", ", counts)})");
        }

        /// <summary>P(X ≥ x) for χ² with df degrees of freedom (Wilson–Hilferty normal approximation).</summary>
        /// <param name="x">The χ² value.</param>
        /// <param name="df">The degrees of freedom; 0 or less gives 1.</param>
        /// <returns>The p-value, between 0 and 1.</returns>
        public static double ChiSquareUpperTail(double x, int df)
        {
            if (df <= 0) return 1;
            double z = (Math.Pow(x / df, 1.0 / 3) - (1 - 2.0 / (9 * df))) / Math.Sqrt(2.0 / (9 * df));
            return 0.5 * Erfc(z / Math.Sqrt(2));
        }

        /// <summary>The complementary error function erfc(x) = 1 - erf(x).</summary>
        static double Erfc(double x)
        {
            // Numerical Recipes erfc approximation, relative error < 1.2e-7.
            double z = Math.Abs(x), t = 1 / (1 + 0.5 * z);
            double r = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                       t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                       t * (-0.82215223 + t * 0.17087277)))))))));
            return x >= 0 ? r : 2 - r;
        }
    }
}
