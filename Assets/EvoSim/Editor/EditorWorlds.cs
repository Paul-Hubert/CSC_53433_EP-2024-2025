using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Editor
{
    /// <summary>What the windows look at (EDIT-05): the worlds of the open scenes, the selected world, the selected animal.</summary>
    public static class EditorWorlds
    {
        /// <summary>The Worlds of the open scenes, enabled ones first.</summary>
        public static List<World> All()
        {
            var list = new List<World>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects()) list.AddRange(root.GetComponentsInChildren<World>(true));
            }
            list.Sort((a, b) => b.isActiveAndEnabled.CompareTo(a.isActiveAndEnabled));
            return list;
        }

        /// <summary>The world of the selection, else the first running one, else the first one.</summary>
        public static World Current()
        {
            if (Selection.activeGameObject != null)
            {
                var w = Selection.activeGameObject.GetComponentInParent<World>(true);
                if (w != null) return w;
            }
            var all = All();
            foreach (var w in all) if (w.IsInitialized) return w;
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>The animal whose view is selected, or null.</summary>
        public static Animal SelectedAnimal(out World world)
        {
            world = null;
            var go = Selection.activeGameObject;
            var view = go != null ? go.GetComponentInParent<AnimalView>() : null;
            if (view == null || view.AnimalId == 0) return null;
            world = view.GetComponentInParent<World>();
            return world != null ? Find(world, view.AnimalId) : null;
        }

        public static Animal Find(World w, int id)
        {
            foreach (var s in w.AllSpecies)
                foreach (var a in s.Animals)
                    if (a.Id == id) return a;
            return null;
        }

        /// <summary>A stable colour per action index (21 §5): the same order always gives the same colours.</summary>
        public static Color ActionColor(int index) =>
            index < 0 ? Color.gray : Color.HSVToRGB(index * 0.61803398875f % 1f, 0.75f, 0.95f);
    }
}
