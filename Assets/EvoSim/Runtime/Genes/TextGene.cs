using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A sentence the brain reads in the genes block of the prompt (GENE-03, PROMPT-02).</summary>
    public class TextGene : Gene
    {
        [SerializeField, Tooltip("Shared founder and contrast sentences; empty = the lists below.")]
        AllelePool pool;
        [SerializeField, TextArea(1, 3)] List<string> founders = new List<string>();
        [SerializeField, Tooltip("Whether the founder pool has a neutral allele (GENE-24).")]
        bool hasNeutral = true;
        [SerializeField] string neutral = "No preference.";
        [SerializeField, Tooltip("Whether the neutral allele may mutate (MUT-22; the reference lets it).")]
        bool neutralMutates = true;
        [SerializeField] string contrastPro = "", contrastAnti = "";
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

        public void SetLabel(string newLabel) => label = newLabel;
        public void SetNeutralMutates(bool value) => neutralMutates = value;
        public void SetPool(AllelePool p) { pool = p; poolCache = null; }
    }
}
