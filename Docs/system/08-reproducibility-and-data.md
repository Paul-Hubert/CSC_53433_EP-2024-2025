# 08 — Reproducibility & data

Seeds, RNG streams, caches, the event log, run outputs, config loading and the helper scripts (`rng.py`, `cache.py`, `eventlog.py`, `config.py`, `progress.py`, `experiments/status.py`, `peek.py`). Status ✅.

## Named RNG streams

`Streams(seed).get(name)` returns a `numpy.random.Generator` seeded with
`SeedSequence([seed, stable_int(name)])`, where `stable_int` is the first 4 bytes
of `sha256(name)`. Each name is an independent stream; drawing more from one never
shifts another. `fresh(name)` returns a new generator from the start of that
stream without disturbing `get()`. No module uses the global `random` or
`np.random` state (prototype `CLAUDE.md` rule).

### Streams used by the simulation

| Stream | Seeded by | Drives |
|---|---|---|
| `world` | world seed | value-noise height map (all retries), initial food |
| `predators` | sim seed | predator start cells and headings, predator moves (`step_toward` ties / side steps, `step_heading`), kill draws in `_predation` |
| `agents` | sim seed | founder and immigrant genomes (`sample_founder` / `sample_control`), spawn cells, initial headings, the C3 shuffled-genome pick |
| `sampling` | sim seed | sampling the action from the decision distribution |
| `actions` | sim seed | per-tick agent execution order (permutation) and all executor randomness: wander turns, `step_toward` ties and side steps, attack success |
| `mutation` | sim seed | crossover picks, per-locus `p_mut` draws, operator choice, operator internals, LLM style choice, mutation seeds |
| `food` | sim seed | food regrowth |

Plan 08 §A4 lists `world, predators, agents, sampling, mutation`; the code adds
`actions` and `food`.

### Streams outside the simulation

| Where | Generator |
|---|---|
| `experiments/e1_sensitivity.py`, `teacher_gate.py`, `make_obs.py` | `np.random.default_rng(cfg.seed)` (genome sets, observation picks) |
| `experiments/make_mutants.py` (parked) | `default_rng(cfg.seed + 101)` |
| `metrics.bootstrap_ci` | `default_rng(0)` unless an `rng` is passed |
| LLM backend | fixed `seed = 0` in request options (points/logprobs/table); ksample uses seeds 0…k−1 |
| LLM rewriter | the mutation seed drawn from the `mutation` stream (+ attempt number) |

### World seed vs simulation seed

`Simulation(cfg, backend, seed=None, world_seed=None)`: both default to
`cfg.seed` (1234). The **world seed** only fixes terrain and initial food; the
**sim seed** fixes everything else, including predator start positions. Plan 08
§A10 runs E4 with one world and several sim seeds: pass `seed=` and leave
`world_seed` unset (e.g. `smoke_run --seed N`). No script exposes `world_seed`.

### What determinism covers

Same config + same seeds + same backend answers → identical event log
(`tests/test_evolution.py::test_simulation_is_deterministic` compares
`events_sha` over 600 ticks; a different seed gives a different digest). With
the LLM brain, "same backend answers" holds when answers come from the caches;
fresh answers depend on the server honouring `seed` + temperature 0, which is
unverified (probe S1.3, item 3; doc 05 notes seeds are not guaranteed on cloud).

## sqlite caches

`cache.KVCache(path)` is a one-table sqlite store (`kv(k TEXT PRIMARY KEY, v TEXT)`,
values JSON, WAL mode on disk, thread-safe with a lock, `hits`/`misses`
counters; `":memory:"` when no path). Keys come from
`make_key(*parts) = sha256(json.dumps(parts, sort_keys=True, ensure_ascii=False, default=str))`.
All files live in `paths.cache_dir` (`prototype/cache/`, git-ignored). Never
delete caches without asking: they hold paid-for model answers.

| File | Owner | Key parts | Value |
|---|---|---|---|
| `cache/policy.sqlite` | `LLMPolicyBackend.from_config` | `"policy"`, model, digest, prompt_id, mode (table → points), style, genome_key, situation text | probabilities `[7]` |
| `cache/ollama.sqlite` | `client_from_config` | `"chat"`, model, digest, messages, schema, options, extra | raw response content (string); for JSON calls only replies that parse as a JSON object are stored |
| (same file) | `OllamaClient.embed` | `"embed"`, model, digest, texts | embedding vectors |
| `cache/laya.sqlite` 💤 | `LayaBackend.from_config` | `"laya"`, client_id, placement, style, genome_key, situation text | `{p, exact}` |

