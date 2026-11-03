using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The shape of a species' network (plan M16 §5.1): its inputs in sense order, its hidden layers, one output per action,
    /// and where each neuron's weights sit in the weights vector.
    /// </summary>
    /// <remarks>
    /// <para>Inputs, in sense order: an <see cref="INeuralSense"/> contributes its <see cref="INeuralSense.NeuralWidth"/>;
    /// any other sense a one-hot of its tokens. Outputs: one logit per action, in action order.</para>
    /// <para>Weights, layer after layer, neuron after neuron: each neuron's slice is its bias then one weight per input of
    /// its layer, so a neuron's slice is contiguous (<see cref="NeuronOffset"/>) and a crossover can take it whole (GENE-30).
    /// Length = Σ (fan-in + 1) × fan-out (D2).</para>
    /// </remarks>
    public sealed class NeuralLayout
    {
        /// <summary>Where one sense's inputs sit.</summary>
        public readonly struct Slot
        {
            /// <summary>The sense's index in the species' sense order.</summary>
            public readonly int SenseIndex;
            /// <summary>The first input of the sense.</summary>
            public readonly int Offset;
            /// <summary>The number of inputs: the attachment width, or the token count for a one-hot.</summary>
            public readonly int Width;
            /// <summary>True when the sense attaches values (<see cref="INeuralSense"/>); false for a one-hot of its token.</summary>
            public readonly bool Attached;

            /// <summary>Makes a slot.</summary>
            public Slot(int senseIndex, int offset, int width, bool attached) { SenseIndex = senseIndex; Offset = offset; Width = width; Attached = attached; }
        }

        readonly int[] sizes;                 // inputs, hidden…, outputs
        readonly int[] layerOffsets;          // where each layer's weights start
        readonly Slot[] slots;

        /// <summary>The number of inputs.</summary>
        public int Inputs => sizes[0];
        /// <summary>The number of outputs: the species' actions.</summary>
        public int Outputs => sizes[sizes.Length - 1];
        /// <summary>The number of weight layers (hidden layers + 1).</summary>
        public int LayerCount => sizes.Length - 1;
        /// <summary>The neurons of each level: inputs, then each hidden layer, then the outputs.</summary>
        public IReadOnlyList<int> Sizes => sizes;
        /// <summary>One slot per sense, in sense order.</summary>
        public IReadOnlyList<Slot> Slots => slots;
        /// <summary>The length of the weights vector.</summary>
        public int VectorLength { get; }
        /// <summary>The widest level: the scratch a forward pass needs per level.</summary>
        public int MaxWidth { get; }
        /// <summary>A hash (16 hex) of the sizes and the slots: two layouts with the same hash read the same vector the same way.</summary>
        public string Hash { get; }

        /// <summary>A layout from explicit sizes, with no senses (tests, tools).</summary>
        /// <param name="inputs">The number of inputs, ≥ 1.</param>
        /// <param name="hidden">The hidden layers' sizes, each ≥ 1; null or empty for none.</param>
        /// <param name="outputs">The number of outputs, ≥ 1.</param>
        public NeuralLayout(int inputs, IReadOnlyList<int> hidden, int outputs)
            : this(new[] { new Slot(-1, 0, inputs, true) }, hidden, outputs) { }

        /// <summary>A layout from sense slots.</summary>
        NeuralLayout(Slot[] slots, IReadOnlyList<int> hidden, int outputs)
        {
            this.slots = slots;
            int inputs = 0;
            foreach (var s in slots) inputs += s.Width;
            int h = hidden != null ? hidden.Count : 0;
            sizes = new int[h + 2];
            sizes[0] = inputs;
            for (int i = 0; i < h; i++) sizes[i + 1] = hidden[i];
            sizes[h + 1] = outputs;
            foreach (int n in sizes)
                if (n < 1) throw new ArgumentException($"Every level of a network needs at least one neuron: {string.Join(" → ", sizes)}.");
            layerOffsets = new int[sizes.Length - 1];
            int length = 0, widest = 0;
            for (int l = 0; l < sizes.Length - 1; l++)
            {
                layerOffsets[l] = length;
                length += (sizes[l] + 1) * sizes[l + 1];
            }
            foreach (int n in sizes) widest = Math.Max(widest, n);
            VectorLength = length;
            MaxWidth = widest;
            var sb = new StringBuilder(string.Join(",", sizes));
            foreach (var s in slots) sb.Append('|').Append(s.SenseIndex).Append(':').Append(s.Width).Append(s.Attached ? 'v' : 't');
            Hash = Hashing.Sha256Hex(sb.ToString());
        }

        /// <summary>The layout of a species' network (plan M16 §5.1): its senses as inputs, its actions as outputs.</summary>
        /// <param name="s">The species, its senses and actions discovered.</param>
        /// <param name="hidden">The hidden layers' sizes; null or empty for none.</param>
        /// <returns>The layout.</returns>
        public static NeuralLayout For(Species s, IReadOnlyList<int> hidden)
        {
            var slots = new Slot[s.Senses.Count];
            int offset = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                var sense = s.Senses[i];
                bool attached = sense is INeuralSense;
                int width = attached ? ((INeuralSense)sense).NeuralWidth : sense.Tokens.Count;
                slots[i] = new Slot(i, offset, width, attached);
                offset += width;
            }
            if (offset == 0) slots = new[] { new Slot(-1, 0, 1, true) };       // no sense: one constant input, so a layout always exists
            return new NeuralLayout(slots, hidden, Math.Max(1, s.Actions.Count));
        }

        /// <summary>The weights vector's length for these sizes: Σ (fan-in + 1) × fan-out (D2).</summary>
        /// <param name="inputs">The number of inputs.</param>
        /// <param name="hidden">The hidden layers' sizes; null or empty for none.</param>
        /// <param name="outputs">The number of outputs.</param>
        /// <returns>The length.</returns>
        public static int LengthOf(int inputs, IReadOnlyList<int> hidden, int outputs)
        {
            int length = 0, previous = inputs;
            if (hidden != null) foreach (int n in hidden) { length += (previous + 1) * n; previous = n; }
            return length + (previous + 1) * outputs;
        }

        /// <summary>Where neuron <paramref name="neuron"/> of weight layer <paramref name="layer"/> starts: its bias, then its weights.</summary>
        /// <param name="layer">The weight layer, 0 = inputs → first hidden layer.</param>
        /// <param name="neuron">The neuron of that layer's output level.</param>
        /// <returns>The index of its bias in the weights vector.</returns>
        public int NeuronOffset(int layer, int neuron) => layerOffsets[layer] + neuron * (sizes[layer] + 1);

        /// <summary>The length of one neuron's slice in weight layer <paramref name="layer"/>: its bias and one weight per input.</summary>
        /// <param name="layer">The weight layer.</param>
        /// <returns>fan-in + 1.</returns>
        public int SliceLength(int layer) => sizes[layer] + 1;

        /// <summary>The weight layer an index of the weights vector belongs to.</summary>
        /// <param name="index">An index in [0, <see cref="VectorLength"/>).</param>
        /// <returns>The weight layer, 0 = inputs → first hidden layer.</returns>
        public int LayerOf(int index)
        {
            int l = layerOffsets.Length - 1;
            while (l > 0 && index < layerOffsets[l]) l--;
            return l;
        }

        /// <summary>The sizes, e.g. <c>96 → 12 → 5 (1229 weights)</c>.</summary>
        /// <inheritdoc/>
        public override string ToString() =>
            string.Join(" → ", sizes) + " (" + VectorLength.ToString(CultureInfo.InvariantCulture) + " weights)";
    }
}
