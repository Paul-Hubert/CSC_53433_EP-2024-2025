using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// Names the matched pairs of a comparison world (plan M16 D7, §6.2): a JEV species and the neural species built to
    /// match it. Validation lists every body or life setting in which a pair differs (V-78), and every way the world treats
    /// the two differently: a diet or a cover that names one of the pair but not the other (V-79).
    /// </summary>
    /// <remarks>
    /// <para>The neural templates (<see cref="NeuralTemplates"/>, <c>GameObject ▸ EvoSim ▸ New Neural Species</c>) add the
    /// pair here. Only the controller may differ: senses, actions, genes, mutation, crossover, the search rule, the brain,
    /// the decision schedule, the body's look, prompt texts, and any module of <c>EvoSim.Neuro</c>.</para>
    /// <para>V-78 compares, module by module of the same class and in hierarchy order, every serialized number, switch and
    /// enum (not texts or references), the species' initial population and <see cref="Edible"/>, and the diets: each entry of the JEV diet must be
    /// in the neural diet, under its own name or its pair's, with the same energy scale.</para>
    /// </remarks>
    public class MatchedPairs : WorldModule
    {
        /// <summary>One pair: the JEV species and its neural match, by id.</summary>
        [Serializable]
        public class Pair
        {
            /// <summary>The JEV species' id (e.g. <c>prey</c>).</summary>
            public string jev = "";
            /// <summary>The neural species' id (e.g. <c>prey-nn</c>).</summary>
            public string neural = "";

            /// <summary>An empty pair (for Unity's serialisation).</summary>
            public Pair() { }
            /// <summary>A pair of species ids.</summary>
            /// <param name="jevId">The JEV species' id.</param>
            /// <param name="neuralId">The neural species' id.</param>
            public Pair(string jevId, string neuralId) { jev = jevId; neural = neuralId; }
        }

        /// <summary>The flags of the serialized fields <see cref="Differences"/> reads, base classes walked one by one.</summary>
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [SerializeField, Tooltip("The matched pairs: a JEV species and its neural match, by id (plan M16 D7).")]
        List<Pair> pairs = new List<Pair>();

        /// <summary>The pairs, in the Inspector's order.</summary>
        public IReadOnlyList<Pair> Pairs => pairs;

        /// <summary>Adds a pair, or replaces the pair of that neural species (templates, tests).</summary>
        /// <param name="jevId">The JEV species' id.</param>
        /// <param name="neuralId">The neural species' id.</param>
        /// <returns>This module.</returns>
        public MatchedPairs Add(string jevId, string neuralId)
        {
            pairs.RemoveAll(p => p.neural == neuralId);
            pairs.Add(new Pair(jevId, neuralId));
            return this;
        }

        /// <summary>The partner of a species id in the pairs (either side), or null.</summary>
        /// <param name="id">A species id.</param>
        /// <returns>The id it is paired with, or null when it is in no pair.</returns>
        public string PartnerOf(string id)
        {
            foreach (var p in pairs)
            {
                if (p.jev == id) return p.neural;
                if (p.neural == id) return p.jev;
            }
            return null;
        }

        /// <summary>V-78 for each pair (with a pair that names no species), then V-79.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            foreach (var p in pairs)
            {
                var j = World.FindSpecies(p.jev);
                var n = World.FindSpecies(p.neural);
                if (j == null || n == null)
                {
                    report.Warning("V-78", this, $"Matched pair {p.jev} / {p.neural}: '{(j == null ? p.jev : p.neural)}' names no species of this world.");
                    continue;
                }
                var diffs = Differences(j, n);
                if (diffs.Count > 0)
                    report.Warning("V-78", this, $"'{n.DisplayName}' differs from its match '{j.DisplayName}' in: {string.Join("; ", diffs)}.");
                foreach (var line in Treatment(j, n))
                    report.Warning("V-79", this, $"The world treats '{j.DisplayName}' and '{n.DisplayName}' differently: {line}.");
            }
        }

        /// <summary>
        /// The body and life settings in which <paramref name="neural"/> differs from <paramref name="jev"/>, one line each
        /// (<c>Locomotion/KinematicLocomotion.runSpeed 2 ≠ 3</c>); empty when matched.
        /// </summary>
        /// <param name="jev">The JEV species.</param>
        /// <param name="neural">The neural species.</param>
        /// <returns>The differences, in the JEV species' hierarchy order.</returns>
        public List<string> Differences(Species jev, Species neural)
        {
            var diffs = new List<string>();
            if (jev.InitialPopulation != neural.InitialPopulation)
                diffs.Add($"initial population {jev.InitialPopulation} ≠ {neural.InitialPopulation}");
            var ej = jev.GetComponent<Edible>();
            var en = neural.GetComponent<Edible>();
            if ((ej != null) != (en != null)) diffs.Add(ej != null ? "Edible: missing" : $"Edible: not in '{jev.DisplayName}'");
            else if (ej != null) CompareFields(ej, en, "Edible", diffs);
            var theirs = new Dictionary<Type, Queue<SpeciesModule>>();
            foreach (var m in neural.ModulesOf<SpeciesModule>())
            {
                if (IsController(m)) continue;
                if (!theirs.TryGetValue(m.GetType(), out var q)) theirs[m.GetType()] = q = new Queue<SpeciesModule>();
                q.Enqueue(m);
            }
            foreach (var m in jev.ModulesOf<SpeciesModule>())
            {
                if (IsController(m)) continue;
                string where = Where(m, jev);
                if (!theirs.TryGetValue(m.GetType(), out var q) || q.Count == 0) { diffs.Add($"{where}: missing"); continue; }
                var other = q.Dequeue();
                CompareFields(m, other, where, diffs);
                if (m is Diet d) CompareDiets(d, (Diet)other, diffs);
            }
            foreach (var kv in theirs)
                foreach (var extra in kv.Value) diffs.Add($"{Where(extra, neural)}: not in '{jev.DisplayName}'");
            return diffs;
        }

        /// <summary>
        /// The ways the world treats a pair differently (V-79): a species' diet that eats, or scavenges, one of the two but
        /// not the other, and a cover that hides one from a species but not the other.
        /// </summary>
        /// <param name="jev">The JEV species.</param>
        /// <param name="neural">The neural species.</param>
        /// <returns>One line per difference; empty when treated alike.</returns>
        public List<string> Treatment(Species jev, Species neural)
        {
            var lines = new List<string>();
            foreach (var s in World.AllSpecies)
            {
                var diet = s.Module<Diet>();
                if (diet == null) continue;
                bool strikesJ = false, strikesN = false, scavJ = false, scavN = false;
                foreach (var t in diet.Resolved)
                {
                    if (t.Kind == EdibleTargetKind.Species) { strikesJ |= t.Species == jev; strikesN |= t.Species == neural; }
                    if (t.Kind == EdibleTargetKind.Carcass) { scavJ |= t.Species == jev; scavN |= t.Species == neural; }
                }
                if (strikesJ != strikesN)
                    lines.Add($"the diet of '{s.DisplayName}' eats '{(strikesJ ? jev : neural).DisplayName}' but not '{(strikesJ ? neural : jev).DisplayName}'");
                if (scavJ != scavN)
                    lines.Add($"the diet of '{s.DisplayName}' scavenges '{(scavJ ? jev : neural).DisplayName}' but not '{(scavJ ? neural : jev).DisplayName}'");
            }
            foreach (var c in World.ServicesOf<Cover>())
                foreach (var s in World.AllSpecies)
                {
                    if (s == jev || s == neural) continue;
                    bool hj = c.Hides(jev, s), hn = c.Hides(neural, s);
                    if (hj != hn)
                        lines.Add($"cover '{c.name}' hides '{(hj ? jev : neural).DisplayName}' from '{s.DisplayName}' but not '{(hj ? neural : jev).DisplayName}'");
                }
            return lines;
        }

        /// <summary>
        /// True for a controller part, which may differ between the pair: senses, actions, genes, mutation, crossover, the
        /// search rule, the decision schedule, the body's look, and every module of the <c>EvoSim.Neuro</c> assembly.
        /// </summary>
        /// <param name="m">A module of either species.</param>
        /// <returns>True when V-78 skips it.</returns>
        public static bool IsController(SpeciesModule m) =>
            m is Sense || m is AnimalAction || m is Gene || m is MutationOperator || m is Crossover || m is SearchRule ||
            m is DecisionSchedule || m is Body || m.GetType().Assembly == typeof(MatchedPairs).Assembly;

        /// <summary>A module's place for a message: its GameObject path below the species, then its class.</summary>
        static string Where(Component m, Species s)
        {
            string path = "";
            for (var t = m.transform; t != null && t != s.transform; t = t.parent) path = path.Length == 0 ? t.name : t.name + "/" + path;
            return (path.Length > 0 ? path + "/" : "") + m.GetType().Name;
        }

        /// <summary>Adds a line for each serialized number, switch, enum or float array that differs between two modules.</summary>
        static void CompareFields(object a, object b, string where, List<string> diffs)
        {
            for (var type = a.GetType(); type != null && type != typeof(SpeciesModule) && type != typeof(MonoBehaviour); type = type.BaseType)
                foreach (var f in type.GetFields(Fields))
                {
                    if (!(f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) || f.IsNotSerialized) continue;
                    var ft = f.FieldType;
                    if (!(ft.IsPrimitive || ft.IsEnum || ft == typeof(float[]))) continue;
                    string x = Text(f.GetValue(a)), y = Text(f.GetValue(b));
                    if (x != y) diffs.Add($"{where}.{f.Name} {x} ≠ {y}");
                }
        }

        /// <summary>
        /// Each entry of the JEV diet must be in the neural diet, under its own name or its pair's (<c>carcass:prey</c> or
        /// <c>carcass:prey-nn</c>), with the same energy scale.
        /// </summary>
        void CompareDiets(Diet jev, Diet neural, List<string> diffs)
        {
            foreach (var e in jev.Entries)
            {
                Diet.Entry match = null;
                string mapped = Mapped(e.target);
                foreach (var o in neural.Entries)
                    if (Same(o.target, e.target) || Same(o.target, mapped)) { match = o; break; }
                if (match == null) diffs.Add($"diet: no entry for '{e.target}'");
                else if (!Mathf.Approximately(match.energyScale, e.energyScale))
                    diffs.Add($"diet '{e.target}' energy scale {Text(e.energyScale)} ≠ {Text(match.energyScale)}");
            }
        }

        /// <summary>A diet target with its species replaced by its pair (<c>carcass:prey</c> → <c>carcass:prey-nn</c>), or null.</summary>
        string Mapped(string target)
        {
            string name = (target ?? "").Trim();
            int colon = name.IndexOf(':');
            string kind = colon > 0 ? name.Substring(0, colon + 1) : "", of = colon > 0 ? name.Substring(colon + 1).Trim() : name;
            var s = World != null ? World.FindSpeciesByName(of) : null;
            string partner = PartnerOf(s != null ? s.Id : of);
            return partner == null ? null : kind + partner;
        }

        /// <summary>Two diet targets name the same thing, spaces aside.</summary>
        static bool Same(string a, string b) => a != null && b != null && a.Trim() == b.Trim();

        /// <summary>A value as invariant text: floats to 4 decimals, arrays element by element.</summary>
        static string Text(object v)
        {
            switch (v)
            {
                case null: return "null";
                case float f: return f.ToString("0.####", CultureInfo.InvariantCulture);
                case double d: return d.ToString("0.####", CultureInfo.InvariantCulture);
                case float[] arr:
                    var parts = new string[arr.Length];
                    for (int i = 0; i < arr.Length; i++) parts[i] = Text(arr[i]);
                    return "[" + string.Join(", ", parts) + "]";
                default: return Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }
    }
}
