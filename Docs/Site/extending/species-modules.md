---
uid: extend-species-modules
---

# Species modules, the common base

Everything under a species is a <xref:EvoSim.SpeciesModule>: senses, actions, genes, stats, death rules, the diet, the
locomotion, the mating rule. The specialised kinds have their own pages; this one covers what they all share: the
lifecycle the World runs them through, declaring stats, depending on another module, and validation. Derive from
`SpeciesModule` directly for a feature of one species that is none of those kinds: a stat, a setting other modules
read, a rule line in the prompt. For something shared by every species, write a world module instead
([Phases, services and world hooks](world-modules.md)).

## The lifecycle

| Member | You | When the World calls it |
|---|---|---|
| [Species](xref:EvoSim.SpeciesModule.Species), [World](xref:EvoSim.SpeciesModule.World) | read | set before `Declare`: the owning species and its World |
| [Declare(b)](xref:EvoSim.SpeciesModule.Declare(EvoSim.SpeciesBuilder)) | may override | once per set-up, before any module of the species initialises: declare your stats and traits, keep their ids |
| [Initialize()](xref:EvoSim.SpeciesModule.Initialize) | may override | after every module of the species declared: look up other modules, services, traits; reset your own fields |
| [WritePromptRules(w)](xref:EvoSim.SpeciesModule.WritePromptRules(EvoSim.PromptWriter)) | may override | while the species' prompt template is built, in module order: add rule lines with `w.Rule(…)` |
| [Validate(report)](xref:EvoSim.SpeciesModule.Validate(EvoSim.ValidationReport)) | may override | after every `Initialize`, in the editor and at the start of a run: report configuration problems |
| [OnBorn(a)](xref:EvoSim.SpeciesModule.OnBorn(EvoSim.Animal)) | may override | each time an animal of this species joins the living (founder, baby, newcomer), its stats set |
| [OnDied(a, cause)](xref:EvoSim.SpeciesModule.OnDied(EvoSim.Animal,System.String)) | may override | each death of an animal of this species, once recorded; the animal is marked gone |

