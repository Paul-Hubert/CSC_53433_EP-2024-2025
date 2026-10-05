# 02 — Architecture overview

Layers, assemblies, folders, the dependency rule, and how each Python
module maps to a Unity type.

## Layers

```text
┌───────────────────────────────────────────────────────────────────────────────┐
│ PRESENTATION   AgentView · AnimatorView · labels · UI     (read state, events) │
├───────────────────────────────────────────────────────────────────────────────┤
│ AUTHORING      SimulationProfile · SpeciesDefinition · FounderPool · prefabs  │
│ (assets)       PromptTemplate · PhraseBook · ExperimentDefinition             │
├───────────────────────────────────────────────────────────────────────────────┤
│ SIMULATION     SimulationRunner (MonoBehaviour, the only Update)               │
│                Simulation → systems[] by phase → agents' components            │
│                DecisionScheduler · Population · AgentFactory · RunRecorder     │
├───────────────────────────────────────────────────────────────────────────────┤
│ DOMAIN CORE    Genome · Allele · AlleleRegistry · Observation · Decision*      │
│ (plain C#)     RngStreams · EventBus · ValidationReport · interfaces           │
├───────────────────────────────────────────────────────────────────────────────┤
│ ADAPTERS       IWorld: GridWorld | TerrainWorld(CustomTerrain)                 │
│                ILocomotion: Grid | Kinematic | Rigidbody | ML-Agents           │
│                IDecisionBackend: Ollama HTTP | Rules | NN | ONNX …            │
└───────────────────────────────────────────────────────────────────────────────┘
EDITOR (separate assembly, never shipped): validation · inspectors · windows · gizmos
```

The **domain core** knows nothing about scenes. It can be unit-tested in
milliseconds and compared with the Python prototype. Adapters connect it to
Unity (terrain, physics) and to the outside (Ollama). Authoring assets
assemble the pieces; the simulation layer runs them.

## Assemblies and the dependency rule

```text
                       ┌────────────────────────────┐
                       │ GeneticAgents.Editor        │  Editor platform only
                       └──────┬───────────┬──────────┘
                              │           │
┌───────────────────┐   ┌─────▼─────────┐ │   ┌──────────────────────────┐
│ StudentWork       │──►│ GeneticAgents │ └──►│ GeneticAgents.Core        │
│ StudentWork.Editor│   │ .Runtime      │────►│ (no MonoBehaviour,        │
└─────────┬─────────┘   └───────────────┘     │  no scene access)         │
          │             ┌───────────────┐     └──────────▲───────────────┘
          └────────────►│ GeneticAgents │────────────────┘
                        │ .LLM (Ollama) │
                        └───────────────┘
Arrows = "references". Core references nothing of ours.
The framework NEVER references StudentWork: it discovers student types by interface.
```

| Assembly | Contains | May reference |
|---|---|---|
| `GeneticAgents.Core` | Genetics, observation, decision contracts, evolution interfaces, RNG, events, validation report, metrics | `UnityEngine.CoreModule` (math types only) |
| `GeneticAgents.Runtime` | Components, ScriptableObjects, systems, world/locomotion adapters, rule-based and random backends, decorators, recorder | Core |
| `GeneticAgents.LLM` | `OllamaClient`, `OllamaBackend`, `LlmRewriteOperator`, `LlmDevelopment`, response cache | Core |
| `GeneticAgents.Editor` | Validation rules & runner, inspectors, drawers, windows, overlays, gizmos, script templates, settings page | Core, Runtime, LLM |
| `StudentWork` / `StudentWork.Editor` | Everything a student writes | Core, Runtime, LLM (and Editor for `.Editor`) |
| `GeneticAgents.Tests.*` | EditMode / PlayMode tests, golden files | all of the above |

Why a separate `StudentWork` assembly: compile errors in a student's file
are clearly *theirs*, grading is a folder diff, and it proves the
framework is extensible without edits — if a feature needs a framework
change, that is a design bug to fix in the framework.

