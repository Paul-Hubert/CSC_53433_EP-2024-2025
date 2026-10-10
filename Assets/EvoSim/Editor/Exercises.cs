using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Editor
{
    /// <summary>
    /// EvoSim ▸ Exercises (21 §6, 22 §0): Replace with stub writes the student stub of a reference module into
    /// Assets/Student and swaps the module for it in the open scenes, keeping its settings; Restore reference undoes it.
    /// </summary>
    public static class Exercises
    {
        public const string Folder = "Assets/EvoSim/Exercises";
        public const string StudentFolder = ScriptTemplates.StudentFolder + "/Exercises";
        const string PendingKey = "EvoSim.Exercises.Pending";

        /// <summary>The teaching path, smallest first (22 §0).</summary>
        public static readonly Type[] Path =
        {
            typeof(RestAction), typeof(LevelSense), typeof(Starvation), typeof(EatAction), typeof(NearestAnimalSense),
            typeof(FleeAction), typeof(HideAction), typeof(Stamina), typeof(Litter), typeof(UniformCrossover),
            typeof(KinematicLocomotion), typeof(HuntAction), typeof(RandomBrain), typeof(LlmMutation),
        };

        public static string StubName(Type reference) => "My" + reference.Name;

        /// <summary>The stub's source: Assets/EvoSim/Exercises/My&lt;Reference&gt;.cs.txt (not compiled in the package).</summary>
        public static string Template(Type reference) => File.ReadAllText($"{Folder}/{StubName(reference)}.cs.txt");

        /// <summary>The loaded class My&lt;Reference&gt; marked [ExerciseOf(reference)], or null before it compiles.</summary>
        public static Type StubType(Type reference) => TypeCache.GetTypesWithAttribute<ExerciseOfAttribute>()
            .Where(t => !t.IsAbstract && t.Name == StubName(reference) && t.GetCustomAttribute<ExerciseOfAttribute>().Reference == reference)
            .OrderBy(t => t.FullName, StringComparer.Ordinal).FirstOrDefault();

        /// <summary>Writes the stub into Assets/Student/Exercises unless the student already has it; returns its path.</summary>
        public static string WriteStub(Type reference)
        {
            ScriptTemplates.EnsureStudentAssemblies();
            Directory.CreateDirectory(StudentFolder);
            string path = $"{StudentFolder}/{StubName(reference)}.cs";
            if (!File.Exists(path)) File.WriteAllText(path, Template(reference));
            return path;
        }

        /// <summary>Every component of exactly this type in the loaded scenes, inactive ones included, in hierarchy order.</summary>
        public static List<Component> InOpenScenes(Func<Component, bool> match)
        {
            var found = new List<Component>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<Component>(true))
                        if (c != null && match(c)) found.Add(c);
            }
            return found;
        }

        /// <summary>
        /// Replaces a component by one of another type on the same object, at the same place among its components
        /// (module order matters, ARCH-04), copying every serialized field the two share and the enabled flag. Undoable.
        /// </summary>
        public static Component Swap(Component from, Type to)
        {
            var go = from.gameObject;
            int index = Array.IndexOf(go.GetComponents<Component>(), from);
            var made = Undo.AddComponent(go, to);
            var source = new SerializedObject(from);
            var target = new SerializedObject(made);
            var p = source.GetIterator();
            for (bool enter = true; p.NextVisible(enter); enter = false)
                if (p.propertyPath != "m_Script" && target.FindProperty(p.propertyPath) != null) target.CopyFromSerializedProperty(p);
            target.ApplyModifiedPropertiesWithoutUndo();
            if (from is Behaviour b && made is Behaviour m) m.enabled = b.enabled;
            for (int guard = 0; guard < 64 && Array.IndexOf(go.GetComponents<Component>(), made) > index; guard++)
                if (!ComponentUtility.MoveComponentUp(made)) break;
            Undo.DestroyObjectImmediate(from);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return made;
        }

        /// <summary>Swaps every instance of the reference module in the open scenes for the stub; returns how many.</summary>
        public static int SwapAll(Type reference, Type stub)
        {
            var list = InOpenScenes(c => c.GetType() == reference);
            foreach (var c in list) Swap(c, stub);
            return list.Count;
        }

        /// <summary>Puts the reference module back wherever an exercise class stands in the open scenes; the student's file stays.</summary>
        public static int RestoreAll()
        {
            var list = InOpenScenes(c => c.GetType().GetCustomAttribute<ExerciseOfAttribute>() != null);
            foreach (var c in list) Swap(c, c.GetType().GetCustomAttribute<ExerciseOfAttribute>().Reference);
            return list.Count;
        }

        /// <summary>The menu action: swap now if the stub is compiled, else write it and swap after the scripts reload.</summary>
        public static void ReplaceWithStub(Type reference)
        {
            var stub = StubType(reference);
            if (stub != null)
            {
                Debug.Log($"EvoSim: {SwapAll(reference, stub)} {reference.Name} replaced by {stub.Name}.");
                return;
            }
            string path = WriteStub(reference);
            SessionState.SetString(PendingKey, reference.AssemblyQualifiedName);
            Debug.Log($"EvoSim: wrote {path}; {reference.Name} is replaced when it has compiled.");
            AssetDatabase.Refresh();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void AfterReload()
        {
            string pending = SessionState.GetString(PendingKey, "");
            if (pending.Length == 0) return;
            SessionState.EraseString(PendingKey);
            var reference = Type.GetType(pending);
            var stub = reference != null ? StubType(reference) : null;
            if (stub == null) { Debug.LogWarning("EvoSim: the stub isn't compiled; fix the errors, then choose Replace with stub again."); return; }
            Debug.Log($"EvoSim: {SwapAll(reference, stub)} {reference.Name} replaced by {stub.Name}.");
        }

        const string Menu = "EvoSim/Exercises/Replace with stub/";
        [MenuItem(Menu + "1 RestAction", false, 300)] static void S01() => ReplaceWithStub(typeof(RestAction));
        [MenuItem(Menu + "2 LevelSense", false, 301)] static void S02() => ReplaceWithStub(typeof(LevelSense));
        [MenuItem(Menu + "3 Starvation", false, 302)] static void S03() => ReplaceWithStub(typeof(Starvation));
        [MenuItem(Menu + "4 EatAction", false, 303)] static void S04() => ReplaceWithStub(typeof(EatAction));
        [MenuItem(Menu + "5 NearestAnimalSense", false, 304)] static void S05() => ReplaceWithStub(typeof(NearestAnimalSense));
        [MenuItem(Menu + "6 FleeAction", false, 305)] static void S06() => ReplaceWithStub(typeof(FleeAction));
        [MenuItem(Menu + "7 HideAction", false, 306)] static void S07() => ReplaceWithStub(typeof(HideAction));
        [MenuItem(Menu + "8 Stamina", false, 307)] static void S08() => ReplaceWithStub(typeof(Stamina));
        [MenuItem(Menu + "9 Litter", false, 308)] static void S09() => ReplaceWithStub(typeof(Litter));
        [MenuItem(Menu + "10 UniformCrossover", false, 309)] static void S10() => ReplaceWithStub(typeof(UniformCrossover));
        [MenuItem(Menu + "11 KinematicLocomotion", false, 310)] static void S11() => ReplaceWithStub(typeof(KinematicLocomotion));
        [MenuItem(Menu + "12 HuntAction", false, 311)] static void S12() => ReplaceWithStub(typeof(HuntAction));
        [MenuItem(Menu + "13 RandomBrain", false, 312)] static void S13() => ReplaceWithStub(typeof(RandomBrain));
        [MenuItem(Menu + "14 LlmMutation", false, 313)] static void S14() => ReplaceWithStub(typeof(LlmMutation));

        [MenuItem("EvoSim/Exercises/Restore reference", false, 330)]
        static void Restore() => Debug.Log($"EvoSim: {RestoreAll()} reference module(s) restored.");
    }
}
