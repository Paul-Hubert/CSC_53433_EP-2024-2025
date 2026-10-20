---
uid: extend-action
---

# An action or an interaction

An action is a behaviour the brain can choose: drink, dig, call for help. Write one when animals need to *do*
something new. If the brain should only *know* something new, write a [sense](sense.md) instead. If the new thing
happens on contact (drink at the shore, drag a carcass), the action asks for an **interaction**, a small class that
runs once the animal is within reach. How actions run is explained in [Actions and movement](../guides/actions-and-movement.md).

## The base class

Derive from <xref:EvoSim.AnimalAction>, itself a [species module](species-modules.md).

| Member | You | What it does, when it is called |
|---|---|---|
| [Act](xref:EvoSim.AnimalAction.Act(EvoSim.Animal,EvoSim.ActContext)) | must implement | Plans one tick for an animal that chose this action, through the context. Called by the act phase every tick until the animal's next decision. |
| [DefaultDescription](xref:EvoSim.AnimalAction.DefaultDescription) | should override | The action's line in the prompt, used when the Inspector's **Description** is empty. |
| `Initialize()` | may override | Find the modules you need once, after every module has declared its stats. |
| `Declare(SpeciesBuilder)`, `Validate(ValidationReport)` | may override | Declare stats or traits; report configuration mistakes before Play. |
| [Rests](xref:EvoSim.AnimalAction.Rests) | may override | True for a resting action: a still tick costs the rest cost, not the base cost. |
| [Mates](xref:EvoSim.AnimalAction.Mates) | may override | True for an action that seeks a partner: the breed phase breeds animals that chose it. |
| [Hunts](xref:EvoSim.AnimalAction.Hunts) | may override | True for an action that chases and strikes; senses such as the sample <xref:EvoSim.Samples.ChasedSense> read it. |
| [Overlay](xref:EvoSim.AnimalAction.Overlay), [OverlayTicks](xref:EvoSim.AnimalAction.OverlayTicks) | may override | True for an action done in passing, beside a second action drawn from the rest of the row; for how many ticks. See [Actions done in passing](#actions-done-in-passing). |
| [IsRelevant](xref:EvoSim.AnimalAction.IsRelevant(EvoSim.ObservationView)) | may override | Declares when a situation is relevant to this action, for the specification's directed tests (CTRL-10). The simulation never calls it. |
| [ActAll](xref:EvoSim.AnimalAction.ActAll(System.Collections.Generic.IReadOnlyList{EvoSim.Animal},EvoSim.ActContext,System.Span{EvoSim.ActPlan})) | may override | Plans a group of animals at once (20 §10). It must give exactly the plans `Act` gives. |
| `Name`, `Description`, `Gene`, `Index` | read | The GameObject's name; the prompt line; the bound text gene; the place in the action order. |

The base class already writes the action's line in the prompt, `- name: description` (PROMPT-01).

## A complete example

The sample <xref:EvoSim.Samples.DrinkAction> (`Assets/EvoSim/Samples/DrinkAction.cs`, recipe 22 §3) walks to the
nearest visible water and resets the animal's thirst there. Thirst is a stat declared by the sample
<xref:EvoSim.Samples.Thirst> module. Its relevance test and its validation are left out here.

```csharp
[RequiresModule(typeof(Thirst))]
public class DrinkAction : AnimalAction
{
    Thirst thirst;   // the species' thirst module, found in Initialize

    protected override string DefaultDescription => "go to the nearest visible water and drink";

    public override void Initialize() => thirst = Species.Module<Thirst>();

    public override void Act(Animal a, ActContext c)
    {
        var water = c.Ground != null ? c.Ground.NearestWater(a.Position, c.Vision(a)) : null;
        if (water == null) { c.Search(); return; }               // nothing in sight: search (ACT-04)
        c.WalkTo(water.Value.Shore, 0f);
        c.OnArrival(new Drink(thirst));                          // applied after moving, within reach
    }

    sealed class Drink : IInteraction
    {
        readonly Thirst thirst;   // the module whose stat is reset
        public Drink(Thirst t) { thirst = t; }
        public float Reach => 0.5f;
        public void Apply(Animal a, InteractionContext c) { if (thirst != null) a[thirst.Value] = 0f; }
    }
}
```

`[RequiresModule(typeof(Thirst))]` makes validation warn when the species has no thirst module (V-26), and makes the
conformance suite add one when it tests the action.

## Where to put it

1. *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Action* asks for a class name and writes `Assets/Student/<Name>.cs`, an
   action that only searches, with a test in `Assets/Student/Tests/<Name>Tests.cs`.
2. In the scene, add a child GameObject under the species' `Actions` object and name it after the action, for example
   `drink`. That name is the action's name in the prompt; it must be unique in the species (V-03).
3. Add your component to it, and a <xref:EvoSim.TextGene> on the same GameObject with its founder sentences
   (*"Drink whenever you pass water."*). Without a gene the brain can't be steered for the action (V-05, with a fix).
   See [Genes](genes.md).
4. The actions' depth-first order is their order in the prompt and in the brain's answer (SPEC-03). Adding an action
   changes the species' signature; accept it in the validation panel (V-12).

The prompt then lists `- drink: go to the nearest visible water and drink`. Set the **Description** field to change
that line for one scene.

## The act context

`Act` receives an <xref:EvoSim.ActContext>. The context is reset to *stay, no interaction* before each animal, and the
last intent and the last `OnArrival` called are the plan.

**Queries.** Each returns only what the animal's senses could report (ACT-05), or `null` when nothing is in sight.

| Method | Returns |
|---|---|
| `Vision(a)` | the animal's vision radius, in meters (20 in the reference) |
| `NearestAnimal(a, set, listed)` | the nearest visible animal of a set (<xref:EvoSim.AnimalSet>: threats, prey, kin, listed), with its distance |
| `NearestReadyPartner(a)` | the nearest kin in sight that is ready to mate |
| `NearestResource(a, layerName)` | the nearest food item its diet grazes, on one layer or all of them |
| `NearestCarcass(a)` | the nearest carcass it may eat from |
| `NearestEgg(a, of)` | the nearest egg its diet eats, of one species or any |
| `NearestEntity<T>(a, filter)` | the nearest entity of a kind of your own; no diet check |
| `NearestCover(a, radius)` | the nearest point in cover; distance 0 when already in cover |
| `InCover(a)`, `IsReady(other)` | whether the animal stands in cover; whether another one is ready to mate |

`World`, `Ground` and `Tick` give the rest, for example `c.Ground.NearestWater(...)`.

**Intents.** Say where to go this tick (ACT-03).

| Method | Intent |
|---|---|
| `WalkTo(point, stopAt)`, `RunTo(point, stopAt)` | go toward a point and stop at a distance from it, never past it (MOVE-04) |
| `WalkTo(animal, stopAt)`, `RunTo(animal, stopAt)` | the same toward an animal, recorded as the animal's target |
| `RunAwayFrom(animal)`, `RunAwayFrom(point)`, `WalkAwayFrom(point)` | go straight away, as far as speed and stamina allow |
| `Stay()` | stay where it is |
| `Wander()` | keep the heading, sometimes turn by ±45° or ±90°; walks (MOVE-05) |
| `Search()` | wander, and count the choice as searching once per decision (ACT-04) |

**Arrival.** `OnArrival(Interaction.Graze(item))` asks for a reference interaction; the other builders of
<xref:EvoSim.Interaction> are `Strike(animal)`, `Scavenge(carcass)` and `EatEgg(egg)`. `OnArrival(myInteraction)` asks
for one of your own.

## Interactions

An interaction is the effect of contact. It is the only place where an action changes an animal (ACT-06). Implement
<xref:EvoSim.IInteraction>:

| Member | What it does, when it is called |
|---|---|
| [Reach](xref:EvoSim.IInteraction.Reach) | How close to the point its intent was heading to, in meters, the animal must end its move. For a `Stay`, that point is the animal's own position. |
| [Apply](xref:EvoSim.IInteraction.Apply(EvoSim.Animal,EvoSim.InteractionContext)) | Called once, in the same tick, right after the animal moved, if it is within reach and was not killed earlier in the tick. |

`Apply` receives an <xref:EvoSim.InteractionContext>: `World`, `Ground`, `Tick`,
[Feed](xref:EvoSim.InteractionContext.Feed(EvoSim.Animal,System.Single,System.Boolean)) and
[Stream](xref:EvoSim.InteractionContext.Stream(EvoSim.Species,System.String)). `Feed(a, energy, digest)` feeds an
animal like a meal: energy gained up to the maximum, one meal counted, and, if `digest` is true, digestion afterwards
for a species with a <xref:EvoSim.Digestion> module. It checks no diet, so check the eater's <xref:EvoSim.Diet> yourself when your food should depend on it.

A complete action with an interaction that feeds: the test probe <xref:EvoSim.Testing.FeedAction>
(`Assets/EvoSim/Testing/Probes/FeedAction.cs`) stays where it is and eats a snack every tick.

```csharp
public class FeedAction : AnimalAction
{
    public float Energy = 5f;

    protected override string DefaultDescription => "stay and eat a snack (a test probe)";

    public override void Act(Animal a, ActContext c)
    {
        c.Stay();
        c.OnArrival(new Snack(Energy));          // reach is measured from where it stays: always in reach
    }

    sealed class Snack : IInteraction
    {
        readonly float energy;   // the snack's energy, in points
        public Snack(float e) { energy = e; }
        public float Reach => 0.5f;
        public void Apply(Animal actor, InteractionContext c) => c.Feed(actor, energy);
    }
}
```

`Reach` may be `float.PositiveInfinity`: the sample <xref:EvoSim.Samples.DragAction> uses it to move a carcass along
with the animal, wherever it ends up.

## Actions done in passing

An action normally runs every tick until the next decision (ACT-02), and decides the intent: where the animal goes.
That doesn't suit something done in a moment, such as a call, a scent mark or a look around: an animal that stood
still to call would stand still for a whole decision period, next to the predator it warns about. Such an action is an
**overlay** (ACT-07): override `Overlay` to return true.

- When the brain's draw picks an overlay, the engine draws a second action, the **carrier**, from the same row with
  every overlay set to 0 (DEC-20). That is what the animal would have done had it not drawn the overlay: flee, hide,
  eat. A row with nothing else leaves no carrier, and the animal stays.
- Each tick the carrier plans as usual. Then the overlay's `Act` runs in the same context: its intent is ignored, its
  interaction applies after the carrier's, measured from the point its own intent names (none: where the animal stood).
  Use `float.PositiveInfinity` as the reach for an effect that happens wherever the carrier took the animal.
- It acts for `OverlayTicks` ticks (1 by default: once, on the tick of the decision; 0: until the next decision), and
  ends early when the animal becomes busy or dies. The brain is asked nothing more; a species without overlays draws
  exactly as before.
- The prompt keeps the action's line, and the engine adds one rule: *Some actions are done in passing (mark): the
  animal also does its next most likely choice.*
- A species needs at least one action that is not an overlay (V-19), and an overlay may not mate or hunt (V-27).
- The inspector shows both, "flee + mark"; the decision counts count the overlay and its carrier.

```csharp
public class MarkAction : AnimalAction
{
    public override bool Overlay => true;                    // done in passing (ACT-07)

    protected override string DefaultDescription => "leave a scent mark";

    public override void Act(Animal a, ActContext c) => c.OnArrival(new Mark());   // no intent: the carrier moves it

    sealed class Mark : IInteraction
    {
        public float Reach => float.PositiveInfinity;        // wherever the carrier took the animal
        public void Apply(Animal actor, InteractionContext c) =>
            actor.Species.Module<Energy>()?.Pay(actor, 0.2f);   // a mark costs a little energy
    }
}
```

The worked example [Alarm calls](worked-example-alarm-calls.md) builds a complete overlay, with a world service that
keeps the calls and a sense that hears them.

## Rules

1. **Search when there is nothing to act on** (ACT-04). Call `c.Search()`, not `c.Wander()`, so the choice is counted.
2. **Target only what the context returns** (ACT-05): within vision, not hidden, not killed, not itself.
3. **Never change an animal in `Act`** (ACT-06): no positions, stats or flags, yours included. Effects go through
   interactions, and the act phase moves the animal.
4. **Never read gene sentences or decision records** (ACT-20). Genes reach an action only through traits.
5. **Draw random numbers only from the species' streams** (RAND-03): `World.Random.For(Species, "actions")` in `Act`,
   `c.Stream(actor.Species, "actions")` in `Apply`. Never `UnityEngine.Random`.
6. **Keep settings in the module, state in the animal.** One action object serves every animal of its species; a
   per-animal memory goes in a stat you declare.
7. **Set up in `Initialize`**, not in `Awake` or `Start`.

## Test it

The conformance suite runs every action class in the project: *Window ▸ General ▸ Test Runner*, EditMode,
`ConformanceTests` ▸ `ActionConformance`, one case per class (the T-ACT template). It puts your action, with a text
gene and the modules it requires, under the prey of a small world and checks that:

- the action has a description and a bound gene;
- calling `Act` on every prey changes no animal anywhere (ACT-06);
- alone in an empty world, the action searches or stays (ACT-04); resting actions are exempt; an overlay plans no
  move and must not search (ACT-07), and gets a rest action beside it as its carrier.

A test to copy, from `Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs` (its class derives from
<xref:EvoSim.Testing.WorldFixture>, which provides `New()`):

```csharp
[Test]
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
}
```

See [Testing your module](testing-your-module.md) for the world builder and the other suites.

## Read next

- The reference actions, smallest first: <xref:EvoSim.RestAction>, <xref:EvoSim.FollowAction>,
  <xref:EvoSim.EatAction>, <xref:EvoSim.FleeAction>, <xref:EvoSim.HideAction>, <xref:EvoSim.HuntAction>,
  <xref:EvoSim.MateAction>. Rewrite one from its stub: [the teaching path](exercises.md).
- The samples <xref:EvoSim.Samples.DrinkAction> and <xref:EvoSim.Samples.DragAction>: [the samples](samples.md).
- [A locomotion](locomotion.md), to change how animals move; [Genes](genes.md), for the gene on your action.
