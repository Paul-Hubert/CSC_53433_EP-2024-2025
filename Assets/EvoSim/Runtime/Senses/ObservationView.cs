namespace EvoSim
{
    /// <summary>An observation read through its species' senses: the token of a sense by label (CTRL-10).</summary>
    /// <remarks>
    /// Directed tests hand one to <see cref="AnimalAction.IsRelevant"/>, so an action can say which situations concern it
    /// (eat needs food in sight, flee a threat…) without knowing the order of the species' senses.
    /// </remarks>
    /// <example>
    /// The sample <c>DrinkAction</c>: relevant when water is in sight.
    /// <code>
    /// public override bool IsRelevant(ObservationView o)
    /// {
    ///     var t = o.Token("Water");
    ///     return t != null &amp;&amp; t != "none";
    /// }
    /// </code>
    /// </example>
    public sealed class ObservationView
    {
        /// <summary>The species whose senses give the observation its meaning.</summary>
        public Species Species { get; }

        /// <summary>The observation read: one token index per sense of <see cref="Species"/>.</summary>
        public Observation Observation { get; }

        /// <summary>Wraps an observation of a species.</summary>
        /// <param name="species">The species whose senses produced the observation.</param>
        /// <param name="observation">An observation of that species, one token per sense.</param>
        public ObservationView(Species species, Observation observation)
        {
            Species = species;
            Observation = observation;
        }

        /// <summary>Gives the token of the first sense with this label.</summary>
        /// <param name="senseLabel">A sense label, exactly as <see cref="Sense.Label"/> gives it ("Food", "Predator"…);
        /// case counts.</param>
        /// <returns>The token as text ("none", "close, ready"…), or null if the species has no sense with this label.</returns>
        public string Token(string senseLabel)
        {
            var senses = Species.Senses;
            for (int i = 0; i < senses.Count; i++)
                if (senses[i].Label == senseLabel) return senses[i].Tokens[Observation[i]];
            return null;
        }
    }
}
