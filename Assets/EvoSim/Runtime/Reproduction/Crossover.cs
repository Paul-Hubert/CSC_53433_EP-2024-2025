using System;

namespace EvoSim
{
    /// <summary>Builds a baby's genome from its parents', locus by locus (09 §4). Genomes never cross species (SPEC-04).</summary>
    public abstract class Crossover : SpeciesModule
    {
        /// <summary>The baby's alleles from two parents (GENE-30), drawing from the mutation stream.</summary>
        public abstract Allele[] Cross(Genome a, Genome b, RandomStream rng);

        /// <summary>Asexual reproduction: the baby copies its parent (GENE-31).</summary>
        public virtual Allele[] Copy(Genome parent) => parent.ToArray();

        /// <summary>Throws unless both genomes belong to this species (SPEC-04, GENE-06).</summary>
        protected void CheckSpecies(Genome a, Genome b)
        {
            if (a.Species != Species || b.Species != Species)
                throw new InvalidOperationException($"Crossover across species ({a.Species.Id} × {b.Species.Id}) is not allowed (SPEC-04).");
        }
    }
}
