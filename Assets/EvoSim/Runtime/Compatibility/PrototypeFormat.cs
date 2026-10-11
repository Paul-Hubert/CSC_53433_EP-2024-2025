namespace EvoSim
{
    /// <summary>
    /// The Python prototype's file conventions, for the recorder's compatibility mode (OUT-05): its two species were
    /// "the agents" and the predators, it logged kills as "predator" and prefixed predator statistics with "pred_".
    /// </summary>
    /// <remarks>Used by <see cref="RunRecorder"/> when <see cref="RunRecorder.Compatibility"/> is on.</remarks>
    public static class PrototypeFormat
    {
        /// <summary>The death cause the prototype wrote for a kill.</summary>
        public const string KillCause = "predator";
        /// <summary>The statistics prefix of the second species.</summary>
        public const string SecondSpeciesPrefix = "pred_";
        /// <summary>The summary key of the second species: its id with an "s" (gene_report reads summary[species + "s"]).</summary>
        /// <param name="speciesId">The species id, e.g. <c>"predator"</c>.</param>
        /// <returns>The key, e.g. <c>"predators"</c>.</returns>
        public static string SummaryKey(string speciesId) => speciesId + "s";
    }
}
