using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EvoSim
{
    /// <summary>
    /// Writes JSON with sorted keys and fixed number formatting, so equal data gives equal text (RAND-10).
    /// Separators follow Python's json.dumps defaults (", " and ": "), the prototype's format.
    /// </summary>
    public static class CanonicalJson
    {
        public static string Write(object value)
        {
            var sb = new StringBuilder(128);
            Append(sb, value);
            return sb.ToString();
        }

        public static void Append(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: AppendString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(Number(f)); break;
                case double d: sb.Append(Number(d)); break;
                case IDictionary dict: AppendObject(sb, dict); break;
                case IEnumerable list: AppendList(sb, list); break;
                default: AppendString(sb, Convert.ToString(value, CultureInfo.InvariantCulture)); break;
            }
        }

        /// <summary>A number with at most 6 decimals, no trailing zeros, no negative zero.</summary>
        public static string Number(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return "null";
            double r = Math.Round(d, 6);
            if (r == 0) return "0";
            if (r == Math.Floor(r) && Math.Abs(r) < 1e15) return ((long)r).ToString(CultureInfo.InvariantCulture) + ".0";
            return r.ToString("0.######", CultureInfo.InvariantCulture);
        }

        static void AppendObject(StringBuilder sb, IDictionary dict)
        {
            var keys = new List<string>(dict.Count);
            foreach (var k in dict.Keys) keys.Add(Convert.ToString(k, CultureInfo.InvariantCulture));
            keys.Sort(StringComparer.Ordinal);
            sb.Append('{');
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                AppendString(sb, keys[i]);
                sb.Append(": ");
                Append(sb, dict[keys[i]]);
            }
            sb.Append('}');
        }

        static void AppendList(StringBuilder sb, IEnumerable list)
        {
            sb.Append('[');
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(", ");
                first = false;
                Append(sb, item);
            }
            sb.Append(']');
        }

        /// <summary>A JSON string as the prototype writes it (json.dumps with ensure_ascii=False): UTF-8, control characters escaped.</summary>
        public static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
