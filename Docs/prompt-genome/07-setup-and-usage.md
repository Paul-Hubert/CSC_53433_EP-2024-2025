# 07 — Setup, usage and troubleshooting

How to install the prototype, run it with each brain, check a new model, read
the outputs and fix the usual problems. Commands run from `prototype/` unless
stated otherwise.

## Contents

1. [Requirements](#1-requirements)
2. [Install](#2-install)
3. [Run with the rule-based brain](#3-run-with-the-rule-based-brain)
4. [Check a new model](#4-check-a-new-model)
5. [Run with the LLM brain](#5-run-with-the-llm-brain)
6. [Read the outputs](#6-read-the-outputs)
7. [Long jobs](#7-long-jobs)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Requirements

| For | You need |
|---|---|
| The simulation, the random and rule-based brains, all offline tests | Python ≥ 3.10 (tested with 3.13 on Windows 11), `numpy`, `pyyaml`, `pytest` |
| Gene mutation (every run, any brain) and the LLM brain | [Ollama](https://ollama.com) (tested with 0.32.0) and a model (`ollama pull gemma4:12b`); a GPU is strongly recommended |
| Measured setup | RTX 5080 16 GB, 62 GB RAM, Windows 11, gemma4:12b (8 GB download), fully on the GPU |

Without a GPU, use a shared lab server or Ollama Cloud
([05](05-decision-backends.md#ollama-settings-that-matter)), or run without
mutation (`--no-mutation`).

## 2. Install

macOS / Linux:

```bash
cd prototype
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
pytest -q                      # 67 passed, no model needed
```

Windows (PowerShell):

```powershell
cd prototype
python -m venv .venv
.venv\Scripts\activate
pip install -e ".[dev]"
pytest -q
```

In Git Bash on Windows, activate with `source .venv/Scripts/activate`, or call
`.venv/Scripts/python` directly.

## 3. Run with the rule-based brain

Genes mutate through the mutator model (`ollama.mutator_model`, gemma4:12b) with
every brain, so Ollama must be running: a 5 000-tick run makes about 60
mutation calls (0.15 per prey birth, 5 genes × 3 %; 0.09 per predator birth),
cached afterwards. If Ollama
doesn't answer, `smoke_run` stops at the start and says so. `--no-mutation`
runs with no model at all (crossover only).

```bash
python -m experiments.smoke_run                          # Lab 1 world (64 × 64), rule-based brain, 5 000 ticks
python -m experiments.smoke_run --no-mutation            # the same without any model, ≈ 9 s
python -m experiments.smoke_run --backend random         # null model: predators live on newcomers only
python -m experiments.smoke_run --seed 7 --ticks 2000    # another seed, shorter
python -m experiments.smoke_run --profile small          # 48 × 48 world for quick checks: 24 prey (cap 40), 4 predators (cap 10)
python -m experiments.smoke_run --world terrain_preview  # preview of the terrain labs (water, mountains)
python -m experiments.e1_sensitivity --backend rule_based   # gene-sensitivity suite (its single-gene edits come from the mutator)
```

`smoke_run` prints a legend, a few ASCII snapshots, then a summary line and
the share of each action for the prey and for the predators (here
`--no-mutation`, seed 1234, 2026-10-07):

```text
animals by current action: E eat, F flee, L follow, R rest, M mate, ? not decided yet | P predator | . food | ~ water | ^ mountain
--- t=3332 prey=38 predators=12
 EE .    .             . E . PPF
        .  .     E          P
  E            .       .
  ... .P . .         M.    .   .
...
prey: {"ticks": 5000, "pop_final": 59, "births": 736, "immigrants": 0, "deaths": {"predator": 435, "starvation": 271, "old_age": 1}, "mean_lifespan": 262.5, "max_gen": 23, ...}
prey actions: {'eat': 0.576, 'flee': 0.183, 'mate': 0.094, 'follow': 0.086, 'rest': 0.06}
predators: {"pop_final": 3, "births": 69, "immigrants": 0, "deaths": {"starvation": 67, "old_age": 5}, "kills": 435, "mean_lifespan": 508.1, "max_gen": 13, ...}
predator actions: {'hunt': 0.692, 'mate': 0.211, 'rest': 0.097}
```

The map is downsampled to fit the terminal, so one character can cover
several cells.

## 4. Check a new model

Run these before trusting any model as a brain:

```bash
ollama pull gemma4:12b
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b   # ≈ 1–3 min
pytest -m ollama                                                                  # two real decisions (a flee contrast pair)
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b   # 372 decisions, ≈ 5–15 min
```

- **The probe** (`results/e0_ollama.md`) checks structured output,
  determinism and logprobs, then measures seconds per decision and prints
  three mutator samples. It overwrites the file, so rename it to keep one per
  model.
- **The gate** answers "does this model read the genes?" (G1, G2;
  [06 §4](06-experiments-and-results.md#4-gates-g1g5)). It writes
  `results/teacher_gate.md`.
- After the gate, check `ollama ps`: the model should show **100% GPU** and
  the context **4096**.

Then set `policy.model` (and `ollama.mutator_model`) in `configs/base.yaml`.

## 5. Run with the LLM brain

1. Install Ollama and pull the model (`ollama pull gemma4:12b`). The server
   runs at `http://localhost:11434`.
2. Check `configs/base.yaml`: `policy.model: gemma4:12b`,
   `ollama.options: {num_ctx: 4096}`, `ollama.think: false`.
3. Start short:

```bash
python -m experiments.smoke_run --backend llm --ticks 500 --snapshots 1
```

The last lines report the cost, here from the first run (2026-10-01, with the
10-gene genome of the time):

```text
llm_calls=761 failures=0 backend_queries=761 (memo hit rate 0.558)
```

On the measured setup this took 425 s. Extrapolate before launching anything
longer: 5 000 ticks ≈ 1 hour. Answers are cached in `cache/`, so repeating a
run with the same genomes costs almost nothing.

**Cloud instead of a local GPU:**

```bash
export OLLAMA_API_KEY=...            # PowerShell: $env:OLLAMA_API_KEY = "..."
# configs/base.yaml: ollama.host: https://ollama.com, policy.model: <a cloud model tag>
```

Never write the key into a file. Alternatively, run `ollama signin` and use a
cloud model tag through the local server.

## 6. Read the outputs

A run writes to `results/runs/<name>/` ([03 §12](03-world-and-simulation.md#12-what-a-run-writes-to-disk)).

```bash
python -m experiments.peek results/runs/smoke/stats.csv -n 10
python -m experiments.peek results/runs/smoke/events.jsonl -n 5
python -m experiments.peek results/runs/smoke/alleles.jsonl -n 5
```

Load them in Python for analysis:

```python
import json, csv
stats = list(csv.DictReader(open("results/runs/smoke/stats.csv")))
events = [json.loads(l) for l in open("results/runs/smoke/events.jsonl")]
alleles = {a["id"]: a for a in map(json.loads, open("results/runs/smoke/alleles.jsonl"))}
births = [e for e in events if e["kind"] == "birth"]
mutations = [m for b in births for m in b["mutations"]]
print(len(births), "births,", len(mutations), "mutations")
print(alleles[mutations[0]["child"]] if mutations else "no mutation yet")
```

## 7. Long jobs

Jobs longer than a few minutes should run in the background with a log file:

```bash
nohup python -m experiments.e1_sensitivity --backend llm --tag llm_points > logs/e1_llm.log 2>&1 &
python -m experiments.status          # one line per job: running / done / DEAD?, progress, ETA
```

On Windows PowerShell, use `Start-Process` with `-RedirectStandardOutput`, or
run from Git Bash. `e1_sensitivity` and `teacher_gate` write
`logs/<job>.progress.json`. `smoke_run` prints only at its snapshots, so watch
the cache grow instead:

```bash
python -c "import sqlite3; print(sqlite3.connect('cache/policy.sqlite').execute('select count(*) from kv').fetchone()[0])"
```

## 8. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Every LLM call takes 30–100 s | Ollama reloads the model on each call, because requests use different context sizes or another model is competing for memory | Keep `ollama.options.num_ctx` fixed (it is sent with every call); check `ollama ps`; stop other models with `ollama stop <model>` |
| `ollama ps` shows e.g. `32%/68% CPU/GPU` | the model, or its context, doesn't fit in GPU memory | use a smaller model, or keep `num_ctx` at 4096 |
| Call times vary between about 1.5 s and 35 s | the GPU is shared with other work (training jobs, games, a Unity editor) | stop the other GPU work, or accept slower runs (results don't change, the simulation waits) |
| Answers come slowly and the model "thinks" | a thinking model with reasoning on | `ollama.think: false` |
| A long run disappears; the system is low on memory | other applications use most of the RAM | close them, or use a smaller model |
| `gene mutation uses the Ollama model … but the server didn't answer` | since 2026-10-02 every run mutates genes with the mutator model, whatever the brain | start Ollama and `ollama pull gemma4:12b`, or add `--no-mutation` |
| A rerun asks the LLM again for everything | the cache key includes the model digest and the prompt's hash, so a new `ollama pull` or an edited prompt starts fresh (the switch to `teacher_v2.md` on 2026-10-07 is such a change). Before 2026-10-01 a bug kept the request cache (`cache/ollama.sqlite`) empty. | expected after a model or prompt change |
| `experiments.status` shows `DEAD?` for a running job | old liveness check on Windows: `os.kill(pid, 0)` sends Ctrl+C there (signal 0) instead of probing (fixed 2026-10-01) | update the code |
| The gate's output got overwritten | `teacher_gate` always writes `results/teacher_gate.md` | copy it per model after each run |
| `RuntimeError: Ollama /api/chat failed after 3 tries` | server not running, wrong host, or model not pulled | `ollama list`, `ollama serve`, check `ollama.host` |
