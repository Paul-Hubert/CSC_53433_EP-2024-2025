---
uid: extend-world-modules
---

# Phases, services and world hooks

A world module sits under the World, outside every species, and serves all species at once. Write a
<xref:EvoSim.TickPhase> to make something happen every tick (seasons, weather, an epidemic), and a
<xref:EvoSim.WorldService> to share state or a query between modules (a calendar, a water map). Both derive from
<xref:EvoSim.WorldModule>, whose [hooks](#hooks) let any world module react to births, deaths, new species and the end of
the run. For a feature of one species, write a [species module](species-modules.md) instead.

## A tick phase

| Member | You | What it does, when |
|---|---|---|
| [Run(t)](xref:EvoSim.TickPhase.Run(EvoSim.TickContext)) | must implement | the phase's work, once per tick, in its place among the phases |
| [WaitsFor(t)](xref:EvoSim.TickPhase.WaitsFor(EvoSim.TickContext)) | may override | the work the phase must wait for before `Run`, or null (the default) |
| [PhaseName](xref:EvoSim.TickPhase.PhaseName) | may override | the name in logs, stop reasons and the profiler; the class name by default |
| [PlaceAfter(other)](xref:EvoSim.TickPhase.PlaceAfter(EvoSim.TickPhase)), [PlaceBefore(other)](xref:EvoSim.TickPhase.PlaceBefore(EvoSim.TickPhase)) | call in `Validate` | a fix that moves this phase just after or before another one (V-08) |
| `Initialize`, `Validate` and the [hooks](#hooks) | may override | inherited from `WorldModule`; a phase initialises after every species module |

Recipe 22 §9, adapted from the sample <xref:EvoSim.Samples.SeasonsPhase>: in the second half of every year the grass
regrows more slowly.

```csharp
using EvoSim;
using UnityEngine;

/// <summary>Slows the grass's regrowth in the second half of every year; must run before Environment.</summary>
public class WinterPhase : TickPhase
{
    [SerializeField, Min(2), Tooltip("Ticks per year (reference 2 000).")] int yearLength = 2000;
    [SerializeField, Range(0f, 1f), Tooltip("Regrowth factor in winter (reference 0.2).")] float winterFactor = 0.2f;
    [SerializeField, Tooltip("The food layer's name.")] string layer = "Grass";
    FoodGrid food;

    public bool IsWinter(int tick) => tick % yearLength >= yearLength / 2;

    public override void Initialize() => food = World.Service<FoodGrid>(layer);

    public override void Run(TickContext t)
    {
        if (food != null) food.RegrowFactor = IsWinter(t.Tick) ? winterFactor : 1f;
    }

    public override void Validate(ValidationReport report)
    {
        if (World.Service<FoodGrid>(layer) == null)
            report.Error("V-20", this, $"Winter acts on the food layer '{layer}', which doesn't exist.");
        var environment = World.Phase<EnvironmentPhase>();
        if (environment != null && IndexOf(environment) < IndexOf(this))
            report.Error("V-08", this, "Winter must come before the Environment phase.", PlaceBefore(environment));
    }

    int IndexOf(TickPhase p)
    {
        for (int i = 0; i < World.Phases.Count; i++) if (World.Phases[i] == p) return i;
        return -1;
    }
}
```

**Where it goes.** *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Phase* writes an empty phase into `Assets/Student`, with a test.
Add the component on a new child of the World's `Phases` object: its place among the children is its place in every
tick. Here it must come before *Environment*, which applies the factor; the World inspector's phase list shows the
order and moves a phase with ▲. The reference phases check their own order (V-08): *Ask brains* after *Sense*,
*Choose actions* after *Ask brains*, *Hatch* after *Breed*.

### Waiting for asynchronous work

A phase that needs an answer from another thread or a server returns it from `WaitsFor` as a <xref:EvoSim.Pending>. The
World runs the phase only when the pending work is done: in Responsive mode it comes back on a later frame, in Freeze
mode it blocks on [Wait](xref:EvoSim.Pending.Wait) ([A tick, phase by phase](../guides/the-tick.md#waiting-for-the-brain)).

| Build it with | Done when |
|---|---|
| [Pending.Done()](xref:EvoSim.Pending.Done) | at once |
| [Pending.Of(task)](xref:EvoSim.Pending.Of(System.Threading.Tasks.Task)) | the `Task` completes (a null task: at once) |
| [Pending.All(parts)](xref:EvoSim.Pending.All(System.Collections.Generic.IReadOnlyList{EvoSim.Pending})) | every part is done; returns null for an empty list |
| a <xref:EvoSim.ManualPending> | your code calls [SetDone](xref:EvoSim.ManualPending.SetDone); in Freeze mode its completion runs at once |

```csharp
using System.Threading.Tasks;
using EvoSim;

/// <summary>Computes next tick's rain on another thread while the rest of the tick runs.</summary>
public class RainPhase : TickPhase
{
    Task<float> forecast;                                   // started by the previous tick
    public float Rain { get; private set; }

    public override void Initialize() { forecast = null; Rain = 0f; }

    public override Pending WaitsFor(TickContext t) => Pending.Of(forecast);

    public override void Run(TickContext t)
    {
        Rain = forecast != null ? forecast.Result : 0f;
        int next = t.Tick + 1;
        forecast = Task.Run(() => RainAt(next));            // pure: no World, no Unity object
    }

    static float RainAt(int tick) => System.Math.Sin(tick * 0.01) > 0.5 ? 1f : 0f;
}
```

The work on the other thread reads nothing of the World and touches no Unity object, and its result doesn't depend on
when it finishes: the run is the same in both wait modes (RAND-11). The reference does the same with the brains
(<xref:EvoSim.ChooseActionsPhase> waits for `World.Decisions.Pending`) and the mutator (<xref:EvoSim.HatchPhase>).

## A world service

| Member | You | What it does, when |
|---|---|---|
| [ServiceName](xref:EvoSim.WorldService.ServiceName) | may override | the name modules find it by; its GameObject's name by default |
| [WritePromptRules(species, w)](xref:EvoSim.WorldService.WritePromptRules(EvoSim.Species,EvoSim.PromptWriter)) | may override | rule lines for one species' prompt, after the species' own modules wrote theirs |
| `Initialize`, `Validate` and the [hooks](#hooks) | may override | a service initialises before the species' modules exist |

```csharp
/// <summary>The year's seasons, shared by every module that asks.</summary>
public class Calendar : WorldService
{
    [SerializeField, Min(2), Tooltip("Ticks per year (reference 2 000).")] int yearLength = 2000;

    public bool IsWinter(int tick) => tick % yearLength >= yearLength / 2;

    public override void WritePromptRules(Species species, PromptWriter w) =>
        w.Rule($"Every {yearLength} steps, a winter of {yearLength / 2} steps slows the grass.");
}
```

Put it anywhere under the World but outside the species, for example under `Environment`. Other modules find it in
their `Initialize`, by role, never through a field pointing into the scene (ARCH-08):

| Call | Finds |
|---|---|
| [`World.Service<T>()`](xref:EvoSim.World.Service*) | the first enabled service of type `T` (a class or an interface), or null |
| `World.Service<T>(name)` | the one whose `ServiceName` is `name`, such as the layer `"Grass"` |
| [`World.ServicesOf<T>()`](xref:EvoSim.World.ServicesOf*) | all of them, in hierarchy order |

Layers and brains are found by name, so two of them may not share one (V-18). A service initialises before the species'
modules: look up species modules later, in `Begin` or when first needed.

## Hooks

Every world module, phase or service, may override these. Each is called on the world modules in hierarchy order. A
species module has its own `OnBorn` and `OnDied`, for its species' animals only ([Species modules](species-modules.md)).

| Hook | Called |
|---|---|
| [Begin()](xref:EvoSim.WorldModule.Begin) | once per run, after every module initialised and validation passed, before the founders spawn; never during Edit-mode validation. Place initial content here (the first food items). |
| [OnSpeciesAdded(species)](xref:EvoSim.WorldModule.OnSpeciesAdded(EvoSim.Species)) | when `World.AddSpecies` adds a species during the run, after its set-up and its `species_created` event, before its founders |
| [OnBorn(a)](xref:EvoSim.WorldModule.OnBorn(EvoSim.Animal)) | each animal of any species that joins the living, founders included, after its species' modules heard it |
| [OnDied(a, cause)](xref:EvoSim.WorldModule.OnDied(EvoSim.Animal,System.String)) | each death, after it was recorded and the species' modules heard it; the animal is marked gone |
| [OnRunStopped(reason)](xref:EvoSim.WorldModule.OnRunStopped(System.String)) | once, when the run stops, whatever the reason; write and close your files here |

```csharp
/// <summary>Counts the births and deaths of every species and reports them when the run ends.</summary>
public class LifeCounter : WorldService
{
    int born, died;

    public override void Initialize() { born = 0; died = 0; }
    public override void OnBorn(Animal a) => born++;
    public override void OnDied(Animal a, string cause) => died++;
    public override void OnRunStopped(string reason) => Debug.Log($"{born} births, {died} deaths ({reason})");
}
```

An exception in `OnRunStopped` is logged, and the other modules still hear the stop.

### World.Configuring

The static event <xref:EvoSim.World.Configuring> is raised by every World in `Awake`, in Play mode, before the run
exists. A launcher sets the seed, speed or brain there, or turns **Start On Play** off to start the run itself. The
player's command line works this way (<xref:EvoSim.PlayerLauncher>).

```csharp
public static class FrozenRuns
{
    static void SetUp(World w) { w.Seed = 7; w.WaitMode = WaitMode.Freeze; }

    public static void Install() { World.Configuring -= SetUp; World.Configuring += SetUp; }
    public static void Uninstall() => World.Configuring -= SetUp;
}
```

The event is static: a handler stays subscribed across scenes and, when domain reload is off, across Play sessions.
Always unsubscribe when you are done, and unsubscribe before subscribing so a handler never runs twice.

## Rules your module keeps

- **Change only what the phase is responsible for** (TICK-02); with nothing to do, return at once (TICK-07).
- **Random numbers only from named streams**: `t.Stream(species, "purpose")` or `t.Stream("name")` in a phase,
  `World.Random.Get("world/" + ServiceName)` for a service's map content (RAND-01, RAND-03, RAND-04).
- **`WaitsFor` only looks**: no random draw and no change to the world, since it may be asked several times per tick.
- **Set up in `Initialize`, create content in `Begin`**: `Initialize` and `Validate` also run in Edit mode.
- **No static state** (ARCH-07): several Worlds may run side by side.

## Test it

- **The phase suite** (T-TICK, `ConformanceTests.PhaseConformance`) runs every phase class 20 ticks in a world without
  animals, and checks that switching it off and on leaves a populated world's events hash unchanged.
- The test written by the *Phase* template builds a small world with your phase and advances it 10 ticks. Add a test
  that runs twice with the same seed ([Determinism and replay](../guides/determinism.md#the-static-checks)), and see
  [Testing your module](testing-your-module.md).

## Read next

- The reference phases, from <xref:EvoSim.SensePhase> to <xref:EvoSim.RecordPhase>, and the sample
  <xref:EvoSim.Samples.SeasonsPhase>.
- Services with hooks: <xref:EvoSim.FoodGrid> (`Begin`), <xref:EvoSim.RunRecorder> (`Begin`, `OnRunStopped`); a test
  phase that waits: <xref:EvoSim.Testing.DelayedPhase>.
- [The world and its modules](../guides/world-and-modules.md), [A tick, phase by phase](../guides/the-tick.md).
- [Food, cover, entities and ground](environment.md) and [Recording and statistics](recording.md): the specialised
  services.
