using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Maps a batch of queries to probability rows over the species' actions (DEC-11).</summary>
    /// <remarks>
    /// <para>A brain is a world service: place the component under the World's services, then pick it as the World's
    /// default brain or as one species' brain (DEC-13). Derive from it, give it an <see cref="Id"/> and implement
    /// <see cref="Ask"/>; override the limits (<see cref="MaxActions"/>, <see cref="MaxPromptTokens"/>,
    /// <see cref="AcceptsAttachments"/>, <see cref="CanMixSpecies"/>), which validation checks against every species that
    /// uses the brain before the run (DEC-14, V-10, V-11, V-51). A brain behind a model server derives from
    /// <c>HttpBrain</c> instead, which adds retries, time-outs and parallel requests.</para>
    /// <para>Once per tick the ask-brains phase builds one <see cref="DecisionQuery"/> per deciding animal, answers what it
    /// can from the memo and the answer cache, and calls <see cref="Ask"/> once with the rest (once per species when
    /// <see cref="CanMixSpecies"/> is false). The choose-actions phase waits for the <see cref="BrainAnswer"/>, checks each
    /// row, stores good rows in the memo and the cache, and draws the action.</para>
    /// <para>What a brain must keep: one row per query, in query order, one entry per action of the query's species in
    /// the species' action order, every entry ≥ 0, summing to 1 ± 1e-6 (DEC-11); the same query always gives the same row
    /// (DEC-12), unless <see cref="Memoizable"/> says otherwise; and it never moves an animal or changes the world
    /// (CORE-06).</para>
    /// </remarks>
    /// <example>
    /// The test brain <c>MemoryBrain</c>: uniform rows, but not memoizable, so it sees every deciding animal's id.
    /// <code>
    /// public class MemoryBrain : Brain
    /// {
    ///     public readonly List&lt;int&gt; AnimalIds = new List&lt;int&gt;();
    ///
    ///     public override string Id => "memory";
    ///     public override bool Memoizable => false;
    ///
    ///     public override BrainAnswer Ask(IReadOnlyList&lt;DecisionQuery&gt; batch)
    ///     {
    ///         var rows = new float[batch.Count][];
    ///         for (int i = 0; i &lt; batch.Count; i++)
    ///         {
    ///             AnimalIds.Add(batch[i].AnimalId);
    ///             rows[i] = Uniform(batch[i].Species.Actions.Count);
    ///         }
    ///         return BrainAnswer.Now(rows);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Brain : WorldService
    {
        /// <summary>
        /// The Inspector's strict setting: a failure after retries stops the run cleanly (DEC-40); off, the decision gets
        /// a uniform row and is counted as a failure. Off by default; on for HTTP brains.
        /// </summary>
        [SerializeField, Tooltip("A failure after retries stops the run cleanly (DEC-40); off: uniform rows, counted.")]
        protected bool strict;

        /// <summary>
        /// The Inspector's cache setting: store and reuse answers in the answer cache across runs (DEC-33). Off by default;
        /// on for HTTP brains.
        /// </summary>
        [SerializeField, Tooltip("Store and reuse answers in the answer cache across runs (DEC-33).")]
        protected bool useCache;

        /// <summary>
        /// A short name of the brain and its settings ("random", "tinynet-8"): part of memo and cache keys, and the name of
        /// its answer cache file (DEC-30, DEC-33). Two brains that may answer differently need different ids.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>The most actions a species may have with this brain (DEC-14, V-11); no limit by default.</summary>
        public virtual int MaxActions => int.MaxValue;

        /// <summary>The longest prompt this brain takes, in estimated tokens (DEC-14, V-11); 0 = no prompt or no limit.</summary>
        public virtual int MaxPromptTokens => 0;

        /// <summary>
        /// True when queries may carry attachments (SENSE-41); false by default, and then V-51 rejects senses that give them.
        /// </summary>
        public virtual bool AcceptsAttachments => false;

        /// <summary>True when one batch may hold queries of several species (DEC-13); false gives one batch per species.</summary>
        public virtual bool CanMixSpecies => true;

        /// <summary>
        /// The answer instruction that ends the prompt, hard-coded per brain because its parsing depends on it (PROMPT-07);
        /// empty for brains that read no prompt.
        /// </summary>
        public virtual string AnswerInstruction => "";

        /// <summary>The model and its pinned revision or digest, part of cache keys (DEC-33); the <see cref="Id"/> by default.</summary>
        public virtual string ModelIdentity => Id;

        /// <summary>The brain's mode (points, choice…), part of cache keys (DEC-33); empty by default.</summary>
        public virtual string Mode => "";

        /// <summary>
        /// True when a failure after retries stops the run cleanly (DEC-40); false when the decision gets a uniform row and
        /// is counted as a failure. Never cached either way (DEC-34).
        /// </summary>
        public bool Strict { get => strict; set => strict = value; }

        /// <summary>
        /// True when the ask-brains phase looks answers up in the world's <see cref="AnswerCache"/> and stores new ones there
        /// (DEC-33).
        /// </summary>
        public bool UsesCache { get => useCache; set => useCache = value; }

        /// <summary>
        /// True (reference): equal queries get equal rows, so they are memoised and shared within a tick (DEC-30, DEC-32).
        /// A brain with memory, or one that learns, says false: every query then reaches it, with its animal's id.
        /// </summary>
        /// <remarks>False also turns the answer cache off for this brain's queries.</remarks>
        public virtual bool Memoizable => true;

        /// <summary>Answers a batch of queries (DEC-11). Fast brains fill the answer at once; HTTP brains later.</summary>
        /// <param name="batch">The queries the memo and the cache could not answer, at most one per memo key (DEC-32); never empty.</param>
        /// <returns>
        /// One row or one failure per query, in query order: <see cref="BrainAnswer.Now"/> when every row is ready, or
        /// <see cref="BrainAnswer.Later(int)"/> filled from another thread, then completed.
        /// </returns>
        /// <remarks>
        /// Called on the main thread by the ask-brains phase, once per tick and per batch. Read only the queries: a row must
        /// depend on the query alone (DEC-12), and the brain must not change the world (CORE-06). A query it can't answer is
        /// marked with <see cref="BrainAnswer.Fail"/>, never answered with a made-up row.
        /// </remarks>
        public abstract BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch);

        /// <summary>Makes a uniform row over n actions (the random brain, and the non-strict failure answer).</summary>
        /// <param name="n">The number of actions, at least 1.</param>
        /// <returns>A new row of n entries, each 1/n.</returns>
        public static float[] Uniform(int n)
        {
            var row = new float[n];
            for (int i = 0; i < n; i++) row[i] = 1f / n;
            return row;
        }

        /// <summary>Makes a probability row from non-negative weights, as floats summing to 1 within 1e-6 (DEC-11).</summary>
        /// <param name="weights">One weight per action, in action order; negative and NaN weights count as 0.</param>
        /// <returns>A new row; uniform when every weight is 0 or the sum is infinite.</returns>
        public static float[] Normalize(double[] weights)
        {
            int n = weights.Length;
            double sum = 0;
            foreach (var w in weights) sum += w > 0 ? w : 0;
            if (!(sum > 0) || double.IsInfinity(sum)) return Uniform(n);
            var row = new float[n];
            double total = 0;
            int largest = 0;
            for (int i = 0; i < n; i++)
            {
                row[i] = (float)((weights[i] > 0 ? weights[i] : 0) / sum);
                total += row[i];
                if (row[i] > row[largest]) largest = i;
            }
            row[largest] = (float)(row[largest] + (1.0 - total));        // float rounding goes to the largest entry
            if (row[largest] < 0f) row[largest] = 0f;
            return row;
        }

        /// <summary>Estimates a prompt's length in model tokens: one token per 4 characters, rounded up (PROMPT-06).</summary>
        /// <param name="prompt">The full prompt text; null or empty counts 0.</param>
        /// <returns>The estimated token count, compared with <see cref="MaxPromptTokens"/>.</returns>
        public static int EstimateTokens(string prompt) => string.IsNullOrEmpty(prompt) ? 0 : (prompt.Length + 3) / 4;
    }
}
