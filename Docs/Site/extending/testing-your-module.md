---
uid: extend-testing-your-module
---

# Testing your module

Every module you write gets two kinds of test. The **conformance suite** of its kind finds your class on its own and
checks that it fits the system: a sense returns declared tokens, an action only sets intents, a locomotion never ends a
tick on water. Your **unit tests** check that it does what you meant: the drink action resets thirst, your sense says
*near* at 3 m. A green suite means your module fits; only your own tests say it is right.

All tests use the Unity Test Framework (NUnit) and run in *Window ▸ General ▸ Test Runner*, without any language
model: brains and mutators are fakes. Their helpers live in the `EvoSim.Testing` assembly ([Testing](../guides/testing.md)).

## Step by step

1. **Create the class with its test.** In the Project window, *Assets ▸ Create ▸ EvoSim ▸ Script ▸* **Sense**,
   **Action**, **Gene kind**, **Stat**, **Phase**, **Brain** or **Mutation operator** asks for a class name and writes
   two files: `Assets/Student/<Name>.cs` (assembly `Student`) and `Assets/Student/Tests/<Name>Tests.cs` (assembly
   `Student.Tests`, editor only, which sees `EvoSim.Testing`). The test class derives from
   <xref:EvoSim.Testing.WorldFixture> and has one test, `Works`, that builds a small world with your class and runs it.
   For a kind with no template (a locomotion, a resource layer), derive from the base class yourself in `Assets/Student`.
