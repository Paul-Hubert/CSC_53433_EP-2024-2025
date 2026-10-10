using System.Text.RegularExpressions;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Checks shared by HTTP brains and mutator clients: no key in a serialized field (V-61, OUT-04).</summary>
    public static class HttpChecks
    {
        static readonly Regex VariableName = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");
        static readonly Regex KeyLike = new Regex("(sk-|hf_|Bearer\\s|api[_-]?key=|token=)", RegexOptions.IgnoreCase);

        /// <summary>V-61: the key variable must be a variable name, and the address must not carry credentials.</summary>
        public static void Secrets(ValidationReport report, Object owner, string host, string apiKeyVariable)
        {
            if (!string.IsNullOrEmpty(apiKeyVariable) && (!VariableName.IsMatch(apiKeyVariable) || KeyLike.IsMatch(apiKeyVariable)))
                report.Error("V-61", owner, "The API key field holds something that isn't an environment variable name: put the key in an " +
                                            "environment variable and write its name here (OUT-04).");
            if (!string.IsNullOrEmpty(host) && (KeyLike.IsMatch(host) || Regex.IsMatch(host, "://[^/@]+:[^/@]+@")))
                report.Error("V-61", owner, "The server address carries a key or a password: move it to an environment variable (OUT-04).");
        }

        /// <summary>V-20: the address must be an http(s) URL.</summary>
        public static void Host(ValidationReport report, Object owner, string host)
        {
            if (string.IsNullOrEmpty(host) || !(host.StartsWith("http://") || host.StartsWith("https://")))
                report.Error("V-20", owner, $"'{host}' isn't a server address (http://host:port).");
        }
    }
}
