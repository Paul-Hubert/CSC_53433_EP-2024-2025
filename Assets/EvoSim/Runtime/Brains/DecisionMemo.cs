using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Within a run, the answer for a key (species, brain, brain-visible genome key, observation) is computed once and
    /// reused (DEC-30). A hit gives exactly what the brain answered. Failed answers are never stored (DEC-34).
    /// Keys are kept as 128-bit hashes and identical rows are shared, so a long run's memo stays small (R-06).
    /// </summary>
    public sealed class DecisionMemo
    {
        readonly struct Id : IEquatable<Id>
        {
            readonly ulong a, b;
            public Id(ulong a, ulong b) { this.a = a; this.b = b; }
            public bool Equals(Id o) => a == o.a && b == o.b;
            public override bool Equals(object obj) => obj is Id o && Equals(o);
            public override int GetHashCode() => (int)(a ^ (a >> 32) ^ b);
        }

        readonly Dictionary<Id, float[]> rows = new Dictionary<Id, float[]>();
        readonly Dictionary<Id, float[]> distinctRows = new Dictionary<Id, float[]>();

        public int Count => rows.Count;
        /// <summary>Most keys kept; a full memo stores no new key (results don't change: brains are deterministic, DEC-12). 0 = no limit.</summary>
        public int Capacity { get; set; }
        /// <summary>Different rows among the stored answers (they are shared between keys).</summary>
        public int DistinctRows => distinctRows.Count;
        public int Hits { get; private set; }

        public static string Key(Species s, Brain b, string genomeKey, Observation o) =>
            s.Id + "|" + (b != null ? b.Id : "-") + "|" + genomeKey + "|" + o.Key;

        public bool TryGet(string key, out float[] row)
        {
            if (key != null && rows.TryGetValue(Of(key), out row)) { Hits++; return true; }
            row = null;
            return false;
        }

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

        public void Clear()
        {
            rows.Clear();
            distinctRows.Clear();
            Hits = 0;
        }

        // Two FNV-1a 64-bit hashes with different offsets: a collision between two keys of one run is out of reach.
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

        static bool Same(float[] x, float[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }
    }
}
