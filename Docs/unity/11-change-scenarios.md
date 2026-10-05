# 11 — Change scenarios

How the architecture absorbs **big** changes. Each scenario names the
seams it touches, what stays untouched, and what to watch out for. If a
change ever needs edits across many seams, the design has a bug.

## The seams

| Seam | Contract | Doc |
|---|---|---|
| S1 Capabilities | `AgentSense`, `AgentAction`, `AgentComponent` hooks | [03](03-agent-anatomy.md) |
| S2 Content | `SpeciesDefinition`, `GenomeSchema`, `FounderPool`, `PromptTemplate`, `PhraseBook`, `SpeciesRelations` | [04](04-data-assets.md) |
| S3 Decision chain | `IDecisionBackend` + decorators, `IObservationRenderer` | [06](06-decisions.md) |
| S4 Evolution rules | `IAgentCondition`, `ICrossover`, `IMutationOperator`, `IGeneGuard`, `IDevelopment`, `IPopulationPolicy` | [07](07-evolution.md) |
| S5 Tick pipeline | `ISimulationSystem` list | [05](05-simulation-loop.md) |
| S6 Body & world | `AgentLocomotion`, `IWorld` | [03](03-agent-anatomy.md), [02](02-architecture-overview.md#key-interfaces-at-a-glance) |
| S7 Tooling | validation rules, windows, gizmos | [09](09-editor-tooling.md) |

## Impact matrix

● = add or change here · ○ = optional · blank = untouched

| # | Scenario | S1 | S2 | S3 | S4 | S5 | S6 | S7 | Framework edit |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Predators that evolve (co-evolution) | ○ | ● | ● | ● | | | ○ | none |
| 2 | Continuous terrain from the terrain/foliage labs | | ● | | | | ● | ○ | none |
| 3 | Physics bodies driven by a DRL controller | ○ | | | | ● | ● | ○ | none |
| 4 | VR interaction with the animals | ● | ● | | | ● | ○ | ○ | none |
| 5 | Option B: LLM at birth | | | ● | ● | | | | none |
| 6 | Numeric genomes / neural-network brain | | ● | ● | ● | | | | none |
| 7 | Agents that communicate | ● | ● | | | ○ | ○ | | none |
| 8 | Explicit-fitness generational GA | | | | ○ | ● | | | none |
| 9 | Distilled on-device model (ONNX) | | | ● | | | | | none |
| 10 | Memory within a lifetime | ● | ○ | ○ | | | | | none |
| 11 | Several decision heads (direction, speed) | ● | | ● | | | | | none (reserved seam) |
| 12 | Another LLM provider | | | ● | ○ | | | | none |

## 1. Predators that evolve (co-evolution)

**Today:** predators are a species with an empty genome, `ScriptedChaseBackend`,
`canMate = Never` and a `FixedCountPolicy`.

**Change:** give the Predator species a `GenomeSchema` (loci *hunt*,
*rest*, *wander*, *patience*), a `FounderPool`, the same Ollama chain with
its own `PromptTemplate` ("You decide what a predator does next…"), and a
`FloorAndCapPolicy`. Add `HuntAction` (locus *hunt*) and a `PreySense`.
`SpeciesRelations` already says who eats whom; `PredatorSense` on the
herbivore keeps working unchanged.

**Untouched:** every herbivore file, all systems (`PredationSystem` reads
relations, not types), the recorder (events carry the species id).

**Watch:** two LLM-driven species double the call volume; give predators
a longer `decisionPeriodTicks`. The Genome Browser's species filter shows
the co-evolution of *flee* and *hunt* alleles side by side.

## 2. Continuous terrain from the terrain/foliage labs

**Change:** `TerrainWorldDefinition` + `TerrainWorld : IWorld` over the
`CustomTerrain` façade students built earlier in the course:

| `IWorld` query | Terrain implementation |
|---|---|
| `IsWalkable(p)` | height above water level and `getSteepness` below the max slope |
| `NearestFood(p, r)` | foliage detail layer cells (grass) or tree instances tagged as food |
| `TryConsumeFood(p)` | clears the detail cell; regrowth by `FoodField` per biome |
| `NearestWater(p, r)` | shoreline cells cached at start |
| `StepToward` / `StepAway` | used only by `GridLocomotion`; continuous bodies steer themselves |

Swap the prefab's `GridLocomotion` for `KinematicLocomotion`
(NavMesh or `CharacterController`), and set the profile's world asset.

**Untouched:** genome, prompts, senses (they ask `IWorld`, and their
outputs are still the discrete buckets), decision chain, evolution.

**Watch:** distances are metres, so `near`/`vision` need re-tuning; the
validator `LocomotionMatchesWorldRule` catches a forgotten locomotion swap.
Seeded terrain generation keeps runs reproducible.

## 3. Physics-based bodies driven by a DRL controller

The genome decides **what** to do at ~1 Hz; a deep-RL policy decides
**how** an articulated body does it at physics rate. This is hierarchical
control, and the Bridge between `AgentAction` and `AgentLocomotion` is
exactly the place to put it.

```text
genes + observation ──LLM──► "flee"            (Decide phase, every N ticks, cached)
FleeAction.Tick ──► Locomotion.MoveAwayFrom(p)  (Act phase: sets a goal)
MLAgentsLocomotion: goal (velocity, heading, gait) in body frame
   └─ ONNX policy (ML-Agents, inference only) ─► joint targets on ArticulationBody drives
PhysicsStepSystem: k substeps of Physics.Simulate(dt); one policy decision per substep
Metabolism: + EffortThisTick = Σ|τ·ω|·dt × scale     ← real mechanical cost
```

- **Locomotion:** `MLAgentsLocomotion : AgentLocomotion` holds a reference
  to an ML-Agents `Agent` subclass on the body (`BehaviorType.InferenceOnly`).
  `MoveToward`/`MoveAwayFrom`/`Wander` only set the goal vector.
- **Lockstep with ML-Agents:** disable automatic stepping
  (`Academy.Instance.AutomaticSteppingEnabled = false`) and let
  `PhysicsStepSystem` call `RequestDecision()` then
  `Academy.Instance.EnvironmentStep()` and `Physics.Simulate(dt)` for each
  substep. Physics and the DRL policy become part of the tick, so runs
  stay reproducible on one machine (enable *Enhanced Determinism* in the
  physics settings; CPU inference).
- **Training** happens in a separate scene with the same component in
  training mode: goal-conditioned velocity tracking (or a motion-imitation
  reward) with randomised goals and terrain. The policy is frozen before
  evolution runs; evolution never touches its weights.
- **Energy from physics:** effort reported by the body feeds
  `Metabolism`, so genes that flee at every shadow now pay a real cost —
  selection pressure toward efficient behaviour emerges from mechanics.
- **New perception:** a `BalanceSense` (`footing = stable | slipping |
  fallen`) and an `IDeathCheck` or stun for falls make the body's limits
  visible to the genome ("Avoid steep ground when tired").
- **Optional:** a trait locus `gait` selects among several trained
  policies (walk-efficient vs sprint), so body style co-evolves with
  behaviour.

**Untouched:** genome, prompts, caches, actions' logic, evolution. The
same evolved genomes can be dropped onto grid tokens and onto ragdolls —
a common-garden test across bodies.

## 4. VR interaction with the animals

The player is not an agent; they are an **external source of commands**
recorded as data, so runs stay replayable.

- **Input as commands:** XR Interaction Toolkit callbacks enqueue
  `PlayerCommand`s (`PlaceFood(p)`, `Grab(agentId)`, `Release(agentId, v)`,
  `Scare(p)`). A `PlayerInputSystem` (World phase) applies the queue at a
  tick boundary and publishes each command as an event. Replaying the
  event log reproduces the session.
- **Being held:** an `XRGrabbableAgent` adapter component on the prefab
  implements `IActionOverride`; while held, `ActionSystem` runs the
  override instead of the chosen action. A `HeldSense` (`held = yes | no`)
  and a `StruggleAction` with locus *struggle* let the genome respond to
  humans ("Stay calm when held").
- **Physics interaction:** with `RigidbodyLocomotion` or scenario 3,
  thrown or pushed animals are just physics; `BalanceSense` notices.
- **Responsiveness:** in VR, waiting on a slow model would freeze the
  animals. `DecisionSystem.timing = Realtime` lets agents keep executing
  their current action while a decision is pending (the decision applies
  when it arrives). This trades bit-for-bit determinism for comfort; keep
  `Lockstep` for experiments.

## 5. Option B: LLM at birth

Two dropdowns on the species: `development = LlmDevelopment` (one cached
call per birth: genome → utilities and thresholds) and terminal
`decision = UtilityBackend` (evaluates the phenotype every decision, no
model call). Option A and B now differ by one asset, which makes the
A-vs-B comparison of [doc 02](../redesign/02-assessment.md) a configuration.

## 6. Numeric genomes / neural-network brain

Add a locus of kind `Trait` named *weights* whose allele carries
`Values` (the MLP weights). Use `GaussianOperator` for it, uniform
crossover per locus (or one locus per layer for layer-wise crossover), and
`NeuralNetBackend` reading `ObservationEncoder` one-hot input. The old
lab's network is now a baseline on the same world and observation as the
prompt genome. Text loci and numeric loci can coexist in one genome
(hybrid: words for drives, numbers for body traits).

## 7. Agents that communicate

`CallAction` (locus *call*) writes a signal from a **closed** vocabulary
(`alarm`, `food`, `mate`) into a `SignalField` on the world; a
`HearingSense` emits `heard = none | alarm | food | mate`. Genes such as
"Call when you see a predator" and "Flee when you hear an alarm" can
co-evolve. Free-text messages are possible (an `{heard}` prompt
placeholder) but break the closed vocabulary and the cache hit rate —
a good discussion point.

## 8. Explicit-fitness generational GA

Replace `MatingSystem`, `BirthSystem` and `ImmigrationSystem` with a
`GenerationalSystem`: every *G* ticks it scores agents with an `IFitness`
strategy (lifespan, food, offspring), selects parents with an
`ISelection` strategy (tournament, roulette, elitism), and refills the
population using the species' existing crossover and mutation pipeline.
Implicit vs explicit selection becomes a comparison of two system lists.

## 9. Distilled on-device model (ONNX)

The parked Laya/distillation path returns as `OnnxBackend` (Unity
Inference Engine). It is a terminal backend like any other: the cache,
memo, fallback and trace decorators wrap it unchanged, and the Gene
Playground measures whether it reads genes as well as the teacher LLM.

## 10. Memory within a lifetime

`EpisodicMemory : AgentComponent, IAgentTick` keeps the last few outcomes
(ate, was attacked, lost a mate). A `MemorySense` renders them as closed
fields (`last_meal = recent | long_ago`). Genes can then express learning
rules ("Avoid places where you were attacked"). Rich free-text memory goes
through an optional `{memory}` placeholder, at the cost of cache hits.

## 11. Several decision heads (direction, speed)

`ActionDistribution.Heads` is reserved for extra named distributions
(empty by default). A backend that can answer more questions fills
`Heads["direction"]`; actions that understand it read
`Root.Brain.LastDistribution.Heads`; everything else ignores it. No
existing backend or action changes.

## 12. Another LLM provider

Write one terminal backend (`OpenAICompatibleBackend`) reusing the shared
`PromptBuilder` and JSON-schema builder from the LLM assembly; keep all
decorators. For gene mutation, write the matching `IMutationOperator`.
Cache keys include the backend id and model name, so answers never mix.

## When the design should bend

If a request needs a **new field on `DecisionRequest`**, a **new phase**,
or a change to a **base class**, treat it as a framework change: discuss
it, version it, update the validators and templates, and add it to these
docs. Everything in the matrix above should stay "none".
