using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>SHA-256 helpers for keys, signatures and prompt ids. Pure functions, no state.</summary>
    public static class Hashing
    {
        /// <summary>The first <paramref name="hexDigits"/> hex digits of the SHA-256 of a UTF-8 text.</summary>
        /// <param name="text">The text to hash; null counts as empty.</param>
        /// <param name="hexDigits">How many hex digits to keep, at most 64.</param>
        /// <returns>Lower-case hex digits, e.g. a species signature or a prompt id.</returns>
        public static string Sha256Hex(string text, int hexDigits = 16)
        {
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")), hexDigits);
        }

        /// <summary>The first <paramref name="hexDigits"/> hex digits of some bytes, two per byte.</summary>
        /// <param name="bytes">The bytes, e.g. a hash.</param>
        /// <param name="hexDigits">How many hex digits to keep; at most two per byte.</param>
        /// <returns>Lower-case hex digits.</returns>
        public static string ToHex(byte[] bytes, int hexDigits = 64)
        {
            int n = System.Math.Min(bytes.Length, (hexDigits + 1) / 2);
            var sb = new StringBuilder(n * 2);
            for (int i = 0; i < n; i++) sb.Append(bytes[i].ToString("x2"));
            return sb.ToString(0, System.Math.Min(sb.Length, hexDigits));
        }
    }
}
