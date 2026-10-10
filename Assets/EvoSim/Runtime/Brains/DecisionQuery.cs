using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a brain receives for one deciding animal (DEC-10): its species, the genes its brain reads (label and
    /// sentence, in locus order), its observation, its situation text in the world's style, and any attachments.
    /// </summary>
    public sealed class DecisionQuery
    {
        public Species Species { get; }
        /// <summary>Brain-visible genes, in locus order: (label, sentence or number).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Genes { get; }
        public Observation Observation { get; }
        public string Situation { get; }
        public TextStyle Style { get; }
        /// <summary>Attachments from senses (images…), or empty (SENSE-40).</summary>
        public IReadOnlyList<object> Attachments { get; }
        /// <summary>The brain-visible genome key (DEC-31).</summary>
        public string GenomeKey { get; }
        /// <summary>The deciding animal's id (-1 when the query is built by a tool): for brains that aren't memoizable.</summary>
        public int AnimalId { get; set; } = -1;

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

        /// <summary>The genes a brain reads from a genome, in locus order, with their labels (DEC-10, GENE-03).</summary>
        public static List<KeyValuePair<string, string>> BrainGenes(Species s, Genome g)
        {
            var genes = new List<KeyValuePair<string, string>>();
            if (g == null) return genes;
            var e = new Expression(null, s.Declarations, genes);
            for (int i = 0; i < s.Genes.Count; i++)
                if (s.Genes[i].ReadByBrain) s.Genes[i].Express(g[i].Value, e);
            return genes;
        }

        /// <summary>The full prompt for a brain: the species' template with genes, situation and the brain's answer instruction.</summary>
        public string Prompt(Brain brain) => Species.Prompt.Fill(GenesBlock, Situation, brain != null ? brain.AnswerInstruction : "");
    }
}
