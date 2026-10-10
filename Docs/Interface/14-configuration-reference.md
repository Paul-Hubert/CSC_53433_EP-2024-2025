# 14 — Configuration reference

## 1. Rules

- **CFG-01 (MUST)** Each parameter lives on the one module that uses it, with
  its unit and valid range, and is visible in the inspector.
- **CFG-02 (MUST)** A **scenario** (a preset) can override any parameter by path
  (`<species>/<module>/<field>`, e.g. `Prey/Litter/max`) without editing the
  scene, like the prototype's `--set agents.litter=[1,1]`.
- **CFG-03 (MUST)** The full effective configuration is saved with every run
  (`run_info.json`).
- **CFG-04 (MUST)** A parameter that a gene may change is a trait (ANIM-16).
- **CFG-05 (SHOULD)** Presets exist for the reference worlds: Lab 1 full, Lab 1
  small, terrain preview.

## 2. Reference values

Values of the prototype on 2026-10-09 (`configs/base.yaml`), one cell read as one
meter, kept exactly (owner decision). "small" is the profile for tests and quick checks.

### World

| Parameter | Value | Unit |
|---|---|---|
| seed | 1234 | — |
| size | 192 × 192 (small 48 × 48) | m |
| decision period | 4 | ticks |
| sampling temperature | 1.0 | — |
| stats interval | 100 | ticks |
| act order | species in turn (simultaneous: random winner for contested items) | — |
| wait mode | responsive in the editor, freeze in batch mode | — |
| text style | V1 | — |
| unit word in texts | "meters" | — |

### Ground (terrain preview only; Lab 1 is flat)

| Parameter | Value |
|---|---|
| water fraction, mountain fraction | 0.15, 0.10 (Lab 1: 0, 0) |
| noise octaves (cell sizes) | 16, 8, 4 m |
| minimum connected walkable share, retries | 0.60, 50 |
| ridges (k × k areas), gap | 0 (off), 4 m |

### Food layer

| Parameter | Value | Unit |
|---|---|---|
| cell size | 1 | m |
| initial fraction | 0.1 | of walkable cells outside cover |
| regrowth probability | 0.0015 (terrain preview 0.001) | per empty cell per tick |
| near-water bonus, radius | × 2, 3 | —, m |
| food in cover | no | — |

### Cover and carcasses

| Parameter | Value | Unit |
|---|---|---|
| cover fraction | 0.2 (0 = no cover) | of walkable ground |
| cover noise octaves | 8, 4 | m |
| flee into cover within (prototype option) | 6 (Unity reference: off, hide action instead) | m |
| carcass portions (on the prey's edible component) | 2 (0 = no carcass) | — |
| carcass energy per portion (same) | 30 | energy |
| carcass lifetime (same) | 100 | ticks |
| eggs edible | no (no edible component) | — |

### Perception

| Parameter | Value | Unit |
|---|---|---|
| band edges | 1, 4, 10 | m |
| vision (trait) | 20 | m |
| partner readiness range | 20 | m |
| energy levels | low < 30 ≤ medium ≤ 70 < high | energy |

### Species

| Parameter | `prey` | `predator` | Unit |
|---|---|---|---|
| initial population | 136 (small 24) | 28 (small 4) | animals |
| floor | 10 | 3 | animals |
| cap | 270 (small 40) | 68 (small 6) | animals |
| cap rule | migrate | migrate | — |
| maximum energy | 100 | 100 | energy |
| start energy (founders, newcomers) | 60 | 60 | energy |
| base cost | 0.7 | 0.7 | energy per tick |
| move cost | 0.5 | 0.5 | energy per meter |
| rest cost | 0.2 | 0.2 | energy per tick |
| locomotion | kinematic, straight lines | kinematic, straight lines | — |
| walk speed (trait of the locomotion) | 1 | 1 | m per tick |
| run speed (trait of the locomotion) | 1 (flee) | 2 (hunt) | m per tick |
| maximum stamina (trait) | 60 | 30 | m |
| stamina recovery | 2 | 2 | per tick without moving |
| recovery cost | 0.3 | 0.3 | energy per tick |
| stamina levels | 20 / 40 | 10 / 20 | low / high thresholds |
| maturity (trait) | 150 | 150 | ticks |
| maximum age | 1 500 | 1 500 | ticks |
| mate energy | 50 | 50 | energy |
| child energy | 40 | 40 | energy per baby, shared by the parents |
| litter | 2–4 | 2–4 | babies |
| incubation | 0 | 0 | ticks (a few ticks hide the mutator's latency) |
| wander turn probability | 0.25 | 0.25 | per tick |
| energy gained per food item (set on the food layer's edible component) | 25 | — | energy |
| kill chance (trait) | — | 0.5 | per strike |
| energy gained per kill (set on the prey's edible component) | — | 60 | energy |
| digestion | — | 50 ticks per kill, in proportion for portions | ticks |

Options of the prototype, off: predator interference (c = 0, radius 3 m),
prey per predator (q = 0), prey seen for breeding (m = 0), egg bank (off, 2 000
ticks), one shared cap.

### Evolution

| Parameter | Value |
|---|---|
| sexual | yes |
| mutation rate | 0.03 per gene per baby |
| text operator | LLM instruction deck v4 (7 instructions) |
| context line | "The sentence below is a rule that a wild animal follows." |
| mutator temperature | 1.2 |
| maximum words | 12 |
| tries | 5 |
| controls | shuffled: off, random founders: off ([15](15-controls-metrics-and-gates.md)) |

### Brains and services

| Service | Parameter | Value |
|---|---|---|
| where everything runs | — | the owner's computer, locally (owner decision) |
| world default brain | — | JEV (the keyword brain is not part of the Unity system) |
| answer cache | format, place | a local JSON-lines file per brain |
| Ollama points brain (optional) | model | gemma4:12b |
| | host | `http://localhost:11434` (or a cloud host with a key from an environment variable) |
| | parallel requests | 2 |
| | context length, thinking | 4 096, off |
| | timeout, keep-alive | 120 s, 30 min |
| JEV brain | host | `http://localhost:8000` (vLLM) |
| | served model | `jev-decision` (the LoRA module) |
| | model repository, revision | `autotrust/JEV-9B`, pinned `b63f651c…` |
| | maximum input | 1 024 tokens (counted, not cut) |
| | timeout, strict | 60 s, yes |
| Mutator | API, host | Ollama, `http://localhost:11434` (its own address; an OpenAI-compatible server also works) |
| | model | qwen3.5:0.8b on the CPU (context 1 024, thinking off) |
| | timeout | 60 s |
