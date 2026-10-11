using System;
using System.Collections.Generic;
using System.Text;

namespace EvoSim
{
    /// <summary>One allele per locus of a species, in the species' locus order (GENE-06): the genes of one animal.</summary>
    /// <remarks>
    /// A genome never changes: a baby gets a new one, built by the crossover (<see cref="Crossover"/>) and completed by its
    /// mutations at hatching. Founders and newcomers get one from <see cref="World.FounderGenome"/>.
    /// </remarks>
    public sealed class Genome
    {
        readonly Allele[] alleles;
        string key, brainKey;                                        // computed on first use, behind Key and BrainKey

        /// <summary>The species it belongs to.</summary>
        public Species Species { get; }
        /// <summary>The alleles, one per locus, in locus order.</summary>
        public IReadOnlyList<Allele> Alleles => alleles;
        /// <summary>The number of loci: the species' number of genes.</summary>
        public int Count => alleles.Length;
        /// <summary>The allele at one locus.</summary>
        /// <param name="locus">The locus, from 0 to <see cref="Count"/> - 1 (a gene's <see cref="Gene.Locus"/>).</param>
        /// <returns>The allele the animal carries at that gene.</returns>
        public Allele this[int locus] => alleles[locus];

        /// <summary>Builds a genome and checks that it fits the species (GENE-06, SPEC-04).</summary>
        /// <param name="species">The species.</param>
        /// <param name="alleles">One allele per gene of the species, in locus order; kept, not copied, so don't change it
        /// afterwards.</param>
        /// <exception cref="ArgumentNullException">The species is null.</exception>
        /// <exception cref="ArgumentException">The array has the wrong length, or an allele belongs to another locus.</exception>
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
        /// <returns>A new array, one allele per locus, that the caller may change.</returns>
        public Allele[] ToArray() => (Allele[])alleles.Clone();

        /// <summary>
        /// Hash of the species id and the allele values in locus order (GENE-13): the same genes give the same key in any run.
        /// </summary>
        public string Key => key ??= Hash(false);

        /// <summary>Hash of the species id and the alleles the brain reads (DEC-31): equal keys give the same genes block.</summary>
        public string BrainKey => brainKey ??= Hash(true);

        /// <summary>A SHA-256 hash (16 hex digits) of the species id and each locus' label and canonical value.</summary>
        /// <param name="brainOnly">True: only the genes the brain reads (<see cref="Gene.ReadByBrain"/>).</param>
        string Hash(bool brainOnly)
        {
            var sb = new StringBuilder(Species.Id);
            for (int i = 0; i < alleles.Length; i++)
            {
                if (brainOnly && !Species.Genes[i].ReadByBrain) continue;
                // The value at the registry's precision (GENE-10).
                sb.Append('\n').Append(Species.Genes[i].Label).Append('=').Append(alleles[i].Value.Canonical(Species.Genes[i].Decimals));
            }
            return Hashing.Sha256Hex(sb.ToString());
        }
    }
}
