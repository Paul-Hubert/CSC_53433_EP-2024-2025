using System;
using System.Globalization;
using System.Text;

namespace EvoSim
{
    /// <summary>One allele value: a sentence or a number.</summary>
    public readonly struct AlleleValue : IEquatable<AlleleValue>
    {
        public readonly AlleleKind Kind;
        public readonly string Text;
        public readonly float Number;

        AlleleValue(AlleleKind kind, string text, float number) { Kind = kind; Text = text; Number = number; }

        public static AlleleValue OfText(string text) => new AlleleValue(AlleleKind.Text, CollapseSpaces(text ?? ""), 0f);
        public static AlleleValue OfNumber(float number) => new AlleleValue(AlleleKind.Number, null, number);

        /// <summary>The value as compared by the registry (GENE-10): collapsed spaces, or the number at a precision.</summary>
        public string Canonical(int decimals = 4) =>
            Kind == AlleleKind.Text ? Text : Math.Round(Number, decimals).ToString("F" + decimals, CultureInfo.InvariantCulture);

        public override string ToString() => Kind == AlleleKind.Text ? Text : Number.ToString("0.####", CultureInfo.InvariantCulture);

        public bool Equals(AlleleValue o) => Kind == o.Kind && Canonical() == o.Canonical();
        public override bool Equals(object obj) => obj is AlleleValue o && Equals(o);
        public override int GetHashCode() => (Canonical() ?? "").GetHashCode() ^ (int)Kind;

        /// <summary>Collapses runs of whitespace into one space and trims (GENE-10).</summary>
        public static string CollapseSpaces(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) { space = sb.Length > 0; continue; }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
