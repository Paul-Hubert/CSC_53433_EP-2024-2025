# 12 — Testing, headless runs & migration

How to keep the framework correct while students extend it, how to run
experiments without the editor UI, and the build order from today's repo.

## Test pyramid

| Level | Framework | What | Speed |
|---|---|---|---|
| **Core unit** | EditMode (NUnit) | genome, registry, crossover, operators, guards, renderers, rule-based logits, metrics, RNG streams | ms |
| **Golden parity** | EditMode | the same inputs as the Python prototype give the same outputs (below) | ms |
| **Contracts** | EditMode | every `IDecisionBackend` found by `TypeCache` returns valid distributions (length, sum = 1, no NaN) for a fixed batch; every operator's output passes the guards or returns null | s |
| **Validation** | EditMode | all sample assets pass the Simulation Doctor (`AllSamplesAreValid`) | s |
| **Determinism** | PlayMode | 500 ticks twice with the same seeds → identical event-stream SHA-256 | s |
| **Smoke** | PlayMode | each sample profile runs 2 000 ticks with `RuleBasedBackend`; population stays above the floor without immigrants dominating | s–min |
| **Model (opt-in)** | EditMode, category `ollama` | real Ollama: schema answers parse, cache keys stable | min |

The **contract tests are generic**: they discover implementations by
interface, so a student's new backend or operator is tested the moment it
compiles. Students run the same suite before handing in.

### Golden parity with the Python prototype

A small script in the prototype (to be added: `experiments/export_golden.py`)
writes JSON cases from the reference implementation; Unity tests replay them:

| Golden file | Cases | Python source |
|---|---|---|
| `render_v1.json`, `render_v2.json` | observation → text | `obs_text.render` |
| `rule_based.json` | (genes, observation) → probabilities (tolerance 1e-5) | `RuleBasedBackend.decide` |
| `word_ops.json` | (text, operator, forced choice) → text | `op_intensity`, `op_negate`, `op_condition_swap`, `op_synonym` |
| `guards.json` | raw LLM output → cleaned text / valid? | `clean`, `valid` |
| `metrics.json` | distributions → MI_G, MI_O, JSD, ΔP | `metrics.py` |
| `founder_pool_v1.json` | the pool itself | `data/founder_pool_v1.json` |

Whole-run parity is checked **statistically** (population, deaths by
cause, action shares within tolerance over several seeds), because numpy's
generators are not reproduced in C#.

## Headless and batch runs

```text
# validate everything (CI, before handing in)
Unity -batchmode -nographics -projectPath . -executeMethod GeneticAgents.Editor.Cli.ValidateAll -quit

# one run
Unity -batchmode -nographics -projectPath . -executeMethod GeneticAgents.Editor.Cli.Run \
      -profile Assets/GeneticAgents/Samples/Profiles/C1_Full.asset -ticks 5000 -seed 7 -out Runs/c1_s7

# an experiment matrix (conditions × seeds)
Unity -batchmode -nographics -projectPath . -executeMethod GeneticAgents.Editor.Cli.RunExperiment \
      -experiment Assets/GeneticAgents/Samples/Experiments/E4.asset -out Runs/E4
```

- Output folders use the prototype layout (`events.jsonl`, `stats.csv`,
  `alleles.jsonl`, `summary.json`, `final_population.json`, plus
  `run_info.json` for provenance), so the Python
  metrics and future analysis scripts read Unity runs directly, and the
  Genome Browser reads Python runs.
- Exit codes: `0` ok, `2` validation errors, `3` run failed.
- Progress is written to `<out>/progress.json` (the prototype's
  `status` helper convention).
- The response cache lives in `Library/GeneticAgents/Cache` by default
  (not committed); a course-wide shared cache folder can be set in
  `OllamaSettings`.

## Migration plan from today's repo

Today: Unity 2021.3 project with the legacy lab
(`Assets/02 - Scripts/04 - Crowds and Evolution/`: `GeneticAlgo`, `Animal`,
`NeuralNet`), the terrain/brush labs (`CustomTerrain`, `TerrainBrush`,
`InstanceBrush`), and the Python prototype as the behavioural reference.

| Step | Deliverable | Done when |
|---|---|---|
| M0 | Upgrade to Unity 6 LTS (or decide to stay on 2021.3); create the assembly skeleton and folders of [02](02-architecture-overview.md) | empty assemblies compile; legacy labs still run |
| M1 | **Core**: genetics, observation, decision contracts, RNG streams, events, validation report, metrics; golden export script in the prototype | golden + core tests green |
| M2 | **Grid parity**: `GridWorld`, the five senses, seven actions, `GridLocomotion`, `Metabolism`, default systems, `RuleBasedBackend`, `MemoDecorator`, recorder, headless CLI | statistical parity with `smoke_run` on the small profile; determinism test green |
| M3 | **Authoring & Doctor**: assets, `SubclassSelector` drawer, built-in validation rules, pre-Play hook, species/founder inspectors, script templates | a new action can be added following R3 with zero framework edits |
| M4 | **LLM**: `OllamaClient`, `OllamaBackend` (points first, then logprobs/table), persistent cache, fallback, `LlmRewriteOperator`, settings page | the prototype's teacher gate gives the same verdict from Unity |
| M5 | **Terrain**: `TerrainWorld` over `CustomTerrain`, `KinematicLocomotion`, walkability overlay | the evolution scene runs on a student-made terrain |
| M6 | **Student windows**: Brain Debugger, Genome Browser, Gene Playground, Population Dashboard, Experiment Runner, New Feature Wizard | lab handout exercises can be done without reading framework code |
| M7 | **Course package**: samples (C1–C7 profiles), lab handout, reference results for founder pool v1 | a dry run by a TA |
| later | Option B (`LlmDevelopment` + `UtilityBackend`), `NeuralNetBackend` port of the legacy lab, predators that evolve, physics/DRL bodies, VR | as course projects |

Order rationale: M1–M2 can start now with the rule-based brain while the
Phase 0 gates on real models are still pending
([09 progress log](../redesign/09-progress-log.md)); if the gates push
the course to option B, only M4's terminal backend changes.

### Legacy code

| Legacy | Becomes |
|---|---|
| `NeuralNet.cs` | `NeuralNetBackend` + `GaussianOperator` (comparison baseline, scenario 6) |
| `Animal.cs` | split into senses (`FoodSense` from the eye ray-march), `WanderAction`/`EatAction`, `Metabolism` |
| `GeneticAlgo.cs` | `FoodRegrowthSystem`, `ImmigrationSystem`, `BirthSystem` |
| `CustomTerrain.cs` | kept as is; wrapped by `TerrainWorld` (Adapter) |
| Brush editors | kept; they produce the terrain the agents live on |

## Definition of done for any framework change

1. Interface or base class documented in these docs.
2. A sample implementation and a script template.
3. A validation hook (local `Validate` or a rule) and, if visual, a gizmo or window panel.
4. Contract tests pick it up; golden tests updated if behaviour changed.
5. The impact matrix in [11](11-change-scenarios.md) still says "none" in the last column.