## Folder layout

```text
Assets/GeneticAgents/
  Core/                         GeneticAgents.Core.asmdef
    Genetics/      Allele.cs  AlleleId.cs  AlleleRegistry.cs  Genome.cs  GenomeKey.cs  LocusInfo.cs
    Perception/    Observation.cs  ObservationField.cs  ObservationBuilder.cs
    Decisions/     IDecisionBackend.cs  DecisionRequest.cs  DecisionContext.cs
                   ActionDistribution.cs  DecisionDecorator.cs  IObservationRenderer.cs
    Evolution/     ICrossover.cs  IMutationOperator.cs  IGeneGuard.cs  IAgentCondition.cs
                   IDevelopment.cs  Phenotype.cs  IPopulationPolicy.cs
    Simulation/    ISimulationSystem.cs  SimPhase.cs  SimContext.cs  EventBus.cs  SimEvents.cs
    Random/        RngStreams.cs  IRandom.cs
    Validation/    IValidatable.cs  ValidationReport.cs  ValidationIssue.cs
    Analysis/      Metrics.cs  (MI_G, MI_O, JSD, ΔP — ports of prototype/promptevo/metrics.py)
  Runtime/                      GeneticAgents.Runtime.asmdef
    Agents/        AgentRoot.cs  Metabolism.cs  Brain.cs
      Senses/      AgentSense.cs  EnergySense.cs  AgeSense.cs  FoodSense.cs  PredatorSense.cs  NeighbourSense.cs
      Actions/     AgentAction.cs  EatAction.cs  FleeAction.cs  FollowAction.cs  WanderAction.cs
                   RestAction.cs  MateAction.cs  AttackAction.cs
      Locomotion/  AgentLocomotion.cs  GridLocomotion.cs  KinematicLocomotion.cs  RigidbodyLocomotion.cs
      Views/       AgentView.cs  AnimatorView.cs
    Definitions/   SimulationProfile.cs  SpeciesDefinition.cs  GenomeSchema.cs  LocusDefinition.cs
                   FounderPool.cs  PromptTemplate.cs  PhraseBook.cs  SpeciesRelations.cs
                   WorldDefinition.cs  ExperimentDefinition.cs
    Decisions/     RandomBackend.cs  RuleBasedBackend.cs  UtilityBackend.cs  ScriptedChaseBackend.cs
      Decorators/  MemoDecorator.cs  PersistentCacheDecorator.cs  TemperatureDecorator.cs
                   FallbackDecorator.cs  GenomeShuffleDecorator.cs  TraceDecorator.cs
      Renderers/   TerseRenderer.cs  PhraseRenderer.cs
    Evolution/
      Crossover/   UniformCrossover.cs  OnePointCrossover.cs  CloneParent.cs
      Operators/   IntensityOperator.cs  NegateOperator.cs  ConditionSwapOperator.cs
                   SynonymOperator.cs  FounderReintroduceOperator.cs
      Guards/      CleanTextGuard.cs  MaxWordsGuard.cs  CharsetGuard.cs  ChangedGuard.cs
      Conditions/  IsAdult.cs  EnergyAbove.cs  ChoseAction.cs  All.cs  Any.cs  Not.cs
      MutationPipeline.cs  FloorAndCapPolicy.cs  NoDevelopment.cs
    Simulation/    SimulationRunner.cs  Simulation.cs  DecisionScheduler.cs  Population.cs  AgentFactory.cs
      Systems/     DecisionSystem.cs  ActionSystem.cs  MatingSystem.cs  PredationSystem.cs
                   BirthSystem.cs  DeathSystem.cs  ImmigrationSystem.cs  FoodRegrowthSystem.cs  StatsSystem.cs
    World/         IWorld.cs  GridWorld.cs  TerrainWorld.cs  WalkabilityMap.cs  FoodField.cs
    Recording/     RunRecorder.cs  IRunSink.cs  JsonlEventSink.cs  CsvStatsSink.cs
    Experiments/   IProfileModifier.cs  modifiers (SetMutationRate, WrapDecision, UseFounderPool, …)
  LLM/                          GeneticAgents.LLM.asmdef
    OllamaClient.cs  OllamaSettings.cs  OllamaBackend.cs  OllamaMode.cs
    LlmRewriteOperator.cs  LlmDevelopment.cs  FileResponseCache.cs
  Editor/                       GeneticAgents.Editor.asmdef
    Validation/  Inspectors/  Drawers/  Windows/  SceneView/  ScriptTemplates/  Settings/  Cli/
  Samples/
    Herbivore/  (prefab, SpeciesDefinition, GenomeSchema, loci, FounderPool v1)
    Predator/   (prefab, SpeciesDefinition with ScriptedChaseBackend)
    Profiles/   C1_Full  C2_NoMut  C3_Shuffled  C4_RandomFounders  C5_RuleBased  C7_Asexual
    Scenes/     GridArena.unity  TerrainValley.unity
  Tests/        EditMode/  PlayMode/  Golden/   (golden JSON exported from the Python prototype)
Assets/StudentWork/             StudentWork.asmdef  (+ Editor/StudentWork.Editor.asmdef)
```

