namespace EvoSim.Editor
{
    /// <summary>
    /// The species inspector's prompt preview (21 §3, PROMPT-06): built by the same code as the Ask brains phase, so it
    /// is exactly what the brain receives — for JEV, its whole input text (T-PROMPT-05).
    /// </summary>
    public static class PromptPreview
    {
        /// <summary>The text a brain receives for a genome and an observation of a species.</summary>
        public static string For(Species s, Genome genome, Observation o, TextStyle style, Brain brain)
        {
            var q = Query(s, genome, o, style);
            return brain is HttpBrain http ? http.TextFor(q) : q.Prompt(brain);
        }

        /// <summary>The query the Ask brains phase would build (DEC-10).</summary>
        public static DecisionQuery Query(Species s, Genome genome, Observation o, TextStyle style) =>
            new DecisionQuery(s, DecisionQuery.BrainGenes(s, genome), o, s.Describe(o, style), style, genome != null ? genome.BrainKey : s.Id);

        /// <summary>A founder genome for the preview: the first founder of each gene (or the one at the given index).</summary>
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
        public static int Tokens(string text) => Brain.EstimateTokens(text);
    }
}
