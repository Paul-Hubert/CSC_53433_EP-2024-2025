using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Mutation statistics of a run (MUT-05): attempts, successes, model calls, rejections by reason.</summary>
    public sealed class MutationStats
    {
        public int Attempts, Successes, ModelCalls, CacheHits, Failures;
        public readonly SortedDictionary<string, int> Rejections = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public void Reject(string reason) => Rejections[reason] = (Rejections.TryGetValue(reason, out var n) ? n : 0) + 1;

        public int RejectionsTotal
        {
            get
            {
                int n = 0;
                foreach (var kv in Rejections) n += kv.Value;
                return n;
            }
        }

        public void Clear()
        {
            Attempts = Successes = ModelCalls = CacheHits = Failures = 0;
            Rejections.Clear();
        }
    }
}
