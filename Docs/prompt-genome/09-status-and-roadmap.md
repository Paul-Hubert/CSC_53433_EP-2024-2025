# 09 — Status, decisions and roadmap

State on **2026-10-07**, branch `claude/ai-agents-course-redesign-71pc1u`.
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
| World, simulation, logging, seeds | working and tested; **Lab 1 flat world** tuned; reference numbers measured again with genetic predators (2026-10-07) |
| Genome | prey: 5 genes, one per action (eat, flee, follow, rest, mate); predators: 3 genes (hunt, rest, mate); both since 2026-10-07; founder pools waiting for review (H1) |
| Predators | genetic animals since 2026-10-07: the same brain, energy, breeding and mutation as the prey; 4 genes (hunt, follow, rest, mate); vision 20 and distance bands for both species; same speed; one partner's `mate` is enough to breed; with the LLM brain they breed in the 96 × 96 world |
| Random and rule-based brains | working |
| LLM brain (`--backend llm`, gemma4:12b, points mode) | working end to end; ≈ 4 decisions/s on a 16 GB GPU |
| Mutation: one blind random change by the LLM (16 instructions, temperature) | working; gemma4:12b; pure-mutation test in `results/mutation_test.md` |
| Metrics, E1 suite, decision-model gate | working; gate run on gemma4 12b and 26b |
| Persistent answer caches | working (request-cache bug fixed 2026-10-01) |
| Tests | 66 offline tests pass; real-model test passed on gemma4:12b (10-gene genome) |
| Analysis tools | `gene_report` (gene fitness, frequency, mutant lineages) since 2026-10-02; `gene_timeline` (the gene pool over time, gene dropping) and `gene_swap` since 2026-10-06; `--species predator` since 2026-10-07 |
| Evolution matrix (E4), common garden (E5), report | not written |
| Unity version | not started |
| Laya brain and distillation | parked |

