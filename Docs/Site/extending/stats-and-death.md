---
uid: extend-stats-and-death
---

# Stats, costs and death rules

Four things can be added or changed here: a **stat** every animal of a species carries (thirst, fear, temperature),
**what living costs** per tick, the reference **energy, stamina and digestion** modules, and a **cause of death**.
Stats and traits, energy, stamina, metabolism and death are explained in
[Animals, stats and death](../guides/animals-and-stats.md). If the cost depends on how an animal moves (slopes,
mud), a [locomotion](locomotion.md#a-complete-example)'s `ExtraCost` is the simpler place.

## A stat module

A stat is declared by a <xref:EvoSim.SpeciesModule>, in `Declare`, before any animal exists. Keep the id you get
back: it is how every module reads and writes the stat.

| Call on the <xref:EvoSim.SpeciesBuilder> | Parameters |
|---|---|
| [DeclareStat](xref:EvoSim.SpeciesBuilder.DeclareStat(System.String,EvoSim.StatStart,System.Single,System.Single,EvoSim.TraitId)) | a name; a start, <xref:EvoSim.StatStart.Fixed(System.Single)> or full at a trait's value (<xref:EvoSim.StatStart.FullTrait(EvoSim.TraitId)>); a minimum and a maximum (default: none); optionally a trait that caps it for each animal |
| [DeclareTrait](xref:EvoSim.SpeciesBuilder.DeclareTrait(System.String,System.Single,System.Single,System.Single,System.Boolean)) | a name; its default, your Inspector setting; a minimum and a maximum; whether it may change during life (default: no) |

The sample <xref:EvoSim.Samples.Thirst> (`Assets/EvoSim/Samples/Thirst.cs`, recipe 22 §1) declares a stat and
describes it in the prompt:

```csharp
public class Thirst : SpeciesModule
{
    [SerializeField, Min(0f), Tooltip("Thirst added every tick (reference 0.5).")]
    float perTick = 0.5f;
    [SerializeField, Min(1f), Tooltip("Thirst at which an animal dies (reference 100).")]
    float deadly = 100f;

    public StatId Value { get; private set; }
    public float PerTick => perTick;
    public float Deadly => deadly;

    public override void Declare(SpeciesBuilder b) =>
        Value = b.DeclareStat("thirst", StatStart.Fixed(0f), 0f, deadly);

    public override void WritePromptRules(PromptWriter w) =>
        w.Rule($"Thirst grows every step; an animal dies of thirst at {Bands.Number(deadly)}. Drinking resets it.");
}
```

Other modules find it with `Species.Module<Thirst>()` in their `Initialize`, then read and write `a[thirst.Value]`.
Every write is clamped to the stat's range (ANIM-22). A trait is read with `a.Trait(id)`. To let the brain sense the
stat, add a <xref:EvoSim.LevelSense> with the stat `thirst`: no code ([A sense](sense.md)).

*Assets ▸ Create ▸ EvoSim ▸ Script ▸ Stat* writes such a module, with a start and a maximum, into `Assets/Student`.
Put the component on a child GameObject of the species, next to its other stats (`Stats` in the reference scenes).

## Changing what a tick costs

<xref:EvoSim.Metabolism> charges each living animal once per tick, after it acted (ANIM-20). Its settings cover most
changes:

| Field | Reference | Meaning |
|---|---|---|
| Base Cost | 0.7 | energy per tick when moving, or when still with an action that doesn't rest |
| Move Cost | 0.5 | energy per meter moved |
| Rest Cost | 0.2 | energy per tick when still and busy, or still with a resting action |
| Cost Trait | empty | a trait the tick's cost grows with, e.g. `stamina.max` (09 §5) |
| Cost Per Unit | 0.005 | extra energy per tick per unit of that trait: 60 stamina costs 0.3 a tick |

A cost trait gives a number gene a price, so evolution can't push the trait up for free. Validation reports a cost
trait that no module declares (V-45), and reminds you when a number gene's trait has no cost (V-47).

For a rule the settings can't express, derive from <xref:EvoSim.Metabolism> and override
[Settle](xref:EvoSim.Metabolism.Settle(EvoSim.Animal,System.Single,System.Single,System.Boolean)). It receives the
animal, the meters it moved, the locomotion's extra cost, and whether it was busy at the start of the tick. The
reference does this:

```csharp
public virtual void Settle(Animal a, float metersMoved, float extraCost, bool wasBusy)
{
    if (energy != null)
    {
        float cost;
        if (metersMoved > Units.Epsilon) cost = baseCost + moveCost * metersMoved;    // less than 1 mm counts as still
        else if (wasBusy) cost = restCost;
        else cost = a.CurrentAction != null && a.CurrentAction.Rests ? restCost : baseCost;
        if (costId.IsValid) cost += costPerUnit * a.Trait(costId);                    // the cost trait (09 §5)
        energy.Pay(a, cost + Mathf.Max(0f, extraCost));
    }
    stamina?.Settle(a, metersMoved, energy);                                         // pays the meters or recovers
}
```

An override must still pay through `Energy.Pay` and let the stamina settle, or stamina never recovers.

## Replacing energy, stamina or digestion

Other modules find these by type: `Species.Module<Energy>()`, `Module<Stamina>()`, `Module<Digestion>()`,
`Module<Metabolism>()`. That call also returns a subclass, so **a replacement must derive from the reference** and
take its place (one of each per species). Override the virtual members:

| Module | Virtual members | Called by |
|---|---|---|
| <xref:EvoSim.Energy> | `Gain(a, amount)`, `Pay(a, amount)` | every meal; the metabolism, stamina recovery, litters |
| <xref:EvoSim.Stamina> | `Budget(a)`, `Settle(a, metersMoved, energy)` | the act phase, before moving; the metabolism, after |
| <xref:EvoSim.Digestion> | `AfterMeal(a, gain)` | the interactions, after a kill or a carcass portion |
| <xref:EvoSim.Metabolism> | `Settle(a, metersMoved, extraCost, wasBusy)` | the act phase, once per animal per tick |

The stub of exercise 8, *EvoSim ▸ Exercises ▸ Replace with stub ▸ 8 Stamina*, is such a subclass: it keeps the
reference's declarations and asks you to write `Budget` and `Settle`.

## A death rule

Derive from <xref:EvoSim.DeathRule> to add a cause of death.

| Member | You | What it does, when it is called |
|---|---|---|
| [CauseOfDeath](xref:EvoSim.DeathRule.CauseOfDeath(EvoSim.Animal)) | must implement | Returns the cause's name for this animal now, or `null` to live on. Called in the death phase, once per tick for every living animal, after it aged. |
| [Causes](xref:EvoSim.DeathRule.Causes) | should override | Lists every cause the rule can return, so its `<species>_deaths_<cause>` column is in `stats.csv` from the first row. |
| [CheckAll](xref:EvoSim.DeathRule.CheckAll(System.Collections.Generic.IReadOnlyList{EvoSim.Animal},System.String[])) | may override | Checks all living animals at once (20 §10). The default calls `CauseOfDeath` for each animal that has no cause yet. |

The sample <xref:EvoSim.Samples.DiesOfThirst> (`Assets/EvoSim/Samples/DiesOfThirst.cs`) makes thirst grow with the
animal and kills at the deadly level:

```csharp
[RequiresModule(typeof(Thirst))]
public class DiesOfThirst : DeathRule
{
    static readonly IReadOnlyList<string> causes = System.Array.AsReadOnly(new[] { "thirst" });
    Thirst thirst;

    public override IEnumerable<string> Causes => causes;

    public override void Initialize() => thirst = Species.Module<Thirst>();

    public override string CauseOfDeath(Animal a)
    {
        if (thirst == null) return null;
        a[thirst.Value] += thirst.PerTick;                       // ages with the animal, in the death phase
        return a[thirst.Value] >= thirst.Deadly ? "thirst" : null;
    }
}
```

Place it under the species, next to the other death rules (`Life` in the reference scenes). The species' rules run in
module order, and the first one that names a cause decides: later rules are not asked about that animal. The death
phase records the death (cause, age, generation, meals, offspring) and removes the animal; the rule only names the
cause. To remove animals at another moment, call `World.RecordDeath` from a phase of your own: see
[Phases, services and world hooks](world-modules.md).

## Rules

1. **Settings in the module, state in the animal** (ANIM-01). One module serves every animal of its species: what
   belongs to one animal goes in a stat it declared, never in a field of the module.
2. **Declare in `Declare`, look up in `Initialize`.** Read and write only through the ids (ANIM-10).
3. **Read traits, not settings** (ANIM-16): a number gene may have changed the animal's value.
4. **Stats stay in range** (ANIM-22): writes are clamped, so a gain never passes the maximum.
5. **A death rule only names causes.** It may update a stat that grows with age, as thirst does, but never removes
   an animal itself.
6. **Draw random numbers only from the species' streams** (RAND-03), `World.Random.For(Species, purpose)`.

## Test it

The stat suite runs every species module with a public <xref:EvoSim.StatId> property: *Window ▸ General ▸ Test
Runner*, EditMode, `ConformanceTests` ▸ `StatConformance` (the T-ANIM stat template). It puts the module under the
prey of a small world and checks that every founder starts as declared, and that the stat stays in its range for
1 000 ticks (ANIM-10, ANIM-22).

There is no suite for death rules. A test to copy, from `Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs` (its
class derives from <xref:EvoSim.Testing.WorldFixture>):

```csharp
[Test]
public void ThirstKills()
{
    var w = New().Flat(20, 20).Food(0f, 0f).Phase<DeathPhase>()
        .Species("prey", s => s.PreyBody().Action<RestAction>("rest").Module<Thirst>(t => t.Configure(10f, 30f)).Module<DiesOfThirst>())
        .Build();
    var sp = w.FindSpecies("prey");
    var a = Place.Animal(sp, Place.At(5, 5));
    a.Choose("rest");
    w.Advance(3);
    Assert.IsTrue(a.IsGone, "dies at 30");
    Assert.AreEqual(1, sp.Counters.Deaths["thirst"]);
}
```

See [Testing your module](testing-your-module.md).

## Read next

- The reference modules: <xref:EvoSim.Energy>, <xref:EvoSim.Stamina>, <xref:EvoSim.Metabolism>,
  <xref:EvoSim.Digestion>, <xref:EvoSim.Starvation> (exercise 3) and <xref:EvoSim.OldAge>.
- The samples <xref:EvoSim.Samples.Thirst>, <xref:EvoSim.Samples.DiesOfThirst> and the action that resets thirst,
  <xref:EvoSim.Samples.DrinkAction>: [An action or an interaction](action.md), [the samples](samples.md).
- [Genes](genes.md): number genes that set your traits.
- Specification: [05 — Animals, stats and life cycle](../spec/05-animals-stats-and-life-cycle.md).
