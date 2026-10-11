using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Founder and contrast sentences shared by several text genes, as an asset (GENE-24, 20 §3.8).</summary>
    /// <remarks>
    /// Create one with Assets ▸ Create ▸ EvoSim ▸ Allele Pool and set it as the pool of one or more <see cref="TextGene"/>
    /// components: each of them then takes its founder sentences from the asset, and its contrast sentences too when the
    /// asset's are not empty. The neutral allele stays a setting of each gene.
    /// </remarks>
    [CreateAssetMenu(menuName = "EvoSim/Allele Pool", fileName = "AllelePool")]
    public class AllelePool : ScriptableObject
    {
        /// <summary>
        /// The founder sentences: at most 12 words (the gene's word limit), imperative, plain words (GENE-22). They replace
        /// the founder list of every gene that uses this pool.
        /// </summary>
        [TextArea(1, 3), Tooltip("Founder sentences: at most 12 words, imperative, plain words (GENE-22).")]
        public List<string> founders = new List<string>();

        /// <summary>A sentence that pushes the action, used only to test brains (GENE-23); empty = each gene's own.</summary>
        [Tooltip("Pushes the action, used only to test brains (GENE-23).")]
        public string contrastPro = "";
        /// <summary>A sentence that pulls the action, used only to test brains (GENE-23); empty = each gene's own.</summary>
        [Tooltip("Pulls the action, used only to test brains (GENE-23).")]
        public string contrastAnti = "";
    }
}