## Runtime object graph (who owns whom)

```text
SimulationRunner (MonoBehaviour, in the scene)
 └─ Simulation                       created from a SimulationProfile at Play / batch start
     ├─ SimContext                   tick, RngStreams, IWorld, Population, EventBus, Recorder
     ├─ systems: List<ISimulationSystem>   sorted by (phase, order) — copied from the profile
     ├─ DecisionScheduler            per species: decision chain (IDecisionBackend), renderer
     ├─ Population                   living AgentRoot list, birth queue, AlleleRegistry
     │   └─ AgentFactory             SpeciesDefinition + Genome → pooled prefab instance
     └─ RunRecorder                  subscribes to EventBus → sinks (JSONL, CSV)
AgentRoot (on each agent prefab instance)
 ├─ Genome, Phenotype, Species, ids     set once by AgentFactory
 ├─ cached lists: AgentSense[], AgentAction[]  (GetComponents once at spawn)
 └─ Metabolism, Brain, AgentLocomotion, views
```

Only `SimulationRunner` has an `Update()`; it asks the `Simulation` to step
(see [05](05-simulation-loop.md)). `AgentRoot` and the capability components
are plain data + methods called by systems in a known order.

## Key interfaces at a glance

Every extension point, on one page. Each has a base class, a sample, a
script template and a validation hook (see [09](09-editor-tooling.md)).

