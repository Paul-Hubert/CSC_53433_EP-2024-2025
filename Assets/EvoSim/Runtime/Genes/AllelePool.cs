using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Founder and contrast sentences shared by several text genes (GENE-24, 20 §3.8).</summary>
    [CreateAssetMenu(menuName = "EvoSim/Allele Pool", fileName = "AllelePool")]
    public class AllelePool : ScriptableObject
    {
        [TextArea(1, 3), Tooltip("Founder sentences: at most 12 words, imperative, plain words (GENE-22).")]
        public List<string> founders = new List<string>();

        [Tooltip("Pushes the action, used only to test brains (GENE-23).")]
        public string contrastPro = "";
        [Tooltip("Pulls the action, used only to test brains (GENE-23).")]
        public string contrastAnti = "";
    }
}
