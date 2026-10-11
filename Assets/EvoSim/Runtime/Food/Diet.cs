using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a species eats (SPEC-15): layers, species, "carcass:&lt;species&gt;", "egg:&lt;species&gt;". Each target must
    /// carry an Edible (SPEC-10, V-24). A species with no diet eats nothing.
    /// </summary>
    /// <remarks>
    /// <para>Whether something is eaten is decided on both sides (SPEC-13): the eaten thing carries an
    /// <see cref="Edible"/>, which says how much energy it gives, and the eater's diet lists it. Each entry names a
    /// resource layer (grazed), a species by id or display name (struck), <c>carcass:&lt;species&gt;</c> (scavenged) or
    /// <c>egg:&lt;species&gt;</c> (eggs eaten), with an energy scale.</para>
    /// <para>The entries are resolved by name in <see cref="Declare"/> and <see cref="Initialize"/>, and again when a
    /// species is added during a run (<see cref="EvoSim.World.RebuildFoodWeb"/>). The World derives the food web from
    /// <see cref="Striking"/>: the species a diet strikes are its prey, and it is their threat (SPEC-12). The interaction
    /// resolver asks <see cref="Grazing"/>, <see cref="Striking"/>, <see cref="Scavenging"/> or <see cref="EatingEggs"/>
    /// before every meal, then <see cref="EnergyFrom"/> for its energy and <see cref="KillChanceAgainst"/> for a
    /// strike's odds.</para>
    /// <para>All six are virtual: derive from <see cref="Diet"/> to make meals depend on the eater or the prey. The
    /// species' modules find it with <c>Species.Module&lt;Diet&gt;()</c>, so keep one diet per species.</para>
    /// </remarks>
    /// <example>
    /// The reference predator's diet, set from code as the reference scenes do:
    /// <code>
    /// var diet = foodObject.AddComponent&lt;Diet&gt;();
    /// diet.Set("prey", "carcass:prey");     // strike prey animals, scavenge their carcasses
    /// diet.SetKillChance(0.5f);              // the default of the hunter's kill.chance trait
    /// </code>
    /// </example>
    public class Diet : SpeciesModule
    {
        /// <summary>One line of the diet: what is eaten, and how much its energy is scaled (SPEC-15).</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>
            /// What is eaten: a resource layer's name (<c>"Grass"</c>), a species' id or display name (<c>"prey"</c>),
            /// <c>"carcass:&lt;species&gt;"</c> or <c>"egg:&lt;species&gt;"</c>.
            /// </summary>
            [Tooltip("A layer, a species, carcass:prey, egg:prey...")]
            public string target = "Grass";
            /// <summary>Multiplies the energy gained from this target (default 1).</summary>
            [Min(0f), Tooltip("Multiplies the energy gained (default 1).")]
            public float energyScale = 1f;

            /// <summary>An entry for <c>"Grass"</c> at scale 1 (Unity's serializer needs a constructor without parameters).</summary>
            public Entry() { }
            /// <summary>An entry for a target, with an energy scale.</summary>
            /// <param name="target">A layer, a species, <c>"carcass:&lt;species&gt;"</c> or <c>"egg:&lt;species&gt;"</c>.</param>
            /// <param name="energyScale">Multiplies the energy gained (default 1).</param>
            public Entry(string target, float energyScale = 1f) { this.target = target; this.energyScale = energyScale; }
        }

        [SerializeField, Tooltip("What the species eats: layers, species, carcass:<species>, egg:<species>, with energy scales.")]
        List<Entry> eats = new List<Entry>();
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a strike kills (a trait of the hunter, reference 0.5; ACT-11).")]
        float killChance = 0.5f;

        readonly List<EdibleTarget> resolved = new List<EdibleTarget>();
        readonly List<string> unresolved = new List<string>();     // entry names that matched nothing, for V-20
        readonly Dictionary<(EdibleTarget, Species), EdibleTarget> forClone = new Dictionary<(EdibleTarget, Species), EdibleTarget>();   // look-ups only

        /// <summary>The entries as written in the Inspector, in diet order.</summary>
        public IReadOnlyList<Entry> Entries => eats;
        /// <summary>The entries resolved at initialisation (ARCH-08), in diet order.</summary>
        public IReadOnlyList<EdibleTarget> Resolved => resolved;
        /// <summary>
        /// The kill-chance trait, <c>kill.chance</c>, declared when the diet strikes (or may strike) a species; invalid
        /// otherwise.
        /// </summary>
        public TraitId KillChance { get; private set; }
        /// <summary>True when an entry resolved to a species: this diet strikes animals.</summary>
        public bool Strikes { get; private set; }

        /// <summary>
        /// Resolves the entries and declares the <c>kill.chance</c> trait (from 0 to 1) when the diet strikes, or may
        /// strike, a species.
        /// </summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            Resolve();
            if (Strikes || MayStrike()) KillChance = b.DeclareTrait("kill.chance", killChance, 0f, 1f);
        }

        /// <summary>
        /// An entry that names no layer and no kind may name a species added later (SPEC-30): it needs the kill chance
        /// too.
        /// </summary>
        bool MayStrike()
        {
            foreach (var e in eats)
            {
                string name = (e.target ?? "").Trim();
                if (name.Length > 0 && name.IndexOf(':') < 0 && World.Service<ResourceLayer>(name) == null) return true;
            }
            return false;
        }

        /// <summary>Resolves the entries again, once every module of every species has declared.</summary>
        /// <inheritdoc/>
        public override void Initialize() => Resolve();

        /// <summary>Resolves the entries by name (ARCH-08); called again when species are added during a run.</summary>
        /// <remarks>
        /// Called by <see cref="Declare"/>, <see cref="Initialize"/> and <see cref="EvoSim.World.RebuildFoodWeb"/>. Names
        /// that match nothing are kept for validation (V-20).
        /// </remarks>
        public void Resolve()
        {
            resolved.Clear();
            unresolved.Clear();
            forClone.Clear();
            Strikes = false;
            foreach (var e in eats)
            {
                var t = ResolveEntry(World, e);
                if (t == null) { unresolved.Add(e.target); continue; }
                resolved.Add(t);
                if (t.Kind == EdibleTargetKind.Species) Strikes = true;
            }
        }

        /// <summary>
        /// Resolves one entry by name: <c>kind:species</c> for carcasses and the other entity kinds, else a resource layer,
        /// else a species; null when nothing matches. The target's Edible is looked up here; null means not edible (V-24).
        /// </summary>
        static EdibleTarget ResolveEntry(World w, Entry e)
        {
            string name = (e.target ?? "").Trim();
            int colon = name.IndexOf(':');
            if (colon > 0)
            {
                string kind = name.Substring(0, colon).Trim(), of = name.Substring(colon + 1).Trim();
                var sp = w.FindSpeciesByName(of);
                if (sp == null) return null;
                if (kind == "carcass")
                {
                    // Carcasses are edible when the species' own Edible leaves some and the world has a carcass system.
                    var c = w.Service<CarcassSystem>();
                    var edible = sp.GetComponent<Edible>();
                    bool leaves = edible != null && edible.enabled && edible.carcassPortions > 0;
                    return new EdibleTarget
                    {
                        Kind = EdibleTargetKind.Carcass, Name = name, Species = sp, Entities = c,
                        Edible = c != null && leaves ? edible : null, EnergyScale = e.energyScale
                    };
                }
                // Any other kind (eggs): the entity system of that kind, edible through its own Edible.
                foreach (var k in w.ServicesOf<EntityKind>())
                    if (k.Kind == kind)
                        return new EdibleTarget
                        {
                            Kind = EdibleTargetKind.Egg, Name = name, Species = sp, Entities = k,
                            Edible = k.Edible, EnergyScale = e.energyScale
                        };
                return null;
            }
            var layer = w.Service<ResourceLayer>(name);
            if (layer != null)
                return new EdibleTarget { Kind = EdibleTargetKind.Layer, Name = name, Layer = layer, Edible = layer.Edible, EnergyScale = e.energyScale };
            var target = w.FindSpeciesByName(name);
            if (target != null)
            {
                var edible = target.GetComponent<Edible>();
                return new EdibleTarget
                {
                    Kind = EdibleTargetKind.Species, Name = name, Species = target,
                    Edible = edible != null && edible.enabled ? edible : null, EnergyScale = e.energyScale
                };
            }
            return null;
        }

        /// <summary>The chance that this hunter's strike kills that prey (ACT-11): the hunter's kill-chance trait by default.</summary>
        /// <param name="hunter">The animal that strikes.</param>
        /// <param name="prey">The animal struck, within reach and edible for this diet.</param>
        /// <returns>A probability from 0 to 1; 0 when no kill-chance trait was declared.</returns>
        /// <remarks>Called by the interaction resolver at every strike. Override it for odds that depend on the prey.</remarks>
        public virtual float KillChanceAgainst(Animal hunter, Animal prey) => KillChance.IsValid ? hunter.Trait(KillChance) : 0f;

        /// <summary>
        /// The energy a meal gives this eater: the entry's energy by default (ACT-11); override it to depend on the
        /// eater.
        /// </summary>
        /// <param name="eater">The animal that eats.</param>
        /// <param name="entry">The diet entry the meal comes from.</param>
        /// <param name="energy">
        /// The meal's energy before this hook, in points: the edible's energy (per item, kill or carcass portion) times the
        /// entry's scale.
        /// </param>
        /// <returns>The energy gained, in points; the energy stat then stays at or below its maximum (ANIM-22).</returns>
        /// <remarks>Called by the interaction resolver for every graze, kill, carcass portion and egg.</remarks>
        public virtual float EnergyFrom(Animal eater, EdibleTarget entry, float energy) => energy;

        /// <summary>The entry for grazing this layer, if the diet lists it and it is edible (SPEC-13); else null.</summary>
        /// <param name="layer">The resource layer of the item to graze.</param>
        /// <returns>The resolved entry, or null when the diet doesn't graze that layer or the layer has no Edible.</returns>
        public virtual EdibleTarget Grazing(ResourceLayer layer)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Layer && t.Layer == layer && t.Edible != null) return t;
            return null;
        }

        /// <summary>The entry for striking this species, if listed and edible (SPEC-13); else null.</summary>
        /// <param name="s">The species of the struck animal; species cloned from a listed one count too (SPEC-31).</param>
        /// <returns>
        /// The entry as it applies to that species, or null when the diet doesn't strike it or its Edible is missing or
        /// disabled.
        /// </returns>
        /// <remarks>
        /// The World also calls it to derive the food web: the species it returns an entry for are this species' prey
        /// (SPEC-12).
        /// </remarks>
        public virtual EdibleTarget Striking(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Species && t.Names(s))
                {
                    var own = Own(t, s);
                    if (own.Edible != null && own.Edible.enabled) return own;            // switched off during the run: absent (ARCH-06)
                }
            return null;
        }

        /// <summary>The entry for scavenging carcasses of this species, if listed and edible; else null.</summary>
        /// <param name="s">The species of the killed animal; species cloned from a listed one count too (SPEC-31).</param>
        /// <returns>
        /// The entry as it applies to that species, or null when the diet doesn't list its carcasses or they are not
        /// edible.
        /// </returns>
        public virtual EdibleTarget Scavenging(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Carcass && t.Names(s))
                {
                    var own = Own(t, s);
                    if (own.Edible != null && own.Edible.enabled) return own;
                }
            return null;
        }

        /// <summary>
        /// The entry as it applies to this species: a species cloned from the one named (SPEC-31) is eaten through its own
        /// Edible, so disabling it or changing its energy counts (SPEC-10).
        /// </summary>
        EdibleTarget Own(EdibleTarget t, Species s)
        {
            if (s == t.Species) return t;
            if (forClone.TryGetValue((t, s), out var own)) return own;
            var edible = s.GetComponent<Edible>();
            bool usable = edible != null && edible.enabled && (t.Kind != EdibleTargetKind.Carcass || (edible.carcassPortions > 0 && t.Entities != null));
            own = new EdibleTarget { Kind = t.Kind, Name = t.Name, Species = s, Entities = t.Entities, Edible = usable ? edible : null, EnergyScale = t.EnergyScale };
            forClone[(t, s)] = own;
            return own;
        }

        /// <summary>The entry for eating eggs of this species, if listed and the egg kind is edible (REPRO-23); else null.</summary>
        /// <param name="s">The species that laid the egg; species cloned from a listed one count too.</param>
        /// <returns>The resolved entry, or null when the diet doesn't eat those eggs or the egg system has no Edible.</returns>
        public virtual EdibleTarget EatingEggs(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Egg && t.Names(s) && t.Edible != null) return t;
            return null;
        }

        /// <summary>The carcass system's line for a diet that scavenges carcasses (08 §7).</summary>
        /// <inheritdoc/>
        /// <remarks>Only the first edible carcass entry writes a line, and only in a world with a carcass system.</remarks>
        public override void WritePromptRules(PromptWriter w)
        {
            var carcasses = World != null ? World.Service<CarcassSystem>() : null;
            if (carcasses == null) return;
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Carcass && t.Edible != null) { w.Rule(carcasses.PromptRuleFor(t)); return; }
        }

        /// <summary>Whether the diet has an edible entry of this kind.</summary>
        /// <param name="kind">Layer, species, carcass or egg.</param>
        /// <returns>True when an entry of that kind resolved and its target has an Edible.</returns>
        public bool Has(EdibleTargetKind kind)
        {
            foreach (var t in resolved) if (t.Kind == kind && t.Edible != null) return true;
            return false;
        }

        /// <summary>Sets the entries from code (WorldBuilder, scenarios).</summary>
        /// <param name="entries">The new entries, in diet order; they replace the old ones.</param>
        /// <returns>This diet, to chain calls.</returns>
        /// <remarks>Call it before the world initializes, or call <see cref="Resolve"/> afterwards.</remarks>
        public Diet Set(params Entry[] entries)
        {
            eats = new List<Entry>(entries);
            return this;
        }

        /// <summary>Sets the entries by name, scale 1.</summary>
        /// <param name="targets">The targets, in diet order, such as <c>"Grass"</c>, <c>"prey"</c> or <c>"carcass:prey"</c>.</param>
        /// <returns>This diet, to chain calls.</returns>
        public Diet Set(params string[] targets)
        {
            eats = new List<Entry>();
            foreach (var t in targets) eats.Add(new Entry(t));
            return this;
        }

        /// <summary>
        /// Sets the kill-chance setting, the default of the <c>kill.chance</c> trait, before the species declares its
        /// traits.
        /// </summary>
        /// <param name="p">A probability; clamped to [0, 1].</param>
        public void SetKillChance(float p) => killChance = Mathf.Clamp01(p);

        /// <summary>
        /// V-24's fix for a species, its carcasses or a layer: enable the target's Edible, or add one (graze for layers,
        /// strike for species); for carcasses, let the species leave some (when the world has a carcass system).
        /// </summary>
        static ValidationFix AddEdible(EdibleTarget t)
        {
            bool carcass = t.Kind == EdibleTargetKind.Carcass, layer = t.Kind == EdibleTargetKind.Layer;
            GameObject target = (t.Kind == EdibleTargetKind.Species || (carcass && t.Entities != null)) && t.Species != null ? t.Species.gameObject
                              : layer && t.Layer != null ? t.Layer.gameObject : null;
            if (target == null) return null;
            return new ValidationFix(carcass ? "Let it leave carcasses" : "Add an Edible", () =>
            {
                if (!target.TryGetComponent<Edible>(out var e)) e = target.AddComponent<Edible>().Configure(layer ? EatMethod.Graze : EatMethod.Strike, 25f);
                e.enabled = true;
                if (carcass && e.carcassPortions < 1) e.carcassPortions = 2;
            });
        }

        /// <summary>
        /// Reports entries that name nothing (V-20), targets without an Edible (V-24, with a fix) and a diet with no food
        /// source at all (V-22).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            foreach (var name in unresolved)
                report.Error("V-20", this, $"Diet entry '{name}' names no layer, species or entity kind.");
            foreach (var t in resolved)
                if (t.Edible == null)
                    report.Error("V-24", this, $"Diet entry '{t.Name}' can never be eaten: its target has no Edible component (SPEC-10).", AddEdible(t));
            if (resolved.Count == 0)
                report.Warning("V-22", this, $"Species '{Species.DisplayName}' has no food source in its diet.");
        }
    }
}
