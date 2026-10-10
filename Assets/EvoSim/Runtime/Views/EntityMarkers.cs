using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Small markers for the World's entities (ENV-20): a dark red disc per carcass, an ivory ball per egg, a grey cube
    /// for any other kind. Pooled primitives, placed each frame from the entity systems; they only read the World
    /// (SPACE-12): no collider, the Ignore Raycast layer.
    /// </summary>
    [DisallowMultipleComponent]
    public class EntityMarkers : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("The World drawn; empty = the first World of the scene.")]
        World world;
        [SerializeField] Color carcass = new Color(0.45f, 0.05f, 0.05f);
        [SerializeField] Color egg = new Color(0.95f, 0.92f, 0.78f);
        [SerializeField] Color other = new Color(0.5f, 0.5f, 0.5f);

        sealed class Pool
        {
            public readonly List<GameObject> Items = new List<GameObject>();
            public int Shown;
            public float Lift;                                                         // half the marker's height: it sits on the ground
        }

        readonly Dictionary<string, Pool> pools = new Dictionary<string, Pool>();
        readonly List<EntityKind> kinds = new List<EntityKind>();
        MaterialPropertyBlock block;

        public World World { get => world; set => world = value; }
        /// <summary>The tick the markers last showed (-1 before the first frame).</summary>
        public int ShownTick { get; private set; } = -1;

        /// <summary>Markers shown this frame for a kind ("carcass", "egg").</summary>
        public int Shown(string kind) => pools.TryGetValue(kind, out var p) ? p.Shown : 0;

        /// <summary>The marker shown for the i-th entity of a kind this frame (tests compare positions).</summary>
        public Transform Marker(string kind, int i) => pools[kind].Items[i].transform;

        void LateUpdate()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (world == null || !world.IsInitialized) return;
            ShownTick = world.Tick;
            kinds.Clear();
            kinds.AddRange(world.ServicesOf<EntityKind>());
            foreach (var p in pools.Values) p.Shown = 0;
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
            foreach (var p in pools.Values)
                for (int i = p.Shown; i < p.Items.Count; i++)
                    if (p.Items[i].activeSelf) p.Items[i].SetActive(false);
        }

        Pool PoolOf(string kind)
        {
            if (!pools.TryGetValue(kind, out var p)) pools.Add(kind, p = new Pool());
            return p;
        }

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
