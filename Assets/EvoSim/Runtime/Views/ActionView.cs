using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference body's view (20 §6): a marker above the head in the colour of the animal's action
    /// (<see cref="ActionColors"/>), and a body that darkens as its energy drops. It only reads the animal (SPACE-12).
    /// Copy it to show something else: override <see cref="AnimalView.OnShow"/> and read what you need.
    /// </summary>
    /// <remarks>
    /// It sits on the root of the reference body prefabs (PreyBody, PredatorBody), with its two renderers set. Colours go
    /// through a MaterialPropertyBlock, so no material is copied, and a renderer is updated only when its colour changed.
    /// </remarks>
    public class ActionView : AnimalView
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");      // the shader's main colour

        [SerializeField, Tooltip("The body's renderer: darker when the animal is hungry.")]
        Renderer body;
        [SerializeField, Tooltip("The marker above the head: the colour of the current action.")]
        Renderer marker;
        [SerializeField, Range(0f, 1f), Tooltip("Brightness of the body at zero energy (1 at full energy).")]
        float hungryBrightness = 0.35f;

        MaterialPropertyBlock block;                                       // sets colours without copying the material
        Color bodyColor = Color.white, shownMarker, shownBody;             // the body's own colour; the colours last set

        /// <summary>The body's renderer, darker when the animal is hungry; null for none.</summary>
        public Renderer Body => body;
        /// <summary>The renderer of the marker above the head, in the colour of the current action; null for none.</summary>
        public Renderer Marker => marker;

        /// <summary>Sets the two renderers from code (the editor's body builder, tests).</summary>
        /// <param name="bodyRenderer">The body's renderer, or null.</param>
        /// <param name="markerRenderer">The marker's renderer, or null.</param>
        public void Configure(Renderer bodyRenderer, Renderer markerRenderer)
        {
            body = bodyRenderer;
            marker = markerRenderer;
        }

        /// <summary>Makes the property block and reads the body's full-energy colour from its material. Called by Unity once.</summary>
        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (body != null && body.sharedMaterial != null) bodyColor = body.sharedMaterial.color;
        }

        /// <summary>
        /// Colours the marker after the current action (white while searching) and darkens the body with the energy left:
        /// full brightness at full energy, <c>hungryBrightness</c> at zero.
        /// </summary>
        /// <inheritdoc/>
        public override void OnShow(Animal a)
        {
            if (marker != null) Set(marker, ActionColors.Of(a), ref shownMarker);
            var energy = a.Species.Module<Energy>();
            if (body != null && energy != null && energy.Max > 0f)
            {
                float f = Mathf.Clamp01(a[energy.Value] / energy.Max);
                float k = Mathf.Round((hungryBrightness + (1f - hungryBrightness) * f) * 20f) / 20f;   // 20 steps: fewer block updates
                Set(body, new Color(bodyColor.r * k, bodyColor.g * k, bodyColor.b * k, 1f), ref shownBody);
            }
        }

        /// <summary>Gives a renderer a colour through the property block, only when it differs from the one last set.</summary>
        void Set(Renderer r, Color c, ref Color shown)
        {
            if (c == shown) return;
            shown = c;
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
