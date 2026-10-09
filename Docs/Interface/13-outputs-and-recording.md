# 13 — Outputs and recording

A run writes enough to rebuild its whole gene history afterwards, and its
files follow the prototype's formats so that the prototype's analysis tools
(`gene_report`, `gene_timeline`, `mutation_list`) can read Unity runs.

## 1. Rules

- **OUT-01 (MUST)** Each run writes into its own folder.
- **OUT-02 (MUST)** Events are written in tick order, one JSON object per line,
  keys sorted. Every event has `kind`, `t` (tick) and `id` (the animal), and the
  species id in `species`.
- **OUT-03 (MUST)** The events are enough to rebuild every genome over time:
  every founder, newcomer and birth carries its genome, every death its cause.
- **OUT-04 (MUST)** No API key or other secret is ever written to an output, a
  log, a scene, a prefab or a configuration asset. Keys come from environment
  variables or the user's local preferences.
- **OUT-05 (SHOULD)** A compatibility mode writes two-species worlds exactly as
  the prototype does (prey events without a `species` field, predator events
  with `"species": "predator"`, prey loci without a prefix).
- **OUT-06 (SHOULD)** The recorder is a module; other sinks (live graphs in the
  editor, a database) receive the same events.

## 2. Files

| File | Content |
|---|---|
| `events.jsonl` | one line per event (§3) |
| `stats.csv` | one row every `statsEvery` ticks (reference 100), §4 |
| `alleles.jsonl` | every allele seen, all species: `id`, `locus`, `text` (or `value` for number genes), `origin`, `parent_id`, `operator`, `model`, `seed` |
| `final_population.json` | the living animals at the end: `id`, `gen`, `genome` (allele ids), `species` |
| `summary.json` | totals per species and for the run (§5) |
| `run_info.json` | what ran: scene or configuration snapshot, git commit, Unity version, brains (model names, digests or pinned revisions, hosts), mutator (API, host, model), seeds, wait mode, act order |

## 3. Events

| Kind | Fields |
|---|---|
| `founder` | `id`, `genome` |
| `immigrant` | `id`, `genome` (a newcomer below the floor) |
| `birth` | `id`, `parents`, `gen`, `genome`, `litter` (its litter's size), `mutations`: a list of {`locus`, `parent` (allele id), `child` (new allele id), `prompt` (instruction number, LLM operator) or `operator`, `text` or `value`}; a hatched egg also has `laid` (the conception tick) |
| `death` | `id`, `cause` (`starvation`, `old_age`, `predator`/`killed`, `migrated`, or a module's cause), `age`, `gen`, `food` (meals), `offspring`, and `killer` for a kill |
| `egg_lost` (Unity) | `id` of the egg, `parents`, `reason` |
| `species_created` (Unity) | `species`, `name`, `parent_species` |

## 4. Statistics rows

One row per `statsEvery` ticks, columns `t` then, per species with the prefix
`<species>_` (the prototype: none for the prey, `pred_` for predators):

| Column | Meaning |
|---|---|
| `pop` | living animals |
| `mean_energy`, `mean_stamina` | means over the living animals |
| `exhausted` | cumulative animal-ticks that began without stamina for any movement |
| `mean_gen`, `max_gen` | generations of the living animals |
| `births`, `immigrants` | cumulative |
| `deaths_starve`, `deaths_pred` (killed), `deaths_age`, `deaths_migrated` | cumulative deaths by cause |
| `decisions`, `backend_queries`, `invalid` | cumulative decisions, brain queries after the memo, searching choices |
| `act_<action>` | cumulative decisions per action |
| `portions` | cumulative carcass portions eaten (hunters) |

and `alleles`, the number of alleles registered (all species).

## 5. Summary

Per species: final population, births, newcomers, hatched eggs, deaths by cause,
mean lifespan, maximum generation, decisions, brain queries, memo hit rate,
brain time, searching rate (`invalid_rate`), share of animal-ticks out of breath,
share of each action; for hunters also kills and carcass portions. For the run:
ticks, seed, brains, mutation statistics (attempts, successes, calls, rejections
by reason), number of alleles, the events hash (first 16 hex digits), why the run
stopped, wall-clock minutes, model calls not answered by a cache, failures.
