using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// JEV's decision head and calibration at a pinned revision (08 §6): the option letters' token ids, a bias per
    /// slot, the temperature of the "choice" kind. Row = softmax((log-prob + bias) / T) over the options.
    /// </summary>
    public sealed class JevHead
    {
        public int ChoiceStart { get; private set; }
        public int ChoiceEnd { get; private set; }
        public int[] TokenIds { get; private set; }
        public double[] Bias { get; private set; }
        public double Temperature { get; private set; }
        /// <summary>The option slots the head has (16: A–P).</summary>
        public int MaxOptions => ChoiceEnd - ChoiceStart;

        /// <summary>Reads decision_head.json and calibration.json; throws with a readable message when they don't fit.</summary>
        public static JevHead Parse(string headJson, string calibrationJson)
        {
            if (string.IsNullOrEmpty(headJson)) throw new ArgumentException("no decision_head.json");
            if (string.IsNullOrEmpty(calibrationJson)) throw new ArgumentException("no calibration.json");
            var head = JObject.Parse(headJson);
            var calibration = JObject.Parse(calibrationJson);
            var range = (JArray)head["slots"]?["ranges"]?["choice"] ?? throw new ArgumentException("decision_head.json has no choice slots");
            var ids = (JArray)head["verbalizer_ids"];
            var bias = (JArray)head["bias"];
            var h = new JevHead
            {
                ChoiceStart = (int)range[0],
                ChoiceEnd = (int)range[1],
                TokenIds = new int[ids.Count],
                Bias = new double[bias.Count],
                Temperature = (double?)calibration["per_kind"]?["choice"] ?? throw new ArgumentException("calibration.json has no choice temperature"),
            };
            for (int i = 0; i < ids.Count; i++) h.TokenIds[i] = (int)ids[i];
            for (int i = 0; i < bias.Count; i++) h.Bias[i] = (double)bias[i];
            if (h.ChoiceEnd > h.TokenIds.Length || h.ChoiceEnd > h.Bias.Length) throw new ArgumentException("decision_head.json: choice slots beyond the head");
            return h;
        }

        /// <summary>The token ids of the first n option letters.</summary>
        public int[] ChoiceIds(int n)
        {
            if (n > MaxOptions) throw new ArgumentException($"{n} options; JEV takes at most {MaxOptions}");
            var ids = new int[n];
            Array.Copy(TokenIds, ChoiceStart, ids, 0, n);
            return ids;
        }

        /// <summary>The probability row from the options' log-probabilities by token id (a missing option counts −1e9).</summary>
        public float[] Row(IReadOnlyDictionary<int, double> logProbabilities, int n)
        {
            var z = new double[n];
            double max = double.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                int slot = ChoiceStart + i;
                double lp = logProbabilities.TryGetValue(TokenIds[slot], out var v) ? v : -1e9;
                z[i] = (lp + Bias[slot]) / Temperature;
                if (z[i] > max) max = z[i];
            }
            for (int i = 0; i < n; i++) z[i] = Math.Exp(z[i] - max);
            return Brain.Normalize(z);
        }
    }
}
