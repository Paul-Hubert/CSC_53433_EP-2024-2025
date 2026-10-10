using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Every allele seen in a run, once per (locus, value) (GENE-10), with ids "&lt;locus&gt;:&lt;n&gt;" (GENE-11).</summary>
    public sealed class AlleleRegistry
    {
        readonly Dictionary<string, Allele> byKey = new Dictionary<string, Allele>();
        readonly Dictionary<string, Allele> byId = new Dictionary<string, Allele>();
        readonly Dictionary<string, int> countPerLocus = new Dictionary<string, int>();
        readonly List<Allele> all = new List<Allele>();

        /// <summary>Every allele in registration order (for alleles.jsonl).</summary>
        public IReadOnlyList<Allele> All => all;
        public int Count => all.Count;

        /// <summary>Raised for each new allele (the recorder writes it).</summary>
        public event System.Action<Allele> Registered;

        /// <summary>
        /// Registers a value for a gene: a known value returns the existing allele, keeping its first origin (GENE-10).
        /// </summary>
        public Allele Register(Gene gene, AlleleValue value, string origin, string parentId = null,
                               string op = null, string model = null, long? seed = null)
        {
            string locus = gene.LocusId;
            string key = locus + "\u0001" + value.Canonical(gene.Decimals);
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

        /// <summary>The allele with this value at this gene, or null if never registered.</summary>
        public Allele Find(Gene gene, AlleleValue value) =>
            byKey.TryGetValue(gene.LocusId + "\u0001" + value.Canonical(gene.Decimals), out var a) ? a : null;

        public Allele ById(string id) => id != null && byId.TryGetValue(id, out var a) ? a : null;

        /// <summary>Number of alleles registered for a locus.</summary>
        public int CountAt(string locus) => countPerLocus.TryGetValue(locus, out int n) ? n : 0;
    }
}
