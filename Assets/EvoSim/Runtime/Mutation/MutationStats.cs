using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Mutation statistics of a run (MUT-05): attempts, successes, model calls, rejections by reason.</summary>
    /// <remarks>
    /// The World keeps one (<see cref="World.Mutations"/>), cleared at the start of each run; the recorder writes it in the
    /// run summary (<c>tried</c>, <c>ok</c>, <c>calls</c>, <c>failed</c>, <c>rejected</c>).
    /// </remarks>
    public sealed class MutationStats
    {
        /// <summary>
        /// Counts of the run. Attempts: mutations started (loci that drew a mutation at conception, MUT-01). Successes:
        /// mutations that changed a baby's allele at hatching. ModelCalls: calls to the mutator model, cached answers excluded. CacheHits:
        /// answers read from the cache (MUT-14). Failures: mutations that left the inherited allele (MUT-04).
        /// </summary>
        public int Attempts, Successes, ModelCalls, CacheHits, Failures;
        /// <summary>
        /// Rejections by reason, in ordinal order: <c>invalid</c> and <c>unchanged</c> for mutator answers (MUT-12), and at
        /// hatching <c>not finished</c>, <c>not allowed</c> (refused by the gene) and <c>unchanged</c> (the parent's value).
        /// </summary>
        public readonly SortedDictionary<string, int> Rejections = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Counts one rejection.</summary>
        /// <param name="reason">The reason, e.g. <c>invalid</c>.</param>
        public void Reject(string reason) => Rejections[reason] = (Rejections.TryGetValue(reason, out var n) ? n : 0) + 1;

        /// <summary>All rejections, every reason together.</summary>
        public int RejectionsTotal
        {
            get
            {
                int n = 0;
                foreach (var kv in Rejections) n += kv.Value;
                return n;
            }
        }

        /// <summary>Sets every count back to zero; the World calls it at the start of a run.</summary>
        public void Clear()
        {
            Attempts = Successes = ModelCalls = CacheHits = Failures = 0;
            Rejections.Clear();
        }
    }
}
