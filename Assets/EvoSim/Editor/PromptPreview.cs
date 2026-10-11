namespace EvoSim.Editor
{
    /// <summary>
    /// The species inspector's prompt preview (21 §3, PROMPT-06): built by the same code as the Ask brains phase, so it
    /// is exactly what the brain receives — for JEV, its whole input text (T-PROMPT-05).
    /// </summary>
    /// <remarks>
    /// The <see cref="SpeciesInspector"/> calls it (and the editor tests, which check that it matches what the brains are
    /// sent). Nothing here changes the world except <see cref="FounderGenome"/>, which registers the founder alleles it uses
    /// in the world's allele registry.
    /// </remarks>
    public static class PromptPreview
    {
        /// <summary>The text a brain receives for a genome and an observation of a species.</summary>
        /// <param name="s">The species whose prompt template, senses and genes are used.</param>
        /// <param name="genome">The genome whose brain-visible sentences fill the genes block, or null for no genes.</param>
        /// <param name="o">The situation: one token per sense.</param>
        /// <param name="style">The situation text style (V1 tokens or V2 sentences).</param>
        /// <param name="brain">The brain that would answer: an HTTP brain gives the text it would send (JEV writes its own
        /// input), any other brain the filled prompt with its answer instruction; null leaves the answer instruction empty.</param>
        /// <returns>The full prompt text.</returns>
        public static string For(Species s, Genome genome, Observation o, TextStyle style, Brain brain)
        {
            var q = Query(s, genome, o, style);
            return brain is HttpBrain http ? http.TextFor(q) : q.Prompt(brain);
        }

        /// <summary>The query the Ask brains phase would build (DEC-10).</summary>
        /// <param name="s">The species of the deciding animal.</param>
        /// <param name="genome">The genome to read, or null: no genes, and the species id as the genome key.</param>
        /// <param name="o">The situation: one token per sense.</param>
        /// <param name="style">The situation text style (V1 or V2).</param>
        /// <returns>A query as a brain receives it, with no animal id (-1).</returns>
        public static DecisionQuery Query(Species s, Genome genome, Observation o, TextStyle style) =>
            new DecisionQuery(s, DecisionQuery.BrainGenes(s, genome), o, s.Describe(o, style), style, genome != null ? genome.BrainKey : s.Id);

        /// <summary>A founder genome for the preview: the first founder of each gene (or the one at the given index).</summary>
        /// <param name="w">The world whose allele registry records the founder alleles.</param>
        /// <param name="s">The species whose genes are read, in locus order.</param>
        /// <param name="which">The index in each gene's founder pool; a gene with fewer founders takes its last one.</param>
        /// <returns>A genome with one founder allele per locus.</returns>
        public static Genome FounderGenome(World w, Species s, int which = 0)
        {
            var alleles = new Allele[s.Genes.Count];
            for (int i = 0; i < alleles.Length; i++)
            {
                var g = s.Genes[i];
                var pool = g.FounderPool;
                alleles[i] = w.Alleles.Register(g, pool[System.Math.Min(which, pool.Count - 1)].Value, "founder");   // known: the same allele
            }
            return new Genome(s, alleles);
        }

        /// <summary>The token estimate shown next to the preview (PROMPT-06).</summary>
        /// <param name="text">A prompt text.</param>
        /// <returns>About one token per four characters, as <see cref="Brain.EstimateTokens"/> counts.</returns>
        public static int Tokens(string text) => Brain.EstimateTokens(text);
    }
}
