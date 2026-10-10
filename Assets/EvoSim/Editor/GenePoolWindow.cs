using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The gene pool (21 §4, labs D and E): per locus, the allele shares among the living over time (stacked), the
    /// current top alleles with their text and origin, and the lineage of a selected allele as a chain of texts; number
    /// genes as histograms of the living animals' values.
    /// </summary>
    public class GenePoolWindow : EditorWindow
    {
        const int SampleEvery = 20, History = 300;
        World watched;
        int speciesIndex, locus, lastSample = -1;
        string selected;
        Vector2 scroll;
        readonly Dictionary<string, List<Dictionary<string, int>>> history = new Dictionary<string, List<Dictionary<string, int>>>();

        [MenuItem("Window/EvoSim/Gene Pool")]
        public static void Open() => GetWindow<GenePoolWindow>("Gene Pool").Show();

        void OnInspectorUpdate()
        {
            var w = EditorWorlds.Current();
            if (w != watched) { watched = w; history.Clear(); lastSample = -1; }
            if (watched != null && watched.IsInitialized && watched.Tick >= lastSample + SampleEvery) Sample();
            if (Application.isPlaying) Repaint();
        }

        void Sample()
        {
            lastSample = watched.Tick;
            foreach (var s in watched.AllSpecies)
                for (int i = 0; i < s.Genes.Count; i++)
                {
                    string key = s.Id + "/" + i;
                    if (!history.TryGetValue(key, out var list)) history[key] = list = new List<Dictionary<string, int>>();
                    list.Add(Counts(s, i));
                    if (list.Count > History) list.RemoveAt(0);
                }
        }

        static Dictionary<string, int> Counts(Species s, int locus)
        {
            var counts = new Dictionary<string, int>();
            foreach (var a in s.Animals)
            {
                if (a.IsGone) continue;
                string id = a.Genome[locus].Id;
                counts.TryGetValue(id, out int n);
                counts[id] = n + 1;
            }
            return counts;
        }

        void OnGUI()
        {
            if (watched == null || !watched.IsInitialized || watched.AllSpecies.Count == 0) { EditorGUILayout.HelpBox("No running world.", MessageType.Info); return; }
            var names = new string[watched.AllSpecies.Count];
            for (int i = 0; i < names.Length; i++) names[i] = watched.AllSpecies[i].DisplayName;
            speciesIndex = Mathf.Clamp(EditorGUILayout.Popup("Species", speciesIndex, names), 0, names.Length - 1);
            var s = watched.AllSpecies[speciesIndex];
            if (s.Genes.Count == 0) return;
            var loci = new string[s.Genes.Count];
            for (int i = 0; i < loci.Length; i++) loci[i] = s.Genes[i].Label;
            locus = Mathf.Clamp(EditorGUILayout.Popup("Locus", locus, loci), 0, loci.Length - 1);
            var gene = s.Genes[locus];
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (gene.Kind == AlleleKind.Number) Histogram(s);
            else
            {
                Stacked(s.Id + "/" + locus);
                Top(s);
                Lineage();
            }
            EditorGUILayout.EndScrollView();
        }

        void Stacked(string key)
        {
            var rect = GUILayoutUtility.GetRect(200, 120, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            if (!history.TryGetValue(key, out var list) || list.Count == 0) return;
            float colW = rect.width / list.Count;
            for (int c = 0; c < list.Count; c++)
            {
                int total = 0;
                foreach (var kv in list[c]) total += kv.Value;
                if (total == 0) continue;
                var ids = new List<string>(list[c].Keys);
                ids.Sort(System.StringComparer.Ordinal);
                float y = rect.yMax;
                foreach (var id in ids)
                {
                    float h = rect.height * list[c][id] / total;
                    y -= h;
                    EditorGUI.DrawRect(new Rect(rect.x + c * colW, y, Mathf.Max(1f, colW), h), ColorOf(id));
                }
            }
        }

        void Top(Species s)
        {
            var counts = Counts(s, locus);
            var ids = new List<string>(counts.Keys);
            ids.Sort((a, b) => counts[b] != counts[a] ? counts[b].CompareTo(counts[a]) : string.CompareOrdinal(a, b));
            int living = 0;
            foreach (var kv in counts) living += kv.Value;
            EditorGUILayout.LabelField($"Top alleles among {living} living", EditorStyles.boldLabel);
            for (int i = 0; i < ids.Count && i < 12; i++)
            {
                var allele = watched.Alleles.ById(ids[i]);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12)), ColorOf(ids[i]));
                if (GUILayout.Button($"{100f * counts[ids[i]] / living:0}%  {allele?.Text}  ({allele?.Origin}, {ids[i]})", EditorStyles.label)) selected = ids[i];
                EditorGUILayout.EndHorizontal();
            }
        }

        void Lineage()
        {
            if (selected == null) return;
            EditorGUILayout.LabelField("Lineage (newest first)", EditorStyles.boldLabel);
            var a = watched.Alleles.ById(selected);
            for (int guard = 0; a != null && guard < 50; guard++)
            {
                EditorGUILayout.LabelField(a.Id, $"{a.Text}   — {a.Origin}{(a.Operator != null ? ", " + a.Operator : "")}", EditorStyles.wordWrappedLabel);
                a = watched.Alleles.ById(a.ParentId);
            }
        }

        void Histogram(Species s)
        {
            var values = new List<float>();
            foreach (var a in s.Animals) if (!a.IsGone) values.Add(a.Genome[locus].Number);
            if (values.Count == 0) return;
            float min = Mathf.Min(values.ToArray()), max = Mathf.Max(values.ToArray());
            const int bins = 20;
            var counts = new int[bins];
            foreach (var v in values) counts[Mathf.Clamp((int)((v - min) / Mathf.Max(1e-6f, max - min) * bins), 0, bins - 1)]++;
            var rect = GUILayoutUtility.GetRect(200, 120, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            int top = Mathf.Max(1, Mathf.Max(counts));
            for (int b = 0; b < bins; b++)
            {
                float h = rect.height * counts[b] / top;
                EditorGUI.DrawRect(new Rect(rect.x + b * rect.width / bins, rect.yMax - h, rect.width / bins - 1, h), new Color(0.4f, 0.7f, 1f));
            }
            EditorGUILayout.LabelField("Range of the living", $"{min:0.##} … {max:0.##} ({values.Count} animals)");
        }

        static Color ColorOf(string id) => Color.HSVToRGB((Mathf.Abs(id.GetHashCode()) % 1000) / 1000f, 0.6f, 0.9f);
    }
}
