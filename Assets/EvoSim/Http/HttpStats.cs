using System.Collections.Generic;
using System.Threading;

namespace EvoSim
{
    /// <summary>What a brain's or mutator's HTTP calls did: tries, retries, failures, latencies (B-01, brain health window).</summary>
    /// <remarks>
    /// <see cref="HttpCaller"/> fills it from the thread pool: the counts are updated atomically and the latencies under a
    /// lock, so it can be read from the main thread at any time. A brain's counts start again from 0 when its client is
    /// made anew (at each run's Initialize, or after a setting changed).
    /// </remarks>
    public sealed class HttpStats
    {
        readonly object gate = new object();                                   // guards the latency list
        readonly List<double> latencies = new List<double>();
        int tries, retries, failures, timeouts;

        /// <summary>Tries made so far: first tries and retries.</summary>
        public int Tries => tries;
        /// <summary>Tries after the first one of a request.</summary>
        public int Retries => retries;
        /// <summary>Requests that failed for good: a non-retryable status, or every try used.</summary>
        public int Failures => failures;
        /// <summary>Tries that got no answer within the time-out.</summary>
        public int Timeouts => timeouts;

        /// <summary>Counts one try.</summary>
        public void AddTry() => Interlocked.Increment(ref tries);
        /// <summary>Counts one retry.</summary>
        public void AddRetry() => Interlocked.Increment(ref retries);
        /// <summary>Counts one request that failed for good.</summary>
        public void AddFailure() => Interlocked.Increment(ref failures);
        /// <summary>Counts one try that timed out.</summary>
        public void AddTimeout() => Interlocked.Increment(ref timeouts);

        /// <summary>Records the latency of one successful try.</summary>
        /// <param name="seconds">The time from sending the try to its answer, in seconds.</param>
        public void AddLatency(double seconds)
        {
            lock (gate) latencies.Add(seconds);
        }

        /// <summary>A copy of the latencies of the successful tries, in seconds, in completion order.</summary>
        public List<double> Latencies
        {
            get { lock (gate) return new List<double>(latencies); }
        }

        /// <summary>Sets every count back to 0 and forgets the latencies.</summary>
        public void Reset()
        {
            lock (gate) latencies.Clear();
            tries = retries = failures = timeouts = 0;
        }
    }
}
