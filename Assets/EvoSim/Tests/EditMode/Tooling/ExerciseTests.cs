using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using EvoSim.Editor;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>The teaching path (22 §0) and EvoSim ▸ Exercises (21 §6): stub templates, the swap and its undo.</summary>
    public class ExerciseTests : WorldFixture
    {
        GameObject made;

        [TearDown] public void Clean() { if (made != null) Object.DestroyImmediate(made); }

        [Test, Description("22 §0: a stub for each of the 14 reference modules of the teaching path, in its order, of the same base class, marked with its reference")]
        public void StubsCoverTheTeachingPath()
        {
            Assert.AreEqual(new[]
            {
                "RestAction", "LevelSense", "Starvation", "EatAction", "NearestAnimalSense", "FleeAction", "HideAction", "Stamina", "Litter",
                "UniformCrossover", "KinematicLocomotion", "HuntAction", "RandomBrain", "LlmMutation",
            }, Exercises.Path.Select(t => t.Name).ToArray());
            foreach (var reference in Exercises.Path)
            {
                string text = Exercises.Template(reference);
                StringAssert.Contains($"[ExerciseOf(typeof({reference.Name}))]", text);
                var m = Regex.Match(text, $@"public class My{reference.Name} : (\w+)");
                Assert.IsTrue(m.Success, reference.Name);
                // Stamina and Litter derive from the reference: other modules look them up by type (open question 10).
                string expected = reference == typeof(Stamina) || reference == typeof(Litter) ? reference.Name : reference.BaseType.Name;
                Assert.AreEqual(expected, m.Groups[1].Value, reference.Name);
                StringAssert.Contains("ExerciseOfAttribute.Todo(", text, reference.Name + ": the stub throws until written");
            }
        }

        [UnityTest, Description("22 §0: every stub template compiles against EvoSim.Runtime, as a student's Assets/Student assembly would")]
        public IEnumerator StubsCompile()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Temp", "EvoSimStubs");
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            System.IO.Directory.CreateDirectory(dir);
            var sources = Exercises.Path.Select(t =>
            {
                string path = System.IO.Path.Combine(dir, Exercises.StubName(t) + ".cs");
                System.IO.File.WriteAllText(path, Exercises.Template(t));
                return path;
            }).ToArray();
            string scripts = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Library", "ScriptAssemblies");
#pragma warning disable CS0618                     // AssemblyBuilder is deprecated but still the editor's in-process compiler in 6000.3
            var builder = new UnityEditor.Compilation.AssemblyBuilder(System.IO.Path.Combine(dir, "Stubs.dll"), sources)
            {
                referencesOptions = UnityEditor.Compilation.ReferencesOptions.UseEngineModules,
                additionalReferences = new[] { System.IO.Path.Combine(scripts, "EvoSim.Runtime.dll") },
            };
            var errors = new System.Collections.Generic.List<string>();
            builder.buildFinished += (path, messages) =>
            {
                foreach (var m in messages)
                    if (m.type == UnityEditor.Compilation.CompilerMessageType.Error) errors.Add(m.message);
            };
#pragma warning restore CS0618
            Assert.IsTrue(builder.Build(), "the build started");
            int guard = 0;
            while (builder.status != UnityEditor.Compilation.AssemblyBuilderStatus.Finished && guard++ < 6000) yield return null;
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test, Description("21 §6: Replace with stub keeps the module's inspector settings and its place among the components; the world runs with it")]
        public void SwapKeepsSettingsAndPlace()
        {
            made = WorldFactory.NewWorld("Exercise World");
            var species = WorldFactory.NewSpecies(made, "grazer");
            foreach (var a in new[] { "Eat", "Rest", "Mate" }) WorldFactory.AddModule(species, "Actions", a);
            foreach (var s in new[] { "Energy", "Food", "Animal", "Age" }) WorldFactory.AddModule(species, "Senses", s);
            var litter = species.GetComponentInChildren<Litter>(true);
            litter.Configure(3, 5, 25f);
            var go = litter.gameObject;
            int place = System.Array.IndexOf(go.GetComponents<Component>(), litter);

            var stub = (Litter)Exercises.Swap(litter, typeof(ExerciseProbe));
            Assert.IsInstanceOf<ExerciseProbe>(stub);
            Assert.AreEqual(place, System.Array.IndexOf(go.GetComponents<Component>(), stub), "the same place (ARCH-04)");
            Assert.AreEqual((3, 5, 25f), (stub.Min, stub.Max, stub.ChildEnergy), "settings kept");
            Assert.AreEqual(1, go.GetComponents<Litter>().Length, "the reference is gone");

            // A field the two classes share by name is carried over, the base class's included (the action's prompt line).
            var eat = species.GetComponentInChildren<EatAction>(true);
            eat.SetDescription("graze the nearest grass");
            SetString(eat, "relevantSense", "Animal");
            var hunt = (AnimalAction)Exercises.Swap(eat, typeof(HuntAction));
            Assert.AreEqual("graze the nearest grass", hunt.Description);
            Assert.AreEqual("Animal", new SerializedObject(hunt).FindProperty("relevantSense").stringValue);
            SetString(Exercises.Swap(hunt, typeof(EatAction)), "relevantSense", "Food");

            var w = made.GetComponent<World>();
            w.WaitMode = WaitMode.Freeze;
            w.StartOnPlay = false;
            w.NoMutation = true;
            foreach (var r in made.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.IsInstanceOf<ExerciseProbe>(w.FindSpecies("grazer").Module<Litter>(), "a stub derived from the reference is found by type (open question 10)");
            w.Advance(50);
            Assert.AreEqual(50, w.Tick);
        }

        static void SetString(Component c, string field, string value)
        {
            var so = new SerializedObject(c);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test, Description("21 §6: Restore reference puts the reference module back, with the settings, wherever an exercise class stands")]
        public void RestoreReference()
        {
            made = WorldFactory.NewWorld("Exercise World");
            var species = WorldFactory.NewSpecies(made, "grazer");
            var litter = species.GetComponentInChildren<Litter>(true);
            litter.Configure(2, 6, 30f);
            int place = System.Array.IndexOf(litter.gameObject.GetComponents<Component>(), litter);
            var go = litter.gameObject;
            Exercises.Swap(litter, typeof(ExerciseProbe));
            Assert.GreaterOrEqual(Exercises.RestoreAll(), 1);
            var back = go.GetComponent<Litter>();
            Assert.AreEqual(typeof(Litter), back.GetType());
            Assert.AreEqual(place, System.Array.IndexOf(go.GetComponents<Component>(), back));
            Assert.AreEqual((2, 6, 30f), (back.Min, back.Max, back.ChildEnergy));
            Assert.IsNull(Exercises.StubType(typeof(Litter)), "the menu only takes the student's My<Reference> class, never a test probe");
        }
    }
}
