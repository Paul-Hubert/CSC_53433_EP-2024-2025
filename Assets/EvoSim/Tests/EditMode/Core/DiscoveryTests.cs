using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>Discovery and ownership (20 §4, ARCH-04…06, SPEC-02…05, GENE-05).</summary>
    public class DiscoveryTests : WorldFixture
    {
        [Test, Description("T-CORE-01 (SPEC-05, ARCH-05): a species inside another species → Initialize fails with V-01, nothing spawned")]
        public void NestedSpeciesIsAnError()
        {
            var b = New().Flat(10, 10).Species("outer", s => s.Population(3).Action<ProbeAction>());
            var outer = b.Root.transform.Find("outer");
            var inner = new GameObject("inner");
            inner.transform.SetParent(outer, false);
            var sp = inner.AddComponent<Species>();
            sp.SetNames("inner", "inner");
            sp.SetInitialPopulation(3);
            var w = b.BuildUninitialized();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("V-01"));
            Assert.IsFalse(w.Initialize());
            Assert.IsTrue(w.LastReport.Has("V-01"), w.LastReport.ToString());
            var msg = w.LastReport.Messages.First(m => m.Id == "V-01");
            Assert.AreEqual(inner, ((Component)msg.Object).gameObject, "the message names the object at fault (EDIT-02)");
            Assert.IsNotNull(msg.Fix, "V-01 offers 'move it up'");
            Assert.IsFalse(w.IsInitialized);
            Assert.AreEqual(0, outer.GetComponent<Species>().Animals.Count, "nothing spawned");
        }

        [Test, Description("T-CORE-03 (ARCH-06): a module on an inactive GameObject and a disabled component are absent from the lists and the signature")]
        public void DisabledModulesAreAbsent()
        {
            var b = New().Flat(10, 10).Species("prey", s => s
                .Action<ProbeAction>("one")
                .Action<ProbeAction>("two")
                .Action<ProbeAction>("three")
                .Sense<ProbeSense>(name: "s1")
                .Sense<ProbeSense>(name: "s2"));
            var sp = b.Root.GetComponentInChildren<Species>(true);
            var reference = New(name: "Reference").Flat(10, 10).Species("prey", s => s
                .Action<ProbeAction>("one")
                .Sense<ProbeSense>(name: "s1")).Build().AllSpecies[0];

            sp.transform.Find("Actions/two").gameObject.SetActive(false);                  // inactive GameObject
            sp.transform.Find("Actions/three").GetComponent<ProbeAction>().enabled = false;  // disabled component
            sp.transform.Find("Actions/three").GetComponent<TextGene>().enabled = false;
            sp.transform.Find("Senses/s2").GetComponent<ProbeSense>().enabled = false;
            b.Build();

            CollectionAssert.AreEqual(new[] { "one" }, sp.Actions.Select(a => a.Name).ToArray());
            Assert.AreEqual(1, sp.Senses.Count);
            Assert.AreEqual(1, sp.Genes.Count, "the gene of the inactive action is absent too");
            Assert.AreEqual(reference.Signature, sp.Signature, "the signature equals a species built without them");
        }

        [Test, Description("T-CORE-04 (GENE-05, ARCH-05): a gene on a child of an action, on the action's GameObject, and elsewhere marked free")]
        public void GenesBindToTheNearestAction()
        {
            var b = New().Flat(10, 10).Species("prey", s => s
                .Action<ProbeAction>("eat", withGene: false)
                .Action<ProbeAction>("flee"));
            var sp = b.Root.GetComponentInChildren<Species>(true);
            var childGene = new GameObject("gene").AddComponent<TextGene>();
            childGene.transform.SetParent(sp.transform.Find("Actions/eat"), false);
            var free = new GameObject("temperament").AddComponent<TextGene>();
            free.transform.SetParent(sp.transform, false);
            free.Configure(new[] { "Be bold." }, isFree: true);
            free.SetLabel("temperament");
            b.Build();

            var eat = sp.FindAction("eat");
            var flee = sp.FindAction("flee");
            Assert.AreSame(childGene, eat.Gene, "a gene on a child of the action");
            Assert.AreSame(eat, childGene.Action);
            Assert.AreEqual("eat", childGene.Label, "labelled with the action's name");
            Assert.AreSame(flee.GetComponent<TextGene>(), flee.Gene, "a gene on the action's GameObject");
            Assert.IsNull(free.Action, "a free gene stays free");
            Assert.AreEqual("temperament", free.Label);
            CollectionAssert.AreEqual(new[] { "prey.eat", "prey.flee", "prey.temperament" }, sp.Genes.Select(g => g.LocusId).ToArray());
        }

        [Test, Description("T-CORE-05 (SPEC-02, SPEC-03): two actions swapped → action order and signature follow; V-12 raised")]
        public void SwappingActionsChangesOrderAndSignature()
        {
            var b = New().Flat(10, 10).Species("prey", s => s.Action<ProbeAction>("eat").Action<ProbeAction>("flee"));
            var w = b.Build();
            var sp = w.AllSpecies[0];
            CollectionAssert.AreEqual(new[] { "eat", "flee" }, sp.Actions.Select(a => a.Name).ToArray());
            CollectionAssert.AreEqual(new[] { "prey.eat", "prey.flee" }, sp.Genes.Select(g => g.LocusId).ToArray());
            string before = sp.Signature;
            sp.AcceptSignature();

            sp.transform.Find("Actions/flee").SetSiblingIndex(0);
            Assert.IsTrue(w.Initialize(), w.LastReport.ToString());
            CollectionAssert.AreEqual(new[] { "flee", "eat" }, sp.Actions.Select(a => a.Name).ToArray());
            CollectionAssert.AreEqual(new[] { "prey.flee", "prey.eat" }, sp.Genes.Select(g => g.LocusId).ToArray(), "locus order follows");
            Assert.AreEqual(0, sp.Actions[0].Index);
            Assert.AreNotEqual(before, sp.Signature);
            Assert.IsTrue(w.LastReport.Has("V-12"), "the signature change is reported");
            sp.AcceptSignature();
            Assert.IsTrue(w.Initialize());
            Assert.IsFalse(w.LastReport.Has("V-12"), "accepted");
        }

        [Test, Description("V-02, V-03, V-04, V-07 (21 §2): structural errors are found before the run")]
        public void StructuralErrors()
        {
            var b = New().Flat(10, 10)
                .Species("prey", s => s.Action<ProbeAction>("eat").Action<ProbeAction>("eat2"))
                .Species("empty")
                .Species("prey", id: "prey2");
            b.Root.transform.Find("prey/Actions/eat2").name = "eat";
            new GameObject("orphan").AddComponent<ProbeSense>().transform.SetParent(b.Root.transform, false);
            var w = b.BuildUninitialized();
            var r = new ValidationReport();
            Assert.IsFalse(w.Prepare(r));
            Assert.IsTrue(r.Has("V-02"), r.ToString());
            w.transform.Find("orphan").gameObject.SetActive(false);
            r = new ValidationReport();
            Assert.IsFalse(w.Prepare(r));
            Assert.IsTrue(r.Has("V-07"), "two species named prey");
            w.transform.Find("prey").GetComponent<Species>().SetNames("prey", "prey");
            Object.DestroyImmediate(w.transform.GetChild(w.transform.childCount - 2).gameObject);   // the second "prey"
            r = new ValidationReport();
            Assert.IsFalse(w.Prepare(r));
            Assert.IsTrue(r.Has("V-03"), "two actions named eat: " + r);
            Assert.IsTrue(r.Has("V-04"), "a species with no action: " + r);
            Assert.IsTrue(r.Messages.All(m => m.Object != null), "every message names an object (EDIT-02)");
        }

        [Test, Description("T-CORE-02 (CORE-02): a test-only sense, action and phase from the test assembly are discovered and run with no change to runtime code")]
        public void TestModulesAreDiscovered()
        {
            var w = New().Flat(10, 10)
                .Phase<TracePhase>()
                .Species("prey", s => s.Population(2).Action<ProbeAction>().Sense<ProbeSense>())
                .Build();
            var sp = w.AllSpecies[0];
            Assert.IsInstanceOf<ProbeAction>(sp.Actions[0]);
            Assert.IsInstanceOf<ProbeSense>(sp.Senses[0]);
            Assert.AreEqual(1, w.Phases.Count);
            w.Advance(3);
            Assert.AreEqual(3, w.Phase<TracePhase>().Log.Count, "the test phase runs every tick");
            Assert.AreEqual(2, sp.Animals.Count);
            Assert.AreEqual(2, w.Events.Count, "two founder events recorded");
        }

        [Test, Description("SPEC-06 (part of T-CORE-06): one module definition used by two species keeps each species' own values")]
        public void SharedDefinitionsKeepTheirOwnValues()
        {
            var w = New().Flat(10, 10)
                .Species("prey", s => s.Action<ProbeAction>("rest", a => a.SetDescription("prey line")))
                .Species("predator", s => s.Action<ProbeAction>("rest", a => a.SetDescription("predator line")))
                .Build();
            Assert.AreEqual("prey line", w.AllSpecies[0].Actions[0].Description);
            Assert.AreEqual("predator line", w.AllSpecies[1].Actions[0].Description);
            Assert.AreEqual("prey.rest", w.AllSpecies[0].Genes[0].LocusId);
            Assert.AreEqual("predator.rest", w.AllSpecies[1].Genes[0].LocusId);
        }
    }
}
