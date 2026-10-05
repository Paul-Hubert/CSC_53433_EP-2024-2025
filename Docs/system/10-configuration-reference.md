# 10 — Configuration reference

Every key of `prototype/configs/base.yaml`, the profiles, and how to override them.

Units: *tick* = one simulation step; *cells* = grid cells (Chebyshev unless
stated); *energy* = agent energy units (max 100). "Used in" lists modules under
`prototype/promptevo/` unless prefixed with `experiments/`.

## Top level

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `seed` | 1234 | int | Default sim seed and world seed; also seeds the experiment scripts' genome/observation sampling | `sim.py`, `experiments/e1_sensitivity.py`, `teacher_gate.py`, `make_obs.py`, `make_mutants.py`, `make_dataset.py` |
| `profile` | (added) | str | Set by `load_config` to the profile name; not in the file | — |

## `world`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `width` | 64 (small 48) | cells | Grid width | `world.py` |
| `height` | 64 (small 48) | cells | Grid height | `world.py` |
| `water_fraction` | 0.15 | fraction | Share of cells below the water quantile | `world.py` |
| `mountain_fraction` | 0.10 | fraction | Share of cells above the mountain quantile | `world.py` |
| `noise_octaves` | [16, 8, 4] | cells per lattice step | Value-noise octave cell sizes; amplitude halves per octave | `world.py` |
| `min_walkable_connected` | 0.60 | fraction of all cells | Minimum size of the largest connected grass region; else regenerate (max 50 tries) | `world.py` |
| `food_initial_fraction` | 0.08 | probability per walkable cell | Initial food | `world.py` |
| `food_regrow_p` | 0.001 | probability per cell per tick | Food regrowth (tuned 2026-09-28) | `world.py` |
| `food_water_bonus` | 2.0 | multiplier | Regrowth multiplier near water | `world.py` |
| `water_bonus_radius` | 3 | cells | "Near water" radius | `world.py` |

## `predators`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `count` | 3 (small 2) | int | Number of scripted predators | `world.py` |
| `chase_radius` | 6 | cells | Chase the nearest agent within this distance | `world.py` |
| `kill_p` | 0.3 | probability | Kill chance when on an agent's cell (tuned; plan started at 0.5) | `sim.py` |
| `turn_p` | 0.2 | probability per tick | Heading change while wandering | `world.py` |
| `rest_after_kill` | 20 | ticks | Pause after a kill (no move, no kill) | `sim.py` |

## `agents`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `init_pop` | 30 (small 24) | agents | Founders at t = 0 | `sim.py` |
| `floor` | 10 | agents | Below this, immigrants are spawned from the founder pool | `sim.py` |
| `cap` | 60 (small 40) | agents | Births blocked at or above this population | `sim.py` |
| `vision` | 12 | cells | Perception radius; also target search radius of executors | `perception.py`, `actions.py` |
| `near` | 3 | cells | "near" bucket boundary | `perception.py` |
| `energy_max` | 100.0 | energy | Cap when gaining energy | `actions.py` |
| `energy_start` | 60.0 | energy | Founders and immigrants | `sim.py` |
| `cost_base` | 0.7 | energy / tick | Base metabolism for every action except an idle rest (tuned; plan 0.5) | `sim.py` |
| `cost_move` | 0.5 | energy / tick | Extra when the agent moved | `sim.py` |
| `cost_rest` | 0.2 | energy / tick | Cost of `rest` (replaces the base cost) | `sim.py` |
| `eat_gain` | 25.0 | energy | Gain per food item | `actions.py` |
| `maturity` | 150 | ticks | Age to be `adult` and mate-ready | `perception.py` |
| `max_age` | 1500 | ticks | Death by old age above this | `sim.py` |
| `mate_energy` | 50.0 | energy | Minimum energy to be mate-ready | `perception.py` |
| `child_energy` | 40.0 | energy | Child's start energy, paid by the parent(s) | `sim.py` |
| `attack_steal` | 10.0 | energy | Maximum stolen per successful attack | `actions.py` |
| `attack_cost` | 3.0 | energy | Paid per attack attempt | `actions.py` |
| `energy_low` | 30.0 | energy | Below → observation `energy: low` | `perception.py` |
| `energy_high` | 70.0 | energy | Above → observation `energy: high` | `perception.py` |
| `wander_turn_p` | 0.25 | probability per tick | Heading change for `wander` and invalid actions | `actions.py` |

