using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>
    /// The world's events and their running hash (RAND-10). The hash is chained:
    /// h0 = SHA-256 of the seeds, h(i) = SHA-256(h(i-1) ‖ UTF-8(line i)), so it can be read after any event.
    /// </summary>
    /// <remarks>
    /// <para>The World creates one per run (<see cref="World.Events"/>). Code records an event with <see cref="Record"/>;
    /// every sink (the run recorder, the editor's event log, tests) then receives it with its canonical line. The final
    /// hash identifies the run: the same configuration, seeds and answer caches give the same hash (RAND-11).</para>
    /// <para>Record events only from the simulation (phases and modules), in a fixed order: an event recorded from the
    /// visuals or in a varying order would change the hash.</para>
    /// </remarks>
    public sealed class EventLog
    {
        readonly List<IEventSink> sinks = new List<IEventSink>();      // receive every event, in the order they were added
        readonly SHA256 sha = SHA256.Create();                         // one hasher for the whole run
        byte[] hash;                                                   // the running hash, 32 bytes
        byte[] buffer = new byte[256];                                 // previous hash + UTF-8 line, reused and grown as needed

        /// <summary>
        /// The chain starts from the hash of the seeds, so two seeds never share a hash, even for a world
        /// whose events happen to be equal (RAND-13).
        /// </summary>
        /// <param name="seed">The run seed.</param>
        /// <param name="worldSeed">The world seed (the map's), which may differ from the run seed (RAND-04).</param>
        public EventLog(long seed = 0, long worldSeed = 0)
        {
            hash = sha.ComputeHash(Encoding.UTF8.GetBytes("evosim:" + seed + ":" + worldSeed));
        }

        /// <summary>How many events were recorded so far.</summary>
        public long Count { get; private set; }
        /// <summary>The running hash, 64 hex digits.</summary>
        public string Hash => Hashing.ToHex(hash);
        /// <summary>The first 16 hex digits, as the summary prints it.</summary>
        public string ShortHash => Hashing.ToHex(hash, 16);
        /// <summary>Optional: keep every line in memory (tests).</summary>
        public List<string> Lines { get; set; }

        /// <summary>Adds a receiver of every event from now on; adding it twice does nothing.</summary>
        /// <param name="sink">The receiver, e.g. a <see cref="RunRecorder"/>.</param>
        public void AddSink(IEventSink sink) { if (!sinks.Contains(sink)) sinks.Add(sink); }
        /// <summary>Stops sending events to a receiver.</summary>
        /// <param name="sink">The receiver to remove.</param>
        public void RemoveSink(IEventSink sink) => sinks.Remove(sink);

        /// <summary>Records an event: adds its canonical line to the hash, counts it, and passes it to every sink.</summary>
        /// <remarks>
        /// Modules record their own event kinds the same way, e.g.
        /// <c>World.Events.Record(new SimEvent("egg_lost", World.Tick, egg.Id, species.Id).With("reason", reason))</c>.
        /// </remarks>
        /// <param name="e">The event; change it no more once recorded.</param>
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
