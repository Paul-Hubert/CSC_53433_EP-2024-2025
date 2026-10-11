---
uid: extend-samples
---

# The samples

The samples are the recipes of [22 — Extending](../spec/22-extending-recipes.md) written out as working modules: thirst,
water, drinking, seasons, slopes, a mutation operator without a model, a sense that tells a hunter from a neighbour, and
a carcass that can be dragged away. Read them as complete examples of each mechanism, and add them to a scene to try
them. They live in `Assets/EvoSim/Samples`, assembly `EvoSim.Samples`, namespace `EvoSim.Samples`. No reference scene
uses them, so they change nothing until you put one in a world. Most are tested in
`Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs`, and the senses, actions, stat, operator, phase and locomotion
also go through the conformance suite of their kind.

| Sample | Adds | Mechanism | Recipe |
|---|---|---|---|
| <xref:EvoSim.Samples.Thirst> | a `thirst` stat and its prompt rule | a stat module | 22 §1 |
| <xref:EvoSim.Samples.DiesOfThirst> | thirst that grows, and death by thirst | a death rule | 22 §1 |
| <xref:EvoSim.Samples.NearestWaterSense> | "Water: 1-4 meters away." | a banded sense | 22 §2 |
| <xref:EvoSim.Samples.DrinkAction> | an action that walks to the shore and drinks | an action and an interaction | 22 §3 |
| <xref:EvoSim.Samples.IntensityLadder> | *often* becomes *always* or *sometimes* | a mutation operator | 22 §8 |
| <xref:EvoSim.Samples.SeasonsPhase> | winters in which grass regrows slowly | a tick phase | 22 §9 |
| <xref:EvoSim.Samples.ChasedSense> | "Something is chasing me." | a plain sense | 22 §13 |
| <xref:EvoSim.Samples.SlopeLocomotion> | steep slopes refused, climbing paid in energy | a locomotion subclass | 22 §14 |
| <xref:EvoSim.Samples.DragAction> | a carcass dragged into cover | an interaction that moves an entity | scenario S18 |

To add a sample, select the GameObject named below, choose **Add Component** in the Inspector and type the class name.
To change a sample, copy its file into `Assets/Student` and rename the class: the `Student` assembly sees the samples,
so your class can also derive from one.

## Thirst

<xref:EvoSim.Samples.Thirst> declares a stat, `thirst`, on every animal of the species: 0 at birth, kept between 0 and
*Deadly* (100). It adds a rule line to the prompt: *"Thirst grows every step; an animal dies of thirst at 100. Drinking
resets it."* It does not change the stat itself: <xref:EvoSim.Samples.DiesOfThirst> makes it grow and
<xref:EvoSim.Samples.DrinkAction> resets it.

- **Mechanism.** A <xref:EvoSim.SpeciesModule> that declares a stat in
  [Declare](xref:EvoSim.Samples.Thirst.Declare(EvoSim.SpeciesBuilder)) and writes a rule in
  [WritePromptRules](xref:EvoSim.Samples.Thirst.WritePromptRules(EvoSim.PromptWriter)). Other modules read it through
  [Value](xref:EvoSim.Samples.Thirst.Value): `a[thirst.Value]`. See [Stats, costs and death rules](stats-and-death.md).
- **In a scene.** On a child of the species, next to its other stats. Settings: *Per Tick* (0.5), *Deadly* (100).
- To let the brain feel thirst, add a <xref:EvoSim.LevelSense> with *Stat* `thirst`: no code needed.

## DiesOfThirst

<xref:EvoSim.Samples.DiesOfThirst> runs in the death phase, once per animal per tick. It adds *Per Tick* to the
animal's thirst, then returns the cause "thirst" when thirst has reached *Deadly*. At the reference values, an animal
that never drinks dies after 200 ticks. The cause gets its own column in `stats.csv` (`deaths_thirst`, prefixed with the
species id).

- **Mechanism.** A <xref:EvoSim.DeathRule>: [Causes](xref:EvoSim.Samples.DiesOfThirst.Causes) names its causes and
  [CauseOfDeath](xref:EvoSim.Samples.DiesOfThirst.CauseOfDeath(EvoSim.Animal)) returns one, or null to live on.
  `[RequiresModule(typeof(Thirst))]` (<xref:EvoSim.RequiresModuleAttribute>) names the module it needs.
- **In a scene.** On a child of the species (its `Life` object, beside `Starvation`). It needs a `Thirst`.

## NearestWaterSense

