using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The nearest entity this animal may eat (06 §4): a carcass with portions left, not its own kill, not eaten from
    /// by it yet; or an egg its diet eats. Reference: the predator's "Carcass" sense.
    /// </summary>
    /// <remarks>
    /// Tokens: the bands and "none", no "here". The default label is "Carcass" whatever the kind: give an egg sense its own
    /// label ("Eggs") in the Inspector. For entities of your own kind, write a <see cref="BandedSense"/> around
    /// <see cref="SenseContext.NearestEntity{T}"/>.
    /// </remarks>
    public class NearestEntitySense : BandedSense
    {
        [SerializeField, Tooltip("\"carcass\" or \"egg\".")]
        string kind = "carcass";
        [SerializeField, Tooltip("For eggs: whose eggs (empty = any the diet eats).")]
        string ofSpecies = "";

        Species of;   // whose eggs, found in Initialize; null for any species the diet eats

        /// <summary>"Carcass", for both kinds.</summary>
        /// <inheritdoc/>
        protected override string DefaultLabel => "Carcass";

        /// <summary>Builds the tokens and finds the species whose eggs to sense (an unknown name means any).</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            of = string.IsNullOrEmpty(ofSpecies) ? null : World.FindSpeciesByName(ofSpecies);
        }

        /// <summary>
        /// The band of the nearest egg ("egg") or carcass (any other kind) the animal may eat, within its vision; "none" when
        /// there is none.
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            float vision = a.Trait(Vision);
            float? d = kind == "egg" ? s.NearestEgg(a, of, vision)?.Distance : s.NearestCarcass(a, vision)?.Distance;
            return d == null ? NoneToken : TokenFor(d.Value, vision);
        }

        /// <summary>Sets what the sense looks for from code (WorldBuilder, tests), as the Inspector fields do.</summary>
        /// <param name="entityKind">"carcass" or "egg".</param>
        /// <param name="species">For eggs, whose eggs by id or name; empty or null for any the diet eats.</param>
        public void SetKind(string entityKind, string species = "")
        {
            kind = entityKind;
            ofSpecies = species ?? "";
        }

        /// <summary>Adds to the band checks V-20 for a kind that is neither "carcass" nor "egg".</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (kind != "carcass" && kind != "egg") report.Error("V-20", this, $"Sense '{Label}': unknown entity kind '{kind}'.");
        }
    }
}
