namespace EvoSim.Neuro
{
    /// <summary>How a <see cref="NeuralBrain"/> turns the network's outputs into a row of probabilities (plan M16 D1, §5.6).</summary>
    public enum NeuralPolicy
    {
        /// <summary>A one-hot on the largest output, ties to the lower index: the draw always picks it (owner's default).</summary>
        Greedy,
        /// <summary>A softmax of the outputs at the brain's own temperature; the World's τ applies on top (DEC-20, V-77).</summary>
        Sample,
    }
}
