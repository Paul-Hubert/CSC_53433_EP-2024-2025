# 08 — Code and configuration reference

A map of the repository, every module, every configuration key and every
command-line script. For concepts, read [03](03-world-and-simulation.md)–[05](05-decision-backends.md) first.

## Contents

1. [Repository layout](#1-repository-layout)
2. [Package `promptevo`](#2-package-promptevo)
3. [Configuration](#3-configuration)
4. [Scripts (`experiments/`)](#4-scripts-experiments)
5. [Data and prompts](#5-data-and-prompts)
6. [Tests](#6-tests)
7. [Generated files](#7-generated-files)
8. [Extending the system](#8-extending-the-system)

---

## 1. Repository layout

```text
CSC_53433_EP-2024-2025/
├── Assets/ Packages/ ProjectSettings/   Unity course project (existing labs, unchanged)
├── README.md                           course README (Unity setup)
├── Docs/
│   ├── prompt-genome/                  ← this documentation
│   └── redesign/                       design history: vision, assessment, plans, decisions
└── prototype/                          headless Python implementation ("promptevo")
    ├── promptevo/                      the package
    ├── experiments/                    runnable scripts (python -m experiments.<name>)
    ├── configs/                        base.yaml, size profiles, world overlays
    ├── data/                           founder, contrast and control alleles; observation set
    ├── prompts/                        LLM prompt templates
    ├── results/                        committed results (runs/ is not committed)
    ├── tests/                          pytest suite
    ├── STATUS.md                       live handoff state of the spike
    ├── CLAUDE.md                       working rules for Claude sessions
    └── pyproject.toml                  package metadata (numpy, pyyaml; pytest for dev)
```

The Unity project is the existing course. The prompt-genome system currently
lives only in `prototype/`. A Unity version is on the roadmap
([09](09-status-and-roadmap.md)).

## 2. Package `promptevo`

| Module | Purpose | Main contents |
|---|---|---|
| `config.py` | Load YAML configuration | `load_config(profile, overrides, extra_files)` deep-merges `base.yaml`, the profile, optional extra files (world overlays) and overrides; `Cfg` (dict with attribute access); `resolve(path)` (paths relative to `prototype/`); `CONFIG_DIR` |
| `rng.py` | Reproducible randomness | `Streams(seed).get(name)`: one independent generator per name; `fresh(name)` |
| `world.py` | Grid, food, predators | `World` (terrain, walkability, food, regrowth, predators, `step_toward`, `step_heading`, `nearest_food`); `value_noise`; `largest_component`; `cheb` (Chebyshev distance); cell codes `GRASS, WATER, MOUNTAIN` |
| `perception.py` | What an animal senses | `Observation` (frozen dataclass, 7 fields, `tags()` for directed tests); `sense(agent, world, agents, cfg)`; `mate_ready`; `RELEVANT_TAG` |
| `obs_text.py` | Observation → text | `render(obs, style)` with styles `V1`, `V2` |
| `actions.py` | Behaviour executors | `EXECUTORS[action](agent, world, agents, cfg, rng) -> moved`; invalid choices call `_invalid` → wander |
| `genome.py` | Genes | `LOCI`, `ACTION_LOCI`, `TEMPERAMENT_LOCI`, `ACTIONS`; `Allele`; `Genome` (10 allele ids); `AlleleRegistry` (dedup, `genes()`, `genome_key()`, `dump_jsonl`); `crossover_uniform` |
| `founder.py` | Allele files → genomes | `AllelePools`: `sample_founder`, `sample_control`, `neutral_genome`, `contrast_pair(locus)` |
| `evolution/mutation.py` | Mutation | one blind random change by the mutator LLM: `TEMPLATE`, `load_instructions(path)`, guards (`clean`, `valid`), `Mutator` (`mutate(genome, rng) -> (genome, events)`, `mutate_text(locus, text, rng) -> (text or None, instruction index, seed)`, `stats`) |
| `sim.py` | The simulation | `Agent`, `Counters`, `Simulation` (`step`, `decide`, `run`, `finish`, `summary`, `stats_row`) |
| `backends/base.py` | Brain protocol | `Query(genome_key, genes, obs)`; `Backend` protocol (`name`, `decide(queries) -> [n, 7]`); `normalise` |
| `backends/random_policy.py` | Uniform brain | `RandomBackend` |
| `backends/rule_based.py` | Keyword brain | `RuleBasedBackend`; `default_logits`, `gene_weight`, `temperament_deltas` |
| `backends/ollama_policy.py` | LLM brain | `LLMPolicyBackend` (modes points / table / ksample / logprobs, caches, workers, `from_config`); `TeacherBackend` (alias); `teacher_prompt`, `table_prompt`, `POINTS_SCHEMA`, `points_to_probs`, `logprobs_to_probs` |
| `backends/factory.py` | Name → brain | `make_backend(name, cfg, strict=False)` (strict: a failed LLM call raises instead of a one-off uniform answer; failures are never cached); `make_rewriter(cfg, check=False)` → `(llm, model)` for mutation, or `(None, None)` when mutation is off; with `check=True` it raises `MutatorUnavailable` if Ollama doesn't answer |
| `backends/laya_backend.py` | Laya brain (parked) | `LayaBackend` |
| `llm/ollama_client.py` | Ollama HTTP client | `OllamaClient` (`chat`, `chat_json`, `chat_raw`, `embed`, `models`, `digest`, retries, sqlite cache, base options, `think`); `client_from_config(cfg)`; `make_rewriter(client, model, temperature)` → `llm(prompt, seed)` |
| `cache.py` | Persistent cache | `KVCache` (sqlite key → JSON value); `make_key(*parts)` (sha256 of the JSON) |
| `metrics.py` | Measurement | `entropy`, `jsd`, `mi_genome`, `mi_obs`, `behaviour_distance`, `directed`, `spearman`, `locality`, `shannon_diversity`, `bootstrap_ci` |
| `eventlog.py` | Run logs | `EventLog` (`events.jsonl` with a running hash, `stats.csv`, `flush`); `replace_file` (a rename that waits out a reader on Windows) |
| `render.py` | ASCII map | `ascii_map(world, agents)`, `LEGEND`, `ACTION_CHAR` |
| `progress.py` | Job progress files | `Progress` → `logs/<job>.progress.json` (`deadline` for time-boxed jobs; states running, done, stopped, failed); `keep_awake()` (asks Windows not to sleep while a job runs) |

### Using the package from Python

```python
from promptevo.config import load_config
from promptevo.backends.factory import make_backend
from promptevo.sim import Simulation

cfg = load_config("small")                       # base.yaml + configs/small.yaml
sim = Simulation(cfg, make_backend("rule_based", cfg), seed=7, out_dir="results/runs/mine")
summary = sim.run(2000)                          # also writes the files listed in 03 §12
print(summary["pop_final"], summary["max_gen"])
```

Overrides without editing YAML:

```python
cfg = load_config("small", overrides={"world": {"food_regrow_p": 0.0005},
                                      "evolution": {"p_mut": 0.0}})
```

## 3. Configuration

`configs/base.yaml` holds every default. A **profile** (`--profile small` or
`full`, default `small` in the scripts that read the config) is merged over it, then optional
**world overlays** from `configs/worlds/`, then overrides.

| File | Content |
|---|---|
| `base.yaml` | all defaults (the full-size Lab 1 world) |
| `small.yaml` | 48 × 48 world, 2 predators, 24 animals at start, cap 40 (CPU-friendly; the default profile) |
| `full.yaml` | empty: the defaults are the full size |
| `worlds/terrain_preview.yaml` | noise terrain: 15 % water, 10 % mountains, food regrowth 0.001 |

### `seed`

| Key | Default | Meaning |
|---|---|---|
| `seed` | 1234 | default seed for the simulation and the world |

### `world`

| Key | Default | Meaning |
|---|---|---|
| `width`, `height` | 64, 64 (small: 48, 48) | grid size in cells |
| `water_fraction` | 0.0 | share of cells that are water (Lab 1: none) |
| `mountain_fraction` | 0.0 | share of cells that are mountains (Lab 1: none) |
| `noise_octaves` | [16, 8, 4] | cell sizes of the heightmap noise (used only with water or mountains) |
| `min_walkable_connected` | 0.60 | minimum share of the map in the largest walkable region, else regenerate |
| `food_initial_fraction` | 0.08 | chance that a walkable cell starts with food |
| `food_regrow_p` | 0.0007 | chance per empty walkable cell per tick to grow food |
| `food_water_bonus` | 2.0 | regrowth multiplier near water |
| `water_bonus_radius` | 3 | "near water" distance in cells |

### `predators`

| Key | Default | Meaning |
|---|---|---|
| `count` | 3 (small: 2) | number of predators |
| `chase_radius` | 6 | chase the nearest animal within this distance |
| `kill_p` | 0.3 | kill chance per tick when on an animal's cell |
| `turn_p` | 0.2 | turning chance per tick while wandering |
| `rest_after_kill` | 20 | ticks of rest after a kill |

### `agents`

| Key | Default | Meaning |
|---|---|---|
| `init_pop` | 30 (small: 24) | animals at tick 0 |
| `floor` | 10 | minimum population; newcomers fill the gap |
| `cap` | 60 (small: 40) | maximum population; no births above it |
| `vision` | 12 | perception radius (cells) |
| `near` | 3 | "near" threshold (cells) |
| `energy_max` | 100 | energy cap |
| `energy_start` | 60 | energy of founders and newcomers |
| `cost_base` | 0.7 | energy cost per tick |
| `cost_move` | 0.5 | extra cost on ticks with a move |
| `cost_rest` | 0.2 | cost of a resting tick (replaces `cost_base`) |
| `eat_gain` | 25 | energy per food item |
| `maturity` | 150 | age at which an animal becomes adult |
| `max_age` | 1500 | age at which it dies |
| `mate_energy` | 50 | minimum energy to mate |
| `child_energy` | 40 | energy of a newborn, paid by the parents |
| `attack_steal` | 10 | energy stolen by a successful attack |
| `attack_cost` | 3 | energy cost of an attack attempt |
| `energy_low`, `energy_high` | 30, 70 | thresholds of the low / medium / high energy buckets |
| `wander_turn_p` | 0.25 | turning chance per tick when wandering |

### `sim`

| Key | Default | Meaning |
|---|---|---|
| `decision_period` | 4 | ticks between decisions |
| `sampling_temperature` | 1.0 | τ for sampling actions (p^(1/τ)) |
| `stats_every` | 100 | ticks between rows of `stats.csv` |

### `evolution`

| Key | Default | Meaning |
|---|---|---|
| `sexual` | true | two parents with crossover (false: one parent, the old lab's regime) |
| `p_mut` | 0.03 | chance per gene per child that the mutator LLM changes it |
| `shuffled` | false | control C3: decide with another living animal's genome |
| `random_founders` | false | control C4: founders with random-text genes |
| `mutation_prompts` | prompts/mutate_v2.txt | the mutation instructions, one drawn at random per mutation ([04 §5](04-genome-and-evolution.md#5-mutation)) |
| `temperature` | 1.2 | sampling temperature of the mutator LLM |
| `max_action_words` | 12 | maximum length of an action gene (longer answers are rejected) |
| `max_temperament_words` | 15 | maximum length of a temperament gene |

### `backend`

| Key | Default | Meaning |
|---|---|---|
| `name` | rule_based | default brain for `smoke_run`: `random`, `rule_based`, `llm` (`laya` is parked) |
| `obs_style` | V1 | observation text style (V1 terse, V2 first person) |

### `policy` (the LLM brain)

| Key | Default | Meaning |
|---|---|---|
| `model` | gemma4:12b | Ollama model tag that decides |
| `mode` | points | `points`, `table`, `ksample` or `logprobs` ([05](05-decision-backends.md#other-modes)) |
| `table_k` | 8 | situations per call in table mode |
| `workers` | 2 | parallel requests to Ollama |
| `prompt` | prompts/teacher_v1.md | prompt template |

### `ollama`

| Key | Default | Meaning |
|---|---|---|
| `host` | http://localhost:11434 | local server, or `https://ollama.com` for the cloud API |
| `api_key_env` | OLLAMA_API_KEY | name of the environment variable holding a cloud key (never put the key itself in a file) |
| `teacher_model` | null | fallback for `policy.model` (older name) |
| `mutator_model` | gemma4:12b | the model that mutates genes (the same as the brain: no model swaps); `null` = no mutation |
| `embed_model` | null | embedding model (optional; parked analyses) |
| `teacher_mode`, `ksample_k` | points, 8 | settings of the labelling pipeline and of ksample mode |
| `timeout_s` | 120 | HTTP timeout per request |
| `options` | {num_ctx: 4096} | Ollama options sent with every chat request |
| `think` | false | turn off the reasoning phase of thinking models (`null` = server default) |

### `laya` (parked)

Checkpoint, input placement and tokenizer settings for the parked Laya
backend. See `Docs/redesign/05-decision-backend.md`.

### `paths`

| Key | Default | Meaning |
|---|---|---|
| `cache_dir` | cache | sqlite caches (`ollama.sqlite`, `policy.sqlite`) |
| `data_dir` | data | allele files and observation sets |
| `results_dir` | results | reports |
| `logs_dir` | logs | job logs and progress files |

## 4. Scripts (`experiments/`)

Run them from `prototype/` with `python -m experiments.<name>`. The scripts that
read the configuration (`smoke_run`, `teacher_gate`, `e1_sensitivity`,
`make_obs`) accept `--profile` (default `small`); `e0_probe_ollama`, `status`
and `peek` don't read it.

### Running the world

| Script | Purpose | Options |
|---|---|---|
| `smoke_run` | Run a simulation, print ASCII snapshots and a summary, write the run files | `--ticks` (5000), `--backend` (config default `rule_based`; `random`, `llm`), `--seed`, `--out` (`results/runs/smoke`), `--snapshots` (3; 0 = none), `--world` (overlay from `configs/worlds/`), `--no-mutation` (crossover only, no model needed), `--minutes` (stop after N minutes of wall-clock time). Stop cleanly with `logs/run_<name>.stop`; run the same command again to resume ([03 §12](03-world-and-simulation.md#12-what-a-run-writes-to-disk)) |

With `--backend llm` the summary also prints `llm_calls`, failures and the
memo hit rate. Genes mutate through the mutator model with every brain, so
Ollama must be running; if it doesn't answer, the run stops at the start with a
clear message. `--no-mutation` runs without it.

### Measuring the brain

| Script | Purpose | Options |
|---|---|---|
| `e0_probe_ollama` | Facts about an Ollama model: structured output, determinism, logprobs, speed, mutator samples → `results/e0_ollama.md` | `--teacher` (decision model, required), `--mutator`, `--embed`, `--host`, `--num-ctx` (4096; 0 = server default), `--think` (off / on / default) |
| `teacher_gate` | Decision-model gate: contrast pairs + 10 founders + 10 random-text genomes × n observations → `results/teacher_gate.md/.json` | `--modes` (points,table), `--n-obs` (24), `--workers`, `--model`, `--prompt` |
| `e1_sensitivity` | The full gene-sensitivity suite (E1) with any brain → `results/e1_<tag>.md/.json` | `--backend`, `--tag`, `--mode`, `--workers`, `--obs`, `--chunk`, `--style`, `--placement` (Laya) |
| `mutation_test` | Pure mutation, no selection: every founder sentence × seeds × temperatures (variety, edit size, length, keyword-brain effect) and lineages mutated step after step → `results/mutation_test.md/.json` | `--temps` (0.9,1.2,1.5,2.0), `--seeds` (8), `--steps` (30), `--workers` (4), `--model`, `--tag` |
| `make_obs` | Rebuild the observation set (`data/observations_v1.jsonl`) from synthetic situations + the most frequent ones of a rule-based run | `--ticks` (3000), `--out` |

### Reading a run

| Script | Purpose | Options |
|---|---|---|
| `gene_report RUN [RUN …]` | Ranks genes by the fitness of their carriers: offspring of the animals that carried them and died, each divided by the mean of the animals that died in the same 5 000 ticks. Genes are grouped by what the keyword brain reads in them (strength, action, conditions), since mutation spreads the population over thousands of texts; exact texts are ranked too. Also: share killed by predators, frequency over time, predator kills per 1 000 animal-ticks, lineage of the most common genes. With several runs (seeds) the marks use all of them: ★ clearly above average, ▲ steady leader (above average in every run), ✗ clearly below → `results/<tag>_genes.md/.json`; method in [10 §2](10-natural-selection-runs.md#2-measuring-which-genes-did-best) | `--min-carriers` (100), `--every` (10000 ticks), `--tag` |
| `gene_timeline RUN` | How the genes develop during one run, from its files (also a run still going): the gene pool per checkpoint and slot (distinct genes, effective number, leader, mutants, mutation depth, length, world words, overlap); genes that reached 25 % with their lineages; mutants produced vs spread vs alive at the end next to the mutation test; gene dropping (genes handed down the real family tree at random) for selection vs drift; families; behaviour per window; optional `--judge` (gemma4:12b says whether a gene still gives a usable rule, `prompts/judge_sense_v1.txt`) → `results/<tag>_timeline.md/.json/.csv/.html` | `--every` (500 ticks), `--window` (2000), `--drops` (500), `--tag`, `--judge`, `--judge-max` (3000) |

### Helpers

| Script | Purpose |
|---|---|
| `status` | One line per background job, from `logs/*.progress.json` (`DEAD?` if the process is gone) |
| `peek FILE [-n 5]` | Show the first rows of a JSONL / CSV file without flooding the terminal |

### Parked (Laya and distillation)

`e0_probe_laya`, `e0_budget`, `make_mutants`, `make_dataset`, `label_teacher`.
These are kept, tested with fakes, and unused since the 2026-09-30 decision to
let the LLM decide directly. Their usage is in their docstrings and in
`prototype/README.md`.

## 5. Data and prompts

| File | Content |
|---|---|
| `data/founder_pool_v1.json` | 10 loci × (4 founder alleles + neutral) ([04 §3](04-genome-and-evolution.md#3-the-founder-pool)) |
| `data/contrast_alleles_v1.json` | pro / anti sentence per action locus, for directed tests only |
| `data/control_alleles_v1.json` | 20 shuffled-word + 20 irrelevant sentences for random-text genomes |
| `data/observations_v1.jsonl` | 48 observations (32 synthetic + 16 frequent in a rule-based run), with directed-test tags |
| `prompts/teacher_v1.md` | decision prompt ([05](05-decision-backends.md#the-prompt)) |
| `prompts/mutate_v2.txt` | the 16 mutation instructions, one per line ([04 §5](04-genome-and-evolution.md#5-mutation)) |
| `prompts/novel_v1.md` | prompt for brand-new alleles (parked dataset pipeline) |

## 6. Tests

`cd prototype && pytest -q` runs 52 offline tests in about 25 s, with no model
needed. LLM calls are replaced by small fake servers. Two marked tests talk to
real models: `pytest -m ollama` (needs Ollama and `policy.model`) and
`pytest -m laya` (parked).

| File | What it checks |
|---|---|
| `test_core.py` | config merge, random streams, cache round-trip, progress files |
| `test_world.py` | flat Lab 1 world, terrain fractions and connectivity, nobody enters blocked cells, unambiguous map symbols |
| `test_behaviour.py` | flee increases distance, eating gains energy, invalid eat → wander, text styles, rule-based directed tests and gibberish |
| `test_genome.py` | allele pools, registry dedup and genome keys, crossover |
| `test_evolution.py` | guards and instruction list, the mutator sends only the instruction and the gene (fake LLM), rejected answers, no LLM → no mutation, determinism, population bounds, shuffled control, no mutation → no new alleles, decisions per action in `stats.csv` |
| `test_metrics.py` | entropy, JSD, mutual information, directed ΔP, Spearman |
| `test_gene_report.py` | `gene_report` on a short run: fitness averages to 1.00 in every slot, frequencies add up to the population; an unfinished run is rebuilt from its events |
| `test_gene_timeline.py` | a hand-made run with known numbers (shares, sweep timing, depth, families, gene dropping); invariants on a short unfinished run (slot shares add up, lineages end at founder texts, dropped shares of a slot add up to 1) |
| `test_long_run.py` | stop file, then resume by replay (same events, no model call asked twice); a failing model stops the run cleanly; `run_info.json`; progress time box |
| `test_llm_policy.py` | table mode vs points, malformed rows, persistent caches (incl. an empty cache file), a failed call is never cached and strict brains raise, API key header, factory (incl. mutation off and Ollama down), logprobs fallback |
| `test_adapters.py` | Laya request layouts and answer parsing (parked), Ollama client |
| `test_dataset.py` | dataset splits, resumable labelling, gate with a fake model (parked pipeline) |
| `test_local_models.py` | real-model smoke tests (`-m ollama`, `-m laya`) |

## 7. Generated files

| Path | Committed | Content |
|---|---|---|
| `results/*.md`, `results/*.json` | yes | probe, gate and E1 reports |
| `results/runs/` | no | per-run files (events, stats, alleles, summary, run info) |
| `cache/` | no | sqlite caches of LLM answers. Never delete them unasked: they make reruns free. |
| `logs/` | no | job logs and progress files |
| `data/mutants_v*.jsonl`, `data/dataset_v*_*.jsonl` | no | regenerable (parked pipeline) |

## 8. Extending the system

Each extension point is one small piece of code. These are the natural
student exercises ([02 §5](02-lab1.md#5-suggested-activities)).

| To add | Where | Remember to |
|---|---|---|
| A brain | a class with `name` and `decide(queries) -> [n, 7]` in `backends/`; register it in `factory.make_backend` | return rows that sum to 1 in `ACTIONS` order |
| A sense | a field in `perception.Observation`, computed in `sense`, rendered in `obs_text.render` | keep values discrete (cache hits); update the rule-based brain if it should react |
| An action | an executor in `actions.EXECUTORS`, a locus in `genome.ACTION_LOCI`, a line in `prompts/teacher_v1.md`, alleles in the founder and contrast files, keywords in `rule_based.ACTION_WORDS` (and a default in `default_logits`), a relevance tag in `perception.RELEVANT_TAG` | the action list is part of the JSON schema (`POINTS_SCHEMA` is built from `ACTIONS`); the rule-based brain and E1 fail with a `KeyError` without the last two |
| A mutation instruction | a line in `prompts/mutate_v2.txt` | it is drawn at random like the others; measure it with `experiments.mutation_test` |
| A crossover scheme | a function like `crossover_uniform`, called in `Simulation._birth` | keep loci homologous |
| A world feature | `world.py` (generation, `regrow_food`, movement) plus keys in `world:` | keep randomness in the world's streams; add a test |
