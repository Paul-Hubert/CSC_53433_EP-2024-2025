using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a sense may read at a decision: the world state at the start of the tick (SENSE-04), through the same
    /// queries as actions, so hidden and killed animals are never reported (SENSE-06).
    /// </summary>
    /// <remarks>
    /// <para>The sense phase makes one context per world and passes it to every <see cref="Sense.Read"/> of the tick; the
    /// ask-brains phase reuses it for attachments. It runs first in the tick, so what the context reports is the state
    /// before anyone acts.</para>
    /// <para>The queries wrap <see cref="WorldQueries"/>: never the animal itself, never a killed animal, never one hidden
    /// in cover from the asking species, diets checked, ties broken by id (SPACE-08). Pass the animal's vision as the
    /// radius (<see cref="Vision"/>) so the sense sees what its actions can reach (ACT-05).</para>
    /// </remarks>
    public sealed class SenseContext
    {
        /// <summary>The world being sensed: its services, species and settings.</summary>
        public World World { get; }

        /// <summary>The current tick, counted from 0.</summary>
        public int Tick => World.Tick;

        /// <summary>The world's ground (terrain heights, walkability, water…), or null when the world has none.</summary>
        public Ground Ground => World.Ground;

        /// <summary>
        /// Within this distance, in meters, an item counts as "here": the act phase's graze reach, 0.5 m without an act
        /// phase. The food sense says "here" within it, because eating needs no walk then.
        /// </summary>
        public float GrazeReach
        {
            get
            {
                var act = World.Phase<ActPhase>();
                return act != null ? act.GrazeReach : 0.5f;
            }
        }

        /// <summary>Makes a context for a world, with an empty ray batch. The sense phase makes one in its Initialize.</summary>
        /// <param name="world">The world to sense.</param>
        public SenseContext(World world) { World = world; }

        /// <summary>
        /// The rays of the batched senses, cast once per sense phase by <see cref="CastRays"/> (SENSE-30). A
        /// <see cref="BatchedSense"/> reads its results with <see cref="RayBatch.Hit"/>.
        /// </summary>
        public RayBatch Rays { get; } = new RayBatch();

        /// <summary>Collects the rays of every batched sense for these animals and casts them in one batch (SENSE-30).</summary>
        /// <param name="animals">The animals that decide this tick, of every species.</param>
        /// <remarks>
        /// Called by the sense phase before any sense reads. It clears the previous rays, calls
        /// <see cref="BatchedSense.RequestRays"/> for each animal and each of its batched senses, then casts them all. A
        /// context whose rays were never cast for an animal reports each of its rays as a miss.
        /// </remarks>
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

        /// <summary>Gives an animal's vision in meters: its "vision" trait, or the reference 20 m without one (SENSE-22).</summary>
        /// <param name="a">The animal that senses.</param>
        /// <returns>How far it sees, in meters.</returns>
        public float Vision(Animal a) => World.Queries.Vision(a);

        /// <summary>Finds the nearest visible animal of these species within a radius (hidden and killed excluded, SENSE-06).</summary>
        /// <param name="a">The animal that senses; never found itself.</param>
        /// <param name="among">The species to look for; empty finds nothing.</param>
        /// <param name="radius">How far to look, in meters: usually the animal's vision.</param>
        /// <returns>The animal found and its distance in meters, or null when none is in range.</returns>
        public AnimalHit? NearestAnimal(Animal a, IReadOnlyList<Species> among, float radius) => World.Queries.NearestAnimal(a, among, radius);

        /// <summary>
        /// Finds the nearest visible animal of a set (threats, prey, kin or listed species, from the food web) within a
        /// radius (hidden and killed excluded, SENSE-06).
        /// </summary>
        /// <param name="a">The animal that senses; never found itself.</param>
        /// <param name="set">Which species, seen from the animal's species: <see cref="AnimalSet.Threats"/>,
        /// <see cref="AnimalSet.Prey"/>, <see cref="AnimalSet.Kin"/> or <see cref="AnimalSet.Listed"/>.</param>
        /// <param name="radius">How far to look, in meters: usually the animal's vision.</param>
        /// <param name="listed">The species for <see cref="AnimalSet.Listed"/>; ignored by the other sets.</param>
        /// <returns>The animal found and its distance in meters, or null when none is in range.</returns>
        /// <example>
        /// In the <c>Read</c> of a <see cref="BandedSense"/>, as <see cref="NearestAnimalSense"/> does:
        /// <code>
        /// float vision = a.Trait(Vision);
        /// var nearest = s.NearestAnimal(a, AnimalSet.Threats, vision);
        /// if (nearest == null) return NoneToken;
        /// return TokenFor(nearest.Value.Distance, vision);
        /// </code>
        /// </example>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, float radius, IReadOnlyList<Species> listed = null) =>
            World.Queries.NearestAnimal(a, World.Resolve(a.Species, set, listed), radius);

        /// <summary>Finds the nearest food item of a layer the animal's diet grazes, within a radius.</summary>
        /// <param name="a">The animal that senses.</param>
        /// <param name="layerName">The layer's name ("Grass"…), or empty for every layer the diet grazes.</param>
        /// <param name="radius">How far to look, in meters.</param>
        /// <returns>The item and its distance in meters, or null when none is in range or the species has no diet.</returns>
        public ResourceItem? NearestResource(Animal a, string layerName, float radius) => World.Queries.NearestResource(a, layerName, radius);

        /// <summary>
        /// Finds the nearest carcass the animal may eat from (06 §4): its diet scavenges it, portions are left, it is not its
        /// own kill and it has not eaten from it yet.
        /// </summary>
        /// <param name="a">The animal that senses.</param>
        /// <param name="radius">How far to look, in meters.</param>
        /// <returns>The carcass and its distance in meters, or null when none is in range or the world has no carcass system.</returns>
        public EntityHit<Carcass>? NearestCarcass(Animal a, float radius) => World.Queries.NearestCarcass(a, radius);

        /// <summary>Finds the nearest egg the animal's diet eats, within a radius (REPRO-23).</summary>
        /// <param name="a">The animal that senses.</param>
        /// <param name="of">Whose eggs, or null for any species the diet eats.</param>
        /// <param name="radius">How far to look, in meters.</param>
        /// <returns>The egg and its distance in meters, or null when none is in range or the world has no egg system.</returns>
        public EntityHit<Egg>? NearestEgg(Animal a, Species of, float radius) => World.Queries.NearestEgg(a, of, radius);

        /// <summary>
        /// Finds the nearest entity of a kind of your own (fruit, nests…) within a radius, across every entity system of
        /// that type, passing the filter; ties by id (see <see cref="WorldQueries.NearestEntity{T}"/>).
        /// </summary>
        /// <typeparam name="T">The kind of entity to look for.</typeparam>
        /// <param name="a">The animal that senses.</param>
        /// <param name="radius">How far to look, in meters.</param>
        /// <param name="filter">Which entities count, given the entity and the animal that senses; null for all.</param>
        /// <returns>The entity and its distance in meters, or null when none is in range.</returns>
        public EntityHit<T>? NearestEntity<T>(Animal a, float radius, System.Func<T, Animal, bool> filter = null) where T : Entity =>
            World.Queries.NearestEntity(a, radius, filter);

        /// <summary>Finds the nearest cover within a radius; distance 0 when the animal stands in it (ENV-10, ENV-12).</summary>
        /// <param name="a">The animal that senses.</param>
        /// <param name="radius">How far to look, in meters.</param>
        /// <returns>The nearest cover point and its distance in meters, or null when there is none in range or no cover layer.</returns>
        public CoverHit? NearestCover(Animal a, float radius) => World.Queries.NearestCover(a, radius);

        /// <summary>Tells whether the animal stands in cover, of any cover layer.</summary>
        /// <param name="a">The animal that senses.</param>
        /// <returns>True when it stands in cover; false otherwise, and always without a cover layer.</returns>
        public bool InCover(Animal a) => World.Queries.InCover(a);

        /// <summary>Tells whether another animal is ready to mate, by its species' mating rule (REPRO-01).</summary>
        /// <param name="other">The animal looked at, usually one found by
        /// <see cref="NearestAnimal(Animal, AnimalSet, float, IReadOnlyList{Species})"/>.</param>
        /// <returns>True when it is ready; always false when its species has no mating rule.</returns>
        public bool IsReady(Animal other) => World.Queries.IsReady(other);

        /// <summary>
        /// Gives the animal another animal's action was about at the start of this tick (its target last tick), or null
        /// (22 §13, the "chased" sense).
        /// </summary>
        /// <param name="other">The animal looked at, such as the nearest threat.</param>
        /// <returns>Its target, which may since have died or been killed, or null when it had none or was busy.</returns>
        /// <remarks>The act phase sets the target; the sense phase runs before it, so this is last tick's target (SENSE-04).
        /// It searches every living animal: fine for one sense, slow if called for many animals per tick.</remarks>
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
