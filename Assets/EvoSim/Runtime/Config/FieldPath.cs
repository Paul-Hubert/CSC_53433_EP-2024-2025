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
    public static class FieldPath
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>Sets the field at the path from its text value; false with a reason if the path or value is wrong.</summary>
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
        public static bool Remove(World world, string path, out string error)
        {
            error = null;
            var t = FindChild(world.transform, path.Split('/'), 0, path.Split('/').Length);
            if (t == null) { error = $"{path}: no such GameObject"; return false; }
            t.gameObject.SetActive(false);
            return true;
        }

        /// <summary>The current value of the field at the path, as text.</summary>
        public static string Get(World world, string path)
        {
            if (!Resolve(world, path, out var target, out var field, out _)) return null;
            return Format(field.GetValue(target));
        }

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

        static bool Matches(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            var s = t.GetComponent<Species>();
            return s != null && (s.RequestedId.Equals(name, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

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

        static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(Fields))
                    if (f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && IsSerialized(f)) return f;
            return null;
        }

        /// <summary>Public or [SerializeField], not [NonSerialized]: what the inspector shows.</summary>
        public static bool IsSerialized(FieldInfo f) =>
            !f.IsNotSerialized && !f.IsInitOnly && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);

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

        static string PathUnder(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }
    }
}
