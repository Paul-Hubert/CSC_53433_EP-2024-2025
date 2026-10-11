using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Reads and overrides any serialized setting by path (CFG-02): "&lt;GameObject path&gt;/&lt;Component&gt;/&lt;field&gt;",
    /// relative to the World, e.g. "Prey/Litter/max", "World/decisionPeriod", "prey/Actions/flee/FleeAction/fleeIntoCover".
    /// Names are matched without case; a component is found by its class or a base class, on the GameObject or below it.
    /// </summary>
    /// <remarks>
    /// <para>How a path is read: the last part is the field, the one before it the component, the rest a GameObject path
    /// under the World. A GameObject matches by name, or a species by its id or display name. When the full GameObject path
    /// has no such component, shorter ones are tried, down to the World itself; the first component found below wins
    /// (depth-first). Failing a class name, the component part may be a GameObject's name: the first species module on that
    /// GameObject is taken. <c>World/x</c> is the World's own field.</para>
    /// <para>Only serialized fields count (public or <c>[SerializeField]</c>, not read-only): what the inspector shows.
    /// Values convert from text in the invariant culture: string, int, long, float, double, bool, any enum (by name, any
    /// case) and float arrays (<c>[8, 4]</c>).</para>
    /// </remarks>
    public static class FieldPath
    {
        /// <summary>The fields a type declares itself, public or not: base classes are walked one by one.</summary>
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>Sets the field at the path from its text value; false with a reason if the path or value is wrong.</summary>
        /// <remarks>Call it before the World initialises: modules read their settings then. It skips the inspector's limits.</remarks>
        /// <param name="world">The World the path starts from.</param>
        /// <param name="path">The setting's path, e.g. <c>Prey/Litter/max</c>.</param>
        /// <param name="value">The new value as text.</param>
        /// <param name="error">Why it failed (no such GameObject, component or field; a value that doesn't convert), or null.</param>
        /// <returns>True when the field was set.</returns>
        public static bool Set(World world, string path, string value, out string error)
        {
            error = null;
            if (!Resolve(world, path, out var target, out var field, out error)) return false;
            try
            {
                field.SetValue(target, Convert(value, field.FieldType));
                return true;
            }
            catch (Exception e)
            {
                error = $"{path}: can't set '{value}' ({e.Message})";
                return false;
            }
        }

        /// <summary>Deactivates the GameObject at a hierarchy path under the World (scenario "remove", S06).</summary>
        /// <param name="world">The World the path starts from.</param>
        /// <param name="path">A GameObject path under the World, e.g. <c>prey/Actions/hide</c>.</param>
        /// <param name="error">Why it failed (no such GameObject), or null.</param>
        /// <returns>True when the GameObject was found and switched off.</returns>
        public static bool Remove(World world, string path, out string error)
        {
            error = null;
            var t = FindChild(world.transform, path.Split('/'), 0, path.Split('/').Length);
            if (t == null) { error = $"{path}: no such GameObject"; return false; }
            t.gameObject.SetActive(false);
            return true;
        }

        /// <summary>The current value of the field at the path, as text.</summary>
        /// <param name="world">The World the path starts from.</param>
        /// <param name="path">The setting's path, e.g. <c>World/decisionPeriod</c>.</param>
        /// <returns>The value as <see cref="Format"/> writes it, or null when the path leads nowhere.</returns>
        public static string Get(World world, string path)
        {
            if (!Resolve(world, path, out var target, out var field, out _)) return null;
            return Format(field.GetValue(target));
        }

        /// <summary>Finds the object and the serialized field a path names (see the class remarks); false with a reason otherwise.</summary>
        static bool Resolve(World world, string path, out object target, out FieldInfo field, out string error)
        {
            target = null; field = null; error = null;
            var parts = path.Split('/');
            if (parts.Length < 2) { error = $"{path}: needs at least <component>/<field>"; return false; }
            string fieldName = parts[parts.Length - 1];
            if (parts.Length == 2 && parts[0].Equals("World", StringComparison.OrdinalIgnoreCase))
            {
                target = world;
                field = FindField(world.GetType(), fieldName);
                if (field == null) error = $"{path}: the World has no field '{fieldName}'";
                return field != null;
            }
            string componentName = parts[parts.Length - 2];
            // The longest prefix that is a GameObject path under the World, then the component below it.
            for (int goParts = parts.Length - 2; goParts >= 0; goParts--)
            {
                var t = goParts == 0 ? world.transform : FindChild(world.transform, parts, 0, goParts);
                if (t == null) continue;
                var c = FindComponent(t, componentName);
                if (c == null) continue;
                target = c;
                field = FindField(c.GetType(), fieldName);
                if (field == null) { error = $"{path}: {c.GetType().Name} has no field '{fieldName}'"; return false; }
                return true;
            }
            error = $"{path}: no component '{componentName}' found";
            return false;
        }

        /// <summary>Follows count parts of a path from root, child by child (first match at each level), or null when one is missing.</summary>
        static Transform FindChild(Transform root, string[] parts, int from, int count)
        {
            var t = root;
            for (int i = from; i < from + count; i++)
            {
                Transform next = null;
                for (int k = 0; k < t.childCount && next == null; k++)
                    if (Matches(t.GetChild(k), parts[i])) next = t.GetChild(k);
                if (next == null) return null;
                t = next;
            }
            return t;
        }

        /// <summary>True when a GameObject has the name, or holds a species with that id or display name (any case).</summary>
        static bool Matches(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            var s = t.GetComponent<Species>();
            return s != null && (s.RequestedId.Equals(name, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The first component on t or below it whose class, or a base class, has the name; else the first species module on a
        /// GameObject of that name; else null.
        /// </summary>
        static Component FindComponent(Transform t, string name)
        {
            foreach (var c in t.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                for (var type = c.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                    if (type.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return c;
            }
            foreach (var c in t.GetComponentsInChildren<MonoBehaviour>(true))
                if (c != null && c.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase) && c is SpeciesModule) return c;
            return null;
        }

        /// <summary>The serialized field of that name (any case) on the type or a base class below MonoBehaviour, or null.</summary>
        static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(Fields))
                    if (f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && IsSerialized(f)) return f;
            return null;
        }

        /// <summary>Public or [SerializeField], not [NonSerialized]: what the inspector shows.</summary>
        /// <param name="f">A field.</param>
        /// <returns>True when the field is public or marked [SerializeField], and neither [NonSerialized] nor read-only.</returns>
        public static bool IsSerialized(FieldInfo f) =>
            !f.IsNotSerialized && !f.IsInitOnly && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);

        /// <summary>Parses a text value into the field's type (invariant culture); throws when the text or the type doesn't fit.</summary>
        static object Convert(string value, Type type)
        {
            if (type == typeof(string)) return value;
            if (type == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(long)) return long.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type.IsEnum) return Enum.Parse(type, value, true);
            if (type.IsArray && type.GetElementType() == typeof(float))
            {
                var parts = value.Trim('[', ']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var arr = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++) arr[i] = float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
                return arr;
            }
            throw new NotSupportedException($"values of type {type.Name}");
        }

        /// <summary>A value as text (for run_info.json and the inspector).</summary>
        /// <param name="v">The value.</param>
        /// <returns>
        /// "" for null; floats and doubles in round-trip form; a Unity object by its name; a list as <c>[a, b]</c>; anything
        /// else as its invariant text.
        /// </returns>
        public static string Format(object v)
        {
            switch (v)
            {
                case null: return "";
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case UnityEngine.Object o: return o != null ? o.name : "";
                case string s: return s;
                case IEnumerable e:
                    var sb = new StringBuilder("[");
                    bool first = true;
                    foreach (var x in e) { if (!first) sb.Append(", "); first = false; sb.Append(Format(x)); }
                    return sb.Append(']').ToString();
                default: return System.Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Every serialized setting of every component under the World, by path (CFG-03).</summary>
        /// <remarks>
        /// Covers the World, its modules, species, species modules and Edibles that are active and enabled. Keys are
        /// <c>&lt;GameObject path&gt;/&lt;class&gt;/&lt;field&gt;</c>; values are numbers, strings, bools, enum names, Unity
        /// objects by name, lists, or dictionaries for serialized classes. <see cref="RunInfo"/> writes it as <c>settings</c>.
        /// </remarks>
        /// <param name="world">The World.</param>
        /// <returns>The settings, keys in ordinal order.</returns>
        public static SortedDictionary<string, object> Snapshot(World world)
        {
            var result = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var c in world.GetComponentsInChildren<MonoBehaviour>(false))
            {
                if (c == null || !(c is World || c is WorldModule || c is SpeciesModule || c is Species || c is Edible)) continue;
                if (!Ownership.IsEnabled(c)) continue;
                string goPath = PathUnder(world.transform, c.transform);
                for (var t = c.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                    foreach (var f in t.GetFields(Fields))
                        if (IsSerialized(f)) result[(goPath.Length > 0 ? goPath + "/" : "") + c.GetType().Name + "/" + f.Name] = Value(f.GetValue(c));
            }
            return result;
        }

        /// <summary>A setting's value in a form canonical JSON writes: plain values as they are, enums and objects by name, lists and classes taken apart.</summary>
        static object Value(object v)
        {
            switch (v)
            {
                case null: return null;
                case string _: case bool _: case int _: case long _: case float _: case double _: return v;
                case Enum e: return e.ToString();
                case UnityEngine.Object o: return o != null ? o.name : null;
                case IEnumerable list:
                    var items = new List<object>();
                    foreach (var x in list) items.Add(Value(x));
                    return items;
                default:
                    var d = new SortedDictionary<string, object>(StringComparer.Ordinal);
                    foreach (var f in v.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        if (IsSerialized(f)) d[f.Name] = Value(f.GetValue(v));
                    return d;
            }
        }

        /// <summary>The GameObject path of t under root, names joined by "/"; "" for root itself.</summary>
        static string PathUnder(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }
    }
}
