---
uid: extend-exercises
---

# The teaching path

The teaching path is a way in: rewrite a reference module yourself, from an empty class, and watch the world run with
your version. Fourteen reference modules have a stub, from the smallest (stay still to rest) to the largest (a gene
rewritten by a language model), in the order of 22 §0. Nothing grades your version and nothing has to match the
reference: write it your own way, or change what it does. The conformance suite of its kind only checks that it fits
the system. When you are done, invent modules of your own: [Extending EvoSim](index.md).

## Replace with stub, Restore reference

1. Open a scene, for example `Assets/EvoSim/Scenes/Lab1_Small.unity`. Work on a copy (*File ▸ Save As*) if you want
   to keep the reference scene as it is.
2. Choose *EvoSim ▸ Exercises ▸ Replace with stub ▸ 1 RestAction* (or any of the 14).
   - It writes `Assets/Student/Exercises/MyRestAction.cs` from the template `Assets/EvoSim/Exercises/MyRestAction.cs.txt`.
     If the file already exists, it is kept: your work is never overwritten. The first time, the menu also creates the
     `Student` and `Student.Tests` assemblies.
   - Once Unity has compiled the stub, every component of exactly the reference type in the open scenes is replaced by
     the stub: on the same GameObject, at the same place among its components, with the same settings (every field of
     the same name) and the same enabled state. Subclasses of the reference are left alone.
   - The swap can be undone (*Edit ▸ Undo*) and marks the scene as modified: save the scene to keep it.
   - If the stub doesn't compile, the Console says so: fix the errors, then choose the menu item again.
3. Write the methods that throw. Until you do, each one throws a `NotImplementedException` that names it
   (*"Exercise: write MyRestAction.Act your own way…"*), so the run stops when the World first calls it.
4. Press **Play** and watch. Run the conformance suite of the module's kind (see [Tips](#tips)).
5. *EvoSim ▸ Exercises ▸ Restore reference* puts the reference module back wherever an exercise class stands in the open
   scenes, with its settings. Your files stay in `Assets/Student/Exercises`.

## What a stub gives you

```csharp
/// <summary>Exercise 1: stay still to rest (ACT-02, ANIM-21). Reference: <see cref="RestAction"/>.</summary>
[ExerciseOf(typeof(RestAction))]
public class MyRestAction : AnimalAction
{
    /// <summary>The action's line in the prompt (08 §7), used when the Inspector's description is empty.</summary>
    protected override string DefaultDescription => "stay still to catch your breath and save energy";

    public override bool Rests => true;

    /// <summary>… The reference gives the intent of staying, <c>c.Stay()</c>, and nothing else …</summary>
    public override void Act(Animal a, ActContext c) => throw new NotImplementedException(ExerciseOfAttribute.Todo("MyRestAction", "Act"));
}
```

A stub has the reference's base class, its Inspector settings under the same names (so the swap keeps them), and the
small members that are not the exercise (the prompt line, the tokens, `IsRelevant`). The methods to write throw. Each
one's documentation comment says what the reference does, step by step, with the calls it uses. `[ExerciseOf]`
(<xref:EvoSim.ExerciseOfAttribute>) ties the stub to its reference, for the two menu items.

## The fourteen exercises

