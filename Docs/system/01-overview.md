# 01 — Overview

The whole system on one page: loop, data flow, layers, modules, what is verified.

## Core loop

```text
 sense ──► decide ──► act ──► reproduce / die ──► mutate (at birth) ──► sense …
   │         │          │            │                    │
   │         │          │            │                    └─ word operators or LLM rewrite of one gene
   │         │          │            └─ mate pairs breed; starvation, predators, old age remove agents
   │         │          └─ one tick of the chosen behaviour (eat / flee / follow / wander / rest / mate / attack)
   │         └─ backend returns p(action | genes, situation); action sampled (every D = 4 ticks)
   └─ discretised Observation (energy, food, predator, animal, age) → text
```

The simulation is **lockstep**: a tick does not advance until the backend has
answered, so a slow backend gives a slower run, not a different one.

## Data flow

```text
 configs/base.yaml ─(+ profile small|full, + overrides)─► Cfg
                                                          │
 data/founder_pool_v1.json ─► AllelePools ─► AlleleRegistry (id ↔ text, origin, parent)
 data/contrast_alleles_v1.json ┘   │
 data/control_alleles_v1.json ─────┘
                                   ▼
 ┌──────────────────────────── Simulation.step()  (promptevo/sim.py) ─────────────────────────┐
 │                                                                                             │
 │  World (terrain, food, predators) ──► sense() ──► Observation (frozen, hashable)            │
 │                                                     │                                       │
 │                       memo[(genome_key, obs)] hit? ─┤                                       │
 │                                  no ─► Query(genome_key, genes, obs) ─► Backend.decide() ───┼─► rule_based | random
 │                                                     │        probs [N, 7]                   │   llm  ─► Ollama (local / cloud)
 │                                                     ▼                                       │   laya (parked)
 │                    sample action (temperature τ, stream "sampling")                         │
 │                                                     ▼                                       │
 │                    EXECUTORS[action] for one tick, energy costs                             │
 │                                                     ▼                                       │
 │      _breed: crossover_uniform ─► Mutator.mutate ───────────────────────────────────────────┼─► Ollama mutator (optional)
 │                                                     ▼                                       │
 │      predators move ─► predation ─► age / starvation ─► food regrowth ─► floor immigrants   │
 └──────────────┬──────────────────────────────────────────────────────────────────────────────┘
                ▼
  run_info.json (provenance, at start) · EventLog: events.jsonl (running sha256), stats.csv  ·  finish(): alleles.jsonl, summary.json,
  final_population.json                                                  (results/runs/<name>/)
```

## Layers

| Layer | Holds | Main code | Page |
|---|---|---|---|
| **World** | 2D grid: grass / water / mountain, food, scripted predators, movement helpers | `prototype/promptevo/world.py` | [02](02-world.md) |
| **Agents** | `Agent` record, perception → `Observation` → text, 7 action executors, metabolism | `sim.py` (`Agent`), `perception.py`, `obs_text.py`, `actions.py` | [04](04-agents-perception-actions.md) |
| **Evolution** | Alleles, genomes, founder pool, crossover, mutation, reproduction, floor | `genome.py`, `founder.py`, `evolution/mutation.py`, `sim.py` (`_breed`, `_birth`) | [05](05-genome.md), [06](06-evolution.md) |
| **Decision service** | `Backend.decide(queries) → probs[N, 7]`; LLM client and caches | `backends/*.py`, `llm/ollama_client.py`, `cache.py` | [07](07-decision-backends.md) |
| **Experiment tooling** | Config, RNG streams, event log, metrics, E1 suite, gate, probes, helpers | `config.py`, `rng.py`, `eventlog.py`, `metrics.py`, `progress.py`, `experiments/*.py` | [08](08-reproducibility-and-data.md), [09](09-metrics-and-experiments.md) |

The simulation loop (`sim.py`, page [03](03-simulation-loop.md)) ties the layers
together. It knows the backend only through the protocol.

**Defaults to be aware of.** The *design* default brain is the LLM (doc 05 §0),
but `configs/base.yaml` ships with `backend.name: rule_based` and
`policy.model: null`, because no model has been chosen yet (STATUS › Next action,
step S1.3). Scripts therefore run the rule-based brain unless `--backend llm` is
given and `policy.model` is set.

## Module map

Paths relative to `prototype/`.

