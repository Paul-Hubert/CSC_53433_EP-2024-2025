# 08 — Pattern catalogue

Every design pattern in the framework, the problem it solves here, and —
more importantly — how patterns **combine**, so that a student's change
lands in one predictable place.

Patterns were chosen for three properties:

- **Local:** using the pattern means adding one file (or one asset), not
  editing several.
- **Composable:** pieces snap together in the Inspector, so combinations
  need no new code.
- **Visible:** the structure shows up in the Inspector, the Scene view or a
  window, not only in code.

## The catalogue

| # | Pattern | Problem it solves here | Where | Student payoff |
|---|---|---|---|---|
| 1 | **Component (composition)** | An agent's capabilities vary by species and by experiment | `AgentSense`, `AgentAction`, `Metabolism`, `AgentLocomotion` on the prefab | Add a capability = add a component |
| 2 | **Template Method** | Every sense/action/system needs the same plumbing (lifecycle, validation, debug) | `AgentComponent`, `AgentSense`, `AgentAction`, `SimulationSystem` | Override one or two methods; plumbing is free |
| 3 | **Type Object** | Kinds of things (species, loci, relations) should not be classes | `SpeciesDefinition`, `LocusDefinition`, `SpeciesRelations` | New species / locus = new asset |
| 4 | **Prototype** | Variants of a creature or a run without copy-paste code | Prefab variants; runtime profile clone + modifiers | Variant = override a few fields |
| 5 | **Strategy** | Algorithms must be swappable per species/experiment | every `[SerializeReference]` slot: backend, crossover, operators, conditions, development, population, renderer, systems, sinks | Write a class, pick it from a dropdown |
| 6 | **Plugin discovery** | The framework must find student classes without referencing them | `TypeCache` scan by interface → `SubclassSelector` dropdown, validation rules, script templates | Zero registration code |
| 7 | **Decorator** | Cross-cutting concerns (cache, temperature, fallback, trace, controls) | `DecisionDecorator` chain; sinks; guarded operators | Add behaviour around anything without touching it |
| 8 | **Null Object** | Optional pieces shouldn't need null checks everywhere | `NoDevelopment`, neutral alleles, `RandomBackend`, uniform fallback distribution | Every slot always has a working default |
| 9 | **Pipeline / Chain of Responsibility** | Ordered processing with early exit | senses → `ObservationBuilder`; mutation guards; validation rules; tick phases | Insert a step anywhere in the order |
| 10 | **Specification (Composite)** | Rules made from reusable parts (`adult AND energy > 50`) | `IAgentCondition` with `All`, `Any`, `Not` | Build rules in the Inspector; read them as sentences |
| 11 | **Bridge** | What the agent does vs how its body moves vary independently | `AgentAction` ↔ `AgentLocomotion` | Same genes drive a grid token, a capsule, a ragdoll |
| 12 | **Adapter** | Plug external things into our interfaces | `TerrainWorld` (CustomTerrain), `OllamaBackend` (HTTP), `MLAgentsLocomotion`, `OnnxBackend` | Reuse earlier labs and external tools as-is |
| 13 | **Facade** | One narrow entry to a complex subsystem | `IWorld` over terrain + foliage + walkability; `CustomTerrain` itself | Senses ask simple questions (`NearestFood`) |
| 14 | **Flyweight / Registry** | Thousands of genomes, few hundred distinct gene texts | `AlleleRegistry` (genomes hold ids) | Allele frequency = counting ids |
| 15 | **Command** | Decisions and motor intents as data | `ActionDistribution` → chosen action; `MotorCommand` to locomotion; `IProfileModifier` | Replay, logging, swapping the executor |
| 16 | **Observer (event bus)** | Many readers of what happens; none may change it | `EventBus` → recorder, dashboards, views | Add analysis without touching the simulation |
| 17 | **Memento / Snapshot** | Save, compare and replay runs | run folder (`events.jsonl`, `alleles.jsonl`, `final_population.json`), genome export | Load any run in the Genome Browser |
| 18 | **Systems pipeline (Update Method, explicit phases)** | Tick order must be deterministic and visible | `ISimulationSystem` list grouped by `SimPhase` | Add a world rule = add a system |
| 19 | **Context Object** | Dependencies must be explicit, no singletons | `SimContext`, `SenseContext`, `ActionContext`, `DecisionContext` | Read the parameters → know the dependencies |
| 20 | **Factory + Object Pool** | Spawning is non-trivial and frequent | `AgentFactory` (species + genome → initialised, pooled instance) | One place to look for "how is an agent born" |
| 21 | **Humble Object** | MonoBehaviours are hard to test | `SimulationRunner` only paces; `Simulation` is plain C# | Unit-test the logic without a scene |
| 22 | **State (inside an action)** | Some behaviours have phases | e.g. `MateAction`: approach → court → breed | Complex behaviour stays inside one action |

