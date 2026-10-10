using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// The queries senses and actions share (ACT-05, SENSE-06): within the animal's vision, never itself, never a
    /// killed animal, never an animal hidden from it in cover, diets checked. Ties by id (SPACE-08).
    /// </summary>
    public sealed class WorldQueries
    {
        /// <summary>The vision of a species that declares no "vision" trait, in meters (14: 20 m).</summary>
        public const float ReferenceVision = 20f;

        readonly World world;
        readonly Dictionary<Species, TraitId> visionTraits = new Dictionary<Species, TraitId>();
        readonly Func<Animal, Animal, bool> visible;
        readonly Func<Animal, Animal, bool> visibleAndReady;
        readonly Func<Carcass, Animal, bool> carcassForViewer;
        readonly Func<Egg, Animal, bool> eggForViewer;
        readonly List<Cover> covers = new List<Cover>();
        Species eggSpecies;

        public WorldQueries(World world)
        {
            this.world = world;
            visible = (candidate, viewer) => !IsHiddenFrom(candidate, viewer.Species);
            visibleAndReady = (candidate, viewer) => !IsHiddenFrom(candidate, viewer.Species) && IsReady(candidate);
            carcassForViewer = (c, viewer) => c.CanBeEatenBy(viewer) && Diet(viewer)?.Scavenging(c.Of) != null;
            eggForViewer = (e, viewer) => !e.UsedUp && (eggSpecies == null || e.Species == eggSpecies) && Diet(viewer)?.EatingEggs(e.Species) != null;
        }

        internal void Bind(IEnumerable<Cover> coverServices)
        {
            covers.Clear();
            covers.AddRange(coverServices);
            visionTraits.Clear();
        }

        /// <summary>The animal's vision (SENSE-22): its "vision" trait, or the reference 20 m.</summary>
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
        public bool IsHiddenFrom(Animal target, Species viewer)
        {
            for (int i = 0; i < covers.Count; i++) if (covers[i].IsHiddenFrom(target, viewer)) return true;   // any cover hides
            return false;
        }

        public bool InCover(Animal a)
        {
            for (int i = 0; i < covers.Count; i++) if (covers[i].InCover(a.Position)) return true;
            return false;
        }

        /// <summary>Ready to mate (REPRO-01), by the species' mating rule; never without one.</summary>
        public bool IsReady(Animal a)
        {
            var rule = a.Species.Module<MatingRule>();
            return rule != null && rule.IsReady(a);
        }

        /// <summary>The nearest visible animal of these species within radius (SPACE-07, ACT-05).</summary>
        public AnimalHit? NearestAnimal(Animal a, IReadOnlyList<Species> among, float radius)
        {
            if (among.Count == 0) return null;
            return world.Space.Nearest(a, a.Position, radius, among, visible, out var hit) ? hit : (AnimalHit?)null;
        }

        /// <summary>The nearest visible animal of an animal set within the animal's vision (SPEC-12).</summary>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, IReadOnlyList<Species> listed = null) =>
            NearestAnimal(a, world.Resolve(a.Species, set, listed), Vision(a));

        /// <summary>
        /// The nearest visible kin that is ready to mate (07 §4, mate), within vision and within the partner range of the
        /// kin sense that reports readiness: the action never targets what the senses can't report (ACT-05).
        /// </summary>
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

        /// <summary>The nearest carcass this animal may eat from (06 §4): its diet scavenges it, not its own kill, not eaten yet.</summary>
        public EntityHit<Carcass>? NearestCarcass(Animal a, float radius)
        {
            var cs = world.Service<CarcassSystem>();
            if (cs == null) return null;
            return cs.Nearest(a.Position, radius, a, carcassForViewer, out var c, out float d) ? new EntityHit<Carcass>(c, d) : (EntityHit<Carcass>?)null;
        }

        /// <summary>The nearest egg (of a species, or any) this animal's diet eats, within radius (REPRO-23).</summary>
        public EntityHit<Egg>? NearestEgg(Animal a, Species of, float radius)
        {
            var eggs = world.Service<EggSystem>();
            if (eggs == null) return null;
            eggSpecies = of;
            bool found = eggs.Nearest(a.Position, radius, a, eggForViewer, out var e, out float d);
            eggSpecies = null;
            return found ? new EntityHit<Egg>(e, d) : (EntityHit<Egg>?)null;
        }

        /// <summary>The nearest cover within radius; distance 0 when standing in it (ENV-10, ENV-12).</summary>
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

        static Diet Diet(Animal a) => a.Species.Module<Diet>();
    }
}
