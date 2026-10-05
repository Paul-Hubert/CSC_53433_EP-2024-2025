# 05 — Simulation loop

One runner, one ordered list of systems, one tick at a time. Decisions may
take seconds; the simulation waits for them (lockstep), so a slow machine
gives the same results, just later.

## Runner, simulation, systems

```text
SimulationRunner (MonoBehaviour, scene)      the ONLY Update() in simulation code
  │  pacing: ticksPerSecond (0 = as fast as possible), pause, single step
  ▼
Simulation.StepAsync()                       plain C#, also used by the headless CLI
  │  for each system in (phase, order):
  │      await system.TickAsync(ctx)         most complete synchronously
  │  ctx.Tick++ ; Events.Publish(TickCompleted)
  ▼
Views interpolate between tick snapshots in LateUpdate (presentation only)
```

```csharp
public sealed class SimulationRunner : MonoBehaviour
{
    [SerializeField] SimulationProfile profile;
    [SerializeField] WorldBindings sceneBindings;          // e.g. the CustomTerrain for TerrainWorld
    [SerializeField, Min(0)] float ticksPerSecond = 10f;   // 0 = as fast as the backend allows

    public Simulation Simulation { get; private set; }
    public bool Paused { get; set; }
    Task pendingStep;
    float budget;

    void Start() => Simulation = Simulation.Create(profile, sceneBindings, RunFolder.ForPlayMode());

    void Update()
    {
        if (pendingStep != null)
        {
            if (!pendingStep.IsCompleted) return;          // lockstep: wait for decisions
            pendingStep.GetAwaiter().GetResult();          // surface exceptions here, not silently
            pendingStep = null;
        }
        if (Paused) return;
        budget += ticksPerSecond > 0 ? Time.deltaTime * ticksPerSecond : 1f;
        if (budget < 1f) return;
        budget -= 1f;
        pendingStep = Simulation.StepAsync(destroyCancellationToken);
    }

    public void StepOnce() { if (pendingStep == null) pendingStep = Simulation.StepAsync(destroyCancellationToken); }
}
```

No `async void`: the runner holds the step `Task` and checks it each frame.
The editor's play controls (pause, step, speed) call the same members.

## The system contract

```csharp
public enum SimPhase { World = 0, Decide = 1, Act = 2, Interact = 3, Lifecycle = 4, Record = 5 }

public interface ISimulationSystem
{
    SimPhase Phase { get; }
    int Order { get; }                                  // within a phase, ascending
    void Initialise(SimContext ctx);
    ValueTask TickAsync(SimContext ctx, CancellationToken ct);
}

/// Template for the common synchronous case: override Tick, done.
[Serializable]
public abstract class SimulationSystem : ISimulationSystem
{
    public abstract SimPhase Phase { get; }
    public virtual int Order => 0;
    public virtual void Initialise(SimContext ctx) { }
    public ValueTask TickAsync(SimContext ctx, CancellationToken ct) { Tick(ctx); return default; }
    protected abstract void Tick(SimContext ctx);
}
```

Systems are `[SerializeReference]` entries in `SimulationProfile.systems`,
so a student adds a `WeatherSystem` by writing one class and picking it
from the dropdown. The inspector groups the list by phase so the tick
order is always visible.

## Default pipeline

| Phase | System (order) | What it does | RNG stream | Prototype (`sim.py`) |
|---|---|---|---|---|
| World | `FoodRegrowthSystem` | regrow food, × bonus near water | `food` | `world.regrow_food` (end of `step`) |
| Decide | `DecisionSystem` | due agents (period elapsed or newcomer): sense → `DecisionScheduler` → sample → set `Brain.Current` | `sampling` | `decide()` |
| Act | `ActionSystem` | species by `actOrder`; agents in a random permutation; unavailable → fallback + invalid; locomotion; `Metabolism.ApplyTickCost` | `actions` | executor loop |
| Act (10) | `PhysicsStepSystem` *(optional)* | `Physics.Simulate(tickSeconds)` for physical bodies | — | — |
| Interact | `MatingSystem` (0) | pair ready agents that chose *mate* within reach → birth queue | — | `_breed` |
| Interact | `BirthSystem` (1) | crossover → mutation → development → spawn (**async** if the LLM mutates) | `mutation` | `_birth` |
| Interact | `PredationSystem` (2) | prey on a predator's cell is killed with `kill_p`; predator rests | `predators` | `_predation` |
| Lifecycle | `AgentTickSystem` (0) | `IAgentTick.OnTick` on components that opt in | — | — |
| Lifecycle | `DeathSystem` (1) | asks every `IDeathCheck` (Metabolism: starvation, old age; students: more) → despawn | — | death loop |
| Lifecycle | `ImmigrationSystem` | refill to each species' floor from its founder pool | `agents` | floor loop |
| Record | `StatsSystem` | one stats row every `statsEveryTicks` | — | `log.stats` |

Parity with the prototype is **behavioural and statistical**, not
bit-for-bit: numpy's generators are not reproduced, and scripted predators
now act as a species in `ActionSystem` (after the animals, by `actOrder`).
Pure functions (rendering, rule-based logits, word operators, guards) are
checked against Python golden files ([12](12-testing-headless-migration.md)).

## Decisions inside a tick (lockstep barrier)

