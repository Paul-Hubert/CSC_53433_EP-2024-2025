# 09 — Status, decisions and roadmap

State on **2026-10-01**, branch `claude/ai-agents-course-redesign-71pc1u`.
The live, step-by-step state of the spike is in `prototype/STATUS.md`, and the
owner-facing log is in `Docs/redesign/09-progress-log.md`.

## Contents

1. [Where things stand](#1-where-things-stand)
2. [Timeline](#2-timeline)
3. [Decisions taken](#3-decisions-taken)
4. [Decisions waiting on the course owner](#4-decisions-waiting-on-the-course-owner)
5. [Roadmap](#5-roadmap)
6. [Parked work](#6-parked-work)

---

## 1. Where things stand

| Part | State |
|---|---|
| World, simulation, logging, seeds | working and tested; **Lab 1 flat world** tuned |
| Random and rule-based brains | working |
| LLM brain (`--backend llm`, gemma4:12b, points mode) | working end to end; ≈ 4 decisions/s on a 16 GB GPU |
| Mutation: word operators + LLM rewrite | working; LLM rewrite uses gemma4:26b |
| Metrics, E1 suite, decision-model gate | working; gate run on gemma4 12b and 26b |
| Persistent answer caches | working (request-cache bug fixed 2026-10-01) |
| Tests | 41 offline tests pass; real-model test passes on gemma4:12b |
| Analysis tools (allele frequencies, lineages, diversity) | not written |
| Evolution matrix (E4), common garden (E5), report | not written |
| Unity version | not started |
| Laya brain and distillation | parked |

| Gate | Status |
|---|---|
| G1 Semantics | ✔ (gemma4 12b and 26b, small gate) |
| G2 Information | ✘: random text moves behaviour as much as real genes |
| G3 Locality | not measured on the LLM brain |
| G4 Evolution | not measured |
| G5 Throughput | ≈ 4 decisions/s vs ≥ 50 targeted → fails at current settings |

## 2. Timeline

| Date | What happened |
|---|---|
| 2026-09-27 | Vision captured; current lab audited; assessment, architecture and genome design written (`Docs/redesign/00–04`). Laya chosen as the decision model (05). |
| 2026-09-28 | Phase 0 spike planned: preregistered gates G1–G5, 7-session runbook (08). Offline core built and tested: world, simulation, rule-based brain, metrics, mutation, probes, E1. Teacher/distillation pipeline written. |
| 2026-09-30 | Decision: no Laya fine-tuning; Ollama LLMs decide and mutate. Laya parked. |
| 2026-10-01 | First local tests (Windows 11, RTX 5080, Ollama 0.32). gemma4:26b and 12b probed. Fixes: `think: false` and a fixed `num_ctx` (model reloads), Windows liveness check in `status`, request cache never written, `smoke_run` LLM mutation with every brain, ambiguous ASCII map symbols. Decision-model gate on both models (G1 ✔, G2 ✘). Brain switched to gemma4:12b. Point-total analysis (all-zero answers on random text). **Lab 1 = flat world with random food**, food regrowth retuned; noise terrain moved to `configs/worlds/terrain_preview.yaml`. First 500-tick run with the LLM brain. This documentation. |

## 3. Decisions taken

| Date | Decision | Why |
|---|---|---|
| 2026-09-27 | Genes are sentences in fixed loci; founder pool frozen and shared by all runs | crossover between matching slots; comparable runs (02, 04) |
| 2026-09-28 | Provisional world tuning (predators, energy costs) | food-limited population; deaths split between predators and starvation |
| 2026-09-28 | Invalid action → wander, counted | plan A6 |
| 2026-09-30 | Ollama LLMs decide and mutate; Laya parked | owner decision |
| 2026-10-01 | Send `think: false` and `num_ctx: 4096` with every request | without them gemma4 spilled to CPU and reloaded on every call (60–100 s) |
| 2026-10-01 | Brain = gemma4:12b (mutator stays gemma4:26b) | reads genes as well as 26b, ≈ 3× faster, fits fully in 16 GB |
| 2026-10-01 | **Lab 1 world is flat, food uniformly random, regrowth 0.0007** | owner: evolution is Lab 1; terrain and foliage labs come later. At 0.001 the flat world sat at the cap 25–68 % of the time; 0.0007 keeps it food-limited |
| 2026-10-01 | `smoke_run` uses LLM mutation only with the LLM brain | matches the script's documentation; rule-based runs need no model |

## 4. Decisions waiting on the course owner

| # | Decision | Context |
|---|---|---|
| 1 | Approve or edit the founder pool (H1) | `prototype/data/founder_pool_v1.json`, [04 §3](04-genome-and-evolution.md#3-the-founder-pool) |
| 2 | How to handle G2: all-zero answers → neutral answer? rewrite the control sentences? prompt iterations? keep the MI_G ≥ 0.25 threshold? | [06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions) |
| 3 | Mutator model: keep gemma4:26b or switch to 12b (no model swaps) | [05 §5](05-decision-backends.md#5-choosing-the-model) |
| 4 | Lab 1 platform (Python prototype or Unity) and format (sessions, deliverables, grading) | [02 §8](02-lab1.md#8-open-decisions-for-the-course-owner) |
| 5 | Default brain in class and student hardware (lab server, cloud plan) | [02 §6](02-lab1.md#6-compute-budget) |
| 6 | Budget for the evolution matrix: run length, decision period, seeds | at ≈ 4 decisions/s, 5 000 ticks ≈ 1 h per run on one GPU |
| 7 | Details of the terrain and foliage labs | `Docs/redesign/06-world-terrain-foliage.md` |
| 8 | URL of your own git server for mirroring | `Docs/redesign/09-progress-log.md` |

## 5. Roadmap

### Phase 0 — finish the spike (Python)

1. Decide G2 handling (decision 2), apply it, and rerun the gate. Cached
   answers make reruns cheap.
2. Run the full E1 suite on the LLM brain → G3 and a larger G1/G2 sample.
3. Run several seeds and longer runs with the LLM brain on the Lab 1 world.
   Re-tune if the LLM-read founders can't sustain a population.
4. Write the evolution tooling: the experiment matrix (C1 FULL, C2 NO-MUT, C3
   SHUFFLED, C4 RANDOM-FOUNDERS, C5 RULE-BASED), the common garden, and the
   analysis (allele frequencies, lineages, diversity, gene length) → G4, G5.
5. Write the report and take the go/no-go decision (H5).

If the brain can't be made to read genes (G1/G2) or is too slow (G5), the
fallback is **"LLM as development"**: one LLM call at birth turns the genome
into a fixed behaviour profile (utility weights, thresholds), which runs
cheaply every tick. That is 100–1 000× fewer calls
(`Docs/redesign/02-assessment.md`).

### Phase 1–3 — Unity (from `Docs/redesign/07-open-questions-and-roadmap.md`)

1. **Unity core:** fixed-step lockstep simulation, configuration asset, seeds,
   logging; agents split into perception / decision / executor / metabolism;
   rule-based and neural-network brains; walkability from the terrain.
2. **Prompt genome in Unity:** loci, founder pool, crossover, operators; an
   inference client with batching and caching; LLM mutation with logging.
3. **Student-facing tools:** genome browser, allele-frequency plots, lineage
   view, lab handouts with exercises, reference results for the founder pool.

### The lab sequence

Lab 1 runs on the flat world. The terrain and foliage labs then feed their
outputs into the world: walkability and food placement
([02 §7](02-lab1.md#7-toward-the-next-labs-terrain-and-foliage)).

## 6. Parked work

Kept in the repository, tested with fakes, not used:

- **Laya** decision model: backend, probes, input-placement study
  (`backends/laya_backend.py`, `experiments/e0_probe_laya.py`,
  `e0_budget.py`).
- **Distillation pipeline:** held-out mutants, allele-split dataset,
  resumable teacher labelling (`make_mutants.py`, `make_dataset.py`,
  `label_teacher.py`).

Background: `Docs/redesign/05-decision-backend.md`.
