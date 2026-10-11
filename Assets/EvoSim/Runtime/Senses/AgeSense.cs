using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Young or adult (06 §4): adult from the maturity trait (ANIM-36), reference 150 ticks.</summary>
    /// <remarks>
    /// Tokens "young" (0) and "adult" (1); an animal is adult once its age in ticks reaches its "maturity" trait. Texts: V1
    /// "Age: adult.", V2 "I am an adult." / "I am young.". Reference label "Age".
    /// </remarks>
    public class AgeSense : Sense
    {
        [SerializeField, Min(0f), Tooltip("Maturity when no module declares the trait \"maturity\", in ticks (reference 150).")]
        float maturity = 150f;

        readonly List<string> tokens = new List<string> { "young", "adult" };
        TraitId maturityTrait;   // the species' "maturity" trait, declared here or by another module

        /// <inheritdoc/>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>"Age".</summary>
        /// <inheritdoc/>
        protected override string DefaultLabel => "Age";

        /// <summary>
        /// Declares the "maturity" trait, in ticks, with this sense's setting as its default; if another module declared it
        /// first, its declaration counts.
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) => maturityTrait = b.DeclareTrait("maturity", maturity, 0f, 100000f);

        /// <summary>"adult" (1) when the animal's age in ticks is at least its maturity trait, else "young" (0).</summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s) => a.Age >= a.Trait(maturityTrait) ? 1 : 0;

        /// <summary>V1 "Age: young." / "Age: adult."; V2 "I am young." / "I am an adult.".</summary>
        /// <inheritdoc/>
        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "I am an adult." : "I am young.") : $"{Label}: {tokens[token]}.";
    }
}
