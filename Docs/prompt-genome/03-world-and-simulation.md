# 03 — World and simulation

How the simulated world works, tick by tick. Two species live in it, prey
animals and predators, and since 2026-10-07 both are genetic animals with a
brain. Everything here is implemented in `prototype/promptevo/` (`species.py`,
`world.py`, `perception.py`, `obs_text.py`, `actions.py`, `sim.py`). Parameter
values come from `prototype/configs/base.yaml` and the two size profiles,
`small.yaml` and `full.yaml`.

## Contents

1. [The grid](#1-the-grid)
2. [Food](#2-food)
3. [Predators](#3-predators)
4. [Animals](#4-animals)
5. [Perception: what an animal knows](#5-perception-what-an-animal-knows)
6. [Actions: five for prey, four for predators](#6-actions-five-for-prey-four-for-predators)
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

| | `full` profile (the values in `base.yaml`; `smoke_run`'s default since 2026-10-07) | `small` profile (tests, quick checks) |
|---|---|---|
| Size | 96 × 96 cells (64 × 64 until 2026-10-07) | 48 × 48 cells |

- **Movement** is at most one cell per tick to any of the 8 neighbours (king
  moves). Prey and predators have the same speed.
- **Distance** for perception, chasing, striking and mating is the Chebyshev
  distance `max(|dy|, |dx|)`, so a diagonal step counts as 1. Greedy movement
  picks the neighbour that best reduces the straight-line distance (§6).
- **Cell types:** ground (walkable), water and mountain. Water and mountain are
  impassable for both species.

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

- A cell holds at most one food item. Only prey animals eat food.
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
grid (48 × 48 × 0.0007) and 6.5 on the full grid. In practice the steady
state is set by how fast the animals eat.

## 3. Predators

Since 2026-10-07 predators are genetic animals like the prey. Each has a genome
of 4 genes (`hunt`, `follow`, `rest`, `mate`) read by the same kind of brain
([04](04-genome-and-evolution.md#1-genes-are-sentences-in-fixed-slots),
[05](05-decision-backends.md)). It has energy and an age, breeds with other
predators, mutates, starves and dies of old age (§4, §8). Its actions are in
§6 and its senses in §5. Until then predators were scripted: they chased the
nearest animal within 6 cells, killed on the same cell with probability 0.3,
never died and never bred.

What is specific to predators (`predators.*`):

| Parameter | Value | Meaning |
|---|---|---|
| `kill_p` | 0.1 | chance that a strike kills; a hunting predator strikes when it is next to its prey (distance ≤ 1) after its step |
| `kill_gain` | 60 | energy from one kill, up to `energy_max` = 100 |
| `digest_ticks` | 50 | after a kill the predator stays still for 50 ticks, pays only `cost_rest` and doesn't decide |
| `init_pop` / `floor` / `cap` | 14 / 3 / 34 (`small`: 4 / 3 / 6) | population limits (§9) |

Everything else uses the same values as the prey: energy, costs, maturity, age
limit, mating energy, vision (20 cells) and speed. A predator eats only prey:
kills are its only food, and `food` in its `death` event counts kills.

The values come from tuning sweeps with the keyword brain on 2026-10-07 (small
world, 4 seeds, 8 000 ticks):

- With `kill_p` 0.2 the prey sat at their floor 29 % of the time, against 6 %
  with 0.1.
- Cheaper predators (lower energy costs) multiplied until the prey sat at their
  floor 34–75 % of the time.
- A digestion of 30 ticks with `kill_p` 0.15–0.2 let the predators overshoot
  (prey at their floor 65–93 % of the time).

## 4. Animals

"Animal" means either species here. Prey and predators have the same state and
life cycle, each with its own parameters: `agents.*` for the prey and
`predators.*` for the predators.

### State

Position, energy, age (in ticks), heading (for the random walk, §6), genome,
generation, parent ids and a few counters (food eaten or prey killed,
offspring). Predators also count down their digestion (§3).

### Life cycle

| Parameter | Prey | Predators | Meaning |
|---|---|---|---|
| `init_pop` | 68 (`small`: 24) | 14 (`small`: 4) | animals at tick 0, with founder genomes |
| `energy_start` | 60 | 60 | energy of a founder or newcomer |
| `energy_max` | 100 | 100 | energy cap |
| `maturity` | 150 | 150 | ticks before an animal is an adult and can mate |
| `max_age` | 1 500 | 1 500 | an animal dies of old age after this many ticks |

### Energy budget per tick

The same for both species:

| Situation | Energy change |
|---|---|
| Chose `rest` and did not move | −0.2 (`cost_rest`) |
| Digesting a kill (predators) | −0.2 per tick for 50 ticks |
| Any other tick without moving | −0.7 (`cost_base`) |
| Any tick with a move | −1.2 (`cost_base` + `cost_move` 0.5) |
| Eating a food item (prey) | +25 (`eat_gain`) |
| A kill (predators) | +60 (`kill_gain`) |
| Having a child | −20 per parent (half of `child_energy` = 40) |

### Death

- **Starvation:** energy at or below 0 (both species).
- **Old age:** age above 1 500 ticks (both species).
- **Predator:** a prey animal killed by a hunting predator (§6).

## 5. Perception: what an animal knows

At each decision an animal senses its surroundings as a small, discrete
**observation** (`perception.sense`). Discrete values keep the prompt short and
let identical situations be cached.

**Vision and distance (since 2026-10-07).** Both species see 20 cells far
(`agents.vision`, `predators.vision`), and every distance is reported as a
band. The band edges are `perception.bands` = [1, 4, 10]:

| Band | Distance |
|---|---|
| `adjacent` | 1 cell (or the same cell) |
| `close` | 2–4 cells |
| `medium` | 5–10 cells |
| `far` | 11–20 cells |
| `none` | nothing of that kind within 20 cells |

Until then animals saw 12 cells, with two bands: near (≤ 3) and far.

**A prey animal senses:**

| Field | Values | Rule |
|---|---|---|
| `energy` | low, medium, high | low < 30 ≤ medium ≤ 70 < high (`energy_low`, `energy_high`) |
| `food` | here, adjacent, close, medium, far, none | nearest food item; `here` = on its cell |
| `predator` | adjacent, close, medium, far, none | nearest predator |
| `animal` | adjacent, close, medium, far, none | nearest other prey animal |
| `animal_ready` | true / false | is that animal ready to mate (adult, energy ≥ 50)? Seen up to `perception.partner_range` = 20 cells, the whole vision |
| `age` | young, adult | young before 150 ticks |

**A predator senses:**

| Field | Values | Rule |
|---|---|---|
| `energy` | low, medium, high | same thresholds |
| `prey` | adjacent, close, medium, far, none | nearest prey animal |
| `animal` | adjacent, close, medium, far, none | nearest other predator |
| `animal_ready` | true / false | is that predator ready to mate? Seen up to 20 cells |
| `age` | young, adult | young before 150 ticks |

That gives 3 × 6 × 5 × 9 × 2 = **1 620 possible prey observations**: energy ×
food × predator × animal (none, or one of the 4 bands, ready to mate or not) ×
age. Predators have 3 × 5 × 9 × 2 = **270**. Before 2026-10-07 the prey had
288.

**Seeing a partner.** Until 2026-10-07 an animal saw whether another was ready
to mate only within 4 cells (`partner_range` 4), so far partners were
invisible and the few predators rarely met. Setting `partner_range: 4` gives
back that rule.

Animals don't sense directions, terrain, how many animals or how much food
there is, or anything about another animal beyond whether it is ready to mate.
A prey animal doesn't know whether a predator is hunting or digesting.

### Observation text

The brain receives the observation as text (`obs_text.render`), in one of two
styles set by `backend.obs_style`. Each band is written as its range of cells,
taken from the config, so the brain knows how far things are and how far it
can see:

| Style | Prey | Predator |
|---|---|---|
| **V1** (default, terse) | `Energy: low. Food: 2-4 cells away. Predator: 5-10 cells away. Animal: none within 20 cells. Age: adult.` | `Energy: medium. Prey: 2-4 cells away. Other predator: none within 20 cells. Age: adult.` |
| **V2** (first person) | `I am hungry and weak. The nearest food is 2-4 cells away. The nearest predator is 5-10 cells away. No other animal within 20 cells. I am an adult.` | `I have some energy. The nearest prey is 2-4 cells away. No other predator within 20 cells. I am an adult.` |

On a food cell V1 says `Food: here.` V1 also says whether the other animal is
ready to mate, for example `Animal: 11-20 cells away, ready to mate.`

## 6. Actions: five for prey, four for predators

The brain picks one action, one per gene of the species. An **executor** in
`actions.py` carries it out, one tick at a time, until the next decision. The
LLM never handles movement itself.

| Species | Action | What the executor does each tick | Searches instead when |
|---|---|---|---|
| prey | `eat` | On a food cell: eat (no move). Otherwise step toward the nearest visible food and eat on arrival in the same tick. | no food within 20 cells |
| prey | `flee` | Step away from the nearest predator. | no predator within 20 cells |
| both | `follow` | Step toward the nearest other animal of its species; stay put once adjacent. Predators have it since 2026-10-07. | no other animal of its kind within 20 cells |
| both | `rest` | Stay still; costs only 0.2 energy per tick. | never |
| both | `mate` | Step toward the nearest mate-ready animal of its species; next to it, they breed (§8), whatever the partner chose. | no mate-ready partner within 20 cells |
| predator | `hunt` | Step toward the nearest prey animal. Next to it after the step (distance ≤ 1), strike: the prey dies with probability `kill_p` = 0.1. A kill feeds the predator (+60) and starts its digestion (§3). | no prey within 20 cells |

**An action with nothing to act on makes the animal search.** If there is
nothing in sight for the chosen action, the animal wanders instead: a
persistent random walk that keeps its heading, turns ±45° or ±90° with
probability 0.25 (`wander_turn_p`) and picks a new random heading when blocked.
So "eat" with no food in sight means searching for food, and "hunt" with no
prey in sight means searching for prey. These choices are counted as invalid
(`invalid` and `pred_invalid` in `stats.csv`, `invalid_rate` in
`summary.json`): 2 % of prey decisions and 0–5 % of predator decisions in
the reference runs (§13), against 11–13 % with the 12-cell vision. The LLM
brain's prompts state the same rule ([05](05-decision-backends.md#the-prompt)).

**Same speed.** Both species move at most one cell per tick. A prey animal
that flees keeps its distance from a hunting predator. It gets caught when it
stops (to eat, rest or mate), moves toward the predator or reaches a border.
Animals decide only every 4 ticks (§7), so a predator can close in between two
decisions. A predator strikes in the same tick it arrives next to its prey, so
fleeing has to start before the predator is adjacent.

Movement is greedy: each step goes to the neighbouring cell that most reduces
(or, for flee, increases) the straight-line distance, with random tie-breaks.
If no neighbour improves the distance, for example behind a lake, the animal
takes a random step.

Animals of the same species never fight each other. Until 2026-10-07 the prey
had seven behaviours: `wander` was an action of its own, and `attack` tried to
steal energy from a neighbour
([09](09-status-and-roadmap.md#3-decisions-taken)).

## 7. Decisions

- **When:** every `sim.decision_period` = 4 ticks, all animals of both species
  decide at once. A newborn or newcomer decides on its first tick, and a
  predator decides again on the tick after its digestion ends. A digesting
  predator doesn't decide. Between decisions the chosen action is repeated
  every tick.
- **What the brain returns:** a probability for each action of the animal's
  species, given its genome and its current observation
  ([05 — The brain](05-decision-backends.md)).
- **Sampling:** the action is drawn from those probabilities. With
  `sim.sampling_temperature` τ ≠ 1 the probabilities are first sharpened
  (τ < 1) or flattened (τ > 1) as p^(1/τ). The default is 1.
- **Memo:** within one run, the distribution for each (species, genome,
  observation) is computed once and reused. Situations repeat, so many
  decisions don't need the brain at all. In the reference runs of §13 the memo
  answered 61–81 % of prey decisions and 75–84 % of predator decisions. Before
  the distance bands (5 genes, 12-cell vision, no mutation) it was 77–89 %.

All decisions due on the same tick are sent to the brain in one batch per
species. The simulation waits for the answers before moving on (**lockstep**),
so a slow brain makes the run slower but never changes its result.

## 8. Reproduction

**Sexual (default, `evolution.sexual: true`).** A child is born when two
animals of the same species:

- at least one of them chose `mate` at its last decision (since 2026-10-07; before, both had to),
- are both mate-ready (age ≥ 150 ticks and energy ≥ 50),
- are adjacent (Chebyshev distance ≤ 1),
- haven't bred yet in this decision period,

and their species is below its cap. The child's genome is a crossover of the
two parents followed by mutation
([04 — Genome and evolution](04-genome-and-evolution.md)). Each parent pays 20
energy. The child appears on the first parent's cell with 40 energy, and its
generation is the larger parent generation + 1.

**Asexual (`evolution.sexual: false`, the old lab's regime).** A single
mate-ready animal that chose `mate` copies its genome (plus mutation) and pays
the full 40 energy.

## 9. Population limits: cap and floor

| Parameter | Prey (`agents.*`) | Predators (`predators.*`) | Effect |
|---|---|---|---|
| `cap` | 135 (`small`: 40) | 34 (`small`: 6) | no births while the species is at its cap |
| `floor` | 10 | 3 | if fewer remain, newcomers with fresh founder genomes are added at random cells |

Newcomers are logged as `immigrant` events and counted in `stats.csv`. They
keep a species alive while its behaviour is poor. For example, predators with a
random brain survive only because of their floor (§13).

## 10. One tick, in order

`Simulation.step()` does the following:

1. **Decide:** every 4th tick all animals of both species decide, one batch per
   species; on other ticks only animals without an action (newborns, newcomers,
   predators done digesting). Digesting predators don't decide.
2. **Prey act** in a random order, then pay the tick's energy cost.
3. **Prey breed:** an animal that chose `mate` and a ready partner next to it produce a child (§8).
4. **Predators act** in a random order and pay their energy cost. A hunting
   predator can kill (§6); a digesting one stays still.
5. **Predators breed.**
6. **Killed prey are removed**, with cause `predator` and the killer's id.
7. **Age and die:** ages increase by 1; animals with energy ≤ 0 starve, animals
   older than 1 500 ticks die of old age.
8. **Food regrows** (§2).
9. **Floors:** newcomers are added to each species below its floor.
10. Every `sim.stats_every` = 100 ticks, a row is written to `stats.csv`.

## 11. Randomness and reproducibility

- There is no global random generator. Each part of the simulation draws from
  its own named stream (`promptevo/rng.py`): `world`, `agents` (prey founders
  and newcomers), `predators` (predator founders and newcomers), `sampling`,
  `actions` (moves and kill rolls), `mutation` and `food`. Changing one part,
  for example how food regrows, doesn't shift the random numbers used
  elsewhere.
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
| `events.jsonl` | one line per event: `founder`, `immigrant` (id, genome), `birth` (child id, parents, generation, genome, mutations with the locus, parent and new allele, instruction number and new text), `death` (cause, age, generation, food eaten or prey killed, offspring; `killer` when a predator killed it). Predator events carry `"species": "predator"`; prey events have no `species` field. |
| `stats.csv` | every 100 ticks: `t, pop, mean_energy, mean_gen, max_gen, births, immigrants, deaths_starve, deaths_pred, deaths_age, decisions, backend_queries, invalid, alleles`, decisions per action `act_eat` … `act_mate`, then the predators' columns with the prefix `pred_` (`pred_pop` … `pred_invalid`, `pred_act_hunt`, `pred_act_rest`, `pred_act_mate`). `deaths_pred` counts prey killed by predators; counts are cumulative; `alleles` covers both species. |
| `alleles.jsonl` | every allele seen in the run, both species: id, locus, text, origin, parent allele, operator (`llm#<n>`: the mutation instruction drawn), model, seed (the lineage of every gene) |
| `final_population.json` | the living animals at the end: id, generation, genome (allele ids); predators carry `"species": "predator"` |
| `run_info.json` | what ran: command, git commit, Ollama version and model digests, brain, seed, the main config sections |
| `summary.json` | totals for the prey at the top level (births, newcomers, deaths by cause, mean lifespan, maximum generation, decisions, brain queries, memo hit rate, brain time, invalid rate, share of each action), the same for the predators under `predators` (plus `kills`), mutation counts, number of alleles, event hash; why the run stopped, minutes, model calls not answered by the cache, failures |

Long runs are safe to stop. `events.jsonl` and `stats.csv` are flushed every
500 ticks and `alleles.jsonl` is saved every 5 000. Creating
`logs/run_<name>.stop` stops a run cleanly within 100 ticks; Ctrl-C, a failed
model call or any error also stop it cleanly, with every file written.
Running the same command again resumes it: the part already done replays from
the caches without model calls, then the run continues. `gene_report` also
reads a run that is still going or was stopped hard.

Genomes are stored as allele ids such as `eat:3` or `predator.hunt:0`.
`alleles.jsonl` maps ids to text. Look at the files with
`python -m experiments.peek FILE -n 5` rather than opening large logs.

Runs made before 2026-10-07 have scripted predators (no predator events), 10
genes per prey genome, a `steals` count in `death` events and, if they count
decisions per action (since 2026-10-05), `act_wander` and `act_attack`
columns. `gene_report` and `gene_timeline` still read them
([08 §4](08-code-and-config-reference.md#4-scripts-experiments)).

## 13. Reference numbers for the Lab 1 world

Measured on 2026-10-07 with the keyword brain and no mutation, 5 000 ticks,
populations from tick 1 000 onward, seeds 1234, 7 and 42.

| | full (96 × 96, caps 135 / 34) | small (48 × 48, caps 40 / 6) |
|---|---|---|
| **Prey:** mean population | 74–93 (92.5 / 91.7 / 74.1) | 22–25 (24.6 / 22.2 / 24.4) |
| Time at the floor / at the cap | 0 % / 0 % | 0–2 % / 0 % |
| Deaths (5 000 ticks): predator / starvation | 896–1 174 / 669–1 012 | 308–331 / 161–188 |
| Mean lifespan · generations reached | 214–222 ticks · 26 | 218–228 ticks · 25–26 |
| Newcomers | 0 | 0 |
| **Predators:** mean population | 15–21 (15.3 / 20.9 / 17.0) | 5.3–5.8 (5.6 / 5.3 / 5.8) |
| Time at the floor (3) / at the cap | 0 % / 0–5 % | 0–4 % / 55–83 % |
| Births · newcomers | 200–266 · 0 | 31–37 · 0 |
| Deaths: starvation / old age | 189–252 / 2–9 | 23–28 / 6–9 |
| Mean lifespan · generations reached | 362–369 ticks · 18–19 | 726–815 ticks · 9–13 |
| Invalid actions (the animal searches instead, §6): prey / predators | 2 % / 2–5 % | 2 % / 0–1 % |
| **Random brain** instead | prey 110–112; predators stay at their floor, 137–157 newcomers | prey 20–24; predators stay at their floor, 127–142 newcomers |

Decisions in the full world: prey eat 63 %, flee 18 %, rest 7 %, follow 7 %,
mate 6 %; predators hunt 78 %, mate 10 %, rest 8 %, follow 5 %. Random prey
are as many as keyword prey or more, because random predators hardly catch
anything (48–67 kills in 5 000 ticks).

**What changed on 2026-10-07**, in order:

- Animals see whether a partner is ready across their whole vision (§5), and
  the keyword brain goes to a ready partner at any distance
  ([05](05-decision-backends.md#3-rule_based-the-transparent-keyword-brain)).
  With readiness seen only within 4 cells, predators rarely met: in the small
  world they had 4–25 births and 0–7 generations.
- One partner's choice is enough to breed (§8), and predators can follow each
  other (§6). In the 64 × 64 world this gave 6.6–9.2 predators with 80–114
  births and 12–18 generations (33–69 births before).
- The full world grew from 64 × 64 to 96 × 96, with populations and caps scaled
  by area, so more predators live in it. The LLM brain's cost grows with the
  number of animals: in the small world the LLM-read prey didn't hold against
  the predators
  ([06 §5.12](06-experiments-and-results.md#512-first-llm-brain-run-with-genetic-predators)),
  and in the 64 × 64 world the few LLM-read predators never bred
  ([06 §5.13](06-experiments-and-results.md#513-llm-brain-in-the-64--64-world-partners-seen-across-the-vision)).
- The small world's predator cap went from 10 to 6: with 10, predators bred so
  well that the prey sat at their floor up to 77 % of the time.

Predators stay few. Prey production sets the limit: making predators cheaper
or deadlier gave more predators only until the prey collapsed (§3). For more
predators, give the prey more food or a bigger world. Before 2026-10-07, with
scripted predators and the 12-cell vision, the same runs gave 25–31 prey in
the small world and 50–55 in the full world.

Food regrowth was tuned to 0.0007 (2026-10-01, with the 10-gene genome and
scripted predators) so that the population stays limited by food, below the
cap. At the earlier 0.001 the flat world sat at the cap 25–68 % of the time,
and births then depend on free slots rather than on finding food. With
predators that hunt, the prey stay below the cap almost all the time in both
worlds. One simulated tick with the keyword brain takes about 1 ms in the small
world and 4 ms in the full one on a desktop CPU (5 000 ticks ≈ 3 s and 22 s).
