using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A sentence the brain reads in the genes block of the prompt (GENE-03, PROMPT-02).</summary>
    public class TextGene : Gene
    {
        [SerializeField, Tooltip("Shared founder and contrast sentences; empty = the lists below.")]
        AllelePool pool;
        [SerializeField, TextArea(1, 3), Tooltip("Founder sentences: at most maxWords words, plain words (GENE-22).")]
        List<string> founders = new List<string>();
        [SerializeField, Tooltip("Whether the founder pool has a neutral allele (GENE-24).")]
        bool hasNeutral = true;
        [SerializeField, Tooltip("The neutral allele's text (reference \"No preference.\").")]
        string neutral = "No preference.";
        [SerializeField, Tooltip("Whether the neutral allele may mutate (MUT-22; the reference lets it).")]
        bool neutralMutates = true;
        [SerializeField, Tooltip("A sentence that pushes the action, for brain tests only (GENE-23).")]
        string contrastPro = "";
        [SerializeField, Tooltip("A sentence that pulls the action, for brain tests only (GENE-23).")]
        string contrastAnti = "";
        [SerializeField, Tooltip("Not bound to an action: appears under its own label (GENE-05).")]
        bool free;
        [SerializeField, Range(1, 64), Tooltip("Founder sentences pass the mutation guards with this word limit (GENE-22).")]
        int maxWords = SentenceGuards.ReferenceMaxWords;

        List<FounderAllele> poolCache;

        public override AlleleKind Kind => AlleleKind.Text;
        public bool IsFree => free;
        public bool HasNeutral => hasNeutral;
        public string Neutral => neutral;
        public int MaxWords => maxWords;
        public AllelePool Pool => pool;
        public IReadOnlyList<string> Founders => pool != null ? pool.founders : founders;
        public string ContrastPro => pool != null && !string.IsNullOrEmpty(pool.contrastPro) ? pool.contrastPro : contrastPro;
        public string ContrastAnti => pool != null && !string.IsNullOrEmpty(pool.contrastAnti) ? pool.contrastAnti : contrastAnti;

        public override IReadOnlyList<FounderAllele> FounderPool
        {
            get
            {
                if (poolCache != null) return poolCache;
                poolCache = new List<FounderAllele>();
                foreach (var f in Founders)
                    if (!string.IsNullOrWhiteSpace(f)) poolCache.Add(new FounderAllele(AlleleValue.OfText(f), "founder"));
                if (hasNeutral) poolCache.Add(new FounderAllele(AlleleValue.OfText(neutral), "neutral"));
                return poolCache;
            }
        }

        public override bool MayMutate(Allele allele) => neutralMutates || allele.Origin != "neutral";

        public override void Express(AlleleValue value, Expression e) => e.PromptGene(Label, value.Text);

        public override string Check(AlleleValue value) => SentenceGuards.Check(value.Text, maxWords);

        public override void Initialize() => poolCache = null;

        public override void Validate(ValidationReport report)
        {
            var seen = new HashSet<string>();
            foreach (var f in Founders)
            {
                if (string.IsNullOrWhiteSpace(f)) continue;
                string problem = Check(AlleleValue.OfText(f));                         // virtual: a gene kind may relax the guards
                if (problem != null) report.Error("V-40", this, $"Founder \"{f}\" of gene '{Label}': {problem} (GENE-22).");
                if (!seen.Add(AlleleValue.OfText(f).Text))
                    report.Warning("V-42", this, $"Gene '{Label}' lists \"{f}\" twice.", pool != null ? null : new ValidationFix("Remove the duplicates", () =>
                    {
                        var kept = new List<string>();
                        var once = new HashSet<string>();
                        foreach (var x in founders) if (once.Add(AlleleValue.OfText(x).Text)) kept.Add(x);
                        founders = kept;
                        poolCache = null;
                    }));
            }
            if (hasNeutral)
            {
                string problem = Check(AlleleValue.OfText(neutral));
                if (problem != null) report.Error("V-40", this, $"Neutral allele of gene '{Label}': {problem} (GENE-22).");
            }
            if (FounderPool.Count == 0)
                report.Error("V-41", this, $"Gene '{Label}' has an empty founder pool.", new ValidationFix("Add the neutral allele", () => { hasNeutral = true; poolCache = null; }));
            else if (!hasNeutral)
                report.Warning("V-43", this, $"Gene '{Label}' has no neutral allele.", new ValidationFix("Add \"No preference.\"", () => { hasNeutral = true; neutral = "No preference."; poolCache = null; }));
            if (string.IsNullOrWhiteSpace(ContrastPro) || string.IsNullOrWhiteSpace(ContrastAnti))
                report.Info("V-44", this, $"Gene '{Label}' has no contrast pair: brain tests skip it.");
        }

        /// <summary>Sets the founder settings from code (WorldBuilder, tests).</summary>
        public void Configure(IEnumerable<string> founderSentences, bool withNeutral = true, string neutralText = "No preference.",
                              bool isFree = false, string pro = "", string anti = "")
        {
            founders = new List<string>(founderSentences);
            hasNeutral = withNeutral;
            neutral = neutralText;
            free = isFree;
            contrastPro = pro;
            contrastAnti = anti;
            poolCache = null;
        }

        public void SetNeutralMutates(bool value) => neutralMutates = value;
        public void SetPool(AllelePool p) { pool = p; poolCache = null; }
    }
}
