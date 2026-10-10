using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The nearest entity this animal may eat (06 §4): a carcass with portions left, not its own kill, not eaten from
    /// by it yet; or an egg its diet eats. Reference: the predator's "Carcass" sense.
    /// </summary>
    public class NearestEntitySense : BandedSense
    {
        [SerializeField, Tooltip("\"carcass\" or \"egg\".")]
        string kind = "carcass";
        [SerializeField, Tooltip("For eggs: whose eggs (empty = any the diet eats).")]
        string ofSpecies = "";

        Species of;

        protected override string DefaultLabel => "Carcass";

        public override void Initialize()
        {
            base.Initialize();
            of = string.IsNullOrEmpty(ofSpecies) ? null : World.FindSpeciesByName(ofSpecies);
        }

        public override int Read(Animal a, SenseContext s)
        {
            float vision = a.Trait(Vision);
            float? d = kind == "egg" ? s.NearestEgg(a, of, vision)?.Distance : s.NearestCarcass(a, vision)?.Distance;
            return d == null ? NoneToken : TokenFor(d.Value, vision);
        }

        public void SetKind(string entityKind, string species = "")
        {
            kind = entityKind;
            ofSpecies = species ?? "";
        }

        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (kind != "carcass" && kind != "egg") report.Error("V-20", this, $"Sense '{Label}': unknown entity kind '{kind}'.");
        }
    }
}
