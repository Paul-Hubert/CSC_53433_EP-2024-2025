# 03 — Agent anatomy

An agent is a prefab whose components **are** its capabilities. Read the
prefab top to bottom and you know what the creature can sense, what it can
do, how it moves and how it spends energy.

## The prefab

```text
Herbivore (prefab)                         Inspector summary
├─ AgentRoot                               species: Herbivore (SpeciesDefinition)
├─ Metabolism                              max 100 · start 60 · base 0.7/tick · move +0.5 · rest 0.2 · adult at 150 · max age 1500
├─ Brain                                   decision period: species default (4 ticks)
├─ GridLocomotion                          (or KinematicLocomotion / RigidbodyLocomotion / …)
├─ Senses/            (child GameObject, grouping only)
│   ├─ EnergySense                         low < 30 · high > 70     → field "energy"
│   ├─ AgeSense                                                     → field "age"
│   ├─ FoodSense                           near 3 · vision 12       → field "food"
│   ├─ PredatorSense                       near 3 · vision 12       → field "predator"
│   └─ NeighbourSense                      near 3 · vision 12       → "animal", "animal_ready", "animal_stronger"
├─ Actions/           (child GameObject, grouping only)
│   ├─ EatAction        locus: eat         "go to the nearest visible food and eat it"
│   ├─ FleeAction       locus: flee        "run away from the nearest predator"
│   ├─ FollowAction     locus: follow      …
│   ├─ WanderAction     locus: wander      (species fallback action)
│   ├─ RestAction       locus: rest
│   ├─ MateAction       locus: mate
│   └─ AttackAction     locus: attack
└─ Model/             AgentView · AnimatorView · mesh      (presentation only)
```

Defaults are the prototype's `configs/base.yaml` values. A species variant
(bigger, slower, short-sighted) is a **prefab variant** that overrides a few
fields — Unity's built-in Prototype pattern.

## Component roles

| Component | Role | Holds state? | Called by |
|---|---|---|---|
| `AgentRoot` | Identity (id, species, generation, parents, birth tick), `Genome`, `Phenotype`, cached capability lists | yes (set once at spawn) | `AgentFactory`, everyone reads |
| `Metabolism` | Energy, age, maturity, costs, death check | yes | `ActionSystem`, `DeathSystem`, actions |
| `Brain` | Current action, last observation / distribution / trace, decision period override | yes | `DecisionSystem`, Brain Debugger |
| `AgentSense` (n) | Turns the world into labelled observation fields | no (tunables only) | `DecisionSystem` |
| `AgentAction` (n) | One behaviour; its **locus** is the gene that describes when to do it | small per-action state (target) | `ActionSystem` |
| `AgentLocomotion` | How the body moves: grid steps, character controller, physics, DRL | yes (heading, velocity) | actions |
| Views | Meshes, animation, labels | presentation only | Unity (`LateUpdate`) |

**Rule:** simulation components have no `Update()`. Systems call them in
a fixed order, once per tick. Views may use `LateUpdate()` because they
only read.

`AgentRoot` is the hub other code talks to:

```csharp
public sealed class AgentRoot : MonoBehaviour
{
    public int Id { get; }                       public SpeciesDefinition Species { get; }
    public Genome Genome { get; }                public Phenotype Phenotype { get; }
    public int Generation { get; }               public IReadOnlyList<int> Parents { get; }
    public Vector3 Position => Locomotion.Position;
    public Metabolism Metabolism { get; }        public Brain Brain { get; }
    public AgentLocomotion Locomotion { get; }
    public IReadOnlyList<AgentSense> Senses { get; }     // in field order
    public IReadOnlyList<AgentAction> Actions { get; }   // in genome-schema order = ActionSet
    public T GetCapability<T>() where T : AgentComponent; // cached at spawn, no scene search
}
```

### Optional hooks

A component opts into simulation services by implementing an interface;
the matching system calls it in agent-id order:

| Interface | Called by | Typical use |
|---|---|---|
| `IAgentTick` | `AgentTickSystem` (Lifecycle) | per-tick state change (thirst, cooldowns, memory) |
| `IDeathCheck` | `DeathSystem` | return a `DeathCause` (`Starvation`, `OldAge`, or a new one such as `new DeathCause("dehydration")`) |
| `IPhenotypeReceiver` | `AgentFactory` at birth | read evolved trait values (vision, speed) |
| `IActionOverride` | `ActionSystem` | external control while active (held in VR, stunned) |

`Metabolism` itself implements `IDeathCheck`; nothing about starvation is
hard-coded in `DeathSystem`.

## Base classes (Template Method)

All simulation components derive from one small base, so pooling and
lookup work the same everywhere:

