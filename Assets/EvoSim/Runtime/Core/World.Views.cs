using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    // The World's views (20 §6): pools from each species' Body, placed in LateUpdate; they never write to the run (SPACE-12).
    public partial class World
    {
        [Header("Views (20 §6)")]
        [SerializeField, Tooltip("Draw the animals. Views only read the simulation (SPACE-12), so this never changes a run.")]
        bool showViews = true;
        [SerializeField, Tooltip("Place each view between its animal's previous and current tick positions.")]
        bool interpolateViews = true;
        [SerializeField, Tooltip("Also make views outside Play mode (tests and batch runs leave this off).")]
        bool viewsOutsidePlayMode;

        Transform viewContainer;                                                      // the hidden "Views (runtime)" object, or null
        readonly Dictionary<Species, ViewPool> pools = new Dictionary<Species, ViewPool>();     // per species; null without a Body
        readonly Dictionary<Animal, AnimalView> viewOf = new Dictionary<Animal, AnimalView>();  // the view each shown animal holds
        readonly List<Animal> viewed = new List<Animal>();                            // the shown animals, in a fixed order

        /// <summary>True to draw the animals. Views only read the run (SPACE-12), so this never changes it.</summary>
        public bool ShowViews { get => showViews; set => showViews = value; }
        /// <summary>True to place each view between its animal's previous and current positions, for smooth motion between ticks.</summary>
        public bool InterpolateViews { get => interpolateViews; set => interpolateViews = value; }
        /// <summary>True to make views outside Play mode too (tests and batch runs leave it off). Read at Initialize.</summary>
        public bool ViewsOutsidePlayMode { get => viewsOutsidePlayMode; set => viewsOutsidePlayMode = value; }
        /// <summary>The tick the views last showed (-1 before the first sync), so checks compare like with like.</summary>
        public int ViewsTick { get; private set; } = -1;
        /// <summary>Views on screen now.</summary>
        public int ViewCount => viewed.Count;
        /// <summary>The view that shows an animal now.</summary>
        /// <param name="a">The animal, or null.</param>
        /// <returns>Its view, or null when it has none (no Body, views off, or gone).</returns>
        public AnimalView ViewOf(Animal a) => a != null && viewOf.TryGetValue(a, out var v) ? v : null;

        /// <summary>
        /// The fraction of the next tick already elapsed (0 just after a tick, 1 when the next is due): the FixedUpdate
        /// clock, or the RealTime carry. 1 while waiting or paused: the views rest where the last tick left them.
        /// </summary>
        public float InterpolationFactor
        {
            get
            {
                if (!interpolateViews || waiting || blocked) return 1f;
                if (runSpeed == RunSpeed.RealTime) return runMode == RunState.Running ? Mathf.Clamp01(realTimeCarry) : 1f;
                if (runSpeed != RunSpeed.PerFixedUpdate || Time.fixedDeltaTime <= 0f) return 1f;
                return Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            }
        }

        /// <summary>Step 9: removes old views, then makes the hidden container and a pool per species (Play mode, or when allowed
        /// outside it).</summary>
        partial void CreateViews()
        {
            DestroyViews();
            if (!Application.isPlaying && !viewsOutsidePlayMode) return;
            var go = new GameObject("Views (runtime)") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            viewContainer = go.transform;
            foreach (var s in species) PoolOf(s);
        }

        /// <summary>The species' view pool, made on first use from its Body; null for a species without a Body.</summary>
        ViewPool PoolOf(Species s)
        {
            if (pools.TryGetValue(s, out var pool)) return pool;
            var body = s.Module<Body>();
            pool = body != null ? new ViewPool(body, viewContainer, body.Prewarm) : null;
            pools[s] = pool;                                          // null: a species without a body runs unseen
            return pool;
        }

        /// <summary>Every frame: returns the views of gone animals to their pools, gives new animals a view, and places them all.</summary>
        partial void SyncViews()
        {
            if (viewContainer == null) return;
            ViewsTick = Tick;
            for (int i = viewed.Count - 1; i >= 0; i--)
            {
                var a = viewed[i];
                if (showViews && !a.IsGone) continue;
                pools[a.Species]?.Return(viewOf[a]);
                viewOf.Remove(a);
                viewed[i] = viewed[viewed.Count - 1];
                viewed.RemoveAt(viewed.Count - 1);
            }
            if (!showViews) return;
            float f = InterpolationFactor;
            foreach (var s in species)
            {
                var pool = PoolOf(s);
                if (pool == null) continue;
                foreach (var a in s.Animals)
                {
                    if (a.IsGone) continue;
                    if (!viewOf.TryGetValue(a, out var view))
                    {
                        view = pool.Take(a.Id);
                        viewOf[a] = view;
                        viewed.Add(a);
                    }
                    Place(view, a, f);
                }
            }
        }

        /// <summary>
        /// Puts a view between its animal's previous and current positions (fraction f), on the ground, facing the way
        /// it moved, or its heading when it stood still.
        /// </summary>
        void Place(AnimalView view, Animal a, float f)
        {
            var p = Vector3.Lerp(a.PreviousPosition, a.Position, f);
            if (ground != null) p.y = ground.Height(p);
            var d = a.Position - a.PreviousPosition;
            d.y = 0f;
            float yaw = d.sqrMagnitude > 1e-6f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : a.Heading;
            view.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            view.OnShow(a);
        }

        /// <summary>Forgets every pool and view, and destroys the container with its views.</summary>
        void DestroyViews()
        {
            pools.Clear();
            viewOf.Clear();
            viewed.Clear();
            if (viewContainer == null) return;
            if (Application.isPlaying) Destroy(viewContainer.gameObject);
            else DestroyImmediate(viewContainer.gameObject);
            viewContainer = null;
        }
    }
}
