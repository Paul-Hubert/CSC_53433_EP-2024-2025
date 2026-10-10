using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The selected animal (21 §4, lab C): stats, traits, genome sentences, last situation, last probabilities as bars,
    /// current action and target, busy reason, parents and offspring. Click an animal's view to select it.
    /// </summary>
    public class AnimalInspectorWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Window/EvoSim/Animal Inspector")]
        public static void Open() => GetWindow<AnimalInspectorWindow>("Animal").Show();

        void OnSelectionChange() => Repaint();
        void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }

        void OnGUI()
        {
            var a = EditorWorlds.SelectedAnimal(out var w);
            if (a == null) { EditorGUILayout.HelpBox("Select an animal's view in the Scene view (Play mode).", MessageType.Info); return; }
            var s = a.Species;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField($"{s.DisplayName} #{a.Id}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Age, generation", $"{a.Age} ticks, generation {a.Generation} ({a.Origin})");
            EditorGUILayout.LabelField("Parents", a.Parents != null && a.Parents.Length > 0 ? string.Join(", ", a.Parents) : "none (founder or newcomer)");
            EditorGUILayout.LabelField("Offspring, meals", $"{a.Offspring}, {a.Meals}");
            EditorGUILayout.LabelField("Position", a.Position.ToString("F1"));
            if (a.IsGone) EditorGUILayout.HelpBox("Gone: dead or migrated.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            foreach (var d in s.Declarations.Stats) EditorGUILayout.LabelField(d.Id.Name, a[d.Id].ToString("0.##"));
            EditorGUILayout.LabelField("Traits", EditorStyles.boldLabel);
            foreach (var d in s.Declarations.Traits) EditorGUILayout.LabelField(d.Id.Name, a.Trait(d.Id).ToString("0.##"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Genome", EditorStyles.boldLabel);
            for (int i = 0; i < s.Genes.Count; i++)
                EditorGUILayout.LabelField(s.Genes[i].Label, $"{a.Genome[i].Text}   [{a.Genome[i].Id}]", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Decision", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Action", a.CurrentAction != null ? a.CurrentAction.Name + (a.Searching ? " (searching)" : "") : "none");
            if (a.IsBusy) EditorGUILayout.LabelField("Busy", $"{a.BusyTicks} ticks: {a.BusyReason}");
            if (a.TargetId >= 0) EditorGUILayout.LabelField("Target", "#" + a.TargetId);
            EditorGUILayout.LabelField("Last situation (tick " + a.LastDecisionTick + ")", a.LastSituation ?? "—", EditorStyles.wordWrappedLabel);
            if (a.LastProbabilities != null)
                for (int i = 0; i < a.LastProbabilities.Length && i < s.Actions.Count; i++)
                {
                    var r = EditorGUILayout.GetControlRect();
                    var label = new Rect(r.x, r.y, 80, r.height);
                    var bar = new Rect(r.x + 84, r.y + 2, (r.width - 140) * a.LastProbabilities[i], r.height - 4);
                    GUI.Label(label, s.Actions[i].Name);
                    EditorGUI.DrawRect(bar, EditorWorlds.ActionColor(i));
                    GUI.Label(new Rect(r.xMax - 50, r.y, 50, r.height), a.LastProbabilities[i].ToString("0.000"));
                }
            EditorGUILayout.EndScrollView();
        }
    }
}
