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

## What already works

Genes mutate through the mutator LLM (`mutator.*`: qwen3.5:0.8b in Ollama on the CPU since
2026-10-08; gemma4:12b before) with every brain, so these runs need Ollama; add `--no-mutation`
to run fully offline (crossover only).

```bash
python -m experiments.smoke_run --ticks 5000              # Lab 1 flat world (192 x 192, cover) + rule-based prey and predators, ASCII snapshots
python -m experiments.smoke_run --no-mutation             # the same with no model at all
python -m experiments.smoke_run --backend random          # null model: predators live on newcomers only
python -m experiments.smoke_run --world terrain_preview   # preview of the terrain labs (water, mountains)
python -m experiments.make_obs                              # data/observations_v2.jsonl
python -m experiments.e1_sensitivity --backend rule_based   # gene-sensitivity suite (reference numbers)
python -m experiments.mutation_test                         # pure mutation: variety per temperature + lineages
python -m experiments.status                                # background jobs
python -m experiments.peek results/runs/smoke/events.jsonl -n 3
python -m experiments.smoke_run --set agents.litter=[1,1] --set world.cover_fraction=0   # change any setting for one run
python -m experiments.mutation_list results/runs/smoke     # every mutation of a run -> results/smoke_mutations.md
python -m experiments.crash_sweep --variants baseline --seeds 8   # do both species last without rescue? (no model calls)
```

## JEV brain (decisions since 2026-10-08)

```bash
docker compose -f docker/compose.yaml up -d                 # JEV-9B on vLLM (GPU); 5-8 min until healthy; docker/README.md
python -m experiments.jev_check                             # speed, contrast genes, mutator
python -m experiments.smoke_run --backend jev --ticks 500   # ≈ 25 min in the 192 x 192 world
nohup python -m experiments.smoke_run --backend jev --minutes 600 --ticks 50000 --out results/runs/<name> > logs/<name>.log 2>&1 &   # long runs: detached
```

## LLM brain (gemma via Ollama; the brain until 2026-10-08)

```bash
# configs/base.yaml: policy.model: gemma4:12b (the mutator is mutator.model since 2026-10-08)
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b  # speed, determinism, logprobs
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b  # do genes steer it? (G1/G2)
python -m experiments.e1_sensitivity --backend llm --tag llm_points               # full E1 suite
python -m experiments.smoke_run --backend llm --ticks 500                         # ≈ 50 min on a 16 GB GPU (--profile small: 10 min); read llm_calls
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
python -m experiments.make_mutants                                 # held-out alleles via the mutator (+ OOD)
python -m experiments.make_dataset --profile small                 # 3 000 keys, allele-split
python -m experiments.label_teacher --limit 50 && python -m experiments.label_teacher --check
nohup python -m experiments.label_teacher --workers 4 > logs/label_teacher.log 2>&1 &
```

## Layout

| Path | What |
|---|---|
| `promptevo/species.py` | the two species: prey (eat, flee, follow, rest, mate) and predators (hunt, follow, rest, mate; loci `predator.<action>`) |
| `promptevo/world.py` | grid world (flat in Lab 1; optional noise terrain with water/mountains, optional ridges), food regrowth, cover (hungry: no food there) |
| `promptevo/genome.py`, `founder.py` | alleles, genomes of either species (one gene per action), crossover; founder/contrast/control pools per species |
| `promptevo/perception.py`, `obs_text.py`, `actions.py` | discretised observations of both species (vision 20, distance bands), text styles V1/V2 with distances in cells, actions incl. hunt (with nothing to act on, the animal searches) |
| `promptevo/sim.py` | lockstep loop for prey and predators, decision memo, stamina, reproduction in litters of 2-4, migration at the caps, kills and digestion, deaths, logging; options off: egg bank, prey-linked predator breeding |
| `promptevo/backends/` | `random`, `rule_based`, `llm` (ollama_policy: points/logprobs/table/ksample), `jev` (JEV-9B on vLLM), `laya` (parked); `factory.py` (brains, mutator client) |
| `promptevo/evolution/mutation.py` | blind mutation: the LLM gets a fixed context line, a small-edit instruction (`prompts/mutate_v4.txt`) and the gene, nothing else; guards with redraws |
| `promptevo/llm/ollama_client.py`, `openai_client.py` | stdlib Ollama and OpenAI-compatible (vLLM) clients with sqlite caches |
| `docker/` | compose file and README for the JEV server and an optional GPU mutator |
| `promptevo/metrics.py` | MI_G, MI_O, JSD, directed ΔP, locality, Spearman, bootstrap |
| `experiments/` | probes, smoke run, observation set, E1 suite, gate, mutation test, gene report / timeline / swap, mutation list, crash screening (`crash_sweep`, `llm_table`), JEV check, status/peek helpers |
| `data/` | founder pool v2 (5 prey slots since 2026-10-07) and predator founder pool v2 (4 slots; drafts, need owner review H1), contrast alleles, control alleles v1, observation sets v1 (near/far, before 2026-10-07) and v2 (distance bands) |
