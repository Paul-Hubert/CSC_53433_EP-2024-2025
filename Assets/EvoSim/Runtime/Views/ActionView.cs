using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference body's view (20 §6): a marker above the head in the colour of the animal's action
    /// (<see cref="ActionColors"/>), and a body that darkens as its energy drops. It only reads the animal (SPACE-12).
    /// Copy it to show something else: override <see cref="AnimalView.OnShow"/> and read what you need.
    /// </summary>
    public class ActionView : AnimalView
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("The body's renderer: darker when the animal is hungry.")]
        Renderer body;
        [SerializeField, Tooltip("The marker above the head: the colour of the current action.")]
        Renderer marker;
        [SerializeField, Range(0f, 1f), Tooltip("Brightness of the body at zero energy (1 at full energy).")]
        float hungryBrightness = 0.35f;

        MaterialPropertyBlock block;
        Color bodyColor = Color.white, shownMarker, shownBody;

        public Renderer Body => body;
        public Renderer Marker => marker;

        public void Configure(Renderer bodyRenderer, Renderer markerRenderer)
        {
            body = bodyRenderer;
            marker = markerRenderer;
        }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (body != null && body.sharedMaterial != null) bodyColor = body.sharedMaterial.color;
        }

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

        void Set(Renderer r, Color c, ref Color shown)
        {
            if (c == shown) return;
            shown = c;
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
