using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EvoSim
{
    /// <summary>
    /// Collects the action lines and rule lines that modules contribute to a species' prompt (PROMPT-01). Numbers in
    /// rule lines come from placeholders filled from module settings (PROMPT-03): {field} on the writing module,
    /// {Module.field} on another module of the species, {species.Module.field} on a module of another species;
    /// {name} and {species.name} are display names.
    /// A module is named by its class, a base class, or its GameObject.
    /// </summary>
    public sealed class PromptWriter
    {
        static readonly Regex Placeholder = new Regex(@"\{([A-Za-z_][\w ]*(?:\.[A-Za-z_][\w ]*){0,2})\}");

        readonly List<string> actions = new List<string>();
        readonly List<string> rules = new List<string>();
        readonly List<string> problems = new List<string>();

        public Species Species { get; }
        /// <summary>The module writing now: {field} placeholders read it.</summary>
        public object Current { get; internal set; }
        public IReadOnlyList<string> ActionLines => actions;
        public IReadOnlyList<string> RuleLines => rules;
        /// <summary>Unknown placeholders found while writing (V-53).</summary>
        public IReadOnlyList<string> Problems => problems;

        public PromptWriter(Species species) { Species = species; }

        /// <summary>One line of the action list: "- name: description".</summary>
        public void Action(string name, string description) => actions.Add("- " + name + ": " + Fill(description));

        /// <summary>One rule line, its placeholders filled.</summary>
        public void Rule(string line)
        {
            if (!string.IsNullOrWhiteSpace(line)) rules.Add(Fill(line));
        }

        /// <summary>Fills {placeholders} from module settings; unknown ones are kept and reported.</summary>
        public string Fill(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            return Placeholder.Replace(text, m =>
            {
                string path = m.Groups[1].Value;
                if (path == "genes" || path == "situation" || path == "ask") return m.Value;
                if (TryResolve(path, out var value)) return value;
                problems.Add($"unknown placeholder {m.Value}");
                return m.Value;
            });
        }

        bool TryResolve(string path, out string value)
        {
            value = null;
            var parts = path.Split('.');
            object target;
            string field;
            if (parts.Length == 1)
            {
                if (path == "name") { value = Species.DisplayName; return true; }
                target = Current; field = parts[0];
            }
            else if (parts.Length == 2)
            {
                target = FindModule(Species, parts[0]);
                field = parts[1];
                var other = target == null && Species.World != null ? Species.World.FindSpeciesByName(parts[0]) : null;
                if (other != null && (field == "name" || field == "id")) { value = field == "name" ? other.DisplayName : other.Id; return true; }
            }
            else
            {
                var other = Species.World != null ? Species.World.FindSpeciesByName(parts[0]) : null;
                target = other != null ? FindModule(other, parts[1]) : null;
                field = parts[2];
            }
            return target != null && TryRead(target, field, out value);
        }

        static object FindModule(Species s, string name)
        {
            if (s == null) return null;
            foreach (var m in s.Modules)
            {
                for (var t = m.GetType(); t != null && t != typeof(SpeciesModule); t = t.BaseType)
                    if (t.Name == name) return m;
                if (m.gameObject.name == name) return m;
            }
            return null;
        }

        /// <summary>Reads a field (public or private, of the class or a base class) and formats it as the brain reads it.</summary>
        public static bool TryRead(object target, string field, out string value)
        {
            value = null;
            for (var t = target.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f == null) continue;
                value = Format(f.GetValue(target));
                return true;
            }
            return false;
        }

        static string Format(object v)
        {
            switch (v)
            {
                case float f: return Bands.Number(f);
                case double d: return Bands.Number((float)d);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case bool b: return b ? "yes" : "no";
                default: return Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The species' prompt template (PROMPT-01): header, actions, rules from its modules and from world services,
        /// the genes intro and {genes}, the situation and {ask}; or its frozen prompt (PROMPT-04).
        /// </summary>
        public static PromptTemplate Build(Species s)
        {
            var w = new PromptWriter(s);
            foreach (var m in s.Modules)
            {
                w.Current = m;
                m.WritePromptRules(w);
            }
            if (s.World != null)
                foreach (var svc in s.World.Services)
                {
                    w.Current = svc;
                    svc.WritePromptRules(s, w);
                }
            w.Current = null;

            if (s.FrozenPrompt != null)
            {
                string frozen = s.FrozenPrompt.text.Replace("\r\n", "\n");
                foreach (var p in new[] { "{genes}", "{situation}", "{ask}" })
                    if (!frozen.Contains(p)) w.problems.Add($"the frozen prompt has no {p}");
                return new PromptTemplate(w.Fill(frozen), w.problems);
            }

            var sb = new StringBuilder();
            sb.Append(w.Fill(s.PromptHeader)).Append("\n\nActions:\n");
            foreach (var line in w.actions) sb.Append(line).Append('\n');
            foreach (var line in w.rules) sb.Append(line).Append('\n');
            sb.Append('\n').Append(w.Fill(s.GenesIntro)).Append("\n{genes}\n\nSituation: {situation}\n\n{ask}");
            return new PromptTemplate(sb.ToString(), w.problems);
        }
    }
}
