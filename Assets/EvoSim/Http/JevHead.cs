using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// JEV's decision head and calibration at a pinned revision (08 §6): the option letters' token ids, a bias per
    /// slot, the temperature of the "choice" kind. Row = softmax((log-prob + bias) / T) over the options.
    /// </summary>
    /// <remarks>Made by <see cref="Parse"/> on the main thread, then read only, also from the thread pool.</remarks>
    public sealed class JevHead
    {
        /// <summary>The head's slot of the first option letter, A (decision_head.json, <c>slots.ranges.choice[0]</c>).</summary>
        public int ChoiceStart { get; private set; }
        /// <summary>One past the head's slot of the last option letter (<c>slots.ranges.choice[1]</c>).</summary>
        public int ChoiceEnd { get; private set; }
        /// <summary>The token id of every slot of the head (<c>verbalizer_ids</c>).</summary>
        public int[] TokenIds { get; private set; }
        /// <summary>The bias added to each slot's log-probability (<c>bias</c>).</summary>
        public double[] Bias { get; private set; }
        /// <summary>The calibrated temperature T of the "choice" kind (calibration.json, <c>per_kind.choice</c>).</summary>
        public double Temperature { get; private set; }
        /// <summary>The option slots the head has (16: A–P).</summary>
        public int MaxOptions => ChoiceEnd - ChoiceStart;

        /// <summary>Reads decision_head.json and calibration.json; throws with a readable message when they don't fit.</summary>
        /// <param name="headJson">The text of <c>adapter_vllm/decision_head.json</c>, or null when the file is missing.</param>
        /// <param name="calibrationJson">The text of <c>calibration.json</c>, or null when the file is missing.</param>
        /// <returns>The head.</returns>
        /// <exception cref="ArgumentException">A file is missing, or lacks the choice slots or the choice temperature.</exception>
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
        /// <param name="n">The number of options, at most <see cref="MaxOptions"/>.</param>
        /// <returns>The ids of A, B, C… up to the n-th letter, in that order: the request's allowed tokens.</returns>
        /// <exception cref="ArgumentException"><paramref name="n"/> is above <see cref="MaxOptions"/>.</exception>
        public int[] ChoiceIds(int n)
        {
            if (n > MaxOptions) throw new ArgumentException($"{n} options; JEV takes at most {MaxOptions}");
            var ids = new int[n];
            Array.Copy(TokenIds, ChoiceStart, ids, 0, n);
            return ids;
        }

        /// <summary>The probability row from the options' log-probabilities by token id (a missing option counts −1e9).</summary>
        /// <param name="logProbabilities">The log-probability of each option letter, by token id, as the server gave them.</param>
        /// <param name="n">The number of options.</param>
        /// <returns>One probability per option, in option order, summing to 1 (DEC-11).</returns>
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
            for (int i = 0; i < n; i++) z[i] = Math.Exp(z[i] - max);                   // minus the largest: no overflow
            return Brain.Normalize(z);
        }
    }
}
