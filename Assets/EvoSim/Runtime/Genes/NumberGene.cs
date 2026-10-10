using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A number expressed into one trait (GENE-04), e.g. a stamina gene setting stamina.max.</summary>
    public class NumberGene : Gene
    {
        [SerializeField, Tooltip("The trait this gene sets (a name a module declares, V-45).")]
        string trait = "stamina.max";
        [SerializeField, Tooltip("False: the value replaces the trait. True: it multiplies the trait's default.")]
        bool multiplyDefault;
        [SerializeField, Tooltip("The gene's own range; mutations are clamped to it (MUT-20).")]
        float min = 20f, max = 120f;
        [SerializeField, Tooltip("Founder values, drawn uniformly (GENE-20); within the gene's range.")]
        List<float> founders = new List<float> { 45f, 60f, 75f };
        [SerializeField, Tooltip("Write the value into the prompt (GENE-04 MAY); off in the reference.")]
        bool showInPrompt;
        [SerializeField, Range(0, 8), Tooltip("Decimals compared when registering alleles (GENE-10).")]
        int decimals = 4;

        List<FounderAllele> poolCache;

        public override AlleleKind Kind => AlleleKind.Number;
        public override bool ReadByBrain => showInPrompt;
        public override int Decimals => decimals;
        public string Trait => trait;
        public bool MultiplyDefault => multiplyDefault;
        public float Min => min;
        public float Max => max;
        public IReadOnlyList<float> Founders => founders;

        public override IReadOnlyList<FounderAllele> FounderPool
        {
            get
            {
                if (poolCache != null) return poolCache;
                poolCache = new List<FounderAllele>();
                foreach (var f in founders) poolCache.Add(new FounderAllele(AlleleValue.OfNumber(f), "founder"));
                return poolCache;
            }
        }

        public override void Express(AlleleValue value, Expression e)
        {
            e.SetTrait(trait, value.Number, multiplyDefault);
            if (showInPrompt) e.PromptGene(Label, value.Number.ToString("0.##", CultureInfo.InvariantCulture));
        }

        public override string Check(AlleleValue value) =>
            value.Number < min || value.Number > max ? $"{value.Number} outside [{min}, {max}]" : null;

        public override void Initialize() => poolCache = null;

        public override void Validate(ValidationReport report)
        {
            var t = Species.Declarations.FindTrait(trait);
            if (!t.IsValid) { report.Error("V-45", this, $"Number gene '{Label}' sets the trait '{trait}', which no module declares."); return; }
            foreach (var f in founders)
                if (f < min || f > max) report.Warning("V-46", this, $"Founder value {f} of gene '{Label}' is outside its range [{min}, {max}].");
            if (founders.Count == 0) report.Error("V-41", this, $"Gene '{Label}' has an empty founder pool.");
            if (t.Default < t.Min || t.Default > t.Max) report.Warning("V-36", this, $"The default of trait '{trait}' is outside its range.");
        }

        /// <summary>Sets the gene from code (WorldBuilder, tests).</summary>
        public void Configure(string traitName, float rangeMin, float rangeMax, IEnumerable<float> founderValues,
                              bool multiply = false, string geneLabel = null)
        {
            trait = traitName;
            min = rangeMin;
            max = rangeMax;
            founders = new List<float>(founderValues);
            multiplyDefault = multiply;
            if (geneLabel != null) label = geneLabel;
            poolCache = null;
        }
    }
}
