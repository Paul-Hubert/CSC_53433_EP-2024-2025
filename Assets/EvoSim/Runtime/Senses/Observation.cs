using System;
using System.Text;

namespace EvoSim
{
    /// <summary>The tuple of an animal's sense tokens at one decision, in sense order (SENSE-02).</summary>
    public sealed class Observation : IEquatable<Observation>
    {
        readonly int[] tokens;
        string key;

        public Observation(int[] tokens) { this.tokens = tokens ?? Array.Empty<int>(); }

        public int Count => tokens.Length;
        public int this[int sense] => tokens[sense];

        /// <summary>A compact text key, e.g. "0.2.5"; equal tuples give equal keys.</summary>
        public string Key
        {
            get
            {
                if (key != null) return key;
                var sb = new StringBuilder(tokens.Length * 2);
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (i > 0) sb.Append('.');
                    sb.Append(tokens[i]);
                }
                return key = sb.ToString();
            }
        }

        public bool Equals(Observation o)
        {
            if (o == null || o.tokens.Length != tokens.Length) return false;
            for (int i = 0; i < tokens.Length; i++) if (tokens[i] != o.tokens[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as Observation);
        public override int GetHashCode() => Key.GetHashCode();
        public override string ToString() => Key;
    }
}
