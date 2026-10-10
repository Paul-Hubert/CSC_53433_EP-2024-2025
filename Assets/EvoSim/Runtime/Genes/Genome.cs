using System;
using System.Collections.Generic;
using System.Text;

namespace EvoSim
{
    /// <summary>One allele per locus of a species, in the species' locus order (GENE-06).</summary>
    public sealed class Genome
    {
        readonly Allele[] alleles;
        string key, brainKey;

        public Species Species { get; }
        public IReadOnlyList<Allele> Alleles => alleles;
        public int Count => alleles.Length;
        public Allele this[int locus] => alleles[locus];

        public Genome(Species species, Allele[] alleles)
        {
            Species = species ?? throw new ArgumentNullException(nameof(species));
            if (alleles.Length != species.Genes.Count)
                throw new ArgumentException($"A genome of {species.Id} needs {species.Genes.Count} alleles, got {alleles.Length} (GENE-06).");
            for (int i = 0; i < alleles.Length; i++)
                if (alleles[i] == null || alleles[i].Locus != species.Genes[i].LocusId)
                    throw new ArgumentException($"Allele {i} of a {species.Id} genome belongs to locus {alleles[i]?.Locus}, not {species.Genes[i].LocusId} (GENE-06, SPEC-04).");
            this.alleles = alleles;
        }

        /// <summary>A copy of the alleles, to build a child genome.</summary>
        public Allele[] ToArray() => (Allele[])alleles.Clone();

        /// <summary>Hash of the species id and the allele values in locus order (GENE-13).</summary>
        public string Key => key ??= Hash(false);

        /// <summary>Hash of the species id and the alleles the brain reads (DEC-31).</summary>
        public string BrainKey => brainKey ??= Hash(true);

        string Hash(bool brainOnly)
        {
            var sb = new StringBuilder(Species.Id);
            for (int i = 0; i < alleles.Length; i++)
            {
                if (brainOnly && !Species.Genes[i].ReadByBrain) continue;
                sb.Append('\n').Append(Species.Genes[i].Label).Append('=').Append(alleles[i].Value.Canonical());
            }
            return Hashing.Sha256Hex(sb.ToString());
        }
    }
}