| Gate | Status |
|---|---|
| G1 Semantics | ✔ (gemma4 12b and 26b, small gate, 10-gene genome) |
| G2 Information | ✘: random text moves behaviour as much as real genes (10-gene genome) |
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
| 2026-10-02 | Mutation reviewed (`prototype/notes/mutation-review.md`), then replaced by one blind operator: the LLM gets a random-change instruction and the gene, nothing else. Mutation test (`results/mutation_test.md`). Three 50 000-tick runs with predators and the new `gene_report` ([10](10-natural-selection-runs.md)): no gene clearly best, careless temperaments clearly selected against. First hour-long LLM-brain run (5 269 ticks): the population escaped the floor after newcomers brought a workable gene set, and "Never fight." spread to 23 of 25 animals ([10 §7](10-natural-selection-runs.md#7-one-hour-with-the-llm-brain)). |
| 2026-10-06 | `smoke_run` made safe for long runs (stop file, clean stops, resume by replay, failed brain calls never cached). One 12-hour LLM-brain run: the rescued population thrived about 60 generations, then died out; nonsense genes swept as often as random inheritance predicts; a predation trap below about 20 animals. New analysis: `gene_timeline` (gene dropping), `gene_swap` ([11](11-gene-development.md)). |
| 2026-10-07 | Owner request: the genome cut to 5 genes, one per action (eat, flee, follow, rest, mate). The temperament genes (risk, social, place) and the attack and wander actions are gone, so animals no longer fight; an action with nothing to act on in sight makes the animal search. New prompt `teacher_v2.md`, founder pool v2, one length guard (`evolution.max_words`). Lab 1 reference numbers measured again: 25–31 animals (small), 50–55 (full) ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)). `gene_report` and `gene_timeline` still read the older 10-gene runs. Then, on the owner's request, **predators became genetic animals** like the prey: 3 genes (hunt, rest, mate), the same LLM brain with its own prompt (`predator_v1.md`), energy, breeding, mutation, a floor and a cap. A hunting predator next to its prey kills with probability 0.1, then digests for 50 ticks. Both species see 20 cells, with distances in bands (1, 2–4, 5–10, 11–20 cells), and move one cell per tick. Tuned with the keyword brain for coexistence: small world 21–25 prey and 4–5 predators. First LLM-brain run with both species (gemma4:12b, small world, seed 1234, 2 000 ticks, about 4 600 calls and 32 minutes from an empty cache, 0 failures): the prey sat at their floor from tick 600 to 1 500 and needed 35 newcomers, then grew to 28 once the founder predators died of old age; predators had 3 births and 4 newcomers ([06 §5.12](06-experiments-and-results.md#512-first-llm-brain-run-with-genetic-predators)). Then, on the owner's request, a **bigger world and partners seen farther**: readiness to mate is seen across the whole vision (`perception.partner_range` 20, was 4) and `smoke_run` runs the 64 × 64 world. With the keyword brain predators now breed in every world size (64 × 64: 4–9 predators, up to 13 generations). With the LLM brain (2 000 ticks): the prey fell to their floor (8 newcomers), then grew to 57; the predators never bred, because the brain chooses `mate` mostly when the ready partner is already next to them ([06 §5.13](06-experiments-and-results.md#513-llm-brain-in-the-64--64-world-partners-seen-across-the-vision)). Then **all four options for both species**: one partner's `mate` is enough to breed, predators can `follow` (4 genes), the full world is 96 × 96, and both prompts state the breeding rule. Keyword brain: 15–21 predators, 18–19 generations in 5 000 ticks. LLM brain (3 hours, 1 673 ticks): predators bred 29 times without newcomers, but stayed at 6–14 and often starved, while the prey filled the world up to their cap ([06 §5.14](06-experiments-and-results.md#514-llm-brain-in-the-96--96-world-with-all-four-changes)). |

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
| 2026-10-01 | `smoke_run` uses LLM mutation only with the LLM brain (superseded 2026-10-02) | matches the script's documentation; rule-based runs need no model |
| 2026-10-02 | **Mutation = one blind random change by the LLM**: an instruction drawn from 16 "random change" variants + the gene, nothing else; the word operators, rewrite styles and founder reintroduction are gone; mutator = gemma4:12b; every run with mutation needs the model (`--no-mutation` otherwise) | owner: mutation doesn't care about state or success, pure random; review in `prototype/notes/mutation-review.md` |
| 2026-10-07 | **Genome = 5 genes, one per action** (eat, flee, follow, rest, mate); the attack and wander actions removed (no fights between animals); temperament (risk, social, place) removed | owner request: genes kept to a minimum before predators become genetic animals in the same way |
| 2026-10-07 | **Predators are genetic animals like the prey**: genes hunt, rest, mate (loci `predator.<action>`), the same LLM brain with `prompts/predator_v1.md`, energy, breeding, mutation, floor and cap; the scripted chase and `predators.count` removed | owner request: "a gene-based actor with an LLM exactly like the prey", with a limited set of actions |
| 2026-10-07 | **Vision 20 cells for both species, distances in bands** (adjacent 1, close 2–4, medium 5–10, far 11–20, none), written as cell ranges in the situation text; **same speed** (one cell per tick); the prey prompt `teacher_v3.md` and the predator prompt state the equal speed | owner request: longer vision and some distance observation for both, the same speed for now |
| 2026-10-07 | **Partners seen across the vision** (`perception.partner_range` 20, was 4), both species; the keyword brain goes to a ready partner at any distance (one rule for both species, replacing the predators' far-partner workaround). **`smoke_run` runs the 64 × 64 world by default**; small (48 × 48) stays for tests and quick checks | owner: "bigger world, further distance for seeing partner"; in the small world the LLM-read prey didn't hold and predators rarely met |
| 2026-10-07 | **All four options, both species**: an animal that chose `mate` breeds with a ready partner next to it, whatever the partner chose; predators get `follow` (genes hunt, follow, rest, mate; `predator_founder_pool_v2.json`); the full world (`smoke_run`'s default) is 96 × 96 with populations scaled by area; `teacher_v4.md` and `predator_v2.md` state the breeding rule; the small world's predator cap is 6 | owner: "all 4 options for both animals, including follow" |
| 2026-10-07 | Predator rules and tuning: strike when next to the prey after a step; `kill_p` 0.1, `digest_ticks` 50, `kill_gain` 60, the prey's metabolism; floor 3 in both worlds; the keyword brain's predators seek a partner they see farther away when well fed | coexistence with the keyword brain over 4 seeds ([03 §3](03-world-and-simulation.md#3-predators)); without the partner search predators rarely met |

## 4. Decisions waiting on the course owner

| # | Decision | Context |
|---|---|---|
| 1 | Approve or edit the founder pools (H1) | `prototype/data/founder_pool_v2.json` and `predator_founder_pool_v1.json`, [04 §3](04-genome-and-evolution.md#3-the-founder-pool) |
| 2 | How to handle G2: all-zero answers → neutral answer? rewrite the control sentences? prompt iterations? keep the MI_G ≥ 0.25 threshold? | [06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions) |
| 3 | Lab 1 platform (Python prototype or Unity) and format (sessions, deliverables, grading) | [02 §8](02-lab1.md#8-open-decisions-for-the-course-owner) |
| 4 | Default brain in class and student hardware (lab server, cloud plan) | [02 §6](02-lab1.md#6-compute-budget) |
| 5 | Budget for the evolution matrix: run length, decision period, seeds | at ≈ 4 decisions/s, 5 000 ticks ≈ 1 h per run on one GPU |
| 6 | Details of the terrain and foliage labs | `Docs/redesign/06-world-terrain-foliage.md` |
| 7 | URL of your own git server for mirroring | `Docs/redesign/09-progress-log.md` |
| 8 | Lab 1 small world against the LLM brain: populations survive only above about 20 animals. Keep it, or change the world size or cap, the predators, or the mutation (`p_mut`, prompts that keep to the gene's world)? | [11 §9](11-gene-development.md#9-what-it-means) |
| 9 | `gene_timeline` does what Lab 1 activity D asks students to write (frequency curves per slot, diversity, instructions of the genes that spread): keep it as the teacher's solution, or change the activity to interpreting it (selection against drift, `gene_swap`)? | [02 activity D](02-lab1.md#d-watch-evolution), [11 §10](11-gene-development.md#10-reproduce-it-and-use-it-in-lab-1) |
| 10 | Predators: are the rules right (a strike next to the prey, digestion 50 ticks, the prey's metabolism)? Only 3–7 predators live in the Lab 1 worlds, so their genes drift; more food or a bigger world would allow more. Since the four changes, LLM predators breed (29 births in 1 673 ticks in the 96 × 96 world), but they stay few (6–14) and often starve, while the LLM-read prey fill the world up to their cap and mostly starve. Rebalance (a higher `kill_p` or a shorter digestion in the full world, less food or a lower prey cap), or accept a food-limited world? The full world also costs about 16 calls per tick: 5 000 ticks ≈ 9 hours. | [03 §3](03-world-and-simulation.md#3-predators), [03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world) |

## 5. Roadmap

### Phase 0 — finish the spike (Python)

**Done on 2026-10-07: predators are genetic animals** (owner request). Both
species evolve, see 20 cells with distances in bands and move at the same
speed; one partner's `mate` is enough to breed, predators can follow, and the
full world is 96 × 96. Next: decision 10 (the balance in the full world and its
cost), then longer LLM-brain runs with both species, followed with
`gene_timeline --species prey` and `--species predator`.

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
2. **Prompt genome in Unity:** loci, founder pool, crossover; an inference
   client with batching and caching; blind LLM mutation with logging.
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
