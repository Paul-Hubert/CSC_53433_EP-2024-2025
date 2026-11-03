using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The whole network of an animal as one vector allele (GENE-07, plan M16 D2, §5.1): one locus, one long
    /// <c>float[]</c>, whose length follows the species' senses and actions (<see cref="NeuralLayout"/>).
    /// </summary>
    /// <remarks>
    /// <para>The founder pool (GENE-20, GENE-21) holds <see cref="Founders"/> random networks drawn from a stream seeded by
    /// <see cref="FounderSeed"/> and the layout's hash, never by the run seed: every run starts from the same random nets,
    /// and newcomers below the floor are drawn from the same pool. There is no neutral allele.</para>
    /// <para>The brain reads the values from <see cref="DecisionQuery.Genome"/> (DEC-10); the genes block of a prompt shows
    /// one summary line, <c>- brain: "vec[1229]#ab12cd34"</c>. The gene sets no trait.</para>
    /// </remarks>
    public class NeuralGenomeGene : Gene
    {
        [SerializeField, Tooltip("Hidden layers, their sizes (owner: one layer of 12). Empty: inputs straight to outputs.")]
        List<int> hidden = new List<int> { 12 };
        [SerializeField, Tooltip("Activation of the hidden layers; outputs are linear.")]
        NeuralActivation activation = NeuralActivation.Tanh;
        [SerializeField, Tooltip("How founder weights are drawn: Xavier, or the previous lab's U(-10, 10).")]
        NeuralInit init = NeuralInit.Xavier;
        [SerializeField, Min(1), Tooltip("Random founder networks in the pool (owner: 256).")]
        int founders = 256;
        [SerializeField, Tooltip("Seed of the founder pool, with the layout's hash; the run seed plays no part (GENE-21).")]
        long founderSeed = 1;
        [SerializeField, Min(0.01f), Tooltip("Largest absolute weight; Check rejects more, mutation clamps to it.")]
        float clamp = 10f;

        NeuralLayout layout;                                             // built on first use, behind Layout
        List<FounderAllele> pool;                                        // built on first use, behind FounderPool
        int poolLength = -1;                                             // the vector length the pool was built for (V-71)

        /// <inheritdoc/>
        public override AlleleKind Kind => AlleleKind.Vector;
        /// <summary>True: the weights are part of the brain-visible genome key (DEC-31).</summary>
        /// <inheritdoc/>
        public override bool ReadByBrain => true;
        /// <inheritdoc/>
        public override int Decimals => 4;

        /// <summary>The hidden layers' sizes.</summary>
        public IReadOnlyList<int> Hidden => hidden;
        /// <summary>The hidden layers' activation.</summary>
        public NeuralActivation Activation => activation;
        /// <summary>How founder weights are drawn.</summary>
        public NeuralInit Init => init;
        /// <summary>The number of founder networks.</summary>
        public int Founders => founders;
        /// <summary>The founder pool's seed.</summary>
        public long FounderSeed => founderSeed;
        /// <summary>The largest absolute weight.</summary>
        public float Clamp => clamp;

        /// <summary>The species' network layout (plan M16 §5.1), built from its senses and actions on first use.</summary>
        public NeuralLayout Layout => layout ??= NeuralLayout.For(Species, hidden);

        /// <summary>Sets the settings from code (templates, tests); the layout and the pool are rebuilt on next use.</summary>
        /// <param name="hiddenSizes">Hidden layers' sizes; null keeps them.</param>
        /// <param name="founderCount">Founder networks; 0 keeps them.</param>
        /// <param name="seed">Founder seed; null keeps it.</param>
        /// <param name="initMode">Founder weights; null keeps it.</param>
        /// <param name="activationMode">Hidden activation; null keeps it.</param>
        /// <param name="maxWeight">Clamp; 0 keeps it.</param>
        public void Configure(IEnumerable<int> hiddenSizes = null, int founderCount = 0, long? seed = null, NeuralInit? initMode = null,
                              NeuralActivation? activationMode = null, float maxWeight = 0f)
        {
            if (hiddenSizes != null) hidden = new List<int>(hiddenSizes);
            if (founderCount > 0) founders = founderCount;
            if (seed.HasValue) founderSeed = seed.Value;
            if (initMode.HasValue) init = initMode.Value;
            if (activationMode.HasValue) activation = activationMode.Value;
            if (maxWeight > 0f) clamp = maxWeight;
            Rebuild();
        }

        /// <summary>Forgets the layout and the founder pool: the next use rebuilds both (the V-71 fix).</summary>
        public void Rebuild() { layout = null; pool = null; poolLength = -1; }

        /// <summary>
        /// Rebuilds the layout at each run, from the species as it is now; keeps the founder pool, so a pool that no longer
        /// fits is reported (V-71) instead of silently replaced.
        /// </summary>
        /// <inheritdoc/>
        public override void Initialize() => layout = null;

        /// <summary>A setting changed in the Inspector: the layout and the pool are rebuilt on next use.</summary>
        void OnValidate() => Rebuild();

        /// <summary>
        /// <see cref="Founders"/> random networks, the same in every run (GENE-21): drawn from the stream
        /// <c>neuro/founders/&lt;layout hash&gt;</c> of <see cref="FounderSeed"/>.
        /// </summary>
        /// <inheritdoc/>
        public override IReadOnlyList<FounderAllele> FounderPool
        {
            get
            {
                if (pool != null) return pool;
                var l = Layout;
                var rng = new RandomStream(founderSeed, "neuro/founders/" + l.Hash);
                var w = new float[l.VectorLength];
                pool = new List<FounderAllele>(founders);
                for (int k = 0; k < founders; k++)
                {
                    for (int i = 0; i < w.Length; i++) w[i] = Draw(l, i, rng);
                    pool.Add(new FounderAllele(AlleleValue.OfVector(w), "founder"));
                }
                poolLength = l.VectorLength;
                return pool;
            }
        }

        /// <summary>One founder weight: Xavier U(−a, a) for weights and 0 for biases, or U(−10, 10) for both (OldLab).</summary>
        /// <remarks>Always one draw, so every founder uses the same number of draws.</remarks>
        float Draw(NeuralLayout l, int index, RandomStream rng)
        {
            float u = rng.NextFloat() * 2f - 1f;
            if (init == NeuralInit.OldLab) return Mathf.Clamp(10f * u, -clamp, clamp);
            int layer = l.LayerOf(index);
            bool bias = (index - l.NeuronOffset(layer, 0)) % l.SliceLength(layer) == 0;
            return bias ? 0f : Mathf.Clamp(InitScale(layer) * u, -clamp, clamp);
        }

        /// <summary>The founders' weight scale of a layer: Xavier's a = √(6 / (fan-in + fan-out)), or 10 for OldLab.</summary>
        /// <param name="layer">The weight layer, 0 = inputs → first hidden layer.</param>
        /// <returns>The half-width of the founders' uniform draw; <see cref="NeuralMutation"/> replaces weights at this scale by default.</returns>
        public float InitScale(int layer)
        {
            if (init == NeuralInit.OldLab) return 10f;
            var sizes = Layout.Sizes;
            return (float)Math.Sqrt(6.0 / (sizes[layer] + sizes[layer + 1]));
        }

        /// <summary>Writes the summary line <c>- brain: "vec[1229]#ab12cd34"</c> into the genes block; sets no trait.</summary>
        /// <inheritdoc/>
        public override void Express(AlleleValue value, Expression e) => e.PromptGene(Label, value.ToString());

        /// <summary>Allowed: a vector of the layout's length, every value finite and within ±<see cref="Clamp"/>.</summary>
        /// <inheritdoc/>
        public override string Check(AlleleValue value)
        {
            if (value.Kind != AlleleKind.Vector) return "not a vector";
            if (value.VectorLength != Layout.VectorLength) return $"{value.VectorLength} weights, the layout {Layout} needs {Layout.VectorLength}";
            var v = value.Vector;
            for (int i = 0; i < v.Length; i++)
                if (float.IsNaN(v[i]) || float.IsInfinity(v[i]) || v[i] > clamp || v[i] < -clamp) return $"weight {i} is {v[i]}, outside ±{clamp}";
            return null;
        }

        /// <summary>V-71: the founders no longer fit the layout (a sense, ray kind, action or hidden size changed).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            foreach (int n in hidden)
                if (n < 1) { report.Error("V-71", this, $"Gene '{Label}' has a hidden layer of {n} neurons: every layer needs at least one."); return; }
            if (poolLength < 0) return;
            int now = Layout.VectorLength;
            if (now != poolLength)
                report.Error("V-71", this, $"The founders of gene '{Label}' have {poolLength} weights, but the species' senses and actions now need {now} (plan M16 §5.1).",
                             new ValidationFix("Rebuild founders", Rebuild));
        }
    }
}
