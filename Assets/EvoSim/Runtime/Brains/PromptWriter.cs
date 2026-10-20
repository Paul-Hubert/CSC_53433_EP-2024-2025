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
    /// {name} and {species.name} are display names. A placeholder reads a field or a property; {{ and }} write a brace.
    /// A module is named by its class, a base class, or its GameObject.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Build"/> makes one writer per species when the run starts, and hands it to every module's
    /// <see cref="SpeciesModule.WritePromptRules"/> in hierarchy order (so action lines come in action order), then to every
    /// world service's <see cref="WorldService.WritePromptRules"/>. A module calls <see cref="Action"/> or
    /// <see cref="Rule"/>; it writes nothing else.</para>
    /// <para>Placeholders keep the prompt true to the world: change a speed in the Inspector and the prompt says the new
    /// number (PROMPT-03). The species' header and genes intro, and a frozen prompt, are filled with no writing module:
    /// there only {name} and the qualified forms work. {genes}, {situation} and {ask} are left for
    /// <see cref="PromptTemplate.Fill"/>. An unknown placeholder stays in the text and is reported (V-53).</para>
    /// </remarks>
    /// <example>
    /// A module writes its Inspector text as a rule; the reference prey locomotion's text begins
    /// <c>"Animals move {walkSpeed} meter per step. Predators run {predator.Locomotion.runSpeed} meters per step when they hunt,"</c>
    /// and goes on with <c>"{predator.Stamina.max} stamina against an animal's {Stamina.max}"</c>. The sample <c>Thirst</c>
    /// writes its number itself:
    /// <code>
    /// public override void WritePromptRules(PromptWriter w) =>
    ///     w.Rule($"Thirst grows every step; an animal dies of thirst at {Bands.Number(deadly)}. Drinking resets it.");
    /// </code>
    /// </example>
    public sealed class PromptWriter
    {
        /// <summary>Matches "{{", "}}", or a placeholder of one to three dotted names (letters, digits, '_' and spaces).</summary>
        static readonly Regex Placeholder = new Regex(@"\{\{|\}\}|\{([A-Za-z_][\w ]*(?:\.[A-Za-z_][\w ]*){0,2})\}");

        readonly List<string> actions = new List<string>();    // the action lines, "- name: description", in call order
        readonly List<string> rules = new List<string>();      // the rule lines, placeholders filled, in call order
        readonly List<string> problems = new List<string>();

        /// <summary>The species whose prompt is being written: {name} is its display name, {Module.field} reads its modules.</summary>
        public Species Species { get; }

        /// <summary>The module writing now: {field} placeholders read it.</summary>
        public object Current { get; internal set; }

        /// <summary>The action lines written so far, "- eat: go to the nearest visible food and eat it".</summary>
        public IReadOnlyList<string> ActionLines => actions;

        /// <summary>The rule lines written so far, placeholders filled.</summary>
        public IReadOnlyList<string> RuleLines => rules;

        /// <summary>Unknown placeholders found while writing (V-53).</summary>
        public IReadOnlyList<string> Problems => problems;

        /// <summary>Makes an empty writer for a species. <see cref="Build"/> makes one; tools and tests may too.</summary>
        /// <param name="species">The species whose prompt is written.</param>
        public PromptWriter(Species species) { Species = species; }

        /// <summary>Adds one line of the action list: "- name: description" (PROMPT-01).</summary>
        /// <param name="name">The action's name, as the brain reads it ("eat").</param>
        /// <param name="description">What the action does, from its Inspector text; its placeholders are filled.</param>
        /// <remarks>Every action writes its own line (<c>AnimalAction.WritePromptRules</c>); other modules write rules.</remarks>
        public void Action(string name, string description) => actions.Add("- " + name + ": " + Fill(description));

        /// <summary>Adds one rule line, its placeholders filled.</summary>
        /// <param name="line">The rule, usually a module's Inspector text; it may hold several lines. Null, empty or blank adds
        /// nothing, so a module with an empty rule text writes nothing.</param>
        public void Rule(string line)
        {
            if (!string.IsNullOrWhiteSpace(line)) rules.Add(Fill(line));
        }

        /// <summary>Fills {placeholders} from module settings; unknown ones are kept and reported.</summary>
        /// <param name="text">A text with placeholders, or null.</param>
        /// <returns>The text with each placeholder replaced by the value it names, "{{" and "}}" by single braces; {genes},
        /// {situation}, {ask} and unknown placeholders unchanged.</returns>
        public string Fill(string text)
        {
            if (string.IsNullOrEmpty(text) || (text.IndexOf('{') < 0 && text.IndexOf('}') < 0)) return text;
            return Placeholder.Replace(text, m =>
            {
                if (m.Value == "{{") return "{";
                if (m.Value == "}}") return "}";
                string path = m.Groups[1].Value;
                if (path == "genes" || path == "situation" || path == "ask") return m.Value;
                if (TryResolve(path, out var value)) return value;
                problems.Add($"unknown placeholder {m.Value}");
                return m.Value;
            });
        }

        /// <summary>
        /// Finds the value of a placeholder path: "name" or "field" (the writing module); "Module.field", or "species.name" /
        /// "species.id"; "species.Module.field". False when nothing matches.
        /// </summary>
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
                // A module of this species wins; else the first part may name another species ({predator.name}).
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

        /// <summary>
        /// The first module of a species whose class, a base class below SpeciesModule, or GameObject has this name; or null.
        /// </summary>
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

        /// <summary>
        /// Reads a field, else a property (public or private, of the class or a base class), formatted as the brain reads it.
        /// </summary>
        /// <param name="target">The module or object to read.</param>
        /// <param name="field">The field's or property's name, exactly as in the code ("runSpeed", "max").</param>
        /// <param name="value">The value as text: numbers as <see cref="Bands.Number"/> writes them, booleans "yes" / "no";
        /// null when not found.</param>
        /// <returns>True when a readable field or property was found and its getter did not throw.</returns>
        public static bool TryRead(object target, string field, out string value)
        {
            value = null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = target.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(field, flags);
                if (f != null) { value = Format(f.GetValue(target)); return true; }
                var p = t.GetProperty(field, flags);
                if (p == null || !p.CanRead || p.GetIndexParameters().Length > 0) continue;
                try { value = Format(p.GetValue(target)); }
                catch (TargetInvocationException) { return false; }                          // a getter that throws: reported as unknown (V-53)
                return true;
            }
            return false;
        }

        /// <summary>A setting's value as the brain reads it: "2.5", "4", "yes" / "no", or its plain text.</summary>
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
        /// The engine's rule line for a species with overlays (ACT-07, PROMPT-03): "Some actions are done in passing
        /// (alarm): the animal also does its next most likely choice."; null when the species has none.
        /// </summary>
        /// <param name="s">The species, its actions found.</param>
        /// <returns>The line, the overlays named in action order, or null.</returns>
        public static string OverlayRule(Species s)
        {
            List<string> names = null;
            foreach (var a in s.Actions)
                if (a.Overlay) (names ??= new List<string>()).Add(a.Name);
            return names == null ? null
                 : $"Some actions are done in passing ({string.Join(", ", names)}): the animal also does its next most likely choice.";
        }

        /// <summary>
        /// The species' prompt template (PROMPT-01): header, actions, rules from its modules and from world services,
        /// the genes intro and {genes}, the situation and {ask}; or its frozen prompt (PROMPT-04).
        /// </summary>
        /// <param name="s">The species, its modules discovered.</param>
        /// <returns>The template and the problems found (unknown placeholders, holes missing from a frozen prompt).</returns>
        /// <remarks>Called by the World for each species when the run starts and when a species is added (SPEC-30), and by
        /// <see cref="Species.RebuildPrompt"/>. With a frozen prompt the modules still write, so their problems are reported,
        /// but only the frozen text is used.</remarks>
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
            string passing = OverlayRule(s);
            if (passing != null) w.rules.Insert(0, passing);                    // ACT-07, PROMPT-03: first rule, below the actions

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
