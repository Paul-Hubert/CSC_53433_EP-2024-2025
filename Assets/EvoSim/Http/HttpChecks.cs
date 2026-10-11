using UnityEngine;

namespace EvoSim
{
    /// <summary>Checks shared by HTTP brains and mutator clients (the key checks, V-61, are <see cref="Secrets.CheckFields"/>).</summary>
    public static class HttpChecks
    {
        /// <summary>
        /// Adds the V-60 warning when a connection test failed. V-60 runs on demand only (the Brain health window's Test
        /// connection), not at every validation: it needs the server.
        /// </summary>
        /// <param name="report">The report to add to.</param>
        /// <param name="owner">The brain or mutator client the warning names.</param>
        /// <param name="error">The result of a <c>TestConnection</c>: null when the server answered, else what went wrong.</param>
        public static void Reachability(ValidationReport report, Object owner, string error)
        {
            if (error != null) report.Warning("V-60", owner, $"The server can't be reached: {error}");
        }

        /// <summary>Adds the V-20 error when the address isn't an <c>http://</c> or <c>https://</c> URL.</summary>
        /// <param name="report">The report to add to.</param>
        /// <param name="owner">The brain or mutator client the error names.</param>
        /// <param name="host">The server's address to check.</param>
        public static void Host(ValidationReport report, Object owner, string host)
        {
            if (string.IsNullOrEmpty(host) || !(host.StartsWith("http://") || host.StartsWith("https://")))
                report.Error("V-20", owner, $"'{host}' isn't a server address (http://host:port).");
        }
    }
}
