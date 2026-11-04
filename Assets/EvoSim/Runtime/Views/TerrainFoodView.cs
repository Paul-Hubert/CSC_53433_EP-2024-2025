using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Shows the food items (03 §2) on a terrain ground (02 §1): a transparent mesh draped over the terrain's heights, with a
    /// texture of one pixel per food cell that is the food colour where a cell holds an item and clear elsewhere. It only
    /// reads the World (SPACE-12): no collider, the Ignore Raycast layer, no random numbers.
    /// </summary>
    /// <remarks>
    /// One of the run views: put it on any GameObject of the scene, outside the World (the "Run views" object). It draws only
    /// on a <see cref="TerrainGround"/>; <see cref="GroundView"/> does the flat ground. Water cells and cells that are not
    /// walkable (steep mountains, trees) never show food, so the terrain and the water plane stay visible there. The mesh
    /// has one vertex per heightmap sample, lifted 8 cm above the terrain, so it follows the surface; it is made once at the
    /// start of a run, as the terrain does not change during a run.
    /// </remarks>
    [DisallowMultipleComponent]
    public class TerrainFoodView : MonoBehaviour
    {
        [SerializeField, Tooltip("The World drawn; empty = the first World of the scene.")]
        World world;
        [SerializeField] Color food = GroundView.DefaultFoodColor;          // a cell with a food item
        [SerializeField, Min(0f), Tooltip("How far the overlay lies above the terrain surface, in meters.")]
        float lift = 0.08f;

        Texture2D texture;
        Color32[] pixels, painted;                                         // the colours now; those last uploaded
        bool[] shown;                                                      // true for the pixels whose centre may show food
        GameObject surface;                                                // the mesh object that shows the texture
        /// <summary>The mesh over the terrain, destroyed with the component.</summary>
        Mesh mesh;
        Rect bounds;                                                       // the terrain's rectangle (x, z), in meters
        float pixel = 1f;                                                  // a pixel's side, in meters
        int width, height, paintedTick = -1;                               // texture size; tick last painted
        FoodGrid[] foods;                                                  // the food layers drawn

        /// <summary>The World drawn; when null, the first World of the scene is found on the next frame.</summary>
        public World World { get => world; set => world = value; }
        /// <summary>The food texture, one pixel per cell; null before the first frame of a run on a terrain.</summary>
        public Texture2D Texture => texture;
        /// <summary>Times the texture was uploaded: once per tick that changed a cell.</summary>
        public int Uploads { get; private set; }
        /// <summary>The tick the texture last showed (-1 before the first frame of a run).</summary>
        public int PaintedTick => paintedTick;
        /// <summary>The colour of a cell with a food item.</summary>
        public Color32 FoodColor => food;

        /// <summary>
        /// Makes the overlay on the first frame of a run on a terrain, then repaints the texture once per new tick and uploads
        /// it when a pixel changed. Called by Unity each frame, after the World's tick.
        /// </summary>
        void LateUpdate()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (world == null || !world.IsInitialized || !(world.Ground is TerrainGround)) return;
            if (surface == null) Create((TerrainGround)world.Ground);
            if (world.Tick == paintedTick) return;
            paintedTick = world.Tick;
            if (!Repaint()) return;
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Uploads++;
        }

        /// <summary>The pixel of a world point (x right, z up in the texture), or -1 outside.</summary>
        /// <param name="p">A point of the world, in meters; its height is ignored.</param>
        /// <returns>The pixel's index in the texture, row by row from the terrain's lower x and z corner; -1 outside.</returns>
        public int PixelOf(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - bounds.xMin) / pixel), z = Mathf.FloorToInt((p.z - bounds.yMin) / pixel);
            return x < 0 || z < 0 || x >= width || z >= height ? -1 : z * width + x;
        }

        /// <summary>The colour now in the texture at a world point (clear where there is no food).</summary>
        /// <param name="p">A point on the terrain, in meters; it must be inside the terrain and the texture made.</param>
        /// <returns>The pixel's colour: <see cref="FoodColor"/> or fully transparent.</returns>
        public Color32 ColorAt(Vector3 p) => pixels[PixelOf(p)];

        /// <summary>
        /// Makes the texture (one pixel per cell of the finest food grid), the walkable mask and the mesh over the terrain, on
        /// the Ignore Raycast layer and never saved with the scene.
        /// </summary>
        void Create(TerrainGround ground)
        {
            bounds = ground.Bounds;
            foods = new System.Collections.Generic.List<FoodGrid>(world.ServicesOf<FoodGrid>()).ToArray();
            pixel = float.PositiveInfinity;
            foreach (var f in foods) pixel = Mathf.Min(pixel, f.Grid.CellSize);
            if (float.IsInfinity(pixel)) pixel = 1f;
            width = Mathf.Max(1, Mathf.CeilToInt(bounds.width / pixel - 1e-4f));
            height = Mathf.Max(1, Mathf.CeilToInt(bounds.height / pixel - 1e-4f));
            pixels = new Color32[width * height];
            painted = new Color32[width * height];
            shown = new bool[width * height];
            for (int i = 0; i < shown.Length; i++) shown[i] = ground.IsWalkable(Center(i)); // not water, not mountain
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Food (runtime)" };

            surface = new GameObject("Food (runtime)") { hideFlags = HideFlags.DontSave, layer = ViewPool.IgnoreRaycastLayer };
            surface.transform.SetParent(transform, false);
            mesh = BuildMesh(ground);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = surface.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");                                  // unlit, alpha blended, two-sided
            var m = new Material(shader != null ? shader : Shader.Find("Unlit/Transparent")) { mainTexture = texture, color = Color.white, name = "Food (runtime)" };
            m.renderQueue = 2500;
            r.sharedMaterial = m;
            paintedTick = -1;
        }

        /// <summary>A grid of quads over the terrain, one vertex per heightmap sample, lifted by <c>lift</c>; uv 0 to 1 over the terrain.</summary>
        Mesh BuildMesh(TerrainGround ground)
        {
            int n = Mathf.Clamp(ground.Terrain.terrainData.heightmapResolution - 1, 1, 254); // 255 × 255 vertices fit 16-bit indices
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
                for (int x = 0; x <= n; x++)
                {
                    float u = (float)x / n, v = (float)z / n;
                    float wx = bounds.xMin + u * bounds.width, wz = bounds.yMin + v * bounds.height;
                    vertices[z * (n + 1) + x] = new Vector3(wx, ground.Height(new Vector3(wx, 0f, wz)) + lift, wz);
                    uvs[z * (n + 1) + x] = new Vector2(u, v);
                }
            int t = 0;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int a = z * (n + 1) + x, b = a + 1, c = a + n + 1, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            var mesh = new Mesh { name = "Food (runtime)" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Paints every pixel: the food colour where its cell holds an item and it may show food, else clear; returns whether one changed.</summary>
        bool Repaint()
        {
            bool changed = false;
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = shown[i] && HasFood(Center(i)) ? (Color32)food : clear;
                if (!changed && !Same(c, painted[i])) changed = true;
                pixels[i] = c;
            }
            if (changed) System.Array.Copy(pixels, painted, pixels.Length);
            return changed;
        }

        /// <summary>True when a point's cell holds a food item in any food layer.</summary>
        bool HasFood(Vector3 p)
        {
            foreach (var f in foods)
            {
                int cell = f.Grid.CellOf(p);
                if (cell >= 0 && f.HasItem(cell)) return true;
            }
            return false;
        }

        /// <summary>The world point at the centre of pixel i, at height 0.</summary>
        Vector3 Center(int i) => new Vector3(bounds.xMin + (i % width + 0.5f) * pixel, 0f, bounds.yMin + (i / width + 0.5f) * pixel);

        /// <summary>True when two colours are equal in every channel.</summary>
        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        /// <summary>Destroys the texture, the mesh and the overlay object with the component. Called by Unity.</summary>
        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
            if (mesh != null) Destroy(mesh);
            if (surface != null) Destroy(surface);
        }
    }
}
