using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>One named random stream: a PCG32 generator seeded from SHA-256 of the seed and the name (RAND-02).</summary>
    /// <remarks>
    /// <para>Never make one yourself in simulation code: ask the World's streams for it
    /// (<see cref="TickContext.Stream(Species, string)"/>, <see cref="RandomStreams.For"/>), so the same seed and name always
    /// give the same numbers, and draws in one stream never shift another's (RAND-02). Never use UnityEngine.Random or
    /// an unseeded System.Random (RAND-01).</para>
    /// <para>Each method's number of draws is fixed, except <see cref="Range(int, int)"/>, which redraws to avoid bias,
    /// and <see cref="Chance"/>, which draws nothing for a probability of 0 or 1. Draw in a fixed order, from ordered
    /// loops (RAND-05), and a seed repeats the run.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rng = t.Stream(a.Species, "actions");          // "&lt;species id&gt;/actions"
    /// float angle = rng.Range(0f, 360f);                  // degrees
    /// if (rng.Chance(0.3)) { /* ... */ }                  // true 30 % of the time
    /// int pick = rng.Range(0, candidates.Count);          // an index
    /// </code>
    /// </example>
    public sealed class RandomStream
    {
        /// <summary>The PCG32 multiplier of the underlying 64-bit linear congruential step.</summary>
        const ulong Multiplier = 6364136223846793005UL;
        ulong state, increment;        // PCG32 state and stream increment (odd)

        /// <summary>The stream's name, e.g. <c>prey/sampling</c> or <c>act-order</c>.</summary>
        public string Name { get; }
        /// <summary>Numbers drawn so far (for tests and the inspector).</summary>
        public long Draws { get; private set; }

        /// <summary>Makes the stream of this seed and name: PCG32 state and increment come from SHA-256 of "seed/name".</summary>
        /// <param name="seed">The run seed (or the map's seed for <c>world</c> streams).</param>
        /// <param name="name">The stream's name; null counts as empty.</param>
        /// <remarks>Simulation code asks <see cref="RandomStreams"/> instead: it makes each stream once.</remarks>
        public RandomStream(long seed, string name)
        {
            Name = name ?? "";
            byte[] hash;
            using (var sha = SHA256.Create())
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + Name));
            ulong initState = BitConverter.ToUInt64(hash, 0);
            ulong initSeq = BitConverter.ToUInt64(hash, 8);
            // The reference PCG32 seeding: pcg32_srandom_r.
            state = 0UL;
            increment = (initSeq << 1) | 1UL;
            NextUInt();
            state += initState;
            NextUInt();
            Draws = 0;
        }

        /// <summary>32 random bits.</summary>
        /// <returns>A uniform unsigned 32-bit integer. One draw.</returns>
        public uint NextUInt()
        {
            ulong old = state;
            state = unchecked(old * Multiplier + increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            Draws++;
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform in [0, 1), 24 bits.</summary>
        /// <returns>A float at least 0 and below 1. One draw.</returns>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>Uniform in [0, 1), 53 bits from two draws.</summary>
        /// <returns>A double at least 0 and below 1. Two draws.</returns>
        public double NextDouble()
        {
            ulong a = NextUInt() >> 5, b = NextUInt() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }

        /// <summary>Uniform integer in [min, maxExclusive), without modulo bias.</summary>
        /// <param name="min">The smallest value.</param>
        /// <param name="maxExclusive">One more than the largest value.</param>
        /// <returns>An integer from <paramref name="min"/> to <paramref name="maxExclusive"/> - 1; <paramref name="min"/>
        /// without any draw when the range is empty.</returns>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            uint span = (uint)(maxExclusive - min);
            uint threshold = (uint)(-span) % span;     // draws below it would favour small values: they are redrawn
            uint r;
            do r = NextUInt(); while (r < threshold);
            return min + (int)(r % span);
        }

        /// <summary>Uniform float in [min, max).</summary>
        /// <param name="min">The smallest value.</param>
        /// <param name="max">The upper bound, never reached.</param>
        /// <returns>A float at least <paramref name="min"/> and below <paramref name="max"/>. One draw.</returns>
        public float Range(float min, float max) => min + (max - min) * NextFloat();

        /// <summary>True with probability p.</summary>
        /// <param name="p">The probability, from 0 to 1.</param>
        /// <returns>True with probability <paramref name="p"/>. No draw when p ≤ 0 (always false) or p ≥ 1 (always true).</returns>
        public bool Chance(double p) => p > 0 && (p >= 1 || NextDouble() < p);

        /// <summary>A fair coin.</summary>
        /// <returns>True or false with equal probability. One draw.</returns>
        public bool NextBool() => (NextUInt() & 1u) == 1u;

        /// <summary>A standard normal draw (Box-Muller, two uniforms per call, no cached spare).</summary>
        /// <returns>A number from the normal distribution with mean 0 and standard deviation 1.</returns>
        public double NextGaussian()
        {
            double u1 = 1.0 - NextDouble(), u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>Draws an index with probability proportional to its weight; uniform when all weights are 0.</summary>
        /// <param name="weights">One weight per choice; negative weights count as 0.</param>
        /// <returns>An index into <paramref name="weights"/>.</returns>
        /// <remarks>The Choose actions phase draws each animal's action this way from the brain's tempered probabilities
        /// (DEC-20).</remarks>
        public int Choose(IReadOnlyList<float> weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0) return Range(0, weights.Count);
            double r = NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                r -= Math.Max(0f, weights[i]);
                if (r < 0) return i;
            }
            // Rounding left r just above 0: take the last choice that can be drawn.
            for (int i = weights.Count - 1; i >= 0; i--)
                if (weights[i] > 0) return i;
            return weights.Count - 1;
        }

        /// <summary>Fisher-Yates shuffle in place.</summary>
        /// <typeparam name="T">The kind of item.</typeparam>
        /// <param name="list">The list to put in a random order; every order is equally likely.</param>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
