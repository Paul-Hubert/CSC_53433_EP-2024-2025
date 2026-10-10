using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What a species eats (SPEC-15): layers, species, "carcass:&lt;species&gt;", "egg:&lt;species&gt;". Each target must
    /// carry an Edible (SPEC-10, V-24). A species with no diet eats nothing.
    /// </summary>
    public class Diet : SpeciesModule
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("A layer, a species, carcass:prey, egg:prey...")]
            public string target = "Grass";
            [Min(0f), Tooltip("Multiplies the energy gained (default 1).")]
            public float energyScale = 1f;

            public Entry() { }
            public Entry(string target, float energyScale = 1f) { this.target = target; this.energyScale = energyScale; }
        }

        [SerializeField, Tooltip("What the species eats: layers, species, carcass:<species>, egg:<species>, with energy scales.")]
        List<Entry> eats = new List<Entry>();
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a strike kills (a trait of the hunter, reference 0.5; ACT-11).")]
        float killChance = 0.5f;

        readonly List<EdibleTarget> resolved = new List<EdibleTarget>();
        readonly List<string> unresolved = new List<string>();
        readonly Dictionary<(EdibleTarget, Species), EdibleTarget> forClone = new Dictionary<(EdibleTarget, Species), EdibleTarget>();   // look-ups only

        public IReadOnlyList<Entry> Entries => eats;
        /// <summary>The entries resolved at initialisation (ARCH-08), in diet order.</summary>
        public IReadOnlyList<EdibleTarget> Resolved => resolved;
        /// <summary>The kill-chance trait, declared when the diet strikes a species.</summary>
        public TraitId KillChance { get; private set; }
        public bool Strikes { get; private set; }

        public override void Declare(SpeciesBuilder b)
        {
            Resolve();
            if (Strikes || MayStrike()) KillChance = b.DeclareTrait("kill.chance", killChance, 0f, 1f);
        }

        /// <summary>An entry that names no layer and no kind may name a species added later (SPEC-30): it needs the kill chance too.</summary>
        bool MayStrike()
        {
            foreach (var e in eats)
            {
                string name = (e.target ?? "").Trim();
                if (name.Length > 0 && name.IndexOf(':') < 0 && World.Service<ResourceLayer>(name) == null) return true;
            }
            return false;
        }

        public override void Initialize() => Resolve();

        /// <summary>Resolves the entries by name (ARCH-08); called again when species are added during a run.</summary>
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
                    var c = w.Service<CarcassSystem>();
                    var edible = sp.GetComponent<Edible>();
                    bool leaves = edible != null && edible.enabled && edible.carcassPortions > 0;
                    return new EdibleTarget
                    {
                        Kind = EdibleTargetKind.Carcass, Name = name, Species = sp, Entities = c,
                        Edible = c != null && leaves ? edible : null, EnergyScale = e.energyScale
                    };
                }
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
        public virtual float KillChanceAgainst(Animal hunter, Animal prey) => KillChance.IsValid ? hunter.Trait(KillChance) : 0f;

        /// <summary>The energy a meal gives this eater: the entry's energy by default (ACT-11); override it to depend on the eater.</summary>
        public virtual float EnergyFrom(Animal eater, EdibleTarget entry, float energy) => energy;

        /// <summary>The entry for grazing this layer, if the diet lists it and it is edible (SPEC-13); else null.</summary>
        public virtual EdibleTarget Grazing(ResourceLayer layer)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Layer && t.Layer == layer && t.Edible != null) return t;
            return null;
        }

        /// <summary>The entry for striking this species, if listed and edible (SPEC-13); else null.</summary>
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
        public virtual EdibleTarget EatingEggs(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Egg && t.Names(s) && t.Edible != null) return t;
            return null;
        }

        /// <summary>The carcass system's line for a diet that scavenges carcasses (08 §7).</summary>
        public override void WritePromptRules(PromptWriter w)
        {
            var carcasses = World != null ? World.Service<CarcassSystem>() : null;
            if (carcasses == null) return;
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Carcass && t.Edible != null) { w.Rule(carcasses.PromptRuleFor(t)); return; }
        }

        /// <summary>Whether the diet has an edible entry of this kind.</summary>
        public bool Has(EdibleTargetKind kind)
        {
            foreach (var t in resolved) if (t.Kind == kind && t.Edible != null) return true;
            return false;
        }

        /// <summary>Sets the entries from code (WorldBuilder, scenarios).</summary>
        public Diet Set(params Entry[] entries)
        {
            eats = new List<Entry>(entries);
            return this;
        }

        /// <summary>Sets the entries by name, scale 1.</summary>
        public Diet Set(params string[] targets)
        {
            eats = new List<Entry>();
            foreach (var t in targets) eats.Add(new Entry(t));
            return this;
        }

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
