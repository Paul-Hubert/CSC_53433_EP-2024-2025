using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>World-wide checks that look across species: edibles nobody eats (V-25), stale species names in genes (V-23).</summary>
    public partial class World
    {
        void ValidateAcrossSpecies(ValidationReport report)
        {
            ValidateEdibles(report);
            ValidateSpeciesWords(report);
        }

        /// <summary>V-61 (OUT-04): no key, token or password in a serialized string of the World's components, enabled or not.</summary>
        void ValidateSecrets(ValidationReport report)
        {
            var reported = new HashSet<UnityEngine.Object>();                     // components that checked their own fields
            foreach (var m in report.Messages) if (m.Id == "V-61" && m.Object != null) reported.Add(m.Object);
            foreach (var c in GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
                if ((c is World || c is WorldModule || c is SpeciesModule || c is Species || c is Edible) && !reported.Contains(c))
                    Secrets.CheckFields(report, c);
        }

        /// <summary>V-26: a module whose [RequiresModule] names a module that isn't there, unless the module reported an error itself.</summary>
        void ValidateCompanions(Species s, ValidationReport report)
        {
            foreach (var m in s.Modules)
            {
                var needs = (RequiresModuleAttribute[])m.GetType().GetCustomAttributes(typeof(RequiresModuleAttribute), true);
                Array.Sort(needs, (x, y) => string.CompareOrdinal(x.Module?.FullName, y.Module?.FullName));   // the attributes' order isn't defined
                foreach (var need in needs)
                {
                    if (need.Module == null || HasModule(s, need.Module) || ReportedError(report, m)) continue;
                    bool onWorld = typeof(WorldModule).IsAssignableFrom(need.Module);
                    report.Warning("V-26", m, $"{m.GetType().Name} needs a {need.Module.Name} {(onWorld ? "under the World" : $"on '{s.DisplayName}'")}.",
                                   AddModuleFix(need.Module, onWorld ? transform : s.transform));
                }
            }
        }

        bool HasModule(Species s, Type t)
        {
            foreach (var m in s.Modules) if (t.IsInstanceOfType(m)) return true;
            foreach (var m in modules) if (t.IsInstanceOfType(m)) return true;
            return false;
        }

        static bool ReportedError(ValidationReport report, UnityEngine.Object o)
        {
            foreach (var m in report.Messages) if (m.Object == o && m.Severity == Severity.Error) return true;
            return false;
        }

        /// <summary>A fix that adds a module of this type on a child named after it; none for an abstract type.</summary>
        static ValidationFix AddModuleFix(Type t, UnityEngine.Transform parent)
        {
            if (t.IsAbstract || !typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(t)) return null;
            return new ValidationFix("Add " + t.Name, () =>
            {
                var go = new UnityEngine.GameObject(t.Name);
                go.transform.SetParent(parent, false);
                go.AddComponent(t);
            });
        }

        /// <summary>V-25 (info): an edible layer or species that no diet lists.</summary>
        void ValidateEdibles(ValidationReport report)
        {
            foreach (var service in services)
            {
                if (!(service is ResourceLayer layer) || layer.Edible == null) continue;
                if (!Eaten(t => t.Kind == EdibleTargetKind.Layer && t.Layer == layer))
                    report.Info("V-25", layer.Edible, $"'{layer.LayerName}' is edible but no diet lists it.");
            }
            foreach (var s in species)
            {
                var e = s.GetComponent<Edible>();
                if (e == null || !e.enabled) continue;
                if (!Eaten(t => (t.Kind == EdibleTargetKind.Species || t.Kind == EdibleTargetKind.Carcass) && t.Names(s)))
                    report.Info("V-25", e, $"'{s.DisplayName}' is edible but no diet lists it or its carcasses.");
            }
        }

        bool Eaten(Func<EdibleTarget, bool> match)
        {
            foreach (var s in species)
            {
                var diet = s.Module<Diet>();
                if (diet == null) continue;
                foreach (var t in diet.Resolved) if (match(t)) return true;
            }
            return false;
        }

        /// <summary>V-23 (warning): a founder sentence names an animal that is no species of this world (SPEC-21).</summary>
        void ValidateSpeciesWords(ValidationReport report)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in species)
                foreach (var n in new[] { s.Id, s.DisplayName })
                {
                    if (string.IsNullOrEmpty(n)) continue;
                    string w = n.ToLowerInvariant();
                    known.Add(w);
                    known.Add(w + "s");
                    known.Add(w + "es");
                    if (w.EndsWith("f")) known.Add(w.Substring(0, w.Length - 1) + "ves");
                }
            foreach (var s in species)
                foreach (var g in s.Genes)
                {
                    if (g.Kind != AlleleKind.Text) continue;
                    foreach (var f in g.FounderPool)
                    {
                        string text = f.Value.ToString();
                        foreach (var word in Words(text))
                        {
                            if (!AnimalWords.Contains(word) || known.Contains(word)) continue;
                            report.Warning("V-23", g, $"Founder sentence \"{text}\" of '{s.DisplayName}' names '{word}', which is no species of this world (SPEC-21).");
                            break;
                        }
                    }
                }
        }

        static IEnumerable<string> Words(string text)
        {
            int start = -1;
            for (int i = 0; i <= text.Length; i++)
            {
                bool letter = i < text.Length && char.IsLetter(text[i]);
                if (letter && start < 0) start = i;
                else if (!letter && start >= 0)
                {
                    yield return text.Substring(start, i - start).ToLowerInvariant();
                    start = -1;
                }
            }
        }
    }
}
