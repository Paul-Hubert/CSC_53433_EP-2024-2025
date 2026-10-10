using System.Collections.Generic;
using System.Threading;

namespace EvoSim
{
    /// <summary>What a brain's or mutator's HTTP calls did: tries, retries, failures, latencies (B-01, brain health window).</summary>
    public sealed class HttpStats
    {
        readonly object gate = new object();
        readonly List<double> latencies = new List<double>();
        int tries, retries, failures, timeouts;

        public int Tries => tries;
        public int Retries => retries;
        public int Failures => failures;
        public int Timeouts => timeouts;

        public void AddTry() => Interlocked.Increment(ref tries);
        public void AddRetry() => Interlocked.Increment(ref retries);
        public void AddFailure() => Interlocked.Increment(ref failures);
        public void AddTimeout() => Interlocked.Increment(ref timeouts);

        public void AddLatency(double seconds)
        {
            lock (gate) latencies.Add(seconds);
        }

        /// <summary>A copy of the latencies of the successful tries, in seconds, in completion order.</summary>
        public List<double> Latencies
        {
            get { lock (gate) return new List<double>(latencies); }
        }

        public void Reset()
        {
            lock (gate) latencies.Clear();
            tries = retries = failures = timeouts = 0;
        }
    }
}
