using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A camera for watching a run: it frames the world at the start; WASD or the arrows pan, the wheel zooms, the right
    /// mouse button (or Q and E) orbits, the middle button drags, F frames the world again. A left click on an animal
    /// follows it (Esc stops). Picking is done on screen, from the views' positions: views have no colliders (SPACE-12),
    /// and the camera only reads the World.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(1000)]                                                          // after the World placed the views
    public class WorldCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("The World watched; empty = the first World of the scene.")]
        World world;
        [SerializeField, Range(10f, 89f), Tooltip("Angle below the horizon, in degrees.")]
        float pitch = 55f;
        [SerializeField, Tooltip("Angle around the vertical, in degrees (0 = looking along +z).")]
        float yaw;
        [SerializeField, Min(0.1f), Tooltip("Pan speed, in camera distances per second.")]
        float panSpeed = 0.8f;
        [SerializeField, Min(1f), Tooltip("Orbit speed, in degrees per second (keys) or per 100 pixels (mouse).")]
        float orbitSpeed = 90f;
        [SerializeField, Min(1f), Tooltip("Picking radius around an animal, in pixels at 1080 lines.")]
        float pickPixels = 24f;
        [SerializeField, Min(2f), Tooltip("Distance of the camera from an animal it starts following, in meters.")]
        float followDistance = 15f;

        Camera cam;
        Vector3 focus;
        float distance = 50f;
        bool framed;
        Vector3 pressedAt;

        public World World { get => world; set => world = value; }
        /// <summary>The animal followed, or null.</summary>
        public Animal Followed { get; private set; }
        /// <summary>The last followed animal that left the world (dead or gone), shown by the overlay.</summary>
        public Animal Lost { get; private set; }
        public Vector3 Focus => focus;
        public float Distance => distance;
        /// <summary>A screen point the overlay covers: clicks there don't pick (set by <see cref="RunOverlay"/>).</summary>
        public System.Func<Vector2, bool> Blocks { get; set; }

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (world == null || !world.IsInitialized || world.Ground == null) return;
            if (!framed) Frame();
            HandleInput(Time.unscaledDeltaTime);
            if (Followed != null)
            {
                var v = world.ViewOf(Followed);
                if (Followed.IsGone || v == null) { Lost = Followed; Followed = null; }
                else focus = Vector3.Lerp(focus, v.transform.position, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            }
            Place();
        }

        /// <summary>Looks at the whole world from the current angles.</summary>
        public void Frame()
        {
            var b = world.Ground.Bounds;
            focus = new Vector3(b.center.x, 0f, b.center.y);
            float near = 1f, far = 4f * Mathf.Max(b.width, b.height) + 10f;              // the nearest distance that shows the four corners
            for (int i = 0; i < 30; i++)
            {
                distance = 0.5f * (near + far);
                Place();
                if (Shows(b, 0.03f)) far = distance; else near = distance;
            }
            distance = far;
            framed = true;
            Place();
        }

        bool Shows(Rect b, float margin)
        {
            foreach (var c in new[] { new Vector3(b.xMin, 0f, b.yMin), new Vector3(b.xMax, 0f, b.yMin), new Vector3(b.xMin, 0f, b.yMax), new Vector3(b.xMax, 0f, b.yMax) })
            {
                var v = cam.WorldToViewportPoint(c);
                if (v.z <= 0f || v.x < margin || v.x > 1f - margin || v.y < margin || v.y > 1f - margin) return false;
            }
            return true;
        }

        /// <summary>Follows an animal (null stops), coming as close as <see cref="followDistance"/>.</summary>
        public void Follow(Animal a)
        {
            Followed = a;
            if (a == null) return;
            Lost = null;
            distance = Mathf.Min(distance, followDistance);
        }

        void Place()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, distance * 3f);
        }

        void HandleInput(float dt)
        {
            if (!Application.isFocused) return;
            var flat = Quaternion.Euler(0f, yaw, 0f);
            var move = new Vector3((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                                   0f,
                                   (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            if (move != Vector3.zero) { Followed = null; focus += flat * move * (panSpeed * distance * dt); }
            if (Input.GetKey(KeyCode.Q)) yaw += orbitSpeed * dt;
            if (Input.GetKey(KeyCode.E)) yaw -= orbitSpeed * dt;
            if (Input.GetKeyDown(KeyCode.F)) { Followed = null; Frame(); }
            if (Input.GetKeyDown(KeyCode.Escape)) Followed = null;

            float wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f && !Blocked(Input.mousePosition)) distance = Mathf.Clamp(distance * Mathf.Pow(0.88f, wheel), 3f, 2000f);

            var delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            if (Input.GetMouseButton(1))
            {
                yaw += delta.x * orbitSpeed * 0.1f;
                pitch = Mathf.Clamp(pitch - delta.y * orbitSpeed * 0.1f, 10f, 89f);
            }
            if (Input.GetMouseButton(2))
            {
                Followed = null;
                focus -= flat * new Vector3(delta.x, 0f, delta.y) * (distance * 0.02f);
            }
            if (Input.GetMouseButtonDown(0)) pressedAt = Input.mousePosition;
            if (Input.GetMouseButtonUp(0) && (Input.mousePosition - pressedAt).sqrMagnitude < 25f && !Blocked(Input.mousePosition))
            {
                var picked = Pick(Input.mousePosition);
                if (picked != null) Follow(picked);
            }
        }

        bool Blocked(Vector2 p) => Blocks != null && Blocks(p);

        /// <summary>The living animal whose view is nearest to a screen point, within the picking radius; null if none.</summary>
        public Animal Pick(Vector2 screen)
        {
            Animal best = null;
            float bestD = pickPixels * Screen.height / 1080f;
            bestD *= bestD;
            foreach (var s in world.AllSpecies)
                foreach (var a in s.Animals)
                {
                    var v = world.ViewOf(a);
                    if (v == null || a.IsGone) continue;
                    var p = cam.WorldToScreenPoint(v.transform.position + Vector3.up * 0.3f);
                    if (p.z <= 0f) continue;
                    float d = ((Vector2)p - screen).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = a; }
                }
            return best;
        }
    }
}
