using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a key, token or password looks like (OUT-04): one rule for validation (V-61, every serialized string of the
    /// World), the HTTP checks and the command line written to run_info.json.
    /// </summary>
    public static class Secrets
    {
        // Prefixes of common keys (OpenAI, Hugging Face, Groq, GitHub, Slack, Google), auth headers and URL parameters.
        static readonly Regex KeyLike = new Regex(
            @"(\bsk-[A-Za-z0-9]|\bsk_[A-Za-z0-9]|\bhf_[A-Za-z0-9]|\bgsk_[A-Za-z0-9]|\bgh[pousr]_[A-Za-z0-9]|github_pat_|\bxox[abprs]-|\bAIza[0-9A-Za-z_-]{8}|" +
            @"\bBearer\s+\S|\bBasic\s+[A-Za-z0-9+/=]{8}|[?&;](api[_-]?key|key|token|access_token|auth|secret|password)=)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex Credentials = new Regex(@"://[^/@\s:]+:[^/@\s]+@", RegexOptions.CultureInvariant);
        static readonly Regex VariableName = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

        /// <summary>True for text that carries a key, a token or a password.</summary>
        public static bool LooksLikeKey(string value) =>
            !string.IsNullOrEmpty(value) && (KeyLike.IsMatch(value) || Credentials.IsMatch(value));

        /// <summary>
        /// For a field that should hold an environment variable's name: true when it holds something else, a key-like
        /// value, or a long run of letters and digits with lower case that names no variable (a pasted hex or base62 key).
        /// </summary>
        public static bool NotAVariableName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (!VariableName.IsMatch(value) || LooksLikeKey(value)) return true;
            if (value.Length < 20 || Environment.GetEnvironmentVariable(value) != null) return false;
            bool digit = false, lower = false;
            foreach (char c in value)
            {
                if (char.IsDigit(c)) digit = true;
                else if (char.IsLower(c)) lower = true;
            }
            return digit && lower;
        }

        /// <summary>
        /// V-61: every serialized string of the component (lists included). A field whose name ends in "Variable" must
        /// hold an environment variable's name; any other field must not hold a key, a token or a password.
        /// </summary>
        public static void CheckFields(ValidationReport report, MonoBehaviour component)
        {
            if (component == null) return;
            for (var t = component.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!FieldPath.IsSerialized(f)) continue;
                    bool variable = f.Name.EndsWith("Variable", StringComparison.OrdinalIgnoreCase);
                    var value = f.GetValue(component);
                    if (value is string s)
                    {
                        if (variable ? NotAVariableName(s) : LooksLikeKey(s))
                        {
                            var field = f;
                            report.Error("V-61", component, Message(component, f.Name, variable),
                                         new ValidationFix("Clear it", () => field.SetValue(component, "")));
                        }
                    }
                    else if (value is IEnumerable list && !(value is UnityEngine.Object))
                        foreach (var item in list)
                            if (item is string text && LooksLikeKey(text)) { report.Error("V-61", component, Message(component, f.Name, false)); break; }
                }
        }

        static string Message(MonoBehaviour c, string field, bool variable) => variable
            ? $"{c.GetType().Name}.{field} on '{c.gameObject.name}' holds something that isn't an environment variable name: put the key in an environment variable and write its name here (OUT-04)."
            : $"{c.GetType().Name}.{field} on '{c.gameObject.name}' carries a key, a token or a password: move it to an environment variable (OUT-04).";

        /// <summary>The command line with every key-like argument masked, and the value after an option that names one (OUT-04).</summary>
        public static string Mask(string[] args)
        {
            var parts = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                bool afterSecretOption = i > 0 && NamesSecret(args[i - 1]) && !args[i - 1].Contains("=") && !args[i - 1].Contains(":");
                bool inline = NamesSecret(a) && (a.Contains("=") || a.Contains(":"));
                parts[i] = afterSecretOption || inline || LooksLikeKey(a) ? "***" : a;
            }
            return string.Join(" ", parts);
        }

        static bool NamesSecret(string arg)
        {
            string a = arg.ToLowerInvariant();
            foreach (var word in new[] { "key", "token", "secret", "password", "passwd", "auth", "bearer", "credential", "serial", "cookie" })
                if (a.Contains(word)) return true;
            return false;
        }
    }
}
