using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A free vector gene for tests (plan M16 §4 C6): a few founder vectors of fixed length, values within ±<see cref="Clamp"/>;
    /// sets no trait and writes nothing into the prompt. The T-MUT suite puts it beside a text and a number gene, so an
    /// operator may accept any of the three kinds (30 §4).
    /// </summary>
    public class VectorProbeGene : Gene
    {
        /// <summary>The length of every vector of this gene.</summary>
        public int Length = 8;
        /// <summary>The largest absolute value allowed (<see cref="Check"/>).</summary>
        public float Clamp = 10f;

        List<FounderAllele> pool;                                         // built on first use

        /// <inheritdoc/>
        public override AlleleKind Kind => AlleleKind.Vector;

        /// <summary>Three founders: 0.1·(i + 1), its negative, and 0.25·(−1)^i.</summary>
        /// <inheritdoc/>
        public override IReadOnlyList<FounderAllele> FounderPool
        {
            get
            {
                if (pool != null) return pool;
                pool = new List<FounderAllele>();
                var a = new float[Length];
                var b = new float[Length];
                var c = new float[Length];
                for (int i = 0; i < Length; i++) { a[i] = 0.1f * (i + 1); b[i] = -a[i]; c[i] = i % 2 == 0 ? 0.25f : -0.25f; }
                foreach (var v in new[] { a, b, c }) pool.Add(new FounderAllele(AlleleValue.OfVector(v), "founder"));
                return pool;
            }
        }

        /// <summary>Sets no trait: the probe is only carried and mutated.</summary>
        /// <inheritdoc/>
        public override void Express(AlleleValue value, Expression e) { }

        /// <summary>Allowed: a vector of <see cref="Length"/> finite values within ±<see cref="Clamp"/>.</summary>
        /// <inheritdoc/>
        public override string Check(AlleleValue value)
        {
            if (value.Kind != AlleleKind.Vector) return "not a vector";
            if (value.VectorLength != Length) return $"length {value.VectorLength}, not {Length}";
            var v = value.Vector;
            for (int i = 0; i < v.Length; i++)
                if (float.IsNaN(v[i]) || float.IsInfinity(v[i]) || v[i] > Clamp || v[i] < -Clamp) return $"value {i} is {v[i]}";
            return null;
        }

        /// <summary>Forgets the founder pool, so a changed <see cref="Length"/> takes effect.</summary>
        /// <inheritdoc/>
        public override void Initialize() => pool = null;
    }
}
