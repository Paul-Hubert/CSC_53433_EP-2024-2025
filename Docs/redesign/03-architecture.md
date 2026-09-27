# 03 — Proposed architecture

Goal: small core, every behaviour-relevant piece behind an interface a
student can re-implement in one file.

## Layers

```
┌──────────────────────── Simulation (fixed-step, seeded) ───────────────────────┐
│  World: CustomTerrain façade → heightmap, walkability, water, food/foliage     │
│                                                                                 │
│  Agent                                                                          │
│   ├─ Genome            (List<Gene>, loci, lineage ids)                          │
│   ├─ Perception        world → Observation (struct) → text                      │
│   ├─ DecisionBackend   (Genome, Observation) → ActionDistribution  [async]      │
│   ├─ ActionExecutor    Action → steering over N ticks (terrain-aware)           │
│   └─ Metabolism        energy, age, cooldowns                                   │
│                                                                                 │
│  Population / Evolution                                                         │
│   ├─ ReproductionRule  who mates, when, cost                                    │
│   ├─ Crossover         (Genome, Genome) → Genome                                │
│   ├─ MutationOperators Gene → Gene   (LLM rewrite, word swap, negate, …)        │
│   └─ Floor / respawn   from founder pool                                        │
│                                                                                 │
│  Experiment: seeds, config asset, logger (CSV/JSONL), genome export, replay     │
└─────────────────────────────────────────────────────────────────────────────────┘
          │ batched requests                         ▲ typed responses
          ▼                                          │
  Inference service (local): llama.cpp server / Ollama / in-engine ONNX / mock
```

## Key interfaces (C# sketch)

```csharp
public enum ActionType { Wander, MoveToFood, Eat, Flee, Follow, Attack, Mate, Rest, LookLeft, LookRight }

public struct Observation {           // structured first, text second
    public float Energy01, Age01;
    public Target NearestFood, NearestAgent, NearestThreat;   // dir, dist, kind
    public TerrainHint Ahead;         // Walkable / Water / Steep
}

public interface IPerception      { Observation Sense(Agent self, World w); }
public interface IObservationText { string Render(Observation o); }       // students tweak wording
public interface IDecisionBackend {                                      // NN, LLM, classifier, rules, "development"
    Task<ActionDistribution> Decide(Genome g, Observation o, CancellationToken ct);
}
public interface ICrossover       { Genome Cross(Genome a, Genome b, IRandom rng); }
public interface IMutationOperator{ Task<Gene> Mutate(Gene g, IRandom rng); }
public interface IReproductionRule{ bool CanMate(Agent a, Agent b); }
```

`IDecisionBackend` implementations planned:

| Backend | Purpose |
|---|---|
| `RuleBasedBackend` | Keyword heuristics over gene text. No model. Default for dev/CI and no-GPU students. |
| `NeuralNetBackend` | Wraps the existing `SimpleNeuralNet` (numeric genome) — continuity + comparison. |
| `LlmPerDecisionBackend` | Option **A**: genome as cached prefix, observation suffix, constrained enum output. |
| `DevelopmentBackend` | Option **B**: one call at birth → typed `Phenotype` (utilities/thresholds/BT JSON), then cheap local evaluation. |

## Simulation loop

- **Fixed timestep, sim-time based** (not per-frame). Rendering is decoupled.
- Each agent has a *decision period* (e.g. 1 s sim time). When due, its
  request is queued; the step does not advance until all due decisions are
  answered (**lockstep** ⇒ deterministic given seeds + temperature/sampler seed).
- A **batcher** sends all due requests together to the inference service.
- A **response cache** keyed by `(genomeHash, observationText, backendVersion)`
  avoids recomputing identical situations (frequent with discretised
  observations).

## Observation design

- Discretise: distances as *near/mid/far*, directions as *ahead/left/right/
  behind*, energy as *low/medium/high*. Short, stable phrasing → better
  cache hit rate and less prompt noise.
- The text renderer is its own class so students can study *how
  observation wording changes evolved behaviour*.

## Data & tooling

- `SimulationConfig` ScriptableObject: population, food growth, rates,
  backend choice, seeds.
- Logger: per-tick population stats (CSV), births/deaths/mutations (JSONL),
  periodic genome dumps.
- **Genome browser** (editor window or simple web page reading the JSONL):
  lineages, allele frequencies per locus over time, top surviving genes.
- Headless mode (batchmode / no rendering) for long runs.
