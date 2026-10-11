using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a gene expresses into (GENE-03/04): a trait of the animal being born, or a line of the prompt.
    /// <see cref="Gene.Express"/> receives one and calls <see cref="SetTrait"/> or <see cref="PromptGene"/>.
    /// </summary>
    /// <remarks>
    /// <para>Every gene is expressed in two situations, each with one target only:</para>
    /// <list type="bullet">
    /// <item><description>at birth, by <see cref="Species.CreateAnimal"/>: <see cref="Animal"/> is the newborn, traits are
    /// set, prompt lines are dropped;</description></item>
    /// <item><description>when the genes block of a decision is written, by <see cref="DecisionQuery.BrainGenes"/>, for the
    /// genes the brain reads only: <see cref="Animal"/> is null, traits are dropped, prompt lines are collected.</description></item>
    /// </list>
    /// <para>So a gene's <see cref="Gene.Express"/> must act only through this object.</para>
    /// </remarks>
    public sealed class Expression
    {
        readonly SpeciesBuilder declarations;                         // the species' traits, found by name
        readonly List<KeyValuePair<string, string>> promptGenes;      // collects (label, text) lines; null at birth

        /// <summary>The animal whose traits are being set, or null when only prompt lines are wanted.</summary>
        public Animal Animal { get; }

        /// <summary>Prepares an expression for one target: an animal's traits, or the lines of a genes block.</summary>
        /// <param name="animal">The animal being born, or null when only prompt lines are wanted.</param>
        /// <param name="declarations">The species' declarations, where traits are found by name.</param>
        /// <param name="promptGenes">The list that collects (label, text) lines for the genes block, or null when no
        /// prompt line is wanted.</param>
        public Expression(Animal animal, SpeciesBuilder declarations, List<KeyValuePair<string, string>> promptGenes = null)
        {
            Animal = animal;
            this.declarations = declarations;
            this.promptGenes = promptGenes;
        }

        /// <summary>Sets a trait, or multiplies its default; the result is clamped to the trait's range (GENE-04, ANIM-18).</summary>
        /// <param name="trait">The trait's name, as a module declared it, e.g. <c>stamina.max</c>.</param>
        /// <param name="value">The value, in the trait's unit, or a factor when <paramref name="multiplyDefault"/> is true.</param>
        /// <param name="multiplyDefault">False: the value replaces the trait. True: the trait becomes its default times the value.</param>
        /// <remarks>Does nothing when <see cref="Animal"/> is null, or when no module declares the trait (V-45 reports it
        /// before the run).</remarks>
        public void SetTrait(string trait, float value, bool multiplyDefault)
        {
            if (Animal == null) return;
            var t = declarations.FindTrait(trait);
            if (!t.IsValid) return;                                  // V-45 reports it before the run
            Animal.SetTrait(t, multiplyDefault ? t.Default * value : value);
        }

        /// <summary>A sentence for the genes block of the prompt (PROMPT-02); dropped at birth.</summary>
        /// <param name="label">What the brain sees before the sentence: the gene's <see cref="Gene.Label"/>.</param>
        /// <param name="text">The sentence, or a number written as text.</param>
        public void PromptGene(string label, string text) =>
            promptGenes?.Add(new KeyValuePair<string, string>(label, text));
    }
}
