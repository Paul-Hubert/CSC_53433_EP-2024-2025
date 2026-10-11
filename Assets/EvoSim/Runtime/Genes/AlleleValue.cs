using System;
using System.Globalization;
using System.Text;

namespace EvoSim
{
    /// <summary>One allele value: a sentence or a number.</summary>
    /// <remarks>
    /// Make one with <see cref="OfText"/> or <see cref="OfNumber"/>. A sentence has its spaces collapsed at once, so
    /// "Eat  when hungry. " and "Eat when hungry." are the same value. Two values are equal when they have the same kind and
    /// the same <see cref="Canonical"/> form at 4 decimals; the registry compares numbers at the gene's own precision.
    /// </remarks>
    public readonly struct AlleleValue : IEquatable<AlleleValue>
    {
        /// <summary>Whether it holds a sentence or a number.</summary>
        public readonly AlleleKind Kind;
        /// <summary>The sentence, spaces collapsed; null for a number.</summary>
        public readonly string Text;
        /// <summary>The number; 0 for a sentence.</summary>
        public readonly float Number;

        /// <summary>Builds a value; <see cref="OfText"/> and <see cref="OfNumber"/> call it.</summary>
        AlleleValue(AlleleKind kind, string text, float number) { Kind = kind; Text = text; Number = number; }

        /// <summary>A sentence value, its runs of whitespace collapsed into one space and trimmed (GENE-10).</summary>
        /// <param name="text">The sentence; null counts as empty.</param>
        /// <returns>A text value.</returns>
        public static AlleleValue OfText(string text) => new AlleleValue(AlleleKind.Text, CollapseSpaces(text ?? ""), 0f);
        /// <summary>A number value.</summary>
        /// <param name="number">The number, in the unit of the trait the gene sets.</param>
        /// <returns>A number value.</returns>
        public static AlleleValue OfNumber(float number) => new AlleleValue(AlleleKind.Number, null, number);

        /// <summary>The value as compared by the registry (GENE-10): collapsed spaces, or the number at a precision.</summary>
        /// <param name="decimals">For a number, the decimals kept (the gene's <see cref="Gene.Decimals"/>; reference 4);
        /// ignored for a sentence.</param>
        /// <returns>The sentence, or the rounded number written with exactly <paramref name="decimals"/> decimals, e.g.
        /// <c>60.0000</c>.</returns>
        public string Canonical(int decimals = 4) =>
            Kind == AlleleKind.Text ? Text : Math.Round(Number, decimals).ToString("F" + decimals, CultureInfo.InvariantCulture);

        /// <summary>The sentence, or the number with at most 4 decimals.</summary>
        /// <inheritdoc/>
        public override string ToString() => Kind == AlleleKind.Text ? Text : Number.ToString("0.####", CultureInfo.InvariantCulture);

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