The client cache doubles as the course-wide **mutation cache**: an LLM rewrite
is a `chat()` call whose options contain the mutation seed, so the same
`(text, style, seed, model)` is answered from cache in any later run. `chat_raw`
(logprobs mode) is not cached at client level; its parsed result is cached at
policy level.

## Event log (`events.jsonl`)

`EventLog.event(kind, t, **fields)` writes one JSON line with keys sorted
(`sort_keys=True`, UTF-8) and feeds the same line into a running sha256.
Events are written only when `out_dir` is set, but the digest is always kept.

| `kind` | Fields (besides `kind`, `t`) |
|---|---|
| `founder` | `id`, `genome` (10 allele ids) — initial population, `t = 0` |
| `immigrant` | `id`, `genome` — floor spawns |
| `birth` | `id`, `parents` (ids), `gen`, `genome`, `mutations` (list of `{locus, parent, child, op, text}`) |
| `death` | `id`, `cause` (`starvation` / `attacked` / `predator` / `old_age`), `age`, `gen`, `food`, `offspring`, `steals` |

There is no separate mutation event: mutations are inside `birth`. Allele ids
resolve to texts through `alleles.jsonl` of the same run.

**Running digest.** `EventLog.digest()` is the sha256 hex of all event lines in
order; `summary.json` stores the first 16 characters as `events_sha`. Two runs
are considered identical when their `events_sha` match.

## `stats.csv`

One row every `sim.stats_every` (100) ticks; the header comes from the first row.
Counters are cumulative since `t = 0`.

| Column | Meaning |
|---|---|
| `t` | tick (after increment) |
| `pop` | living agents |
| `mean_energy`, `mean_gen` | over living agents (2 decimals) |
| `max_gen` | highest living generation |
| `births`, `immigrants` | cumulative |
| `deaths_starve`, `deaths_pred`, `deaths_age`, `deaths_attacked` | cumulative deaths by cause |
| `decisions`, `backend_queries`, `invalid` | cumulative decision counters |
| `alleles` | registry size |

## Run output files

| File | Format | Content |
|---|---|---|
| `events.jsonl` | JSONL | events above |
| `stats.csv` | CSV | stats rows above |
| `alleles.jsonl` | JSONL | every `Allele` (`id, locus, text, origin, parent_id, operator, model, seed`) |
| `run_info.json` | JSON | provenance: seeds, profile, merged config, backend description (model, digest, prompt id…), mutator model, founder pool, git commit, versions; written at start, completed at `finish()` ([03](03-simulation-loop.md)) |
| `summary.json` | JSON | `Simulation.summary()` ([03](03-simulation-loop.md)) |
| `final_population.json` | JSON | `[{"id", "gen", "genome"}]` for living agents |

Experiment results go to `paths.results_dir`: e.g. `results/e1_<tag>.json` + `.md`,
`results/teacher_gate.md` + `.json`, `results/e0_ollama.md`. Run folders under
`results/runs/` are git-ignored; small result files in `results/` are committed.

## Config loading

`config.load_config(profile="small", overrides=None, extra_files=None) → Cfg`:

```text
data = base.yaml
data = deep_merge(data, configs/<profile>.yaml)    # if the file exists
for f in extra_files: data = deep_merge(data, f)
data = deep_merge(data, overrides)
data["profile"] = profile
```

- `deep_merge` recurses into dicts and replaces everything else (lists are
  replaced, not merged).
- `Cfg` is a dict with attribute access (`cfg.agents.vision`); nested dicts are
  wrapped too, so `cfg.evolution.operators.get("llm_rewrite")` also works.
- Note the default profile is **`small`** both in `load_config` and in every
  script's `--profile`.
- `config.resolve(path)`: relative paths in configs are relative to
  `prototype/`, not to the current directory.

Profiles and every key: [10](10-configuration-reference.md).

## Helper scripts

| Script | What it does | Status |
|---|---|---|
| `python -m experiments.status [logs_dir]` | one line per `logs/*.progress.json`: job, state (`DEAD?` if the pid is gone), done/total, ETA, last update, error | ✅ |
| `python -m experiments.peek FILE [-n 5] [--width 200]` | CSV: row count, columns, first n rows, last row · JSONL: line count, keys of line 1, first n lines · other: char count and the start | ✅ |
| `promptevo.progress.Progress(logs_dir, job, total)` | atomic writes of `{job, pid, state, done, total, elapsed_s, eta_s, updated, …}`; `update`, `finish`, `fail` | ✅ |

Working rule (prototype `CLAUDE.md`): never print whole JSONL/CSV/log files; use
`peek`. Long jobs run under `nohup … > logs/X.log 2>&1 &` and are checked with
`status`.
