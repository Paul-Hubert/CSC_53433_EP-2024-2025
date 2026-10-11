using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a species' animals look like (20 §6): a view prefab, or a placeholder capsule of a colour. Views only
    /// read the simulation (SPACE-12); a species without a Body runs unseen.
    /// </summary>
    /// <remarks>
    /// Place it under the species, with the other species modules. When a run starts in Play mode, the World makes a
    /// <see cref="ViewPool"/> from it, with <see cref="Prewarm"/> views ready. Each instance of the prefab has its colliders
    /// disabled and every part moved to the Ignore Raycast layer, so no sense ray ever hits a view. To show more than
    /// the reference, put an <see cref="AnimalView"/> subclass on the prefab's root.
    /// </remarks>
    public class Body : SpeciesModule
    {
        [SerializeField, Tooltip("The view prefab (mesh, animator, maybe a collider for picking); empty = a placeholder capsule.")]
        GameObject prefab;
        [SerializeField, Tooltip("The placeholder's colour, when there is no prefab.")]
        Color color = new Color(0.45f, 0.8f, 0.35f);
        [SerializeField, Min(0.05f), Tooltip("The view's size in meters (reference prey 0.6, predator 0.9).")]
        float size = 0.6f;
        [SerializeField, Min(0), Tooltip("Views made in advance when the run starts (0 = the initial population).")]
        int prewarm;

        /// <summary>The view prefab, or null for the placeholder capsule.</summary>
        public GameObject Prefab => prefab;
        /// <summary>The placeholder capsule's colour (not used with a prefab).</summary>
        public Color Color => color;
        /// <summary>The view's size, in meters: the prefab's scale is multiplied by it; the capsule is this wide and this high.</summary>
        public float Size => size;
        /// <summary>The views made when the run starts: the Inspector's number, or the species' initial population when it is 0.</summary>
        public int Prewarm => prewarm > 0 ? prewarm : Species != null ? Species.InitialPopulation : 0;

        /// <summary>Sets the look from code (scene builders, tests).</summary>
        /// <param name="viewPrefab">The view prefab, or null for the placeholder capsule.</param>
        /// <param name="placeholderColor">The capsule's colour.</param>
        /// <param name="meters">The view's size, in meters.</param>
        /// <returns>This Body, so calls can be chained after AddComponent.</returns>
        public Body Configure(GameObject viewPrefab, Color placeholderColor, float meters)
        {
            prefab = viewPrefab;
            color = placeholderColor;
            size = meters;
            return this;
        }
    }
}
