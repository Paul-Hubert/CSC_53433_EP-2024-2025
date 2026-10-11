using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// One animal's view (20 §6): it holds only the animal's id and has no Update; the World places it in LateUpdate
    /// and never reads it back (SPACE-12).
    /// </summary>
    /// <remarks>
    /// <para>It is the component on the root of a species' <see cref="Body"/> prefab. When an animal appears, the World takes
    /// a view from the species' <see cref="ViewPool"/>; when the animal is gone, it gives the view back. Each frame, in its
    /// LateUpdate, the World moves every view between its animal's previous and current tick positions, turns it toward
    /// where the animal goes, sets its height from the ground, then calls <see cref="OnShow"/>.</para>
    /// <para>Derive from it to show more of an animal (a colour per action, an animation by speed): override
    /// <see cref="OnShow"/> and put your component on the root of the Body's prefab; a prefab without one gets a plain
    /// AnimalView. A view only reads: nothing the World computes may depend on it, so a run gives the same results with
    /// or without views (SPACE-12). <see cref="ActionView"/> is the reference.</para>
    /// </remarks>
    /// <example>
    /// A trimmed <see cref="ActionView"/>: a marker in the colour of the animal's current action.
    /// <code>
    /// public class MarkerView : AnimalView
    /// {
    ///     static readonly int ColorId = Shader.PropertyToID("_Color");
    ///     [SerializeField] Renderer marker;
    ///     MaterialPropertyBlock block;
    ///
    ///     void Awake() => block = new MaterialPropertyBlock();
    ///
    ///     public override void OnShow(Animal a)
    ///     {
    ///         block.SetColor(ColorId, ActionColors.Of(a));
    ///         marker.SetPropertyBlock(block);
    ///     }
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public class AnimalView : MonoBehaviour
    {
        /// <summary>The animal shown, or -1 while the view waits in its pool (ids start at 0).</summary>
        public int AnimalId { get; internal set; } = -1;

        /// <summary>
        /// Called each frame after the World placed the view: change colours or animations from the animal's state here.
        /// Read the animal only; a view never writes to the run (SPACE-12).
        /// </summary>
        /// <remarks>
        /// The World calls it from its LateUpdate on the main thread, every frame the view is shown, also while the run is
        /// paused or waits for a brain in the Responsive mode. The animal is as the last tick left it. Don't change it, its
        /// species or the World, and draw no random number from the simulation's streams.
        /// </remarks>
        /// <param name="a">The animal this view shows.</param>
        public virtual void OnShow(Animal a) { }
    }
}
