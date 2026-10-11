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
    /// <remarks>
    /// Scenes and run files are shared, so a key must never sit in a component's field: put it in an environment
    /// variable and write the variable's name in a field whose name ends in <c>Variable</c> (e.g. <c>apiKeyVariable</c>).
    /// </remarks>
    public static class Secrets
    {
        // Prefixes of common keys (OpenAI, Hugging Face, Groq, GitHub, Slack, Google), auth headers and URL parameters.
        static readonly Regex KeyLike = new Regex(
            @"(\bsk-[A-Za-z0-9]|\bsk_[A-Za-z0-9]|\bhf_[A-Za-z0-9]|\bgsk_[A-Za-z0-9]|\bgh[pousr]_[A-Za-z0-9]|github_pat_|\bxox[abprs]-|\bAIza[0-9A-Za-z_-]{8}|" +
            @"\bBearer\s+\S|\bBasic\s+[A-Za-z0-9+/=]{8}|[?&;](api[_-]?key|key|token|access_token|auth|secret|password)=)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        /// <summary>A user name and password inside a URL: <c>scheme://user:password@host</c>.</summary>
        static readonly Regex Credentials = new Regex(@"://[^/@\s:]+:[^/@\s]+@", RegexOptions.CultureInvariant);
        /// <summary>A valid environment variable name: a letter or underscore, then letters, digits or underscores.</summary>
        static readonly Regex VariableName = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

        /// <summary>True for text that carries a key, a token or a password.</summary>
        /// <param name="value">The text to check, or null.</param>
        /// <returns>True when it holds a known key prefix, an auth header, a secret URL parameter or a URL with a
        /// password; false for null or empty.</returns>
        public static bool LooksLikeKey(string value) =>
            !string.IsNullOrEmpty(value) && (KeyLike.IsMatch(value) || Credentials.IsMatch(value));

        /// <summary>
        /// For a field that should hold an environment variable's name: true when it holds something else, a key-like
        /// value, or a long run of letters and digits with lower case that names no variable (a pasted hex or base62 key).
        /// </summary>
        /// <param name="value">The field's value, or null.</param>
        /// <returns>True when the value is not a plausible variable name; false for null or empty.</returns>
        public static bool NotAVariableName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (!VariableName.IsMatch(value) || LooksLikeKey(value)) return true;
            // A long name that is set in this environment is a real variable, whatever it looks like.
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
        /// <param name="report">Receives one V-61 error per bad field, with a fix that clears a string field.</param>
        /// <param name="component">The component to check, or null (nothing happens).</param>
        /// <remarks>The World runs it on its own components and every module; a module that checks itself (an HTTP
        /// brain) is not checked twice.</remarks>
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

        /// <summary>The V-61 message for one field: what it should hold instead.</summary>
        static string Message(MonoBehaviour c, string field, bool variable) => variable
            ? $"{c.GetType().Name}.{field} on '{c.gameObject.name}' holds something that isn't an environment variable name: put the key in an environment variable and write its name here (OUT-04)."
            : $"{c.GetType().Name}.{field} on '{c.gameObject.name}' carries a key, a token or a password: move it to an environment variable (OUT-04).";

        /// <summary>The command line with every key-like argument masked, and the value after an option that names one (OUT-04).</summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The arguments joined by spaces, each secret one replaced by <c>***</c>.</returns>
        /// <example>
        /// <code>
        /// Secrets.Mask(new[] { "-apiKey", "abc123", "-seed", "7" })   // "-apiKey *** -seed 7"
        /// </code>
        /// </example>
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

        /// <summary>True when a command-line argument names a secret (key, token, password, cookie…).</summary>
        static bool NamesSecret(string arg)
        {
            string a = arg.ToLowerInvariant();
            foreach (var word in new[] { "key", "token", "secret", "password", "passwd", "auth", "bearer", "credential", "serial", "cookie" })
                if (a.Contains(word)) return true;
            return false;
        }
    }
}
