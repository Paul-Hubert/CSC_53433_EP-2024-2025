using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace EvoSim.Editor
{
    /// <summary>The scene-view overlay (21 §5): toggles for food, cover, carcasses, eggs and action letters, and species filters.</summary>
    /// <remarks>
    /// It appears in the Scene view as the <c>EvoSim</c> panel (shown by default; the Overlays menu of the Scene view
    /// hides or shows it). Each toggle writes a setting of <see cref="SceneDrawing"/>, which draws the items.
    /// </remarks>
    [Overlay(typeof(SceneView), "EvoSim", true)]
    public class EvoSimOverlay : Overlay
    {
        /// <summary>
        /// Builds the panel: one toggle per kind of item and, when a world runs, one <c>Show &lt;species&gt;</c> toggle per
        /// species.
        /// </summary>
        /// <inheritdoc/>
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement { style = { minWidth = 160 } };
            root.Add(Toggle("Food items", SceneDrawing.Food, v => SceneDrawing.Food = v));
            root.Add(Toggle("Cover", SceneDrawing.CoverCells, v => SceneDrawing.CoverCells = v));
            root.Add(Toggle("Carcasses", SceneDrawing.Carcasses, v => SceneDrawing.Carcasses = v));
            root.Add(Toggle("Eggs", SceneDrawing.Eggs, v => SceneDrawing.Eggs = v));
            root.Add(Toggle("Action letters", SceneDrawing.ActionLetters, v => SceneDrawing.ActionLetters = v));
            var w = EditorWorlds.Current();
            if (w != null && w.IsInitialized)
                foreach (var s in w.AllSpecies)
                {
                    string id = s.Id;
                    root.Add(Toggle("Show " + s.DisplayName, !SceneDrawing.HiddenSpecies.Contains("|" + id + "|"), v =>
                    {
                        string hidden = SceneDrawing.HiddenSpecies.Replace("|" + id + "|", "");
                        SceneDrawing.HiddenSpecies = v ? hidden : hidden + "|" + id + "|";
                    }));
                }
            return root;
        }

        /// <summary>A toggle that calls <paramref name="set"/> and repaints the Scene views when it changes.</summary>
        static Toggle Toggle(string label, bool value, System.Action<bool> set)
        {
            var t = new Toggle(label) { value = value };
            t.RegisterValueChangedCallback(e => { set(e.newValue); SceneView.RepaintAll(); });
            return t;
        }
    }
}
