# 06 — Decisions

From world to action: senses build an **open** observation, a renderer
turns it into text, a **chain of decorators** around a backend returns a
probability per action, and the scheduler samples one.

```text
Senses ──► ObservationBuilder ──► Observation (fields, canonical key)
                                        │
                     ┌──────────────────┴──────────────────┐
                     ▼                                     ▼
          IObservationRenderer (text)           ObservationEncoder (one-hot floats)
                     │                                     │
                     └──────► DecisionRequest ◄────────────┘
                                   │  agent id, genome, genes, phenotype, observation, action set
                                   ▼
        DecisionScheduler ──► species.decision : IDecisionBackend  (decorator chain)
                                   │
                                   ▼
                           ActionDistribution[]  ──► sample with Rng("sampling") ──► Brain.Current
```

## Open observation model

The prototype's `Observation` is a fixed dataclass: adding a field means
editing perception, both text styles, the rule-based backend and the
tests. In Unity the observation is an **ordered list of labelled fields**
contributed by whatever senses the prefab has.

```csharp
public readonly struct ObservationField
{
    public readonly string Key;     // "food"
    public readonly string Value;   // "near"
}

public sealed class ObservationBuilder
{
    public void Add(string key, string value);                   // one field
    public void Add(string key, bool value) => Add(key, value ? "yes" : "no");
    public Observation Build();                                  // ordered by the species' FieldOrder
}

public sealed class Observation : IEquatable<Observation>
{
    public IReadOnlyList<ObservationField> Fields { get; }
    public string CanonicalKey { get; }                          // "energy=low|food=near|predator=none|…"
    public string this[string key] { get; }                      // null when the field is absent
    public bool Has(string key, string value);
}
```

