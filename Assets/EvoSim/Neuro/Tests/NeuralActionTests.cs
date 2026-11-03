using System;
using System.Collections.Generic;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Neuro.Tests
{
    /// <summary>
    /// The neural species' actions (plan M16 D1, D5, D6, §5.5): steering (T-NEURO-09), contacts (T-NEURO-10) and breeding
    /// both ways (T-NEURO-11).
    /// </summary>
    public class NeuralActionTests : WorldFixture
    {
        const int Left = 0, Right = 1, Walk = 2, Run = 3, Stop = 4, Rest = 5;

        /// <summary>The action each placed animal does every tick, by animal id; the scripted brain reads it.</summary>
        readonly Dictionary<int, int> pick = new Dictionary<int, int>();

        [SetUp]
        public void ClearPicks() => pick.Clear();

        float[] Row(DecisionQuery q)
        {
            var row = new float[q.Species.Actions.Count];
            row[pick.TryGetValue(q.AnimalId, out int k) && k < row.Length ? k : row.Length - 1] = 1f;
            return row;
        }

        /// <summary>
        /// A 60 m world, empty food layer, carcasses and eggs; a prey with the five neural actions (graze) and rest, and a
        /// predator with the five (strike, scavenge). Breeding needs energy 50 and no maturity; litters of one cost 40.
        /// </summary>
        World ActWorld(string name, bool sexual = false, float mutationRate = 0f, Action<TurnAction> left = null)
        {
            return New(4, name: name).Flat(60, 60).Food(0f, 0f).Carcasses().Service<EggSystem>("Eggs").ReferencePhases()
                .DefaultBrain<ScriptedBrain>(b => { b.ReadsText = false; b.TakesAttachments = true; b.Policy = Row; })
                .Species("prey", s =>
                {
                    s.PreyBody(run: 2f).Sense<InteroceptionSense>().Module<HungerClock>();   // an attachment: queries are never shared
                    s.Get<MatingRule>().Configure(50f, 0f, 1f, sexual);
                    NeuralActions(s, NeuralContacts.Graze, left);
                    s.Action<RestAction>("rest", withGene: false);
                    s.Module<Litter>(l => l.Configure(1, 1, 40f))
                     .Module<NeuralGenomeGene>(g => { g.SetLabel("brain"); g.Configure(new[] { 4 }, 8); }, "Brain")
                     .Module<NeuralMutation>(m => m.Rate = mutationRate).Module<NeuronCrossover>()
                     .Population(0);
                })
                .Species("predator", s =>
                {
                    s.PredatorBody("prey").Sense<InteroceptionSense>().Module<HungerClock>();
                    s.Get<Diet>().SetKillChance(1f);
                    s.Get<MatingRule>().Configure(500f, 0f, 1f, sexual);
                    NeuralActions(s, NeuralContacts.Strike | NeuralContacts.Scavenge, null);
                    s.Population(0);
                })
                .Build();
        }

        static void NeuralActions(SpeciesSetup s, NeuralContacts contacts, Action<TurnAction> left) => s
            .Action<TurnAction>("left", t => { t.Configure(false); left?.Invoke(t); t.SetContacts(contacts); }, withGene: false)
            .Action<TurnAction>("right", t => { t.Configure(true); t.SetContacts(contacts); }, withGene: false)
            .Action<ForwardAction>("walk", f => { f.Configure(false); f.SetContacts(contacts); }, withGene: false)
            .Action<ForwardAction>("run", f => { f.Configure(true); f.SetContacts(contacts); }, withGene: false)
            .Action<StopAction>("stop", x => x.SetContacts(contacts), withGene: false);

        Animal Put(World w, string species, float x, float z, int action, float heading = 0f)
        {
            var a = Place.Animal(w.FindSpecies(species), Place.At(x, z), heading: heading);
            pick[a.Id] = action;
            return a;
        }

        static float Dist(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        // ---- T-NEURO-09 ----

        [Test, Description("T-NEURO-09 (plan D1): left for 9 ticks turns the heading by −20° a tick at walk speed; right by +20°")]
        public void TurnsAtWalkSpeed()
        {
            var w = ActWorld("turn");
            var l = Put(w, "prey", 20f, 30f, Left);
            var r = Put(w, "prey", 40f, 30f, Right);
            for (int t = 1; t <= 9; t++)
            {
                var pl = l.Position;
                var pr = r.Position;
                w.Advance(1);
                Assert.AreEqual(0f, Mathf.DeltaAngle(l.Heading, -20f * t), 1e-3f, $"left, tick {t}");
                Assert.AreEqual(0f, Mathf.DeltaAngle(r.Heading, 20f * t), 1e-3f, $"right, tick {t}");
                Assert.AreEqual(1f, Dist(pl, l.Position), 1e-4f, "at walk speed");
                Assert.AreEqual(1f, Dist(pr, r.Position), 1e-4f);
            }
        }

        [Test, Description("T-NEURO-09 (plan §5.5): a turn of 35° at run speed turns −35° a tick and moves the run speed")]
        public void TurnsAtRunSpeed()
        {
            var w = ActWorld("turn run", left: t => t.Configure(false, 35f, NeuralSpeed.Run));
            var a = Put(w, "prey", 30f, 30f, Left);
            for (int t = 1; t <= 4; t++)
            {
                var p = a.Position;
                w.Advance(1);
                Assert.AreEqual(0f, Mathf.DeltaAngle(a.Heading, -35f * t), 1e-3f, $"tick {t}");
                Assert.AreEqual(2f, Dist(p, a.Position), 1e-4f, "at run speed");
            }
        }

        [Test, Description("T-NEURO-09 (plan D1): walk and run keep the heading and move their speed; stop doesn't move, pays the rest cost and recovers stamina like rest")]
        public void ForwardAndStop()
        {
            var w = ActWorld("forward");
            var walker = Put(w, "prey", 10f, 10f, Walk, heading: 90f);
            var runner = Put(w, "prey", 10f, 30f, Run, heading: 90f);
            var stopper = Put(w, "prey", 40f, 10f, Stop);
            var rester = Put(w, "prey", 40f, 40f, Rest);
            foreach (var a in new[] { stopper, rester }) { a.SetStat("stamina", 10f); a.SetStat("energy", 45f); }   // not ready: no litter to pay
            var p0 = (walker.Position, runner.Position, stopper.Position);
            w.Advance(1);
            Assert.AreEqual(90f, walker.Heading, 1e-3f);
            Assert.AreEqual(90f, runner.Heading, 1e-3f);
            Assert.AreEqual(1f, Dist(p0.Item1, walker.Position), 1e-4f);
            Assert.AreEqual(2f, Dist(p0.Item2, runner.Position), 1e-4f);
            Assert.AreEqual(p0.Item3, stopper.Position, "stop stays");
            Assert.Greater(stopper.StatOf("stamina"), 10f, "stamina recovers");
            Assert.AreEqual(rester.StatOf("stamina"), stopper.StatOf("stamina"), 1e-5f, "as at rest");
            Assert.AreEqual(rester.StatOf("energy"), stopper.StatOf("energy"), 1e-5f, "the rest cost");
        }

        // ---- T-NEURO-10 ----

        [Test, Description("T-NEURO-10 (plan D5, ACT-10): grass 0.8 m ahead → the neural prey walks and grazes in the same tick; stop on food grazes too")]
        public void WalkOntoGrassGrazes()
        {
            var w = ActWorld("graze");
            var grass = w.Service<FoodGrid>();
            Place.Food(grass, Place.At(30.5f, 31.5f));
            Place.Food(grass, Place.At(10.5f, 10.5f));
            var walker = Put(w, "prey", 30.5f, 30.7f, Walk);
            var stopper = Put(w, "prey", 10.5f, 10.5f, Stop);
            w.Advance(1);
            Assert.AreEqual(1, walker.Meals, "walked onto the grass and ate it");
            Assert.AreEqual(1, stopper.Meals, "stop still grazes what is in reach");
            Assert.AreEqual(0, grass.Count);
        }

        [Test, Description("T-NEURO-10 (plan D5): a prey 0.8 m ahead → the neural predator strikes it, gets busy digesting, and the prey leaves a carcass")]
        public void WalkIntoPreyStrikes()
        {
            var w = ActWorld("strike");
            var prey = Put(w, "prey", 30f, 30.8f, Stop);
            var hunter = Put(w, "predator", 30f, 30f, Walk);
            w.Advance(1);
            Assert.IsTrue(prey.Killed || prey.IsGone, "struck and killed (kill chance 1)");
            Assert.IsTrue(hunter.IsBusy, "busy digesting");
            Assert.AreEqual(1, w.Service<CarcassSystem>().EntityCount, "a carcass is left");
        }

        [Test, Description("T-NEURO-10 (ACT-03): at most one interaction a tick, strike before scavenge before graze, on what is within reach of the step's end")]
        public void OneContactPerTick()
        {
            var w = ActWorld("one contact");
            var predator = w.FindSpecies("predator");
            var hunter = Put(w, "predator", 30f, 30f, Stop);
            var prey = Put(w, "prey", 30f, 30.5f, Stop);
            var carcass = Place.Carcass(w.Service<CarcassSystem>(), w.FindSpecies("prey"), Place.At(30.5f, 30f));
            var stop = predator.Actions[Stop];
            w.Space.Rebuild();
            var c = new ActContext(w);
            c.Begin(hunter);
            stop.Act(hunter, c);
            Assert.AreEqual(InteractionKind.Strike, c.Interaction.Kind, "strike first");
            Assert.AreSame(prey, c.Interaction.Target);
            prey.Killed = true;
            c.Begin(hunter);
            stop.Act(hunter, c);
            Assert.AreEqual(InteractionKind.Scavenge, c.Interaction.Kind, "then scavenge");
            Assert.AreSame(carcass, c.Interaction.Carcass);

            var walk = predator.Actions[Walk];
            hunter.Heading = 90f;
            c.Begin(hunter);
            walk.Act(hunter, c);
            Assert.AreEqual(InteractionKind.Scavenge, c.Interaction.Kind, "the carcass is within reach of the step's end too");
            hunter.Heading = 180f;
            c.Begin(hunter);
            walk.Act(hunter, c);
            Assert.IsTrue(c.Interaction.IsNone, "walking away, nothing is within reach of the step's end");
        }

        // ---- T-NEURO-11 ----

        [Test, Description("T-NEURO-11 (REPRO-04, plan D5): asexual → a ready animal breeds alone whatever neural action it does and pays the litter; an action with mating off doesn't breed; without mutation the baby has its parent's network")]
        public void AsexualBreeding()
        {
            var w = ActWorld("asexual");
            var prey = w.FindSpecies("prey");
            ((TurnAction)prey.Actions[Right]).SetContacts(NeuralContacts.Graze, breeds: false);
            var walker = Put(w, "prey", 10f, 10f, Walk);
            var turner = Put(w, "prey", 40f, 40f, Right);
            float before = walker.StatOf("energy");
            w.Advance(1);
            Assert.AreEqual(1, walker.Offspring, "bred alone");
            Assert.Less(walker.StatOf("energy"), before - 39f, "paid the litter (40)");
            Assert.AreEqual(0, turner.Offspring, "mating off: no baby");
            var baby = WaitForBaby(w, walker);
            int locus = prey.Module<NeuralGenomeGene>().Locus;
            Assert.AreSame(walker.Genome[locus], baby.Genome[locus], "no mutation: the parent's allele (GENE-31)");
        }

        [Test, Description("T-NEURO-11 (REPRO-02, plan §5.3): sexual → only on contact with a ready kin; the baby's network is a neuron mix registered with both parents")]
        public void SexualBreeding()
        {
            var w = ActWorld("sexual", sexual: true);
            var prey = w.FindSpecies("prey");
            var gene = prey.Module<NeuralGenomeGene>();
            var alone = Put(w, "prey", 10f, 10f, Stop);
            var far = Put(w, "prey", 10f, 14f, Stop);
            var a = Place.Animal(prey, Place.At(40f, 40f), GenomeWith(w, prey, 1));
            var b = Place.Animal(prey, Place.At(40f, 40.5f), GenomeWith(w, prey, 2));
            pick[a.Id] = Stop;
            pick[b.Id] = Stop;
            w.Advance(1);
            Assert.AreEqual(0, alone.Offspring + far.Offspring, "4 m apart: no contact, no baby");
            Assert.AreEqual(1, a.Offspring);
            Assert.AreEqual(1, b.Offspring);
            var baby = WaitForBaby(w, a);
            var allele = baby.Genome[gene.Locus];
            Assert.AreEqual("recombinant", allele.Origin);
            CollectionAssert.AreEquivalent(new[] { a.Genome[gene.Locus].Id, b.Genome[gene.Locus].Id }, new[] { allele.ParentId, allele.SecondParentId });
        }

        [Test, Description("T-NEURO-11 (MUT-01): with rate 1 every baby's network mutates (operator nn-*); with rate 0 none does")]
        public void MutationAtRate()
        {
            foreach (var rate in new[] { 0f, 1f })
            {
                var w = ActWorld("rate " + rate, mutationRate: rate);
                var prey = w.FindSpecies("prey");
                int locus = prey.Module<NeuralGenomeGene>().Locus;
                var parents = Enumerable.Range(0, 4).Select(i => Put(w, "prey", 10f + 10f * i, 10f, Stop)).ToList();
                w.Advance(1);
                foreach (var p in parents)
                {
                    var baby = WaitForBaby(w, p);
                    if (rate == 0f) Assert.AreSame(p.Genome[locus], baby.Genome[locus], "rate 0");
                    else
                    {
                        Assert.AreNotSame(p.Genome[locus], baby.Genome[locus], "rate 1");
                        Assert.AreEqual(p.Genome[locus].Id, baby.Genome[locus].ParentId);
                        StringAssert.StartsWith("nn-", baby.Genome[locus].Operator);
                    }
                }
            }
        }

        /// <summary>A founder genome whose network is founder number <paramref name="k"/> of the pool.</summary>
        static Genome GenomeWith(World w, Species s, int k)
        {
            var alleles = new Allele[s.Genes.Count];
            for (int i = 0; i < alleles.Length; i++)
            {
                var pool = s.Genes[i].FounderPool;
                var f = pool[Math.Min(k, pool.Count - 1)];
                alleles[i] = w.Alleles.Register(s.Genes[i], f.Value, f.Origin);
            }
            return new Genome(s, alleles);
        }

        /// <summary>Advances until the parent's baby has hatched (at most 5 ticks) and returns it.</summary>
        static Animal WaitForBaby(World w, Animal parent)
        {
            for (int t = 0; t < 5; t++)
            {
                var baby = parent.Species.Animals.FirstOrDefault(x => x.Parents != null && x.Parents.Contains(parent.Id));
                if (baby != null) return baby;
                w.Advance(1);
            }
            Assert.Fail($"no baby of {parent} hatched");
            return null;
        }
    }
}