`Declare`, `Initialize`, `WritePromptRules` and `Validate` also run in Edit mode, when the editor validates the scene.
The whole set-up order is in [The world and its modules](../guides/world-and-modules.md#initialisation-step-by-step).

## A complete example: a stat

Recipe 22 §1, the sample `Assets/EvoSim/Samples/Thirst.cs` (<xref:EvoSim.Samples.Thirst>): every animal gets a thirst
stat, and the prompt explains it.

```csharp
/// <summary>A thirst stat that grows every tick (with DiesOfThirst) and kills at a threshold.</summary>
public class Thirst : SpeciesModule
{
    [SerializeField, Min(0f), Tooltip("Thirst added every tick (reference 0.5).")] float perTick = 0.5f;
    [SerializeField, Min(1f), Tooltip("Thirst at which an animal dies (reference 100).")] float deadly = 100f;

    public StatId Value { get; private set; }
    public float PerTick => perTick;
    public float Deadly => deadly;

    public override void Declare(SpeciesBuilder b) =>
        Value = b.DeclareStat("thirst", StatStart.Fixed(0f), 0f, deadly);

    public override void WritePromptRules(PromptWriter w) =>
        w.Rule($"Thirst grows every step; an animal dies of thirst at {Bands.Number(deadly)}. Drinking resets it.");
}
```

The module holds settings only. The value belongs to each animal: other modules read and write it with `a[thirst.Value]`,
clamped to its range. Here the sample death rule <xref:EvoSim.Samples.DiesOfThirst> raises it every tick, and
<xref:EvoSim.Samples.DrinkAction> resets it.

**Where it goes.** Put your class in `Assets/Student`: *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Stat* writes a stat module
there, with a test in `Assets/Student/Tests`. Add the component anywhere under the species, for example on its `Stats`
object beside <xref:EvoSim.Energy> and <xref:EvoSim.Stamina>. (The examples on this page leave out the usual
`using EvoSim;` and `using UnityEngine;` lines.)

## Stats, traits and their ids

A **stat** changes during life (energy, stamina, thirst); a **trait** is fixed at birth (vision, `stamina.max`) and a
number gene may set it. Declare both in `Declare` with the <xref:EvoSim.SpeciesBuilder>:

| Call | Returns |
|---|---|
| `b.DeclareStat(name, start, min, max, capTrait)` | a <xref:EvoSim.StatId>; `start` is `StatStart.Fixed(v)` or `StatStart.FullTrait(trait)` |
| `b.DeclareTrait(name, default, min, max, changeable)` | a <xref:EvoSim.TraitId> |
| `Species.Declarations.FindStat(name)`, `FindTrait(name)` | the id another module declared, for `Initialize` |

Two modules that declare the same name share one value; the first declaration counts (V-38 if they differ). Read and
write with `a[statId]` and `a.Trait(traitId)`.

> [!WARNING]
> An id is only good once resolved. `FindStat` and `FindTrait` return an invalid id when no module declared the name.
> Resolve every id in `Declare` or `Initialize`, check `IsValid`, and report a missing one in `Validate`. Never read or
> write an animal with an id you have not checked.

## Rules your module keeps

- **Settings in the module, state in the animal** (CORE-05). One module serves every animal of its species: anything
  that belongs to one animal goes in a stat or a trait. A static check enforces it for the reference modules.
- **Set up in `Initialize`**, never in `Awake`, `Start` or `OnEnable` (ARCH-04), and reset your fields there: the same
  object starts every run.
- **Change nothing in the scene** in `Declare`, `Initialize` or `Validate`: they also run in Edit mode.
- **Random numbers only from a named stream**, such as `World.Random.For(Species, "purpose")` (RAND-01, RAND-03).
- **Keep rule lines stable.** They are part of the prompt, so changing one changes the prompt's id and the answers the
  cache can reuse.

## Species

A new species is configuration, not code. Duplicate the prey in the Hierarchy and change, on its <xref:EvoSim.Species>:

| Field | What it is |
|---|---|
| **Id** | the stable id of files, streams, locus ids and statistics columns (`deer`); empty = the display name; no `.`, not `world` |
| **Display Name** | what the brain reads in prompts and situations |
| **Brain** | its own brain; empty = the World's **Default Brain** |
| **Initial Population** | founders at tick 0 |
| **Prompt Header**, **Genes Intro**, **Frozen Prompt** | the fixed parts of its prompt |

Ids and display names must be unique (V-07). Then change its modules and founder sentences, and give a hunter a diet
entry for it (`deer`): the food web and the threats update by themselves. *GameObject ▸ EvoSim ▸ New Species* starts a
species from scratch, and *Add Action* and *Add Sense* fill it. Adding, removing or moving an action, gene or sense
changes the species' signature (SPEC-03). Once a signature has been accepted, validation warns when it changes (V-12),
until you accept the new one.

**Subclass `Species` only for its two hooks**, `OnBorn` and `OnDied`. They run before the modules' hooks. They are
`protected internal`, so from your own assembly the override is `protected`:

```csharp
public class LoggedSpecies : Species
{
    protected override void OnBorn(Animal a) => Debug.Log($"{a} joins ({a.Origin})");
    protected override void OnDied(Animal a, string cause) => Debug.Log($"{a} died: {cause}");
}
```

**At run time**, [World.AddSpecies](xref:EvoSim.World.AddSpecies(EvoSim.Species,System.String,EvoSim.Species,System.Int32))
clones a species under the World with a new id (SPEC-30): `World.AddSpecies(prey, "deer", parent: prey, founders: 6)`.
The clone gets its own population, streams and allele ids, and inherits the parent's relations both ways: it eats what
the parent eats, and whoever hunted the parent hunts it (SPEC-31). It is set up and validated like a species at
`Initialize`; an error throws an `InvalidOperationException` and the clone is removed. An id is never reused: a second
`deer` becomes `deer-2` (SPEC-32). Call it between ticks, or from a phase outside any loop over `World.AllSpecies`.

## Dependencies

A module that needs another one beside it says so with <xref:EvoSim.RequiresModuleAttribute>. The sample death rule
needs a thirst:

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
        if (thirst == null) return null;                         // no Thirst: nothing to do
        a[thirst.Value] += thirst.PerTick;
        return a[thirst.Value] >= thirst.Deadly ? "thirst" : null;
    }
    // … (Validate)
}
```

Put it on a species module's class, once per module needed; subclasses inherit it, and a subclass of the named type
counts. The named module is looked for on the same species, or under the World for a world module. When it is missing,
validation warns (V-26: *DiesOfThirst needs a Thirst on 'prey'*) with a fix that adds it on a new child named after it
(no fix for an abstract type). It is only a warning, so the run still starts: look the companion up in `Initialize` and
cope with null, as above. The conformance suites add the companions before they test your module.

## Validation

Override `Validate` to catch configuration mistakes before Play. Add one message per problem to the
<xref:EvoSim.ValidationReport>, naming the object at fault (usually `this`), with a validator id from the catalogue of
21 §2 that fits (V-20 for a name that matches nothing, V-35 for a value out of range…):

| Method | Effect |
|---|---|
| [Error(id, obj, text, fix)](xref:EvoSim.ValidationReport.Error(System.String,UnityEngine.Object,System.String,EvoSim.ValidationFix)) | blocks Play and the run |
| [Warning(id, obj, text, fix)](xref:EvoSim.ValidationReport.Warning(System.String,UnityEngine.Object,System.String,EvoSim.ValidationFix)) | the run starts; something is probably wrong |
| [Info(id, obj, text, fix)](xref:EvoSim.ValidationReport.Info(System.String,UnityEngine.Object,System.String,EvoSim.ValidationFix)) | worth knowing |

A <xref:EvoSim.ValidationFix> is a button in the inspector's validation list: a label and the change to make; the
editor records it for Undo, then validates again. Validation itself only reads: the change belongs in the fix. Write
each message for a student who has never seen your code: what is wrong, and the rule it breaks.

```csharp
public class PreferredFood : SpeciesModule
{
    [SerializeField, Tooltip("A food layer's name.")] string layer = "Grass";
    [SerializeField, Range(0f, 1f), Tooltip("How strongly it prefers that layer.")] float preference = 0.5f;
    // …