| # | Stub | Base class | Reference | Write | Look at |
|---|---|---|---|---|---|
| 1 | `MyRestAction` | <xref:EvoSim.AnimalAction> | <xref:EvoSim.RestAction> | `Act` | [Stay](xref:EvoSim.ActContext.Stay); `Rests` makes the metabolism charge the rest cost |
| 2 | `MyLevelSense` | <xref:EvoSim.Sense> | <xref:EvoSim.LevelSense> | `Read`, `Write` | find the stat once in `Initialize` with [FindStat](xref:EvoSim.SpeciesBuilder.FindStat(System.String)); low below 30, high above 70 |
| 3 | `MyStarvation` | <xref:EvoSim.DeathRule> | <xref:EvoSim.Starvation> | `CauseOfDeath` | find the <xref:EvoSim.Energy> module in `Initialize`; energy ≤ 0 gives "starvation" |
| 4 | `MyEatAction` | <xref:EvoSim.AnimalAction> | <xref:EvoSim.EatAction> | `Act` | [NearestResource](xref:EvoSim.ActContext.NearestResource(EvoSim.Animal,System.String)), [GrazeReach](xref:EvoSim.ActContext.GrazeReach), [Interaction.Graze](xref:EvoSim.Interaction.Graze(EvoSim.ResourceItem)); search when nothing is in sight (ACT-04) |
| 5 | `MyNearestAnimalSense` | <xref:EvoSim.BandedSense> | <xref:EvoSim.NearestAnimalSense> | `BuildTokens`, `Read` | the base's `bands`, `Add` and `NoneToken`; [NearestAnimal](xref:EvoSim.SenseContext.NearestAnimal(EvoSim.Animal,EvoSim.AnimalSet,System.Single,System.Collections.Generic.IReadOnlyList{EvoSim.Species})), [IsReady](xref:EvoSim.SenseContext.IsReady(EvoSim.Animal)) |
| 6 | `MyFleeAction` | <xref:EvoSim.AnimalAction> | <xref:EvoSim.FleeAction> | `Act` | [NearestAnimal](xref:EvoSim.ActContext.NearestAnimal(EvoSim.Animal,EvoSim.AnimalSet,System.Collections.Generic.IReadOnlyList{EvoSim.Species})), [RunAwayFrom](xref:EvoSim.ActContext.RunAwayFrom(EvoSim.Animal)); the *Flee Into Cover* option |
| 7 | `MyHideAction` | <xref:EvoSim.AnimalAction> | <xref:EvoSim.HideAction> | `Act` | [InCover](xref:EvoSim.ActContext.InCover(EvoSim.Animal)), [NearestCover](xref:EvoSim.ActContext.NearestCover(EvoSim.Animal,System.Single)); no cover in sight: search (ENV-12) |
| 8 | `MyStamina` | <xref:EvoSim.Stamina> | <xref:EvoSim.Stamina> | `Budget`, `Settle` | the stat `Value`, the traits `Max` and `Regen`, `RegenEnergyCost`, [Energy.Pay](xref:EvoSim.Energy.Pay(EvoSim.Animal,System.Single)) |
| 9 | `MyLitter` | <xref:EvoSim.Litter> | <xref:EvoSim.Litter> | `Size` | `Min`, `Max`, `ChildEnergy`; draw k with `rng.Range(Min, Max + 1)` (REPRO-10) |
| 10 | `MyUniformCrossover` | <xref:EvoSim.Crossover> | <xref:EvoSim.UniformCrossover> | `Cross` | [CheckSpecies](xref:EvoSim.Crossover.CheckSpecies(EvoSim.Genome,EvoSim.Genome)), one `rng.NextBool()` per locus (GENE-30) |
| 11 | `MyKinematicLocomotion` | <xref:EvoSim.Locomotion> | <xref:EvoSim.KinematicLocomotion> | `Move` | the speed traits `WalkSpeed` and `RunSpeed`, [SlideTo](xref:EvoSim.Ground.SlideTo(UnityEngine.Vector3,UnityEngine.Vector3)), [SideStep](xref:EvoSim.Ground.SideStep(UnityEngine.Vector3,System.Single,EvoSim.RandomStream)) (MOVE-02, MOVE-04) |
| 12 | `MyHuntAction` | <xref:EvoSim.AnimalAction> | <xref:EvoSim.HuntAction> | `Act` | [NearestCarcass](xref:EvoSim.ActContext.NearestCarcass(EvoSim.Animal)), [Interaction.Strike](xref:EvoSim.Interaction.Strike(EvoSim.Animal)), [Interaction.Scavenge](xref:EvoSim.Interaction.Scavenge(EvoSim.Carcass)) |
| 13 | `MyRandomBrain` | <xref:EvoSim.Brain> | <xref:EvoSim.RandomBrain> | `Ask` | [Uniform](xref:EvoSim.Brain.Uniform(System.Int32)), [BrainAnswer.Now](xref:EvoSim.BrainAnswer.Now(System.Single[][])) (DEC-11) |
| 14 | `MyLlmMutation` | <xref:EvoSim.MutationOperator> | <xref:EvoSim.LlmMutation> | `StartMutation` | [MutationText.Instructions](xref:EvoSim.MutationText.Instructions(System.String)), [MutatorService.Submit](xref:EvoSim.MutatorService.Submit(System.String,System.Int32[],System.Int64[],System.String,System.Collections.Generic.IReadOnlyList{System.String},System.Int32)); draw every try first (MUT-30) |

