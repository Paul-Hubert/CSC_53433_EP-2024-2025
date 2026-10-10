using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EvoSim.Tests
{
    /// <summary>
    /// T-EDIT-01 (EDIT-01, EDIT-02): every validator of the catalogue (21 §2) has a fixture that triggers it, the clean
    /// Lab 1 world triggers none, and every message names the object at fault. V-60's reachability half is on demand
    /// (Test connection, covered in the HTTP tests); its static half (LLM mutation without a mutator) is here.
    /// </summary>
    public class ValidationCatalogueTests : WorldFixture
    {
        sealed class Lab
        {
            public World World;
            public Species Prey, Predator;
            public T Of<T>(Species s) where T : Component => s.GetComponentInChildren<T>(true);
            public GameObject Under(Species s, string name) { var go = new GameObject(name); go.transform.SetParent(s.transform, false); return go; }
        }

        Lab Make(string name)
        {
            var w = New(5, name: name).Lab1(prey: 6, predators: 2).BuildUninitialized();
            var lab = new Lab { World = w };
            foreach (var s in w.GetComponentsInChildren<Species>(true))
            {
                if (s.gameObject.name == "prey") lab.Prey = s;
                if (s.gameObject.name == "predator") lab.Predator = s;
            }
            return lab;
        }

        static TextAsset Model(string file) => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/EvoSim/Data/Models/JEV-9B/" + file);

        static TextGene EatGene(Lab l)
        {
            var g = l.Prey.GetComponentInChildren<EatAction>(true).GetComponent<TextGene>();
            g.SetPool(null);
            return g;
        }

        static readonly Dictionary<string, Action<Lab>> Fixtures = new Dictionary<string, Action<Lab>>
        {
            ["V-01"] = l => l.Under(l.Prey, "inner").AddComponent<Species>(),
            ["V-02"] = l => { var go = new GameObject("orphan"); go.transform.SetParent(l.World.transform, false); go.AddComponent<RestAction>(); },
            ["V-03"] = l => { var go = new GameObject("eat"); go.transform.SetParent(l.Of<EatAction>(l.Prey).transform.parent, false); go.AddComponent<RestAction>(); },
            ["V-04"] = l => { var s = new GameObject("empty"); s.transform.SetParent(l.World.transform, false); s.AddComponent<Species>().SetNames("empty", "empty"); s.AddComponent<KinematicLocomotion>(); },
            ["V-05"] = l => l.Under(l.Prey, "wait").AddComponent<RestAction>(),
            ["V-06"] = l => l.Under(l.Prey, "loose").AddComponent<TextGene>().Configure(new[] { "Wait." }),
            ["V-07"] = l => { var s = new GameObject("prey again"); s.transform.SetParent(l.World.transform, false); s.AddComponent<Species>().SetNames("prey", "prey again"); },
            ["V-08"] = l => l.World.GetComponentInChildren<ChooseActionsPhase>(true).transform.SetSiblingIndex(0),
            ["V-09"] = l => Object.DestroyImmediate(l.World.GetComponentInChildren<DeathPhase>(true).gameObject),
            ["V-10"] = l => { l.World.DefaultBrain = null; },
            ["V-11"] = l => { var b = new GameObject("tight").AddComponent<ScriptedBrain>(); b.transform.SetParent(l.World.transform, false); b.ActionLimit = 2; l.World.DefaultBrain = b; },
            ["V-12"] = l =>
            {
                l.World.Prepare(new ValidationReport());
                l.Prey.AcceptSignature();
                l.Of<EatAction>(l.Prey).transform.SetAsLastSibling();
            },
            ["V-13"] = l => l.Prey.gameObject.AddComponent<KinematicLocomotion>(),
            ["V-14"] = l => l.Prey.GetComponentInChildren<FleeAction>(true).GetComponent<TextGene>().SetLabel("eat"),
            ["V-20"] = l => l.Of<Diet>(l.Prey).Set("Nothing"),
            ["V-21"] = l => Object.DestroyImmediate(l.Predator.gameObject),
            ["V-22"] = l => l.Of<Diet>(l.Prey).Set(new Diet.Entry[0]),
            ["V-23"] = l => EatGene(l).Configure(new[] { "Run from any wolf you see." }),
            ["V-24"] = l => Object.DestroyImmediate(l.Prey.GetComponent<Edible>()),
            ["V-25"] = l => l.Predator.gameObject.AddComponent<Edible>(),
            ["V-30"] = l => l.Prey.GetComponentInChildren<NearestResourceSense>(true).SetBands(new[] { 10f, 4f }, 20f),
            ["V-31"] = l => l.Prey.GetComponentInChildren<LevelSense>(true).Configure("energy", 70f, 30f),
            ["V-32"] = l => l.Of<CapRule>(l.Prey).Configure(1),
            ["V-33"] = l => l.Of<Litter>(l.Prey).Configure(0, 2, 40f),
            ["V-34"] = l => l.Of<Litter>(l.Prey).Configure(4, 6, 40f),
            ["V-35"] = l => l.World.GetComponentInChildren<FoodGrid>(true).Configure(2f, 0.01f),
            ["V-36"] = l => l.Under(l.Prey, "second stamina").AddComponent<Stamina>().Configure(999f),
            ["V-40"] = l => EatGene(l).Configure(new[] { "Eat @ once." }),
            ["V-41"] = l => EatGene(l).Configure(new string[0], withNeutral: false),
            ["V-42"] = l => EatGene(l).Configure(new[] { "Eat now.", "Eat now." }),
            ["V-43"] = l => EatGene(l).Configure(new[] { "Eat now." }, withNeutral: false),
            ["V-44"] = l => EatGene(l).Configure(new[] { "Eat now." }),
            ["V-45"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("nothing.at.all", 0f, 1f, new[] { 0.5f }),
            ["V-46"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 500f }),
            ["V-47"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 60f }),
            ["V-48"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 60f }),
            ["V-50"] = l => { for (int i = 0; i < 12; i++) l.Under(l.Prey, "level " + i).AddComponent<LevelSense>().Configure("energy", 30f, 70f); },
            ["V-51"] = l => l.Under(l.Prey, "camera").AddComponent<CameraProbeSense>(),
            ["V-53"] = l => l.Of<Locomotion>(l.Prey).SetPromptRule("Animals move {nothing} meters."),
            ["V-60"] = l => Object.DestroyImmediate(l.World.GetComponentInChildren<MutatorService>(true).gameObject),
            ["V-61"] = l =>
            {
                var j = new GameObject("jev").AddComponent<JevBrain>();
                j.transform.SetParent(l.World.transform, false);
                j.SetModelFiles(Model("decision_head.json"), Model("calibration.json"));
                j.ConfigureClient("http://localhost:8000", 5f, 1, 0f, 1, "sk-123456789");
            },
            ["V-62"] = l =>
            {
                string file = Path.Combine(Application.temporaryCachePath, "evosim-not-a-folder");
                File.WriteAllText(file, "a file, so the folder below it can't exist");
                var c = new GameObject("cache").AddComponent<AnswerCache>();
                c.transform.SetParent(l.World.transform, false);
                c.SetFolder(Path.Combine(file, "cache"));
            },
        };

        static IEnumerable<string> Codes() => Fixtures.Keys.OrderBy(k => k, StringComparer.Ordinal);

        [Test, Description("T-EDIT-01 (EDIT-01, EDIT-02): each validator's fixture triggers it, and every message names an object")]
        public void EachValidatorFires([ValueSource(nameof(Codes))] string code)
        {
            var lab = Make("catalogue " + code);
            Fixtures[code](lab);
            var report = new ValidationReport();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            lab.World.Prepare(report);
            Assert.IsTrue(report.Has(code), $"{code} not reported; got: {string.Join(" | ", report.Messages.Select(m => m.ToString()))}");
            foreach (var m in report.Messages) Assert.IsNotNull(m.Object, $"{m.Id} names no object (EDIT-02): {m.Text}");
        }

        [Test, Description("T-EDIT-01 (EDIT-01): the clean Lab 1 world triggers none of the catalogue's validators")]
        public void CleanWorldIsQuiet()
        {
            var lab = Make("catalogue clean");
            var report = new ValidationReport();
            lab.World.Prepare(report);
            Assert.IsEmpty(report.Messages.Select(m => m.ToString()), "a clean world validates green");
        }

        [Test, Description("21 §2: the catalogue's codes are exactly the ones with fixtures (plus V-14, duplicate gene labels, a decision of 50)")]
        public void EveryCatalogueCodeHasAFixture()
        {
            var doc = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Docs", "Interface", "21-editor-tooling.md"));
            var codes = System.Text.RegularExpressions.Regex.Matches(doc, @"^\| (V-\d+) \|", System.Text.RegularExpressions.RegexOptions.Multiline)
                .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            Assert.Greater(codes.Count, 40);
            CollectionAssert.IsSubsetOf(codes, Fixtures.Keys, "a fixture per catalogue code");
        }
    }
}
