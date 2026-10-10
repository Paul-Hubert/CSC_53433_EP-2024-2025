namespace EvoSim
{
    /// <summary>An observation read through its species' senses: the token of a sense by label (CTRL-10).</summary>
    public sealed class ObservationView
    {
        public Species Species { get; }
        public Observation Observation { get; }

        public ObservationView(Species species, Observation observation)
        {
            Species = species;
            Observation = observation;
        }

        /// <summary>The token of the first sense with this label, or null if the species has no such sense.</summary>
        public string Token(string senseLabel)
        {
            var senses = Species.Senses;
            for (int i = 0; i < senses.Count; i++)
                if (senses[i].Label == senseLabel) return senses[i].Tokens[Observation[i]];
            return null;
        }
    }
}