- **Closed vocabularies.** Each sense declares `FieldSpec(key, values…)`.
  The editor multiplies them to show the **observation-space size** (the
  prototype's ≈ few hundred situations) and warns when a new sense would
  blow up cache hit rates.
- **Field order** is part of the species (default: the order of sense
  components), so text is stable and cache keys don't change when a
  component is moved in the hierarchy.
- **Numeric backends** use `ObservationEncoder`: each field becomes a
  one-hot block from its `FieldSpec`. A neural-network or DRL policy reads
  the same observation as the LLM — a fair comparison.

## Renderers

```csharp
public interface IObservationRenderer { string Render(Observation o, SpeciesDefinition species); }
```

| Renderer | Output (same observation) | Prototype |
|---|---|---|
| `TerseRenderer` | `Energy: low. Food: near. Predator: none. Animal: near, ready to mate, weaker. Age: adult.` | V1 |
| `PhraseRenderer` + `PhraseBook` | `I am hungry and weak. Food is close. No predator in sight. Another animal is next to me. …` | V2 |
| `JsonRenderer` | `{"energy":"low","food":"near",…}` | — |

Changing wording is editing a `PhraseBook` asset, which is itself a lab
exercise ("how does observation wording change evolved behaviour?").

## Requests and results

```csharp
public sealed class DecisionRequest
{
    public int AgentId { get; }
    public GenomeKey GenomeKey { get; }                         // hash of gene texts (cache key part)
    public IReadOnlyDictionary<string, string> Genes { get; }  // locus id → text
    public Phenotype Phenotype { get; }                         // option B / trait loci; may be empty
    public Observation Observation { get; }
    public ActionSet Actions { get; }                           // ordered action ids + prompt lines
}

public sealed class ActionDistribution
{
    public ActionSet Actions { get; }
    public IReadOnlyList<float> Probabilities { get; }          // sums to 1, same order as Actions
    public DecisionSource Source { get; }                       // Model, Cache, Memo, Fallback…
    /// Reserved seam: extra named distributions ("direction", "speed"). Empty by default;
    /// backends that can't answer them, and actions that don't read them, are unaffected.
    public IReadOnlyDictionary<string, IReadOnlyList<float>> Heads { get; }
}
```

`ActionSet` comes from the prefab's `AgentAction` components in genome
schema order, so a distribution is always aligned with what the agent can
actually do.

## The backend contract

```csharp
public interface IDecisionBackend
{
    string Name { get; }
    /// One distribution per request, in request order. Never throw for a bad answer:
    /// return a valid distribution and mark its Source; throw only for cancellation.
    Task<ActionDistribution[]> DecideAsync(IReadOnlyList<DecisionRequest> batch,
                                           DecisionContext ctx, CancellationToken ct);
}
```

`DecisionContext` carries the species, its renderer and prompt, the tick,
the living population (for controls) and the `development` RNG stream.

### Terminal backends (they answer)

| Backend | Assembly | Answers from | Notes |
|---|---|---|---|
| `RandomBackend` | Runtime | uniform | control, smoke tests |
| `RuleBasedBackend` | Runtime | default logits + keyword reading of genes | port of `rule_based.py`; keyword tables are a `RuleBook` asset students can edit |
| `OllamaBackend` | LLM | local or cloud Ollama | modes `Points`, `Logprobs`, `Table`, `KSample` (prototype `ollama_policy.py`) |
| `UtilityBackend` | Runtime | `Phenotype` utilities | option B — see below |
| `NeuralNetBackend` | Runtime | `ObservationEncoder` + numeric genes | the old lab's MLP, for comparison |
| `OnnxBackend` | Runtime | Unity Inference Engine | a distilled model (parked Laya path) on-device |
| `ScriptedChaseBackend` | Runtime | hand-written rule | prototype predators |

### Decorators (they wrap)

```csharp
[Serializable]
public abstract class DecisionDecorator : IDecisionBackend
{
    [SerializeReference, SubclassSelector] protected IDecisionBackend inner;
    public virtual string Name => $"{GetType().Name} → {inner?.Name}";
    public abstract Task<ActionDistribution[]> DecideAsync(IReadOnlyList<DecisionRequest> batch,
                                                           DecisionContext ctx, CancellationToken ct);
}
```

| Decorator | Effect | Prototype equivalent |
|---|---|---|
| `GenomeShuffleDecorator` | each request uses a random *other* living agent's genome | control C3 `evolution.shuffled` |
| `TemperatureDecorator` | p ← p^(1/τ), renormalised | `sim.sampling_temperature` |
| `MemoDecorator` | in-run memo: dedupe the batch, reuse (genome key, observation key) | `Simulation.memo` |
| `PersistentCacheDecorator` | on-disk cache across runs, key = backend id + model digest + prompt hash + renderer + genome key + observation text | `policy.sqlite` |
| `FallbackDecorator` | timeout or failure → secondary backend, logged; results marked `Source = Fallback` (caches skip them, so the question is asked again later) | uniform fallback / `strict` |
| `AvailabilityMaskDecorator` | zero out actions whose `IsAvailable` is false | *(new; experiment: does masking change evolution?)* |
| `TraceDecorator` | records prompt, raw answer, timings, cache path → `Brain.LastTrace` | *(new; for the Brain Debugger)* |
| `RateLimitDecorator` | max concurrent requests (`workers`) | `policy.workers` |

### Default chain (as shown in the inspector)

```text
Herbivore ▸ Decision
  TraceDecorator
   └ TemperatureDecorator            τ = 1.0
      └ MemoDecorator
         └ PersistentCacheDecorator  folder: Library/GeneticAgents/Cache
            └ FallbackDecorator      timeout 30 s
               ├ primary:  RateLimitDecorator (2) → OllamaBackend  model: <tag> · mode: Points
               └ fallback: RuleBasedBackend
```

Order matters and is visible: the shuffle control must sit **outside**
the memo (otherwise memo keys use the agent's own genome), so
`GenomeShuffleDecorator` is inserted outermost and a validation rule
checks it. Students learn caching, fallbacks and controls by moving boxes,
not by reading flags.

## OllamaBackend

```csharp
[Serializable, DisplayName("LLM / Ollama")]
public sealed class OllamaBackend : IDecisionBackend
{
    public string model;                       // local tag or cloud model tag
    public OllamaMode mode = OllamaMode.Points; // Points | Logprobs | Table | KSample
    [Min(1)] public int tableSize = 8;         // Table mode (+ prefetch of frequent situations)
    [Min(1)] public int kSamples = 8;          // KSample mode
    public string keepAlive = "30m";
    // Host and API key come from OllamaSettings (UserSettings/, per student) and the
    // environment variable named there. Nothing secret is serialised in the asset.
}
```

- Prompt = species `PromptTemplate` with `{actions}`, `{genes}`,
  `{temperament}`, `{situation}`, `{ask}` filled in.
- Structured output with a JSON schema built from the `ActionSet`
  (points mode: integer per action), so a new action changes the schema
  automatically.
- HTTP via `UnityWebRequest` (main thread, awaitable) or `HttpClient`;
  results are applied in request order regardless of completion order.

## Option A and option B behind the same interfaces

```text
Option A — LLM as brain (per decision)         Option B — LLM as development (at birth)
  genome text ─► prompt each decision            genome text ─► LlmDevelopment (once, at birth)
  OllamaBackend in the decision chain                           └► Phenotype { utilities, thresholds }
  cost: births × decisions (cached)              UtilityBackend reads the phenotype every decision
                                                 cost: one call per birth; runs on any laptop
```

Switching is two dropdowns on the species: `development` and the terminal
`decision` backend. Comparing A and B on the same world and founder pool is
a graded exercise ([doc 02](../redesign/02-assessment.md#the-design-decision-i-would-push-hardest-on)).

## The scheduler

```csharp
public sealed class DecisionScheduler
{
    public Task<ActionDistribution[]> DecideAsync(SpeciesDefinition species,
        IReadOnlyList<DecisionRequest> requests, CancellationToken ct);
    public DecisionStats Stats { get; }   // decisions, model queries, memo/cache hits, fallbacks, seconds
}
```

It owns one chain instance per species (deserialised from the runtime
profile clone), builds the `DecisionContext`, and exposes the counters the
prototype reports in `summary.json` (`decisions`, `backend_queries`,
`memo_hit_rate`, `backend_s`, `invalid_rate`). Sampling stays in
`DecisionSystem`, in one place, with one RNG stream.
