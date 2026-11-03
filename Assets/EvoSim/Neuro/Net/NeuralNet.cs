using System;

namespace EvoSim.Neuro
{
    /// <summary>
    /// A forward pass over a <see cref="NeuralLayout"/> (plan M16 §5.6): pure arithmetic on the given spans, no random draw,
    /// no allocation after construction. It is the test oracle for any faster path.
    /// </summary>
    /// <remarks>
    /// One instance holds the scratch of one pass, so it is not shared between threads: each worker makes its own. Hidden
    /// layers use the activation; the output layer is linear (logits).
    /// </remarks>
    public sealed class NeuralNet
    {
        readonly NeuralLayout layout;
        readonly NeuralActivation activation;
        readonly float[] a, b;                                     // the current level's values, and the next one's

        /// <summary>The layout it computes.</summary>
        public NeuralLayout Layout => layout;

        /// <summary>Makes a net for a layout.</summary>
        /// <param name="layout">The layout.</param>
        /// <param name="activation">The hidden layers' activation.</param>
        public NeuralNet(NeuralLayout layout, NeuralActivation activation = NeuralActivation.Tanh)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.activation = activation;
            a = new float[layout.MaxWidth];
            b = new float[layout.MaxWidth];
        }

        /// <summary>Computes the outputs (logits) of a network.</summary>
        /// <param name="weights">The weights, <see cref="NeuralLayout.VectorLength"/> of them, in layout order.</param>
        /// <param name="input">The inputs, <see cref="NeuralLayout.Inputs"/> of them.</param>
        /// <param name="output">Receives the <see cref="NeuralLayout.Outputs"/> logits.</param>
        /// <exception cref="ArgumentException">A span has the wrong length.</exception>
        public void Forward(ReadOnlySpan<float> weights, ReadOnlySpan<float> input, Span<float> output)
        {
            if (weights.Length != layout.VectorLength) throw new ArgumentException($"{weights.Length} weights for a layout of {layout.VectorLength}.");
            if (input.Length != layout.Inputs) throw new ArgumentException($"{input.Length} inputs for a layout of {layout.Inputs}.");
            if (output.Length != layout.Outputs) throw new ArgumentException($"{output.Length} outputs for a layout of {layout.Outputs}.");
            var sizes = layout.Sizes;
            input.CopyTo(a);
            float[] current = a, next = b;
            int last = layout.LayerCount - 1;
            for (int l = 0; l <= last; l++)
            {
                int fanIn = sizes[l], fanOut = sizes[l + 1];
                for (int j = 0; j < fanOut; j++)
                {
                    int w = layout.NeuronOffset(l, j);
                    float sum = weights[w];                                          // bias
                    for (int i = 0; i < fanIn; i++) sum += weights[w + 1 + i] * current[i];
                    next[j] = l == last ? sum : Activate(sum);
                }
                var swap = current; current = next; next = swap;
            }
            for (int j = 0; j < output.Length; j++) output[j] = current[j];
        }

        /// <summary>The hidden layers' activation of one sum.</summary>
        float Activate(float x)
        {
            switch (activation)
            {
                case NeuralActivation.Sigmoid: return 1f / (1f + (float)Math.Exp(-x));
                case NeuralActivation.Relu: return x > 0f ? x : 0f;
                default: return (float)Math.Tanh(x);
            }
        }

        /// <summary>The index of the largest value, ties to the lower index (the Greedy policy, plan M16 §5.6).</summary>
        /// <param name="values">The logits; at least one.</param>
        /// <returns>The index; -1 for an empty span.</returns>
        public static int ArgMax(ReadOnlySpan<float> values)
        {
            int best = -1;
            for (int i = 0; i < values.Length; i++)
                if (best < 0 || values[i] > values[best]) best = i;
            return best;
        }
    }
}
