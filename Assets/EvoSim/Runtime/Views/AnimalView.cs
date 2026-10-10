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
        /// <summary>The animal shown, or -1 while the view waits in its pool (ids start at 0).</summary>
        public int AnimalId { get; internal set; } = -1;

        /// <summary>
        /// Called each frame after the World placed the view: change colours or animations from the animal's state here.
        /// Read the animal only; a view never writes to the run (SPACE-12).
        /// </summary>
        public virtual void OnShow(Animal a) { }
    }
}