```csharp
namespace GeneticAgents.Agents
{
    /// Base for every simulation component on an agent. No Update(): systems call us.
    public abstract class AgentComponent : MonoBehaviour, IValidatable
    {
        public AgentRoot Root { get; private set; }

        /// Called by AgentFactory when a pooled instance becomes a new agent. Reset per-agent state here.
        public virtual void OnSpawned(AgentRoot root) => Root = root;

        /// Called when the agent dies and returns to the pool.
        public virtual void OnDespawned() { }

        /// Local checks only (this component's own fields). Cross-object checks are ValidationRules.
        public virtual void Validate(ValidationReport report) { }
    }
}
```

### Senses

```csharp
/// A sense adds labelled fields to the observation. It never decides anything.
public abstract class AgentSense : AgentComponent
{
    /// The fields this sense can emit and every value each can take (a closed vocabulary).
    /// Used for phrase-book checks, one-hot encoding and observation-space size estimates.
    public abstract IEnumerable<FieldSpec> Fields { get; }

    public abstract void Sense(in SenseContext ctx, ObservationBuilder obs);
}
```

```csharp
[AddComponentMenu("Genetic Agents/Senses/Food Sense")]
public sealed class FoodSense : AgentSense
{
    [Tooltip("Distance (cells or metres) at or below which food is 'near'.")]
    [SerializeField, Min(0)] float near = 3;
    [Tooltip("Food farther than this is not seen at all.")]
    [SerializeField, Min(0)] float vision = 12;

    public override IEnumerable<FieldSpec> Fields =>
        new[] { new FieldSpec("food", "none", "far", "near", "here") };

    public override void Sense(in SenseContext ctx, ObservationBuilder obs)
    {
        FoodHit? food = ctx.World.NearestFood(ctx.Self.Position, vision);
        obs.Add("food", food is null           ? "none"
                      : food.Value.Distance == 0 ? "here"
                      : food.Value.Distance <= near ? "near" : "far");
    }

    public override void Validate(ValidationReport r)
    {
        if (near > vision) r.Error(this, "'near' is larger than 'vision': food can never be 'far'.",
                                   fix: ("Set near = vision / 4", () => near = vision / 4));
    }
}
```

`SenseContext` gives exactly what a sense may read: `Self` (AgentRoot),
`World` (`IWorld`), `Neighbours` (spatial query over the population),
`Relations` (who is prey/predator/mate), `Tick`. Interoception is a sense
too: `EnergySense` reads `Self.Metabolism`, it does not live inside it.

### Actions

```csharp
/// One behaviour the brain can choose. The linked locus is the gene that describes
/// when this animal does it, so adding an action adds a gene slot (see GenomeSchema).
public abstract class AgentAction : AgentComponent
{
    [Tooltip("Locus whose gene text describes this behaviour (homologous across the species).")]
    [SerializeField] LocusDefinition locus;
    [Tooltip("One line shown to the LLM in the action list of the prompt.")]
    [SerializeField, TextArea(1, 2)] string promptDescription;

    public LocusDefinition Locus => locus;
    public string ActionId => locus.Id;                  // "eat", "flee", …
    public string PromptDescription => promptDescription;

    /// False when there is nothing to act on (no food in sight…). The species
    /// fallback action runs instead and the choice is counted as invalid.
    public virtual bool IsAvailable(in ActionContext ctx) => true;

    /// Called on the tick this action is chosen.
    public virtual void Begin(in ActionContext ctx) { }

    /// One tick of behaviour. Move only through ctx.Locomotion; change the world only through ctx.
    public abstract void Tick(in ActionContext ctx);

    /// Which metabolic rate applies this tick (Rest uses Metabolism.restCost).
    public virtual Effort Effort => Effort.Normal;

    /// Target and label for gizmos and the Brain Debugger. Read-only.
    public virtual ActionDebugInfo DebugInfo => default;
}
```

```csharp
[AddComponentMenu("Genetic Agents/Actions/Eat Action")]
public sealed class EatAction : AgentAction
{
    [SerializeField, Min(0)] float vision = 12;
    [SerializeField, Min(0)] float energyPerFood = 25;
    Vector3? target;

    public override bool IsAvailable(in ActionContext ctx) =>
        ctx.World.HasFoodAt(ctx.Self.Position) || ctx.World.NearestFood(ctx.Self.Position, vision) != null;

    public override void Tick(in ActionContext ctx)
    {
        if (TryEatHere(ctx)) return;
        target = ctx.World.NearestFood(ctx.Self.Position, vision)?.Position;
        if (target is Vector3 t) ctx.Locomotion.MoveToward(t);
        TryEatHere(ctx);                          // arrived this tick → eat now (prototype parity)
    }

    bool TryEatHere(in ActionContext ctx)
    {
        if (!ctx.World.TryConsumeFood(ctx.Self.Position)) return false;
        ctx.Self.Metabolism.Gain(energyPerFood);
        ctx.Events.Publish(new FoodEaten(ctx.Tick, ctx.Self.Id));
        return true;
    }

    public override ActionDebugInfo DebugInfo => new(target, "eat");
}
```