## `sim`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `decision_period` | 4 | ticks (D) | All agents decide when `t % D == 0`; doc 05 suggests D = 8 to halve LLM calls | `sim.py`, `experiments/make_obs.py` |
| `sampling_temperature` | 1.0 | τ | `p^(1/τ)` renormalised before sampling | `sim.py` |
| `stats_every` | 100 | ticks | `stats.csv` row interval | `sim.py` |

## `evolution`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `sexual` | true | bool | false = asexual reproduction (C7) | `sim.py` |
| `p_mut` | 0.03 | probability per locus per child | Mutation rate; 0 = C2 | `evolution/mutation.py` |
| `shuffled` | false | bool | C3: decide with a random other agent's genome | `sim.py` |
| `random_founders` | false | bool | C4: founders, immigrants and `founder_reintroduce` from control texts | `sim.py` |
| `operators.intensity` | 1.0 | weight | never ↔ … ↔ always ladder | `evolution/mutation.py` |
| `operators.negate` | 1.0 | weight | Toggle opposites / add "Do not" | `evolution/mutation.py` |
| `operators.condition_swap` | 1.0 | weight | Swap or append a "when …" condition | `evolution/mutation.py` |
| `operators.synonym` | 1.0 | weight | Small lexicon substitution | `evolution/mutation.py` |
| `operators.founder_reintroduce` | 0.5 | weight | Replace with another founder allele | `evolution/mutation.py` |
| `operators.llm_rewrite` | 1.0 | weight | LLM rewrite; effective only when `ollama.mutator_model` is set | `evolution/mutation.py`, `backends/factory.py` |
| `max_action_words` | 12 | words | Guard cap for action genes; `{max_words}` in the mutate prompt for action loci | `evolution/mutation.py`, `backends/factory.py` |
| `max_temperament_words` | 15 | words | Guard cap for temperament genes; `{max_words}` in the mutate prompt for temperament loci | `evolution/mutation.py` |

Operator names must match the operators the `Mutator` knows; an unknown name
with weight > 0 fails with `KeyError` at the first mutation.

## `backend`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `name` | rule_based | `random` \| `rule_based` \| `llm` \| `laya` | Default backend of `smoke_run` (others take `--backend`) | `experiments/smoke_run.py` |
| `obs_style` | V1 | `V1` \| `V2` | Observation text style for LLM / Laya backends | `backends/ollama_policy.py`, `backends/laya_backend.py`, `experiments/teacher_gate.py`, `label_teacher.py` |

