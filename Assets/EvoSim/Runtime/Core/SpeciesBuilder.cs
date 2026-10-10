using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Collects the stats and traits a species' modules declare before any animal exists (ANIM-10, ANIM-15).</summary>
    public sealed class SpeciesBuilder
    {
        /// <summary>One declared stat, with the module that declared it (for the species inspector).</summary>
        public sealed class StatDeclaration
        {
            public StatId Id;
            public StatStart Start;
            public SpeciesModule DeclaredBy;
        }

        /// <summary>One declared trait, with the module that declared it.</summary>
        public sealed class TraitDeclaration
        {
            public TraitId Id;
            public SpeciesModule DeclaredBy;
        }

        readonly List<StatDeclaration> stats = new List<StatDeclaration>();
        readonly List<TraitDeclaration> traits = new List<TraitDeclaration>();
        readonly List<string> problems = new List<string>();
        readonly List<string> costs = new List<string>();

        public Species Species { get; }
        /// <summary>The module currently declaring (set by the species while it calls Declare).</summary>
        public SpeciesModule Current { get; internal set; }

        public IReadOnlyList<StatDeclaration> Stats => stats;
        public IReadOnlyList<TraitDeclaration> Traits => traits;
        /// <summary>Conflicting declarations (same name, different settings), reported by validation.</summary>
        public IReadOnlyList<string> Problems => problems;

        public SpeciesBuilder(Species species) { Species = species; }

        /// <summary>Declares a stat; declaring the same name again returns the first declaration.</summary>
        public StatId DeclareStat(string name, StatStart start, float min = float.MinValue, float max = float.MaxValue,
                                  TraitId capTrait = default)
        {
            foreach (var s in stats)
                if (s.Id.Name == name) return s.Id;
            var id = new StatId(stats.Count, name, min, max, capTrait.IsValid ? capTrait.Index : -1);
            stats.Add(new StatDeclaration { Id = id, Start = start, DeclaredBy = Current });
            return id;
        }

        /// <summary>Declares a trait with its default and range; the same name again returns the first declaration.</summary>
        public TraitId DeclareTrait(string name, float defaultValue, float min = float.MinValue, float max = float.MaxValue,
                                    bool changeable = false)
        {
            foreach (var t in traits)
                if (t.Id.Name == name)
                {
                    if (!Same(t.Id.Default, defaultValue))
                        problems.Add($"Trait '{name}' declared twice with defaults {t.Id.Default} ({Who(t.DeclaredBy)}) and {defaultValue} ({Who(Current)}).");
                    return t.Id;
                }
            var id = new TraitId(traits.Count, name, defaultValue, min, max, changeable);
            traits.Add(new TraitDeclaration { Id = id, DeclaredBy = Current });
            return id;
        }

        /// <summary>Traits some cost depends on (09 §5): a number gene on any other trait drifts to one end (V-47).</summary>
        public IReadOnlyList<string> Costs => costs;

        /// <summary>Declares that a cost depends on this trait (for example a metabolism that grows with stamina.max).</summary>
        public void DeclareCost(string trait)
        {
            if (!string.IsNullOrEmpty(trait) && !costs.Contains(trait)) costs.Add(trait);
        }

        public bool HasCost(string trait) => costs.Contains(trait);

        /// <summary>A declared stat by name, or an invalid id.</summary>
        public StatId FindStat(string name)
        {
            foreach (var s in stats) if (s.Id.Name == name) return s.Id;
            return default;
        }

        /// <summary>A declared trait by name, or an invalid id.</summary>
        public TraitId FindTrait(string name)
        {
            foreach (var t in traits) if (t.Id.Name == name) return t.Id;
            return default;
        }

        static bool Same(float a, float b) => System.Math.Abs(a - b) <= 1e-6f * System.Math.Max(1f, System.Math.Abs(a));
        static string Who(SpeciesModule m) => m == null ? "?" : m.GetType().Name + " on " + m.gameObject.name;
    }
}
