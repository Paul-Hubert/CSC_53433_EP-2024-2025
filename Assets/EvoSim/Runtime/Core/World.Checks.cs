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
