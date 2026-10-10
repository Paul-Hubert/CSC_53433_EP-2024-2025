using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// Points answers to rows (DEC-41): a missing action counts 0, negative points count 0, every action gets +0.01,
    /// then the row is normalised, so an all-zero answer becomes uniform. An answer that isn't a JSON object, or
    /// points that aren't numbers, are failures (as the prototype's int() raised).
    /// </summary>
    public static class PointsAnswer
    {
        public const double Smoothing = 0.01;

        /// <summary>The row for an answer text, or null with the reason.</summary>
        public static float[] ToRow(string answer, IReadOnlyList<string> actions, out string problem)
        {
            problem = null;
            JObject o;
            try { o = JObject.Parse(answer ?? ""); }
            catch (Exception) { problem = "the answer isn't a JSON object"; return null; }
            var w = new double[actions.Count];
            for (int i = 0; i < actions.Count; i++)
            {
                var v = o[actions[i]];
                long points = 0;
                if (v == null || v.Type == JTokenType.Null) points = 0;
                else if (v.Type == JTokenType.Integer) points = (long)v;
                else if (v.Type == JTokenType.Float) points = (long)Math.Truncate((double)v);
                else if (v.Type == JTokenType.String && long.TryParse(((string)v).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) points = parsed;
                else { problem = $"the points of '{actions[i]}' aren't a number"; return null; }
                w[i] = Math.Max(0, points) + Smoothing;
            }
            return Brain.Normalize(w);
        }

        /// <summary>The JSON schema of a points answer: one integer 0–100 per action, all required (08 §6).</summary>
        public static JObject Schema(IReadOnlyList<string> actions)
        {
            var properties = new JObject();
            var required = new JArray();
            foreach (var a in actions)
            {
                properties[a] = new JObject { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 100 };
                required.Add(a);
            }
            return new JObject { ["type"] = "object", ["properties"] = properties, ["required"] = required };
        }
    }
}
