using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A gene (locus) of a species: a sentence for the brain or a number for a trait (GENE-01…04).</summary>
    public abstract class Gene : SpeciesModule
    {
        [SerializeField, Tooltip("What the brain and the inspector show. Empty = the bound action's name (GENE-05).")]
        protected string label = "";

        /// <summary>The action this gene is bound to (GENE-05), or null for a free gene.</summary>
        public AnimalAction Action { get; internal set; }

        public string Label =>
            !string.IsNullOrEmpty(label) ? label : Action != null ? Action.Name : gameObject.name;

        /// <summary>"&lt;species id&gt;.&lt;label&gt;", unique in the world (GENE-11).</summary>
        public string LocusId => Species.Id + "." + Label;

        /// <summary>This gene's position in the species' locus order.</summary>
        public int Locus { get; internal set; } = -1;

        public abstract AlleleKind Kind { get; }

        /// <summary>Whether the brain reads this gene (text genes; number genes only if configured, GENE-04).</summary>
        public virtual bool ReadByBrain => Kind == AlleleKind.Text;

        /// <summary>Precision used to compare number alleles (GENE-10).</summary>
        public virtual int Decimals => 4;

        /// <summary>The founder pool with its origins, neutral allele included (GENE-20).</summary>
        public abstract IReadOnlyList<FounderAllele> FounderPool { get; }

        /// <summary>Whether this allele may mutate (the neutral allele's setting, MUT-22).</summary>
        public virtual bool MayMutate(Allele allele) => true;

        /// <summary>Turn this gene's allele into what acts: a prompt line or a trait (GENE-03/04).</summary>
        public abstract void Express(AlleleValue value, Expression e);

        /// <summary>Values allowed for this gene, e.g. the mutation guards for sentences (GENE-22).</summary>
        public virtual string Check(AlleleValue value) => null;
    }
}