`ActionContext` = `SenseContext` + `Locomotion`, `Rng` (the `actions`
stream), `Events`, `Interactions` (attack/mate helpers). Mating itself is
resolved by `MatingSystem` after all actions, as in the prototype.

### Locomotion (Bridge)

Actions say **what** to do ("move toward that food"); locomotion decides
**how** the body gets there. That split is what lets the same genes drive a
grid token, a capsule on terrain or a physics-simulated quadruped.

```csharp
public abstract class AgentLocomotion : AgentComponent
{
    public abstract Vector3 Position { get; }
    public bool MovedThisTick { get; protected set; }

    public abstract void MoveToward(Vector3 target);
    public abstract void MoveAwayFrom(Vector3 threat);
    public abstract void Wander(IRandom rng);       // persistent random walk
    public abstract void Stop();

    /// Physical bodies can report real effort; Metabolism adds it to the tick cost.
    public virtual float EffortThisTick => 0f;
}
```

| Implementation | Movement | World | Use |
|---|---|---|---|
| `GridLocomotion` | one 8-neighbour step per tick, greedy + side-step when stuck | `GridWorld` | parity with Python, fastest, default lab |
| `KinematicLocomotion` | `CharacterController` / NavMesh at a set speed | `TerrainWorld` | terrain lab continuity |
| `RigidbodyLocomotion` | forces on a capsule, slope-aware | `TerrainWorld` | physics interaction, VR |
| `MLAgentsLocomotion` | a trained DRL policy tracks the goal on an articulated body | `TerrainWorld` | advanced project ([11](11-change-scenarios.md#3-physics-based-bodies-driven-by-a-drl-controller)) |

### Metabolism and Brain

```csharp
public sealed class Metabolism : AgentComponent
{
    [SerializeField] float maxEnergy = 100, startEnergy = 60;
    [SerializeField] float baseCostPerTick = 0.7f, moveCostPerTick = 0.5f, restCostPerTick = 0.2f;
    [SerializeField] int adultAtTick = 150, maxAgeTicks = 1500;

    public float Energy { get; private set; }
    public int AgeTicks { get; private set; }
    public bool IsAdult => AgeTicks >= adultAtTick;

    public void Gain(float e)  => Energy = Mathf.Min(maxEnergy, Energy + e);
    public void Spend(float e) => Energy -= e;

    /// Prototype rule: resting without moving costs restCost, otherwise base (+ move if moved).
    public void ApplyTickCost(Effort effort, bool moved, float bodyEffort)
    {
        Spend(effort == Effort.Resting && !moved ? restCostPerTick
              : baseCostPerTick + (moved ? moveCostPerTick : 0f) + bodyEffort);
        AgeTicks++;
    }

    public DeathCause? CheckDeath() =>
        Energy <= 0 ? DeathCause.Starvation : AgeTicks > maxAgeTicks ? DeathCause.OldAge : null;
}
```

`Brain` holds the decision state that the debugger shows: `Current`
action, `LastObservation`, `LastDistribution`, `LastWasInvalid`,
`LastTrace` (filled by `TraceDecorator`), and an optional period override.
It never calls a model: the `DecisionScheduler` batches all due agents of a
species into one request (see [06](06-decisions.md)).

## Lifecycle

```text
AgentFactory.Spawn(species, genome, parents, position)
   ├─ take instance from pool (or Instantiate prefab)
   ├─ AgentRoot.Initialise(id, species, genome, phenotype, generation, parents, tick)
   ├─ every AgentComponent.OnSpawned(root)           ← reset per-agent state here
   └─ EventBus: AgentBorn / FounderSpawned / ImmigrantSpawned
… ticks: Decide → Act → Interact → Lifecycle …
DeathSystem → EventBus: AgentDied(cause) → every AgentComponent.OnDespawned() → back to pool
```

The pool matters for long runs (thousands of births). Because state resets
in `OnSpawned`, a validator can flag fields that are not serialised
tunables but are never reset (see [09](09-editor-tooling.md)).

## What the agent does *not* contain

- No model calls, no HTTP: that is the decision chain (species level).
- No reproduction logic: `MatingSystem` and `BirthSystem` (population level).
- No logging: events go to the `EventBus`; the recorder writes them.
- No gizmos: `[DrawGizmo]` drawers in the Editor assembly read `DebugInfo`.

Each of those lives in exactly one place, so changing it never means
editing every creature.