## Why MyStamina and MyLitter derive from the reference

Twelve stubs derive from the reference's base class, as a module of your own would. `MyStamina` and `MyLitter` derive
from the reference itself, because other parts of the system look these two modules up by their type: the act phase
and the metabolism ask the species for its <xref:EvoSim.Stamina>, and the breed phase asks for its
<xref:EvoSim.Litter>. A stub of another type would not be found.

Deriving also keeps what the reference declares. `MyStamina` inherits the stamina stat and the `stamina.max` and
`stamina.regen` traits, so the stamina sense and number genes still work; you override only
[Budget](xref:EvoSim.Stamina.Budget(EvoSim.Animal)) and
[Settle](xref:EvoSim.Stamina.Settle(EvoSim.Animal,System.Single,EvoSim.Energy)). `MyLitter` inherits the *Min*,
*Max* and *Child Energy* settings; you override only [Size](xref:EvoSim.Litter.Size(EvoSim.RandomStream,System.Single[])).

## What the stubs leave out

A stub holds what you must write. Some references do a little more, which you may want to add yourself:

| Stub | The reference also | Why it matters |
|---|---|---|
| `MyHuntAction` | overrides [Hunts](xref:EvoSim.AnimalAction.Hunts) to return `true` | senses such as the sample <xref:EvoSim.Samples.ChasedSense> read it to know an animal is hunting |
| `MyLlmMutation` | overrides [UsesMutatorService](xref:EvoSim.MutationOperator.UsesMutatorService) to return `true`, and reads its deck in `Initialize` | it tells the mutator client that an operator sends it work; the Ollama client then fetches the model's digest, recorded with the alleles (GENE-12) |
| `MyNearestAnimalSense` | overrides [Write](xref:EvoSim.Sense.Write(System.Int32,EvoSim.TextStyle)) to add ", ready to mate." or ", not ready to mate.", and names its label after the species when the set holds one | without it the readiness tokens are written like the plain band, and renaming a species doesn't change the label (SPEC-20) |

Several references also set themselves up in `Initialize` (the level sense finds its stat, starvation finds the energy
module, eat, flee and the animal sense resolve species names) and check their settings in `Validate`. Each stub's
class comment says which.

## Tips

- **Read the reference's API page** (the links in the table) and the stub's comments before you write: they say what the
  reference does and when the World calls each method. The [extension page of the kind](index.md) lists the
  rules a module of that kind keeps.
- **Draw from the streams you are given.** `Size`, `Cross` and `StartMutation` receive a <xref:EvoSim.RandomStream>; a
  locomotion asks the move context for one (`c.Stream(Species, "actions")`). Never use `UnityEngine.Random`: a seed must
  replay the run (RAND-01). If your version does what the reference does, with the same draws in the same order, the
  run even gives the same events hash.
- **Run the conformance suite of the kind** in the Test Runner: the stub is in the `Student` assembly, so the suite
  finds it at once, and fails until the methods are written. Crossovers, litters and death rules have no suite of their
  own. See [Testing your module](testing-your-module.md).
- **The reference's unit tests keep testing the reference.** To hold your version to the same checks, copy a reference
  test into `Assets/Student/Tests` and put your class in the builder call (`Action<MyRestAction>("rest")`), with the
  helpers of [Testing](../guides/testing.md).
- **Restore the reference before the regression tests.** They run the reference scenes and compare pinned hashes.

## Read next

- [Extending EvoSim](index.md): the base class for each kind of module, once you invent your own.
- [The samples](samples.md): complete extensions (thirst, water, seasons, slopes) to read and combine.
- [22 — Extending: recipes](../spec/22-extending-recipes.md): the teaching path and the recipes in the specification.