## `policy` — LLM brain

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `model` | null | Ollama model tag | Decision model; falls back to `ollama.teacher_model`; must be set for `--backend llm` | `backends/ollama_policy.py`, `experiments/teacher_gate.py` |
| `mode` | points | `points` \| `logprobs` \| `table` \| `ksample` | Decision mode ([07](07-decision-backends.md#modes-policymode)) | `backends/ollama_policy.py` |
| `table_k` | 8 | situations per call | K in table mode | `backends/ollama_policy.py`, `experiments/teacher_gate.py` |
| `workers` | 2 | threads | Parallel LLM requests | `backends/ollama_policy.py` |
| `prompt` | prompts/teacher_v1.md | path | Decision prompt template | `backends/ollama_policy.py` |

## `laya` — parked 💤

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `checkpoint` | convaiinnovations/laya | HF id | Laya checkpoint | `backends/laya_backend.py`, `experiments/e0_probe_laya.py` |
| `subfolder` | null | str | e.g. the multilingual variant | same |
| `placement` | P4 | P1–P4 | Where genes go in Laya's input | `backends/laya_backend.py`, `experiments/e0_budget.py` |
| `device` | null | str | null = library default | `backends/laya_backend.py` |
| `head_max_len` | {english: 192, multilingual: 256} | tokens | Question-segment budget from the model card | `experiments/e0_budget.py`, `e0_probe_laya.py` |
| `tokenizer` | {english: answerdotai/ModernBERT-large, multilingual: jhu-clsp/mmBERT-base} | HF ids | Fallback tokenizers for the budget estimate | `experiments/e0_budget.py`, `e0_probe_laya.py` |

## `ollama`

| Key | Default | Type / unit | Meaning | Used in |
|---|---|---|---|---|
| `host` | http://localhost:11434 | URL | Local server, or `https://ollama.com` for direct cloud | `llm/ollama_client.py` |
| `api_key_env` | OLLAMA_API_KEY | env var **name** | The key is read from this variable; never put the key itself in a config | `llm/ollama_client.py` |
| `teacher_model` | null | model tag | Fallback for `policy.model`; labelling teacher (parked) | `backends/ollama_policy.py`, `experiments/teacher_gate.py`, `label_teacher.py` |
| `mutator_model` | null | model tag | Small model for `llm_rewrite`; null disables LLM mutation | `backends/factory.py`, `experiments/smoke_run.py`, `make_mutants.py` |
| `embed_model` | null | model tag | Planned for embedding text distance; **not read by any code** (the probe takes `--embed`) | — |
| `teacher_mode` | points | `points` \| `ksample` | Mode of the parked labelling script only | `experiments/label_teacher.py` |
| `ksample_k` | 8 | samples | k in `ksample` mode | `backends/ollama_policy.py`, `experiments/teacher_gate.py`, `label_teacher.py` |
| `timeout_s` | 120 | seconds | HTTP timeout per request | `llm/ollama_client.py` |

## `paths`

All relative to `prototype/` (`config.resolve`).

| Key | Default | Meaning | Used in |
|---|---|---|---|
| `cache_dir` | cache | sqlite caches (`policy`, `ollama`, `laya`) | `llm/ollama_client.py`, `backends/*.py` |
| `data_dir` | data | allele files, observation set, datasets | `sim.py`, `founder.py`, experiments |
| `results_dir` | results | experiment reports | `experiments/e1_sensitivity.py`, `teacher_gate.py` |
| `logs_dir` | logs | progress files and logs | `progress.py` (via callers), experiments |

`experiments/status.py` defaults to `logs` directly rather than reading
`paths.logs_dir`.

## Not configurable (hard-coded)

| Value | Where |
|---|---|
| LLM request seed 0, temperature 0 (points/logprobs/table), 0.8 (ksample), `keep_alive "30m"`, `top_logprobs` 10, logprob coverage 0.5 | `backends/ollama_policy.py` |
| Rewriter temperature 0.9, 3 attempts | `llm/ollama_client.py`, `evolution/mutation.py` |
| Client retries 3, back-off 1/2/4 s | `llm/ollama_client.py` |
| Rule-based logits, keywords, deltas | `backends/rule_based.py` |
| E1 set sizes, gate thresholds | `experiments/e1_sensitivity.py` (`SIZES`, `GATES`), `teacher_gate.py` |
| Allele file version `v1` | `founder.py` default, `sim.py` |

## Profiles

| File | Content | Purpose |
|---|---|---|
| `configs/small.yaml` | `world: {width: 48, height: 48}`, `predators: {count: 2}`, `agents: {init_pop: 24, cap: 40}` | CPU-friendly; **the default profile** |
| `configs/full.yaml` | `{}` | GPU profile; `base.yaml` values are already "full" |

## Overriding

```python
from promptevo.config import load_config
cfg = load_config("small",
                  overrides={"evolution": {"shuffled": True, "p_mut": 0.0}},
                  extra_files=["configs/my_experiment.yaml"])     # applied before overrides
```

- Order: `base.yaml` → profile → each extra file → `overrides` (deep merge;
  lists replaced).
- Command line: every script takes `--profile`; some take specific flags
  (`smoke_run --backend --seed --ticks --out`, `e1_sensitivity --backend --mode
  --workers --style --obs --tag`, `teacher_gate --modes --model --prompt
  --n-obs --workers`). There is no generic `--set key=value`; for other keys,
  edit a YAML file or call `load_config` from a script.
- Record every tuning change in `prototype/STATUS.md` › Decisions.
