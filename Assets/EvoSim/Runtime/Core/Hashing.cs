using System.Security.Cryptography;
using System.Text;

namespace EvoSim
{
    /// <summary>SHA-256 helpers for keys, signatures and prompt ids. Pure functions, no state.</summary>
    public static class Hashing
    {
        /// <summary>The first <paramref name="hexDigits"/> hex digits of the SHA-256 of a UTF-8 text.</summary>
        public static string Sha256Hex(string text, int hexDigits = 16)
        {
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")), hexDigits);
        }

        public static string ToHex(byte[] bytes, int hexDigits = 64)
        {
            int n = System.Math.Min(bytes.Length, (hexDigits + 1) / 2);
            var sb = new StringBuilder(n * 2);
            for (int i = 0; i < n; i++) sb.Append(bytes[i].ToString("x2"));
            return sb.ToString(0, System.Math.Min(sb.Length, hexDigits));
        }
    }
}
