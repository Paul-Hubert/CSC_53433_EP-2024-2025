namespace EvoSim.Neuro
{
    /// <summary>
    /// Sexual crossover for networks (plan M16 §5.3): each neuron of each layer, its bias and incoming weights together,
    /// comes whole from one parent or the other, with probability ½ (GENE-30 at neuron granularity).
    /// </summary>
    /// <remarks>
    /// <para>The draws come from the stream the breed phase gives, the species' <c>mutation</c> stream; every baby makes its
    /// own (GENE-32). A network equal to one parent's reuses that parent's allele. Otherwise it is registered at
    /// conception as a <c>recombinant</c> with both parents (GENE-12, C7), in conception order, so ids are deterministic;
    /// <see cref="NeuralMutation"/> then applies to it as to any inherited allele.</para>
    /// <para>Other loci are crossed as <see cref="UniformCrossover"/> does: one parent's allele each. Asexual babies use
    /// <see cref="Crossover.Copy"/> (GENE-31).</para>
    /// </remarks>
    public class NeuronCrossover : Crossover
    {
        /// <inheritdoc/>
        public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
        {
            CheckSpecies(a, b);
            var child = new Allele[a.Count];
            for (int i = 0; i < child.Length; i++)
            {
                if (Species.Genes[i] is NeuralGenomeGene gene && a[i].Kind == AlleleKind.Vector && b[i].Kind == AlleleKind.Vector)
                    child[i] = Mix(gene, a[i], b[i], rng);
                else child[i] = rng.NextBool() ? a[i] : b[i];
            }
            return child;
        }

        /// <summary>One network from two: each neuron's slice from parent A or B, one draw per neuron.</summary>
        Allele Mix(NeuralGenomeGene gene, Allele first, Allele second, RandomStream rng)
        {
            var layout = gene.Layout;
            var pa = first.Value.Vector;
            var pb = second.Value.Vector;
            var w = new float[layout.VectorLength];
            bool fromA = false, fromB = false;
            for (int l = 0; l < layout.LayerCount; l++)
            {
                int length = layout.SliceLength(l);
                for (int j = 0; j < layout.Sizes[l + 1]; j++)
                {
                    bool takeA = rng.NextBool();                                     // one draw per neuron, whatever the parents
                    int at = layout.NeuronOffset(l, j);
                    (takeA ? pa : pb).Slice(at, length).CopyTo(new System.Span<float>(w, at, length));
                    if (takeA) fromA = true; else fromB = true;
                }
            }
            if (first == second || !fromB) return first;
            if (!fromA) return second;
            var value = AlleleValue.OfVector(w);
            if (value.Equals(first.Value)) return first;
            if (value.Equals(second.Value)) return second;
            return World.Alleles.Register(gene, value, "recombinant", first.Id, "neuron-crossover", secondParent: second.Id);
        }
    }
}
