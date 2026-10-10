using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>The World's views (20 §6): pools from each species' Body, placed in LateUpdate; they never write to the run (SPACE-12).</summary>
    public partial class World
    {
        [Header("Views (20 §6)")]
        [SerializeField, Tooltip("Draw the animals. Views only read the simulation (SPACE-12), so this never changes a run.")]
        bool showViews = true;
        [SerializeField, Tooltip("Place each view between its animal's previous and current tick positions.")]
        bool interpolateViews = true;
        [SerializeField, Tooltip("Also make views outside Play mode (tests and batch runs leave this off).")]
        bool viewsOutsidePlayMode;

        Transform viewContainer;
        readonly Dictionary<Species, ViewPool> pools = new Dictionary<Species, ViewPool>();
        readonly Dictionary<Animal, AnimalView> viewOf = new Dictionary<Animal, AnimalView>();
        readonly List<Animal> viewed = new List<Animal>();

        public bool ShowViews { get => showViews; set => showViews = value; }
        public bool InterpolateViews { get => interpolateViews; set => interpolateViews = value; }
        public bool ViewsOutsidePlayMode { get => viewsOutsidePlayMode; set => viewsOutsidePlayMode = value; }
        /// <summary>The tick the views last showed (-1 before the first sync), so checks compare like with like.</summary>
        public int ViewsTick { get; private set; } = -1;
        /// <summary>Views on screen now.</summary>
        public int ViewCount => viewed.Count;
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

        partial void CreateViews()
        {
            DestroyViews();
            if (!Application.isPlaying && !viewsOutsidePlayMode) return;
            var go = new GameObject("Views (runtime)") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            viewContainer = go.transform;
            foreach (var s in species) PoolOf(s);
        }

        ViewPool PoolOf(Species s)
        {
            if (pools.TryGetValue(s, out var pool)) return pool;
            var body = s.Module<Body>();
            pool = body != null ? new ViewPool(body, viewContainer, body.Prewarm) : null;
            pools[s] = pool;                                          // null: a species without a body runs unseen
            return pool;
        }

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
