using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Edible items spread over the ground, e.g. grass (03 §1). The layer's <see cref="EvoSim.Edible"/> says how
    /// it is eaten; diets name the layer by its GameObject's name.
    /// </summary>
    public abstract class ResourceLayer : WorldService
    {
        /// <summary>The name diets and senses use (ARCH-08).</summary>
        public string LayerName => ServiceName;

        /// <summary>The layer's edible component (SPEC-10), or null: then nothing can eat it.</summary>
        public Edible Edible
        {
            get
            {
                var e = GetComponent<Edible>();
                return e != null && e.enabled ? e : null;
            }
        }

        /// <summary>How many items the layer holds (ENV-01).</summary>
        public abstract int Count { get; }

        /// <summary>The nearest item within radius of a point (ENV-01, SPACE-07); false if none.</summary>
        public abstract bool Nearest(Vector3 p, float radius, out ResourceItem item);

        /// <summary>The nearest item within reach of a point (ENV-01); false if none.</summary>
        public virtual bool WithinReach(Vector3 p, float reach, out ResourceItem item) => Nearest(p, reach, out item);

        /// <summary>Removes the item at once; false if it was already gone (ENV-02).</summary>
        public abstract bool Consume(ResourceItem item);

        /// <summary>Regrowth, once per tick in the environment phase, with the layer's own stream (ENV-03).</summary>
        public virtual void Regrow(RandomStream rng) { }

        /// <summary>The name of the layer's regrowth stream (RAND-03): the layer's name.</summary>
        public virtual string StreamName => LayerName;
    }
}
