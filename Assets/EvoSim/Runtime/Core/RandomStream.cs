using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>One named random stream: a PCG32 generator seeded from SHA-256 of the seed and the name (RAND-02).</summary>
    public sealed class RandomStream
    {
        const ulong Multiplier = 6364136223846793005UL;
        ulong state, increment;

        public string Name { get; }
        /// <summary>Numbers drawn so far (for tests and the inspector).</summary>
        public long Draws { get; private set; }

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
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>Uniform in [0, 1), 53 bits from two draws.</summary>
        public double NextDouble()
        {
            ulong a = NextUInt() >> 5, b = NextUInt() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }

        /// <summary>Uniform integer in [min, maxExclusive), without modulo bias.</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            uint span = (uint)(maxExclusive - min);
            uint threshold = (uint)(-span) % span;
            uint r;
            do r = NextUInt(); while (r < threshold);
            return min + (int)(r % span);
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();

        /// <summary>True with probability p.</summary>
        public bool Chance(double p) => p > 0 && (p >= 1 || NextDouble() < p);

        public bool NextBool() => (NextUInt() & 1u) == 1u;

        /// <summary>A standard normal draw (Box-Muller, two uniforms per call, no cached spare).</summary>
        public double NextGaussian()
        {
            double u1 = 1.0 - NextDouble(), u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>Draws an index with probability proportional to its weight; uniform when all weights are 0.</summary>
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
            for (int i = weights.Count - 1; i >= 0; i--)
                if (weights[i] > 0) return i;
            return weights.Count - 1;
        }

        /// <summary>Fisher-Yates shuffle in place.</summary>
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
