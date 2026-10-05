# System reference — prompt-genome evolutionary agents

Reference documentation for the system as it is **implemented today** in
[`prototype/`](../../prototype/) (Python, headless). The design history and the
reasons behind each choice live in [`../redesign/`](../redesign/README.md); the
proposed Unity architecture lives in [`../unity/`](../unity/README.md).

Status: written 2026-10-05 against branch `claude/ai-agents-course-redesign-71pc1u`
(prototype state of 2026-09-30, 38 offline tests passing). Every number below was
read from code, config or the redesign docs; anything not verified is marked so.

## Reading order

| # | File | What it holds |
|---|------|---------------|
| — | [README](README.md) | This page: purpose, reading order, status legend. |
| 01 | [Overview](01-overview.md) | The system on one page: core loop, data flow, layers, module map, what is verified, history. |
| 02 | [World](02-world.md) | Grid terrain generation, walkability, food, scripted predators, movement helpers. |
| 03 | [Simulation loop](03-simulation-loop.md) | Exact tick order of `Simulation.step()`, decisions, memo, energy, breeding, deaths, floor, outputs. |
| 04 | [Agents, perception, actions](04-agents-perception-actions.md) | Agent fields, the discretised `Observation`, text styles V1/V2, the 7 action executors. |
| 05 | [Genome](05-genome.md) | Loci, alleles, registry, genome key, founder pool v1, contrast and control alleles. |
| 06 | [Evolution](06-evolution.md) | Reproduction, crossover, mutation operators and guards, implicit selection, experimental controls. |
| 07 | [Decision backends](07-decision-backends.md) | Backend protocol, `random`, `rule_based`, the LLM brain (Ollama) and its modes, Laya (parked), option A/B. |
| 08 | [Reproducibility & data](08-reproducibility-and-data.md) | RNG streams, seeds, caches, event log, run outputs, config loading, helper scripts. |
| 09 | [Metrics & experiments](09-metrics-and-experiments.md) | Every metric, experiments E0–E5, conditions C1–C7, gates G1–G5, current key numbers. |
| 10 | [Configuration reference](10-configuration-reference.md) | Every key of `configs/base.yaml`, the profiles, how to override. |
| 11 | [Extending the prototype](11-extending-the-prototype.md) | Recipes (new action, sense, backend, operator, metric, model) and the hard-coded coupling points. |
| — | [Glossary](glossary.md) | Terms used across these pages. |

Read 01 first. 02–07 follow the data flow (world → agent → genome → evolution →
brain). 08–10 are look-up pages. 11 is for whoever changes the code.

## One-paragraph summary

Each agent carries a **genome of 10 short natural-language genes**: one per
action (`eat`, `flee`, `follow`, `wander`, `rest`, `mate`, `attack`) and three
temperament genes (`risk`, `social`, `place`). Every 4 ticks, all agents sense a
discretised situation (energy, food, predator, other animal, age); a **decision
backend** — by default in the design an LLM served by Ollama, locally or in the
cloud — reads the genes plus the situation text and returns a probability for
each of the 7 actions, from which the action is sampled. Agents that choose
`mate` next to another mating agent produce a child whose genes are a
**per-locus uniform crossover** of the parents, then **mutated** at a low rate by
word-level operators or an LLM rewrite. Nothing scores fitness explicitly:
agents that starve, are caught by predators or grow old simply leave no more
children (**implicit selection**). Every run starts from the same **founder
pool**, and a population floor tops up from that pool, so runs share a common
origin and the surviving prompts can be read and compared at the end.

## Status legend

Used in every page of this folder.

| Icon | Meaning |
|---|---|
| ✅ | Working and tested (offline test suite and/or measured runs). |
| 🧪 | Written; tested only with fakes (fake Ollama transport, fake rewriter) or not run yet. Needs real models to verify. |
| 💤 | Parked: kept in the code, not on the default path (Laya, distillation). |
| ☐ | Planned in [`../redesign/08`](../redesign/08-phase0-spike-plan.md), not built. |

## Related

- Design history and decisions: [`../redesign/README.md`](../redesign/README.md)
  (vision 00 … progress log 09). Doc 05 §0 is the current backend decision.
- Proposed Unity architecture: [`../unity/README.md`](../unity/README.md).
- Prototype entry points: `prototype/README.md` (commands), `prototype/STATUS.md`
  (handoff state), `prototype/CLAUDE.md` (working rules).
