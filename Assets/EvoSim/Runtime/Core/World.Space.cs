using UnityEngine;

namespace EvoSim
{
    // Spatial bookkeeping: the animal index and entity ids.
    public partial class World
    {
        SpatialIndex space;
        int nextEntityId;

        /// <summary>The spatial index of animals (SENSE-32), built on the ground's rectangle.</summary>
        public SpatialIndex Space => space ??= new SpatialIndex(this, ground != null ? ground.Bounds : new Rect(-0.5f, -0.5f, 1f, 1f));

        /// <summary>The next entity id (carcasses, eggs): unique in the run, never reused.</summary>
        public int NextEntityId() => nextEntityId++;

        partial void ResetSpace()
        {
            space = null;
            nextEntityId = 0;
        }

        /// <summary>A new animal enters the spatial index.</summary>
        internal void OnAnimalAdded(Animal a) => space?.Added(a);
    }
}
