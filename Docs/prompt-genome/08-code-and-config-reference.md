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
| `species.py` | The two species | `Species(name, actions, prefix, config)` with `loci`; `PREY` (eat, flee, follow, rest, mate; config section `agents`), `PREDATOR` (hunt, follow, rest, mate; loci `predator.<action>`; section `predators`); `SPECIES`, `ALL_LOCI`, `species_of_locus`, `species_cfg(cfg, species)` |
| `world.py` | Grid, food and carcasses | `World(cfg, rng_world)` (terrain, walkability, food, regrowth, `carcasses` and `age_carcasses`, `step_toward`, `step_heading`, `nearest_food`); `Carcass` (portions, killer, rot, `edible_by`); `value_noise`; `largest_component`; `cheb` (Chebyshev distance); cell codes `GRASS, WATER, MOUNTAIN` |
| `perception.py` | What an animal senses | `Observation` (prey) and `PredatorObservation` (frozen dataclasses with a `scale` of band edges, `tags()` for directed tests); `sense(agent, world, prey, predators, cfg)`; `stamina_level`, `edible_carcass`; `stamina_level`, `edible_carcass`; distance bands `DIST`, `NEAR`, `band(d, scale)`, `make_scale(cfg, species)`, `DEFAULT_SCALE`; `mate_ready`; `RELEVANT_TAG` |
| `obs_text.py` | Observation → text | `render(obs, style)` for both species, styles `V1`, `V2`; `distance(band, scale)` ("2-4 cells away") |
| `actions.py` | Behaviour executors | `EXECUTORS[action](agent, world, prey, predators, cfg, rng) -> cells moved` for both species, including `do_hunt` (run, strike, kill, leave a carcass, eat from one, digest); `cells` (1 walking, the species' `speed` running, never beyond stamina); an action with nothing to act on in sight calls `_invalid`: a random walk (`_wander`), counted as invalid |
| `genome.py` | Genes | `ACTIONS`, `LOCI` (the prey's: one gene per action); `Allele`; `Genome(alleles, species)`; `AlleleRegistry` for both species (dedup, `genes()` → action → text, `genome_key()`, `make_genome(genes, origin, species)`, `dump_jsonl`); `crossover_uniform` |
| `founder.py` | Allele files → genomes | `AllelePools(registry, data_dir, version=None, control_file="control_alleles_v1.json", species=PREY)`: `sample_founder`, `sample_control`, `neutral_genome`, `contrast_pair(locus or action)`; the predator files start with `predator_` |
| `evolution/mutation.py` | Mutation | one blind small change by the mutator LLM: `TEMPLATE`, `load_instructions(path)`, guards (`clean`, `valid`, `changed_words`, `load_vocabulary`, `unknown_words`), `Mutator` (`mutate(genome, rng) -> (genome, events)`, `mutate_text(locus, text, rng) -> (text or None, instruction index, seed)` with up to `tries` attempts, `attempt` (one call, with the reason of a rejection), `reject(new, old)`, `stats` with `calls` and `rejected`) |
| `sim.py` | The simulation | `Agent` (`species` from its genome, `stamina`, `digest`, `killer`), `Counters`, `Simulation` (`agents` = prey, `predators`, `members(species)`, counters per species in `cs` with `c` = prey; `step`, `decide(agents)` one batch per species, `run`, `finish`, `summary`, `stats_row`) |
| `backends/base.py` | Brain protocol | `Query(genome_key, genes, obs, species)`; `Backend` protocol (`name`, `decide(queries) -> [n, actions of the species]`); `batch_actions` (one species per batch); `normalise` |
| `backends/random_policy.py` | Uniform brain | `RandomBackend` |
| `backends/rule_based.py` | Keyword brain | `RuleBasedBackend`; `default_logits` (`prey_logits`, `predator_logits`), `gene_weight`; `CONDITIONS` (prey), `PREDATOR_CONDITIONS` |
| `backends/ollama_policy.py` | LLM brain | `LLMPolicyBackend` (one prompt per species: `prompt_path`, `predator_prompt_path`; modes points / table / ksample / logprobs, caches, workers, `from_config`); `TeacherBackend` (alias); `teacher_prompt`, `table_prompt`, `points_schema(actions)`, `points_to_probs`, `logprobs_to_probs` |
| `backends/factory.py` | Name → brain | `make_backend(name, cfg, strict=False)` (strict: a failed LLM call raises instead of a one-off uniform answer; failures are never cached); `make_rewriter(cfg, check=False)` → `(llm, model)` for mutation, or `(None, None)` when mutation is off; with `check=True` it raises `MutatorUnavailable` if Ollama doesn't answer |
| `backends/laya_backend.py` | Laya brain (parked) | `LayaBackend` |
| `llm/ollama_client.py` | Ollama HTTP client | `OllamaClient` (`chat`, `chat_json`, `chat_raw`, `embed`, `models`, `digest`, retries, sqlite cache, base options, `think`); `client_from_config(cfg)`; `make_rewriter(client, model, temperature)` → `llm(prompt, seed)` |
| `cache.py` | Persistent cache | `KVCache` (sqlite key → JSON value); `make_key(*parts)` (sha256 of the JSON) |
| `metrics.py` | Measurement | `entropy`, `jsd`, `mi_genome`, `mi_obs`, `behaviour_distance`, `directed`, `spearman`, `locality`, `shannon_diversity`, `bootstrap_ci` |
| `eventlog.py` | Run logs | `EventLog` (`events.jsonl` with a running hash, `stats.csv`, `flush`); `replace_file` (a rename that waits out a reader on Windows) |
| `render.py` | ASCII map | `ascii_map(world, agents, predators)` (prey by action letter, predators as `P`), `LEGEND`, `ACTION_CHAR` |
| `progress.py` | Job progress files | `Progress` → `logs/<job>.progress.json` (`deadline` for time-boxed jobs; states running, done, stopped, failed); `keep_awake()` (asks Windows not to sleep while a job runs) |

### Using the package from Python

```python
from promptevo.config import load_config
from promptevo.backends.factory import make_backend
from promptevo.sim import Simulation

cfg = load_config("small")                       # base.yaml + configs/small.yaml
sim = Simulation(cfg, make_backend("rule_based", cfg), seed=7, out_dir="results/runs/mine")
summary = sim.run(2000)                          # also writes the files listed in 03 §12
print(summary["pop_final"], summary["max_gen"])  # prey
print(summary["predators"]["pop_final"], summary["predators"]["kills"])
```

Overrides without editing YAML:

```python
cfg = load_config("small", overrides={"world": {"food_regrow_p": 0.0005},
                                      "evolution": {"p_mut": 0.0}})
```

## 3. Configuration

`configs/base.yaml` holds every default. A **profile** (`--profile small` or
`full`; `smoke_run` defaults to `full` since 2026-10-07, the other scripts to `small`) is merged over it, then optional
**world overlays** from `configs/worlds/`, then overrides.

| File | Content |
|---|---|
| `base.yaml` | all defaults (the full-size Lab 1 world) |
| `small.yaml` | 48 × 48 world; 24 prey at start, cap 40; 4 predators at start, cap 6 (tests and quick checks) |
| `full.yaml` | empty: the defaults are the full size |
| `worlds/terrain_preview.yaml` | noise terrain: 15 % water, 10 % mountains, food regrowth 0.001 |

### `seed`

| Key | Default | Meaning |
|---|---|---|
| `seed` | 1234 | default seed for the simulation and the world |

### `world`

| Key | Default | Meaning |
|---|---|---|
| `width`, `height` | 96, 96 (small: 48, 48) | grid size in cells (64 × 64 until 2026-10-07) |
| `water_fraction` | 0.0 | share of cells that are water (Lab 1: none) |
| `mountain_fraction` | 0.0 | share of cells that are mountains (Lab 1: none) |
| `noise_octaves` | [16, 8, 4] | cell sizes of the heightmap noise (used only with water or mountains) |
| `min_walkable_connected` | 0.60 | minimum share of the map in the largest walkable region, else regenerate |
| `food_initial_fraction` | 0.08 | chance that a walkable cell starts with food |
| `food_regrow_p` | 0.0007 | chance per empty walkable cell per tick to grow food |
| `food_water_bonus` | 2.0 | regrowth multiplier near water |
| `water_bonus_radius` | 3 | "near water" distance in cells |

### `perception`

| Key | Default | Meaning |
|---|---|---|
| `bands` | [1, 4, 10] | distance bands in cells for both species: adjacent ≤ 1, close ≤ 4, medium ≤ 10, far ≤ vision ([03 §5](03-world-and-simulation.md#5-perception-what-an-animal-knows)); three increasing values below the vision |
| `partner_range` | 20 | cells within which an animal sees whether the nearest other animal of its kind is ready to mate (4 until 2026-10-07) |

### `agents` (the prey)

| Key | Default | Meaning |
|---|---|---|
| `init_pop` | 68 (small: 24) | prey animals at tick 0 |
| `floor` | 10 | minimum population; newcomers fill the gap |
| `cap` | 135 (small: 40) | maximum population; no births above it |
| `vision` | 20 | perception radius in cells (12 until 2026-10-07; `near` 3 was removed) |
| `energy_max` | 100 | energy cap |
| `energy_start` | 60 | energy of founders and newcomers |
| `cost_base` | 0.7 | energy cost per tick |
| `cost_move` | 0.5 | extra cost per cell moved |
| `cost_rest` | 0.2 | cost of a resting tick (replaces `cost_base`) |
| `speed` | 1 | cells per tick when running (`flee`); every other move walks one cell ([03 §4](03-world-and-simulation.md#4-animals)) |
| `stamina_max` | 60 | full stamina: cells an animal can move before it must stop, one point per cell |
| `stamina_regen` | 2 | stamina back per tick without moving |
| `cost_regen` | 0.3 | extra energy per tick without moving, until stamina is full |
| `stamina_low`, `stamina_high` | 20, 40 | thresholds of the low / medium / high stamina levels |
| `eat_gain` | 25 | energy per food item |
| `maturity` | 150 | age at which an animal becomes adult |
| `max_age` | 1500 | age at which it dies |
| `mate_energy` | 50 | minimum energy to mate |
| `child_energy` | 40 | energy of a newborn, paid by the parents |
| `energy_low`, `energy_high` | 30, 70 | thresholds of the low / medium / high energy buckets |
| `wander_turn_p` | 0.25 | turning chance per tick of the random walk of an animal whose chosen action has nothing to act on in sight |

### `predators`

Genetic animals since 2026-10-07 ([03 §3](03-world-and-simulation.md#3-predators)).
They have every key of `agents` except `eat_gain`, with the same values unless
listed here:

| Key | Default | Meaning |
|---|---|---|
| `init_pop` | 14 (small: 4) | predators at tick 0 |
| `floor` | 3 | minimum number of predators; newcomers from the predator founder pool fill the gap |
| `cap` | 34 (small: 6) | maximum number of predators |
| `vision` | 20 | perception radius in cells |
| `speed` | 2 | cells per tick when hunting |
| `stamina_max` | 30 | half the prey's |
| `stamina_low`, `stamina_high` | 10, 20 | thresholds of the stamina levels |
| `kill_p` | 0.1 | chance that a strike kills; a hunting predator strikes when it is next to its prey after its move |
| `kill_gain` | 60 | energy from one kill |
| `digest_ticks` | 50 | ticks a predator stays still after a kill, without deciding; after a carcass portion, in proportion to the energy (25 ticks) |
| `carcass_portions` | 2 | portions a kill leaves for other predators, one each (0: no carcass) |
| `carcass_gain` | 30 | energy from one portion |
| `carcass_ticks` | 100 | ticks before a carcass rots away |

The scripted predators' keys (`count`, `chase_radius`, `turn_p`,
`rest_after_kill`) were removed on 2026-10-07.

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
| `mutation_prompts` | prompts/mutate_v3.txt | the mutation instructions, one drawn at random per mutation ([04 §5](04-genome-and-evolution.md#5-mutation)); `mutate_v2.txt` until 2026-10-07 |
| `temperature` | 1.2 | sampling temperature of the mutator LLM |
| `max_words` | 12 | maximum length of a gene in words (longer answers are rejected; replaces `max_action_words` and `max_temperament_words` since 2026-10-07) |
| `max_changed_words` | 3 | a mutant may differ from its parent in at most this many words (null: any size; since 2026-10-08) |
| `vocabulary` | data/world_vocabulary_v1.txt | a mutant may use only these words, with simple endings (null: any word; since 2026-10-08) |
| `mutation_tries` | 5 | a rejected answer is drawn again, up to this many attempts (1 before 2026-10-08) |
| `max_changed_words` | 3 | a mutant may differ from its parent in at most this many words (null: any size; since 2026-10-08) |
| `vocabulary` | data/world_vocabulary_v1.txt | a mutant may use only these words, with simple endings (null: any word; since 2026-10-08) |
| `mutation_tries` | 5 | a rejected answer is drawn again, up to this many attempts (1 before 2026-10-08) |

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
| `prompt` | prompts/teacher_v5.md | prompt template of the prey |
| `predator_prompt` | prompts/predator_v3.md | prompt template of the predators |

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
`make_obs`) accept `--profile` (default `full` for `smoke_run`, `small` for the others); `e0_probe_ollama`, `status`
and `peek` don't read it.

### Running the world

| Script | Purpose | Options |
|---|---|---|
| `smoke_run` | Run a simulation, print ASCII snapshots and a summary, write the run files | `--profile` (`full`, 64 × 64; `small` 48 × 48), `--ticks` (5000), `--backend` (config default `rule_based`; `random`, `llm`), `--seed`, `--out` (`results/runs/smoke`), `--snapshots` (3; 0 = none), `--world` (overlay from `configs/worlds/`), `--no-mutation` (crossover only, no model needed), `--minutes` (stop after N minutes of wall-clock time). Stop cleanly with `logs/run_<name>.stop`; run the same command again to resume ([03 §12](03-world-and-simulation.md#12-what-a-run-writes-to-disk)) |

The summary has a line for the prey and one for the predators (population,
births, newcomers, deaths, kills, lifespan, generations, action shares). With
`--backend llm` it also prints `llm_calls`, failures and the memo hit rates. Genes mutate through the mutator model with every brain, so
Ollama must be running; if it doesn't answer, the run stops at the start with a
clear message. `--no-mutation` runs without it.

### Measuring the brain

| Script | Purpose | Options |
|---|---|---|
| `e0_probe_ollama` | Facts about an Ollama model: structured output, determinism, logprobs, speed, mutator samples → `results/e0_ollama.md` | `--teacher` (decision model, required), `--mutator`, `--embed`, `--host`, `--num-ctx` (4096; 0 = server default), `--think` (off / on / default) |
| `teacher_gate` | Decision-model gate: contrast pairs + 10 founders + 10 random-text genomes × n observations → `results/teacher_gate.md/.json` | `--modes` (points,table), `--n-obs` (24), `--workers`, `--model`, `--prompt` |
| `e1_sensitivity` | The full gene-sensitivity suite (E1) with any brain → `results/e1_<tag>.md/.json` | `--backend`, `--tag`, `--mode`, `--workers`, `--obs`, `--chunk`, `--style`, `--placement` (Laya) |
| `mutation_test` | Pure mutation, no selection: every founder sentence of both species × seeds × temperatures (variety, edit size, length, keyword-brain effect, attempts and rejections) and lineages mutated step after step; meaning: usable rule (gemma4 judge), world words, only world words → `results/<tag>.md/.json`. `gene_timeline` compares a run with the mutation test made with the run's instructions | `--rules` (current, or old: the rules of 2026-10-02), `--temps` (0.9,1.2,1.5,2.0), `--seeds` (8), `--steps` (30), `--lineages` (all, or first: one per prey slot), `--no-judge`, `--max-changed`, `--tries`, `--workers` (4), `--model`, `--tag` |
| `make_obs` | Rebuild the prey observation set (`data/observations_v2.jsonl`) from synthetic situations + the most frequent ones of a rule-based run | `--ticks` (3000), `--out` |

### Reading a run

| Script | Purpose | Options |
|---|---|---|
| `gene_report RUN [RUN …]` | Ranks genes by the fitness of their carriers: offspring of the animals that carried them and died, each divided by the mean of the animals that died in the same 5 000 ticks. Genes are grouped by what the keyword brain reads in them (strength, action, conditions), since mutation spreads the population over thousands of texts; exact texts are ranked too. Also: share killed by predators, frequency over time, predator kills per 1 000 animal-ticks, lineage of the most common genes. With several runs (seeds) the marks use all of them: ★ clearly above average, ▲ steady leader (above average in every run), ✗ clearly below → `results/<tag>_genes.md/.json`; method in [10 §2](10-natural-selection-runs.md#2-measuring-which-genes-did-best). It takes the slots from the run itself (allele ids such as `eat:3`), so runs made before 2026-10-07, with 10 slots, stay readable; a slot the genome no longer has reads as "not read (slot removed)" | `--min-carriers` (100), `--every` (10000 ticks), `--tag`, `--species` (`prey`; `predator` reads the predators' genes, default tag `<run>_predator`) |
| `gene_timeline RUN` | How the genes develop during one run, from its files (also a run still going): the gene pool per checkpoint and slot (distinct genes, effective number, leader, mutants, mutation depth, length, world words, overlap); genes that reached 25 % with their lineages; mutants produced vs spread vs alive at the end next to the mutation test; gene dropping (genes handed down the real family tree at random) for selection vs drift, with the number of sweeps that inheritance alone gives; families; behaviour per window; optional `--judge` (gemma4:12b says whether a gene still gives a usable rule, `prompts/judge_sense_v1.txt`) → `results/<tag>_timeline.md/.json/.csv/.html`. Like `gene_report`, it takes the slots and actions from the run itself | `--every` (500 ticks), `--window` (2000), `--drops` (500), `--tag`, `--judge`, `--judge-max` (3000), `--test TEXT --test-from T` (a gene singled out at tick T, tested on what came after), `--species` (`prey` or `predator`) |
| `gene_swap RUN --tick T --slot S --texts …` | Does one gene change behaviour? Puts each text in slot S of the genomes alive at tick T (optionally only those `--carrying` a text) and asks the LLM brain about the E1 situations relevant to the slot; prints the mean action probabilities per text and the change with a 95 % interval. Prey slots only; needs a run made with the current 5 prey genes (since 2026-10-07) | `--carrying`, `--genomes` (8), `--max-calls` (500) |

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
| `data/founder_pool_v2.json` | prey: 5 loci × (4 founder alleles + neutral) ([04 §3](04-genome-and-evolution.md#3-the-founder-pool)) |
| `data/predator_founder_pool_v2.json` | predators: 4 loci × (4 founder alleles + neutral) |
| `data/contrast_alleles_v2.json`, `data/predator_contrast_alleles_v2.json` | pro / anti sentence per locus, for directed tests only |
| `data/control_alleles_v1.json` | 20 shuffled-word + 20 irrelevant sentences for random-text genomes (both species) |
| `data/observations_v2.jsonl` | 48 prey observations with distance bands (32 synthetic + 16 frequent in a rule-based run), with directed-test tags |
| `data/observations_v1.jsonl` | the near / far observation set of the code before 2026-10-07 (E1 results in [06](06-experiments-and-results.md)); today's brains can't read it |
| `prompts/teacher_v5.md` | decision prompt of the prey ([05](05-decision-backends.md#the-prompt)) |
| `prompts/predator_v3.md` | decision prompt of the predators |
| `prompts/mutate_v3.txt` | the 9 mutation instructions (small edits of the rule), one per line ([04 §5](04-genome-and-evolution.md#5-mutation)) |
| `prompts/mutate_v2.txt` | the 16 random-change instructions used from 2026-10-02 to 2026-10-07 (`mutation_test --rules old`) |
| `data/world_vocabulary_v1.txt` | the words a mutant may use (`evolution.vocabulary`), with comments |
| `prompts/novel_v1.md` | prompt for brand-new alleles (parked dataset pipeline) |

The 10-slot files of before 2026-10-07 (`founder_pool_v1.json`,
`contrast_alleles_v1.json`, `prompts/teacher_v1.md`) and the prompt of the
12-cell vision (`prompts/teacher_v2.md`) were removed; they remain in the git
history.

## 6. Tests

`cd prototype && pytest -q` runs 75 offline tests in about 30 s, with no model
needed. LLM calls are replaced by small fake servers. Two marked tests talk to
real models: `pytest -m ollama` (needs Ollama and `policy.model`) and
`pytest -m laya` (parked).

| File | What it checks |
|---|---|
| `test_core.py` | config merge, random streams, cache round-trip, progress files |
| `test_world.py` | flat Lab 1 world, terrain fractions and connectivity, nobody enters blocked cells, unambiguous map symbols |
| `test_behaviour.py` | flee increases distance, eating gains energy, eat with no food in sight → a random walk, counted invalid; distance bands and the same vision for both species; a partner's readiness seen 15 cells away (and not with `partner_range` 4); text styles of both species; rule-based directed tests and gibberish, the keyword brain reads predator genes |
| `test_stamina.py` | moving costs stamina and energy, standing still brings stamina back at a price until it is full; no stamina, no move (a predator with 1.5 points runs 1 cell); a carcass feeds two other predators, not the killer, then rots; no carcass with 0 portions; stamina and carcasses sensed and written; the keyword brain rests when out of breath, goes for carcasses, reads "tired" as out of breath |
| `test_predators.py` | hunt runs 2 cells, strikes, feeds and leaves a carcass; hunt with no prey in sight searches; a kill removes the prey (cause, killer id) and the predator digests without moving or deciding; predators breed with their own genes; newcomers below each floor; prey move at most 1 cell per tick and predators 2, never beyond their stamina; one registry holds both species |
| `test_genome.py` | allele pools, registry dedup and genome keys, crossover |
| `test_evolution.py` | guards and instruction lists, size and vocabulary checks (every founder gene passes), a rejected answer drawn again, the mutator sends only the instruction and the gene (fake LLM), rejected answers, no LLM → no mutation, determinism, population bounds, shuffled control (each species uses its own genomes), no mutation → no new alleles, decisions per action in `stats.csv` (both species) |
| `test_metrics.py` | entropy, JSD, mutual information, directed ΔP, Spearman |
| `test_gene_report.py` | `gene_report` on a short run: fitness averages to 1.00 in every slot, frequencies add up to the population; an unfinished run is rebuilt from its events |
| `test_gene_timeline.py` | a hand-made run with known numbers (shares, sweep timing, depth, families, gene dropping); invariants on a short unfinished run (slot shares add up, lineages end at founder texts, dropped shares of a slot add up to 1); sweep counts under inheritance alone; `--test` start tick; the judge with a fake model (budget, cache); a run with other slots (10 before 2026-10-07) is read with its own slots; `--species predator` follows the predators and keeps the two species apart |
| `test_gene_swap.py` | `gene_swap` with the keyword brain: only the slot changes, "Always eat." beats "Never eat." |
| `test_long_run.py` | stop file, then resume by replay (same events, no model call asked twice); a failing model stops the run cleanly; `run_info.json`; progress time box |
| `test_llm_policy.py` | table mode vs points, malformed rows, persistent caches (incl. an empty cache file), a failed call is never cached and strict brains raise, API key header, factory (incl. mutation off and Ollama down), logprobs fallback; predator queries get their own prompt, actions and cache key, and one batch holds one species |
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
| A brain | a class with `name` and `decide(queries) -> [n, k]` in `backends/`; register it in `factory.make_backend` | a batch holds one species (`base.batch_actions`); return rows that sum to 1 in that species' action order |
| A sense | a field in `perception.Observation` or `PredatorObservation`, computed in `sense`, rendered in `obs_text.render` | keep values discrete (cache hits); update the rule-based brain if it should react |
| An action | a name in the species' action tuple in `species.py` (one gene per action, so this also adds a locus), an executor in `actions.EXECUTORS`, a line in the species' prompt (`prompts/teacher_v5.md` or `predator_v3.md`), alleles in its founder and contrast files, keywords in `rule_based.ACTION_WORDS` (and a default in `prey_logits` or `predator_logits`), a relevance tag in `perception.RELEVANT_TAG`, a letter in `render.ACTION_CHAR` (prey) | the action list is part of the JSON schema (`points_schema(actions)`); the founder and contrast files need the new slot; the rule-based brain and E1 fail with a `KeyError` without the keywords and the relevance tag; without a letter the map shows `?` |
| A mutation instruction | a line in `prompts/mutate_v3.txt` (and words it needs in `data/world_vocabulary_v1.txt`) | it is drawn at random like the others; measure it with `experiments.mutation_test` |
| A crossover scheme | a function like `crossover_uniform`, called in `Simulation._birth` | keep loci homologous |
| A world feature | `world.py` (generation, `regrow_food`, movement) plus keys in `world:` | keep randomness in the world's streams; add a test |
