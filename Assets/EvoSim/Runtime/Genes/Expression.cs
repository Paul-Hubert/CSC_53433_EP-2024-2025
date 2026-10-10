using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>What a gene expresses into (GENE-03/04): a trait of the animal being born, or a line of the prompt.</summary>
    public sealed class Expression
    {
        readonly SpeciesBuilder declarations;
        readonly List<KeyValuePair<string, string>> promptGenes;

        /// <summary>The animal whose traits are being set, or null when only prompt lines are wanted.</summary>
        public Animal Animal { get; }

        public Expression(Animal animal, SpeciesBuilder declarations, List<KeyValuePair<string, string>> promptGenes = null)
        {
            Animal = animal;
            this.declarations = declarations;
            this.promptGenes = promptGenes;
        }

        /// <summary>Sets a trait, or multiplies its default; the result is clamped to the trait's range (GENE-04, ANIM-18).</summary>
        public void SetTrait(string trait, float value, bool multiplyDefault)
        {
            if (Animal == null) return;
            var t = declarations.FindTrait(trait);
            if (!t.IsValid) return;                                  // V-45 reports it before the run
            Animal.SetTrait(t, multiplyDefault ? t.Default * value : value);
        }

        /// <summary>A sentence for the genes block of the prompt (PROMPT-02).</summary>
        public void PromptGene(string label, string text) =>
            promptGenes?.Add(new KeyValuePair<string, string>(label, text));
    }
}
