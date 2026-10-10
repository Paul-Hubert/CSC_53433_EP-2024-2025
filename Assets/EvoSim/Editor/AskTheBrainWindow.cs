using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// Ask the brain (21 §4, labs C and I): build a genome (founders, edited sentences, a gene's contrast pair, control
    /// sentences), pick a situation, and ask several brains side by side; a directed test gives P(action) with the pro
    /// sentence minus with the anti one. Queries are built from texts only: nothing is registered in the world, so asking
    /// never changes a run.
    /// </summary>
    public class AskTheBrainWindow : EditorWindow
    {
        World world;
        int speciesIndex, directedGene;
        string[] texts;
        int[] tokens;
        bool[] use;
        readonly List<(Brain brain, string label, BrainAnswer answer, int species)> pending = new List<(Brain, string, BrainAnswer, int)>();
        readonly List<string> results = new List<string>();
        Vector2 scroll;

        [MenuItem("Window/EvoSim/Ask the Brain")]
        public static void Open() => GetWindow<AskTheBrainWindow>("Ask the Brain").Show();

        void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                if (!p.answer.IsDone) continue;
                var s = world.AllSpecies[p.species];
                var row = p.answer.Row(0);
                var parts = new List<string>();
                if (row == null) parts.Add("failed: " + p.answer.Error(0));
                else for (int k = 0; k < row.Length && k < s.Actions.Count; k++) parts.Add($"{s.Actions[k].Name} {row[k]:0.00}");
                results.Add($"{p.brain.gameObject.name} [{p.label}]: {string.Join("  ", parts)}");
                pending.RemoveAt(i);
                Repaint();
            }
        }

        void OnGUI()
        {
            var w = EditorWorlds.Current();
            if (w == null) { EditorGUILayout.HelpBox("No World in the open scenes.", MessageType.Info); return; }
            if (!w.IsInitialized && (world != w || w.AllSpecies.Count == 0))
            {
                if (GUILayout.Button("Prepare the world (validates it)")) { ValidationPanel.Validate(w); world = w; texts = null; }
                if (world != w) return;
            }
            if (world != w) { world = w; texts = null; }
            if (w.AllSpecies.Count == 0) return;
            var names = new string[w.AllSpecies.Count];
            for (int i = 0; i < names.Length; i++) names[i] = w.AllSpecies[i].DisplayName;
            int before = speciesIndex;
            speciesIndex = Mathf.Clamp(EditorGUILayout.Popup("Species", speciesIndex, names), 0, names.Length - 1);
            var s = w.AllSpecies[speciesIndex];
            if (texts == null || before != speciesIndex || texts.Length != s.Genes.Count) Reset(s);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Genome", EditorStyles.boldLabel);
            for (int i = 0; i < s.Genes.Count; i++)
            {
                if (!(s.Genes[i] is TextGene g)) continue;
                EditorGUILayout.BeginHorizontal();
                texts[i] = EditorGUILayout.TextField(g.Label, texts[i]);
                if (GUILayout.Button("pro", GUILayout.Width(36))) texts[i] = g.ContrastPro;
                if (GUILayout.Button("anti", GUILayout.Width(36))) texts[i] = g.ContrastAnti;
                if (GUILayout.Button("neutral", GUILayout.Width(54))) texts[i] = g.Neutral;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Founders")) Reset(s);
            if (GUILayout.Button("Control sentences") && w.ControlSentences.Count > 0)
                for (int i = 0; i < texts.Length; i++) texts[i] = w.ControlSentences[(i * 7) % w.ControlSentences.Count];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Situation", EditorStyles.boldLabel);
            for (int i = 0; i < s.Senses.Count; i++)
            {
                var opts = new string[s.Senses[i].Tokens.Count];
                for (int k = 0; k < opts.Length; k++) opts[k] = s.Senses[i].Tokens[k];
                tokens[i] = EditorGUILayout.Popup(s.Senses[i].Label, tokens[i], opts);
            }
            EditorGUILayout.LabelField(s.Describe(new Observation((int[])tokens.Clone()), w.TextStyle), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.LabelField("Brains", EditorStyles.boldLabel);
            var brains = w.GetComponentsInChildren<Brain>(true);
            if (use == null || use.Length != brains.Length) use = new bool[brains.Length];
            for (int i = 0; i < brains.Length; i++) use[i] = EditorGUILayout.ToggleLeft($"{brains[i].gameObject.name} ({brains[i].Id})", use[i]);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ask")) for (int i = 0; i < brains.Length; i++) if (use[i]) Ask(brains[i], s, texts, "as written");
            var geneNames = new List<string>();
            foreach (var g in s.Genes) geneNames.Add(g.Label);
            directedGene = EditorGUILayout.Popup(directedGene, geneNames.ToArray(), GUILayout.Width(100));
            if (GUILayout.Button("Directed test (pro, anti)") && s.Genes[directedGene] is TextGene dg)
                for (int i = 0; i < brains.Length; i++)
                    if (use[i])
                    {
                        var pro = (string[])texts.Clone(); pro[directedGene] = dg.ContrastPro;
                        var anti = (string[])texts.Clone(); anti[directedGene] = dg.ContrastAnti;
                        Ask(brains[i], s, pro, "pro " + dg.Label);
                        Ask(brains[i], s, anti, "anti " + dg.Label);
                    }
            if (GUILayout.Button("Clear")) results.Clear();
            EditorGUILayout.EndHorizontal();
            if (pending.Count > 0) EditorGUILayout.LabelField($"waiting for {pending.Count} answers…");
            foreach (var r in results) EditorGUILayout.SelectableLabel(r, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(30));
            EditorGUILayout.EndScrollView();
        }

        void Reset(Species s)
        {
            texts = new string[s.Genes.Count];
            for (int i = 0; i < texts.Length; i++) texts[i] = s.Genes[i].FounderPool.Count > 0 ? s.Genes[i].FounderPool[0].Value.ToString() : "";
            tokens = new int[s.Senses.Count];
            directedGene = 0;
        }

        void Ask(Brain brain, Species s, string[] geneTexts, string label)
        {
            var genes = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < s.Genes.Count; i++)
                if (s.Genes[i].ReadByBrain) genes.Add(new KeyValuePair<string, string>(s.Genes[i].Label, geneTexts[i]));
            var o = new Observation((int[])tokens.Clone());
            string key = Hashing.Sha256Hex(s.Id + "\n" + string.Join("\n", geneTexts), 16);       // like a brain-visible genome key
            var q = new DecisionQuery(s, genes, o, s.Describe(o, world.TextStyle), world.TextStyle, key);
            pending.Add((brain, label, brain.Ask(new[] { q }), speciesIndex));
        }
    }
}
