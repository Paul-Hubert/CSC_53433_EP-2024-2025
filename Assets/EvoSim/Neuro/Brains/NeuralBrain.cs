using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// A brain that runs each animal's own network (plan M16 D1, D4, §5.6): the weights come from the animal's
    /// <see cref="NeuralGenomeGene"/> allele, the inputs from its senses, and the row from the outputs, a one-hot on the
    /// largest by default.
    /// </summary>
    /// <remarks>
    /// <para>Inputs follow <see cref="NeuralLayout"/>: an <see cref="INeuralSense"/>'s attachment (found by
    /// <see cref="NeuralInput.SenseIndex"/>) is copied, any other sense gives a one-hot of its token. The weights are read
    /// from <see cref="DecisionQuery.Genome"/> (DEC-10, C3); the gene's text is never read (<see cref="ReadsGeneText"/>, C5).</para>
    /// <para>It is pure arithmetic and draws no random number, so the same query gives the same row (DEC-12). Its queries
    /// are never memoised or cached: no two animals share weights (SENSE-41). No genome, a vector of the wrong length, a
    /// missing attachment or a NaN output fails the query; it never makes a row up (DEC-40).</para>
    /// <para>This is the synchronous form (M16.4): it answers at once. The worker-task form of D4 comes with M16.8.</para>
    /// </remarks>
    [RequiresModule(typeof(NeuralGenomeGene))]
    public class NeuralBrain : Brain
    {
        [SerializeField, Tooltip("Greedy: a one-hot on the largest output (default). Sample: a softmax at the temperature below.")]
        NeuralPolicy policy = NeuralPolicy.Greedy;
        [SerializeField, Min(0.01f), Tooltip("The softmax temperature of the Sample policy (reference 1).")]
        float temperature = 1f;

        /// <summary>One species' network, inputs and outputs, rebuilt when its layout changes.</summary>
        sealed class Net
        {
            public NeuralGenomeGene Gene;
            public NeuralLayout Layout;
            public NeuralNet Network;
            public float[] Input, Output;
        }

        readonly Dictionary<Species, Net> nets = new Dictionary<Species, Net>();

        /// <summary>The policy.</summary>
        public NeuralPolicy Policy => policy;
        /// <summary>The Sample policy's temperature.</summary>
        public float Temperature => temperature;

        /// <inheritdoc/>
        public override string Id => policy == NeuralPolicy.Greedy ? "neural-greedy" : $"neural-sample-{temperature:0.##}";
        /// <inheritdoc/>
        public override bool AcceptsAttachments => true;
        /// <inheritdoc/>
        public override bool CanMixSpecies => true;
        /// <inheritdoc/>
        public override bool ReadsGeneText => false;
        /// <inheritdoc/>
        public override bool Memoizable => false;

        /// <summary>Sets the policy from code (templates, tests), as the Inspector does.</summary>
        /// <param name="newPolicy">Greedy or Sample.</param>
        /// <param name="tau">The Sample policy's temperature.</param>
        /// <returns>This brain.</returns>
        public NeuralBrain Configure(NeuralPolicy newPolicy, float tau = 1f)
        {
            policy = newPolicy;
            temperature = Mathf.Max(0.01f, tau);
            return this;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            useCache = false;
            nets.Clear();
        }

        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            foreach (var s in World.AllSpecies)
            {
                if (s.Brain != this || s.Module<NeuralGenomeGene>() != null) continue;
                var species = s;
                report.Error("V-70", s, $"'{s.DisplayName}' uses {Id} but has no NeuralGenomeGene: its animals have no weights.",
                             new ValidationFix("Add a NeuralGenomeGene", () => species.gameObject.AddComponent<NeuralGenomeGene>()));
            }
            if (policy == NeuralPolicy.Sample && Mathf.Abs(World.SamplingTemperature - 1f) > 1e-6f)
                report.Info("V-77", this, $"{Id} samples, and the World's sampling τ {World.SamplingTemperature} tempers its rows too (DEC-20).");
        }

        /// <inheritdoc/>
        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var answer = BrainAnswer.Later(batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                string error = Answer(batch[i], out var row);
                if (error == null) answer.SetRow(i, row);
                else answer.Fail(i, error);
            }
            answer.Complete();
            return answer;
        }

        /// <summary>Runs one query's network: the row, or the reason it can't (DEC-40).</summary>
        /// <param name="q">The query.</param>
        /// <param name="row">The row, or null on failure.</param>
        /// <returns>Null, or the error.</returns>
        public string Answer(DecisionQuery q, out float[] row)
        {
            row = null;
            var net = NetOf(q.Species);
            if (net == null) return $"'{q.Species.DisplayName}' has no NeuralGenomeGene (V-70)";
            if (q.Genome == null) return "the query carries no genome: a network needs its weights (DEC-10)";
            var weights = q.Genome[net.Gene.Locus].Value;
            if (weights.Kind != AlleleKind.Vector || weights.VectorLength != net.Layout.VectorLength)
                return $"the weights hold {weights.VectorLength} values; the layout needs {net.Layout.VectorLength} (V-71)";
            string error = Encode(q, net);
            if (error != null) return error;
            net.Network.Forward(weights.Vector, net.Input, net.Output);
            for (int k = 0; k < net.Output.Length; k++)
                if (float.IsNaN(net.Output[k]) || float.IsInfinity(net.Output[k])) return $"output {k} is not a number";
            row = ToRow(net.Output, policy, temperature);
            return null;
        }

        /// <summary>Turns the outputs into a row: a one-hot on the largest (Greedy), or a softmax at τ (Sample).</summary>
        /// <param name="outputs">The network's outputs, one per action.</param>
        /// <param name="p">The policy.</param>
        /// <param name="tau">The softmax temperature.</param>
        /// <returns>A row that sums to 1.</returns>
        public static float[] ToRow(ReadOnlySpan<float> outputs, NeuralPolicy p, float tau)
        {
            var row = new float[outputs.Length];
            if (p == NeuralPolicy.Greedy)
            {
                row[NeuralNet.ArgMax(outputs)] = 1f;
                return row;
            }
            float max = outputs[0];
            for (int k = 1; k < outputs.Length; k++) max = Mathf.Max(max, outputs[k]);
            var w = new double[outputs.Length];
            for (int k = 0; k < outputs.Length; k++) w[k] = Math.Exp((outputs[k] - max) / tau);
            return Normalize(w);
        }

        /// <summary>Writes the query's inputs in layout order: attachments copied, other senses one-hot.</summary>
        static string Encode(DecisionQuery q, Net net)
        {
            var input = net.Input;
            Array.Clear(input, 0, input.Length);
            foreach (var slot in net.Layout.Slots)
            {
                if (slot.SenseIndex < 0) { input[slot.Offset] = 1f; continue; }          // no sense: one constant input
                if (!slot.Attached)
                {
                    int token = q.Observation != null && slot.SenseIndex < q.Observation.Count ? q.Observation[slot.SenseIndex] : -1;
                    if (token < 0 || token >= slot.Width) return $"sense {slot.SenseIndex}: token {token} outside its {slot.Width} inputs";
                    input[slot.Offset + token] = 1f;
                    continue;
                }
                NeuralInput found = null;
                if (q.Attachments != null)
                    foreach (var att in q.Attachments)
                        if (att is NeuralInput n && n.SenseIndex == slot.SenseIndex) { found = n; break; }
                if (found == null) return $"sense {slot.SenseIndex} gave no attachment";
                if (found.Values.Length != slot.Width) return $"sense {slot.SenseIndex} gave {found.Values.Length} values; the layout has {slot.Width} (V-72)";
                Array.Copy(found.Values, 0, input, slot.Offset, slot.Width);
            }
            return null;
        }

        /// <summary>The species' network, built on first use and again when the gene's layout changed.</summary>
        Net NetOf(Species s)
        {
            if (nets.TryGetValue(s, out var net) && net.Layout == net.Gene.Layout) return net;
            var gene = s.Module<NeuralGenomeGene>();
            if (gene == null) return null;
            var layout = gene.Layout;
            net = new Net
            {
                Gene = gene,
                Layout = layout,
                Network = new NeuralNet(layout, gene.Activation),
                Input = new float[layout.Inputs],
                Output = new float[layout.Outputs],
            };
            nets[s] = net;
            return net;
        }
    }
}