```text
DecisionSystem.TickAsync
  due = agents where Brain.Current == null || tick % species.decisionPeriodTicks == 0
  group by species
  for each species:
     requests = due.Select(a => new DecisionRequest(a.Id, a.Genome, a.Phenotype, Sense(a), species.ActionSet))
     results  = await ctx.Decisions.DecideAsync(species, requests, ct)      ← may call Ollama; the tick waits
     for each (agent, result) in REQUEST order:                            ← never completion order
         action = Sample(result.Distribution, ctx.Rng.Get("sampling"))
         agent.Brain.Set(action, observation, result)
         Events.Publish(new DecisionMade(...))
```

If the model server is down, the tick would wait forever, so the default
chain includes a `FallbackDecorator` with a timeout that answers from the
rule-based backend and logs a `fallback` event ([06](06-decisions.md)).

`DecisionSystem.timing` is `Lockstep` (default, deterministic) or
`Realtime`: agents keep running their current action while a decision is
pending and switch when it arrives. `Realtime` is for live demos and VR
([11](11-change-scenarios.md#4-vr-interaction-with-the-animals)), never
for experiments.

## Context objects (explicit dependencies)

```csharp
public sealed class SimContext
{
    public int Tick { get; internal set; }
    public float TickSeconds { get; }               // sim seconds per tick (continuous worlds)
    public RngStreams Rng { get; }
    public IWorld World { get; }
    public Population Population { get; }          // living agents, birth queue, AlleleRegistry
    public DecisionScheduler Decisions { get; }
    public EventBus Events { get; }
    public SimulationProfile Profile { get; }       // the runtime clone, never the asset
}
```

`SenseContext` and `ActionContext` are `readonly struct`s built from it,
narrowed to what a sense or an action may touch. There are no singletons:
two simulations can run side by side (the Gene Playground does this for
A/B comparisons).

## Randomness

```csharp
var rng = ctx.Rng.Get("sampling");        // same name + same seed → same sequence, every run
int i = rng.Choice(distribution.Probabilities);
```

- `RngStreams` derives each stream's seed from `hash(simulationSeed, name)`
  (the prototype's `rng.Streams`), backed by a small portable PRNG
  (xoshiro128\*\*) in Core — not `System.Random`, not `UnityEngine.Random`.
- Built-in stream names: `world`, `food`, `agents`, `sampling`, `actions`,
  `mutation`, `predators`, `development`. A student system asks for its own
  name (`"weather"`) and cannot disturb any other stream.
- `worldSeed` feeds only `world`; changing the simulation seed keeps the
  same map (E4: one world, several runs).

## Determinism checklist

| Rule | Why |
|---|---|
| All randomness through named streams | reproducibility; independence of subsystems |
| Iterate agents by id, or by a permutation drawn from a named stream | `List` order after removals is not a design decision |
| Never iterate `Dictionary` / `HashSet` to decide simulation order | enumeration order is unspecified |
| Apply async results in request order | network completion order varies |
| Model answers are cached with model + digest + prompt hash | same question → same answer, even on cloud models that change |
| `Physics.simulationMode = Script`, stepped inside the tick | physics becomes part of lockstep |
| Observers never mutate state | logging/visualisation can't change results |

The determinism test runs 500 ticks twice and compares the SHA-256 of the
event stream — the same check as the prototype's `events_sha`.

## Events

```csharp
public sealed class EventBus
{
    public void Subscribe<T>(Action<T> handler) where T : struct, ISimEvent;
    public void Unsubscribe<T>(Action<T> handler) where T : struct, ISimEvent;
    public void Publish<T>(in T e) where T : struct, ISimEvent;   // synchronous, subscription order
}
```

| Event | Fields | JSONL `kind` (prototype-compatible) |
|---|---|---|
| `FounderSpawned` / `ImmigrantSpawned` | tick, id, genome | `founder` / `immigrant` |
| `AgentBorn` | tick, id, parents, generation, genome, mutations | `birth` |
| `AgentDied` | tick, id, cause, age, generation, food, offspring, steals | `death` |
| `GeneMutated` | locus, parent allele, child allele, operator, text | inside `birth.mutations` |
| `DecisionMade` | agent, observation key, distribution, chosen, cache hit | `decision` (optional, verbose) |
| `ActionInvalid`, `FoodEaten`, `AttackResolved`, `DecisionFallback` | … | optional |
| `StatsRow` | the prototype's stats columns | `stats.csv` |

`RunRecorder` subscribes and fans out to the profile's sinks. The Population
Dashboard, the Genome Browser and views subscribe the same way.

## Headless and batch

The same `Simulation` runs without a scene runner:

```text
Unity -batchmode -nographics -projectPath . \
      -executeMethod GeneticAgents.Editor.Cli.RunExperiment \
      -experiment Assets/GeneticAgents/Samples/Experiments/E4.asset -out Runs/E4
```

The CLI validates the experiment first (same rules as the Simulation
Doctor; errors abort with a non-zero exit code), then runs
`conditions × seeds` sequentially, writing one folder per run:
`events.jsonl`, `stats.csv`, `alleles.jsonl`, `summary.json`,
`final_population.json` — the prototype's layout — plus `run_info.json`
(profile hash, seeds, git commit, model digests, prompt hashes), so its analysis scripts
apply unchanged. Agents remain GameObjects (cheap at 20–60 agents); their
`Model/` child is disabled when no camera renders them.
