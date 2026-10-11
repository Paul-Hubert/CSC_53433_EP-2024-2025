using System;

namespace EvoSim
{
    /// <summary>
    /// A brain's answer to a batch (DEC-11): one probability row per query, in query order, or a failure per query.
    /// Fast brains fill it at once; HTTP brains later, from another thread, then mark it done.
    /// </summary>
    /// <remarks>
    /// <para>Answer now with <see cref="Now"/>. Answer later with <see cref="Later(int)"/>: fill each entry with
    /// <see cref="SetRow"/> or <see cref="Fail"/>, from any thread, then call <see cref="Complete"/> exactly once, also when
    /// everything failed: the choose-actions phase waits on <see cref="Pending"/> until then (TICK-03), and in the Freeze wait
    /// mode it blocks the main thread.</para>
    /// <para>The choose-actions phase reads each entry with <see cref="Row"/> and <see cref="Failed"/>, checks it with
    /// <see cref="Check"/>, and treats a bad row like a failure.</para>
    /// </remarks>
    public sealed class BrainAnswer
    {
        readonly float[][] rows;                                         // one row per query, null until set or after a failure
        readonly string[] errors;                                        // the failure message per query, or null
        readonly ManualPending pending;                                  // the pending Complete() marks done; null when given one
        readonly long started = System.Diagnostics.Stopwatch.GetTimestamp();   // when the batch was created, in stopwatch ticks
        volatile bool done;                                              // set by Complete(), read from the main thread

        /// <summary>The number of queries in the batch, so of rows.</summary>
        public int Count => rows.Length;

        /// <summary>True once <see cref="Complete"/> was called.</summary>
        public bool IsDone => done;

        /// <summary>What the Choose actions phase waits for (TICK-03).</summary>
        public Pending Pending { get; }

        /// <summary>
        /// Model calls this batch made (cached answers excluded), for the summary. The brain sets it; 0 for brains without a
        /// model.
        /// </summary>
        public int ModelCalls { get; set; }

        /// <summary>
        /// Wall-clock time from the batch's creation to its completion, in milliseconds (the summary's backend_s, 13 §5).
        /// </summary>
        public double Milliseconds { get; private set; }

        /// <summary>Makes an empty answer for a batch.</summary>
        /// <param name="count">The number of queries.</param>
        /// <param name="wait">What callers wait for, or null for a pending that <see cref="Complete"/> marks done.</param>
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

        /// <summary>Makes an answer given at once, already complete.</summary>
        /// <param name="rows">One probability row per query, in query order; a null row counts as a failure.</param>
        /// <returns>The complete answer.</returns>
        public static BrainAnswer Now(float[][] rows)
        {
            var a = new BrainAnswer(rows.Length, null);
            for (int i = 0; i < rows.Length; i++) a.rows[i] = rows[i];
            a.Complete();
            return a;
        }

        /// <summary>Makes an answer that will be filled later; call <see cref="Complete"/> when every row or failure is in.</summary>
        /// <param name="count">The number of queries in the batch.</param>
        /// <returns>An empty answer, not done yet.</returns>
        public static BrainAnswer Later(int count) => new BrainAnswer(count, null);

        /// <summary>Makes an answer filled later, done when the given pending is done (fakes that answer after N calls).</summary>
        /// <param name="count">The number of queries in the batch.</param>
        /// <param name="wait">What the choose-actions phase waits for instead of this answer's own pending.</param>
        /// <returns>An empty answer, not done yet; <see cref="Complete"/> is still needed.</returns>
        public static BrainAnswer Later(int count, Pending wait) => new BrainAnswer(count, wait);

        /// <summary>Sets the probability row of one query.</summary>
        /// <param name="i">The query's index in the batch.</param>
        /// <param name="row">One entry per action of the query's species, in action order, ≥ 0, summing to 1 (DEC-11).</param>
        public void SetRow(int i, float[] row) => rows[i] = row;

        /// <summary>The query failed (after retries): the row is not cached (DEC-34) and strict brains stop the run (DEC-40).</summary>
        /// <param name="i">The query's index in the batch.</param>
        /// <param name="error">What went wrong, for the log; null gives "failed".</param>
        public void Fail(int i, string error) { rows[i] = null; errors[i] = error ?? "failed"; }

        /// <summary>
        /// Marks the answer done and records its time: call it once, after the last <see cref="SetRow"/> or
        /// <see cref="Fail"/>. <see cref="Now"/> calls it for you.
        /// </summary>
        public void Complete()
        {
            Milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            done = true;
            pending?.SetDone();
        }

        /// <summary>Gives the row of one query.</summary>
        /// <param name="i">The query's index in the batch.</param>
        /// <returns>The row, or null when the query failed or has no answer yet.</returns>
        public float[] Row(int i) => rows[i];

        /// <summary>Tells whether a query has no row: it failed, or the brain never answered it.</summary>
        /// <param name="i">The query's index in the batch.</param>
        /// <returns>True when there is no row.</returns>
        public bool Failed(int i) => rows[i] == null;

        /// <summary>Gives why a query has no row.</summary>
        /// <param name="i">The query's index in the batch.</param>
        /// <returns>The failure message, "no answer" when the row was never set, or null when there is a row.</returns>
        public string Error(int i) => errors[i] ?? (rows[i] == null ? "no answer" : null);

        /// <summary>Checks a row: one entry per action, all ≥ 0, summing to 1 ± 1e-6 (DEC-11).</summary>
        /// <param name="row">The row to check, or null.</param>
        /// <param name="actions">The number of actions of the query's species.</param>
        /// <returns>What is wrong ("no row", "3 entries for 5 actions", "sums to 0.9"…), or null when the row is fine.</returns>
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
