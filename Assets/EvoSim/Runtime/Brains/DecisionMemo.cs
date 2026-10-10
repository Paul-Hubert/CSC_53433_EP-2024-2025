using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Within a run, the answer for a key (species, brain, brain-visible genome key, observation) is computed once and
    /// reused (DEC-30). A hit gives exactly what the brain answered. Failed answers are never stored (DEC-34).
    /// </summary>
    public sealed class DecisionMemo
    {
        readonly Dictionary<string, float[]> rows = new Dictionary<string, float[]>();

        public int Count => rows.Count;
        public int Hits { get; private set; }

        public static string Key(Species s, Brain b, string genomeKey, Observation o) =>
            s.Id + "|" + (b != null ? b.Id : "-") + "|" + genomeKey + "|" + o.Key;

        public bool TryGet(string key, out float[] row)
        {
            if (key != null && rows.TryGetValue(key, out row)) { Hits++; return true; }
            row = null;
            return false;
        }

        public void Store(string key, float[] row)
        {
            if (key != null && row != null) rows[key] = row;
        }

        public void Clear()
        {
            rows.Clear();
            Hits = 0;
        }
    }
}
