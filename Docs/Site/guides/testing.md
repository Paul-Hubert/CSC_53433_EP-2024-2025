---
uid: guide-testing
---

# Testing

EvoSim's tests build small worlds in code, put animals exactly where a rule needs them, run a few ticks and check what
happened. No test calls a language model: a scripted brain and fake mutators and servers answer instead, so every test
repeats exactly with its seed. This page explains the test assemblies and the toolkit they share, `EvoSim.Testing`. To
test a module of your own step by step, and to see what the conformance suites check, read
[Testing your module](../extending/testing-your-module.md).

## The test assemblies

| Assembly | Folder | Holds | Where it runs |
|---|---|---|---|
| `EvoSim.Tests.EditMode` | `Assets/EvoSim/Tests/EditMode` | the unit tests of every system, the conformance suites, the static checks, the slow `T3` tests | *Window ▸ General ▸ Test Runner*, tab **EditMode** |
| `EvoSim.Tests.PlayMode` | `Assets/EvoSim/Tests/PlayMode` | whole Lab 1 runs driven by `FixedUpdate`: run files, views, wait modes (tier T2) | tab **PlayMode** |
| `EvoSim.Testing` | `Assets/EvoSim/Testing` | the toolkit described below, and the test probes | no tests of its own |
| `Student.Tests` | `Assets/Student/Tests` | your tests, written by *Assets ▸ Create ▸ EvoSim ▸ Script ▸* | tab **EditMode** |

All four compile only when the Test Framework is present (`UNITY_INCLUDE_TESTS`), so none of them is ever part of a
build. The EditMode folders follow the specification's chapters:

| Folder | Covers |
|---|---|
| `Actions` | intents, interactions, act orders, locomotion and movement (07) |
| `Animals` | stats, traits, metabolism, busy states, age, death (05) |
| `Config` | tooltips and ranges of every setting, scenario overrides (14) |
| `Conformance` | the suites run against every module of a kind (30 §4) |
| `Core` | discovery, the tick loop, random streams, open hooks, static checks, the coverage gate |
| `Decisions`, `Http` | queries, brains, memo, cache, prompts; HTTP brains and mutators against fake servers (08) |
| `Environment` | distances, food, cover, entities, the food web (02, 03, 04) |
| `Genes`, `Mutation`, `Reproduction` | alleles and founders (09), mutation (10), mating, eggs and population (11) |
| `Recording`, `Regression` | run files (13); pinned hashes, soak, performance, the reference scenes (30 §6) |
| `Samples`, `Scenarios`, `Senses`, `Tooling` | the samples, the scenarios of 31, senses and situation texts (06), the editor and the exercises |

Every test states what it checks in its `Description`, with the test id and the rules it keeps:
`[Test, Description("T-ACT-04 (ACT-10): …")]`. The coverage gate (`Core/CoverageTests.cs`) fails when a MUST rule of
the specification is cited by no test.

## WorldFixture and New

Derive your test class from <xref:EvoSim.Testing.WorldFixture> and start each world with
[New](xref:EvoSim.Testing.WorldFixture.New(System.Int32,EvoSim.WaitMode,System.String))`(seed = 1234, waitMode =
Freeze, name = "Test World")`. It returns an empty <xref:EvoSim.Testing.WorldBuilder>: a root with a
<xref:EvoSim.World> and its `Environment`, `Brains` and `Phases` objects, kept inactive until you build it. After each
test the fixture destroys every world it made. Give each world of one test its own `name` when a test builds several.
The default wait mode, `Freeze`, delivers late answers at once (<xref:EvoSim.WaitMode>).

## Building a world

Each call adds one thing and returns the builder, so the calls chain. A world has only what you add: **no phase runs
unless you add it**, and species start with **no founders**.

