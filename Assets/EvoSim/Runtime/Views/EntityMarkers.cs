using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Small markers for the World's entities (ENV-20): a dark red disc per carcass, an ivory ball per egg, a grey cube
    /// for any other kind. Pooled primitives, placed each frame from the entity systems; they only read the World
    /// (SPACE-12): no collider, the Ignore Raycast layer.
    /// </summary>
    /// <remarks>
    /// One of the run views: put it on any GameObject of the scene, outside the World (the "Run views" object of the
    /// reference scenes). It reads every <see cref="EntityKind"/> service of the World through
    /// <see cref="EntityKind.EntityAt"/>; a kind that keeps no list of its entities shows nothing.
    /// </remarks>
    [DisallowMultipleComponent]
    public class EntityMarkers : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");      // the shader's main colour

        [SerializeField, Tooltip("The World drawn; empty = the first World of the scene.")]
        World world;
        [SerializeField] Color carcass = new Color(0.45f, 0.05f, 0.05f);   // the carcass discs' colour
        [SerializeField] Color egg = new Color(0.95f, 0.92f, 0.78f);       // the eggs' colour
        [SerializeField] Color other = new Color(0.5f, 0.5f, 0.5f);        // the colour of any other kind's cubes

        /// <summary>The markers of one entity kind, reused from frame to frame.</summary>
        sealed class Pool
        {
            public readonly List<GameObject> Items = new List<GameObject>();         // every marker made for the kind
            public int Shown;                                                          // markers placed this frame; the rest hide
            public float Lift;                                                         // half the marker's height: it sits on the ground
        }

        readonly Dictionary<string, Pool> pools = new Dictionary<string, Pool>();      // by kind name, for lookups only
        readonly List<Pool> ordered = new List<Pool>();                                   // iterated: never a dictionary (RAND-05)
        readonly List<EntityKind> kinds = new List<EntityKind>();                         // the World's entity kinds, gathered each frame
        MaterialPropertyBlock block;                                                       // colours the markers without copying materials

        /// <summary>The World drawn; when null, the first World of the scene is found on the next frame.</summary>
        public World World { get => world; set => world = value; }
        /// <summary>The tick the markers last showed (-1 before the first frame).</summary>
        public int ShownTick { get; private set; } = -1;

        /// <summary>Markers shown this frame for a kind ("carcass", "egg").</summary>
        /// <param name="kind">The entity kind's name, as <see cref="EntityKind.Kind"/> gives it.</param>
        /// <returns>How many markers of that kind are shown; 0 for a kind never seen.</returns>
        public int Shown(string kind) => pools.TryGetValue(kind, out var p) ? p.Shown : 0;

        /// <summary>The marker shown for the i-th entity of a kind this frame (tests compare positions).</summary>
        /// <param name="kind">The entity kind's name, such as <c>carcass</c>.</param>
        /// <param name="i">The entity's place among the shown ones, from 0 to <see cref="Shown"/> − 1, in the kind's order.</param>
        /// <returns>The marker's transform.</returns>
        /// <exception cref="KeyNotFoundException">No marker of that kind was ever made.</exception>
        public Transform Marker(string kind, int i) => pools[kind].Items[i].transform;

        /// <summary>
        /// Places one marker on the ground under each entity not used up, as the last tick left them, and hides the markers
        /// left over. Called by Unity each frame, after the World's tick.
        /// </summary>
        void LateUpdate()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (world == null || !world.IsInitialized) return;
            ShownTick = world.Tick;
            kinds.Clear();
            kinds.AddRange(world.ServicesOf<EntityKind>());
            foreach (var p in ordered) p.Shown = 0;
            foreach (var k in kinds)
            {
                var pool = PoolOf(k.Kind);
                for (int i = 0; i < k.EntityCount; i++)
                {
                    var e = k.EntityAt(i);
                    if (e == null || e.UsedUp) continue;
                    var go = pool.Shown < pool.Items.Count ? pool.Items[pool.Shown] : Make(k.Kind, pool);
                    pool.Shown++;
                    var p = e.Position;
                    if (world.Ground != null) p.y = world.Ground.Height(p);
                    p.y += pool.Lift;
                    go.transform.position = p;
                    if (!go.activeSelf) go.SetActive(true);
                }
            }
            foreach (var p in ordered)
                for (int i = p.Shown; i < p.Items.Count; i++)
                    if (p.Items[i].activeSelf) p.Items[i].SetActive(false);
        }

        /// <summary>The pool of a kind, made the first time the kind is met.</summary>
        Pool PoolOf(string kind)
        {
            if (!pools.TryGetValue(kind, out var p)) { pools.Add(kind, p = new Pool()); ordered.Add(p); }
            return p;
        }

        /// <summary>
        /// Makes one more marker for a kind and adds it to its pool: a primitive in the kind's colour, without a collider,
        /// on the Ignore Raycast layer, casting no shadow, never saved with the scene.
        /// </summary>
        GameObject Make(string kind, Pool pool)
        {
            GameObject go;
            Color c;
            switch (kind)
            {
                case "carcass":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(0.8f, 0.04f, 0.8f);
                    c = carcass;
                    break;
                case "egg":
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(0.3f, 0.38f, 0.3f);
                    c = egg;
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = Vector3.one * 0.3f;
                    c = other;
                    break;
            }
            pool.Lift = go.transform.localScale.y * (kind == "carcass" ? 1f : 0.5f);   // a cylinder is 2 high, a sphere or cube 1
            go.name = kind + " marker";
            go.hideFlags = HideFlags.DontSave;
            DestroyImmediate(go.GetComponent<Collider>());                             // nothing to hit (SPACE-12)
            go.layer = ViewPool.IgnoreRaycastLayer;
            go.transform.SetParent(transform, true);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            block ??= new MaterialPropertyBlock();
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
            pool.Items.Add(go);
            return go;
        }
    }
}
