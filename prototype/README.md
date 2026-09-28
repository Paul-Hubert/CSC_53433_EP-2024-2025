# promptevo — Phase 0 spike

Headless Python test of *genes = text prompts → Laya decides → an LLM mutates*.
Plan: [`../Docs/redesign/08-phase0-spike-plan.md`](../Docs/redesign/08-phase0-spike-plan.md) ·
Handoff state: [`STATUS.md`](STATUS.md) · Rules for Claude: [`CLAUDE.md`](CLAUDE.md)

## Setup (local machine with Laya + Ollama)

```bash
cd prototype
python -m venv .venv && source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -e ".[dev]"            # numpy, pyyaml, pytest
pip install laya                   # decision model (S1.2)
pytest -q                          # offline tests (no models needed)
```

## What already works (offline)

```bash
python -m experiments.smoke_run --ticks 5000              # world + sim + rule-based agents, ASCII snapshots
python -m experiments.make_obs                              # data/observations_v1.jsonl
python -m experiments.e1_sensitivity --backend rule_based   # gene-sensitivity suite (reference numbers)
python -m experiments.status                                # background jobs
python -m experiments.peek results/runs/smoke/events.jsonl -n 3
```

## First local steps (need the real models)

```bash
python -m experiments.e0_probe_laya                         # S1.2 → results/e0_laya_api.md
python -m experiments.e0_probe_ollama --teacher <model> --mutator <small-model> --embed <embed-model>
python -m experiments.e0_budget                             # S1.5 (pip install transformers)
pytest -m laya ; pytest -m ollama                           # real-model smoke tests
python -m experiments.e1_sensitivity --backend laya --placement P4 --style V1 --tag laya_zs_P4V1
```

## Layout

| Path | What |
|---|---|
| `promptevo/world.py` | grid terrain (water/mountains), food regrowth, scripted predators |
| `promptevo/genome.py`, `founder.py` | alleles, genomes, crossover; founder/contrast/control pools |
| `promptevo/perception.py`, `obs_text.py`, `actions.py` | discretised observations, text styles V1/V2, 7 behaviours |
| `promptevo/sim.py` | lockstep loop, decision memo, reproduction, deaths, logging |
| `promptevo/backends/` | `random`, `rule_based`, `laya` (P1–P4), `ollama_policy` (teacher) |
| `promptevo/evolution/mutation.py` | word operators + LLM rewrite with guards |
| `promptevo/llm/ollama_client.py` | stdlib Ollama client with sqlite cache |
| `promptevo/metrics.py` | MI_G, MI_O, JSD, directed ΔP, locality, Spearman, bootstrap |
| `experiments/` | probes, smoke run, observation set, E1 suite, status/peek helpers |
| `data/` | founder pool v1 (draft, needs owner review H1), contrast & control alleles |
