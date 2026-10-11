using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Inactive views of one species, reused (20 §6): a new animal takes one, a dead one gives it back. Views have no
    /// active collider and sit on the "Ignore Raycast" layer, which every sense ray leaves out, so what the animals sense
    /// never depends on rendering (SPACE-12, RAND-11).
    /// </summary>
    /// <remarks>
    /// The World makes one pool per species with a <see cref="Body"/> when the run starts, under a "Views (runtime)" object
    /// that is never saved, and calls <see cref="Take"/> and <see cref="Return"/> from its LateUpdate. Main thread only.
    /// </remarks>
    public sealed class ViewPool
    {
        /// <summary>The views' layer ("Ignore Raycast"): RayBatch removes it from every mask.</summary>
        public const int IgnoreRaycastLayer = 2;
        static readonly int ColorId = Shader.PropertyToID("_Color");          // the built-in pipeline's main colour
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");  // the same in URP and HDRP

        readonly Body body;                                                    // the species' look
        readonly Transform container;                                          // the parent of every view
        readonly Stack<AnimalView> free = new Stack<AnimalView>();

        /// <summary>Views made so far, taken or free.</summary>
        public int Created { get; private set; }
        /// <summary>Views waiting in the pool.</summary>
        public int Free => free.Count;

        /// <summary>Makes a pool and its first views.</summary>
        /// <param name="body">The species' Body: its prefab, or the colour and size of the placeholder capsule.</param>
        /// <param name="container">The parent of the views.</param>
        /// <param name="prewarm">Views made now, inactive, so the first births don't instantiate.</param>
        public ViewPool(Body body, Transform container, int prewarm)
        {
            this.body = body;
            this.container = container;
            for (int i = 0; i < prewarm; i++) free.Push(Make());
        }

        /// <summary>Gives a view to an animal: a free one, or a new one when none is free.</summary>
        /// <param name="animalId">The animal's id.</param>
        /// <returns>The view, active, with <see cref="AnimalView.AnimalId"/> set.</returns>
        public AnimalView Take(int animalId)
        {
            var v = free.Count > 0 ? free.Pop() : Make();
            v.AnimalId = animalId;
            v.gameObject.SetActive(true);
            return v;
        }

        /// <summary>Takes a view back when its animal is gone: it is hidden until another animal takes it.</summary>
        /// <param name="v">A view this pool gave.</param>
        public void Return(AnimalView v)
        {
            v.AnimalId = -1;
            v.gameObject.SetActive(false);
            free.Push(v);
        }

        /// <summary>
        /// Makes one inactive view: the prefab scaled by the Body's size, or a capsule of its colour; every part on the
        /// Ignore Raycast layer, every collider off, and an <see cref="AnimalView"/> on the root when the prefab has none.
        /// </summary>
        AnimalView Make()
        {
            GameObject go;
            if (body.Prefab != null)
            {
                go = Object.Instantiate(body.Prefab, container);
                go.transform.localScale *= body.Size;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(container, false);
                go.transform.localScale = new Vector3(body.Size, body.Size * 0.5f, body.Size);   // the capsule is 2 high
                var block = new MaterialPropertyBlock();
                block.SetColor(ColorId, body.Color);
                block.SetColor(BaseColorId, body.Color);
                go.GetComponent<Renderer>().SetPropertyBlock(block);
            }
            go.name = body.Species != null ? body.Species.Id : "view";
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IgnoreRaycastLayer;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;   // views are never hit (SPACE-12)
            if (!go.TryGetComponent<AnimalView>(out var view)) view = go.AddComponent<AnimalView>();
            go.SetActive(false);
            Created++;
            return view;
        }
    }
}
