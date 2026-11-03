namespace EvoSim.Neuro
{
    /// <summary>How founder networks draw their weights (plan M16 §5.1).</summary>
    public enum NeuralInit
    {
        /// <summary>
        /// Xavier (reference): each weight U(−a, a) with a = √(6 / (fan-in + fan-out)) of its layer; biases 0.
        /// </summary>
        Xavier,
        /// <summary>The previous lab's: every weight and bias U(−10, 10) (saturated, bang-bang steering).</summary>
        OldLab
    }
}
