# Prompt-Genome Evolution — project documentation

Animals in a simulated world carry **genomes made of English sentences**. A
local **LLM** reads those sentences, together with the animal's situation, and
decides what the animal does. **Evolution** (mating, crossover, mutation by
blind random LLM changes, survival) reshapes the sentences over
generations, and at the end you can read what evolved.

This is the redesign of the course's Crowds & Evolution lab, which becomes
**Lab 1**. It runs on a **flat world with food scattered at random**. In the
following labs, students build terrain and then foliage, and those change the
world the animals live in.

> **Status (2026-10-01):** a working headless Python prototype (`prototype/`,
> package `promptevo`). The LLM brain (gemma4:12b via Ollama) runs end to end;
> genes steer behaviour in the intended direction (gate G1 ✔), but random text
> still moves behaviour too much (G2 ✘), and it is slow (≈ 4 decisions/s).
> Evolution experiments and the Unity version are next.
> [09 — Status and roadmap](09-status-and-roadmap.md).
>
> **2026-10-07:** the genome now has 5 genes, one per behaviour (eat, flee,
> follow, rest, mate). The temperament genes and the attack and wander actions
> are gone ([04 §1](04-genome-and-evolution.md#1-genes-are-sentences-in-fixed-slots)).
> The same day the **predators became genetic animals**: 4 genes (hunt, follow,
> rest, mate), the same LLM brain, energy, breeding and mutation. Both species see 20
> cells, with distances in bands, see a partner's readiness as far, and move at
> the same speed ([03 §3](03-world-and-simulation.md#3-predators)). Runs use the
> 96 × 96 world by default, and one partner's choice is enough to breed.

## Documents

| # | Document | Read it for |
|---|---|---|
| 01 | [Overview](01-overview.md) | the idea, why the lab changes, how it works, design choices, glossary |
| 02 | [Lab 1](02-lab1.md) | the flat world, learning goals, how to run it, suggested activities, compute budget, the bridge to the terrain and foliage labs |
| 03 | [World and simulation](03-world-and-simulation.md) | grid, food, the two species (prey and predators), energy, perception and distance bands, actions, decisions, reproduction, tick order, outputs, reference numbers |
| 04 | [Genome and evolution](04-genome-and-evolution.md) | loci, alleles, founder pool, crossover, blind LLM mutation with real examples, selection, controls |
| 05 | [The brain: decision backends](05-decision-backends.md) | random, rule-based and LLM brains; the prompt; points mode; caching; Ollama settings; model choice; measured costs |
| 06 | [Experiments, metrics and results](06-experiments-and-results.md) | metrics, gates G1–G5, every result so far, known issues, next experiments |
| 07 | [Setup, usage and troubleshooting](07-setup-and-usage.md) | install, run with and without a model, check a model, read outputs, long jobs, fixes |
| 08 | [Code and configuration reference](08-code-and-config-reference.md) | modules, every configuration key, every script, tests, data, how to extend |
| 09 | [Status, decisions and roadmap](09-status-and-roadmap.md) | where things stand, timeline, decisions taken and pending, roadmap |
| 10 | [Natural selection in long runs](10-natural-selection-runs.md) | 230-generation runs with (scripted) predators, how the gene report measures which genes did best, marked genes, drift |
| 11 | [How genes develop in one long run](11-gene-development.md) | 12 hours with the LLM brain: five phases from rescue to extinction, lineages of nonsense genes, gene dropping (selection or drift), the predation trap, whether genes stay meaningful |

## Where to start

- **Course owner / teaching staff:** 01 → 02 → 09 (pending decisions).
- **Students (Lab 1):** 01 → 02 → 07 → 03 and 04 as needed.
- **Developers:** 01 → 03 → 04 → 05 → 08, with 06 for the measurements.

## Quick start

```bash
cd prototype
python -m venv .venv && source .venv/bin/activate     # Windows: .venv\Scripts\activate
pip install -e ".[dev]"
pytest -q                                              # 69 tests, no model needed
python -m experiments.smoke_run                        # Lab 1 world (96 × 96), rule-based brain, ≈ 22 s
ollama pull gemma4:12b
python -m experiments.smoke_run --backend llm --ticks 500   # the LLM brain for both species, ≈ 11 min on a 16 GB GPU
```

## Related material

| Where | What |
|---|---|
| `prototype/` | the code ([08](08-code-and-config-reference.md)) |
| `prototype/STATUS.md` | the live step-by-step state of the spike, updated every session |
| `Docs/redesign/` | design history: vision (00), audit of the previous lab (01), assessment (02), architecture (03), genome design (04), decision backend analysis (05), world requirements (06), roadmap (07), spike plan with gates (08), progress log (09) |
| `README.md` (repository root) | the Unity course setup |
