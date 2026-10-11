using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Every allele seen in a run, once per (locus, value) (GENE-10), with ids <c>"&lt;locus&gt;:&lt;n&gt;"</c> (GENE-11).
    /// </summary>
    /// <remarks>
    /// <para>The World makes a new one at each run (<see cref="World.Alleles"/>). It registers the founder pools first, in
    /// pool order, so founders get the same ids in every run (GENE-21); founders and newcomers are registered as they are
    /// drawn, and the hatch phase registers each mutant.</para>
    /// <para>Sentences are compared after collapsing spaces, numbers at the gene's <see cref="Gene.Decimals"/>: registering
    /// a value already known at that locus returns the existing allele, with its first origin and lineage.</para>
    /// </remarks>
    public sealed class AlleleRegistry
    {
        readonly Dictionary<string, Allele> byKey = new Dictionary<string, Allele>();     // by locus id and canonical value
        readonly Dictionary<string, Allele> byId = new Dictionary<string, Allele>();      // by allele id
        readonly Dictionary<string, int> countPerLocus = new Dictionary<string, int>();   // alleles per locus id: the next n
        readonly List<Allele> all = new List<Allele>();

        /// <summary>Every allele in registration order; the recorder writes them to <c>alleles.jsonl</c>.</summary>
        public IReadOnlyList<Allele> All => all;
        /// <summary>The number of alleles registered so far, every locus together.</summary>
        public int Count => all.Count;

        /// <summary>
        /// Raised once for each new allele, just after it is registered; not raised when a known value is registered again.
        /// </summary>
        public event System.Action<Allele> Registered;

        /// <summary>
        /// Registers a value for a gene: a known value returns the existing allele, keeping its first origin (GENE-10).
        /// </summary>
        /// <param name="gene">The gene (locus) the value belongs to; its <see cref="Gene.LocusId"/> and
        /// <see cref="Gene.Decimals"/> are used.</param>
        /// <param name="value">The sentence or number.</param>
        /// <param name="origin"><c>founder</c>, <c>neutral</c>, <c>control</c>, <c>mutant</c>… (GENE-12); ignored for a known
        /// value.</param>
        /// <param name="parentId">For a mutant, the id of the allele it came from; else null.</param>
        /// <param name="op">For a mutant, the operator detail (<c>llm#3</c>, <c>gauss</c>); else null.</param>
        /// <param name="model">For a mutant sentence, the mutator model's name; else null.</param>
        /// <param name="seed">For a mutant sentence, the mutator call's seed; else null.</param>
        /// <returns>The new allele, or the one already registered with this value at this locus.</returns>
        public Allele Register(Gene gene, AlleleValue value, string origin, string parentId = null,
                               string op = null, string model = null, long? seed = null)
        {
            string locus = gene.LocusId;
            string key = locus + "\u0001" + value.Canonical(gene.Decimals);           // one key per (locus, canonical value)
            if (byKey.TryGetValue(key, out var known)) return known;
            countPerLocus.TryGetValue(locus, out int n);
            var allele = new Allele(locus, n, value, origin, parentId, op, model, seed);
            countPerLocus[locus] = n + 1;
            byKey.Add(key, allele);
            byId.Add(allele.Id, allele);
            all.Add(allele);
            Registered?.Invoke(allele);
            return allele;
        }

        /// <summary>Looks up the allele with this value at this gene, without registering anything.</summary>
        /// <param name="gene">The gene (locus).</param>
        /// <param name="value">The value, compared as <see cref="Register"/> compares it.</param>
        /// <returns>The allele, or null if this value was never registered at this gene.</returns>
        public Allele Find(Gene gene, AlleleValue value) =>
            byKey.TryGetValue(gene.LocusId + "\u0001" + value.Canonical(gene.Decimals), out var a) ? a : null;

        /// <summary>Looks up an allele by its id.</summary>
        /// <param name="id">An allele id, e.g. <c>prey.eat:3</c>; null is allowed.</param>
        /// <returns>The allele, or null for an unknown or null id.</returns>
        public Allele ById(string id) => id != null && byId.TryGetValue(id, out var a) ? a : null;

        /// <summary>Counts the alleles registered at one locus.</summary>
        /// <param name="locus">A locus id, e.g. <c>prey.eat</c>.</param>
        /// <returns>The number of alleles of that locus; 0 for an unknown locus.</returns>
        public int CountAt(string locus) => countPerLocus.TryGetValue(locus, out int n) ? n : 0;
    }
}
