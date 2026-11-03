using UnityEditor;
using UnityEngine;

namespace EvoSim.Neuro.Editor
{
    /// <summary>
    /// <c>GameObject ▸ EvoSim ▸ New Neural Species ▸ Prey | Predator</c> (plan M16 §5.7): a neural species matched to a JEV
    /// species of the scene, built by <see cref="NeuralTemplates"/>, with undo.
    /// </summary>
    /// <remarks>
    /// The JEV species to match is the selected one when a species (or something under it) is selected, else the scene's
    /// <c>prey</c> or <c>predator</c>. The new species copies its body and life settings, gets the neural controller and the
    /// <c>NeuroPreyBody</c> or <c>NeuroPredatorBody</c> view, and the pair goes into the World's <see cref="MatchedPairs"/>.
    /// Other species' diets are left as they are: add the new prey to the predators that should eat it (V-79 lists them).
    /// </remarks>
    public static class NeuralSpeciesMenu
    {
        /// <summary>The neural module prefabs' folder: the bodies and the two species.</summary>
        public const string ModulesFolder = "Assets/EvoSim/Neuro/Modules";

        /// <summary>Adds a neural prey matched to the selected or the scene's <c>prey</c>.</summary>
        [MenuItem("GameObject/EvoSim/New Neural Species/Prey", false, 12)]
        static void NewPrey() => New("prey", true);

        /// <summary>Adds a neural predator matched to the selected or the scene's <c>predator</c>.</summary>
        [MenuItem("GameObject/EvoSim/New Neural Species/Predator", false, 12)]
        static void NewPredator() => New("predator", false);

        /// <summary>Builds the species against the JEV species found, selects it and registers the undo.</summary>
        static void New(string defaultId, bool prey)
        {
            var jev = Match(defaultId);
            if (jev == null)
            {
                EditorUtility.DisplayDialog("New Neural Species",
                    $"Select the JEV species to match, or open a scene whose World has a species '{defaultId}'.", "OK");
                return;
            }
            var world = jev.GetComponentInParent<World>(true);
            bool hadBrain = world.GetComponentInChildren<NeuralBrain>(true) != null;
            bool hadPairs = world.GetComponentInChildren<MatchedPairs>(true) != null;
            if (hadPairs) Undo.RecordObject(world.GetComponentInChildren<MatchedPairs>(true), "New Neural Species");
            var s = prey ? NeuralTemplates.Prey(jev, body: Body("NeuroPreyBody")) : NeuralTemplates.Predator(jev, body: Body("NeuroPredatorBody"));
            Undo.RegisterCreatedObjectUndo(s.gameObject, "New Neural Species");
            if (!hadBrain) Undo.RegisterCreatedObjectUndo(world.GetComponentInChildren<NeuralBrain>(true).gameObject, "New Neural Species");
            if (!hadPairs) Undo.RegisterCreatedObjectUndo(world.GetComponentInChildren<MatchedPairs>(true).gameObject, "New Neural Species");
            Selection.activeGameObject = s.gameObject;
        }

        /// <summary>The selected species if it is not neural, else the first World's species with that id, else null.</summary>
        static Species Match(string defaultId)
        {
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Species>(true) : null;
            if (selected != null && selected.GetComponentInChildren<NeuralGenomeGene>(true) == null) return selected;
            foreach (var w in Object.FindObjectsByType<World>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var s in w.GetComponentsInChildren<Species>(true))
                    if (s.RequestedId == defaultId) return s;
            return null;
        }

        /// <summary>A neural body prefab of <see cref="ModulesFolder"/>, or null when it isn't built yet (the JEV body stays).</summary>
        /// <param name="name">The prefab's file name without extension.</param>
        /// <returns>The prefab, or null.</returns>
        public static GameObject Body(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModulesFolder + "/" + name + ".prefab");
    }
}
