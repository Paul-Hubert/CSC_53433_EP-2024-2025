using System;
using System.Text;

namespace EvoSim
{
    /// <summary>The tuple of an animal's sense tokens at one decision, in sense order (SENSE-02).</summary>
    /// <remarks>
    /// <para>Entry k is the token index the species' k-th sense returned (<see cref="Sense.Index"/>); the species turns it
    /// into the situation text with <see cref="Species.Describe"/>. Two observations are equal when their tuples are
    /// equal: positions, ids and anything else the senses did not report are not part of it.</para>
    /// <para>It is the part of the memo key that changes from decision to decision (DEC-30), so a species with few
    /// possible observations gets many memo hits. Read a token by sense label with <see cref="ObservationView"/>.</para>
    /// </remarks>
    public sealed class Observation : IEquatable<Observation>
    {
        readonly int[] tokens;   // one token index per sense, in sense order; kept as given, not copied
        string key;              // Key, built on first use

        /// <summary>Makes an observation from token indices in sense order.</summary>
        /// <param name="tokens">One token index per sense of the species. The array is kept, not copied: don't change it
        /// afterwards. Null gives an empty observation (a species without senses).</param>
        public Observation(int[] tokens) { this.tokens = tokens ?? Array.Empty<int>(); }

        /// <summary>The number of tokens: the number of the species' senses.</summary>
        public int Count => tokens.Length;

        /// <summary>The token index the sense at this place in the sense order returned.</summary>
        /// <param name="sense">The sense's place in the species' sense order (<see cref="Sense.Index"/>).</param>
        public int this[int sense] => tokens[sense];

        /// <summary>
        /// A compact text key, e.g. "0.2.5": the token indices joined by dots. Equal tuples give equal keys; the memo and
        /// the logs use it.
        /// </summary>
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

        /// <summary>Compares two observations token by token (SENSE-02).</summary>
        /// <param name="o">The other observation, or null.</param>
        /// <returns>True when both have the same tokens in the same order; false for null.</returns>
        public bool Equals(Observation o)
        {
            if (o == null || o.tokens.Length != tokens.Length) return false;
            for (int i = 0; i < tokens.Length; i++) if (tokens[i] != o.tokens[i]) return false;
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as Observation);

        /// <summary>A hash of <see cref="Key"/>, so equal observations hash alike.</summary>
        /// <inheritdoc/>
        public override int GetHashCode() => Key.GetHashCode();

        /// <summary>Gives <see cref="Key"/>, e.g. "0.2.5".</summary>
        /// <inheritdoc/>
        public override string ToString() => Key;
    }
}