    public override void Validate(ValidationReport report)
    {
        if (World.Service<ResourceLayer>(layer) == null)
        {
            var first = World.Service<ResourceLayer>();
            report.Error("V-20", this, $"No food layer is named '{layer}'.",
                         first == null ? null : new ValidationFix($"Use {first.LayerName}", () => layer = first.LayerName));
        }
        if (!(preference >= 0f && preference <= 1f))                   // also catches NaN
            report.Error("V-35", this, $"The preference {preference} is outside [0, 1].",
                         new ValidationFix("Clamp", () => preference = Mathf.Clamp01(preference)));
    }
}
```

## Test it

- **The stat suite** (T-ANIM, `ConformanceTests.StatConformance`) runs every species module with a public `StatId`
  property: founders start as declared, and the stat stays in its range for 1 000 ticks.
- **Your own tests** derive from <xref:EvoSim.Testing.WorldFixture> and build a world in code
  ([Testing your module](testing-your-module.md)). To test validation, build it without initialising and prepare it:

```csharp
var w = New(name: "no layer").Flat(20, 20)
    .Species("prey", s => s.Population(0).Action<RestAction>("rest").Module<PreferredFood>())
    .BuildUninitialized();
var report = new ValidationReport();
Assert.IsFalse(w.Prepare(report));
Assert.IsTrue(report.Has("V-20"), report.ToString());
```

## Read next

- References: <xref:EvoSim.Energy>, <xref:EvoSim.Stamina>, <xref:EvoSim.Metabolism>, <xref:EvoSim.DecisionSchedule>; the
  samples <xref:EvoSim.Samples.Thirst>, <xref:EvoSim.Samples.DiesOfThirst>, <xref:EvoSim.Samples.DrinkAction>.
- The specialised kinds: [A sense](sense.md), [An action](action.md), [Stats, costs and death rules](stats-and-death.md),
  [Genes](genes.md), [A locomotion](locomotion.md).
- [Phases, services and world hooks](world-modules.md), for what all species share.
