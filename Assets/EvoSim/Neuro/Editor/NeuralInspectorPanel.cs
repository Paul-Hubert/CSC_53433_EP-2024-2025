using System.Text;
using EvoSim.Editor;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Editor
{
    /// <summary>
    /// The Animal Inspector's panel for a neural animal (plan M16 §5): its network's inputs, grouped by sense in layout
    /// order, and its outputs, one bar per action with the largest marked, computed from the animal's state now.
    /// </summary>
    /// <remarks>
    /// The panel builds the query the ask-brains phase would build (<see cref="NeuralBrain.QueryOf"/>) and runs the network
    /// on it (<see cref="NeuralBrain.Explain"/>); it only reads the world (SPACE-12). Pause the run to read a decision: the
    /// outputs are those of the current tick, which the animal acts on at its next decision.
    /// </remarks>
    public class NeuralInspectorPanel : AnimalInspectorPanel
    {
        /// <inheritdoc/>
        public override string Title => "Network";

        /// <inheritdoc/>
        public override bool Shows(Animal a) => a.Species.Brain is NeuralBrain && a.Species.Module<NeuralGenomeGene>() != null;

        /// <inheritdoc/>
        public override void Draw(Animal a, World w)
        {
            var s = a.Species;
            var brain = (NeuralBrain)s.Brain;
            var layout = s.Module<NeuralGenomeGene>().Layout;
            EditorGUILayout.LabelField("Layout", layout.ToString(), EditorStyles.wordWrappedLabel);
            if (a.IsGone) return;
            string error = brain.Explain(NeuralBrain.QueryOf(a), out var inputs, out var outputs);
            if (error != null) { EditorGUILayout.HelpBox(error, MessageType.Warning); return; }

            EditorGUILayout.LabelField("Inputs", EditorStyles.miniBoldLabel);
            foreach (var slot in layout.Slots)
            {
                string label = slot.SenseIndex < 0 ? "bias" : s.Senses[slot.SenseIndex].Label;
                var sb = new StringBuilder();
                for (int k = 0; k < slot.Width; k++)
                {
                    if (k > 0) sb.Append(' ');
                    sb.Append(inputs[slot.Offset + k].ToString("0.00"));
                }
                EditorGUILayout.LabelField(label, sb.ToString(), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.LabelField("Outputs", EditorStyles.miniBoldLabel);
            int best = NeuralNet.ArgMax(outputs);
            float scale = 0f;
            foreach (var o in outputs) scale = Mathf.Max(scale, Mathf.Abs(o));
            for (int i = 0; i < outputs.Length && i < s.Actions.Count; i++)
            {
                var r = EditorGUILayout.GetControlRect();
                float half = (r.width - 140) / 2f, mid = r.x + 84 + half;
                float len = scale > 0f ? half * outputs[i] / scale : 0f;
                GUI.Label(new Rect(r.x, r.y, 80, r.height), (i == best ? "▶ " : "") + s.Actions[i].Name);
                EditorGUI.DrawRect(len >= 0 ? new Rect(mid, r.y + 2, len, r.height - 4) : new Rect(mid + len, r.y + 2, -len, r.height - 4),
                                   EditorWorlds.ActionColor(i));
                GUI.Label(new Rect(r.xMax - 50, r.y, 50, r.height), outputs[i].ToString("0.000"));
            }
        }
    }
}
