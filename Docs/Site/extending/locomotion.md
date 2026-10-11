---
uid: extend-locomotion
---

# A locomotion

A locomotion decides how a species moves: an action only says where the animal wants to go (an
<xref:EvoSim.Intent>), and the locomotion turns that into a movement over the ground (MOVE-01). Write one to make
movement depend on the terrain (slopes, mud, water), to follow paths around obstacles, or to charge extra energy
for some moves. To change *where* animals may stand at all, change the [ground](environment.md) instead. How
movement fits in the tick is explained in [Actions and movement](../guides/actions-and-movement.md#moving).

## The base class

Derive from <xref:EvoSim.Locomotion>, or from the reference <xref:EvoSim.KinematicLocomotion> to keep its straight
lines, sliding and side steps and change only part of them.

| Member | You | What it does, when it is called |
|---|---|---|
| [Move](xref:EvoSim.Locomotion.Move(EvoSim.Animal,EvoSim.Intent,System.Single,EvoSim.MoveContext)) | must implement | Moves one animal for this tick, sets its `Position` and returns the meters actually moved (MOVE-06). Called in the act phase, after the action planned, for animals whose intent moves. |
| [ExtraCost](xref:EvoSim.Locomotion.ExtraCost(EvoSim.Animal,UnityEngine.Vector3,UnityEngine.Vector3)) | may override | Extra energy for a move from one point to another (slopes, mud). Called after the animal moved more than 1 mm; the metabolism adds it to the tick's cost. 0 in the reference. |
| [MoveAll](xref:EvoSim.Locomotion.MoveAll(System.Collections.Generic.IReadOnlyList{EvoSim.Animal},System.Collections.Generic.IReadOnlyList{EvoSim.Intent},EvoSim.MoveContext,System.Span{System.Single})) | may override | Moves a group of animals at once (20 §10). The default calls `Move` for each with its stamina budget. |
| `Declare(SpeciesBuilder)` | inherited | Declares the traits `speed.walk` and `speed.run`. If you override it, call `base.Declare(b)`. |
| `WritePromptRules`, `Validate` | inherited | Write the **Prompt Rule** line; check **Wander Turn P** (V-35). |
| [WalkSpeed](xref:EvoSim.Locomotion.WalkSpeed), [RunSpeed](xref:EvoSim.Locomotion.RunSpeed) | read | The speed traits, in meters per tick: read them with `a.Trait(WalkSpeed)`. |
| `walkSpeed`, `runSpeed`, `wanderTurnP`, `promptRule` | protected | The Inspector settings: **Walk Speed**, **Run Speed**, **Wander Turn P**, **Prompt Rule**. |

## What Move receives

**The intent** says what the action wants:

| Member | Meaning |
|---|---|
| `Direction` | a unit vector on the horizontal plane; zero for a stay |
| `Run` | true to run (`RunSpeed`), false to walk (`WalkSpeed`) |
| `MaxDistance` | the farthest to go this tick, in meters, so a stop distance is never overshot; infinity for "as far as you can" |
| `Target` | the point the action is heading to (or running away from) |
| `Wandering` | true for a wandering intent |
| `IsStay`, `Stay()` | whether it moves at all; the same intent turned into a stay, to refuse a move |

**The stamina budget** is the most the animal may move this tick, in meters: its stamina, or infinity for a species
without a stamina module (ANIM-21).

**The move context** (<xref:EvoSim.MoveContext>) gives `Ground` (heights, walkable points, `SlideTo`, `SideStep`,
`SlopeDegrees`), `Distance(a, b)` (the world distance, SPACE-02), `Stream(species, purpose)` (a random stream),
`StaminaBudget(a)`, `World` and `Tick`.

## A complete example

The sample <xref:EvoSim.Samples.SlopeLocomotion> (`Assets/EvoSim/Samples/SlopeLocomotion.cs`, recipe 22 §14) refuses
ground steeper than a limit and charges energy for every meter climbed. It changes the intent, then lets the
kinematic locomotion do the moving.

```csharp
public class SlopeLocomotion : KinematicLocomotion
{
    [SerializeField, Range(0f, 90f), Tooltip("Steeper ground is refused, in degrees (reference 35).")]
    float maxSlopeDegrees = 35f;
    [SerializeField, Min(0f), Tooltip("Energy per meter climbed (reference 0.3).")]
    float energyPerMeterClimbed = 0.3f;

    public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
    {
        if (!intent.IsStay && c.Ground != null)
        {
            Vector3 ahead = a.Position + intent.Direction;
            if (c.Ground.SlopeDegrees(a.Position, ahead) > maxSlopeDegrees) intent = intent.Stay();   // too steep: don't go
        }
        return base.Move(a, intent, staminaBudget, c);
    }

    public override float ExtraCost(Animal a, Vector3 from, Vector3 to) =>
        Mathf.Max(0f, to.y - from.y) * energyPerMeterClimbed;                                     // downhill is free
}
```

The reference <xref:EvoSim.KinematicLocomotion> itself moves by the smallest of speed, stamina budget and
`MaxDistance`; `Ground.SlideTo` slides along borders and obstacles; with no progress, `Ground.SideStep` takes a random
step drawn from `c.Stream(Species, "actions")`.

## Where to put it

1. Write the class in `Assets/Student`. There is no script template for a locomotion: start from the example above,
   or from the stub of exercise 11 (*EvoSim ▸ Exercises ▸ Replace with stub ▸ 11 KinematicLocomotion*).
2. On the species' `Locomotion` child, replace the <xref:EvoSim.KinematicLocomotion> component by yours and set its
   speeds. A species has exactly one locomotion (V-13); with none, validation offers to add a kinematic one.
3. Each species can have its own: give the predator a slope locomotion and leave the prey kinematic, and they move
   differently over the same terrain. Slopes only matter on a ground with heights, such as the terrain ground.

## Rules

1. **Whatever `Move` does is the tick's result** (MOVE-01). Change only the moved animal's `Position`; the act phase
   sets its heading afterwards.
2. **Never end on non-walkable ground or outside the world** (MOVE-02). Use `Ground.SlideTo`, or check
   `Ground.Clear` and `Ground.IsWalkable`. If you don't, the act phase moves the animal back and logs a warning.
3. **Never move more than the stamina budget** (ANIM-21), nor more than `MaxDistance` (MOVE-04).
4. **Read speeds from the animal's traits**, `a.Trait(WalkSpeed)`, never from the settings (MOVE-03, ANIM-16), so a
   number gene can change them.
5. **Return the meters actually moved**, measured with `c.Distance` (MOVE-06). The metabolism and the stamina charge
   exactly that. `ExtraCost` returns 0 or more.
6. **Draw random numbers only from `c.Stream(Species, ...)`** (RAND-03), so a seed replays the run.
7. An override of `MoveAll` must give the same result as calling `Move` for each animal in order.

## Test it

The conformance suite runs every locomotion class in the project: *Window ▸ General ▸ Test Runner*, EditMode,
`ConformanceTests` ▸ `LocomotionConformance`, one case per class (the T-MOVE template). Eight walkers on a 20 × 20 m
ground start on a circle of 8 m around a pond of radius 3 m and walk toward its centre for 30 ticks. The suite checks
that:

- no walker ever ends a tick on water or outside the world (MOVE-02);
- the meters reported are the distance actually moved (MOVE-06);
- no walker stays stuck for more than 3 ticks away from the shore (MOVE-04);
- `ExtraCost` is never negative.

A test to copy: `SlopeLocomotion` in `Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs`, which walks an animal up a
ramp and checks where it stops and what climbing costs. See [Testing your module](testing-your-module.md).

## Read next

- <xref:EvoSim.KinematicLocomotion> (exercise 11 of [the teaching path](exercises.md)) and the sample
  <xref:EvoSim.Samples.SlopeLocomotion>.
- <xref:EvoSim.Ground>: what is walkable, `SlideTo`, `SideStep`, `SlopeDegrees`; see
  [Food, cover, entities and ground](environment.md).
- [Stats, costs and death rules](stats-and-death.md): how the meters you report are charged.
- Specification: 07 §2, in [07 — Actions and locomotion](../spec/07-actions-and-locomotion.md).
