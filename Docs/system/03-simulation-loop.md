# 03 — Simulation loop

What one tick of `Simulation.step()` does, in order, and what a run writes (`prototype/promptevo/sim.py`). Status ✅.

## Construction

`Simulation(cfg, backend, seed=None, world_seed=None, out_dir=None, registry=None,
pools=None, rewriter=None, rewriter_model=None, progress=None)`

1. `seed` (default `cfg.seed`) creates the simulation streams; `world_seed`
   (default `cfg.seed`) creates the world stream. See [08](08-reproducibility-and-data.md).
2. Builds the `World`, an `AlleleRegistry`, `AllelePools` (loads the three allele
   files), a `Mutator` (with the optional LLM `rewriter`), an `EventLog(out_dir)`,
   an empty decision memo and the counters.
3. Spawns `agents.init_pop` founders (30; small 24) at random walkable cells with
   `energy_start` (60), generation 0 and a random heading. Each is logged as a
   `founder` event at `t = 0`.

## Tick order

```text
step():
 1. decisions      t % D == 0 → decide(all agents)        else → decide(newcomers only)
 2. act            random permutation of agents; run EXECUTORS[action] once; pay energy
 3. breed          _breed(): pair or clone agents that chose mate (cap applies)
 4. predators move world.move_predators(agents)
 5. predation      _predation(): kills → "predator" deaths, removed now
 6. ageing/death   age += 1; energy ≤ 0 → "starvation"; else age > max_age → "old_age"; removed
 7. food           world.regrow_food(stream "food")
 8. floor          while population < floor: spawn an immigrant from the founder pool
 9. clock          t += 1; every stats_every ticks → one stats.csv row
```

### 1. Decisions (lockstep, every D ticks)

- `D = sim.decision_period` (4). On ticks where `t % D == 0`, **every** agent
  decides in one backend batch. The chosen action is then executed for the next
  D ticks (re-run each tick, so targets are re-acquired each tick).
- On other ticks, only **newcomers** decide: agents with `action is None`, i.e.
  children born and immigrants spawned since the last decision. They act at once
  instead of idling until the next decision tick.
- `decide(agents)`:
  1. For each agent, `sense()` builds the `Observation` (page [04](04-agents-perception-actions.md)).
  2. `gk = registry.genome_key(genome)`; memo key = `(gk, obs)`.
  3. Keys already in the memo count as `memo_hits`. New keys are collected once
     each into `pending` as `Query(gk, genes, obs)`.
  4. If anything is pending, **one** `backend.decide(list(pending))` call returns
     `probs[len(pending), 7]`; each row is stored in the memo. The wall time is
     added to `backend_time`.
  5. Each agent samples its action from its memo row (below), and its per-period
     flags `invalid`, `attacked`, `bred` are reset to `False`.

### The decision memo

`self.memo: dict[(genome_key, Observation) → probs]` lives for the whole run.

- The backend is only asked about a `(genome, situation)` pair once per run.
  With a few hundred distinct observations and inherited genomes, this saves
  most calls: memo hit rate ≈ 0.75 with `rule_based` on the small profile
  (STATUS, 2026-09-28); doc 05 §0 quotes 60–75 %.
- Consequence: within a run, an agent's *distribution* for a given situation is
  fixed; only the sampled action varies. With the LLM brain this also means one
  bad answer is reused for the rest of the run.
- The key uses the `Observation` object, not the rendered text, so the memo is
  style-independent. The backends' own persistent caches key on the text (page [07](07-decision-backends.md)).
- `memo_hits` counts hits against *earlier* batches only. Duplicate keys inside
  one batch are neither queries nor hits, so `memo_hit_rate` slightly
  understates reuse.

### Sampling with temperature

```text
p = memo[key]
if τ ≠ 1:  p = p^(1/τ) / Σ p^(1/τ)          τ = sim.sampling_temperature (1.0)
action = ACTIONS[ rng_sampling.choice(7, p=p) ]
```

τ < 1 sharpens towards the most likely action; τ > 1 flattens. Sampling uses
the `sampling` stream.

### 2. Act and energy accounting

Agents run in a fresh random order each tick (`actions` stream permutation), so
nobody always gets the food first. For each agent:

| Case | Energy change this tick |
|---|---|
| action `rest` and did not move (always, `rest` never moves) | − `cost_rest` (0.2) |
| any other action, did not move (eating in place, adjacent mate/attack/follow) | − `cost_base` (0.7) |
| any other action, moved one cell | − (`cost_base` + `cost_move`) = − 1.2 |

Executor-specific changes happen inside the executor: `+eat_gain` (25, capped at
100), attack cost and steal (page [04](04-agents-perception-actions.md)). If the
executor sets `invalid` for the first time this period, the `invalid` counter
increases (counted once per decision, not per tick).

### 3. Breeding resolution

`mate_ready(a)` = `a.age ≥ maturity` (150) **and** `a.energy ≥ mate_energy` (50),
checked *after* this tick's energy costs.

**Sexual mode** (`evolution.sexual: true`, default):

```text
ready = [a in agents (list order) if a.action == "mate" and not a.bred and mate_ready(a)]
for a in ready:
    if population ≥ cap or a.bred: skip
    for b in ready: if b ≠ a, not b.bred, cheb(a, b) ≤ 1 → _birth([a, b]); break
```

Both partners must have **chosen `mate`** and be within one cell (Chebyshev).
Pairing is greedy in list order. `bred` is reset only at the next decision, so
each agent breeds at most once per decision period.

