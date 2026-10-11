using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Within a run, the answer for a key (species, brain, brain-visible genome key, observation) is computed once and
    /// reused (DEC-30). A hit gives exactly what the brain answered. Failed answers are never stored (DEC-34).
    /// Keys are kept as 128-bit hashes and identical rows are shared, so a long run's memo stays small (R-06).
    /// </summary>
    /// <remarks>
    /// <para>The World holds one, in <see cref="DecisionState.Memo"/>, cleared when a run starts. The ask-brains phase looks
    /// each decision up with <see cref="TryGet"/> before asking the brain; the choose-actions phase stores fresh answers,
    /// and answers read from the answer cache, with <see cref="Store"/>.</para>
    /// <para>Rows are shared between keys: never change a row the memo gave you.</para>
    /// </remarks>
    public sealed class DecisionMemo
    {
        /// <summary>A 128-bit hash of a key or of a row's contents.</summary>
        readonly struct Id : IEquatable<Id>
        {
            readonly ulong a, b;   // the two 64-bit halves
            /// <summary>Makes a hash from its two halves.</summary>
            public Id(ulong a, ulong b) { this.a = a; this.b = b; }
            /// <summary>True when both halves are equal.</summary>
            public bool Equals(Id o) => a == o.a && b == o.b;
            /// <inheritdoc/>
            public override bool Equals(object obj) => obj is Id o && Equals(o);
            /// <inheritdoc/>
            public override int GetHashCode() => (int)(a ^ (a >> 32) ^ b);
        }

        readonly Dictionary<Id, float[]> rows = new Dictionary<Id, float[]>();           // key hash: the stored row
        readonly Dictionary<Id, float[]> distinctRows = new Dictionary<Id, float[]>();   // content hash: one shared copy of each row

        /// <summary>The number of keys stored.</summary>
        public int Count => rows.Count;

        /// <summary>
        /// Most keys kept; a full memo stores no new key (results don't change: brains are deterministic, DEC-12). 0 = no
        /// limit.
        /// </summary>
        /// <remarks>Set from the World's memo capacity at the start of a run (reference 262 144 keys, about 10 MB). Beyond it,
        /// new situations go to the brain, and its answer cache, every time.</remarks>
        public int Capacity { get; set; }

        /// <summary>Different rows among the stored answers (they are shared between keys).</summary>
        public int DistinctRows => distinctRows.Count;

        /// <summary>Lookups answered from the memo since the last <see cref="Clear"/>.</summary>
        public int Hits { get; private set; }

        /// <summary>Builds the memo key of a decision (DEC-30).</summary>
        /// <param name="s">The deciding animal's species.</param>
        /// <param name="b">Its brain, or null ("-").</param>
        /// <param name="genomeKey">The brain-visible genome key (DEC-31): <see cref="DecisionQuery.GenomeKey"/>.</param>
        /// <param name="o">The animal's observation.</param>
        /// <returns>"species|brain|genome key|observation key"; the ask-brains phase appends "|" and the attachment key when
        /// there is one.</returns>
        public static string Key(Species s, Brain b, string genomeKey, Observation o) =>
            s.Id + "|" + (b != null ? b.Id : "-") + "|" + genomeKey + "|" + o.Key;

        /// <summary>Looks a key up and counts a hit.</summary>
        /// <param name="key">A key from <see cref="Key"/>, or null (never found).</param>
        /// <param name="row">The stored row, shared: don't change it. Null when not found.</param>
        /// <returns>True when the key was found.</returns>
        public bool TryGet(string key, out float[] row)
        {
            if (key != null && rows.TryGetValue(Of(key), out row)) { Hits++; return true; }
            row = null;
            return false;
        }

        /// <summary>Stores the row of a key, sharing it with any equal row already stored.</summary>
        /// <param name="key">A key from <see cref="Key"/>; null stores nothing.</param>
        /// <param name="row">A good row, never a failure (DEC-34); null stores nothing. Don't change it afterwards.</param>
        /// <remarks>When the memo is full (<see cref="Capacity"/>), a new key is not stored; a known key is still updated.</remarks>
        public void Store(string key, float[] row)
        {
            if (key == null || row == null) return;
            var id = Of(key);
            if (Capacity > 0 && rows.Count >= Capacity && !rows.ContainsKey(id)) return;
            var content = Of(row);
            if (distinctRows.TryGetValue(content, out var known) && Same(known, row)) row = known;
            else distinctRows[content] = row;
            rows[id] = row;
        }

        /// <summary>Forgets every key and row and resets <see cref="Hits"/>: the World calls it when a run starts.</summary>
        public void Clear()
        {
            rows.Clear();
            distinctRows.Clear();
            Hits = 0;
        }

        // Two 64-bit hashes (FNV-1a, and an FNV-style variant with another offset and an added constant): a collision
        // between two keys of one run is out of reach.
        /// <summary>The 128-bit hash of a key.</summary>
        static Id Of(string s)
        {
            ulong a = 14695981039346656037UL, b = 0x9E3779B97F4A7C15UL;
            for (int i = 0; i < s.Length; i++)
            {
                a = (a ^ s[i]) * 1099511628211UL;
                b = (b ^ s[i]) * 0x100000001B3UL + 0x632BE59BD9B4E019UL;
            }
            return new Id(a, b ^ (ulong)s.Length);
        }

        /// <summary>The 128-bit hash of a row's contents, bit for bit, to share equal rows.</summary>
        static Id Of(float[] row)
        {
            ulong a = 14695981039346656037UL, b = 0x9E3779B97F4A7C15UL;
            for (int i = 0; i < row.Length; i++)
            {
                uint bits = (uint)BitConverter.SingleToInt32Bits(row[i]);
                a = (a ^ bits) * 1099511628211UL;
                b = (b ^ bits) * 0x100000001B3UL + 0x632BE59BD9B4E019UL;
            }
            return new Id(a, b ^ (ulong)row.Length);
        }

        /// <summary>True when two rows have the same entries, so one copy can serve both.</summary>
        static bool Same(float[] x, float[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }
    }
}