| WorldBuilder call | Adds |
|---|---|
| `Flat(width, depth)` | a <xref:EvoSim.FlatGround>, x from 0 to width and z from 0 to depth, in meters |
| `Ground<T>(width, depth)` | a flat ground of your subclass: <xref:EvoSim.Testing.PondGround>, <xref:EvoSim.Testing.RampGround>, <xref:EvoSim.Testing.DoubleDistanceGround> |
| `Food(initial, regrow, name, energy, hungryCover)` | a <xref:EvoSim.FoodGrid> named "Grass" with an <xref:EvoSim.Edible> (25 points an item); `Food(0f, 0f)` is empty and never regrows |
| `Cover(fraction)`, `Carcasses()` | a <xref:EvoSim.CoverLayer> "Thickets" (share 0.2), the <xref:EvoSim.CarcassSystem> |
| `Service<T>(name, configure)` | any <xref:EvoSim.WorldService>: brains, mutators and caches go under `Brains`, the rest under `Environment` |
| `DefaultBrain<T>(configure)` | a brain under `Brains`, made the World's default |
| `Phase<T>(configure)` | a phase after those added so far; phases run in that order (TICK-01) |
| `Species(name, s => …, id)` | a species, whose modules `s` (a <xref:EvoSim.Testing.SpeciesSetup>) adds |
| `Configure(w => …)` | any World setting now: `w.TextStyle`, `w.TickLimit`, `w.Asexual`, `w.DecisionPeriod`… |
| `Get<T>()`, `Root`, `World` | the first component of a type built so far; the root GameObject; the World |

| SpeciesSetup call | Adds |
|---|---|
| `Population(n)` | n founders at random walkable positions when the world initialises (POP-04) |
| `Action<T>(name, configure, founders, withGene)` | an action under `Actions` with a <xref:EvoSim.TextGene> on the same object; the name defaults to the class name without "Action", in lower case |
| `Sense<T>(configure, name)` | a sense under `Senses`; the call order is the order of the situation text (SPEC-03) |
| `Module<T>(configure, name)` | any other species module under `Life`: a stat, a rule, a locomotion, a crossover… |
| `On<T>(configure)` | a component on the species' own object: an <xref:EvoSim.Edible>, a <xref:EvoSim.SearchRule>, a <xref:EvoSim.Body> |
| `Get<T>()`, `Find<T>(gameObjectName)` | a module added so far, to wire one module to another |

Every `configure` argument is called at once with the new component: set its fields there, or keep a reference to it.

### The Lab recipes

<xref:EvoSim.Testing.Lab> adds the reference parts in one call each, with the prototype's values. They are extension
methods, so they chain with the builder's own calls.

| Call | Adds |
|---|---|
| `PreyBody()`, `PredatorBody()` | locomotion, energy, stamina (60 / 30), metabolism, diet, mating rule, starvation, old age; the predator also digests and strikes with kill chance 0.5 |
| `PreySenses()`, `PredatorSenses()` | the 7 and 6 reference senses, in situation-text order |
| `PreyActionSet()`, `PredatorActionSet()` | the 6 and 4 actions, each with its text gene, founder pool and contrast pair |
| `LifeRules(cap, floor, incubation)` | litters of 2 to 4, uniform crossover, incubation, a cap and a floor |
| `LlmMutation(rate)` | the LLM mutation with the reference deck v4; the world then needs a mutator |
| `ReferencePhases(order)` | the 11 reference phases in the order of TICK-04 |
| `FakeMutation(configure)` | a <xref:EvoSim.MutatorService> whose client is a <xref:EvoSim.Testing.FakeMutator> |
| `Recorder(files)`, `CheckInvariants()` | a run recorder (no files unless `files` is true); an <xref:EvoSim.Testing.InvariantPhase> |
| `Ecology(size, cover, food, prey, predator, regrow)` | a 48 m square with grass, cover, carcasses, eggs and both species, without founders, phases or brain |
| `Lab1(prey, predators, …)` | the Lab 1 small world (S03): 24 prey, 4 predators, caps and floors, the phases, the fake mutator, the random brain |

### Build or BuildUninitialized

[Build](xref:EvoSim.Testing.WorldBuilder.Build) activates the tree and calls
[Initialize](xref:EvoSim.World.Initialize): discovery, validation, founders. It returns the ready <xref:EvoSim.World>,
or throws with the validation report. To test validation itself, call
[BuildUninitialized](xref:EvoSim.Testing.WorldBuilder.BuildUninitialized) and validate yourself:

```cs
var w = New(name: "requires").Flat(20, 20).DefaultBrain<RandomBrain>()
    .Species("prey", s => s.Action<RestAction>("rest").Module<KinematicLocomotion>(name: "Locomotion").Module<NeedsEnergyProbe>())
    .BuildUninitialized();
var report = new ValidationReport();
Assert.IsTrue(w.Prepare(report), report.ToString());
Assert.AreEqual("Add Energy", report.Messages.Single(x => x.Id == "V-26").Fix.Label);
```

