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
    /// <remarks>
    /// <para><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 1 RestAction … 14 LlmMutation</c>, in the order of the teaching
    /// path (<see cref="Path"/>). The first time, the menu copies <c>Assets/EvoSim/Exercises/My&lt;Reference&gt;.cs.txt</c> to
    /// <c>Assets/Student/Exercises/My&lt;Reference&gt;.cs</c> (with the student assemblies, if missing) and waits for Unity
    /// to compile it; after the reload, every instance of the reference module in the open scenes is swapped for the
    /// student's class, which is marked <c>[ExerciseOf(typeof(Reference))]</c>. The student then writes the stub, in their
    /// own way: nothing grades it, and the conformance suite of its kind (Test Runner, EditMode) only checks that it fits
    /// the system.</para>
    /// <para><c>EvoSim ▸ Exercises ▸ Restore reference</c> puts every reference module back in the open scenes; the student's
    /// files stay. Both swaps keep the inspector settings and the component's place, and can be undone (Edit ▸ Undo).
    /// Save the scene to keep a swap.</para>
    /// </remarks>
    public static class Exercises
    {
        /// <summary>The stub templates' folder: <c>Assets/EvoSim/Exercises</c> (<c>.cs.txt</c> files, not compiled).</summary>
        public const string Folder = "Assets/EvoSim/Exercises";
        /// <summary>Where the students' stubs are written: <c>Assets/Student/Exercises</c>.</summary>
        public const string StudentFolder = ScriptTemplates.StudentFolder + "/Exercises";
        /// <summary>The session key that remembers which reference to swap once the stub has compiled.</summary>
        const string PendingKey = "EvoSim.Exercises.Pending";

        /// <summary>The teaching path, smallest first (22 §0).</summary>
        public static readonly Type[] Path =
        {
            typeof(RestAction), typeof(LevelSense), typeof(Starvation), typeof(EatAction), typeof(NearestAnimalSense),
            typeof(FleeAction), typeof(HideAction), typeof(Stamina), typeof(Litter), typeof(UniformCrossover),
            typeof(KinematicLocomotion), typeof(HuntAction), typeof(RandomBrain), typeof(LlmMutation),
        };

        /// <summary>The class name of a reference module's stub: <c>My</c> followed by the reference's name.</summary>
        /// <param name="reference">A reference module class, such as <c>RestAction</c>.</param>
        /// <returns>The stub's class name, such as <c>MyRestAction</c>.</returns>
        public static string StubName(Type reference) => "My" + reference.Name;

        /// <summary>The stub's source: Assets/EvoSim/Exercises/My&lt;Reference&gt;.cs.txt (not compiled in the package).</summary>
        /// <param name="reference">A reference module class of <see cref="Path"/>.</param>
        /// <returns>The C# source of the stub.</returns>
        public static string Template(Type reference) => File.ReadAllText($"{Folder}/{StubName(reference)}.cs.txt");

        /// <summary>The loaded class My&lt;Reference&gt; marked [ExerciseOf(reference)], or null before it compiles.</summary>
        /// <param name="reference">A reference module class.</param>
        /// <returns>The student's compiled stub class (the first by full name when several match), or null.</returns>
        public static Type StubType(Type reference) => TypeCache.GetTypesWithAttribute<ExerciseOfAttribute>()
            .Where(t => !t.IsAbstract && t.Name == StubName(reference) && t.GetCustomAttribute<ExerciseOfAttribute>().Reference == reference)
            .OrderBy(t => t.FullName, StringComparer.Ordinal).FirstOrDefault();

        /// <summary>Writes the stub into Assets/Student/Exercises unless the student already has it; returns its path.</summary>
        /// <remarks>Also creates the student assemblies (<see cref="ScriptTemplates.EnsureStudentAssemblies"/>). An existing
        /// file is never overwritten: a student's work is safe.</remarks>
        /// <param name="reference">A reference module class of <see cref="Path"/>.</param>
        /// <returns>The stub's path, relative to the project.</returns>
        public static string WriteStub(Type reference)
        {
            ScriptTemplates.EnsureStudentAssemblies();
            Directory.CreateDirectory(StudentFolder);
            string path = $"{StudentFolder}/{StubName(reference)}.cs";
            if (!File.Exists(path)) File.WriteAllText(path, Template(reference));
            return path;
        }

        /// <summary>Every component that matches in the loaded scenes, inactive ones included, in hierarchy order.</summary>
        /// <param name="match">The test a component must pass, such as <c>c =&gt; c.GetType() == typeof(RestAction)</c>.</param>
        /// <returns>The matching components, possibly none.</returns>
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
        /// (module order matters, SPEC-03), copying every serialized field the two share and the enabled flag. Undoable.
        /// </summary>
        /// <param name="from">The component to replace; it is destroyed.</param>
        /// <param name="to">The new component's class.</param>
        /// <returns>The new component.</returns>
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
            // AddComponent puts the new one last: move it up to the old one's place (the guard stops a component Unity won't move).
            for (int guard = 0; guard < 64 && Array.IndexOf(go.GetComponents<Component>(), made) > index; guard++)
                if (!ComponentUtility.MoveComponentUp(made)) break;
            Undo.DestroyObjectImmediate(from);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return made;
        }

        /// <summary>Swaps every instance of the reference module in the open scenes for the stub; returns how many.</summary>
        /// <param name="reference">The reference module class; only components of exactly this class are swapped.</param>
        /// <param name="stub">The student's class.</param>
        /// <returns>The number of components swapped.</returns>
        public static int SwapAll(Type reference, Type stub)
        {
            var list = InOpenScenes(c => c.GetType() == reference);
            foreach (var c in list) Swap(c, stub);
            return list.Count;
        }

        /// <summary>Puts the reference module back wherever an exercise class stands in the open scenes; the student's file stays.</summary>
        /// <returns>The number of components restored.</returns>
        public static int RestoreAll()
        {
            var list = InOpenScenes(c => c.GetType().GetCustomAttribute<ExerciseOfAttribute>() != null);
            foreach (var c in list) Swap(c, c.GetType().GetCustomAttribute<ExerciseOfAttribute>().Reference);
            return list.Count;
        }

        /// <summary>The menu action: swap now if the stub is compiled, else write it and swap after the scripts reload.</summary>
        /// <param name="reference">A reference module class of <see cref="Path"/>.</param>
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

        /// <summary>
        /// After Unity recompiles the scripts, finishes a pending Replace with stub: swaps the modules if the stub compiled,
        /// else warns the student to fix the compile errors and choose the menu again.
        /// </summary>
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

        /// <summary>The menu folder of the stubs: <c>EvoSim ▸ Exercises ▸ Replace with stub</c>.</summary>
        const string Menu = "EvoSim/Exercises/Replace with stub/";
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 1 RestAction</c>: the first exercise.</summary>
        [MenuItem(Menu + "1 RestAction", false, 300)] static void S01() => ReplaceWithStub(typeof(RestAction));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 2 LevelSense</c>.</summary>
        [MenuItem(Menu + "2 LevelSense", false, 301)] static void S02() => ReplaceWithStub(typeof(LevelSense));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 3 Starvation</c>.</summary>
        [MenuItem(Menu + "3 Starvation", false, 302)] static void S03() => ReplaceWithStub(typeof(Starvation));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 4 EatAction</c>.</summary>
        [MenuItem(Menu + "4 EatAction", false, 303)] static void S04() => ReplaceWithStub(typeof(EatAction));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 5 NearestAnimalSense</c>.</summary>
        [MenuItem(Menu + "5 NearestAnimalSense", false, 304)] static void S05() => ReplaceWithStub(typeof(NearestAnimalSense));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 6 FleeAction</c>.</summary>
        [MenuItem(Menu + "6 FleeAction", false, 305)] static void S06() => ReplaceWithStub(typeof(FleeAction));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 7 HideAction</c>.</summary>
        [MenuItem(Menu + "7 HideAction", false, 306)] static void S07() => ReplaceWithStub(typeof(HideAction));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 8 Stamina</c>.</summary>
        [MenuItem(Menu + "8 Stamina", false, 307)] static void S08() => ReplaceWithStub(typeof(Stamina));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 9 Litter</c>.</summary>
        [MenuItem(Menu + "9 Litter", false, 308)] static void S09() => ReplaceWithStub(typeof(Litter));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 10 UniformCrossover</c>.</summary>
        [MenuItem(Menu + "10 UniformCrossover", false, 309)] static void S10() => ReplaceWithStub(typeof(UniformCrossover));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 11 KinematicLocomotion</c>.</summary>
        [MenuItem(Menu + "11 KinematicLocomotion", false, 310)] static void S11() => ReplaceWithStub(typeof(KinematicLocomotion));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 12 HuntAction</c>.</summary>
        [MenuItem(Menu + "12 HuntAction", false, 311)] static void S12() => ReplaceWithStub(typeof(HuntAction));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 13 RandomBrain</c>.</summary>
        [MenuItem(Menu + "13 RandomBrain", false, 312)] static void S13() => ReplaceWithStub(typeof(RandomBrain));
        /// <summary><c>EvoSim ▸ Exercises ▸ Replace with stub ▸ 14 LlmMutation</c>: the last exercise.</summary>
        [MenuItem(Menu + "14 LlmMutation", false, 313)] static void S14() => ReplaceWithStub(typeof(LlmMutation));

        /// <summary><c>EvoSim ▸ Exercises ▸ Restore reference</c>: puts every reference module back and logs how many.</summary>
        [MenuItem("EvoSim/Exercises/Restore reference", false, 330)]
        static void Restore() => Debug.Log($"EvoSim: {RestoreAll()} reference module(s) restored.");
    }
}
