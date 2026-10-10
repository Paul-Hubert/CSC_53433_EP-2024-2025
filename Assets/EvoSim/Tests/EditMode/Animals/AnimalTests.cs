using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Animals, stats, traits, metabolism, busy states and death (05).</summary>
    public class AnimalTests : WorldFixture
    {
        World ActWorld(System.Action<WorldBuilder> more = null)
        {
            var b = New().Flat(40, 40).Food(0f, 0f).Carcasses()
                .Phase<ActPhase>().Phase<DeathPhase>()
                .Species("prey", s => s.PreyBody().PreyActionSet().Action<GoToAction>("goto"))
                .Species("predator", s => s.PredatorBody(killChance: 1f).PredatorActionSet().Action<GoToAction>("goto"));
            more?.Invoke(b);
            return b.Build();
        }

        [Test, Description("T-ANIM-02 (ANIM-10, ANIM-11, ANIM-22): a stat written above its maximum reads the maximum; age exists without a module; no stamina module → no stamina stat")]
        public void StatsAreDeclaredByModules()
        {
            var w = New().Flat(10, 10).Species("probe", s => s.Action<ProbeAction>().Module<TraitProbe>()).Build();
            var sp = w.AllSpecies[0];
            var probe = sp.Module<TraitProbe>();
            var a = Place.Animal(sp, Place.At(1, 1));
            Assert.AreEqual(5f, a[probe.Level]);
            a[probe.Level] = 50f;
            Assert.AreEqual(10f, a[probe.Level], "clamped to the maximum");
            a[probe.Level] = -3f;
            Assert.AreEqual(0f, a[probe.Level], "and the minimum");
            Assert.AreEqual(0, a.Age, "age is built in");
            Assert.IsFalse(sp.Declarations.FindStat("stamina").IsValid, "no stamina module, no stamina");
            Assert.IsFalse(sp.Declarations.FindStat("energy").IsValid);
        }

        [Test, Description("T-ANIM-03 (ANIM-15, ANIM-16, ANIM-17, GENE-04): a number gene sets or scales a trait, clamped to its range; no gene → the default; unchanged after 1 000 ticks")]
        public void NumberGenesSetTraits()
        {
            World Make(string name, float value, bool multiply, bool gene = true) => New(name: name).Flat(10, 10).Phase<DeathPhase>()
                .Species("s", s =>
                {
                    s.Action<ProbeAction>().Module<TraitProbe>();
                    if (gene) s.Module<NumberGene>(g => g.Configure("probe.trait", 0f, 1000f, new[] { value }, multiply, "level"));
                }).Build();

            float TraitAfter(World w)
            {
                var sp = w.AllSpecies[0];
                var a = Place.Animal(sp, Place.At(1, 1));
                float before = a.Trait(sp.Module<TraitProbe>().Trait);
                w.Advance(1000);
                Assert.AreEqual(before, a.Trait(sp.Module<TraitProbe>().Trait), "traits don't change during life (ANIM-17)");
                return before;
            }

            Assert.AreEqual(75f, TraitAfter(Make("set", 75f, false)));
            Assert.AreEqual(90f, TraitAfter(Make("multiply", 1.5f, true)));
            Assert.AreEqual(120f, TraitAfter(Make("clamped", 500f, false)), "clamped to the trait's range [1, 120]");
            Assert.AreEqual(60f, TraitAfter(Make("none", 0f, false, gene: false)), "no gene: the module's setting");
        }

        [Test, Description("T-ANIM-04 (ANIM-20, ANIM-21, ANIM-22): the energy table of 05 §4, one case per row, after one tick")]
        public void EnergyTable()
        {
            void Case(string what, string species, string action, float energy0, float stamina0, float expectEnergy, float expectStamina,
                      System.Action<World, Animal> setup = null)
            {
                var w = ActWorld();
                var sp = w.FindSpecies(species);
                var a = Place.Animal(sp, Place.At(10.5f, 10.5f));
                a.SetStat("energy", energy0);
                a.SetStat("stamina", stamina0);
                a.Choose(action);
                setup?.Invoke(w, a);
                w.Advance(1);
                Assert.AreEqual(expectEnergy, a.StatOf("energy"), 1e-4f, what + ": energy");
                Assert.AreEqual(expectStamina, a.StatOf("stamina"), 1e-4f, what + ": stamina");
            }
            GoToAction Goto(Animal a) => (GoToAction)a.Species.FindAction("goto");

            Case("walk 1 m", "prey", "goto", 50, 60, 50 - 0.7f - 0.5f, 59, (w, a) => Goto(a).Target = Place.At(15.5f, 10.5f));
            Case("run 2 m", "predator", "goto", 50, 30, 50 - 0.7f - 1.0f, 28, (w, a) => { Goto(a).Target = Place.At(15, 10); Goto(a).Run = true; });
            Case("rest", "prey", "rest", 50, 60, 50 - 0.2f, 60);
            Case("other action, still", "prey", "goto", 50, 60, 50 - 0.7f, 60, (w, a) => Goto(a).Target = a.Position);
            Case("recovering while resting", "prey", "rest", 50, 50, 50 - 0.2f - 0.3f, 52);
            Case("recovering, other action", "prey", "goto", 50, 50, 50 - 0.7f - 0.3f, 52, (w, a) => Goto(a).Target = a.Position);
            Case("busy", "predator", "hunt", 50, 30, 50 - 0.2f, 30, (w, a) => { a.BusyTicks = 5; a.BusyReason = "digesting"; });
            Case("busy, recovering", "predator", "hunt", 50, 20, 50 - 0.2f - 0.3f, 22, (w, a) => { a.BusyTicks = 5; a.BusyReason = "digesting"; });
            Case("graze one item", "prey", "eat", 50, 60, 50 + 25 - 0.7f, 60, (w, a) => Place.Food(w.Service<FoodGrid>(), a.Position));
            Case("kill", "predator", "hunt", 30, 30, 30 + 60 - 0.7f, 30, (w, a) => Place.Animal(w.FindSpecies("prey"), a.Position + new Vector3(0.5f, 0, 0)));
            Case("carcass portion", "predator", "hunt", 30, 30, 30 + 30 - 0.7f, 30,
                 (w, a) => Place.Carcass(w.Service<CarcassSystem>(), w.FindSpecies("prey"), a.Position + new Vector3(0.5f, 0, 0)));
            Case("gains never pass the maximum", "prey", "eat", 90, 60, 100 - 0.7f, 60, (w, a) => Place.Food(w.Service<FoodGrid>(), a.Position));
        }

        [Test, Description("T-ANIM-05 (ANIM-21, MOVE-03): stamina 0.6 and an intent of 2 m → moves 0.6 m; stamina 0 → doesn't move")]
        public void StaminaLimitsMovement()
        {
            var w = ActWorld();
            var sp = w.FindSpecies("predator");
            var a = Place.Animal(sp, Place.At(10, 10));
            a.Choose("goto");
            var go = (GoToAction)sp.FindAction("goto");
            go.Target = Place.At(20, 10);
            go.Run = true;
            a.SetStat("stamina", 0.6f);
            w.Advance(1);
            Assert.AreEqual(10.6f, a.Position.x, 1e-4f);
            Assert.AreEqual(0f, a.StatOf("stamina"), 1e-5f);
            var b = Place.Animal(sp, Place.At(5, 5));
            b.Choose("goto");
            b.SetStat("stamina", 0f);
            w.Advance(1);
            Assert.AreEqual(Place.At(5, 5), b.Position, "no stamina, no movement");
        }

        [Test, Description("T-ANIM-06 (ANIM-30, DEC-02, DEC-01): a busy animal doesn't move, pays the busy cost, recovers stamina, and has no action after its busy ticks")]
        public void BusyAnimals()
        {
            var w = ActWorld();
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
            a.Choose("goto");
            ((GoToAction)a.Species.FindAction("goto")).Target = Place.At(30, 10);
            a.BusyTicks = 3;
            a.BusyReason = "digesting";
            a.SetStat("energy", 50f);
            a.SetStat("stamina", 40f);
            for (int t = 1; t <= 3; t++)
            {
                w.Advance(1);
                Assert.AreEqual(Place.At(10, 10), a.Position, $"tick {t}: no movement");
                Assert.AreEqual(50f - t * 0.5f, a.StatOf("energy"), 1e-4f, $"tick {t}: busy cost and recovery cost");
                Assert.AreEqual(40f + 2f * t, a.StatOf("stamina"), 1e-4f, $"tick {t}: stamina recovers");
            }
            Assert.AreEqual(0, a.BusyTicks);
            Assert.AreEqual(-1, a.Action, "no action: it decides at the start of the 4th tick");
            Assert.IsNull(a.BusyReason);
        }

        [Test, Description("T-ANIM-07 (ANIM-31): digestion after a kill and after a portion → 50 and 25 ticks, reason \"digesting\"")]
        public void Digestion()
        {
            var w = ActWorld();
            var predator = w.FindSpecies("predator");
            var prey = w.FindSpecies("prey");
            var hunter = Place.Animal(predator, Place.At(10, 10));
            hunter.Choose("hunt");
            Place.Animal(prey, Place.At(10.5f, 10));
            var scavenger = Place.Animal(predator, Place.At(30, 30));
            scavenger.Choose("hunt");
            Place.Carcass(w.Service<CarcassSystem>(), prey, Place.At(30.5f, 30));
            w.Advance(1);
            Assert.AreEqual(50, hunter.BusyTicks);
            Assert.AreEqual("digesting", hunter.BusyReason);
            Assert.AreEqual(25, scavenger.BusyTicks);
            Assert.AreEqual("digesting", scavenger.BusyReason);
        }

        [Test, Description("T-ANIM-08 (ANIM-35, ANIM-36): an animal created this tick has age 1 after the death phase; adult at age 150")]
        public void AgeAndMaturity()
        {
            var w = ActWorld();
            var sp = w.FindSpecies("prey");
            var a = Place.Animal(sp, Place.At(5, 5));
            a.Choose("rest");
            Assert.AreEqual(0, a.Age);
            w.Advance(1);
            Assert.AreEqual(1, a.Age);
            var rule = sp.Module<MatingRule>();
            a.Age = 149;
            Assert.IsFalse(rule.IsAdult(a));
            a.Age = 150;
            Assert.IsTrue(rule.IsAdult(a));
        }

        [Test, Description("T-ANIM-09, part (ANIM-40, ANIM-42): energy 0, age 1 501 and a kill → starvation, old_age, killed with the killer; each recorded once")]
        public void CausesOfDeath()
        {
            var w = ActWorld();
            w.Events.Lines = new System.Collections.Generic.List<string>();
            var prey = w.FindSpecies("prey");
            var starving = Place.Animal(prey, Place.At(5, 5)); starving.Choose("rest"); starving.SetStat("energy", 0.1f);
            var old = Place.Animal(prey, Place.At(15, 15)); old.Choose("rest"); old.Age = 1500;
            var victim = Place.Animal(prey, Place.At(30, 30)); victim.Choose("rest");
            var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(30.5f, 30)); hunter.Choose("hunt");
            w.Advance(1);
            var deaths = w.Events.Lines.Where(l => l.Contains("\"kind\": \"death\"")).ToList();
            Assert.AreEqual(3, deaths.Count, string.Join("\n", deaths));
            Assert.That(deaths.Single(l => l.Contains($"\"id\": {starving.Id},")), Does.Contain("\"cause\": \"starvation\""));
            Assert.That(deaths.Single(l => l.Contains($"\"id\": {old.Id},")), Does.Contain("\"cause\": \"old_age\""));
            var kill = deaths.Single(l => l.Contains($"\"id\": {victim.Id},"));
            Assert.That(kill, Does.Contain("\"cause\": \"killed\""));
            Assert.That(kill, Does.Contain($"\"killer\": {hunter.Id}"));
            Assert.IsFalse(prey.Animals.Contains(victim), "gone before the next tick (ANIM-43)");
            Assert.AreEqual(1, prey.Counters.DeathsBy("starvation"));
            w.Advance(3);
            Assert.AreEqual(3, w.Events.Lines.Count(l => l.Contains("\"kind\": \"death\"")), "recorded once");
        }

        [Test, Description("T-ANIM-10 (ANIM-41, ANIM-43): a prey animal killed early in the act phase (all mixed) is not targeted later, doesn't act, and is gone after the death phase")]
        public void KilledAnimalsAreOutOfTheTick()
        {
            int killedBeforeActing = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var w = New(seed).Flat(40, 40).Food(0f, 0f).Carcasses()
                    .Phase<ActPhase>(p => p.Order = ActOrder.AllMixed).Phase<DeathPhase>()
                    .Species("prey", s => s.PreyBody().PreyActionSet().Action<ProbeAction>("probe"))
                    .Species("predator", s => s.PredatorBody(killChance: 1f).PredatorActionSet())
                    .Build();
                var prey = w.FindSpecies("prey");
                var victim = Place.Animal(prey, Place.At(10, 10));
                victim.Choose("probe");
                var h1 = Place.Animal(w.FindSpecies("predator"), Place.At(10.5f, 10)); h1.Choose("hunt");
                var h2 = Place.Animal(w.FindSpecies("predator"), Place.At(9.5f, 10)); h2.Choose("hunt");
                var probe = (ProbeAction)prey.FindAction("probe");
                w.Advance(1);
                Assert.AreEqual(1, prey.Counters.DeathsBy("killed"), $"seed {seed}");
                Assert.AreEqual(1, w.FindSpecies("predator").Counters.Kills, $"seed {seed}: the dead prey is never struck again");
                Assert.LessOrEqual(probe.Calls, 1, $"seed {seed}");
                Assert.AreEqual(0, probe.CallsWhileKilled, $"seed {seed}: a killed animal doesn't act");
                Assert.IsFalse(prey.Animals.Contains(victim), "gone after the death phase");
                if (probe.Calls == 0) killedBeforeActing++;
            }
            Assert.Greater(killedBeforeActing, 0, "in some orders the prey is killed before its turn");
        }
    }
}
