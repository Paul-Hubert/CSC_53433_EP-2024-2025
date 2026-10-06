# 04 — Data assets (ScriptableObjects)

Everything that is **content or a shared setting** is an asset. Assets
make the *kinds* of things (species, loci, experiment conditions) data
instead of code (Type Object pattern), and they are what the validators
inspect.

## Asset graph

```text
ExperimentDefinition ── baseProfile ──► SimulationProfile
   │ seeds[], ticks                        │ simulationSeed, worldSeed
   └ conditions[] (C1…C7)                  ├ world ───────────► WorldDefinition (Grid | Terrain)
       └ modifiers[] [SerializeReference]  ├ relations ───────► SpeciesRelations
                                           ├ species[] ───────► SpeciesDefinition ×N
                                           │                      ├ prefab ────────► AgentRoot prefab (components)
                                           │                      ├ genome ────────► GenomeSchema ──► LocusDefinition ×N
                                           │                      ├ founders ──────► FounderPool (alleles per locus)
                                           │                      ├ prompt ────────► PromptTemplate
                                           │                      ├ renderer ──────► [SR] TerseRenderer | PhraseRenderer ─► PhraseBook
                                           │                      ├ decision ──────► [SR] decorator chain → backend
                                           │                      └ evolution ─────► [SR] canMate, crossover, mutation, development, population
                                           ├ systems[] [SerializeReference] (tick pipeline)
                                           └ sinks[]   [SerializeReference] (recording)
[SR] = [SerializeReference] plain C# object chosen from a dropdown (see 08 and 09).
```

## SimulationProfile — the root of a run

```csharp
[CreateAssetMenu(menuName = "Genetic Agents/Simulation Profile", order = 0)]
public sealed class SimulationProfile : ScriptableObject, IValidatable
{
    [Header("Reproducibility")]
    public int simulationSeed = 1234;            // agents, sampling, mutation, actions, food
    public int worldSeed = 1234;                 // terrain + initial food (same world, different runs)

    [Header("World & species")]
    public WorldDefinition world;
    public List<SpeciesDefinition> species = new();
    public SpeciesRelations relations;

    [Header("Tick pipeline (order shown in the inspector by phase)")]
    [SerializeReference, SubclassSelector] public List<ISimulationSystem> systems = DefaultSystems.Create();

    [Header("Recording")]
    [Min(1)] public int statsEveryTicks = 100;
    [SerializeReference, SubclassSelector] public List<IRunSink> sinks = new() { new JsonlEventSink(), new CsvStatsSink() };

    public void Validate(ValidationReport r) { /* local checks: species non-empty, seeds set, … */ }
}
```

## SpeciesDefinition — one kind of creature

```csharp
[CreateAssetMenu(menuName = "Genetic Agents/Species", order = 1)]
public sealed class SpeciesDefinition : ScriptableObject, IValidatable
{
    public string displayName = "Herbivore";
    public AgentRoot prefab;
    [Tooltip("Species with lower values act first in a tick (prototype: animals, then predators).")]
    public int actOrder;

    [Header("Genetics")]
    public GenomeSchema genome;
    public FounderPool founders;

    [Header("Brain")]
    [Min(1)] public int decisionPeriodTicks = 4;
    public PromptTemplate prompt;
    [SerializeReference, SubclassSelector] public IObservationRenderer renderer = new TerseRenderer();
    [SerializeReference, SubclassSelector] public IDecisionBackend decision = new RuleBasedBackend();
    [Tooltip("Runs when the chosen action is not available (counted as invalid).")]
    public string fallbackActionId = "wander";

    [Header("Evolution")]
    [SerializeReference, SubclassSelector] public IAgentCondition canMate = Conditions.DefaultMating();
    [SerializeReference, SubclassSelector] public ICrossover crossover = new UniformCrossover();
    public MutationPipeline mutation = MutationPipeline.PrototypeDefaults();
    [SerializeReference, SubclassSelector] public IDevelopment development = new NoDevelopment();
    [SerializeReference, SubclassSelector] public IPopulationPolicy population = new FloorAndCapPolicy();
}
```

