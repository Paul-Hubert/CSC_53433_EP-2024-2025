using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Collects the stats and traits a species' modules declare before any animal exists (ANIM-10, ANIM-15).</summary>
    /// <remarks>
    /// <para>A <b>stat</b> is a per-animal number that changes during life (energy, stamina, thirst); a <b>trait</b> is a
    /// per-animal number fixed at birth (vision, stamina.max), which a number gene may set (ANIM-15). Each module
    /// declares what it needs in <see cref="SpeciesModule.Declare"/> and keeps the id it gets back; the animal stores
    /// the values in slots, read with <c>a[statId]</c> and <c>a.Trait(traitId)</c>.</para>
    /// <para>Two modules may declare the same name: they share one slot, and the first declaration counts.
    /// <see cref="EvoSim.Species.Declarations"/> keeps the result for the run.</para>
    /// </remarks>
    /// <example>
    /// The reference stamina (20 §3.4):
    /// <code>
    /// public override void Declare(SpeciesBuilder b)
    /// {
    ///     Max = b.DeclareTrait("stamina.max", max, 1f, 1000f);
    ///     Value = b.DeclareStat("stamina", StatStart.FullTrait(Max), 0f, float.MaxValue, Max);   // capped by stamina.max
    /// }
    /// </code>
    /// </example>
    public sealed class SpeciesBuilder
    {
        /// <summary>One declared stat, with the module that declared it (for the species inspector).</summary>
        public sealed class StatDeclaration
        {
            /// <summary>The stat's id: its slot, name and range.</summary>
            public StatId Id;
            /// <summary>The value the stat takes when an animal is created.</summary>
            public StatStart Start;
            /// <summary>The module that declared it first, or null when declared outside a module's Declare.</summary>
            public SpeciesModule DeclaredBy;
        }

        /// <summary>One declared trait, with the module that declared it.</summary>
        public sealed class TraitDeclaration
        {
            /// <summary>The trait's id: its slot, name, default and range.</summary>
            public TraitId Id;
            /// <summary>The module that declared it first, or null when declared outside a module's Declare.</summary>
            public SpeciesModule DeclaredBy;
        }

        readonly List<StatDeclaration> stats = new List<StatDeclaration>();
        readonly List<TraitDeclaration> traits = new List<TraitDeclaration>();
        readonly List<Problem> problems = new List<Problem>();
        readonly List<string> costs = new List<string>();

        /// <summary>The species whose stats and traits these are.</summary>
        public Species Species { get; }
        /// <summary>The module currently declaring (set by the species while it calls Declare).</summary>
        public SpeciesModule Current { get; internal set; }

        /// <summary>The declared stats, in declaration order: a stat's index is its place here.</summary>
        public IReadOnlyList<StatDeclaration> Stats => stats;
        /// <summary>The declared traits, in declaration order: a trait's index is its place here.</summary>
        public IReadOnlyList<TraitDeclaration> Traits => traits;
        /// <summary>A declaration problem, reported by validation: V-36 (a default outside its range), V-38 (declared twice
        /// differently).</summary>
        public readonly struct Problem
        {
            /// <summary>The validator id, <c>V-36</c> or <c>V-38</c>.</summary>
            public readonly string Code;
            /// <summary>How serious it is.</summary>
            public readonly Severity Severity;
            /// <summary>The module whose declaration caused it, or null.</summary>
            public readonly SpeciesModule Module;
            /// <summary>The message, for the validation report.</summary>
            public readonly string Text;
            /// <summary>Makes a problem.</summary>
            /// <param name="code">The validator id.</param>
            /// <param name="severity">How serious it is.</param>
            /// <param name="module">The module at fault, or null.</param>
            /// <param name="text">The message.</param>
            public Problem(string code, Severity severity, SpeciesModule module, string text) { Code = code; Severity = severity; Module = module; Text = text; }
            /// <summary>The code and the message, for logs.</summary>
            /// <returns>The text <c>"&lt;code&gt; &lt;message&gt;"</c>.</returns>
            public override string ToString() => Code + " " + Text;
        }

        /// <summary>Declaration problems, reported by validation.</summary>
        public IReadOnlyList<Problem> Problems => problems;

        /// <summary>Starts an empty set of declarations; the species makes one at each set-up.</summary>
        /// <param name="species">The species whose modules declare.</param>
        public SpeciesBuilder(Species species) { Species = species; }

        /// <summary>Declares a stat; declaring the same name again returns the first declaration (V-38 if it differs).</summary>
        /// <param name="name">The stat's name, e.g. <c>"energy"</c>; senses and other modules find it by this name.</param>
        /// <param name="start">Its value when an animal is created: <see cref="StatStart.Fixed"/> or
        /// <see cref="StatStart.FullTrait"/>.</param>
        /// <param name="min">The lowest value; writes below it are raised to it.</param>
        /// <param name="max">The highest value; gains never pass it (ANIM-22).</param>
        /// <param name="capTrait">A trait that caps the stat per animal (stamina ≤ stamina.max), or the default id for none.</param>
        /// <returns>The stat's id, to keep in the module and use as <c>a[id]</c>.</returns>
        public StatId DeclareStat(string name, StatStart start, float min = float.MinValue, float max = float.MaxValue,
                                  TraitId capTrait = default)
        {
            foreach (var s in stats)
                if (s.Id.Name == name)
                {
                    if (!Same(s.Start.Value, start.Value) || s.Start.Trait != start.Trait || !Same(s.Id.Min, min) || !Same(s.Id.Max, max))
                        problems.Add(new Problem("V-38", Severity.Warning, Current,
                            $"Stat '{name}' declared twice with different start or range ({Who(s.DeclaredBy)} and {Who(Current)}); the first one counts."));
                    return s.Id;
                }
            var id = new StatId(stats.Count, name, min, max, capTrait.IsValid ? capTrait.Index : -1);
            stats.Add(new StatDeclaration { Id = id, Start = start, DeclaredBy = Current });
            return id;
        }

        /// <summary>Declares a trait with its default and range; the same name again returns the first declaration.</summary>
        /// <param name="name">The trait's name, e.g. <c>"vision"</c>; number genes and other modules find it by this name.</param>
        /// <param name="defaultValue">Its value when no gene sets it.</param>
        /// <param name="min">The lowest value a gene or a change may give it.</param>
        /// <param name="max">The highest value a gene or a change may give it.</param>
        /// <param name="changeable">True when it may change during life (ANIM-17); false for every reference trait.</param>
        /// <returns>The trait's id, to keep in the module and use as <c>a.Trait(id)</c>.</returns>
        /// <remarks>A default outside the range is a warning (V-36); a second declaration with another default or range
        /// is a warning too (V-38).</remarks>
        public TraitId DeclareTrait(string name, float defaultValue, float min = float.MinValue, float max = float.MaxValue,
                                    bool changeable = false)
        {
            foreach (var t in traits)
                if (t.Id.Name == name)
                {
                    if (!Same(t.Id.Default, defaultValue) || !Same(t.Id.Min, min) || !Same(t.Id.Max, max))
                        problems.Add(new Problem("V-38", Severity.Warning, Current,
                            $"Trait '{name}' declared twice with defaults {t.Id.Default} ({Who(t.DeclaredBy)}) and {defaultValue} ({Who(Current)}), or different ranges; the first one counts."));
                    return t.Id;
                }
            if (defaultValue < min || defaultValue > max || float.IsNaN(defaultValue))
                problems.Add(new Problem("V-36", Severity.Warning, Current, $"Trait '{name}' has the default {defaultValue}, outside its range [{min}, {max}] ({Who(Current)})."));
            var id = new TraitId(traits.Count, name, defaultValue, min, max, changeable);
            traits.Add(new TraitDeclaration { Id = id, DeclaredBy = Current });
            return id;
        }

        /// <summary>Traits some cost depends on (09 §5): a number gene on any other trait drifts to one end (V-47).</summary>
        public IReadOnlyList<string> Costs => costs;

        /// <summary>Declares that a cost depends on this trait (for example a metabolism that grows with stamina.max).</summary>
        /// <param name="trait">The trait's name; null or empty is ignored.</param>
        public void DeclareCost(string trait)
        {
            if (!string.IsNullOrEmpty(trait) && !costs.Contains(trait)) costs.Add(trait);
        }

        /// <summary>Whether some module declared a cost on this trait.</summary>
        /// <param name="trait">The trait's name.</param>
        /// <returns>True when a cost depends on it; a number gene on a trait without one gets warning V-47.</returns>
        public bool HasCost(string trait) => costs.Contains(trait);

        /// <summary>A declared stat by name, or an invalid id.</summary>
        /// <param name="name">The stat's name.</param>
        /// <returns>Its id, or the default id (<see cref="StatId.IsValid"/> false) when no module declared it.</returns>
        public StatId FindStat(string name)
        {
            foreach (var s in stats) if (s.Id.Name == name) return s.Id;
            return default;
        }

        /// <summary>A declared trait by name, or an invalid id.</summary>
        /// <param name="name">The trait's name.</param>
        /// <returns>Its id, or the default id (<see cref="TraitId.IsValid"/> false) when no module declared it.</returns>
        public TraitId FindTrait(string name)
        {
            foreach (var t in traits) if (t.Id.Name == name) return t.Id;
            return default;
        }

        /// <summary>True when two declared numbers are equal, up to a relative 1e-6.</summary>
        static bool Same(float a, float b) => a == b || System.Math.Abs(a - b) <= 1e-6f * System.Math.Max(1f, System.Math.Abs(a));
        /// <summary>A module's name for messages: its class and GameObject, or "?".</summary>
        static string Who(SpeciesModule m) => m == null ? "?" : m.GetType().Name + " on " + m.gameObject.name;
    }
}
