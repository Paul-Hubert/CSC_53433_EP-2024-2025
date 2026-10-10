using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>Mutation: rates, operators, the LLM deck with a fake mutator, guards, asynchronous answers (10).</summary>
    public class MutationTests : WorldFixture
    {
        /// <summary>A species with five number genes and a Gaussian operator (no model needed).</summary>
        World NumberWorld(float rate, float sigma = 5f)
        {
            return New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs")
                .Species("s", s =>
                {
                    s.PreyBody().Action<RestAction>("rest", founders: new[] { "Rest." });
                    for (int i = 0; i < 5; i++)
                        s.Module<NumberGene>(g => g.Configure("stamina.max", 20f, 120f, new[] { 60f }, false, "g" + i), "Gene" + i);
                    s.Module<GaussianMutation>(m => { m.Rate = rate; m.Sigma = sigma; });
                }).Build();
        }

        static Egg EggOf(Species s, World w) => new Egg { Species = s, Alleles = s.Genes.Select(g => w.Alleles.Register(g, g.FounderPool[0].Value, "founder")).ToArray() };

        [Test, Description("T-MUT-01 (MUT-01, CTRL-01): rate 0.03 over 100 000 baby-loci → 3 % attempts (± 4 s.e.); rate 0 → no new allele")]
        public void MutationRate()
        {
            var w = NumberWorld(0.03f);
            var s = w.AllSpecies[0];
            var rng = new RandomStream(2, "mut");
            int numberLoci = s.Genes.Count(g => g.Kind == AlleleKind.Number);
            int babies = 100000 / numberLoci, attempts = 0;
            for (int i = 0; i < babies; i++)
            {
                var egg = EggOf(s, w);
                BreedPhase.DrawMutations(w, s, egg, rng);
                attempts += egg.Mutations.Count(m => m.Operator is GaussianMutation);
            }
            Stat.Binomial(attempts, babies * numberLoci, 0.03, 4, "attempts");

            var w0 = NumberWorld(0f);
            var s0 = w0.AllSpecies[0];
            int before = w0.Alleles.Count;
            for (int i = 0; i < 1000; i++) { var egg = EggOf(s0, w0); BreedPhase.DrawMutations(w0, s0, egg, rng); Assert.AreEqual(0, egg.Mutations.Count); }
            Assert.AreEqual(before, w0.Alleles.Count, "rate 0: no new allele");
        }

        [Test, Description("T-MUT-02 (MUT-02): with no operator configured for a gene the species' default for its kind applies; a configured operator wins; one operator per locus")]
        public void OperatorChoice()
        {
            var w = New().Flat(20, 20).Food(0f, 0f).FakeMutation()
                .Species("s", s =>
                {
                    s.PreyBody().Action<RestAction>("rest", founders: new[] { "Rest." }).Action<EatAction>("eat", founders: new[] { "Eat." });
                    s.Module<NumberGene>(g => g.Configure("stamina.max", 20f, 120f, new[] { 60f }, false, "stamina"));
                    s.LlmMutation().Module<GaussianMutation>();
                    s.Module<LlmMutation>(m => m.Configure(Testing.Lab.Deck, ""), "Special");
                }).Build();
            var s0 = w.AllSpecies[0];
            var special = s0.Modules.OfType<LlmMutation>().Last();
            special.SetOnlyThese(new[] { s0.FindAction("eat").Gene });
            var rest = s0.FindAction("rest").Gene;
            var eat = s0.FindAction("eat").Gene;
            var stamina = s0.Genes.First(g => g.Kind == AlleleKind.Number);
            Assert.AreSame(s0.Modules.OfType<LlmMutation>().First(), BreedPhase.OperatorFor(s0, rest), "the default for text genes");
            Assert.AreSame(special, BreedPhase.OperatorFor(s0, eat), "the configured one wins");
            Assert.IsInstanceOf<GaussianMutation>(BreedPhase.OperatorFor(s0, stamina), "the default for number genes");
        }

        [Test, Description("T-MUT-03 (MUT-03, OUT-03, GENE-12): a mutation registers a new allele and is listed in the birth event with locus, parent, child, operator detail and value")]
        public void MutationsAreRecorded()
        {
            var w = New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").FakeMutation()
                .Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f))
                .Build();
            w.Events.Lines = new List<string>();
            var s = w.AllSpecies[0];
            foreach (var x in new[] { 10f, 10.5f })
            {
                var a = Place.Animal(s, Place.At(x, 10));
                a.Age = 200; a.SetStat("energy", 100); a.Choose("mate");
            }
            w.Advance(1);
            var birth = w.Events.Lines.First(l => l.Contains("\"kind\": \"birth\""));
            var mutation = (JObject)JObject.Parse(birth)["mutations"][0];
            Assert.AreEqual("prey.mate", (string)mutation["locus"]);
            Assert.AreEqual("prey.mate:0", (string)mutation["parent"]);
            StringAssert.StartsWith("prey.mate:", (string)mutation["child"]);
            Assert.AreEqual("Mate often quickly.", (string)mutation["text"]);
            Assert.That((int)mutation["prompt"], Is.InRange(0, 6), "the instruction number");
            var allele = w.Alleles.ById((string)mutation["child"]);
            Assert.AreEqual("mutant", allele.Origin);
            Assert.AreEqual("prey.mate:0", allele.ParentId);
            StringAssert.StartsWith("llm#", allele.Operator);
            Assert.AreEqual("fake", allele.Model);
            Assert.IsNotNull(allele.Seed);
        }

        [Test, Description("T-MUT-04 (MUT-04, MUT-05, MUT-13): a fake mutator whose every answer is rejected → after 5 tries the inherited allele stays; counters record the rejections by reason")]
        public void RejectedEverywhere()
        {
            var w = New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").FakeMutation(f => f.Answer = (sentence, seed, prompt) => sentence)
                .Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f))
                .Build();
            var s = w.AllSpecies[0];
            foreach (var x in new[] { 10f, 10.5f })
            {
                var a = Place.Animal(s, Place.At(x, 10));
                a.Age = 200; a.SetStat("energy", 100); a.Choose("mate");
            }
            w.Advance(1);
            var babies = s.Animals.Where(a => a.Origin == "birth").ToList();
            Assert.IsTrue(babies.All(b => b.Genome[0].Id == "prey.mate:0"), "the inherited allele stays");
            int jobs = babies.Count;
            Assert.AreEqual(jobs * 5, w.GetComponentInChildren<FakeMutator>().Calls, "5 tries each");
            Assert.AreEqual(jobs * 5, w.Mutations.Rejections["invalid"], "equal to the parent: invalid");
            Assert.AreEqual(jobs, w.Mutations.Attempts);
            Assert.AreEqual(0, w.Mutations.Successes);
            Assert.AreEqual(jobs, w.Mutations.Failures);
        }

        [Test, Description("T-MUT-05 (MUT-10): a deck with comments and blank lines → only instruction lines are drawn; adding a line adds an instruction")]
        public void DeckIsData()
        {
            var deck = MutationText.Instructions(Testing.Lab.Deck.text);
            Assert.AreEqual(7, deck.Count, "the reference deck v4");
            Assert.AreEqual("Make the rule in this sentence a little weaker.", deck[0]);
            Assert.IsTrue(deck.All(l => !l.StartsWith("#")));
            var more = MutationText.Instructions(Testing.Lab.Deck.text + "\n\n# a comment\nMake it rhyme.\n");
            Assert.AreEqual(8, more.Count);
            Assert.AreEqual("Make it rhyme.", more.Last());
        }

        [Test, Description("T-MUT-06 (MUT-11, CORE-07): the mutator's prompt is exactly the context line, one instruction, the quoted sentence and the reply line — no world data, no other gene")]
        public void BlindPrompts()
        {
            var deck = MutationText.Instructions(Testing.Lab.Deck.text);
            Assert.AreEqual("The sentence below is a rule that a wild animal follows.\nChange when this rule applies.\n\n\"Rest when you are tired.\"\n\nReply with the new sentence only.",
                            MutationText.Prompt("The sentence below is a rule that a wild animal follows.", deck[1], "Rest when you are tired."));
            Assert.AreEqual("Change when this rule applies.\n\n\"Rest when you are tired.\"\n\nReply with the new sentence only.",
                            MutationText.Prompt("", deck[1], "Rest when you are tired."));

            var w = New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").FakeMutation().Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." })
                    .Action<RestAction>("rest", founders: new[] { "Rest a lot." }).LifeRules(100, 0).LlmMutation(1f)).Build();
            var sp = w.AllSpecies[0];
            foreach (var x in new[] { 10f, 10.5f }) { var a = Place.Animal(sp, Place.At(x, 10)); a.Age = 200; a.SetStat("energy", 100); a.Choose("mate"); }
            w.Advance(1);
            var prompts = w.GetComponentInChildren<FakeMutator>().Prompts;
            Assert.Greater(prompts.Count, 0);
            foreach (var p in prompts)
            {
                var lines = p.Split('\n');
                Assert.AreEqual(6, lines.Length, p);
                Assert.AreEqual("The sentence below is a rule that a wild animal follows.", lines[0]);
                CollectionAssert.Contains(deck, lines[1]);
                Assert.IsTrue(lines[3] == "\"Mate often.\"" || lines[3] == "\"Rest a lot.\"", "only its own gene");
                Assert.AreEqual("Reply with the new sentence only.", lines[5]);
                Assert.IsFalse(p.Contains("prey") || p.Contains("energy"), "no world data");
            }
        }

        [Test, Description("T-MUT-07 (MUT-12): cleaning and guards — quoted answers, preambles, two sentences, 13 words, case only, punctuation only, forbidden characters — give the prototype's results")]
        public void CleaningMatchesThePrototype()
        {
            var cases = JArray.Parse(Golden.Read("mutation_cleaning.json"));
            Assert.Greater(cases.Count, 10);
            foreach (JObject c in cases)
            {
                string cleaned = MutationText.Clean((string)c["answer"]);
                Assert.AreEqual((string)c["clean"], cleaned, (string)c["answer"]);
                string why = cleaned.Length == 0 ? "invalid" : MutationText.Reject(cleaned, (string)c["parent"], 12);
                Assert.AreEqual((string)c["reject"], why, (string)c["answer"]);
            }
        }

        [Test, Description("T-MUT-08 (MUT-14): the same mutation prompt and seed twice through the cache → one model call")]
        public void MutatorCache()
        {
            var dir = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "evosim-mutator-cache");
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            int Run()
            {
                var w = New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs").Service<AnswerCache>("Answer cache", c => c.SetFolder(dir))
                    .Service<MutatorService>("Mutator", m => m.Configure("fake", 1.2f)).Service<FakeMutator>("client")
                    .Phase<BreedPhase>().Phase<HatchPhase>()
                    .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f)).Build();
                var sp = w.AllSpecies[0];
                foreach (var x in new[] { 10f, 10.5f }) { var a = Place.Animal(sp, Place.At(x, 10)); a.Age = 200; a.SetStat("energy", 100); a.Choose("mate"); }
                w.Advance(1);
                return w.GetComponentInChildren<FakeMutator>().Calls;
            }
            int first = Run();
            Assert.Greater(first, 0);
            Assert.AreEqual(0, Run(), "the same prompts and seeds: answered from the cache");
        }

        [Test, Description("T-MUT-09 (MUT-20): Gaussian mutation near the range edges → always clamped; the same seed gives the same value")]
        public void GaussianIsClamped()
        {
            var w = NumberWorld(1f, sigma: 50f);
            var s = w.AllSpecies[0];
            var gene = (NumberGene)s.Genes.First(g => g.Kind == AlleleKind.Number);
            var op = s.Module<GaussianMutation>();
            var nearTop = w.Alleles.Register(gene, AlleleValue.OfNumber(118f), "founder");
            var rng = new RandomStream(1, "gauss");
            for (int i = 0; i < 1000; i++)
            {
                float v = op.Start(gene, nearTop, rng).Result.Number;
                Assert.That(v, Is.InRange(20f, 120f));
            }
            Assert.AreEqual(op.Start(gene, nearTop, new RandomStream(9, "g")).Result.Number, op.Start(gene, nearTop, new RandomStream(9, "g")).Result.Number);
        }

        [Test, Description("T-MUT-11 (MUT-31): 12 babies conceived in one tick, 3 answers rejected → one first batch of all requests, then one redraw batch")]
        public void MutationRounds()
        {
            int calls = 0;
            var w = New().Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs")
                .FakeMutation(f => f.Answer = (sentence, seed, prompt) => ++calls <= 3 ? sentence : sentence.TrimEnd('.') + " quickly.")
                .Configure(x => x.Asexual = true).Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", s => { s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f); s.Get<Litter>().Configure(3, 3, 40f); })
                .Build();
            var sp = w.AllSpecies[0];
            for (int i = 0; i < 4; i++) { var a = Place.Animal(sp, Place.At(5 + 3 * i, 10)); a.Age = 200; a.SetStat("energy", 100); a.Choose("mate"); }
            w.Advance(1);
            CollectionAssert.AreEqual(new[] { 12, 3 }, w.GetComponentInChildren<FakeMutator>().BatchSizes);
            Assert.AreEqual(12, sp.Animals.Count(a => a.Origin == "birth"));
        }

        [Test, Description("T-MUT-12 (MUT-32, REPRO-24): incubation 2 with answers 5 calls late → hatching waits; with answers in time → no wait")]
        public void HatchingWaitsForTheMutator()
        {
            int Waits(int delay)
            {
                var w = New(waitMode: WaitMode.Responsive, name: "delay " + delay).Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs")
                    .FakeMutation(f => f.DelayCalls = delay).Phase<BreedPhase>().Phase<HatchPhase>()
                    .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0, incubation: 2).LlmMutation(1f))
                    .Configure(x => x.TickLimit = 6).Build();
                var sp = w.AllSpecies[0];
                foreach (var x in new[] { 10f, 10.5f }) { var a = Place.Animal(sp, Place.At(x, 10)); a.Age = 200; a.SetStat("energy", 100); a.Choose("mate"); }
                while (w.State != RunState.Stopped) w.Advance(1);
                Assert.Greater(sp.Counters.Hatched, 0);
                return w.WaitCount;
            }
            Assert.Greater(Waits(5), 0, "answers 5 calls late: the hatch waits");
            Assert.AreEqual(0, Waits(1), "answers in time: no wait");
        }

        [Test, Description("T-GENE-11, mutation part (GENE-24, MUT-22): a neutral allele marked as not mutating is never mutated over 10 000 babies; the other alleles of the gene are")]
        public void NeutralCanBeFrozen()
        {
            var w = New().Flat(20, 20).Food(0f, 0f).FakeMutation()
                .Species("prey", s => { s.PreyBody().Action<RestAction>("rest", founders: new[] { "Rest." }).LlmMutation(0.5f); s.Get<TextGene>().SetNeutralMutates(false); })
                .Build();
            var sp = w.AllSpecies[0];
            var gene = sp.Genes[0];
            var neutral = w.Alleles.Register(gene, AlleleValue.OfText("No preference."), "neutral");
            var founder = w.Alleles.Register(gene, AlleleValue.OfText("Rest."), "founder");
            var rng = new RandomStream(3, "neutral");
            int neutralMutations = 0, founderMutations = 0;
            for (int i = 0; i < 10000; i++)
            {
                var e1 = new Egg { Species = sp, Alleles = new[] { neutral } };
                var e2 = new Egg { Species = sp, Alleles = new[] { founder } };
                BreedPhase.DrawMutations(w, sp, e1, rng);
                BreedPhase.DrawMutations(w, sp, e2, rng);
                neutralMutations += e1.Mutations.Count;
                founderMutations += e2.Mutations.Count;
            }
            Assert.AreEqual(0, neutralMutations);
            Assert.Greater(founderMutations, 4000);
        }
    }
}