## How the patterns combine ("chords")

Each common change is a fixed combination of patterns. Knowing the chord
tells a student which files to write.

| Chord | Patterns | You write | Example |
|---|---|---|---|
| **New behaviour** | Component + Template Method + Type Object | one `AgentAction` class, one `LocusDefinition` asset, founder alleles | `DrinkAction` + `drink` locus |
| **New perception** | Component + Pipeline + closed vocabulary | one `AgentSense` class (+ phrases) | `ThirstSense` → `thirst=low/medium/high` |
| **New algorithm** | Strategy + Plugin discovery + Null Object default | one class implementing the interface | `TournamentCrossover` |
| **Cross-cutting concern** | Decorator over Strategy | one decorator class | `NoiseDecorator` (ε-greedy exploration) |
| **New rule from parts** | Specification + Strategy | zero code (compose) or one condition | `All(IsAdult, InSameHerd)` |
| **New experiment condition** | Prototype + Command (modifiers) | zero code (compose modifiers) or one modifier | "C3 with τ = 2" |
| **New body** | Bridge + Adapter | one `AgentLocomotion` | `MLAgentsLocomotion` |
| **New world** | Adapter + Facade | one `IWorld` + one `WorldDefinition` | `IslandsWorld` |
| **New world rule** | Systems pipeline + Context Object | one `SimulationSystem` | `SeasonsSystem` |
| **New view / analysis** | Observer + Memento | one subscriber or one Editor window | lineage heatmap |
| **New safety check** | Plugin discovery + Chain of Responsibility | one `ValidationRule` | "prompt mentions every action" |

```text
             Type Object ──defines──► Component ◄──base── Template Method
                 │                       │
                 │ locus                 │ calls through
                 ▼                       ▼
        Strategy ◄──wrapped by── Decorator          Bridge ──► Adapter (physics, ML-Agents)
            ▲    \                                    ▲
 discovered │     composed by                         │ motor intent (Command)
            │      \                                  │
   Plugin discovery  Specification          Systems pipeline ──publishes──► Observer ──► Memento
            │                                          ▲
            └──── validated by Chain of Responsibility ┘   all wired through Context Objects
```

## Rules that keep the patterns honest

1. **A decorator never changes the request/response types**: it can only
   transform, cache, delay, reroute or record. If you need new data, add a
   field to the request (framework change, discussed in review).
2. **A strategy is stateless or owns only its own state.** Per-agent state
   belongs in components; per-run state in a system.
3. **Components don't find each other at runtime by search.** `AgentRoot`
   caches the lists at spawn; components reach siblings through `Root`.
4. **Assets are read-only at runtime.** The runner clones the profile and
   species; nothing writes into an asset during Play (a classic Unity trap:
   ScriptableObject edits made in Play mode persist).
5. **Observers are read-only.** Anything that changes the world is a system
   (visible in the tick order).
6. **Every extension point has a default** (Null Object), so an empty
   slot is never a crash.

## Anti-patterns deliberately avoided

| Anti-pattern | Why it hurts students | Replaced by |
|---|---|---|
| `GameManager` singleton that does everything | One 2 000-line file; hidden coupling | `Simulation` + systems + contexts |
| `class Herbivore : Animal`, `class Predator : Animal` | Deep hierarchies; a new trait means a new subclass | Composition + `SpeciesDefinition` |
| `enum ActionType` + `switch` | Every new action edits the enum, the switch, the prompt, the schema | Action components; `ActionSet` derived from them |
| Boolean flags (`if (shuffled)`, `if (useCache)`) | Combinatorial branches, untestable | Decorators and modifiers |
| `Update()` on every agent | Frame-rate coupling, non-deterministic order | Systems in phases, lockstep |
| `UnityEvent` wiring for core logic | Invisible in code; order unclear | `EventBus` for observers only; systems for logic |
| `FindObjectOfType`, static state | Hidden dependencies; two simulations can't coexist | Context objects |
| Fixed `Observation` struct | Adding a sense edits five files | Open field list + `FieldSpec` |

## Why not ECS/DOTS, behaviour trees or GOAP?

- **ECS/DOTS**: optimises for 100 000 entities; we have 20–60 agents and a
  model call that costs a million times more than a component update.
  Readability wins.
- **Behaviour trees / GOAP** answer "which behaviour now?", which is
  exactly the job we give the evolved genome + model. They are welcome
  *inside* an action (pattern 22) or as a terminal backend to compare
  against (`BehaviourTreeBackend` is a good student project).
