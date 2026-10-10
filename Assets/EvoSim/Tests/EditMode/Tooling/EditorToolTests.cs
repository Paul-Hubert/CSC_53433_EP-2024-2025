using System.Linq;
using EvoSim.Editor;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Editor tooling (21): building from the menus, the play-mode gate.</summary>
    public class EditorToolTests : WorldFixture
    {
        GameObject made;

        [TearDown] public void Clean() { if (made != null) Object.DestroyImmediate(made); }

        [Test, Description("M10 done-when (21 §6): a world built only from the menus — New World, New Species, Add Action, Add Sense — validates and runs")]
        public void WorldFromTheMenus()
        {
            made = WorldFactory.NewWorld("Menu World");
            var species = WorldFactory.NewSpecies(made, "grazer");
            foreach (var a in new[] { "Eat", "Rest", "Mate" }) WorldFactory.AddModule(species, "Actions", a);
            foreach (var s in new[] { "Energy", "Food", "Animal", "Age" }) WorldFactory.AddModule(species, "Senses", s);
            var w = made.GetComponent<World>();
            w.WaitMode = WaitMode.Freeze;
            w.StartOnPlay = false;
            w.NoMutation = true;
            foreach (var r in made.GetComponentsInChildren<RunRecorder>(true)) r.WriteFiles = false;
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            Assert.IsInstanceOf<RandomBrain>(w.DefaultBrain, "a new world runs without a model server");
            Assert.AreEqual(new[] { "eat", "rest", "mate" }, w.FindSpecies("grazer").Actions.Select(a => a.Name).ToArray());
            w.Advance(200);
            Assert.AreEqual(200, w.Tick);
            Assert.Greater(w.FindSpecies("grazer").Counters.Decisions, 0);
        }

        [Test, Description("21 §6: every module prefab the menus offer exists and validates inside a new species")]
        public void EveryMenuModuleExists()
        {
            made = WorldFactory.NewWorld("Menu World");
            var species = WorldFactory.NewSpecies(made, "grazer");
            foreach (var a in WorldFactory.Actions) Assert.IsNotNull(WorldFactory.AddModule(species, "Actions", a), a);
            foreach (var s in WorldFactory.Senses) Assert.IsNotNull(WorldFactory.AddModule(species, "Senses", s), s);
            foreach (var g in WorldFactory.Genes) Assert.IsNotNull(WorldFactory.AddModule(species, "Genes", g), g);
        }

        static System.Collections.Generic.IEnumerable<string> ModulePrefabs() => UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/EvoSim/Modules" })
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p => !p.Contains("/Bodies/")).OrderBy(p => p, System.StringComparer.Ordinal);

        [Test, Description("T-EDIT-03 (EDIT-01): every module prefab validates without an error inside the reference prey or predator")]
        public void EveryModulePrefabValidates([ValueSource(nameof(ModulePrefabs))] string path)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            var problems = new System.Collections.Generic.List<string>();
            foreach (var who in new[] { "prey", "predator" })
            {
                var b = New(name: "prefab " + who).Lab1(prey: 2, predators: 1);
                var species = b.Root.GetComponentsInChildren<Species>(true).First(s => s.gameObject.name == who);
                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "probe " + prefab.name.ToLowerInvariant();
                instance.transform.SetParent(species.transform, false);
                foreach (var a in instance.GetComponentsInChildren<AnimalAction>(true))
                    if (species.GetComponentsInChildren<AnimalAction>(true).Any(x => x != a && x.Name == a.Name)) a.gameObject.name += " 2";
                foreach (var s in instance.GetComponentsInChildren<Sense>(true)) s.SetLabel("Probe " + s.Label);
                foreach (var g in instance.GetComponentsInChildren<Gene>(true)) g.SetLabel("probe." + g.Label);
                var report = new ValidationReport();
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
                b.BuildUninitialized().Prepare(report);
                problems.AddRange(report.Messages.Where(m => m.Severity == Severity.Error).Select(m => $"{who}: {m}"));
                if (problems.Count == 0) return;                                       // fits one of the two reference species
            }
            Assert.IsEmpty(problems, path);
        }

        [Test, Description("T-PROMPT-05 (PROMPT-06): the species inspector's preview equals the text the brain receives for the same animal (a prompt brain and JEV)")]
        public void PreviewEqualsWhatTheBrainReceives()
        {
            ScriptedBrain scripted = null;
            var w = New(3, name: "preview").Lab1(prey: 10, predators: 2, randomBrain: false)
                .DefaultBrain<ScriptedBrain>(b => { b.Instruction = "Pick one."; scripted = b; }).Build();
            w.Advance(1);
            var seen = new System.Collections.Generic.HashSet<string>(scripted.Seen.Select(q => q.Prompt(scripted)));
            foreach (var s in w.AllSpecies)
                foreach (var a in s.Animals)
                    if (a.LastObservation != null && a.LastDecisionTick == 0)                    // babies of tick 0 haven't decided yet
                        Assert.IsTrue(seen.Contains(PromptPreview.For(s, a.Genome, a.LastObservation, w.TextStyle, scripted)), $"{s.Id} {a.Id}");

            var fake = FakeHttpTransport.Jev();
            var j = New(3, name: "preview-jev").Lab1(prey: 10, predators: 2, randomBrain: false).DefaultBrain<JevBrain>(b =>
            {
                b.SetModelFiles(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/decision_head.json"),
                                UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/calibration.json"));
                b.Transport = fake;
                b.ConfigureClient("http://jev.test", 5f, 1, 0f, 4);
            }).Build();
            j.Advance(1);
            var sent = new System.Collections.Generic.HashSet<string>(fake.Requests.SelectMany(r => r.Body["prompt"].Select(t => (string)t)));
            var jev = j.GetComponentInChildren<JevBrain>();
            foreach (var s in j.AllSpecies)
                foreach (var a in s.Animals)
                    if (a.LastObservation != null && a.LastDecisionTick == 0)
                        Assert.IsTrue(sent.Contains(PromptPreview.For(s, a.Genome, a.LastObservation, j.TextStyle, jev)), $"JEV {s.Id} {a.Id}");
        }

        [UnityEngine.TestTools.UnityTest, Description("21 §6: every script template and its matching test compile against the EvoSim assemblies")]
        public System.Collections.IEnumerator ScriptTemplatesCompile()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Temp", "EvoSimTemplates");
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            System.IO.Directory.CreateDirectory(dir);
            var sources = new System.Collections.Generic.List<string>();
            foreach (var kind in ScriptTemplates.Kinds)
            {
                var (script, test) = ScriptTemplates.Render(kind, "My" + kind);
                StringAssert.Contains("class My" + kind, script);
                StringAssert.Contains("class My" + kind + "Tests : WorldFixture", test);
                foreach (var (name, text) in new[] { ("My" + kind, script), ("My" + kind + "Tests", test) })
                {
                    string path = System.IO.Path.Combine(dir, name + ".cs");
                    System.IO.File.WriteAllText(path, text);
                    sources.Add(path);
                }
            }
            string scripts = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Library", "ScriptAssemblies");
#pragma warning disable CS0618                     // AssemblyBuilder is deprecated but still the editor's in-process compiler in 6000.3
            var builder = new UnityEditor.Compilation.AssemblyBuilder(System.IO.Path.Combine(dir, "Templates.dll"), sources.ToArray())
            {
                referencesOptions = UnityEditor.Compilation.ReferencesOptions.UseEngineModules,
                additionalReferences = new[]
                {
                    System.IO.Path.Combine(scripts, "EvoSim.Runtime.dll"), System.IO.Path.Combine(scripts, "EvoSim.Testing.dll"),
                    typeof(Assert).Assembly.Location, typeof(UnityEngine.TestTools.LogAssert).Assembly.Location,
                },
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

        [Test, Description("EDIT-01: the play-mode gate finds a world's validation errors (a species without an action) and passes a valid one")]
        public void PlayModeGate()
        {
            made = WorldFactory.NewWorld("Gate World");
            var species = WorldFactory.NewSpecies(made, "grazer");
            var w = made.GetComponent<World>();
            var errors = EvoSim.Editor.PlayModeGate.Errors(new[] { w });
            Assert.IsTrue(errors.Any(m => m.Id == "V-04"), "no action: V-04 blocks Play");
            Assert.IsTrue(errors.All(m => m.Object != null), "every message names an object (EDIT-02)");
            WorldFactory.AddModule(species, "Actions", "Rest");
            Assert.IsEmpty(EvoSim.Editor.PlayModeGate.Errors(new[] { w }).Select(m => m.ToString()));
        }
    }
}
