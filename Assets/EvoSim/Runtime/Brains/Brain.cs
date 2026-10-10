using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Maps a batch of queries to probability rows over the species' actions (DEC-11).</summary>
    public abstract class Brain : WorldService
    {
        [SerializeField, Tooltip("A failure after retries stops the run cleanly (DEC-40); off: uniform rows, counted.")]
        protected bool strict;
        [SerializeField, Tooltip("Store and reuse answers in the answer cache across runs (DEC-33).")]
        protected bool useCache;

        /// <summary>Part of memo and cache keys (DEC-30, DEC-33).</summary>
        public abstract string Id { get; }

        /// <summary>The most actions a species may have with this brain (DEC-14).</summary>
        public virtual int MaxActions => int.MaxValue;

        /// <summary>The longest prompt this brain takes, in estimated tokens (DEC-14); 0 = no prompt or no limit.</summary>
        public virtual int MaxPromptTokens => 0;

        /// <summary>Whether queries may carry attachments (SENSE-41).</summary>
        public virtual bool AcceptsAttachments => false;

        /// <summary>Whether one batch may hold queries of several species (DEC-13).</summary>
        public virtual bool CanMixSpecies => true;

        /// <summary>The answer instruction that ends the prompt, hard-coded per brain because its parsing depends on it (PROMPT-07).</summary>
        public virtual string AnswerInstruction => "";

        /// <summary>The model and its pinned revision or digest, part of cache keys (DEC-33).</summary>
        public virtual string ModelIdentity => Id;

        /// <summary>The brain's mode (points, choice…), part of cache keys (DEC-33).</summary>
        public virtual string Mode => "";

        public bool Strict { get => strict; set => strict = value; }
        public bool UsesCache { get => useCache; set => useCache = value; }

        /// <summary>
        /// True (reference): equal queries get equal rows, so they are memoised and shared within a tick (DEC-30, DEC-32).
        /// A brain with memory, or one that learns, says false: every query then reaches it, with its animal's id.
        /// </summary>
        public virtual bool Memoizable => true;

        /// <summary>Answer a batch (DEC-11). Fast brains fill the answer at once; HTTP brains later.</summary>
        public abstract BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch);

        /// <summary>A uniform row over n actions (the random brain, and the non-strict failure answer).</summary>
        public static float[] Uniform(int n)
        {
            var row = new float[n];
            for (int i = 0; i < n; i++) row[i] = 1f / n;
            return row;
        }

        /// <summary>A probability row from non-negative weights, as floats summing to 1 within 1e-6 (DEC-11); all zero = uniform.</summary>
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

        /// <summary>The prompt's estimated token count: one token per 4 characters (PROMPT-06).</summary>
        public static int EstimateTokens(string prompt) => string.IsNullOrEmpty(prompt) ? 0 : (prompt.Length + 3) / 4;
    }
}
