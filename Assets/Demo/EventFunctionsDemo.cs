using UnityEngine;

namespace Examples
{
    // Shows Unity's built-in event functions in the order they normally run,
    // and caches components with GetComponent / GetComponentsInChildren.
    [RequireComponent(typeof(Rigidbody))]
    public class EventFunctionsDemo : MonoBehaviour
    {
        // ---------- Public fields (shown in the Inspector) ----------
        [Header("Settings")]
        public Material highlightMaterial;
        public Color tint = Color.white;
        [Range(0f, 10f)] public float spinSpeed = 2f;
        public bool logEveryFrame = false;

        // Private but still shown in the Inspector.
        [SerializeField] private int hitCount;

        // ---------- Cached components (private) ----------
        // Real Unity components
        private Rigidbody body;
        private Collider ownCollider;
        private Renderer ownRenderer;
        private Renderer[] childRenderers;
        private Light[] childLights;

        // Our own (fake) components
        private DemoHealth health;
        private DemoWeapon[] weapons;

        private Material originalMaterial;

        // ---------- Editor-only ----------

        // Called when the component is added or "Reset" is chosen in the Inspector menu.
        private void Reset()
        {
            spinSpeed = 2f;
            tint = Color.white;
            Debug.Log("[Demo] Reset");
        }

        // Called when a value changes in the Inspector (also on load).
        private void OnValidate()
        {
            hitCount = Mathf.Max(0, hitCount);
        }

        // ---------- Initialisation ----------

        // First call, even if the component is disabled. Cache components here.
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ownCollider = GetComponent<Collider>();
            ownRenderer = GetComponent<Renderer>();
            health = GetComponent<DemoHealth>();

            // includeInactive: true also finds children whose GameObject is disabled.
            childRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            childLights = GetComponentsInChildren<Light>();
            weapons = GetComponentsInChildren<DemoWeapon>();

            if (ownRenderer != null) originalMaterial = ownRenderer.sharedMaterial;

            Debug.Log($"[Demo] Awake: collider={ownCollider != null}, health={health != null}, " +
                      $"{childRenderers.Length} renderers, {childLights.Length} lights, {weapons.Length} weapons");
        }

        // Each time the component or its GameObject becomes enabled.
        private void OnEnable() => Debug.Log("[Demo] OnEnable");

        // Once, before the first Update, after every Awake has run.
        private void Start()
        {
            body.useGravity = false;
            foreach (var r in childRenderers)
                r.material.color = tint; // .material makes a per-object copy
            Debug.Log("[Demo] Start");
        }

        // ---------- Loop ----------

        // Fixed timestep; do physics here.
        private void FixedUpdate()
        {
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, spinSpeed, 0f));
        }

        // Once per frame; input and game logic.
        private void Update()
        {
            if (logEveryFrame) Debug.Log($"[Demo] Update {Time.frameCount}");

            if (Input.GetKeyDown(KeyCode.Space) && health != null)
            {
                float total = 0f;
                foreach (var w in weapons) total += w.damage;
                health.Damage(total);
                Debug.Log($"[Demo] Took {total} damage, health = {health.current}");
            }
        }

        // After every Update; good for cameras and follow logic.
        private void LateUpdate()
        {
            if (logEveryFrame) Debug.Log("[Demo] LateUpdate");
        }

        // ---------- Physics callbacks ----------

        private void OnCollisionEnter(Collision c)
        {
            hitCount++;
            Debug.Log($"[Demo] OnCollisionEnter with {c.gameObject.name}");
        }
        private void OnCollisionStay(Collision c) { }
        private void OnCollisionExit(Collision c) => Debug.Log($"[Demo] OnCollisionExit {c.gameObject.name}");

        private void OnTriggerEnter(Collider other)
        {
            if (ownRenderer != null && highlightMaterial != null) ownRenderer.sharedMaterial = highlightMaterial;
            Debug.Log($"[Demo] OnTriggerEnter {other.name}");
        }
        private void OnTriggerStay(Collider other) { }
        private void OnTriggerExit(Collider other)
        {
            if (ownRenderer != null) ownRenderer.sharedMaterial = originalMaterial;
            Debug.Log($"[Demo] OnTriggerExit {other.name}");
        }

        // ---------- Mouse (needs a Collider) ----------

        private void OnMouseEnter() => Debug.Log("[Demo] OnMouseEnter");
        private void OnMouseExit() => Debug.Log("[Demo] OnMouseExit");
        private void OnMouseDown() => Debug.Log("[Demo] OnMouseDown");

        // ---------- Rendering ----------

        private void OnBecameVisible() => Debug.Log("[Demo] OnBecameVisible");
        private void OnBecameInvisible() => Debug.Log("[Demo] OnBecameInvisible");

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Hits: {hitCount}  Health: {(health ? health.current : 0f)}");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = tint;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }

        // ---------- Hierarchy ----------

        private void OnTransformParentChanged() => Debug.Log("[Demo] OnTransformParentChanged");

        // Children changed, so refresh the cached child arrays.
        private void OnTransformChildrenChanged()
        {
            childRenderers = GetComponentsInChildren<Renderer>(true);
            childLights = GetComponentsInChildren<Light>();
            weapons = GetComponentsInChildren<DemoWeapon>();
        }

        // ---------- Application ----------

        private void OnApplicationFocus(bool hasFocus) => Debug.Log($"[Demo] OnApplicationFocus {hasFocus}");
        private void OnApplicationPause(bool paused) => Debug.Log($"[Demo] OnApplicationPause {paused}");
        private void OnApplicationQuit() => Debug.Log("[Demo] OnApplicationQuit");

        // ---------- Teardown ----------

        private void OnDisable() => Debug.Log("[Demo] OnDisable");

        private void OnDestroy() => Debug.Log("[Demo] OnDestroy");
    }
}