## Placing and steering animals

After `Build`, <xref:EvoSim.Testing.Place> sets up the exact situation a test needs. It records no event.

| Call | Does |
|---|---|
| `Place.At(x, z)` | a point on the ground plane |
| `Place.Animal(species, point, genome, heading)` | an animal with each gene's first founder allele (or the given genome), its height set from the ground; the species' modules hear `OnBorn` |
| `Place.WithGenes(species, point, "sentence", …)` | an animal whose text genes hold these sentences, locus by locus |
| `Place.Food(layer, point)`, `Place.Cover(cover, point)` | an item, or cover, on the cell that holds the point |
| `Place.Carcass(carcasses, species, point, killerId, portions, energy, lifetime)` | a carcass, as if an animal had been killed there |

A placed animal has chosen no action. `a.Choose("eat")` sets it by name, `a.SetStat("energy", 90f)` and
`a.StatOf("energy")` write and read a stat by name, and `a.Age = 200` makes it an adult. With only `.Phase<ActPhase>()`
the chosen action runs every tick; with the decision phases and a brain, the brain chooses again at the next decision.
[Advance](xref:EvoSim.World.Advance(System.Int32))`(n)` then runs n ticks.

## The scripted brain

<xref:EvoSim.Testing.ScriptedBrain> answers each query in this order:

1. `FailWhen(query)` returns a message: the query fails, as if the server were down (DEC-34);
2. `Vectors["species|observation key"]` holds a row: that row;
3. `Policy(query)` returns a row: that row;
4. otherwise: a uniform row.

`ScriptedBrain.Prefer(species, "flee", epsilon)` makes a row that prefers one action, and `ScriptedBrain.Token(query,
"Predator")` reads one sense's token, so a policy can react to the situation. The brain counts `Calls` and `Batches`,
keeps every query in `Seen`, and answers `DelayCalls` Advance calls late. `MixesSpecies`, `ActionLimit`, `TokenLimit`
and `TakesAttachments` play the limits of real brains. The system ships no rule-based brain: this one is for tests only.

## Fake mutators and servers

<xref:EvoSim.Testing.FakeMutator> replaces the mutator model. By default it answers each sentence with "quickly" added
before its period; set `Answer = (sentence, seed, prompt) => …` for another rule, `FailWhen` to fail, `DelayCalls` to
answer late. It records `Prompts` and `BatchSizes`. The operators, guards and redraws stay the real ones: an answer
equal to its parent is rejected and drawn again (MUT-13).

<xref:EvoSim.Testing.FakeHttpTransport> plays a server for an HTTP brain or mutator client: set it as their `Transport`.
`Jev()` and `OllamaPoints()` answer like the two reference servers, `Respond` scripts any answer, `FailWith` returns an
HTTP status, `Hang` never answers, `DelayMs` waits. `Requests` and `MaxInFlight` show what was sent. See
[An HTTP brain or mutator server](../extending/http-brains.md).

## Checks

