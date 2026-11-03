using System;
using System.Globalization;
using System.Text;

namespace EvoSim
{
    /// <summary>One allele value: a sentence, a number or a vector of numbers.</summary>
    /// <remarks>
    /// Make one with <see cref="OfText"/>, <see cref="OfNumber"/> or <see cref="OfVector"/>. A sentence has its spaces
    /// collapsed at once, so "Eat  when hungry. " and "Eat when hungry." are the same value. Two values are equal when they
    /// have the same kind and the same <see cref="Canonical"/> form at 4 decimals; the registry compares numbers and vectors
    /// at the gene's own precision (GENE-07, GENE-10).
    /// </remarks>
    public readonly struct AlleleValue : IEquatable<AlleleValue>
    {
        /// <summary>Whether it holds a sentence, a number or a vector.</summary>
        public readonly AlleleKind Kind;
        /// <summary>The sentence, spaces collapsed; null for a number or a vector.</summary>
        public readonly string Text;
        /// <summary>The number; 0 for a sentence or a vector.</summary>
        public readonly float Number;
        readonly float[] vector;                                       // a copy owned by this value; null unless a vector
        readonly string vectorCanonical;                               // Canonical(4) of a vector, computed once

        /// <summary>Builds a value; <see cref="OfText"/> and <see cref="OfNumber"/> call it.</summary>
        AlleleValue(AlleleKind kind, string text, float number) { Kind = kind; Text = text; Number = number; vector = null; vectorCanonical = null; }

        /// <summary>Builds a vector value around a copy it owns; <see cref="OfVector"/> calls it.</summary>
        AlleleValue(float[] owned)
        {
            Kind = AlleleKind.Vector;
            Text = null;
            Number = 0f;
            vector = owned;
            vectorCanonical = VectorCanonical(owned, 4);
        }

        /// <summary>A sentence value, its runs of whitespace collapsed into one space and trimmed (GENE-10).</summary>
        /// <param name="text">The sentence; null counts as empty.</param>
        /// <returns>A text value.</returns>
        public static AlleleValue OfText(string text) => new AlleleValue(AlleleKind.Text, CollapseSpaces(text ?? ""), 0f);
        /// <summary>A number value.</summary>
        /// <param name="number">The number, in the unit of the trait the gene sets.</param>
        /// <returns>A number value.</returns>
        public static AlleleValue OfNumber(float number) => new AlleleValue(AlleleKind.Number, null, number);
        /// <summary>A vector value, such as the weights of a small network (GENE-07, plan M16 §4 C1); the values are copied.</summary>
        /// <param name="values">The values.</param>
        /// <returns>A vector value that later changes to <paramref name="values"/> don't reach.</returns>
        public static AlleleValue OfVector(ReadOnlySpan<float> values) => new AlleleValue(values.ToArray());

        /// <summary>The values of a vector, read-only; empty for a sentence or a number.</summary>
        public ReadOnlySpan<float> Vector => vector;

        /// <summary>The number of values of a vector; 0 for a sentence or a number.</summary>
        public int VectorLength => vector != null ? vector.Length : 0;

        /// <summary>
        /// The value as compared by the registry (GENE-10): collapsed spaces, the number at a precision, or a vector's length
        /// and hash at a precision (GENE-07).
        /// </summary>
        /// <param name="decimals">For a number or a vector, the decimals kept (the gene's <see cref="Gene.Decimals"/>;
        /// reference 4); ignored for a sentence.</param>
        /// <returns>The sentence; the rounded number written with exactly <paramref name="decimals"/> decimals, e.g.
        /// <c>60.0000</c>; or for a vector <c>vec[1229]#</c> followed by <see cref="VectorSha"/> at
        /// <paramref name="decimals"/>.</returns>
        public string Canonical(int decimals = 4)
        {
            if (Kind == AlleleKind.Text) return Text;
            if (Kind == AlleleKind.Vector) return decimals == 4 && vectorCanonical != null ? vectorCanonical : VectorCanonical(vector, decimals);
            return Math.Round(Number, decimals).ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        /// <summary>The sentence, the number with at most 4 decimals, or a vector's short form <c>vec[1229]#ab12cd34</c>.</summary>
        /// <inheritdoc/>
        public override string ToString()
        {
            if (Kind == AlleleKind.Text) return Text;
            if (Kind == AlleleKind.Number) return Number.ToString("0.####", CultureInfo.InvariantCulture);
            string c = Canonical();
            return c.Substring(0, c.Length - 8);                                        // 8 of the 16 hex digits
        }

        /// <summary>Compares two values: same kind and same canonical form at 4 decimals.</summary>
        /// <param name="o">The other value.</param>
        /// <returns>True when they are the same value.</returns>
        public bool Equals(AlleleValue o) => Kind == o.Kind && Canonical() == o.Canonical();
        /// <summary>True when <paramref name="obj"/> is an <see cref="AlleleValue"/> equal to this one.</summary>
        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is AlleleValue o && Equals(o);
        /// <summary>A hash of the kind and the canonical form, consistent with <see cref="Equals(AlleleValue)"/>.</summary>
        /// <inheritdoc/>
        public override int GetHashCode() => (Canonical() ?? "").GetHashCode() ^ (int)Kind;

        /// <summary>The first 16 hex digits of the SHA-256 of a vector's values, each rounded to <paramref name="decimals"/>.</summary>
        /// <remarks>
        /// Each value is hashed as the 64-bit integer round(v · 10^decimals), little-endian, so the hash doesn't depend on how
        /// numbers are written as text. The recorder writes it as <c>vector_sha</c> (13, plan M16 §4 C2).
        /// </remarks>
        /// <param name="values">The values; null counts as empty.</param>
        /// <param name="decimals">The decimals kept.</param>
        /// <returns>16 lower-case hex digits.</returns>
        public static string VectorSha(ReadOnlySpan<float> values, int decimals)
        {
            var bytes = new byte[values.Length * 8];
            double scale = Math.Pow(10, decimals);
            for (int i = 0; i < values.Length; i++)
            {
                long q = (long)Math.Round(values[i] * scale);
                for (int b = 0; b < 8; b++) bytes[i * 8 + b] = (byte)(q >> (8 * b));
            }
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Hashing.ToHex(sha.ComputeHash(bytes), 16);
        }

        /// <summary>A vector's canonical form: <c>vec[n]#</c> and its <see cref="VectorSha"/>.</summary>
        static string VectorCanonical(float[] values, int decimals) =>
            "vec[" + (values != null ? values.Length : 0).ToString(CultureInfo.InvariantCulture) + "]#" + VectorSha(values, decimals);

        /// <summary>Collapses runs of whitespace into one space and trims (GENE-10).</summary>
        /// <param name="s">The text; must not be null.</param>
        /// <returns>The text with single spaces between words and none at either end.</returns>
        public static string CollapseSpaces(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                // A space is written only before the next word: leading and trailing whitespace disappear.
                if (char.IsWhiteSpace(c)) { space = sb.Length > 0; continue; }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
