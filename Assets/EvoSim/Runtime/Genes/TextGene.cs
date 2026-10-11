using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A sentence the brain reads in the genes block of the prompt (GENE-03, PROMPT-02).</summary>
    /// <remarks>
    /// <para>Place it on or under an action: it is bound to that action and labelled with its name (GENE-05), like the
    /// prey's eat gene, <c>prey.eat</c>. A gene that belongs to no action (a temperament, say) is marked free and appears
    /// under its own label.</para>
    /// <para>Its founder pool is the founder sentences (from the shared <see cref="AllelePool"/> when one is set, else its
    /// own list), plus the neutral allele "No preference." when it has one (GENE-20, GENE-24). Every sentence, founder or
    /// mutant, must pass the sentence guards with <see cref="MaxWords"/> (GENE-22). The contrast pair is used only by brain
    /// tests (GENE-23).</para>
    /// </remarks>
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

        List<FounderAllele> poolCache;                               // FounderPool, built on first use; reset when settings change

        /// <inheritdoc/>
        public override AlleleKind Kind => AlleleKind.Text;
        /// <summary>True for a free gene, bound to no action: it appears under its own label (GENE-05).</summary>
        public bool IsFree => free;
        /// <summary>True when the founder pool includes the neutral allele (GENE-24).</summary>
        public bool HasNeutral => hasNeutral;
        /// <summary>The neutral allele's sentence (reference "No preference.").</summary>
        public string Neutral => neutral;
        /// <summary>The longest sentence allowed, in words (reference 12): founders and mutants are checked with it (GENE-22).</summary>
        public int MaxWords => maxWords;
        /// <summary>The shared pool asset, or null when the gene uses its own lists.</summary>
        public AllelePool Pool => pool;
        /// <summary>The founder sentences: the pool's when a pool is set, else the gene's own list.</summary>
        public IReadOnlyList<string> Founders => pool != null ? pool.founders : founders;
        /// <summary>
        /// The sentence that pushes the action, for brain tests only (GENE-23): the pool's when set and not empty, else the
        /// gene's own.
        /// </summary>
        public string ContrastPro => pool != null && !string.IsNullOrEmpty(pool.contrastPro) ? pool.contrastPro : contrastPro;
        /// <summary>
        /// The sentence that pulls the action, for brain tests only (GENE-23): the pool's when set and not empty, else the
        /// gene's own.
        /// </summary>
        public string ContrastAnti => pool != null && !string.IsNullOrEmpty(pool.contrastAnti) ? pool.contrastAnti : contrastAnti;

        /// <summary>
        /// The founder sentences (origin <c>founder</c>, blank ones skipped), then the neutral allele (origin <c>neutral</c>)
        /// when the gene has one.
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>False only for the neutral allele, when the "neutral mutates" setting is off (MUT-22).</summary>
        /// <inheritdoc/>
        public override bool MayMutate(Allele allele) => neutralMutates || allele.Origin != "neutral";

        /// <summary>Adds the sentence to the genes block, under the gene's label.</summary>
        /// <inheritdoc/>
        public override void Express(AlleleValue value, Expression e) => e.PromptGene(Label, value.Text);

        /// <summary>Applies the sentence guards with this gene's word limit (MUT-12, GENE-22).</summary>
        /// <inheritdoc/>
        public override string Check(AlleleValue value) => SentenceGuards.Check(value.Text, maxWords);

        /// <summary>Forgets the cached founder pool, so that changes made in the Inspector are seen.</summary>
        /// <inheritdoc/>
        public override void Initialize() => poolCache = null;

        /// <summary>
        /// Reports founders that fail the guards (V-40), founders listed twice (V-42), an empty pool (V-41), no neutral
        /// allele (V-43) and a missing contrast pair (V-44).
        /// </summary>
        /// <inheritdoc/>
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
        /// <param name="founderSentences">The founder sentences (the gene's own list; a pool, if set, still wins).</param>
        /// <param name="withNeutral">Whether the pool has a neutral allele.</param>
        /// <param name="neutralText">The neutral allele's sentence.</param>
        /// <param name="isFree">True for a free gene, bound to no action.</param>
        /// <param name="pro">The contrast sentence that pushes the action, or empty.</param>
        /// <param name="anti">The contrast sentence that pulls the action, or empty.</param>
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

        /// <summary>Sets whether the neutral allele may mutate (MUT-22), from code.</summary>
        /// <param name="value">True: it may mutate (the reference). False: it never mutates.</param>
        public void SetNeutralMutates(bool value) => neutralMutates = value;
        /// <summary>Sets the shared pool asset, from code.</summary>
        /// <param name="p">The pool, or null to use the gene's own lists.</param>
        public void SetPool(AllelePool p) { pool = p; poolCache = null; }
    }
}