- **Golden files** (<xref:EvoSim.Testing.Golden>). `Golden.Check("situations.tsv", text)` compares a long output with
  a file in `Assets/EvoSim/Tests/Golden`; a missing file is written from this run. See
  [Golden files](../extending/testing-your-module.md#golden-files).
- **Invariants** (<xref:EvoSim.Testing.Invariants>). `Invariants.Check(w)` lists every broken invariant of 30 §5
  (positions, NaN, stat ranges, ids, dead animals still listed, probability rows, cap and floor, carcass portions,
  genomes). `CheckInvariants()` adds an <xref:EvoSim.Testing.InvariantPhase> that fails the test at the first one; call
  it after the other phases.
- **Statistics** (<xref:EvoSim.Testing.Stat>). `Stat.Binomial(successes, trials, p)` and `Stat.Mean(values, expected)`
  pass within 4 standard errors; `Stat.Uniform(counts)` is a χ² test. Draw from a fixed seed, so the test passes or
  fails the same way every run.
- **Static checks** (<xref:EvoSim.Testing.SourceScan>). `SourceScan.Find(SourceScan.Files("Runtime"), pattern)`
  searches the code with comments removed. `Core/StaticChecks.cs` uses it to forbid `UnityEngine.Random` (T-RAND-01),
  `Actions/ActionTests.cs` to forbid actions that read genes (T-ACT-09).

## The probes

The probes (`Assets/EvoSim/Testing/Probes`) stand in for a student's module; the conformance suites leave them out.

| Kind | Probes |
|---|---|
| phases | <xref:EvoSim.Testing.CallbackPhase> (runs a callback between two phases), <xref:EvoSim.Testing.TracePhase>, <xref:EvoSim.Testing.BlackboardPhase>, <xref:EvoSim.Testing.DelayedPhase> (waits like a slow brain), <xref:EvoSim.Testing.RandomEventPhase>, <xref:EvoSim.Testing.SnapshotPhase>, <xref:EvoSim.Testing.ThrowingPhase>, <xref:EvoSim.Testing.AllocationProbe>, <xref:EvoSim.Testing.InvariantPhase> |
| actions | <xref:EvoSim.Testing.GoToAction> (walks or runs to a set point), <xref:EvoSim.Testing.FeedAction>, <xref:EvoSim.Testing.ProbeAction>, <xref:EvoSim.Testing.BatchProbeAction> |
| senses | <xref:EvoSim.Testing.ProbeSense>, <xref:EvoSim.Testing.ObstacleSense>, <xref:EvoSim.Testing.AllLayersObstacleSense>, <xref:EvoSim.Testing.CameraProbeSense>, <xref:EvoSim.Testing.BatchProbeSense> |
| movement and ground | <xref:EvoSim.Testing.TurningLocomotion>, <xref:EvoSim.Testing.SlopeLimitLocomotion>, <xref:EvoSim.Testing.BatchProbeLocomotion>, <xref:EvoSim.Testing.PondGround>, <xref:EvoSim.Testing.RampGround>, <xref:EvoSim.Testing.DoubleDistanceGround>, <xref:EvoSim.Testing.SideCover> |
| other modules | <xref:EvoSim.Testing.SureKillDiet>, <xref:EvoSim.Testing.TraitProbe>, <xref:EvoSim.Testing.HookProbe>, <xref:EvoSim.Testing.WorldHookProbe>, <xref:EvoSim.Testing.NeedsEnergyProbe>, <xref:EvoSim.Testing.CountingCrossover>, <xref:EvoSim.Testing.FruitSystem>, <xref:EvoSim.Testing.MemoryBrain>, <xref:EvoSim.Testing.ShowProbeView> |

## A complete test

Two adults next to each other, a brain that always chooses *mate*, one tick: the babies are born during the tick and
aged by its death phase, so they end it at age 1 (ANIM-35).

```csharp
using System.Linq;
using EvoSim;
using EvoSim.Testing;
using NUnit.Framework;

public class BabyAgeTests : WorldFixture
{
    [Test, Description("ANIM-35: babies born in a tick are aged by that tick's death phase")]
    public void BabiesAgeOnTheirBirthTick()
    {
        var w = New(name: "baby age").Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.LifeRules(40, 0)).ReferencePhases()
            .DefaultBrain<ScriptedBrain>(b => b.Policy = q => ScriptedBrain.Prefer(q.Species, "mate")).Build();
        var prey = w.FindSpecies("prey");
        foreach (var x in new[] { 10f, 10.5f })
        {
            var a = Place.Animal(prey, Place.At(x, 10));
            a.Age = 200; a.SetStat("energy", 90f);
        }
        w.Advance(1);
        var babies = prey.Animals.Where(a => a.Origin == "birth").ToList();
        Assert.IsNotEmpty(babies);
        Assert.IsTrue(babies.All(b => b.Age == 1 && b.BornTick == 0));
    }
}
```

## Go further

- [Testing your module](../extending/testing-your-module.md): the step-by-step for your own module, the conformance
  suites, golden files and the slow `T3` tests.
- [The teaching path](../extending/exercises.md) and [the samples](../extending/samples.md): modules to rewrite and
  complete extensions, each with its tests.
- [Determinism and replay](determinism.md), which every test relies on, and [30 — Tests](../spec/30-tests.md): every
  test id, the tiers and the regression tests.
