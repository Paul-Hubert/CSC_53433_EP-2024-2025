using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// An animal view that also draws its ray fan (plan M16 §5.4): one line per ray, as long as the ray went and coloured by
    /// what stopped it. It shows the action colour like <see cref="ActionView"/>.
    /// </summary>
    /// <remarks>
    /// Put it on the root of a neural species' Body prefab instead of an <see cref="ActionView"/>. It casts the fan of the
    /// species' first <see cref="RayFanSense"/> from the animal's tick state each frame, with its own buffer, and only reads,
    /// so a run gives the same results with or without it (SPACE-12).
    /// </remarks>
    public class RayFanView : ActionView
    {
        static readonly Dictionary<RayHitKind, Color> colors = new Dictionary<RayHitKind, Color>
        {
            { RayHitKind.Threat, new Color(0.95f, 0.15f, 0.15f) },
            { RayHitKind.Prey, new Color(1f, 0.55f, 0.1f) },
            { RayHitKind.ReadyKin, new Color(1f, 0.45f, 0.75f) },
            { RayHitKind.Kin, new Color(0.3f, 0.9f, 0.9f) },
            { RayHitKind.OtherAnimal, new Color(0.75f, 0.6f, 1f) },
            { RayHitKind.Carcass, new Color(0.55f, 0.3f, 0.2f) },
            { RayHitKind.Entity, new Color(0.9f, 0.9f, 0.5f) },
            { RayHitKind.Food, new Color(0.35f, 0.85f, 0.25f) },
            { RayHitKind.Item, new Color(0.6f, 0.8f, 0.5f) },
            { RayHitKind.Cover, new Color(0.2f, 0.45f, 1f) },
            { RayHitKind.Water, new Color(0.3f, 0.6f, 0.95f) },
            { RayHitKind.Obstacle, new Color(0.4f, 0.4f, 0.4f) },
        };

        [SerializeField, Tooltip("Draw the fan.")]
        bool showFan = true;
        [SerializeField, Min(0.005f), Tooltip("The lines' width, in meters.")]
        float lineWidth = 0.04f;
        [SerializeField, Tooltip("The colour of a ray that hit nothing.")]
        Color missColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField, Min(0f), Tooltip("The height of the lines above the animal's feet, in meters.")]
        float height = 0.3f;

        readonly List<LineRenderer> lines = new List<LineRenderer>();
        RayHit[] hits = new RayHit[0];
        Material material;

        /// <summary>Shows or hides the fan.</summary>
        public bool ShowFan { get => showFan; set => showFan = value; }

        /// <summary>The colour a ray is drawn in when it stopped on this kind.</summary>
        /// <param name="kind">What stopped the ray.</param>
        /// <returns>The colour.</returns>
        public static Color ColorOf(RayHitKind kind) => colors.TryGetValue(kind, out var c) ? c : Color.white;

        /// <inheritdoc/>
        public override void OnShow(Animal a)
        {
            base.OnShow(a);
            var fan = showFan ? a.Species.Module<RayFanSense>() : null;
            if (fan == null) { SetVisible(0); return; }
            int n = fan.Rays;
            if (hits.Length < n) hits = new RayHit[n];
            fan.Cast(a, hits);
            float range = fan.RangeOf(a);
            var angles = fan.Angles;
            Grow(n);
            var origin = transform.position + Vector3.up * height;
            for (int k = 0; k < n; k++)
            {
                var h = hits[k];
                var dir = ActContext.HeadingDirection(a.Heading + angles[k]);
                var line = lines[k];
                line.SetPosition(0, origin);
                line.SetPosition(1, origin + dir * (h.Any ? h.Distance : range));
                var c = h.Any ? ColorOf(h.Kind) : missColor;
                line.startColor = c;
                line.endColor = c;
            }
            SetVisible(n);
        }

        /// <summary>Makes lines until there are <paramref name="n"/>.</summary>
        void Grow(int n)
        {
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            while (lines.Count < n)
            {
                var go = new GameObject("ray " + lines.Count);
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = line.endWidth = lineWidth;
                line.sharedMaterial = material;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                lines.Add(line);
            }
        }

        /// <summary>Shows the first <paramref name="n"/> lines and hides the others.</summary>
        void SetVisible(int n)
        {
            for (int i = 0; i < lines.Count; i++)
                if (lines[i].enabled != (i < n)) lines[i].enabled = i < n;
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
