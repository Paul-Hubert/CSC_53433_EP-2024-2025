using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// Random float mutations of a vector gene (plan M16 §5.2), apart from the LLM and Gaussian operators: it accepts vector
    /// genes only, so MUT-02 gives every network this operator and no other.
    /// </summary>
    /// <remarks>
    /// <para>The owner's "Gentle" defaults: <see cref="MutationOperator.Rate"/> 0.5 (MUT-01: the chance that a baby's
    /// network mutates). When it does, each weight independently, with <see cref="AddChance"/> (0.05) gets a random float
    /// added, U(−<see cref="AddStrength"/>, +<see cref="AddStrength"/>) (0.5) or N(0, <see cref="AddStrength"/>) with
    /// <see cref="Gaussian"/>; else with <see cref="ReplaceChance"/> (0.005) is replaced by U(−s, +s), s =
    /// <see cref="ReplaceStrength"/> or, at 0, the gene's founder scale of that layer; then it is clamped to ±clamp.</para>
    /// <para>When no weight changed, one index drawn at the end gets a random float added, so a mutation never returns its
    /// parent (MUT-04). Every draw comes from the given stream, the same number whatever happens (MUT-30): per weight the
    /// choice, the added float and the replacement, then the fallback's index and float. It finishes at once
    /// (<see cref="MutationJob.Done(AlleleValue,string,string,long?)"/>, operator <c>nn-add</c>, <c>nn-replace</c> or
    /// <c>nn-both</c>); eggs never wait (MUT-32). Rate 1, add 0.01 × 0.5 and replace 0.01 × 10 reproduce the old lab.</para>
    /// </remarks>
    public class NeuralMutation : MutationOperator
    {
        [SerializeField, Range(0f, 1f), Tooltip("Chance per weight of a random float added (Gentle: 0.05).")]
        float addChance = 0.05f;
        [SerializeField, Min(0f), Tooltip("Half-width of the added float, or its standard deviation with Gaussian (Gentle: 0.5).")]
        float addStrength = 0.5f;
        [SerializeField, Tooltip("Add N(0, strength) instead of U(-strength, strength).")]
        bool gaussian;
        [SerializeField, Range(0f, 1f), Tooltip("Chance per weight of being replaced by a random float (Gentle: 0.005).")]
        float replaceChance = 0.005f;
        [SerializeField, Min(0f), Tooltip("Half-width of a replacement; 0 = the gene's founder scale of the weight's layer.")]
        float replaceStrength;

        /// <summary>Sets the Gentle rate (0.5) on a new component; a saved value replaces it when the scene loads.</summary>
        public NeuralMutation() { Rate = 0.5f; }

        /// <summary>Chance per weight of a random float added.</summary>
        public float AddChance { get => addChance; set => addChance = Mathf.Clamp01(value); }
        /// <summary>Half-width (or standard deviation) of the added float.</summary>
        public float AddStrength { get => addStrength; set => addStrength = Mathf.Max(0f, value); }
        /// <summary>True: the added float is Gaussian.</summary>
        public bool Gaussian { get => gaussian; set => gaussian = value; }
        /// <summary>Chance per weight of a replacement.</summary>
        public float ReplaceChance { get => replaceChance; set => replaceChance = Mathf.Clamp01(value); }
        /// <summary>Half-width of a replacement; 0 = the gene's founder scale.</summary>
        public float ReplaceStrength { get => replaceStrength; set => replaceStrength = Mathf.Max(0f, value); }

        /// <summary>Vector genes only (MUT-02).</summary>
        /// <inheritdoc/>
        public override bool Accepts(Gene g) => g.Kind == AlleleKind.Vector;

        /// <inheritdoc/>
        public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng)
        {
            var parentValues = parent.Value.Vector;
            int n = parentValues.Length;
            if (n == 0) return MutationJob.Failed("empty vector");
            var neural = gene as NeuralGenomeGene;
            float clamp = neural != null ? neural.Clamp : float.MaxValue;
            var w = parentValues.ToArray();
            bool added = false, replaced = false;
            for (int i = 0; i < n; i++)
            {
                // Three draws per weight whatever happens (MUT-30): the choice, the added float, the replacement.
                double choice = rng.NextDouble();
                double x = gaussian ? rng.NextGaussian() : rng.NextDouble();
                double r = rng.NextDouble();
                if (choice < addChance)
                {
                    w[i] += Added(x);
                    added = true;
                }
                else if (choice < addChance + replaceChance)
                {
                    float s = replaceStrength > 0f ? replaceStrength : neural != null ? neural.InitScale(neural.Layout.LayerOf(i)) : 1f;
                    w[i] = (float)(r * 2.0 - 1.0) * s;
                    replaced = true;
                }
                w[i] = Mathf.Clamp(w[i], -clamp, clamp);
            }
            int at = rng.Range(0, n);                                                     // the fallback: drawn every time (MUT-30)
            double y = gaussian ? rng.NextGaussian() : rng.NextDouble();
            if (!added && !replaced)
            {
                w[at] = Mathf.Clamp(w[at] + Added(y), -clamp, clamp);
                added = true;
            }
            return MutationJob.Done(AlleleValue.OfVector(w), added && replaced ? "nn-both" : replaced ? "nn-replace" : "nn-add");
        }

        /// <summary>The float added for a draw: U(−s, s) from a uniform draw, or N(0, s) from a Gaussian one.</summary>
        float Added(double x) => gaussian ? (float)(x * addStrength) : (float)(x * 2.0 - 1.0) * addStrength;
    }
}