**Asexual mode** (`evolution.sexual: false`, control C7): every agent with
action `mate`, not yet bred and `mate_ready` clones itself (plus mutation),
until the cap is reached.

`_birth(parents)`: genome = `crossover_uniform` (two parents) or the parent's
genome (one), then `Mutator.mutate`; each parent pays `child_energy / n_parents`
(20 each, or 40 alone), gets `offspring += 1` and `bred = True`; the child gets
`child_energy` (40), generation `max(parent generations) + 1`, and is placed on
the first parent's cell. A `birth` event is logged with the mutation list.
Details: [06](06-evolution.md).

**Cap.** `agents.cap` (60; small 40) blocks births only. Immigrants are never
blocked by the cap, but they only arrive below the floor.

### 4–5. Predators and predation

After predators move ([02](02-world.md)), each non-resting predator looks at the
agents on its cell in list order. For the **first** one not already killed, it
draws once from the `predators` stream: with probability `kill_p` (0.3) the
agent is marked `killed` and the predator rests for `rest_after_kill` (20)
ticks. Whether or not the draw succeeds, that predator does not try another
agent this tick. All killed agents are then logged as `predator` deaths and
removed **before** the starvation check.

### 6. Ageing and deaths by cause

Every survivor ages by 1. Then, per agent: `energy ≤ 0` → `starvation`;
otherwise `age > max_age` (1 500) → `old_age`. Death records the age and
lifetime counters (page [08](08-reproducibility-and-data.md)).

| Cause | Trigger |
|---|---|
| `predator` | a predator's kill draw succeeded (step 5) |
| `starvation` | energy ≤ 0 at the end of the tick, including energy lost to an attacker |
| `old_age` | age > `max_age` |

### 7–8. Food regrowth and floor immigration

Food regrows ([02](02-world.md)). Then, while the population is below
`agents.floor` (10), a new agent is spawned exactly like a founder (random
walkable cell, `energy_start`, generation 0) with a genome sampled from the
founder pool — or from the control texts when `evolution.random_founders` is on
(C4). It is flagged `immigrant`, logged as an `immigrant` event and counted.
It decides at the start of the next tick (newcomer rule).

### 9. Clock and stats

`t += 1`. When `t % stats_every == 0` (100), `stats_row()` is appended to
`stats.csv` (first row at `t = 100`).

## Counters

`Counters` (cumulative over the run):

| Field | Meaning |
|---|---|
| `decisions` | actions sampled (one per agent per decision) |
| `backend_queries` | distinct `(genome, obs)` pairs sent to the backend |
| `memo_hits` | decisions answered from earlier batches |
| `invalid` | decisions whose executor fell back to wander |
| `births`, `immigrants` | as named |
| `deaths` | `Counter` by cause |
| `actions` | `Counter` of sampled actions |
| `backend_time` | seconds inside `backend.decide` |

The LLM backend keeps its own counters (`calls`, `answered`, `failures`,
`prefetched`, `logprob_fallbacks`); see [07](07-decision-backends.md).

## Running

- `run(ticks, progress_every=500)` → `step()` × ticks, updating the optional
  `Progress` file, then `finish()`.
- `finish()` builds the summary; if `out_dir` is set it writes `alleles.jsonl`,
  `summary.json` and `final_population.json`, then closes the log.

### Summary fields (`summary()`)

| Field | Meaning |
|---|---|
| `ticks`, `seed`, `backend` | run identity (`backend` = the backend's `name`) |
| `pop_final`, `births`, `immigrants`, `deaths` | population outcome; `deaths` by cause |
| `mean_lifespan` | mean age at death over all deaths (`None` if none) |
| `max_gen` | highest generation among the living |
| `decisions`, `backend_queries`, `memo_hit_rate` | decision load; hit rate = `memo_hits / decisions` |
| `backend_s` | total seconds in the backend |
| `invalid_rate` | `invalid / decisions` |
| `action_share` | share of each action among all decisions |
| `mutations` | per operator `{tried, ok}`, only operators tried at least once |
| `alleles` | registry size (includes the 64 pre-loaded founder, neutral and contrast alleles) |
| `events_sha` | first 16 hex chars of the running sha256 of the event log |

Not recorded in the summary: `world_seed`, the config, the profile, model names
or digests. Keep them next to the run (e.g. the command line) until this is
added.

### Run outputs

Written to `out_dir` (the smoke run uses `results/runs/smoke/`, git-ignored):

| File | Written | Content |
|---|---|---|
| `events.jsonl` | during the run | founder / immigrant / birth / death events |
| `stats.csv` | every `stats_every` ticks | population and counter snapshot |
| `alleles.jsonl` | at `finish()` | every allele in the registry |
| `summary.json` | at `finish()` | the summary above |
| `final_population.json` | at `finish()` | `[{id, gen, genome}]` of living agents |

Formats: [08](08-reproducibility-and-data.md).

## Tuning record

Provisional tuning (2026-09-28, `rule_based`, small profile, 5 000 ticks):
population ≈ 28 (below the cap of 40, food-limited); deaths 294 starvation /
254 predator; lifespan ≈ 300; 24 generations; a random policy collapses and
needs immigrants. Plan 08 S2.6 targets were population 20–40 with turnover,
mean lifespan 300–800 and ≥ 10 generations per 20 k ticks; re-check on small
and full (STATUS › Progress).
