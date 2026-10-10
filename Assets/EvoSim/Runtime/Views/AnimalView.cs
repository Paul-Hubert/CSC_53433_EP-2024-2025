using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// One animal's view (20 §6): it holds only the animal's id and has no Update; the World places it in LateUpdate
    /// and never reads it back (SPACE-12).
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimalView : MonoBehaviour
    {
        /// <summary>The animal shown, or 0 while the view waits in its pool.</summary>
        public int AnimalId { get; internal set; }
    }
}
