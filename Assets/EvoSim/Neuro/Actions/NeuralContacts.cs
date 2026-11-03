using System;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The contact interactions a <see cref="NeuralAction"/> may plan after its move (plan M16 D5, §5.5): at most one a tick,
    /// strike before scavenge before graze (ACT-03). The species' diet must allow each one (V-75).
    /// </summary>
    [Flags]
    public enum NeuralContacts
    {
        /// <summary>No contact: the action only moves.</summary>
        None = 0,
        /// <summary>Eat an item of a layer the diet grazes, within the graze reach.</summary>
        Graze = 1,
        /// <summary>Strike an animal of a species the diet strikes, within the strike reach.</summary>
        Strike = 2,
        /// <summary>Eat from a carcass the diet scavenges, within the scavenge reach.</summary>
        Scavenge = 4,
    }
}
