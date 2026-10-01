# promptevo — Phase 0 spike

Headless Python test of *genes = text prompts → an LLM decides → an LLM mutates*
(Ollama, local or cloud; Laya parked — see `../Docs/redesign/05-decision-backend.md` §0).
Plan: [`../Docs/redesign/08-phase0-spike-plan.md`](../Docs/redesign/08-phase0-spike-plan.md) ·
Handoff state: [`STATUS.md`](STATUS.md) · Rules for Claude: [`CLAUDE.md`](CLAUDE.md) ·
Full documentation: [`../Docs/prompt-genome/`](../Docs/prompt-genome/README.md)

## Setup (machine with Ollama, or an Ollama Cloud key)

```bash
cd prototype
python -m venv .venv && source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -e ".[dev]"            # numpy, pyyaml, pytest
# optional, parked: pip install laya
pytest -q                          # offline tests (no models needed)
```

## What already works (offline)

```bash
python -m experiments.smoke_run --ticks 5000              # Lab 1 flat world + rule-based agents, ASCII snapshots
python -m experiments.smoke_run --backend random          # null model: the population collapses to the floor
python -m experiments.smoke_run --world terrain_preview   # preview of the terrain labs (water, mountains)
python -m experiments.make_obs                              # data/observations_v1.jsonl
python -m experiments.e1_sensitivity --backend rule_based   # gene-sensitivity suite (reference numbers)
python -m experiments.status                                # background jobs
python -m experiments.peek results/runs/smoke/events.jsonl -n 3
```

## LLM brain (needs Ollama)

```bash
# configs/base.yaml: policy.model: gemma4:12b (chosen 2026-10-01), ollama.mutator_model: gemma4:26b
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b  # speed, determinism, logprobs
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b  # do genes steer it? (G1/G2)
python -m experiments.e1_sensitivity --backend llm --tag llm_points               # full E1 suite
python -m experiments.smoke_run --backend llm --ticks 500                         # ≈ 7 min on a 16 GB GPU; read llm_calls
# cloud: export OLLAMA_API_KEY=...; set ollama.host: https://ollama.com (or use cloud tags via local server)
```

## Parked: Laya + distillation (kept for a later project)

```bash
python -m experiments.e0_probe_laya                         # S1.2 → results/e0_laya_api.md
python -m experiments.e0_probe_ollama --teacher <model> --mutator <small-model> --embed <embed-model>
python -m experiments.e0_budget                             # S1.5 (pip install transformers)
pytest -m laya ; pytest -m ollama                           # real-model smoke tests
python -m experiments.e1_sensitivity --backend laya --placement P4 --style V1 --tag laya_zs_P4V1
```

## Parked: distillation dataset (S4.5–S5)

```bash
python -m experiments.make_mutants --mutator <small-model>         # held-out alleles (+ OOD)
python -m experiments.make_dataset --profile small                 # 3 000 keys, allele-split
python -m experiments.label_teacher --limit 50 && python -m experiments.label_teacher --check
nohup python -m experiments.label_teacher --workers 4 > logs/label_teacher.log 2>&1 &
```

## Layout

| Path | What |
|---|---|
| `promptevo/world.py` | grid world (flat in Lab 1; optional noise terrain with water/mountains), food regrowth, scripted predators |
| `promptevo/genome.py`, `founder.py` | alleles, genomes, crossover; founder/contrast/control pools |
| `promptevo/perception.py`, `obs_text.py`, `actions.py` | discretised observations, text styles V1/V2, 7 behaviours |
| `promptevo/sim.py` | lockstep loop, decision memo, reproduction, deaths, logging |
| `promptevo/backends/` | `random`, `rule_based`, `llm` (ollama_policy: points/logprobs/table/ksample), `laya` (parked); `factory.py` |
| `promptevo/evolution/mutation.py` | word operators + LLM rewrite with guards |
| `promptevo/llm/ollama_client.py` | stdlib Ollama client with sqlite cache |
| `promptevo/metrics.py` | MI_G, MI_O, JSD, directed ΔP, locality, Spearman, bootstrap |
| `experiments/` | probes, smoke run, observation set, E1 suite, status/peek helpers |
| `data/` | founder pool v1 (draft, needs owner review H1), contrast & control alleles |
