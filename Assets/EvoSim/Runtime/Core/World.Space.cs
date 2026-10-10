using UnityEngine;

namespace EvoSim
{
    // Spatial bookkeeping: the animal index and entity ids.
    public partial class World
    {
        SpatialIndex space;
        int nextEntityId;

        /// <summary>The spatial index of animals (SENSE-32), built on the ground's rectangle.</summary>
        public SpatialIndex Space
        {
            get
            {
                if (space != null) return space;
                space = new SpatialIndex(this, ground != null ? ground.Bounds : new Rect(-0.5f, -0.5f, 1f, 1f));
                space.Rebuild();
                return space;
            }
        }

        /// <summary>The queries senses and actions share (ACT-05, SENSE-06).</summary>
        public WorldQueries Queries { get; private set; }

        /// <summary>The next entity id (carcasses, eggs): unique in the run, never reused.</summary>
        public int NextEntityId() => nextEntityId++;

        partial void ResetSpace()
        {
            space = null;
            nextEntityId = 0;
            Queries = new WorldQueries(this);
        }

        /// <summary>The decisions of the current tick and the run's memo (08).</summary>
        public DecisionState Decisions { get; } = new DecisionState();

        /// <summary>Mutation statistics of the run (MUT-05).</summary>
        public MutationStats Mutations { get; } = new MutationStats();

        partial void ResetDecisions()
        {
            Decisions.Reset();
            Decisions.Memo.Capacity = memoCapacity;
            Mutations.Clear();
        }

        /// <summary>A new animal enters the spatial index.</summary>
        internal void OnAnimalAdded(Animal a) => space?.Added(a);
    }
}
