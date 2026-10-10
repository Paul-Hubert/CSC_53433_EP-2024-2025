using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a species' animals look like (20 §6): a view prefab, or a placeholder capsule of a colour. Views only
    /// read the simulation (SPACE-12); a species without a Body runs unseen.
    /// </summary>
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

        public GameObject Prefab => prefab;
        public Color Color => color;
        public float Size => size;
        public int Prewarm => prewarm > 0 ? prewarm : Species != null ? Species.InitialPopulation : 0;

        public Body Configure(GameObject viewPrefab, Color placeholderColor, float meters)
        {
            prefab = viewPrefab;
            color = placeholderColor;
            size = meters;
            return this;
        }
    }
}
