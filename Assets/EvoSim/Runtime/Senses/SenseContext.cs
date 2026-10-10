using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a sense may read at a decision: the world state at the start of the tick (SENSE-04), through the same
    /// queries as actions, so hidden and killed animals are never reported (SENSE-06).
    /// </summary>
    public sealed class SenseContext
    {
        public World World { get; }
        public int Tick => World.Tick;
        public Ground Ground => World.Ground;

        /// <summary>Within this distance an item counts as "here" (the act phase's graze reach, 0.5 m by default).</summary>
        public float GrazeReach
        {
            get
            {
                var act = World.Phase<ActPhase>();
                return act != null ? act.GrazeReach : 0.5f;
            }
        }

        public SenseContext(World world) { World = world; }

        /// <summary>The rays of the batched senses, cast once per sense phase (SENSE-30).</summary>
        public RayBatch Rays { get; } = new RayBatch();

        /// <summary>Collects the rays of every batched sense for these animals and casts them in one batch (SENSE-30).</summary>
        public void CastRays(IReadOnlyList<Animal> animals)
        {
            Rays.Clear();
            for (int i = 0; i < animals.Count; i++)
            {
                var a = animals[i];
                var senses = a.Species.Senses;
                for (int k = 0; k < senses.Count; k++)
                {
                    if (!(senses[k] is BatchedSense b)) continue;
                    Rays.Begin(a, b);
                    b.RequestRays(a, Rays);
                    Rays.End();
                }
            }
            Rays.Execute();
        }

        public float Vision(Animal a) => World.Queries.Vision(a);

        /// <summary>The nearest visible animal of these species within radius (hidden and killed excluded).</summary>
        public AnimalHit? NearestAnimal(Animal a, IReadOnlyList<Species> among, float radius) => World.Queries.NearestAnimal(a, among, radius);

        /// <summary>The nearest visible animal of a set within radius.</summary>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, float radius, IReadOnlyList<Species> listed = null) =>
            World.Queries.NearestAnimal(a, World.Resolve(a.Species, set, listed), radius);

        public ResourceItem? NearestResource(Animal a, string layerName, float radius) => World.Queries.NearestResource(a, layerName, radius);
        public EntityHit<Carcass>? NearestCarcass(Animal a, float radius) => World.Queries.NearestCarcass(a, radius);
        public EntityHit<Egg>? NearestEgg(Animal a, Species of, float radius) => World.Queries.NearestEgg(a, of, radius);
        /// <summary>The nearest entity of a kind of your own within radius (see WorldQueries.NearestEntity).</summary>
        public EntityHit<T>? NearestEntity<T>(Animal a, float radius, System.Func<T, Animal, bool> filter = null) where T : Entity =>
            World.Queries.NearestEntity(a, radius, filter);
        public CoverHit? NearestCover(Animal a, float radius) => World.Queries.NearestCover(a, radius);
        public bool InCover(Animal a) => World.Queries.InCover(a);

        /// <summary>Whether an animal is ready to mate (REPRO-01).</summary>
        public bool IsReady(Animal other) => World.Queries.IsReady(other);

        /// <summary>
        /// The animal another animal's action was about at the start of this tick (its target last tick), or null
        /// (22 §13, the "chased" sense).
        /// </summary>
        public Animal CurrentTarget(Animal other)
        {
            if (other.TargetId < 0) return null;
            foreach (var s in World.AllSpecies)
                foreach (var a in s.Animals)
                    if (a.Id == other.TargetId) return a;
            return null;
        }
    }
}