A species bundles **body** (prefab), **genetics** (schema + founders),
**brain** (renderer + decision chain + prompt) and **evolutionary rules**.
The scripted predator of the prototype is just another species: empty
genome schema, `ScriptedChaseBackend`, `canMate = Never`, a fixed
population policy. Making predators evolve is then a data change
([11](11-change-scenarios.md#1-predators-that-evolve-co-evolution)).

## GenomeSchema and LocusDefinition — homologous loci as data

```csharp
public enum LocusKind { Action, Temperament, Free, Trait }

[CreateAssetMenu(menuName = "Genetic Agents/Locus")]
public sealed class LocusDefinition : ScriptableObject
{
    public string id = "eat";                    // stable key used in logs, caches, prompts
    public LocusKind kind = LocusKind.Action;
    [TextArea] public string role = "When to eat."; // shown in the founder editor and to LLM drafting
    [Min(1)] public int maxWords = 12;           // prototype: 12 action, 15 temperament
    public bool mutable = true;                  // freeze a locus for an experiment
    public Vector2 traitRange = new(0, 1);       // Trait loci only (numeric genes)
}

[CreateAssetMenu(menuName = "Genetic Agents/Genome Schema")]
public sealed class GenomeSchema : ScriptableObject
{
    public List<LocusDefinition> loci = new();   // ORDER = gene order in the genome and in the prompt
}
```

| Kind | Meaning | Default loci | Who reads it |
|---|---|---|---|
| `Action` | When to do the linked `AgentAction` | eat, flee, follow, wander, rest, mate, attack | prompt `{genes}`; rule-based gene weights |
| `Temperament` | General disposition | risk, social, place | prompt `{temperament}`; rule-based temperament deltas |
| `Free` | No assigned role (open-ended evolution) | — | prompt `{free}` if present |
| `Trait` | Numeric gene (speed, vision, size) | — | `IDevelopment` → component parameters |

The schema is the **contract** between components and genes: every
`AgentAction` on the prefab must point to an `Action` locus in the schema
and vice versa (rule `LociMatchActionsRule`).

## FounderPool — the common origin

```csharp
[CreateAssetMenu(menuName = "Genetic Agents/Founder Pool")]
public sealed class FounderPool : ScriptableObject
{
    public string version = "v1";
    [Tooltip("Frozen pools are read-only: change = new version, so all runs share an origin.")]
    public bool frozen;
    public List<LocusAlleles> loci = new();
}

[Serializable]
public sealed class LocusAlleles
{
    public LocusDefinition locus;
    [GeneText] public string neutral = "No preference.";
    [GeneText] public List<string> alleles = new();
}
```

- Imports and exports the prototype's `data/founder_pool_v1.json` format,
  so Python and Unity runs start from byte-identical genes.
- Sibling assets with the same shape: `ContrastSet` (pro/anti allele per
  action locus, for directed tests) and `ControlAlleles` (random-text
  genes, for control C4 and gibberish tests).
- `[GeneText]` gives a word-count bar and warnings in the inspector
  ([09](09-editor-tooling.md)).

## PromptTemplate and PhraseBook — wording is content

```text
PromptTemplate (TextArea)                         placeholders filled by OllamaBackend
──────────────────────────────────────────────    ───────────────────────────────────────────
You decide what a wild animal does next …         {actions}     built from the prefab's actions:
Actions:                                                        "- eat: go to the nearest visible food …"
{actions}                                         {genes}       one line per Action locus
This animal's instincts (its genes) …             {temperament} Temperament loci, quoted
{genes}                                           {situation}   the rendered observation
Temperament: {temperament}                        {ask}         mode-specific instruction (points/word)
Situation: {situation}
{ask}
```

Because `{actions}` is generated from `AgentAction.PromptDescription`,
adding a `DrinkAction` component updates the prompt automatically. The
template's text hash is part of every cache key, so editing wording never
returns stale answers.

`PhraseBook` maps `(field, value)` → sentence for the first-person
renderer (prototype style V2): `("food","near") → "Food is close."`. A
validator checks that every value declared by every sense's `FieldSpec`
has a phrase.

## SpeciesRelations

A small matrix asset: for each pair of species, flags `Prey`, `Predator`,
`Mate`, `Competitor`. Senses ask it instead of hard-coding tags:
`PredatorSense` looks for the nearest agent whose species is a `Predator`
of mine. Adding a third species is filling one row and one column.

## WorldDefinition

An abstract asset with one method, `IWorld Create(WorldBindings scene, IRandom rng)`:

| Subclass | Fields (prototype `world:` section) | Creates |
|---|---|---|
| `GridWorldDefinition` | width, height, water/mountain fraction, noise octaves, min walkable connected, food initial fraction, regrow p, water bonus ×/radius | `GridWorld` (headless parity) |
| `TerrainWorldDefinition` | water level, max walkable slope, food detail layer, regrow p per biome, cell size | `TerrainWorld` over the scene's `CustomTerrain` (bound via `WorldBindings` on the runner) |

## ExperimentDefinition — conditions as stacks of modifiers

```csharp
[CreateAssetMenu(menuName = "Genetic Agents/Experiment")]
public sealed class ExperimentDefinition : ScriptableObject
{
    public SimulationProfile baseProfile;
    [Min(1)] public int ticks = 20000;
    public List<int> seeds = new() { 1, 2, 3 };
    public List<ExperimentCondition> conditions = new();
}

[Serializable]
public sealed class ExperimentCondition
{
    public string id = "C2_NoMut";
    [TextArea] public string description;
    [SerializeReference, SubclassSelector] public List<IProfileModifier> modifiers = new();
}

public interface IProfileModifier { void Apply(SimulationProfile runtimeClone); }
```

The runner deep-clones the profile (and the species it references), then
applies the condition's modifiers. Base profile assets are never mutated.

| Condition (plan 08) | Modifiers |
|---|---|
| C1 FULL | *(none)* |
| C2 NO-MUT | `SetMutationRate(0)` |
| C3 SHUFFLED | `WrapDecision(new GenomeShuffleDecorator(), outermost: true)` |
| C4 RANDOM-FOUNDERS | `UseFounderPool(controlAllelesPool)` |
| C5 RULE-BASED | `ReplaceTerminalBackend(new RuleBasedBackend())` |
| C7 ASEXUAL | `SetCrossover(new CloneParent())`, `SetMating(Conditions.Solo())` |

Conditions compose: "C3 on the rule-based brain" is just both modifier
lists. The Experiment Runner window and the CLI run
`conditions × seeds` and write one run folder each.

## Where every prototype setting lives

| `configs/base.yaml` section | Unity home |
|---|---|
| `seed` | `SimulationProfile.simulationSeed` / `worldSeed` |
| `world.*` | `GridWorldDefinition` (or `TerrainWorldDefinition`) |
| `predators.*` | Predator species prefab (`count` → its `FixedCountPolicy`), `PredationSystem` (`kill_p`, rest after kill), `ScriptedChaseBackend` (`chase_radius`, `turn_p`) |
| `agents.vision/near/energy_low/high` | sense components on the prefab |
| `agents.energy_*`, `cost_*`, `maturity`, `max_age` | `Metabolism` on the prefab |
| `agents.eat_gain`, `attack_*` | `EatAction`, `AttackAction` fields |
| `agents.mate_energy`, `child_energy` | species `canMate` condition, `BirthSystem` |
| `agents.init_pop/floor/cap` | species `FloorAndCapPolicy` |
| `sim.decision_period` | `SpeciesDefinition.decisionPeriodTicks` |
| `sim.sampling_temperature` | `TemperatureDecorator` in the decision chain |
| `sim.stats_every` | `SimulationProfile.statsEveryTicks` |
| `evolution.*` | species `crossover`, `mutation` (operators, weights, guards), modifiers for controls |
| `backend`, `policy`, `ollama` | species `decision` chain (`OllamaBackend` fields) + `OllamaSettings` (host, per user) |
| `paths.*` | runner / CLI output folder, `FileResponseCache` folder |
