using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A camera for watching a run: it frames the world at the start; WASD or the arrows pan, the wheel zooms, the right
    /// mouse button (or Q and E) orbits, the middle button drags, F frames the world again. A left click on an animal
    /// follows it (Esc stops). Picking is done on screen, from the views' positions: views have no colliders (SPACE-12),
    /// and the camera only reads the World.
    /// </summary>
    /// <remarks>
    /// <para>Put it on the scene's camera (it needs a Camera on the same GameObject). It orbits a focus point on the ground,
    /// at <see cref="Distance"/> meters, looking down at the Inspector's pitch. Controls, read only while the window has
    /// the focus, in real time, so they work while the run is paused or waits:</para>
    /// <list type="bullet">
    /// <item><description>W A S D or the arrow keys: pan, at <c>panSpeed</c> camera distances per second; stops
    /// following.</description></item>
    /// <item><description>Mouse wheel: zoom, from 3 to 2 000 m; not over the overlay's panels.</description></item>
    /// <item><description>Right mouse button held: orbit (mouse left and right) and tilt (mouse up and down, 10° to
    /// 89°).</description></item>
    /// <item><description>Q and E: orbit left and right, at <c>orbitSpeed</c> degrees per second.</description></item>
    /// <item><description>Middle mouse button held: drag the ground; stops following.</description></item>
    /// <item><description>F: frame the whole world again; stops following.</description></item>
    /// <item><description>Left click (press and release within 5 pixels) on an animal: follow it; Esc: stop following.</description></item>
    /// </list>
    /// <para>The <see cref="RunOverlay"/> shows the followed animal and keeps clicks on its panels from picking.</para>
    /// </remarks>
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

        Camera cam;                                                            // the Camera on this GameObject
        Vector3 focus;
        float distance = 50f;
        bool framed;                                                           // the world was framed once this run
        Vector3 pressedAt;                                                     // where the left button went down, in pixels

        /// <summary>The World watched; when null, the first World of the scene is found on the next frame.</summary>
        public World World { get => world; set => world = value; }
        /// <summary>The animal followed, or null.</summary>
        public Animal Followed { get; private set; }
        /// <summary>The last followed animal that left the world (dead or gone), shown by the overlay.</summary>
        public Animal Lost { get; private set; }
        /// <summary>The point on the ground the camera looks at and orbits, in world coordinates.</summary>
        public Vector3 Focus => focus;
        /// <summary>The camera's distance from <see cref="Focus"/>, in meters.</summary>
        public float Distance => distance;
        /// <summary>A screen point the overlay covers: clicks there don't pick (set by <see cref="RunOverlay"/>).</summary>
        public System.Func<Vector2, bool> Blocks { get; set; }

        /// <summary>Finds the Camera on this GameObject. Called by Unity once.</summary>
        void Awake() => cam = GetComponent<Camera>();

        /// <summary>
        /// Frames the world on the first frame of a run, reads the keys and the mouse, eases the focus toward the followed
        /// animal's view, then places the camera. Called by Unity each frame, after the World placed the views.
        /// </summary>
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
                // An ease that takes the same time at any frame rate.
                else focus = Vector3.Lerp(focus, v.transform.position, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            }
            Place();
        }

        /// <summary>Looks at the whole world from the current angles.</summary>
        /// <remarks>Call it once the World is initialised: it reads the ground's bounds.</remarks>
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

        /// <summary>
        /// True when the camera, as placed, sees the four corners of a ground rectangle, inside a margin of the viewport.
        /// </summary>
        bool Shows(Rect b, float margin)
        {
            foreach (var c in new[] { new Vector3(b.xMin, 0f, b.yMin), new Vector3(b.xMax, 0f, b.yMin), new Vector3(b.xMin, 0f, b.yMax), new Vector3(b.xMax, 0f, b.yMax) })
            {
                var v = cam.WorldToViewportPoint(c);
                if (v.z <= 0f || v.x < margin || v.x > 1f - margin || v.y < margin || v.y > 1f - margin) return false;
            }
            return true;
        }

        /// <summary>Follows an animal (null stops), coming as close as the <c>followDistance</c> field says.</summary>
        /// <param name="a">The animal to follow, or null to stop following.</param>
        public void Follow(Animal a)
        {
            Followed = a;
            if (a == null) return;
            Lost = null;
            distance = Mathf.Min(distance, followDistance);
        }

        /// <summary>
        /// Puts the camera at its distance from the focus, at the current pitch and yaw, and pushes its far plane out if needed.
        /// </summary>
        void Place()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, distance * 3f);
        }

        /// <summary>
        /// Applies the keys and the mouse of this frame (the controls in the class remarks); does nothing while the window
        /// doesn't have the focus.
        /// </summary>
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
            // A click, not a drag: the button came up within 5 pixels of where it went down.
            if (Input.GetMouseButtonUp(0) && (Input.mousePosition - pressedAt).sqrMagnitude < 25f && !Blocked(Input.mousePosition))
            {
                var picked = Pick(Input.mousePosition);
                if (picked != null) Follow(picked);
            }
        }

        /// <summary>True when the overlay covers a screen point.</summary>
        bool Blocked(Vector2 p) => Blocks != null && Blocks(p);

        /// <summary>The living animal whose view is nearest to a screen point, within the picking radius; null if none.</summary>
        /// <remarks>It compares screen positions of the views (a little above their feet), so views need no collider.</remarks>
        /// <param name="screen">A point on the screen, in pixels, from the bottom left, as <c>Input.mousePosition</c> gives it.</param>
        /// <returns>The animal, or null when no living animal's view is within <c>pickPixels</c> (scaled to the window's height).</returns>
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
