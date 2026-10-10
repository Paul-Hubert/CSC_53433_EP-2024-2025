using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Young or adult (06 §4): adult from the maturity trait (ANIM-36), reference 150 ticks.</summary>
    public class AgeSense : Sense
    {
        [SerializeField, Min(0f), Tooltip("Maturity when no module declares the trait \"maturity\", in ticks (reference 150).")]
        float maturity = 150f;

        readonly List<string> tokens = new List<string> { "young", "adult" };
        TraitId maturityTrait;

        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Age";

        public override void Declare(SpeciesBuilder b) => maturityTrait = b.DeclareTrait("maturity", maturity, 0f, 100000f);

        public override int Read(Animal a, SenseContext s) => a.Age >= a.Trait(maturityTrait) ? 1 : 0;

        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "I am an adult." : "I am young.") : $"{Label}: {tokens[token]}.";
    }
}
