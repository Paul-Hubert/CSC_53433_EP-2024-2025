using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// <para>From <see cref="SyncBelow"/> queries on, <see cref="Ask"/> encodes the inputs on the main thread and runs the
    /// networks on a worker task (D4): the tick waits for it like any other brain (TICK-03), so the work hides inside JEV's
    /// round-trip. The worker has its own buffers and reads only copied inputs and the alleles' read-only vectors, so it gives
    /// the rows of the synchronous path bit for bit (DEC-12, SPACE-14).</para>
    /// </remarks>
    [RequiresModule(typeof(NeuralGenomeGene))]
    public class NeuralBrain : Brain
    {
        [SerializeField, Tooltip("Greedy: a one-hot on the largest output (default). Sample: a softmax at the temperature below.")]
        NeuralPolicy policy = NeuralPolicy.Greedy;
        [SerializeField, Min(0.01f), Tooltip("The softmax temperature of the Sample policy (reference 1).")]
        float temperature = 1f;
        [SerializeField, Min(0), Tooltip("Below this many queries in a batch it answers at once; from it on, on a worker task (plan D4, default 32).")]
        int syncBelow = 32;

        /// <summary>One species' network, inputs and outputs, rebuilt when its layout changes; the worker has its own.</summary>
        sealed class Net
        {
            public NeuralGenomeGene Gene;
            public NeuralLayout Layout;
            public NeuralNet Network, WorkerNetwork;
            public float[] Input, Output, WorkerOutput;
        }

        /// <summary>One query of a batch, ready for the worker: its network, weights and inputs, or why it can't run.</summary>
        struct Job
        {
            public Net Net;
            public AlleleValue Weights;
            public int Offset;                                          // where its inputs start in the batch's buffer
            public string Error;
        }

        readonly Dictionary<Species, Net> nets = new Dictionary<Species, Net>();
        Job[] jobs = new Job[0];                                        // the batch's jobs, reused (one batch in flight, TICK-03)
        float[] inputs = new float[0];                                  // the batch's inputs end to end, reused

        /// <summary>The batch size from which the networks run on a worker task.</summary>
        public int SyncBelow { get => syncBelow; set => syncBelow = Math.Max(0, value); }

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
                if (s.Brain != this) continue;
                var mating = s.Module<MatingRule>();
                if (mating != null && mating.Sexual && s.Module<NeuronCrossover>() == null)
                    report.Warning("V-73", s, $"'{s.DisplayName}' mates in pairs without a NeuronCrossover: each baby takes one parent's whole network.");
                var schedule = s.Module<DecisionSchedule>();
                int period = schedule != null ? schedule.Period(World) : World.DecisionPeriod;
                if (period > 1)
                    report.Info("V-74", s, $"'{s.DisplayName}' decides every {period} ticks: it steers late between decisions (expected in the matched-rate arm).");
                if (s.Module<NeuralGenomeGene>() != null) continue;
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
            int n = batch.Count;
            if (jobs.Length < n) jobs = new Job[Math.Max(n, jobs.Length * 2)];
            int size = 0;
            for (int i = 0; i < n; i++)
            {
                jobs[i] = default;
                jobs[i].Error = Prepare(batch[i], out jobs[i].Net, out jobs[i].Weights);
                jobs[i].Offset = size;
                if (jobs[i].Error == null) size += jobs[i].Net.Layout.Inputs;
            }
            if (inputs.Length < size) inputs = new float[Math.Max(size, inputs.Length * 2)];
            for (int i = 0; i < n; i++)                                     // the inputs are copied now, on the main thread (D4)
                if (jobs[i].Error == null)
                    jobs[i].Error = Encode(batch[i], jobs[i].Net.Layout, new Span<float>(inputs, jobs[i].Offset, jobs[i].Net.Layout.Inputs));
            if (n < syncBelow)
            {
                var now = BrainAnswer.Later(n);
                Run(n, now);
                return now;
            }
            // The answer waits on a task, not a spin: a blocked wait wakes when the worker ends (a spin falls back to
            // 15 ms sleeps on Windows, which cost more than the networks).
            var finished = new TaskCompletionSource<bool>();
            var answer = BrainAnswer.Later(n, Pending.Of(finished.Task));
            Task.Run(() =>
            {
                Run(n, answer);
                finished.SetResult(true);
            });
            return answer;
        }

        /// <summary>Runs a prepared batch's networks into the answer and completes it, whatever happens (the worker's body).</summary>
        void Run(int n, BrainAnswer answer)
        {
            try
            {
                for (int i = 0; i < n; i++)
                {
                    var job = jobs[i];
                    if (job.Error != null) { answer.Fail(i, job.Error); continue; }
                    var net = job.Net;
                    string error = Forward(net.WorkerNetwork, job.Weights, new ReadOnlySpan<float>(inputs, job.Offset, net.Layout.Inputs),
                                           net.WorkerOutput, out var row);
                    if (error == null) answer.SetRow(i, row);
                    else answer.Fail(i, error);
                }
            }
            catch (Exception e)
            {
                for (int i = 0; i < n; i++)
                    if (answer.Row(i) == null) answer.Fail(i, "the network failed: " + e.Message);
            }
            finally
            {
                answer.Complete();
            }
        }

        /// <summary>Runs one query's network: the row, or the reason it can't (DEC-40).</summary>
        /// <param name="q">The query.</param>
        /// <param name="row">The row, or null on failure.</param>
        /// <returns>Null, or the error.</returns>
        public string Answer(DecisionQuery q, out float[] row)
        {
            row = null;
            string error = Prepare(q, out var net, out var weights);
            if (error != null) return error;
            error = Encode(q, net.Layout, net.Input);
            if (error != null) return error;
            return Forward(net.Network, weights, net.Input, net.Output, out row);
        }

        /// <summary>The query's network and weights, or why it has none (main thread: it reads the species).</summary>
        string Prepare(DecisionQuery q, out Net net, out AlleleValue weights)
        {
            weights = default;
            net = NetOf(q.Species);
            if (net == null) return $"'{q.Species.DisplayName}' has no NeuralGenomeGene (V-70)";
            if (q.Genome == null) return "the query carries no genome: a network needs its weights (DEC-10)";
            weights = q.Genome[net.Gene.Locus].Value;
            if (weights.Kind != AlleleKind.Vector || weights.VectorLength != net.Layout.VectorLength)
                return $"the weights hold {weights.VectorLength} values; the layout needs {net.Layout.VectorLength} (V-71)";
            return null;
        }

        /// <summary>The forward pass and the policy: pure arithmetic on its arguments, so any thread may run it.</summary>
        string Forward(NeuralNet network, AlleleValue weights, ReadOnlySpan<float> input, float[] output, out float[] row)
        {
            row = null;
            network.Forward(weights.Vector, input, output);
            for (int k = 0; k < output.Length; k++)
                if (float.IsNaN(output[k]) || float.IsInfinity(output[k])) return $"output {k} is not a number";
            row = ToRow(output, policy, temperature);
            return null;
        }

        /// <summary>
        /// The query the ask-brains phase would build for this animal now: its observation, the senses' attachments and its
        /// genome. For tools (the Animal Inspector's neural panel); it only reads the world (SPACE-12).
        /// </summary>
        /// <param name="a">A living animal of a neural species.</param>
        /// <returns>The query, with the animal's id.</returns>
        public static DecisionQuery QueryOf(Animal a)
        {
            var s = a.Species;
            var ctx = new SenseContext(s.World);
            var o = s.Observe(a, ctx);
            var attachments = new List<object>();
            foreach (var sense in s.Senses)
                if (sense.HasAttachments && sense.Attachment(a, ctx) is object att) attachments.Add(att);
            return new DecisionQuery(s, new List<KeyValuePair<string, string>>(), o, s.Describe(o, TextStyle.V1), TextStyle.V1,
                                     a.Genome.BrainKey, attachments, a.Genome) { AnimalId = a.Id };
        }

        /// <summary>
        /// Runs one query's network and copies out its inputs and outputs (the Animal Inspector's panel): what
        /// <see cref="Answer"/> computes, without the policy.
        /// </summary>
        /// <param name="q">The query.</param>
        /// <param name="inputs">The network's inputs in layout order, or null on failure.</param>
        /// <param name="outputs">One output per action, or null on failure.</param>
        /// <returns>Null, or why the network can't run (as <see cref="Answer"/> says it).</returns>
        public string Explain(DecisionQuery q, out float[] inputs, out float[] outputs)
        {
            inputs = outputs = null;
            string error = Answer(q, out _);
            if (error != null) return error;
            var net = NetOf(q.Species);
            inputs = (float[])net.Input.Clone();
            outputs = (float[])net.Output.Clone();
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
        static string Encode(DecisionQuery q, NeuralLayout layout, Span<float> input)
        {
            input.Clear();
            foreach (var slot in layout.Slots)
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
                found.Values.AsSpan().CopyTo(input.Slice(slot.Offset, slot.Width));
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
                WorkerNetwork = new NeuralNet(layout, gene.Activation),
                Input = new float[layout.Inputs],
                Output = new float[layout.Outputs],
                WorkerOutput = new float[layout.Outputs],
            };
            nets[s] = net;
            return net;
        }
    }
}
