using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Draws a flat world's ground (02 §1) with its cover cells and food items (03 §1–2): one quad the size of the
    /// ground and a texture of one pixel per cell, repainted when a tick changed a cell. It only reads the World
    /// (SPACE-12): no collider, the Ignore Raycast layer, no random numbers.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundView : MonoBehaviour
    {
        [SerializeField, Tooltip("The World drawn; empty = the first World of the scene.")]
        World world;
        [SerializeField] Color soil = new Color(0.62f, 0.55f, 0.4f);
        [SerializeField] Color cover = new Color(0.1f, 0.32f, 0.12f);
        [SerializeField] Color food = new Color(0.55f, 0.95f, 0.3f);
        [SerializeField, Tooltip("Draw the cover cells.")]
        bool showCover = true;
        [SerializeField, Tooltip("Draw the food items.")]
        bool showFood = true;

        Texture2D texture;
        Color32[] pixels, painted;
        GameObject surface;
        Rect bounds;
        float pixel = 1f;
        int width, height, paintedTick = -1, coverCells = -1;
        bool[] coverMask;
        FoodGrid[] foods;
        Cover[] covers;

        public World World { get => world; set => world = value; }
        public Texture2D Texture => texture;
        /// <summary>Times the texture was uploaded: once per tick that changed a cell.</summary>
        public int Uploads { get; private set; }
        public int PaintedTick => paintedTick;
        public Color32 SoilColor => soil;
        public Color32 CoverColor => cover;
        public Color32 FoodColor => food;

        void LateUpdate()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (world == null || !world.IsInitialized || !(world.Ground is FlatGround)) return;
            if (surface == null) Create();
            if (world.Tick == paintedTick) return;
            paintedTick = world.Tick;
            if (!Repaint()) return;
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Uploads++;
        }

        /// <summary>The pixel of a world point (x right, z up in the texture), or -1 outside.</summary>
        public int PixelOf(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - bounds.xMin) / pixel), z = Mathf.FloorToInt((p.z - bounds.yMin) / pixel);
            return x < 0 || z < 0 || x >= width || z >= height ? -1 : z * width + x;
        }

        /// <summary>The colour now in the texture at a world point.</summary>
        public Color32 ColorAt(Vector3 p) => pixels[PixelOf(p)];

        void Create()
        {
            bounds = world.Ground.Bounds;
            foods = new System.Collections.Generic.List<FoodGrid>(world.ServicesOf<FoodGrid>()).ToArray();
            covers = new System.Collections.Generic.List<Cover>(world.ServicesOf<Cover>()).ToArray();
            pixel = float.PositiveInfinity;                                             // the finest grid of the layers drawn
            foreach (var f in foods) pixel = Mathf.Min(pixel, f.Grid.CellSize);
            foreach (var c in covers) if (c is CoverLayer layer) pixel = Mathf.Min(pixel, layer.Grid.CellSize);
            if (float.IsInfinity(pixel)) pixel = 1f;
            width = Mathf.Max(1, Mathf.CeilToInt(bounds.width / pixel - 1e-4f));
            height = Mathf.Max(1, Mathf.CeilToInt(bounds.height / pixel - 1e-4f));
            pixels = new Color32[width * height];
            painted = new Color32[width * height];
            coverMask = new bool[width * height];
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Ground (runtime)" };

            surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Ground (runtime)";
            surface.hideFlags = HideFlags.DontSave;
            DestroyImmediate(surface.GetComponent<Collider>());                       // nothing to hit (SPACE-12)
            surface.layer = ViewPool.IgnoreRaycastLayer;
            surface.transform.SetParent(transform, false);
            surface.transform.SetPositionAndRotation(new Vector3(bounds.center.x, -0.01f, bounds.center.y), Quaternion.Euler(90f, 0f, 0f));
            surface.transform.localScale = new Vector3(bounds.width, bounds.height, 1f);
            var r = surface.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var m = r.material;
            m.mainTexture = texture;
            m.color = Color.white;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
            paintedTick = -1;
        }

        /// <summary>Paints every pixel from the layers; returns whether one changed.</summary>
        bool Repaint()
        {
            int cells = 0;
            foreach (var c in covers) if (c is CoverLayer layer) cells += layer.CoverCells;
            if (cells != coverCells)                                                  // cover is set at the start; redo it if it changed
            {
                coverCells = cells;
                for (int i = 0; i < coverMask.Length; i++) coverMask[i] = InCover(Center(i));
            }
            bool changed = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = showCover && coverMask[i] ? (Color32)cover : (Color32)soil;
                if (showFood && HasFood(Center(i))) c = food;
                if (!changed && !Same(c, painted[i])) changed = true;
                pixels[i] = c;
            }
            if (changed) System.Array.Copy(pixels, painted, pixels.Length);
            return changed;
        }

        bool InCover(Vector3 p)
        {
            foreach (var c in covers) if (c.InCover(p)) return true;
            return false;
        }

        bool HasFood(Vector3 p)
        {
            foreach (var f in foods)
            {
                int cell = f.Grid.CellOf(p);
                if (cell >= 0 && f.HasItem(cell)) return true;
            }
            return false;
        }

        Vector3 Center(int i) => new Vector3(bounds.xMin + (i % width + 0.5f) * pixel, 0f, bounds.yMin + (i / width + 0.5f) * pixel);

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
            if (surface != null) Destroy(surface);
        }
    }
}
