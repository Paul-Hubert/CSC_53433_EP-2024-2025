using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// The queries senses and actions share (ACT-05, SENSE-06): within the animal's vision, never itself, never a
    /// killed animal, never an animal hidden from it in cover, diets checked. Ties by id (SPACE-08).
    /// </summary>
    /// <remarks>
    /// <para>Reach it with <see cref="World.Queries"/>; senses usually call it through their <see cref="SenseContext"/>
    /// and actions through their <see cref="ActContext"/>. Using the same queries is what keeps an action from targeting
    /// something its senses could not report (ACT-05).</para>
    /// <para>Every query only reads the world. Results are null when nothing is found.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var threat = World.Queries.NearestAnimal(a, AnimalSet.Threats);   // within a's vision
    /// if (threat != null &amp;&amp; threat.Value.Distance &lt; 5f) { /* ... */ }
    /// </code>
    /// </example>
    public sealed class WorldQueries
    {
        /// <summary>The vision of a species that declares no "vision" trait, in meters (14: 20 m).</summary>
        public const float ReferenceVision = 20f;

        readonly World world;                                                           // whose space, food web and services it queries
        readonly Dictionary<Species, TraitId> visionTraits = new Dictionary<Species, TraitId>();   // each species' "vision" trait
        readonly Func<Animal, Animal, bool> visible;                                    // filter: not hidden from the viewer in cover
        readonly Func<Animal, Animal, bool> visibleAndReady;                            // filter: visible and ready to mate
        readonly Func<Carcass, Animal, bool> carcassForViewer;                          // filter: a carcass the viewer may eat from
        readonly Func<Egg, Animal, bool> eggForViewer;                                  // filter: an egg of eggSpecies the diet eats
        readonly List<Cover> covers = new List<Cover>();                               // every cover service: hidden by any (ENV-11)
        Species eggSpecies;                                                             // set during NearestEgg only; null = any

        /// <summary>Makes the queries of a World; the World makes one at each set-up.</summary>
        /// <param name="world">The World to query.</param>
        public WorldQueries(World world)
        {
            this.world = world;
            // Made once and kept: no delegate is allocated per query (R-07).
            visible = (candidate, viewer) => !IsHiddenFrom(candidate, viewer.Species);
            visibleAndReady = (candidate, viewer) => !IsHiddenFrom(candidate, viewer.Species) && IsReady(candidate);
            carcassForViewer = (c, viewer) => c.CanBeEatenBy(viewer) && Diet(viewer)?.Scavenging(c.Of) != null;
            eggForViewer = (e, viewer) => !e.UsedUp && (eggSpecies == null || e.Species == eggSpecies) && Diet(viewer)?.EatingEggs(e.Species) != null;
        }

        /// <summary>Takes the World's cover services, and forgets the vision traits (step 3 of the set-up).</summary>
        internal void Bind(IEnumerable<Cover> coverServices)
        {
            covers.Clear();
            covers.AddRange(coverServices);
            visionTraits.Clear();
        }

        /// <summary>The animal's vision (SENSE-22): its "vision" trait, or the reference 20 m.</summary>
        /// <param name="a">The animal.</param>
        /// <returns>How far it sees, in meters.</returns>
        public float Vision(Animal a)
        {
            if (!visionTraits.TryGetValue(a.Species, out var t))
            {
                t = a.Species.Declarations.FindTrait("vision");
                visionTraits[a.Species] = t;
            }
            return t.IsValid ? a.Trait(t) : ReferenceVision;
        }

        /// <summary>Whether this animal stands in cover that hides it from that species (ENV-11).</summary>
        /// <param name="target">The animal that may be hidden.</param>
        /// <param name="viewer">The species that looks.</param>
        /// <returns>True when any cover service hides it from that species.</returns>
        public bool IsHiddenFrom(Animal target, Species viewer)
        {
            for (int i = 0; i < covers.Count; i++) if (covers[i].IsHiddenFrom(target, viewer)) return true;   // any cover hides
            return false;
        }

        /// <summary>Whether the animal stands in cover of any cover service, whoever looks.</summary>
        /// <param name="a">The animal.</param>
        /// <returns>True when its position is in cover.</returns>
        public bool InCover(Animal a)
        {
            for (int i = 0; i < covers.Count; i++) if (covers[i].InCover(a.Position)) return true;
            return false;
        }

        /// <summary>Ready to mate (REPRO-01), by the species' mating rule; never without one.</summary>
        /// <param name="a">The animal.</param>
        /// <returns>True when its species has a mating rule and the rule says it is ready.</returns>
        public bool IsReady(Animal a)
        {
            var rule = a.Species.Module<MatingRule>();
            return rule != null && rule.IsReady(a);
        }

        /// <summary>The nearest visible animal of these species within radius (SPACE-07, ACT-05).</summary>
        /// <param name="a">The animal that looks; never found itself.</param>
        /// <param name="among">The species to search.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <returns>The nearest animal not killed and not hidden from <paramref name="a"/>, with its distance; null when none.</returns>
        public AnimalHit? NearestAnimal(Animal a, IReadOnlyList<Species> among, float radius)
        {
            if (among.Count == 0) return null;
            return world.Space.Nearest(a, a.Position, radius, among, visible, out var hit) ? hit : (AnimalHit?)null;
        }

        /// <summary>The nearest visible animal of an animal set within the animal's vision (SPEC-12).</summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="set">Whose animals: its species' threats, prey, kin, or a listed set.</param>
        /// <param name="listed">The species of a listed set; ignored for the others.</param>
        /// <returns>The nearest such animal within <see cref="Vision"/>, with its distance; null when none.</returns>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, IReadOnlyList<Species> listed = null) =>
            NearestAnimal(a, world.Resolve(a.Species, set, listed), Vision(a));

        /// <summary>
        /// The nearest visible kin that is ready to mate (07 §4, mate), within vision and within the partner range of the
        /// kin sense that reports readiness: the action never targets what the senses can't report (ACT-05).
        /// </summary>
        /// <param name="a">The animal that looks for a partner.</param>
        /// <returns>The nearest ready partner, with its distance; null when none.</returns>
        public AnimalHit? NearestReadyKin(Animal a)
        {
            var kin = world.Resolve(a.Species, AnimalSet.Kin);
            float radius = Math.Min(Vision(a), PartnerRange(a.Species));
            return world.Space.Nearest(a, a.Position, radius, kin, visibleAndReady, out var hit) ? hit : (AnimalHit?)null;
        }

        /// <summary>The partner range of the species' kin senses that report readiness (the widest), or no limit without one.</summary>
        static float PartnerRange(Species s)
        {
            float range = -1f;
            var senses = s.Senses;
            for (int i = 0; i < senses.Count; i++)
                if (senses[i] is NearestAnimalSense n && n.Targets == AnimalSet.Kin && n.ReportsReadiness) range = Math.Max(range, n.PartnerRange);
            return range < 0f ? float.PositiveInfinity : range;
        }

        /// <summary>The nearest item of a layer the diet grazes (all of them for an empty name), within radius.</summary>
        /// <param name="a">The animal that looks; its species' diet decides which layers count.</param>
        /// <param name="layerName">One layer's name, or null or empty for every layer the diet grazes.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <returns>The nearest item (a tie keeps the first layer in diet order); null when none or without a diet.</returns>
        public ResourceItem? NearestResource(Animal a, string layerName, float radius)
        {
            var diet = Diet(a);
            if (diet == null) return null;
            ResourceItem? best = null;
            var targets = diet.Resolved;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.Kind != EdibleTargetKind.Layer || t.Edible == null) continue;
                if (!string.IsNullOrEmpty(layerName) && t.Layer.LayerName != layerName) continue;
                if (t.Layer.Nearest(a.Position, radius, out var item) && (best == null || item.Distance < best.Value.Distance - 1e-6f))
                    best = item;
            }
            return best;
        }

        /// <summary>The nearest carcass this animal may eat from (06 §4): its diet scavenges it, not its own kill, not eaten
        /// yet.</summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <returns>The nearest such carcass, with its distance; null when none or without a carcass system.</returns>
        public EntityHit<Carcass>? NearestCarcass(Animal a, float radius)
        {
            var cs = world.Service<CarcassSystem>();
            if (cs == null) return null;
            return cs.Nearest(a.Position, radius, a, carcassForViewer, out var c, out float d) ? new EntityHit<Carcass>(c, d) : (EntityHit<Carcass>?)null;
        }

        /// <summary>The nearest egg (of a species, or any) this animal's diet eats, within radius (REPRO-23).</summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="of">Only eggs of this species, or null for any species.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <returns>The nearest such egg, with its distance; null when none or without an egg system.</returns>
        public EntityHit<Egg>? NearestEgg(Animal a, Species of, float radius)
        {
            var eggs = world.Service<EggSystem>();
            if (eggs == null) return null;
            eggSpecies = of;                                    // read by the shared filter during this call only
            bool found = eggs.Nearest(a.Position, radius, a, eggForViewer, out var e, out float d);
            eggSpecies = null;
            return found ? new EntityHit<Egg>(e, d) : (EntityHit<Egg>?)null;
        }

        /// <summary>The nearest cover within radius; distance 0 when standing in it (ENV-10, ENV-12).</summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <returns>The nearest point in cover of any cover service, with its distance; null when none.</returns>
        public CoverHit? NearestCover(Animal a, float radius)
        {
            CoverHit? best = null;
            for (int i = 0; i < covers.Count; i++)                                       // the nearest of every cover; a tie keeps the first
                if (covers[i].NearestCover(a.Position, radius, out var p, out float d) && (best == null || d < best.Value.Distance - 1e-6f))
                    best = new CoverHit(p, d);
            return best;
        }

        /// <summary>
        /// The nearest entity of a kind of your own (fruit, nests…) within radius, across every entity system of that type,
        /// passing the filter, which receives the viewer; ties by id (SPACE-07, SPACE-08).
        /// </summary>
        /// <typeparam name="T">The entity kind, e.g. a <c>Fruit</c> class derived from <see cref="Entity"/>.</typeparam>
        /// <param name="a">The animal that looks; the filter receives it as the viewer.</param>
        /// <param name="radius">The farthest distance, in meters.</param>
        /// <param name="filter">Which entities count, given the entity and the viewer; null for all.</param>
        /// <returns>The nearest entity, with its distance; null when none.</returns>
        public EntityHit<T>? NearestEntity<T>(Animal a, float radius, Func<T, Animal, bool> filter = null) where T : Entity
        {
            EntityHit<T>? best = null;
            foreach (var system in world.ServicesOf<EntitySystem<T>>())
            {
                if (!system.Nearest(a.Position, radius, a, filter, out var e, out float d)) continue;
                if (best == null || d < best.Value.Distance - 1e-6f || (Math.Abs(d - best.Value.Distance) <= 1e-6f && e.Id < best.Value.Entity.Id))
                    best = new EntityHit<T>(e, d);
            }
            return best;
        }

        /// <summary>The animal's species' diet, or null.</summary>
        static Diet Diet(Animal a) => a.Species.Module<Diet>();
    }
}
