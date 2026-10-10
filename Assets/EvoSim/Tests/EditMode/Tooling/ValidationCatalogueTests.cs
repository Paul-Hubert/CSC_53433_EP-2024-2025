using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EvoSim.Tests
{
    /// <summary>
    /// T-EDIT-01 (EDIT-01, EDIT-02): every validator of the catalogue (21 §2, with the codes 50 adds) has a fixture that
    /// triggers it with its severity, every fix the catalogue offers repairs its problem, the clean Lab 1 world triggers
    /// none, and every message names the object at fault. V-60 (unreachable server) is checked on demand.
    /// </summary>
    public class ValidationCatalogueTests : WorldFixture
    {
        sealed class Lab
        {
            public World World;
            public Species Prey, Predator;
            public T Of<T>(Species s) where T : Component => s.GetComponentInChildren<T>(true);
            public GameObject Under(Species s, string name) { var go = new GameObject(name); go.transform.SetParent(s.transform, false); return go; }
            public GameObject Under(Transform t, string name) { var go = new GameObject(name); go.transform.SetParent(t, false); return go; }
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

        static void SetField(Object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);

        static readonly Dictionary<string, Action<Lab>> Fixtures = new Dictionary<string, Action<Lab>>
        {
            ["V-01"] = l => l.Under(l.Prey, "inner").AddComponent<Species>(),
            ["V-02"] = l => l.Under(l.World.transform, "orphan").AddComponent<RestAction>(),
            ["V-03"] = l => l.Under(l.Of<EatAction>(l.Prey).transform.parent, "eat").AddComponent<RestAction>(),
            ["V-04"] = l => { var s = l.Under(l.World.transform, "empty"); s.AddComponent<Species>().SetNames("empty", "empty"); s.AddComponent<KinematicLocomotion>(); },
            ["V-05"] = l => l.Under(l.Prey, "wait").AddComponent<RestAction>(),
            ["V-06"] = l => l.Under(l.Prey, "loose").AddComponent<TextGene>().Configure(new[] { "Wait." }),
            ["V-07"] = l => l.Under(l.World.transform, "prey again").AddComponent<Species>().SetNames("prey", "prey again"),
            ["V-08"] = l => l.World.GetComponentInChildren<ChooseActionsPhase>(true).transform.SetSiblingIndex(0),
            ["V-09"] = l => Object.DestroyImmediate(l.World.GetComponentInChildren<DeathPhase>(true).gameObject),
            ["V-10"] = l => { l.World.DefaultBrain = null; },
            ["V-11"] = l => { var b = l.Under(l.World.transform, "tight").AddComponent<ScriptedBrain>(); b.ActionLimit = 2; l.World.DefaultBrain = b; },
            ["V-12"] = l =>
            {
                l.World.Prepare(new ValidationReport());
                l.Prey.AcceptSignature();
                l.Of<EatAction>(l.Prey).transform.SetAsLastSibling();
            },
            ["V-13"] = l => Object.DestroyImmediate(l.Of<Locomotion>(l.Prey)),
            ["V-14"] = l => l.Prey.GetComponentInChildren<FleeAction>(true).GetComponent<TextGene>().SetLabel("eat"),
            ["V-15"] = l => l.Under(l.Prey, "carcasses here").AddComponent<CarcassSystem>(),
            ["V-16"] = l => l.Under(l.World.transform, "inner world").AddComponent<World>(),
            ["V-17"] = l => l.Under(l.Prey, "energy again").AddComponent<LevelSense>().Configure("energy", 30f, 70f),
            ["V-18"] = l => l.Under(l.World.GetComponentInChildren<FoodGrid>(true).transform.parent, "Grass").AddComponent<FoodGrid>(),
            ["V-20"] = l => l.Of<Diet>(l.Prey).Set("Nothing"),
            ["V-21"] = l => Object.DestroyImmediate(l.Predator.gameObject),
            ["V-22"] = l => l.Of<Diet>(l.Prey).Set(new Diet.Entry[0]),
            ["V-23"] = l => EatGene(l).Configure(new[] { "Run from any wolf you see." }),
            ["V-24"] = l => Object.DestroyImmediate(l.Prey.GetComponent<Edible>()),
            ["V-25"] = l => l.Predator.gameObject.AddComponent<Edible>(),
            ["V-26"] = l => Object.DestroyImmediate(l.Of<Litter>(l.Prey)),
            ["V-30"] = l => l.Prey.GetComponentInChildren<NearestResourceSense>(true).SetBands(new[] { 10f, 4f }, 20f),
            ["V-31"] = l => l.Prey.GetComponentInChildren<LevelSense>(true).Configure("energy", 70f, 30f),
            ["V-32"] = l => l.Of<CapRule>(l.Prey).Configure(1),
            ["V-33"] = l => l.Of<Litter>(l.Prey).Configure(0, 2, 40f),
            ["V-34"] = l => l.Of<Litter>(l.Prey).Configure(4, 6, 40f),
            ["V-35"] = l => Assert.IsTrue(FieldPath.Set(l.World, "World/decisionPeriod", "0", out _)),
            ["V-36"] = l => l.Of<Stamina>(l.Prey).Configure(2000f),
            ["V-37"] = l => SetField(l.World.GetComponentInChildren<FlatGround>(true), "width", 0.5f),
            ["V-38"] = l => l.Under(l.Prey, "second stamina").AddComponent<Stamina>().Configure(999f),
            ["V-40"] = l => EatGene(l).Configure(new[] { "Eat @ once." }),
            ["V-41"] = l => EatGene(l).Configure(new string[0], withNeutral: false),
            ["V-42"] = l => EatGene(l).Configure(new[] { "Eat now.", "Eat now." }),
            ["V-43"] = l => EatGene(l).Configure(new[] { "Eat now." }, withNeutral: false),
            ["V-44"] = l => EatGene(l).Configure(new[] { "Eat now." }),
            ["V-45"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("nothing.at.all", 0f, 1f, new[] { 0.5f }),
            ["V-46"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 500f }),
            ["V-47"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 60f }),
            ["V-48"] = l => l.Under(l.Prey, "gene").AddComponent<NumberGene>().Configure("stamina.max", 20f, 120f, new[] { 60f }),
            ["V-49"] = l => l.Of<LlmMutation>(l.Prey).Configure(Testing.Lab.Deck, "The sentence below is a rule.\nThe world is 48 m wide."),
            ["V-50"] = l => { for (int i = 0; i < 12; i++) l.Under(l.Prey, "level " + i).AddComponent<LevelSense>().Configure("energy", 30f, 70f); },
            ["V-51"] = l => l.Under(l.Prey, "camera").AddComponent<CameraProbeSense>(),
            ["V-53"] = l => l.Of<Locomotion>(l.Prey).SetPromptRule("Animals move {nothing} meters."),
            ["V-61"] = l =>
            {
                var j = l.Under(l.World.transform, "jev").AddComponent<JevBrain>();
                j.SetModelFiles(Model("decision_head.json"), Model("calibration.json"));
                j.ConfigureClient("http://localhost:8000", 5f, 1, 0f, 1, "sk-123456789");
            },
            ["V-62"] = l =>
            {
                string file = Path.Combine(Application.temporaryCachePath, "evosim-not-a-folder");
                File.WriteAllText(file, "a file, so the folder below it can't exist");
                l.Under(l.World.transform, "cache").AddComponent<AnswerCache>().SetFolder(Path.Combine(file, "cache"));
            },
            ["V-63"] = l => Object.DestroyImmediate(l.World.GetComponentInChildren<MutatorService>(true).gameObject),
        };

        /// <summary>Each code's severity: 21 §2, and the codes 50 adds (V-15…V-18, V-26, V-37, V-38, V-49, V-63).</summary>
        static readonly Dictionary<string, Severity> Severities = new Dictionary<string, Severity>
        {
            ["V-05"] = Severity.Warning, ["V-06"] = Severity.Warning, ["V-09"] = Severity.Warning, ["V-12"] = Severity.Warning,
            ["V-15"] = Severity.Warning, ["V-21"] = Severity.Warning, ["V-22"] = Severity.Warning, ["V-23"] = Severity.Warning,
            ["V-25"] = Severity.Info, ["V-26"] = Severity.Warning, ["V-34"] = Severity.Warning, ["V-36"] = Severity.Warning,
            ["V-38"] = Severity.Warning, ["V-42"] = Severity.Warning, ["V-43"] = Severity.Warning, ["V-44"] = Severity.Info,
            ["V-46"] = Severity.Warning, ["V-47"] = Severity.Info, ["V-48"] = Severity.Warning, ["V-63"] = Severity.Warning,
        };

        static Severity SeverityOf(string code) => Severities.TryGetValue(code, out var s) ? s : Severity.Error;

        /// <summary>The codes whose catalogue row offers a fix that runs without the editor (V-20, V-36 and V-62 need a person's choice).</summary>
        static readonly string[] FixCodes =
        {
            "V-01", "V-03", "V-04", "V-05", "V-06", "V-07", "V-08", "V-09", "V-10", "V-12", "V-13", "V-24", "V-26", "V-30", "V-31",
            "V-35", "V-37", "V-41", "V-42", "V-43", "V-46", "V-48", "V-49", "V-61",
        };

        static IEnumerable<string> Codes() => Fixtures.Keys.OrderBy(k => k, StringComparer.Ordinal);

        [Test, Description("T-EDIT-01 (EDIT-01, EDIT-02): each validator's fixture triggers it with its severity, and every message names an object")]
        public void EachValidatorFires([ValueSource(nameof(Codes))] string code)
        {
            var lab = Make("catalogue " + code);
            Fixtures[code](lab);
            var report = new ValidationReport();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            lab.World.Prepare(report);
            Assert.IsTrue(report.Messages.Any(m => m.Id == code && m.Severity == SeverityOf(code)),
                          $"{code} ({SeverityOf(code)}) not reported; got: {string.Join(" | ", report.Messages.Select(m => m.ToString()))}");
            foreach (var m in report.Messages) Assert.IsNotNull(m.Object, $"{m.Id} names no object (EDIT-02): {m.Text}");
        }

        [Test, Description("EDIT-02: every fix the catalogue offers is there, and applying it removes its problem")]
        public void FixesRepairTheirProblem([ValueSource(nameof(FixCodes))] string code)
        {
            var lab = Make("fix " + code);
            Fixtures[code](lab);
            var report = new ValidationReport();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            lab.World.Prepare(report);
            var fixes = report.Messages.Where(m => m.Id == code && m.Fix != null).Select(m => m.Fix).ToList();
            Assert.IsNotEmpty(fixes, $"{code} offers a fix; got: {string.Join(" | ", report.Messages.Where(m => m.Id == code))}");
            foreach (var f in fixes) f.Apply();
            var after = new ValidationReport();
            lab.World.Prepare(after);
            Assert.IsFalse(after.Has(code), $"{code} is still there after its fix: {string.Join(" | ", after.Messages.Where(m => m.Id == code))}");
        }

        [Test, Description("T-EDIT-01 (EDIT-01): the clean Lab 1 world triggers none of the catalogue's validators")]
        public void CleanWorldIsQuiet()
        {
            var lab = Make("catalogue clean");
            var report = new ValidationReport();
            lab.World.Prepare(report);
            Assert.IsEmpty(report.Messages.Select(m => m.ToString()), "a clean world validates green");
        }

        [Test, Description("V-07 (SPEC-01): a species id that is another species' display name is ambiguous in diets and senses")]
        public void IdsAndNamesDontCross()
        {
            var lab = Make("cross names");
            lab.Prey.SetNames("deer", "prey");
            lab.Predator.SetNames("prey", "predator");
            var report = new ValidationReport();
            Assert.IsFalse(lab.World.Prepare(report));
            Assert.IsTrue(report.Messages.Any(m => m.Id == "V-07" && m.Text.Contains("ambiguous")), report.ToString());
        }

        [Test, Description("V-60 on demand (EDIT-01): Test connection reports an unreachable brain or mutator server as a V-60 warning, a reachable one as nothing")]
        public void UnreachableServers()
        {
            var jev = new GameObject("jev").AddComponent<JevBrain>();
            var client = jev.gameObject.AddComponent<OllamaMutatorClient>();
            try
            {
                jev.SetModelFiles(Model("decision_head.json"), Model("calibration.json"));
                jev.ConfigureClient("http://jev.test", 1f, 1, 0f, 1);
                client.ConfigureClient("http://mutator.test", 1f, 1, 0f, 1);
                foreach (bool down in new[] { true, false })
                {
                    jev.Transport = new FakeHttpTransport { FailWith = (p, b) => down ? 503 : 0 };
                    client.Transport = new FakeHttpTransport { FailWith = (p, b) => down ? 503 : 0 };
                    var report = new ValidationReport();
                    HttpChecks.Reachability(report, jev, System.Threading.Tasks.Task.Run(() => jev.TestConnection()).GetAwaiter().GetResult());
                    HttpChecks.Reachability(report, client, System.Threading.Tasks.Task.Run(() => client.TestConnection()).GetAwaiter().GetResult());
                    if (down) Assert.AreEqual(new[] { "V-60", "V-60" }, report.Messages.Where(m => m.Severity == Severity.Warning).Select(m => m.Id).ToArray());
                    else Assert.IsEmpty(report.Messages);
                }
            }
            finally { Object.DestroyImmediate(jev.gameObject); }
        }

        [Test, Description("21 §2: the catalogue's codes are exactly the ones with fixtures (plus the codes 50 adds); V-60 is the on-demand check above")]
        public void EveryCatalogueCodeHasAFixture()
        {
            var doc = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Docs", "Interface", "21-editor-tooling.md"));
            var codes = System.Text.RegularExpressions.Regex.Matches(doc, @"^\| (V-\d+) \|", System.Text.RegularExpressions.RegexOptions.Multiline)
                .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            Assert.Greater(codes.Count, 40);
            CollectionAssert.IsSubsetOf(codes.Where(c => c != "V-60"), Fixtures.Keys, "a fixture per catalogue code");
        }
    }
}
