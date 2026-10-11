using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The Species inspector (21 §3, EDIT-04): read-only tables computed from the subtree (actions, senses, genes, stats
    /// and traits, food, population), the observation-space size, the signature, and a prompt preview built by the same
    /// code as the Ask brains phase (T-PROMPT-05).
    /// </summary>
    /// <remarks>
    /// <para>Unity shows it below the Species' own fields when a Species is selected. In Edit mode, press
    /// <c>Show the computed tables</c>: it validates the world (which prepares it) and fills the tables; in Play mode they
    /// show the running world. The validation panel ends the inspector, with its fix buttons.</para>
    /// <para>The prompt preview: pick a brain, a founder genome (the n-th founder of each gene), a text style (V1 or V2) and
    /// one token per sense (or <c>Random situation</c>), press <c>Build</c>, and read the exact prompt with its token estimate
    /// and the brain's limit; <c>Copy</c> puts it on the clipboard.</para>
    /// </remarks>
    [CustomEditor(typeof(Species))]
    public class SpeciesInspector : UnityEditor.Editor
    {
        bool prepared;                    // true once the tables were computed in Edit mode
        ValidationReport report;          // the validation of the last computation, or null
        bool showActions = true, showSenses = true, showGenes, showStats, showFood, showPopulation, showPreview;   // foldouts
        int founder, brainIndex;          // the preview's founder index and brain
        int[] tokens;                     // the preview's situation: one token index per sense
        TextStyle style = TextStyle.V1;   // the preview's situation text style
        string preview = "";              // the last built prompt
        Vector2 previewScroll;            // the prompt's scroll position

        /// <summary>
        /// Draws the Species' fields, then, once computed, the observation-space size, the signature, the tables, the prompt
        /// preview and the validation.
        /// </summary>
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            var s = (Species)target;
            DrawDefaultInspector();
            var w = s.GetComponentInParent<World>(true);
            if (w == null) { EditorGUILayout.HelpBox("This species is under no World (V-02).", MessageType.Error); return; }
            EditorGUILayout.Space();
            if (!Application.isPlaying && GUILayout.Button(prepared ? "Refresh the tables" : "Show the computed tables"))
            {
                report = ValidationPanel.Validate(w);
                prepared = true;
                tokens = null;
            }
            if (!prepared && !(Application.isPlaying && w.IsInitialized)) return;
            if (s.World == null) { EditorGUILayout.HelpBox("The World didn't take this species in: see the validation.", MessageType.Warning); ValidationPanel.Draw(report); return; }

            EditorGUILayout.LabelField("Observation space", $"{s.ObservationSpace:N0} situations");
            EditorGUILayout.LabelField("Signature", s.Signature ?? "—");
            Actions(s);
            Senses(s);
            Genes(s);
            Stats(s);
            Food(s, w);
            Population(s);
            Preview(s, w);
            if (report != null && ValidationPanel.Draw(report)) report = ValidationPanel.Validate(w);
        }

        /// <summary>Lists the actions in order, each with the gene that steers it (or V-05 when none).</summary>
        void Actions(Species s)
        {
            showActions = EditorGUILayout.Foldout(showActions, $"Actions ({s.Actions.Count})", true);
            if (!showActions) return;
            for (int i = 0; i < s.Actions.Count; i++)
            {
                var a = s.Actions[i];
                EditorGUILayout.LabelField($"{i + 1}. {a.Name}", a.Gene != null ? "gene: " + a.Gene.Label : "no gene (V-05)");
            }
        }

        /// <summary>Lists the senses in situation-text order, with their tokens and the V1 and V2 text of their first token.</summary>
        void Senses(Species s)
        {
            showSenses = EditorGUILayout.Foldout(showSenses, $"Senses ({s.Senses.Count}), in situation-text order", true);
            if (!showSenses) return;
            for (int i = 0; i < s.Senses.Count; i++)
            {
                var sense = s.Senses[i];
                EditorGUILayout.LabelField($"{i + 1}. {sense.Label}", $"{sense.Tokens.Count} tokens: {string.Join(", ", sense.Tokens)}");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("V1", sense.Write(0, TextStyle.V1), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("V2", sense.Write(0, TextStyle.V2), EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>Lists the genes in locus order, with their kind, founder pool size, mutation operator and rate.</summary>
        void Genes(Species s)
        {
            showGenes = EditorGUILayout.Foldout(showGenes, $"Genes ({s.Genes.Count}), in locus order", true);
            if (!showGenes) return;
            foreach (var g in s.Genes)
            {
                var op = BreedPhase.OperatorFor(s, g);
                EditorGUILayout.LabelField(g.LocusId, $"{g.Kind}, {g.FounderPool.Count} founders, " +
                                                      (op != null ? $"{op.GetType().Name} at {op.Rate:0.###}" : "no mutation"));
            }
        }

        /// <summary>Lists the declared stats and traits with the module that declares each; traits with default, range and cost.</summary>
        void Stats(Species s)
        {
            showStats = EditorGUILayout.Foldout(showStats, "Stats and traits", true);
            if (!showStats) return;
            foreach (var d in s.Declarations.Stats)
                EditorGUILayout.LabelField("stat " + d.Id.Name, $"declared by {Who(d.DeclaredBy)}");
            foreach (var d in s.Declarations.Traits)
                EditorGUILayout.LabelField("trait " + d.Id.Name, $"default {d.Id.Default}, range [{d.Id.Min}, {d.Id.Max}], by {Who(d.DeclaredBy)}" +
                                                                 (s.Declarations.HasCost(d.Id.Name) ? ", has a cost" : ""));
        }

        /// <summary>Lists what the species eats (method, energy), whether it is edible itself, and its derived threats.</summary>
        void Food(Species s, World w)
        {
            showFood = EditorGUILayout.Foldout(showFood, "Food", true);
            if (!showFood) return;
            var diet = s.Module<Diet>();
            if (diet != null)
                foreach (var t in diet.Resolved)
                    EditorGUILayout.LabelField("eats " + t.Name, t.Edible != null ? $"{t.Edible.method}, {t.Energy:0.#} energy" : "not edible (V-24)");
            var e = s.GetComponent<Edible>();
            EditorGUILayout.LabelField("Edible itself", e != null ? $"{e.method}, {e.energy} energy, carcass {e.carcassPortions} × {e.carcassEnergy}" : "no");
            var threats = new List<string>();
            foreach (var x in w.ThreatsOf(s)) threats.Add(x.DisplayName);
            EditorGUILayout.LabelField("Threats", threats.Count > 0 ? string.Join(", ", threats) : "none");
        }

        /// <summary>Shows the initial population, the floor, the cap and its rule, and the living count in Play mode.</summary>
        void Population(Species s)
        {
            showPopulation = EditorGUILayout.Foldout(showPopulation, "Population", true);
            if (!showPopulation) return;
            var cap = s.Module<CapRule>();
            var floor = s.Module<FloorRule>();
            EditorGUILayout.LabelField("Initial", s.InitialPopulation.ToString());
            EditorGUILayout.LabelField("Floor", floor != null ? floor.Floor.ToString() : "none");
            EditorGUILayout.LabelField("Cap", cap != null ? $"{cap.Cap} ({cap.Rule})" : "none");
            if (Application.isPlaying) EditorGUILayout.LabelField("Living", s.Animals.Count.ToString());
        }

        /// <summary>Draws the prompt preview: brain, founder genome, text style and situation pickers, then the built prompt.</summary>
        void Preview(Species s, World w)
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Prompt preview", true);
            if (!showPreview) return;
            if (tokens == null || tokens.Length != s.Senses.Count) tokens = new int[s.Senses.Count];
            var brains = w.GetComponentsInChildren<Brain>(true);
            var names = new string[brains.Length];
            for (int i = 0; i < brains.Length; i++) names[i] = brains[i].gameObject.name;
            if (brains.Length == 0) { EditorGUILayout.HelpBox("No brain in this world.", MessageType.Warning); return; }
            brainIndex = Mathf.Clamp(EditorGUILayout.Popup("Brain", brainIndex, names), 0, brains.Length - 1);
            int founders = 1;
            foreach (var g in s.Genes) founders = Mathf.Max(founders, g.FounderPool.Count);
            founder = EditorGUILayout.IntSlider("Founder genome", founder, 0, founders - 1);
            style = (TextStyle)EditorGUILayout.EnumPopup("Text style", style);
            for (int i = 0; i < s.Senses.Count; i++)
            {
                var opts = new string[s.Senses[i].Tokens.Count];
                for (int k = 0; k < opts.Length; k++) opts[k] = s.Senses[i].Tokens[k];
                tokens[i] = EditorGUILayout.Popup(s.Senses[i].Label, tokens[i], opts);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Random situation"))
                for (int i = 0; i < tokens.Length; i++) tokens[i] = Random.Range(0, s.Senses[i].Tokens.Count);   // an editor convenience, not the simulation
            if (GUILayout.Button("Build")) preview = PromptPreview.For(s, PromptPreview.FounderGenome(w, s, founder), new Observation((int[])tokens.Clone()), style, brains[brainIndex]);
            if (GUILayout.Button("Copy")) EditorGUIUtility.systemCopyBuffer = preview;
            EditorGUILayout.EndHorizontal();
            if (preview.Length == 0) return;
            EditorGUILayout.LabelField($"About {PromptPreview.Tokens(preview)} tokens" +
                                       (brains[brainIndex].MaxPromptTokens > 0 ? $" (limit {brains[brainIndex].MaxPromptTokens})" : ""));
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(240));
            EditorGUILayout.TextArea(preview, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>The class name of the module that declared a stat or trait, or "?" when unknown.</summary>
        static string Who(SpeciesModule m) => m == null ? "?" : m.GetType().Name;
    }
}
