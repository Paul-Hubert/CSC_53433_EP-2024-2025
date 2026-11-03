namespace EvoSim.Neuro
{
    /// <summary>The activation of the hidden layers (plan M16 §5.1); the output layer is linear (logits).</summary>
    public enum NeuralActivation
    {
        /// <summary>tanh (reference).</summary>
        Tanh,
        /// <summary>1 / (1 + e^−x), as the previous lab's net.</summary>
        Sigmoid,
        /// <summary>max(0, x).</summary>
        Relu
    }
}
