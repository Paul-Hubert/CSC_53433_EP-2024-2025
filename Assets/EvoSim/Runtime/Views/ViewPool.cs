using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Inactive views of one species, reused (20 §6): a new animal takes one, a dead one gives it back. Views sit on the
    /// "Ignore Raycast" layer, so the senses' rays never hit them (SPACE-12).
    /// </summary>
    public sealed class ViewPool
    {
        const int IgnoreRaycastLayer = 2;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        readonly Body body;
        readonly Transform container;
        readonly Stack<AnimalView> free = new Stack<AnimalView>();

        public int Created { get; private set; }
        public int Free => free.Count;

        public ViewPool(Body body, Transform container, int prewarm)
        {
            this.body = body;
            this.container = container;
            for (int i = 0; i < prewarm; i++) free.Push(Make());
        }

        public AnimalView Take(int animalId)
        {
            var v = free.Count > 0 ? free.Pop() : Make();
            v.AnimalId = animalId;
            v.gameObject.SetActive(true);
            return v;
        }

        public void Return(AnimalView v)
        {
            v.AnimalId = 0;
            v.gameObject.SetActive(false);
            free.Push(v);
        }

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
                go.transform.localScale = new Vector3(body.Size, body.Size * 0.5f, body.Size);
                var block = new MaterialPropertyBlock();
                block.SetColor(ColorId, body.Color);
                block.SetColor(BaseColorId, body.Color);
                go.GetComponent<Renderer>().SetPropertyBlock(block);
            }
            go.name = body.Species != null ? body.Species.Id : "view";
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IgnoreRaycastLayer;
            var view = go.GetComponent<AnimalView>() ?? go.AddComponent<AnimalView>();
            go.SetActive(false);
            Created++;
            return view;
        }
    }
}
