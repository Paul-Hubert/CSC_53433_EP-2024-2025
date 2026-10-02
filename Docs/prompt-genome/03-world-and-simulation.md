# 03 — World and simulation

How the simulated world works, tick by tick. Everything here is implemented in
`prototype/promptevo/` (`world.py`, `perception.py`, `obs_text.py`,
`actions.py`, `sim.py`). Parameter values come from `prototype/configs/base.yaml`
and the two size profiles, `small.yaml` and `full.yaml`.

## Contents

1. [The grid](#1-the-grid)
2. [Food](#2-food)
3. [Predators](#3-predators)
4. [Animals](#4-animals)
5. [Perception: what an animal knows](#5-perception-what-an-animal-knows)
6. [Actions: the seven behaviours](#6-actions-the-seven-behaviours)
7. [Decisions](#7-decisions)
8. [Reproduction](#8-reproduction)
9. [Population limits: cap and floor](#9-population-limits-cap-and-floor)
10. [One tick, in order](#10-one-tick-in-order)
11. [Randomness and reproducibility](#11-randomness-and-reproducibility)
12. [What a run writes to disk](#12-what-a-run-writes-to-disk)
13. [Reference numbers for the Lab 1 world](#13-reference-numbers-for-the-lab-1-world)

---

## 1. The grid

The world is a 2D grid of square cells with hard borders (no wrap-around).

| | `full` profile (the values in `base.yaml`) | `small` profile (the scripts' default) |
|---|---|---|
| Size | 64 × 64 cells | 48 × 48 cells |

- **Movement** is one cell per tick to any of the 8 neighbours (king moves).
- **Distance** for perception, chasing, mating and attacking is the Chebyshev
  distance `max(|dy|, |dx|)`, so a diagonal step counts as 1. Greedy movement
  picks the neighbour that best reduces the straight-line distance (§6).
- **Cell types:** ground (walkable), water and mountain. Water and mountain are
  impassable for animals and predators.

### Lab 1: a flat world

In Lab 1 every cell is ground: `world.water_fraction` and
`world.mountain_fraction` are both `0`. Terrain arrives in the later labs
([02 — Lab 1](02-lab1.md#7-toward-the-next-labs-terrain-and-foliage)).

### Terrain preview (later labs)

The code can already generate a simple terrain. It does this whenever one of
the two fractions is above zero. `configs/worlds/terrain_preview.yaml` turns it
on with 15 % water and 10 % mountains:

1. A heightmap is made from value noise with three octaves (cell sizes 16, 8
   and 4) and smoothstep interpolation, scaled to 0–1.
2. The lowest `water_fraction` of cells become water and the highest
   `mountain_fraction` become mountains.
3. Only the largest connected walkable region is kept. Isolated pockets of
   ground become mountain. If the connected region covers less than
   `min_walkable_connected` (60 %) of the map, the generator tries again, up to
   50 times.

```bash
python -m experiments.smoke_run --world terrain_preview
```

## 2. Food

- A cell holds at most one food item.
- **Start:** each walkable cell has food with probability
  `food_initial_fraction` = 0.08.
- **Regrowth:** every tick, each empty walkable cell grows a food item with
  probability `food_regrow_p`. In Lab 1 this is **0.0007** and the same for
  every cell, so food appears uniformly at random.
  - With water (terrain preview only), cells within `water_bonus_radius` = 3
    of water regrow `food_water_bonus` = 2 × faster. The preview uses
    `food_regrow_p` = 0.001.
- **Eating** removes the item and gives the animal `eat_gain` = 25 energy, up
  to `energy_max` = 100.

On an empty map, regrowth would produce about 1.6 items per tick on the small
grid (48 × 48 × 0.0007) and 2.9 on the full grid. In practice the steady
state is set by how fast the animals eat.

## 3. Predators

Predators are scripted. They never die, never reproduce and have no genes.

| Parameter | Value | Meaning |
|---|---|---|
| `predators.count` | 3 (`small`: 2) | number of predators |
| `chase_radius` | 6 | a predator chases the nearest animal within 6 cells |
| `turn_p` | 0.2 | otherwise it walks, turning with this probability per tick |
| `kill_p` | 0.3 | chance to kill an animal on the same cell, once per tick |
| `rest_after_kill` | 20 | ticks a predator rests after a kill |

Each tick, a predator that isn't resting takes one greedy step toward the
nearest animal within its chase radius. If no animal is that close, it keeps
walking in its current direction and turns now and then. A predator that ends
its move on the same cell as an animal tries one kill. On success the animal
dies (cause `predator`) and the predator rests for 20 ticks.

## 4. Animals

### State

Position, energy, age (in ticks), heading (for wandering), genome, generation,
parent ids and a few counters (food eaten, offspring, energy steals).

### Life cycle

| Parameter | Value | Meaning |
|---|---|---|
| `init_pop` | 30 (`small`: 24) | animals at tick 0, with founder genomes |
| `energy_start` | 60 | energy of a founder or newcomer |
| `energy_max` | 100 | energy cap |
| `maturity` | 150 | ticks before an animal is an adult and can mate |
| `max_age` | 1 500 | an animal dies of old age after this many ticks |

### Energy budget per tick

| Situation | Energy change |
|---|---|
| Chose `rest` and did not move | −0.2 (`cost_rest`) |
| Any other tick without moving | −0.7 (`cost_base`) |
| Any tick with a move | −1.2 (`cost_base` + `cost_move` 0.5) |
| Eating a food item | +25 (`eat_gain`) |
| Attack attempt | −3 (`attack_cost`); on success, steal up to 10 (`attack_steal`) |
| Having a child | −20 per parent in sexual mode (half of `child_energy` = 40) |

### Death

- **Starvation:** energy at or below 0.
- **Old age:** age above 1 500 ticks.
- **Predator:** killed as described above.

## 5. Perception: what an animal knows

At each decision an animal senses its surroundings as a small, discrete
**observation** (`perception.sense`). Discrete values keep the prompt short and
let identical situations be cached.

| Field | Values | Rule |
|---|---|---|
| `energy` | low, medium, high | low < 30 ≤ medium ≤ 70 < high (`energy_low`, `energy_high`) |
| `food` | none, far, near, here | nearest food item: `here` = on the cell, `near` ≤ 3 cells, `far` ≤ 12, else `none` |
| `predator` | none, far, near | nearest predator: `near` ≤ 3, `far` ≤ 12, else `none` |
| `animal` | none, far, near | nearest other animal, same distance buckets |
| `animal_ready` | true / false | only when `animal` = near: is it ready to mate (adult, energy ≥ 50)? |
| `animal_stronger` | true / false | only when `animal` = near: does it have more energy? |
| `age` | young, adult | young before 150 ticks |

`vision` = 12 and `near` = 3 set the distance buckets. That gives 3 × 4 × 3 × 6 × 2
= **432 possible observations**: energy × food × predator × animal (none, far
or one of four "near" combinations) × age.

Animals don't sense directions, terrain, the number of predators, how much food
there is, or anything about the other animal's genes.

### Observation text

The brain receives the observation as text (`obs_text.render`), in one of two
styles set by `backend.obs_style`:

| Style | Example |
|---|---|
| **V1** (default, terse) | `Energy: low. Food: near. Predator: near. Animal: none. Age: adult.` |
| **V2** (first person) | `I am hungry and weak. Food is close. A predator is very close! I am alone. I am an adult.` |

When another animal is near, V1 adds for example `Animal: near, ready to mate,
weaker.`

## 6. Actions: the seven behaviours

The brain picks one of seven behaviours. An **executor** in `actions.py`
carries the chosen behaviour out, one tick at a time, until the next decision.
The LLM never handles movement itself.

| Action | What the executor does each tick | Invalid when |
|---|---|---|
| `eat` | On a food cell: eat (no move). Otherwise step toward the nearest visible food and eat on arrival in the same tick. | no food within 12 cells |
| `flee` | Step away from the nearest visible predator. | no predator within 12 cells |
| `follow` | Step toward the nearest other animal; stay put once adjacent. | no animal within 12 cells |
| `wander` | Persistent random walk: keep the heading, turn ±45° or ±90° with probability 0.25 (`wander_turn_p`), pick a new random heading when blocked. | never |
| `rest` | Stay still; costs only 0.2 energy per tick. | never |
| `mate` | Step toward the nearest mate-ready animal; stay put once adjacent (the simulation then resolves breeding). | no mate-ready animal within 12 cells |
| `attack` | Step toward the nearest animal. Once adjacent, attack once per decision period: pay 3 energy, and with probability `own / (own + target)` energy steal up to 10 energy from it. | no animal within 12 cells |

**Invalid choices fall back to `wander`** and are counted (`invalid` in
`stats.csv`, `invalid_rate` in `summary.json`). For example, "eat" with no food
in sight makes the animal wander instead.

Movement is greedy: each step goes to the neighbouring cell that most reduces
(or, for flee, increases) the straight-line distance, with random tie-breaks.
If no neighbour improves the distance, for example behind a lake, the animal
takes a random step.

## 7. Decisions

- **When:** every `sim.decision_period` = 4 ticks, all animals decide at once.
  A newborn or newcomer decides on its first tick. Between decisions the
  chosen behaviour is repeated every tick.
- **What the brain returns:** a probability for each of the seven actions,
  given the animal's genome and its current observation
  ([05 — The brain](05-decision-backends.md)).
- **Sampling:** the action is drawn from those probabilities. With
  `sim.sampling_temperature` τ ≠ 1 the probabilities are first sharpened
  (τ < 1) or flattened (τ > 1) as p^(1/τ). The default is 1.
- **Memo:** within one run, the distribution for each (genome, observation)
  pair is computed once and reused. Several animals share a genome and
  situations repeat, so only about 40 % of decisions need the brain at all. The
  memo hit rate was 0.58–0.63 in the Lab 1 tuning runs.

All decisions due on the same tick are sent to the brain as one batch. The
simulation waits for the answers before moving on (**lockstep**), so a slow
brain makes the run slower but never changes its result.

## 8. Reproduction

**Sexual (default, `evolution.sexual: true`).** A child is born when two
animals:

- both chose `mate` at their last decision,
- are both mate-ready (age ≥ 150 ticks and energy ≥ 50),
- are adjacent (Chebyshev distance ≤ 1),
- haven't bred yet in this decision period,

and the population is below the cap. The child's genome is a crossover of the
two parents followed by mutation ([04 — Genome and evolution](04-genome-and-evolution.md)).
Each parent pays 20 energy. The child appears on the first parent's cell with
40 energy, and its generation is the larger parent generation + 1.

**Asexual (`evolution.sexual: false`, the old lab's regime).** A single
mate-ready animal that chose `mate` copies its genome (plus mutation) and pays
the full 40 energy.

## 9. Population limits: cap and floor

| Parameter | Value | Effect |
|---|---|---|
| `agents.cap` | 60 (`small`: 40) | no births while the population is at the cap |
| `agents.floor` | 10 | if fewer animals remain, newcomers with fresh founder genomes are added at random cells |

Newcomers are logged as `immigrant` events and counted in `stats.csv`. They
keep a population alive while its behaviour is poor. For example, a random
brain survives only because of the floor (see §13).

## 10. One tick, in order

`Simulation.step()` does the following:

1. **Decide:** every 4th tick all animals decide; on other ticks only animals
   without an action yet (newborns, newcomers).
2. **Act:** animals run their behaviour for this tick in a random order, then
   pay the tick's energy cost.
3. **Breed:** adjacent pairs that both chose `mate` produce children (§8).
4. **Predators move** (§3).
5. **Predation:** each predator on an animal's cell tries one kill.
6. **Age and die:** ages increase by 1; animals with energy ≤ 0 starve, animals
   older than 1 500 ticks die of old age.
7. **Food regrows** (§2).
8. **Floor:** newcomers are added if the population is below 10.
9. Every `sim.stats_every` = 100 ticks, a row is written to `stats.csv`.

## 11. Randomness and reproducibility

- There is no global random generator. Each part of the simulation draws from
  its own named stream (`promptevo/rng.py`): `world`, `predators`, `agents`,
  `sampling`, `actions`, `mutation` and `food`. Changing one part, for example
  how food regrows, doesn't shift the random numbers used elsewhere.
- The world can have its own seed (`Simulation(..., world_seed=...)`), so many
  runs can share one map while everything else varies.
- Every logged event updates a running hash (`events_sha` in `summary.json`).
  The test `test_simulation_is_deterministic` checks that the same seed gives
  exactly the same run.
- The LLM brain answers at temperature 0 with a fixed seed and caches every
  answer, so its decisions are reproducible too
  ([05](05-decision-backends.md#caching)).

## 12. What a run writes to disk

`smoke_run` writes into `--out` (default `results/runs/smoke/`):

| File | Content |
|---|---|
| `events.jsonl` | one line per event: `founder`, `immigrant` (id, genome), `birth` (child id, parents, generation, genome, mutations with the locus, parent and new allele, instruction number and new text), `death` (cause, age, generation, food eaten, offspring, steals) |
| `stats.csv` | every 100 ticks: `t, pop, mean_energy, mean_gen, max_gen, births, immigrants, deaths_starve, deaths_pred, deaths_age, decisions, backend_queries, invalid, alleles` (counts are cumulative) |
| `alleles.jsonl` | every allele seen in the run: id, locus, text, origin, parent allele, operator (`llm#<n>`: the mutation instruction drawn), model, seed (the lineage of every gene) |
| `final_population.json` | the living animals at the end: id, generation, genome (allele ids) |
| `summary.json` | totals: births, newcomers, deaths by cause, mean lifespan, maximum generation, decisions, brain queries, memo hit rate, brain time, invalid rate, share of each action, mutation counts per operator, number of alleles, event hash |

Genomes are stored as allele ids such as `eat:3`. `alleles.jsonl` maps ids to
text. Look at the files with `python -m experiments.peek FILE -n 5` rather
than opening large logs.

## 13. Reference numbers for the Lab 1 world

Measured on 2026-10-01 with the rule-based brain, 5 000 ticks, statistics from
tick 1 000 onward, seeds 1234, 7 and 42.

| | small (48 × 48, cap 40) | full (64 × 64, cap 60) |
|---|---|---|
| Mean population | 25–28 | 47–49 |
| Time at the cap | 0 % | 0 % |
| Deaths (5 000 ticks): predator / starvation | ≈ 226–242 / 181–190 | ≈ 338–390 / 410–413 |
| Mean lifespan | 271–301 ticks | 281–291 ticks |
| Generations reached | 22–25 | 24 |
| Newcomers needed | 0 | 0 |
| **Random brain** instead | collapses to the floor of 10; ≈ 250 newcomers | same |

Food regrowth was tuned to 0.0007 so that the population stays limited by food,
below the cap. At the earlier 0.001 the flat world sat at the cap 25–68 % of
the time, and births then depend on free slots rather than on finding food.
One simulated tick with the rule-based brain takes about 2–3 ms on a desktop
CPU (5 000 ticks ≈ 12 s).
