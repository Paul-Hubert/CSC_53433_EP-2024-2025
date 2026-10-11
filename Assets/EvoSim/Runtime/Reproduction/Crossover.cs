using System;

namespace EvoSim
{
    /// <summary>Builds a baby's genome from its parents', locus by locus (09 §4). Genomes never cross species (SPEC-04).</summary>
    /// <remarks>
    /// <para>One crossover per species, placed under it and found by type (<c>Species.Module&lt;Crossover&gt;()</c>). At each
    /// conception the breed phase calls <see cref="Cross"/> once per baby of a two-parent litter, so siblings differ
    /// (GENE-32), and <see cref="Copy"/> per baby of a one-parent litter. Mutations are drawn afterwards, on the result.
    /// Without a crossover, babies copy their first parent (V-26 warns).</para>
    /// <para>What an implementation must keep: call <see cref="CheckSpecies"/> first; return a new array with one allele
    /// per locus, in locus order, taken from that same locus (loci are homologous, GENE-30); draw only from the stream it
    /// is given; never change the parents' genomes.</para>
    /// </remarks>
    /// <example>
    /// The reference, <see cref="UniformCrossover"/>:
    /// <code>
    /// public class UniformCrossover : Crossover
    /// {
    ///     public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
    ///     {
    ///         CheckSpecies(a, b);
    ///         var child = new Allele[a.Count];
    ///         for (int i = 0; i &lt; child.Length; i++) child[i] = rng.NextBool() ? a[i] : b[i];
    ///         return child;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Crossover : SpeciesModule
    {
        /// <summary>The baby's alleles from two parents (GENE-30), drawing from the mutation stream.</summary>
        /// <param name="a">The first parent's genome: the animal that chose to mate.</param>
        /// <param name="b">The partner's genome.</param>
        /// <param name="rng">The species' mutation stream (RAND-03): draw only from it.</param>
        /// <returns>A new array with one allele per locus, in locus order.</returns>
        public abstract Allele[] Cross(Genome a, Genome b, RandomStream rng);

        /// <summary>Asexual reproduction: the baby copies its parent (GENE-31).</summary>
        /// <param name="parent">The only parent's genome.</param>
        /// <returns>A new array with the parent's alleles.</returns>
        public virtual Allele[] Copy(Genome parent) => parent.ToArray();

        /// <summary>Throws unless both genomes belong to this species (SPEC-04, GENE-06).</summary>
        /// <param name="a">The first parent's genome.</param>
        /// <param name="b">The second parent's genome.</param>
        /// <exception cref="InvalidOperationException">A genome belongs to another species.</exception>
        protected void CheckSpecies(Genome a, Genome b)
        {
            if (a.Species != Species || b.Species != Species)
                throw new InvalidOperationException($"Crossover across species ({a.Species.Id} × {b.Species.Id}) is not allowed (SPEC-04).");
        }
    }
}