2. **Write the module.** If it needs another module to work, say so with `[RequiresModule(typeof(…))]` (see
   [Species modules](species-modules.md#dependencies)).
3. **Run the conformance suite of its kind.** Open *Window ▸ General ▸ Test Runner*, tab **EditMode**, and expand
   `EvoSim.Tests.EditMode` ▸ `EvoSim.Tests` ▸ `ConformanceTests`. Each suite lists one case per class, named after it.
   Select the cases of your class and choose *Run Selected*. A failure message names the rule broken.
4. **Write your unit tests** in the generated file: they appear under `Student.Tests` in the same tab. Start from the
   example below, or copy a test of a reference module of the same kind (see [Tests to copy](#tests-to-copy)).
5. **Before you share your work,** run every EditMode test (*Run All*), then the PlayMode tab. Before a merge into the
   main branch, also run the slow tests of category `T3` ([below](#the-slow-tests-t3)).

## The conformance suites

`ConformanceTests` (`Assets/EvoSim/Tests/EditMode/Conformance/ConformanceTests.cs`) holds one parameterised test per
kind of module. Each builds a small world, puts your module in it (under the prey species, or under the World for a
phase or a layer), runs it and checks the rules every module of that kind must keep.

| Kind | Base class | The suite checks | Test |
|---|---|---|---|
| sense | <xref:EvoSim.Sense> | tokens are declared and none is empty; `Write` gives the same non-empty text every time, for every token, in both text styles; over 60 ticks every observation holds a declared token (SENSE-01, SENSE-10) | `SenseConformance` |
| action | <xref:EvoSim.AnimalAction> | it has a description for the prompt; a text gene placed beside it binds to it; `Act` changes no animal, it only sets an intent; alone in an empty world it searches or stays, unless it rests (ACT-04, ACT-06) | `ActionConformance` |
| stat | a <xref:EvoSim.SpeciesModule> with a public <xref:EvoSim.StatId> property | founders start at the declared value; the stat stays in its range for 1 000 ticks, with every other invariant (ANIM-10, ANIM-22) | `StatConformance` |
| food or other item layer | <xref:EvoSim.ResourceLayer> | the layer starts with items; the nearest item is found within a radius (for grids, the same one as a brute-force search); an item can be consumed only once (ENV-01, ENV-02) | `LayerConformance` |
| mutation operator | <xref:EvoSim.MutationOperator> | it accepts a text or a number gene; the same parent and seed give the same result; a result passes the gene's own checks (MUT-20, MUT-30) | `MutationOperatorConformance` |
| tick phase | <xref:EvoSim.TickPhase> | it runs 20 ticks in a world without animals; disabling and re-enabling it leaves the events hash of a populated world unchanged (TICK-01) | `PhaseConformance` |
| locomotion | <xref:EvoSim.Locomotion> | eight walkers head into a pond: none ever ends a tick on water or outside the world; the meters reported are the meters moved; the extra cost is never negative; none stands still more than 3 ticks away from the shore (MOVE-02, MOVE-03, SPACE-04, SPACE-05) | `LocomotionConformance` |

### How a class enters a suite

You do nothing: no registration, no class to derive from. Each suite finds its classes by reflection, so a new class
appears in the Test Runner as soon as Unity has compiled it:

- every class deriving from the suite's base class that is **concrete**, **not generic** and has a **parameterless
  constructor** (every MonoBehaviour without a constructor of its own has one);
- in the assemblies `EvoSim.Runtime`, `EvoSim.Http` and `EvoSim.Samples`, in `Student` (everything under
  `Assets/Student`), and in `Assembly-CSharp` (scripts outside any assembly). The test probes of `EvoSim.Testing` and the
  editor code are left out;
- the modules your class names with <xref:EvoSim.RequiresModuleAttribute> are added beside it first: a species module
  on the species under test, a world module under the World. A drink action is tested with its thirst.

## Testing a brain

The brain suite is `DecisionTests.BrainConformance` (`Assets/EvoSim/Tests/EditMode/Decisions/DecisionTests.cs`). It
checks DEC-11 and DEC-12: one valid row per query, of the species' length, in query order, and the same row for the
same query. It only looks at brains of the EvoSim assemblies, so it does not run yours. Copy it into your test file
with your class:

```csharp
[Test, Description("DEC-11, DEC-12: one valid row per query, in order; the same query twice gives the same row")]
public void MyBrainConforms()
{
    var w = New().Ecology(48f).ReferencePhases().DefaultBrain<MyBrain>().Build();
    var rng = new RandomStream(3, "queries");
    var queries = new List<DecisionQuery>();
    for (int i = 0; i < 20; i++)
    {
        var s = w.FindSpecies(i % 2 == 0 ? "prey" : "predator");
        var a = Place.Animal(s, Place.At(rng.Range(0f, 48f), rng.Range(0f, 48f)));
        w.Space.Rebuild();
        var o = s.Observe(a, new SenseContext(w));
        queries.Add(new DecisionQuery(s, new List<KeyValuePair<string, string>>(), o,
                                      s.Describe(o, TextStyle.V1), TextStyle.V1, a.Genome.BrainKey));
    }
    var answer = w.DefaultBrain.Ask(queries); answer.Pending.Wait();
    var again = w.DefaultBrain.Ask(queries); again.Pending.Wait();
    for (int i = 0; i < queries.Count; i++)
    {
        Assert.IsNull(BrainAnswer.Check(answer.Row(i), queries[i].Species.Actions.Count), $"row {i}");
        CollectionAssert.AreEqual(answer.Row(i), again.Row(i), "deterministic");
    }
}
```

It needs `using System.Collections.Generic;` beside the usings of the generated file. A brain that calls a server (an
<xref:EvoSim.HttpBrain>) is tested against a fake server: set its `Transport` to a
<xref:EvoSim.Testing.FakeHttpTransport> before `Build()` (see [An HTTP brain or mutator server](http-brains.md)).

## A complete unit test

This test checks the sample <xref:EvoSim.Samples.DrinkAction>: an animal 2 m from a pond walks to the shore and
drinks. Your own tests have the same shape: build a world in code, place animals, give them an action, advance, assert.

```csharp
using EvoSim;
using EvoSim.Samples;
using EvoSim.Testing;
using NUnit.Framework;

public class DrinkActionTests : WorldFixture
{
    [Test, Description("ACT-10 (22 §3): drink walks to the shore and resets thirst on arrival")]
    public void DrinkResetsThirst()
    {
        var w = New().Ground<PondGround>(20, 20).Food(0f, 0f).Phase<ActPhase>()
            .Species("prey", s => s.PreyBody().Action<DrinkAction>("drink").Module<Thirst>()).Build();
        var sp = w.FindSpecies("prey");
        var a = Place.Animal(sp, Place.At(10f, 15f));          // 2 m from the pond's shore
        var thirst = sp.Module<Thirst>().Value;
        a[thirst] = 50f;
        a.Choose("drink");                                     // no decision phase: it keeps this action
        w.Advance(3);
        Assert.AreEqual(0f, a[thirst], "drank");
        Assert.IsFalse(w.Ground.IsWater(a.Position), "on the shore, not in the water");
    }
}
```

[New](xref:EvoSim.Testing.WorldFixture.New(System.Int32,EvoSim.WaitMode,System.String)) starts a
<xref:EvoSim.Testing.WorldBuilder> with seed 1234, and the fixture destroys the world after the test. The world has only
the phases the test adds: here the act phase, so no brain is asked and the animal keeps the action given by
[Choose](xref:EvoSim.Testing.Lab.Choose(EvoSim.Animal,System.String)).
[Place.Animal](xref:EvoSim.Testing.Place.Animal(EvoSim.Species,UnityEngine.Vector3,EvoSim.Genome,System.Single,System.String,System.Action{EvoSim.Animal}))
puts an animal exactly where the test needs it, and [Advance](xref:EvoSim.World.Advance(System.Int32)) runs ticks. The
`Description` is the test's documentation: say what it checks and name the rules it keeps. The reference set-ups
(`PreyBody`, `Ecology`, `Lab1`, `ReferencePhases`) are in <xref:EvoSim.Testing.Lab>; see [Testing](../guides/testing.md).

## Fakes and helpers

| Helper | Use it to |
|---|---|
| <xref:EvoSim.Testing.ScriptedBrain> | decide without a model. Its [Policy](xref:EvoSim.Testing.ScriptedBrain.Policy) maps a query to a row; `ScriptedBrain.Prefer(species, "flee")` builds a row that prefers one action and `ScriptedBrain.Token(query, "Predator")` reads one sense. It counts `Calls` and `Batches`, keeps every query in `Seen`, can answer `DelayCalls` calls late and fail on demand (`FailWhen`). Without a policy it answers uniformly. |
| <xref:EvoSim.Testing.FakeMutator> | mutate without a model. Add it with [FakeMutation](xref:EvoSim.Testing.Lab.FakeMutation(EvoSim.Testing.WorldBuilder,System.Action{EvoSim.Testing.FakeMutator})). By default it appends *quickly* to the sentence; set `Answer` for another rule. It records every prompt in `Prompts` and can answer late or fail. |
| <xref:EvoSim.Testing.FakeHttpTransport> | play a server for an HTTP brain or mutator client: `FakeHttpTransport.Jev()`, `FakeHttpTransport.OllamaPoints()`, or your own `Respond`. It records every request (`Requests`) and can fail (`FailWith`), hang or answer slowly. |
| <xref:EvoSim.Testing.Stat> | check a statistic over many draws with a fixed seed: `Stat.Binomial(successes, trials, p)` within 4 standard errors, `Stat.Mean`, `Stat.Uniform` (a χ² test). |
| [CheckInvariants](xref:EvoSim.Testing.Lab.CheckInvariants(EvoSim.Testing.WorldBuilder)) | called after the other phases, add a phase that fails the test at the first broken invariant of 30 §5: an animal outside or on water, a NaN, a stat out of range, a duplicate id, a dead animal still listed, a bad probability row… <xref:EvoSim.Testing.Invariants> runs the same checks at any moment. |

Draw every random number in a test from a fixed seed, the world's (`New(seed)`) or a <xref:EvoSim.RandomStream> of
your own (`new RandomStream(7, "my-test")`), so that a failing test fails the same way every time.

## Tests to copy

The tests of the reference modules are the best models. They are in `Assets/EvoSim/Tests/EditMode`:

| To test | Copy from |
|---|---|
| any module, in the simplest form | `Samples/SampleTests.cs`: one short test per sample (thirst, water sense, drink, seasons, slopes) |
| a sense | `Senses/SenseTests.cs` (tokens one by one, distance bands, situation texts) |
| an action or an interaction | `Actions/ActionTests.cs` (intents, reach, contested items) |
| a locomotion | `Actions/MovementTests.cs` (speeds, stop distances, corners, slopes) |
| a stat or a death rule | `Animals/AnimalTests.cs` (the energy table, causes of death) |
| a mutation operator | `Mutation/MutationTests.cs` (rates, same seed same result, clamping) |
| a phase | `Core/TickLoopTests.cs` (order, what later phases see) |
| a brain | `Decisions/DecisionTests.cs`; over HTTP, `Http/HttpBrainTests.cs` |
| a food layer or cover | `Environment/EnvironmentTests.cs` (queries against brute force) |

## The teaching path

*EvoSim ▸ Exercises ▸ Replace with stub ▸ …* writes `Assets/Student/Exercises/My<Reference>.cs` and swaps the reference
module for it in the open scenes ([The teaching path](exercises.md)). The stub is in the `Student` assembly, so the
conformance suite of its kind runs it at once; it fails until you write the methods that throw. The reference
module's own unit tests do **not** grade your stub: they build their worlds with the reference class by type
(`Action<RestAction>("rest")`). To hold your version to the same expectations, copy such a test into your test file
and put your class in the builder call (`Action<MyRestAction>("rest")`).

## Golden files

Some tests compare an output with a file pinned from an accepted run, in `Assets/EvoSim/Tests/Golden`
(<xref:EvoSim.Testing.Golden>):

| File | Pinned by | Holds |
|---|---|---|
| `hashes.json` | `RegressionTests.PinnedHashes` (R-01) | the events hash of 12 runs: two reference scenes × seeds 1234, 7, 42 × the random brain or a scripted prey, 2 000 ticks |
| `prompts.txt` | `PromptTests` (T-PROMPT-03) | the assembled prompts of both reference species |
| `situations.tsv` | `SenseTests` (T-SENSE-08) | the situation texts of 48 situations per species |
| `jev_text.txt` | `HttpBrainTests` | the text the JEV brain sends for the example of 08 §7 |

A missing file is written from the current run; an existing one must match exactly, and the failure shows the first
line that differs. To **re-pin**, delete the file, run its test once, and commit the new file with a message that says
why. Re-pin only when the change was meant: a rule changed, a reference value changed, a prompt text was rewritten. Do
not re-pin after a change that should not alter any run (a refactoring, comments): find what changed the behaviour.
Hashes are compared on one machine only (RAND-12).

Your modules in `Assets/Student` change no golden file unless you put them in a reference scene or change a reference
module: try them in a copy of a scene, and restore any exercise stub (*EvoSim ▸ Exercises ▸ Restore reference*) before
you run the regression tests.

## The slow tests (T3)

Eleven tests carry the NUnit category `T3`: the regression tests of 30 §6 (pinned hashes, equivalences, soak,
performance, the prototype's analysis tools) and the long scenarios (S01, S06, S10, S16, S17, S23). They run whole
scenes for thousands of ticks: minutes each, the soak up to 30 minutes. Run them before a merge, from the Test Runner
with the category filter set to `T3`, or headless:

```
Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunRegression
```

[RunRegression](xref:EvoSim.Batch.RunRegression) writes its totals to `Logs/EvoSim/Reports/regression-<date-time>.txt`
and exits with code 1 when a test failed ([Running and building](../guides/running-and-building.md)).

> [!WARNING]
> Tests that use a reference scene open it beside the current one and close it unsaved
> (<xref:EvoSim.Batch.WithScene(System.String,System.Func{EvoSim.World,System.String})>). If that scene is open in the
> editor, the test fails with *"… is open in the editor: open another scene first"*: do that and run it again.

## Read next

- [Testing](../guides/testing.md): the `EvoSim.Testing` assembly in depth: the builder, the fixture, the test probes.
- [30 — Tests](../spec/30-tests.md): every test id, the tiers, the conformance suites and the regression tests.
- [The teaching path](exercises.md) and [the samples](samples.md): modules to rewrite, and tested extensions to read.
