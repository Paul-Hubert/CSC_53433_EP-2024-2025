using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>
    /// The world's events and their running hash (RAND-10). The hash is chained:
    /// h0 = SHA-256 of the seeds, h(i) = SHA-256(h(i-1) ‖ UTF-8(line i)), so it can be read after any event.
    /// </summary>
    public sealed class EventLog
    {
        readonly List<IEventSink> sinks = new List<IEventSink>();
        readonly SHA256 sha = SHA256.Create();
        byte[] hash;
        byte[] buffer = new byte[256];

        /// <summary>
        /// The chain starts from the hash of the seeds, so two seeds never share a hash, even for a world
        /// whose events happen to be equal (RAND-13).
        /// </summary>
        public EventLog(long seed = 0, long worldSeed = 0)
        {
            hash = sha.ComputeHash(Encoding.UTF8.GetBytes("evosim:" + seed + ":" + worldSeed));
        }

        public long Count { get; private set; }
        /// <summary>The running hash, 64 hex digits.</summary>
        public string Hash => Hashing.ToHex(hash);
        /// <summary>The first 16 hex digits, as the summary prints it.</summary>
        public string ShortHash => Hashing.ToHex(hash, 16);
        /// <summary>Optional: keep every line in memory (tests).</summary>
        public List<string> Lines { get; set; }

        public void AddSink(IEventSink sink) { if (!sinks.Contains(sink)) sinks.Add(sink); }
        public void RemoveSink(IEventSink sink) => sinks.Remove(sink);

        public void Record(SimEvent e)
        {
            string line = e.ToJson();
            int n = Encoding.UTF8.GetMaxByteCount(line.Length) + 32;
            if (buffer.Length < n) buffer = new byte[n * 2];
            System.Buffer.BlockCopy(hash, 0, buffer, 0, 32);
            int len = Encoding.UTF8.GetBytes(line, 0, line.Length, buffer, 32);
            hash = sha.ComputeHash(buffer, 0, 32 + len);
            Count++;
            Lines?.Add(line);
            for (int i = 0; i < sinks.Count; i++) sinks[i].OnEvent(e, line);
        }
    }
}
