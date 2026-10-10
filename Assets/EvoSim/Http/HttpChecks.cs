using UnityEngine;

namespace EvoSim
{
    /// <summary>Checks shared by HTTP brains and mutator clients (the key checks, V-61, are Secrets.CheckFields).</summary>
    public static class HttpChecks
    {
        /// <summary>V-60 on demand: a connection test's result (null = reachable) as a warning naming the brain or client.</summary>
        public static void Reachability(ValidationReport report, Object owner, string error)
        {
            if (error != null) report.Warning("V-60", owner, $"The server can't be reached: {error}");
        }

        /// <summary>V-20: the address must be an http(s) URL.</summary>
        public static void Host(ValidationReport report, Object owner, string host)
        {
            if (string.IsNullOrEmpty(host) || !(host.StartsWith("http://") || host.StartsWith("https://")))
                report.Error("V-20", owner, $"'{host}' isn't a server address (http://host:port).");
        }
    }
}