| Module | Responsibility | Status |
|---|---|---|
| `promptevo/config.py` | Load `base.yaml` + profile + extra files + overrides; `resolve()` paths | ✅ |
| `promptevo/rng.py` | Named, independent, seeded RNG streams | ✅ |
| `promptevo/cache.py` | sqlite key-value cache, `make_key()` | ✅ |
| `promptevo/progress.py` | `logs/<job>.progress.json` for background jobs | ✅ |
| `promptevo/eventlog.py` | `events.jsonl` with running sha256, `stats.csv` | ✅ |
| `promptevo/render.py` | Downsampled ASCII map for debugging | ✅ (used by the smoke run; no unit test) |
| `promptevo/world.py` | Terrain, walkability, food, predators, `step_toward`, `step_heading`, `cheb` | ✅ |
| `promptevo/perception.py` | `Observation`, `sense()`, `mate_ready()`, `RELEVANT_TAG` | ✅ |
| `promptevo/obs_text.py` | `render(obs, style)` for V1 / V2 | ✅ |
| `promptevo/actions.py` | 7 executors, invalid → wander | ✅ |
| `promptevo/genome.py` | `Allele`, `Genome`, `AlleleRegistry`, `crossover_uniform`, loci constants | ✅ |
| `promptevo/founder.py` | `AllelePools`: founder / neutral / contrast / control sets, genome builders | ✅ (founder texts are a DRAFT, H1) |
| `promptevo/evolution/mutation.py` | Word operators, LLM rewrite, guards, `Mutator` | ✅ word ops · 🧪 LLM rewrite |
| `promptevo/sim.py` | Lockstep loop, memo, breeding, predation, deaths, floor, logging | ✅ |
| `promptevo/backends/base.py` | `Query`, `Backend` protocol, `normalise()` | ✅ |
| `promptevo/backends/random_policy.py` | Uniform distribution | ✅ |
| `promptevo/backends/rule_based.py` | Keyword "ideal interpreter" of genes | ✅ |
| `promptevo/backends/ollama_policy.py` | LLM brain: points / logprobs / table / ksample, caches, workers | 🧪 |
| `promptevo/llm/ollama_client.py` | Stdlib Ollama client (local or cloud), retries, sqlite cache, rewriter | 🧪 |
| `promptevo/backends/factory.py` | `make_backend(name)`, `make_rewriter(cfg)` | ✅ / 🧪 (LLM paths) |
| `promptevo/backends/laya_backend.py` | Laya decision model, placements P1–P4 | 💤 |
| `promptevo/metrics.py` | MI_G, MI_O, JSD, ΔP, locality, Spearman, diversity, bootstrap | ✅ |
| `experiments/smoke_run.py` | One run with ASCII snapshots and a summary | ✅ (rule-based) · 🧪 (`--backend llm`) |
| `experiments/make_obs.py` | `data/observations_v1.jsonl` (48 observations, small) | ✅ |
| `experiments/e1_sensitivity.py` | E1 gene-sensitivity suite, any backend | ✅ (rule-based) · 🧪 (llm) |
| `experiments/teacher_gate.py` | Decision-model gate (G1/G2 on the LLM, per mode) | 🧪 |
| `experiments/e0_probe_ollama.py` | Probe: models, structured output, determinism, logprobs, timing | 🧪 (no test; must be run locally) |
| `experiments/status.py`, `peek.py` | Background-job status; look at data files briefly | ✅ |
| `experiments/make_mutants.py`, `make_dataset.py`, `label_teacher.py` | Distillation data pipeline | 💤 (fake-tested) |
| `experiments/e0_probe_laya.py`, `e0_budget.py` | Laya probe, token budget | 💤 (untested) |
| `e2_bench`, `run_matrix`, `common_garden`, `analyze`, `report`, `finetune_laya`, `e3_eval` | Planned in plan 08 | ☐ |
| `evolution/crossover.py`, `evolution/reproduction.py`, `configs/exp_*.yaml` | Planned in plan 08 §A3; crossover lives in `genome.py`, reproduction in `sim.py` | ☐ |

## Verified vs unverified

| Verified offline (tests or measured runs) | Unverified (needs real models) |
|---|---|
| World generation, connectivity, nobody enters blocked cells | Whether a real LLM reads the genes (G1/G2 on the LLM: `teacher_gate`, E1 `--backend llm`) |
| Executors: flee increases distance, eat adds energy, invalid → wander | Ollama `logprobs` response format (mode auto-falls back to `points`) |
| Rule-based directed tests; gibberish = neutral | Seconds per decision, real memo/cache hit rates, cost of a 20 k-tick run |
| Determinism: same seed → same `events_sha` | Seed determinism of Ollama (local; not guaranteed on cloud) |
| Population stays within [floor, cap]; births occur | LLM mutator output quality (bland drift, bloat — risk R3 in doc 02) |
| Shuffled control uses other genomes; `p_mut = 0` adds no alleles | Laya answer schema (parked) |
| LLM backend logic against a fake Ollama: table = points, malformed rows, cache reuse, API-key header, logprobs parsing | Whether evolution beats the controls (G4) — E4/E5 not built |

## History

The legacy lab (`Assets/02 - Scripts/04 - Crowds and Evolution/`, ≈ 420 lines
of C#) evolved a fixed MLP `[nEyes, 5, 1]` per animal (`NeuralNet.cs`) whose
single output is a turn angle. An animal standing on grass eats it and
immediately spawns one mutated asexual offspring (`Animal.cs`); `GeneticAlgo.cs`
keeps the population at ≥ `popSize / 2` with fresh random animals. Genomes were
unreadable weights, the behaviour space was one steering output, and there were
no seeds or logs (audit: [`../redesign/01`](../redesign/01-current-system.md)).

The redesign (docs 00–09, from 2026-09-27) replaces the weights with readable
text genes on homologous loci, adds a 7-action behaviour space, predators and
sexual reproduction, and puts every model behind one decision interface. Laya
was chosen first as the decision model, then parked on 2026-09-30 in favour of
an Ollama LLM brain (doc 05 §0). The Python prototype is the Phase 0 feasibility
spike; Unity work follows only if the gates in plan 08 §A2 pass.