```csharp
// ── Agent capabilities (components, Runtime) ─────────────────────────────── 03
abstract class AgentSense      : AgentComponent { IEnumerable<FieldSpec> Fields; void Sense(in SenseContext, ObservationBuilder); }
abstract class AgentAction     : AgentComponent { LocusDefinition Locus; bool IsAvailable(in ActionContext); void Tick(in ActionContext); }
abstract class AgentLocomotion : AgentComponent { Vector3 Position; void MoveToward(Vector3); void MoveAwayFrom(Vector3); void Wander(IRandom); void Stop(); }
interface IAgentTick         { void OnTick(SimContext ctx); }                 // optional component hooks
interface IDeathCheck        { DeathCause? CheckDeath(); }
interface IPhenotypeReceiver { void Apply(Phenotype p); }
interface IActionOverride    { bool IsActive { get; } void Tick(in ActionContext ctx); }

// ── Decisions (Core) ─────────────────────────────────────────────────────── 06
interface IObservationRenderer { string Render(Observation o, SpeciesDefinition species); }
interface IDecisionBackend     { string Name { get; }
                                 Task<ActionDistribution[]> DecideAsync(IReadOnlyList<DecisionRequest>, DecisionContext, CancellationToken); }
abstract class DecisionDecorator : IDecisionBackend { IDecisionBackend inner; }

// ── Evolution (Core) ─────────────────────────────────────────────────────── 07
interface IAgentCondition   { bool IsMet(AgentRoot a, SimContext ctx); string Describe(); }
interface ICrossover        { Genome Cross(Genome a, Genome b, GenomeSchema s, IRandom rng); }
interface IMutationOperator { string Id { get; } bool CanMutate(Allele a, LocusDefinition l); Task<string> MutateAsync(MutationRequest r, CancellationToken ct); }
interface IGeneGuard        { GuardResult Check(string candidate, Allele parent, LocusDefinition locus); }
interface IDevelopment      { Task<Phenotype> DevelopAsync(Genome g, AlleleRegistry a, SpeciesDefinition s, IRandom rng, CancellationToken ct); }
interface IPopulationPolicy { int InitialCount { get; } int Floor { get; } int Cap { get; } }

// ── Simulation & world (Core / Runtime) ──────────────────────────────────── 05
interface ISimulationSystem { SimPhase Phase { get; } int Order { get; } void Initialise(SimContext); ValueTask TickAsync(SimContext, CancellationToken); }
interface IWorld {
    bool IsWalkable(Vector3 p);
    FoodHit?  NearestFood(Vector3 p, float radius);   bool HasFoodAt(Vector3 p);   bool TryConsumeFood(Vector3 p);
    WaterHit? NearestWater(Vector3 p, float radius);  bool HasWater { get; }
    FoodField Food { get; }                            // regrowth state and multipliers
    Vector3 RandomWalkablePoint(IRandom rng);
}
interface IRunSink        { void Open(RunInfo run); void Write<T>(in T e) where T : struct, ISimEvent; void Close(); }
interface IProfileModifier { void Apply(SimulationProfile runtimeClone); }

// ── Tooling (Core contract, Editor rules) ────────────────────────────────── 09
interface IValidatable { void Validate(ValidationReport report); }
abstract class ValidationRule<T> { protected abstract void Check(T target, ValidationReport report); }
```

## Python prototype → Unity

| Prototype (`prototype/promptevo/…`) | Unity type(s) |
|---|---|
| `world.py` `World` | `GridWorld : IWorld` (parity); `TerrainWorld : IWorld` over `CustomTerrain` |
| `world.py` `Predator`, `move_predators` | `Predator` species + `ScriptedChaseBackend` + `PredationSystem` |
| `perception.py` `sense()`, `Observation` | `AgentSense` components → `ObservationBuilder` → `Observation` |
| `obs_text.py` V1 / V2 | `TerseRenderer` / `PhraseRenderer` + `PhraseBook` asset |
| `actions.py` `EXECUTORS` | `AgentAction` components (`EatAction`, …) |
| `sim.py` `Agent` | `AgentRoot` + `Metabolism` + `Brain` |
| `sim.py` `Simulation.step()` | `Simulation` + ordered `ISimulationSystem` list |
| `sim.py` `memo` | `MemoDecorator` |
| `genome.py` | `Core/Genetics` + `GenomeSchema` / `LocusDefinition` assets |
| `founder.py` `AllelePools` | `FounderPool` asset (+ contrast and control allele assets) |
| `evolution/mutation.py` `Mutator` | `MutationPipeline` = operators + guards |
| `backends/*.py` | `IDecisionBackend` implementations + decorators |
| `llm/ollama_client.py` | `OllamaClient` + `FileResponseCache` |
| `cache.py` `KVCache` | `FileResponseCache` (append-only JSONL, no native dependency) |
| `eventlog.py` | `RunRecorder` + `JsonlEventSink` / `CsvStatsSink` (same schema) |
| `rng.py` `Streams` | `RngStreams` (named substreams, hash(seed, name)) |
| `config.py` + YAML | `SimulationProfile` + `ExperimentDefinition` (+ JSON import/export for the CLI) |
| `metrics.py` | `Core/Analysis/Metrics` + the Gene Playground window |
| `experiments/e1_sensitivity.py` | Gene Playground "Sensitivity" tab + CLI command |
