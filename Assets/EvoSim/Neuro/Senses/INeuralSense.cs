namespace EvoSim.Neuro
{
    /// <summary>
    /// A sense that gives a network a vector of numbers (plan M16 §5.1, §5.4): besides its token, each reading attaches a
    /// <see cref="NeuralInput"/> of <see cref="NeuralWidth"/> values (SENSE-40). <see cref="NeuralLayout"/> gives it that
    /// many inputs; any other sense gets a one-hot of its tokens.
    /// </summary>
    public interface INeuralSense
    {
        /// <summary>The number of values each attachment holds: the sense's inputs in the network.</summary>
        int NeuralWidth { get; }
    }
}
