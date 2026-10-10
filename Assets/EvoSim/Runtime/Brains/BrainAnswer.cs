using System;

namespace EvoSim
{
    /// <summary>
    /// A brain's answer to a batch (DEC-11): one probability row per query, in query order, or a failure per query.
    /// Fast brains fill it at once; HTTP brains later, from another thread, then mark it done.
    /// </summary>
    public sealed class BrainAnswer
    {
        readonly float[][] rows;
        readonly string[] errors;
        readonly ManualPending pending;
        volatile bool done;

        public int Count => rows.Length;
        public bool IsDone => done;
        /// <summary>What the Choose actions phase waits for (TICK-03).</summary>
        public Pending Pending { get; }
        /// <summary>Model calls this batch made (cached answers excluded), for the summary.</summary>
        public int ModelCalls { get; set; }

        BrainAnswer(int count, Pending wait)
        {
            rows = new float[count][];
            errors = new string[count];
            if (wait != null) Pending = wait;
            else
            {
                pending = new ManualPending();
                Pending = pending;
            }
        }

        /// <summary>An answer given at once.</summary>
        public static BrainAnswer Now(float[][] rows)
        {
            var a = new BrainAnswer(rows.Length, null);
            for (int i = 0; i < rows.Length; i++) a.rows[i] = rows[i];
            a.Complete();
            return a;
        }

        /// <summary>An answer that will be filled later; call Complete() when every row or failure is in.</summary>
        public static BrainAnswer Later(int count) => new BrainAnswer(count, null);

        /// <summary>An answer filled later, done when the given pending is done (fakes that answer after N calls).</summary>
        public static BrainAnswer Later(int count, Pending wait) => new BrainAnswer(count, wait);

        public void SetRow(int i, float[] row) => rows[i] = row;

        /// <summary>The query failed (after retries): the row is not cached (DEC-34) and strict brains stop the run (DEC-40).</summary>
        public void Fail(int i, string error) { rows[i] = null; errors[i] = error ?? "failed"; }

        public void Complete()
        {
            done = true;
            pending?.SetDone();
        }

        public float[] Row(int i) => rows[i];
        public bool Failed(int i) => rows[i] == null;
        public string Error(int i) => errors[i] ?? (rows[i] == null ? "no answer" : null);

        /// <summary>Checks a row: one entry per action, all ≥ 0, summing to 1 ± 1e-6 (DEC-11).</summary>
        public static string Check(float[] row, int actions)
        {
            if (row == null) return "no row";
            if (row.Length != actions) return $"{row.Length} entries for {actions} actions";
            double sum = 0;
            foreach (var p in row)
            {
                if (float.IsNaN(p) || p < 0f) return "a negative or NaN entry";
                sum += p;
            }
            return Math.Abs(sum - 1.0) <= 1e-6 ? null : $"sums to {sum}";
        }
    }
}
