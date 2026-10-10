namespace EvoSim
{
    /// <summary>
    /// The Python prototype's file conventions, for the recorder's compatibility mode (OUT-05): its two species were
    /// "the agents" and the predators, it logged kills as "predator" and prefixed predator statistics with "pred_".
    /// </summary>
    public static class PrototypeFormat
    {
        /// <summary>The death cause the prototype wrote for a kill.</summary>
        public const string KillCause = "predator";
        /// <summary>The statistics prefix of the second species.</summary>
        public const string SecondSpeciesPrefix = "pred_";
        /// <summary>The summary key of the second species: its id with an "s" (gene_report reads summary[species + "s"]).</summary>
        public static string SummaryKey(string speciesId) => speciesId + "s";
    }
}