<xref:EvoSim.Samples.NearestWaterSense> tells the brain how far the nearest water is: *here* within 0.5 m (V2: *"I am at
the water's edge."*), one token per distance band, or *none*. It asks the ground with
[NearestWater](xref:EvoSim.Ground.NearestWater(UnityEngine.Vector3,System.Single)).

- **Mechanism.** A <xref:EvoSim.BandedSense>: the base class builds the tokens and writes the texts; the sample turns on
  the *here* token ([HasHere](xref:EvoSim.Samples.NearestWaterSense.HasHere)) and maps a distance to a token in
  [Read](xref:EvoSim.Samples.NearestWaterSense.Read(EvoSim.Animal,EvoSim.SenseContext)). See [A sense](sense.md).
- **In a scene.** On a new child of the species' `Senses` object: its place among the senses is its place in the
  situation text. It needs a ground that knows water: the <xref:EvoSim.TerrainGround> (ground below its *Water Level* is
  water). On a flat ground it always says *none*. It multiplies the species' number of possible observations by 6.

## DrinkAction

<xref:EvoSim.Samples.DrinkAction> gives the brain a new choice, *drink*: walk to the shore of the nearest water in sight,
and reset thirst to 0 on arrival. With no water in sight the animal searches (ACT-04).

- **Mechanism.** An <xref:EvoSim.AnimalAction> that only states an intent and an interaction: the act phase moves the
  animal, then applies a small <xref:EvoSim.IInteraction> (reach 0.5 m) that sets thirst to 0. Its
  [IsRelevant](xref:EvoSim.Samples.DrinkAction.IsRelevant(EvoSim.ObservationView)) reads the *Water* sense. See
  [An action or an interaction](action.md).
- **In a scene.** On a new GameObject under the species' `Actions`, named `drink` (the action's name in the prompt and
  its gene's label), with a <xref:EvoSim.TextGene> on the same GameObject. It needs a `Thirst` on the species and a
  ground with water.

## IntensityLadder

<xref:EvoSim.Samples.IntensityLadder> mutates a text gene without a model: it finds the first of *never, rarely,
sometimes, often, always* in the sentence and moves it one step up or down, keeping its capital and punctuation. *"Rest
often when tired."* becomes *"Rest always when tired."* or *"Rest sometimes when tired."*. A sentence without such a
word fails the job, and the gene stays as it was.

- **Mechanism.** A <xref:EvoSim.MutationOperator>:
  [Accepts](xref:EvoSim.Samples.IntensityLadder.Accepts(EvoSim.Gene)) takes text genes, and
  [StartMutation](xref:EvoSim.Samples.IntensityLadder.StartMutation(EvoSim.Gene,EvoSim.Allele,EvoSim.RandomStream))
  draws its one coin from the stream it is given (MUT-30). It sees nothing but the gene (CORE-07). See
  [Mutation](mutation.md).
- **In a scene.** On the species' `Mutation` object, beside the `LlmMutation`. Drag the genes it should handle into its
  *Only These* list. A gene goes to the operator that lists it; the other genes go to the first operator whose list is
  empty (MUT-02).

## SeasonsPhase

<xref:EvoSim.Samples.SeasonsPhase> splits time into years of *Year Length* ticks (2 000). In the second half of each year,
winter, the food layer named in *Layer* ("Grass") regrows at *Winter Regrow Factor* (0.2) times its normal rate.

- **Mechanism.** A <xref:EvoSim.TickPhase>: [Initialize](xref:EvoSim.Samples.SeasonsPhase.Initialize) finds the layer,
  [Run](xref:EvoSim.Samples.SeasonsPhase.Run(EvoSim.TickContext)) sets its regrowth factor every tick, and the
  environment phase, later in the same tick, uses it (TICK-02).
  [Validate](xref:EvoSim.Samples.SeasonsPhase.Validate(EvoSim.ValidationReport)) reports a missing layer (V-20) and a
  wrong place (V-08). See [Phases, services and world hooks](world-modules.md).
- **In a scene.** On a new GameObject under the World's `Phases`, above `Environment`: phases run in hierarchy order.

## ChasedSense

<xref:EvoSim.Samples.ChasedSense> answers *yes* when the nearest threat in sight is doing an action that hunts and its
target is this animal, *no* otherwise. A cannibal, which flees its own kind, can then tell a hungry neighbour from a
peaceful one.

- **Mechanism.** A plain <xref:EvoSim.Sense> with two tokens. It reads the hunter's action through
  [Hunts](xref:EvoSim.AnimalAction.Hunts) and its target through
  [CurrentTarget](xref:EvoSim.SenseContext.CurrentTarget(EvoSim.Animal)), both as they were at the start of the tick
  (SENSE-04), and writes its own V2 sentences.
- **In a scene.** On a new child of the species' `Senses`. It only says *yes* in a species that has threats. It doubles
  the number of possible observations.

## SlopeLocomotion

<xref:EvoSim.Samples.SlopeLocomotion> moves like the kinematic locomotion, but refuses a step onto ground steeper than
*Max Slope Degrees* (35) and charges *Energy Per Meter Climbed* (0.3 points) for every meter of height gained. Going
down costs nothing extra.

- **Mechanism.** A subclass of a reference module (MOVE-07): it overrides
  [Move](xref:EvoSim.Samples.SlopeLocomotion.Move(EvoSim.Animal,EvoSim.Intent,System.Single,EvoSim.MoveContext)) to
  change the intent before calling the base, and
  [ExtraCost](xref:EvoSim.Samples.SlopeLocomotion.ExtraCost(EvoSim.Animal,UnityEngine.Vector3,UnityEngine.Vector3)),
  which the metabolism adds to the cost of the move. See [A locomotion](locomotion.md).
- **In a scene.** It replaces the species' `KinematicLocomotion` on its `Locomotion` object: a species has exactly one
  locomotion (V-13). Copy *Walk Speed*, *Run Speed* and *Prompt Rule* from the old one. It matters only on a ground with
  heights.

## DragAction

<xref:EvoSim.Samples.DragAction> walks to the nearest carcass in sight; within *Grab Reach* (1 m) it walks toward the
nearest cover and drags the carcass along, at walking speed, until both are hidden. It is not a recipe of 22: it writes
out scenario S18 of [31 — Scenarios](../spec/31-scenarios.md), an entity that moves (ENV-21).

- **Mechanism.** An action whose interaction moves an entity: the carcass changes position in the act phase, never from
  the views. Its interaction's reach is unlimited, so it applies wherever the animal ends its move.
- **In a scene.** On a new GameObject `drag` under the actions of a species whose diet eats carcasses (the predator),
  with a <xref:EvoSim.TextGene>. It needs a cover layer, and reads the *Carcass* sense.

## Worked example: thirst on the terrain

The `Terrain_Locomotion` scene has a 96 m terrain whose lowest ground, about 15 %, is water. Four samples together make
its prey thirsty. Do this in a copy of the scene.

1. Open `Assets/EvoSim/Scenes/Terrain_Locomotion.unity` and save it as `Assets/Student/ThirstyTerrain.unity`.
2. Select `Terrain World`. Unless the JEV server runs, set **Default Brain** to `Brains/Random`.
3. Select `Terrain World/Prey/Stats` (it holds `Energy`, `Stamina` and `Metabolism`) and add a **Thirst**. Keep *Per
   Tick* 0.5 and *Deadly* 100.
4. Select `Prey/Life` and add a **Dies Of Thirst**.
5. Under `Prey/Senses`, create an empty child named `Water`, below `Food`, and add a **Nearest Water Sense**.
6. Optionally, create a `Thirst` child below it with a **Level Sense**: *Stat* `thirst`, *Low* 30, *High* 70, and
   *Words* "I am not thirsty.", "I am thirsty.", "I am parched." (low thirst comes first).
7. Under `Prey/Actions`, create a child named `drink` and add a **Drink Action** and a **Text Gene**. In the gene's
   *Founders*, write "Drink whenever you pass water." and "Drink only when very thirsty."; keep *Has Neutral* on.
   *Contrast Pro* and *Contrast Anti* ("Always drink, whatever happens." / "Never drink unless parched.") are optional:
   only brain tests use them.
8. Press **Play**. The validation panel may warn that the prey's signature changed (V-12; *Accept* it) and that its
   observation space is large (V-50). Warnings don't stop the run.
9. Open *Window ▸ EvoSim ▸ Event Log*: deaths with the cause *thirst* appear from about tick 200, for prey that
   found no water. In the run's `stats.csv` the column `prey_deaths_thirst` counts them.
10. To see what drinking is worth, disable the `drink` GameObject and run the same seed again, then compare the deaths by
    thirst.
    Scenario S16 makes the same comparison with a scripted brain.

Then try the others on the same scene: a **Seasons Phase** above `Phases/Environment`, or a **Slope Locomotion** for the
predator only, with a *Max Slope Degrees* below the ground's 35°, so the two species cross the hills differently.

## Test it

The same combination, in a test world with a pond (<xref:EvoSim.Testing.PondGround>): a thirsty prey 2 m from the shore
drinks within three ticks. See [Testing](../guides/testing.md) for the helpers.

```csharp
public class ThirstyPondTests : WorldFixture
{
    [Test, Description("22 §3: drink walks to the shore and resets thirst on arrival")]
    public void DrinkResetsThirst()
    {
        var w = New().Ground<PondGround>(20, 20).Food(0f, 0f).Phase<ActPhase>()
            .Species("prey", s => s.PreyBody().Action<DrinkAction>("drink").Module<Thirst>()).Build();
        var sp = w.FindSpecies("prey");
        var a = Place.Animal(sp, Place.At(10f, 15f));
        var thirst = sp.Module<Thirst>().Value;
        a[thirst] = 50f;
        a.Choose("drink");
        w.Advance(3);
        Assert.AreEqual(0f, a[thirst], "drank");
        Assert.IsFalse(w.Ground.IsWater(a.Position), "on the shore, not in the water");
    }
}
```

## Read next

- [Extending EvoSim](index.md): every mechanism the samples use, and the others.
- [The teaching path](exercises.md): rewrite the reference modules the samples build on.
- [31 — Scenarios](../spec/31-scenarios.md): S16 (water and thirst), S17 (terrain and locomotion), S18 (moving carcasses).
