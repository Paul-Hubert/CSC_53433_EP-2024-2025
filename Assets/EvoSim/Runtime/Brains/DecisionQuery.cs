using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a brain receives for one deciding animal (DEC-10): its species, the genes its brain reads (label and
    /// sentence, in locus order), its observation, its situation text in the world's style, and any attachments.
    /// </summary>
    /// <remarks>
    /// The ask-brains phase builds one per deciding animal and passes the ones the memo and the cache could not answer to
    /// <see cref="Brain.Ask"/>. A brain reads what it needs: a prompt-based brain calls <see cref="Prompt"/>, a numeric one
    /// reads <see cref="Observation"/> and <see cref="Genes"/>. Tools (prompt preview, integrity gates) build queries too.
    /// </remarks>
    public sealed class DecisionQuery
    {
        /// <summary>The deciding animal's species: its actions give the row's length and order, its template the prompt.</summary>
        public Species Species { get; }

        /// <summary>Brain-visible genes, in locus order: (label, sentence or number).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Genes { get; }

        /// <summary>The animal's sense tokens, in sense order (SENSE-02).</summary>
        public Observation Observation { get; }

        /// <summary>The observation as text, in <see cref="Style"/> ("Energy: low. Food: 1-4 meters away. …", SENSE-10).</summary>
        public string Situation { get; }

        /// <summary>The world's text style the situation was written in (SENSE-11); part of the cache key.</summary>
        public TextStyle Style { get; }

        /// <summary>Attachments from senses (images…), or empty (SENSE-40).</summary>
        public IReadOnlyList<object> Attachments { get; }

        /// <summary>
        /// The brain-visible genome key (DEC-31): a hash of the species id and the alleles the brain reads, or the species id
        /// when the animal has no genome. Part of the memo and cache keys.
        /// </summary>
        public string GenomeKey { get; }

        /// <summary>The deciding animal's id (-1 when the query is built by a tool): for brains that aren't memoizable.</summary>
        public int AnimalId { get; set; } = -1;

        /// <summary>Makes a query. The ask-brains phase builds them; build one yourself for tools and tests.</summary>
        /// <param name="species">The deciding animal's species.</param>
        /// <param name="genes">The genes the brain reads, in locus order, from <see cref="BrainGenes"/>.</param>
        /// <param name="observation">The animal's observation.</param>
        /// <param name="situation">The observation as text, from <see cref="Species.Describe"/>.</param>
        /// <param name="style">The style of that text.</param>
        /// <param name="genomeKey">The brain-visible genome key (DEC-31).</param>
        /// <param name="attachments">Attachments from senses, or null for none.</param>
        public DecisionQuery(Species species, IReadOnlyList<KeyValuePair<string, string>> genes, Observation observation,
                             string situation, TextStyle style, string genomeKey, IReadOnlyList<object> attachments = null)
        {
            Species = species;
            Genes = genes;
            Observation = observation;
            Situation = situation;
            Style = style;
            GenomeKey = genomeKey;
            Attachments = attachments ?? System.Array.Empty<object>();
        }

        /// <summary>The genes block of the prompt: one line per gene, - label: "sentence" (PROMPT-02).</summary>
        /// <remarks>For example <c>- eat: "Eat whenever food is close."</c>, the label being the action name for action genes.</remarks>
        public string GenesBlock
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < Genes.Count; i++)
                {
                    if (i > 0) sb.Append('\n');
                    sb.Append("- ").Append(Genes[i].Key).Append(": \"").Append(Genes[i].Value).Append('"');
                }
                return sb.ToString();
            }
        }

        /// <summary>Lists the genes a brain reads from a genome, in locus order, with their labels (DEC-10, GENE-03).</summary>
        /// <param name="s">The species whose genes to read.</param>
        /// <param name="g">The genome, or null.</param>
        /// <returns>(label, sentence or number) per gene the brain reads (text genes by default); empty for a null genome.</returns>
        public static List<KeyValuePair<string, string>> BrainGenes(Species s, Genome g)
        {
            var genes = new List<KeyValuePair<string, string>>();
            if (g == null) return genes;
            var e = new Expression(null, s.Declarations, genes);
            for (int i = 0; i < s.Genes.Count; i++)
                if (s.Genes[i].ReadByBrain) s.Genes[i].Express(g[i].Value, e);
            return genes;
        }

        /// <summary>
        /// Builds the full prompt for a brain: the species' template with genes, situation and the brain's answer instruction.
        /// </summary>
        /// <param name="brain">The brain that will read it, for its <see cref="Brain.AnswerInstruction"/>; null leaves {ask} empty.</param>
        /// <returns>The prompt text, exactly what an LLM brain sends (PROMPT-01, PROMPT-04).</returns>
        public string Prompt(Brain brain) => Species.Prompt.Fill(GenesBlock, Situation, brain != null ? brain.AnswerInstruction : "");
    }
}
