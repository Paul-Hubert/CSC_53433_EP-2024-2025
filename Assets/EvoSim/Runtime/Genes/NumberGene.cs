using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A number expressed into one trait (GENE-04), e.g. a stamina gene setting stamina.max.</summary>
    /// <remarks>
    /// <para>Place it under the species and set its trait (a name a module declares), its range and its founder values;
    /// <see cref="GaussianMutation"/> mutates it and clamps the result to the range (MUT-20). It stays out of the prompt
    /// unless "show in prompt" is on.</para>
    /// <para>Give the trait a cost, e.g. a metabolism that grows with it: without one the gene drifts to whichever end
    /// helps (09 §5, V-47).</para>
    /// </remarks>
    public class NumberGene : Gene, INumberRange
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

        List<FounderAllele> poolCache;                               // FounderPool, built on first use; reset when settings change

        /// <inheritdoc/>
        public override AlleleKind Kind => AlleleKind.Number;
        /// <summary>True when the value is written into the prompt (the "show in prompt" setting, off in the reference).</summary>
        /// <inheritdoc/>
        public override bool ReadByBrain => showInPrompt;
        /// <summary>The decimals setting (reference 4).</summary>
        /// <inheritdoc/>
        public override int Decimals => decimals;
        /// <summary>Sets the decimals the registry compares (GENE-10), from code.</summary>
        /// <param name="d">The number of decimals, clamped to [0, 8].</param>
        public void SetDecimals(int d) { decimals = Mathf.Clamp(d, 0, 8); poolCache = null; }
        /// <summary>The name of the trait this gene sets, e.g. <c>stamina.max</c>.</summary>
        public string Trait => trait;
        /// <summary>True when the value multiplies the trait's default; false when it replaces the trait.</summary>
        public bool MultiplyDefault => multiplyDefault;
        /// <summary>The smallest value of the gene's own range; mutants outside the range are refused (MUT-20).</summary>
        public float Min => min;
        /// <summary>The largest value of the gene's own range; mutants outside the range are refused (MUT-20).</summary>
        public float Max => max;
        /// <summary>The founder values, as set in the Inspector.</summary>
        public IReadOnlyList<float> Founders => founders;

        /// <summary>The founder values, each with the origin <c>founder</c>; a number gene has no neutral allele.</summary>
        /// <inheritdoc/>
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

        /// <summary>
        /// Sets the trait (or multiplies its default) and, when shown in the prompt, writes the value with at most 2 decimals.
        /// </summary>
        /// <inheritdoc/>
        public override void Express(AlleleValue value, Expression e)
        {
            e.SetTrait(trait, value.Number, multiplyDefault);
            if (showInPrompt) e.PromptGene(Label, value.Number.ToString("0.##", CultureInfo.InvariantCulture));
        }

        /// <summary>Refuses a value outside the gene's range [<see cref="Min"/>, <see cref="Max"/>].</summary>
        /// <inheritdoc/>
        public override string Check(AlleleValue value) =>
            value.Number < min || value.Number > max ? $"{value.Number} outside [{min}, {max}]" : null;

        /// <summary>Forgets the cached founder pool, so that changes made in the Inspector are seen.</summary>
        /// <inheritdoc/>
        public override void Initialize() => poolCache = null;

        /// <summary>
        /// Reports a trait no module declares (V-45), founder values outside the range (V-46), an empty founder pool (V-41)
        /// and a trait no cost depends on (V-47).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            var t = Species.Declarations.FindTrait(trait);
            if (!t.IsValid) { report.Error("V-45", this, $"Number gene '{Label}' sets the trait '{trait}', which no module declares."); return; }
            foreach (var f in founders)
                if (f < min || f > max)
                    report.Warning("V-46", this, $"Founder value {f} of gene '{Label}' is outside its range [{min}, {max}].", new ValidationFix("Clamp", () =>
                    {
                        for (int i = 0; i < founders.Count; i++) founders[i] = Mathf.Clamp(founders[i], min, max);
                        poolCache = null;
                    }));
            if (founders.Count == 0) report.Error("V-41", this, $"Gene '{Label}' has an empty founder pool.");
            if (!Species.Declarations.HasCost(trait))
                report.Info("V-47", this, $"No cost depends on '{trait}': gene '{Label}' will drift to whichever end helps (09 §5). " +
                                          "Pair it with a cost, e.g. Metabolism's cost trait.");
        }

        /// <summary>Sets the gene from code (WorldBuilder, tests).</summary>
        /// <param name="traitName">The trait it sets, e.g. <c>stamina.max</c>.</param>
        /// <param name="rangeMin">The smallest value of the gene's range.</param>
        /// <param name="rangeMax">The largest value of the gene's range.</param>
        /// <param name="founderValues">The founder values, within the range.</param>
        /// <param name="multiply">True: the value multiplies the trait's default. False: it replaces the trait.</param>
        /// <param name="geneLabel">A label, or null to keep the current one.</param>
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
