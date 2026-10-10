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

        [SerializeField] List<Entry> eats = new List<Entry>();
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a strike kills (a trait of the hunter, reference 0.5; ACT-11).")]
        float killChance = 0.5f;

        readonly List<EdibleTarget> resolved = new List<EdibleTarget>();
        readonly List<string> unresolved = new List<string>();

        public IReadOnlyList<Entry> Entries => eats;
        /// <summary>The entries resolved at initialisation (ARCH-08), in diet order.</summary>
        public IReadOnlyList<EdibleTarget> Resolved => resolved;
        /// <summary>The kill-chance trait, declared when the diet strikes a species.</summary>
        public TraitId KillChance { get; private set; }
        public bool Strikes { get; private set; }

        public override void Declare(SpeciesBuilder b)
        {
            Resolve();
            if (Strikes) KillChance = b.DeclareTrait("kill.chance", killChance, 0f, 1f);
        }

        public override void Initialize() => Resolve();

        /// <summary>Resolves the entries by name (ARCH-08); called again when species are added during a run.</summary>
        public void Resolve()
        {
            resolved.Clear();
            unresolved.Clear();
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

        /// <summary>The entry for grazing this layer, if the diet lists it and it is edible (SPEC-13); else null.</summary>
        public EdibleTarget Grazing(ResourceLayer layer)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Layer && t.Layer == layer && t.Edible != null) return t;
            return null;
        }

        /// <summary>The entry for striking this species, if listed and edible (SPEC-13); else null.</summary>
        public EdibleTarget Striking(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Species && t.Names(s) && t.Edible != null) return t;
            return null;
        }

        /// <summary>The entry for scavenging carcasses of this species, if listed and edible; else null.</summary>
        public EdibleTarget Scavenging(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Carcass && t.Names(s) && t.Edible != null) return t;
            return null;
        }

        /// <summary>The entry for eating eggs of this species, if listed and the egg kind is edible (REPRO-23); else null.</summary>
        public EdibleTarget EatingEggs(Species s)
        {
            foreach (var t in resolved)
                if (t.Kind == EdibleTargetKind.Egg && t.Names(s) && t.Edible != null) return t;
            return null;
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

        public override void Validate(ValidationReport report)
        {
            foreach (var name in unresolved)
                report.Error("V-20", this, $"Diet entry '{name}' names no layer, species or entity kind.");
            foreach (var t in resolved)
                if (t.Edible == null)
                    report.Error("V-24", this, $"Diet entry '{t.Name}' can never be eaten: its target has no Edible component (SPEC-10).");
            if (resolved.Count == 0)
                report.Warning("V-22", this, $"Species '{Species.DisplayName}' has no food source in its diet.");
        }
    }
}
