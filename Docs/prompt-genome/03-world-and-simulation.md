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
| Size | 192 × 192 cells since 2026-10-09 (96 × 96 from 2026-10-07, 64 × 64 before) | 48 × 48 cells |

The world doubled in width and height on 2026-10-09, together with the
populations, to give evolution more room (§13 and
[06 §5.21](06-experiments-and-results.md#521-an-evolution-test-world)).

- **Movement** goes to any of the 8 neighbours (king moves). Walking covers
  one cell per tick; running (`hunt`, `flee`) covers up to `speed` cells: 2 for
  predators, 1 for prey. Every cell costs stamina (§4). Until the stamina
  change of 2026-10-07 both species moved at most one cell per tick.
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

### Cover (since 2026-10-09)

Cover is thickets: clustered patches of ground where a prey animal is safe.
It is on by default on 20 % of the cells (`world.cover_fraction`, 0 = none).

- **Hidden prey.** Predators don't see a prey animal standing in cover: it is
  left out of their observation (§5) and of their `hunt` target, so it can't
  be struck (§6).
- **Hungry cover.** No food grows in cover (`world.cover_food: false`), and none
  is placed there at the start. Hiding and eating compete.
- **Fleeing into cover.** A fleeing prey animal already in cover stays where it
  is. One with cover within `world.cover_seek` = 6 cells runs into it instead of
  running away. Otherwise it runs away as before (§6).
- **Not an action, observation or gene.** The brain never hears about cover:
  there is no "hide" action, prey don't sense where cover is, and no gene slot
  is about it. Cover only changes how `flee` is carried out and what predators
  see. So it can favour genes that make prey flee or rest more, but not genes
  that use cover on purpose. Adding cover to the prey's observation would be the
  next step.

The patches are the top 20 % of a two-octave value-noise map (cell sizes 8 and
4) over walkable cells. They are drawn after everything else, so a world
without cover is the same as before. On 2026-10-08 cover was first tried with
food in it (`cover_food: true`) as a way to stop crashes
([06 §5.19](06-experiments-and-results.md#519-boom-and-bust-how-to-stop-the-crashes)).

### Ridges (option, off)

`world.patches` = k splits the world into k × k areas with mountain ridges one
cell thick, each ridge segment with one gap of `world.wall_gap` cells in its
middle. Tried on 2026-10-08 against crashes, it made things worse: predators
couldn't reach prey across the ridges and died out (06 §5.19). It is off
(`patches: 0`).

## 2. Food

- A cell holds at most one food item. Only prey animals eat food.
- **Start:** each walkable cell outside cover has food with probability
  `food_initial_fraction` = **0.1**.
- **Regrowth:** every tick, each empty walkable cell outside cover grows a food
  item with probability `food_regrow_p` = **0.0015** (about 670 ticks to regrow
  an eaten cell), the same for every such cell, so food appears uniformly at
  random. Cover never grows food (§1).
  - With water (terrain preview only), cells within `water_bonus_radius` = 3
    of water regrow `food_water_bonus` = 2 × faster. The preview uses
    `food_regrow_p` = 0.001.
- **Eating** removes the item and gives the animal `eat_gain` = 25 energy, up
  to `energy_max` = 100.

On an empty map, regrowth would produce about 44 items per tick on the full
grid (192 × 192 × 0.8 × 0.0015) and 2.8 on the small grid. In practice the
steady state is set by how fast the animals eat.

| Date | `food_regrow_p` | `food_initial_fraction` | Why |
|---|---|---|---|
| 2026-10-01 | 0.0007 | 0.08 | tuned so the prey stay limited by food, below their cap (§13) |
| 2026-10-08 | 0.005 | 0.3 | owner: food quick and plentiful, against crashes ([06 §5.19](06-experiments-and-results.md#519-boom-and-bust-how-to-stop-the-crashes)) |
| 2026-10-09 | 0.003 | 0.3 | owner: a bit slower, with the 192 × 192 world |
| 2026-10-09 | 0.0015 | 0.1 | owner: lower still, so that food matters for selection ([06 §5.21](06-experiments-and-results.md#521-an-evolution-test-world)) |

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
| `speed` | 2 | cells per tick when hunting, against 1 for a fleeing prey animal; every other move walks one cell (§4) |
| `stamina_max` | 30 | half the prey's 60: 15 ticks of running from full (§4) |
| `kill_p` | 0.5 | chance that a strike kills; a hunting predator strikes when it is next to its prey (distance ≤ 1) after its move. 0.1 until 2026-10-09, then 0.2 and 0.5 the same day (owner, §13) |
| `kill_gain` | 60 | energy from one kill, up to `energy_max` = 100 |
| `digest_ticks` | 50 | after a kill the predator stays still for 50 ticks, pays only `cost_rest` (plus `cost_regen` while its stamina comes back) and doesn't decide; after a carcass portion, 25 ticks (in proportion to the energy) |
| `carcass_portions` | 2 | a kill leaves a carcass on the prey's cell with one portion each for up to 2 other predators, never the killer (0: no carcass, the rule before) |
| `carcass_gain` | 30 | energy from one portion |
| `carcass_ticks` | 100 | a carcass rots away after 100 ticks, eaten or not |
| `init_pop` / `floor` / `cap` | 28 / 3 / 68 (`small`: 4 / 3 / 6; 14 / 3 / 34 in the 96 × 96 world) | population limits (§9) |
| `litter` | [2, 4] | babies per mating, the same as the prey (§8) |

Three more options came out of the crash exploration of 2026-10-08 and are off
by default ([06 §5.19](06-experiments-and-results.md#519-boom-and-bust-how-to-stop-the-crashes)):

| Option | Effect | Result in the screen |
|---|---|---|
| `interference` c, `interference_radius` r | a strike succeeds with `kill_p` / (1 + c × other predators within r cells of the prey): crowding predators get in each other's way | c = 3 lasted 7/8 at `kill_p` 0.1 but failed at 0.15–0.2 |
| `prey_per_predator` q | predators breed only while there are at least q prey per predator in the whole world | lasted 24/24, but the owner rejected it: no animal could know the head count |
| `breed_prey_seen` m | a predator breeds only with at least m prey within its vision (the local version) | weak: predators gather where prey still are |

Everything else uses the same values as the prey: energy, costs, stamina
recovery, maturity, age limit, mating energy and vision (20 cells). A predator
eats only prey, its own kills and other predators' leftovers: `food` in its
`death` event counts both kinds of meal. It finds a carcass with `hunt` (§6)
and senses the nearest one it may eat from (§5).

The values come from tuning sweeps with the keyword brain on 2026-10-07 (small
world, 4 seeds, 8 000 ticks):

- With `kill_p` 0.2 the prey sat at their floor 29 % of the time, against 6 %
  with 0.1.
- Cheaper predators (lower energy costs) multiplied until the prey sat at their
  floor 34–75 % of the time.
- A digestion of 30 ticks with `kill_p` 0.15–0.2 let the predators overshoot
  (prey at their floor 65–93 % of the time).

Speed, stamina and carcasses came later the same day, on the owner's request.
Their values were checked with the keyword brain (5 000 ticks, 3 seeds, both
worlds; §13). Smaller portions (20 energy), alone or with `kill_p` 0.07, kept
the predators below their cap but let them starve in waves, and the prey then
sat at their floor up to 10 % of the time in the full world. `kill_p` 0.07, a
digestion of 80 ticks or a slower recovery (1.5 per tick) left the predators at
their cap and the prey at 96–120.

On 2026-10-09 `kill_p` went to 0.2, then 0.5, to make more deaths depend on
behaviour. Kills rose less than the kill chance: each kill is followed by 50
ticks of digestion, and that caps how often a predator can kill (§13).

## 4. Animals

"Animal" means either species here. Prey and predators have the same state and
life cycle, each with its own parameters: `agents.*` for the prey and
`predators.*` for the predators.

### State

Position, energy, stamina (below), age (in ticks), heading (for the random
walk, §6), genome, generation, parent ids and a few counters (food eaten or a
predator's meals, offspring). Predators also count down their digestion (§3).

### Life cycle

| Parameter | Prey | Predators | Meaning |
|---|---|---|---|
| `init_pop` | 136 (`small`: 24; 68 in the 96 × 96 world) | 28 (`small`: 4; 14 before) | animals at tick 0, with founder genomes |
| `energy_start` | 60 | 60 | energy of a founder or newcomer |
| `energy_max` | 100 | 100 | energy cap |
| `maturity` | 150 | 150 | ticks before an animal is an adult and can mate |
| `max_age` | 1 500 | 1 500 | an animal dies of old age after this many ticks |

### Stamina and speed

Since 2026-10-07 every animal has **stamina**: the number of cells it can move
before it must stop. Each cell moved costs one point. A tick without moving
brings back `stamina_regen` points and costs `cost_regen` extra energy, until
stamina is full again. An animal without stamina for one cell can't move: it
stays where it is, whatever it chose. Newborns and newcomers start with full
stamina.

| Parameter | Prey | Predators | Meaning |
|---|---|---|---|
| `speed` | 1 | 2 | cells per tick when running: `flee` (prey), `hunt` (predators); every other move walks one cell |
| `stamina_max` | 60 | 30 | full stamina, in cells |
| `stamina_regen` | 2 | 2 | stamina back per tick without moving |
| `cost_regen` | 0.3 | 0.3 | extra energy per tick without moving, until stamina is full |
| `stamina_low` / `stamina_high` | 20 / 40 | 10 / 20 | thresholds of the sensed level (§5) |

So predators win short chases and prey long ones (§6). Running costs a
predator 2 points per tick: 15 ticks from full.

### Energy budget per tick

The same for both species:

| Situation | Energy change |
|---|---|
| Chose `rest` and did not move | −0.2 (`cost_rest`) |
| Digesting a meal (predators) | −0.2 per tick: 50 ticks after a kill, 25 after a carcass portion |
| Any other tick without moving | −0.7 (`cost_base`) |
| A tick without moving while stamina isn't full (added to the lines above) | −0.3 (`cost_regen`) |
| A tick with a move | −0.7 (`cost_base`) and −0.5 (`cost_move`) per cell: −1.2 walking, −1.7 for a predator running 2 cells |
| Eating a food item (prey) | +25 (`eat_gain`) |
| A kill (predators) | +60 (`kill_gain`) |
| A carcass portion (predators) | +30 (`carcass_gain`) |
| Having a litter | −20 per parent for each baby (half of `child_energy` = 40): −40 to −80 for 2–4 babies (§8) |

### Death

- **Starvation:** energy at or below 0 (both species).
- **Old age:** age above 1 500 ticks (both species).
- **Predator:** a prey animal killed by a hunting predator (§6).
- **Migrated** (since 2026-10-09): above its cap, a random animal of the species
  leaves the world (§9). It is logged as a death with cause `migrated`.

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
| `stamina` | low, medium, high | low < 20 ≤ medium ≤ 40 < high, of 60 (`stamina_low`, `stamina_high`) |
| `food` | here, adjacent, close, medium, far, none | nearest food item; `here` = on its cell |
| `predator` | adjacent, close, medium, far, none | nearest predator |
| `animal` | adjacent, close, medium, far, none | nearest other prey animal |
| `animal_ready` | true / false | is that animal ready to mate (adult, energy ≥ 50)? Seen up to `perception.partner_range` = 20 cells, the whole vision |
| `age` | young, adult | young before 150 ticks |

**A predator senses:**

| Field | Values | Rule |
|---|---|---|
| `energy` | low, medium, high | same thresholds |
| `stamina` | low, medium, high | low < 10 ≤ medium ≤ 20 < high, of 30 |
| `prey` | adjacent, close, medium, far, none | nearest prey animal not in cover (prey in cover are hidden, §1) |
| `carcass` | adjacent, close, medium, far, none | nearest carcass it may eat from: portions left, not its own kill, not eaten from yet |
| `animal` | adjacent, close, medium, far, none | nearest other predator |
| `animal_ready` | true / false | is that predator ready to mate? Seen up to 20 cells |
| `age` | young, adult | young before 150 ticks |

That gives 3 × 3 × 6 × 5 × 9 × 2 = **4 860 possible prey observations**:
energy × stamina × food × predator × animal (none, or one of the 4 bands, ready
to mate or not) × age. Predators have 3 × 3 × 5 × 5 × 9 × 2 = **4 050**.
Without stamina and carcasses (until later on 2026-10-07) they had 1 620 and
270; before the distance bands the prey had 288. Situations repeat less often
now, so the memo and the caches answer fewer decisions (§7).

**Seeing a partner.** Until 2026-10-07 an animal saw whether another was ready
to mate only within 4 cells (`partner_range` 4), so far partners were
invisible and the few predators rarely met. Setting `partner_range: 4` gives
back that rule.

Animals don't sense directions, terrain, cover, how many animals or how much
food there is, or anything about another animal beyond whether it is ready to
mate.
A prey animal doesn't know whether a predator is hunting or digesting, nor how
much stamina it has left.

### Observation text

The brain receives the observation as text (`obs_text.render`), in one of two
styles set by `backend.obs_style`. Each band is written as its range of cells,
taken from the config, so the brain knows how far things are and how far it
can see:

| Style | Prey | Predator |
|---|---|---|
| **V1** (default, terse) | `Energy: low. Stamina: high. Food: 2-4 cells away. Predator: 5-10 cells away. Animal: none within 20 cells. Age: adult.` | `Energy: medium. Stamina: low. Prey: 2-4 cells away. Carcass: none within 20 cells. Other predator: none within 20 cells. Age: adult.` |
| **V2** (first person) | `I am hungry and weak. I am rested. The nearest food is 2-4 cells away. The nearest predator is 5-10 cells away. No other animal within 20 cells. I am an adult.` | `I have some energy. I am out of breath. The nearest prey is 2-4 cells away. No carcass within 20 cells. No other predator within 20 cells. I am an adult.` |

On a food cell V1 says `Food: here.` V1 also says whether the other animal is
ready to mate, for example `Animal: 11-20 cells away, ready to mate.` In V2 the
stamina levels read *I am out of breath.*, *I am getting tired.* and *I am
rested.* Observation sets made before the stamina change
(`data/observations_v2.jsonl`) have no stamina, and their text stays as it was.

## 6. Actions: five for prey, four for predators

The brain picks one action, one per gene of the species. An **executor** in
`actions.py` carries it out, one tick at a time, until the next decision. The
LLM never handles movement itself.

| Species | Action | What the executor does each tick | Searches instead when |
|---|---|---|---|
| prey | `eat` | On a food cell: eat (no move). Otherwise walk one cell toward the nearest visible food and eat on arrival in the same tick. | no food within 20 cells |
| prey | `flee` | Run away from the nearest predator, `speed` cells per tick (1 for prey). With cover (§1): stay put when already in cover, or run into cover within 6 cells instead. | no predator within 20 cells |
| both | `follow` | Walk toward the nearest other animal of its species; stay put once adjacent. Predators have it since 2026-10-07. | no other animal of its kind within 20 cells |
| both | `rest` | Stay still; costs only 0.2 energy per tick, plus 0.3 while stamina comes back. | never |
| both | `mate` | Walk toward the nearest mate-ready animal of its species; next to it, they breed (§8), whatever the partner chose. | no mate-ready partner within 20 cells |
| predator | `hunt` | Run toward the nearest prey animal (not in cover) or carcass it may eat from, up to 2 cells per tick, stopping next to it. Next to a carcass: eat a portion (+30, then 25 ticks of digestion). Next to a prey animal (distance ≤ 1), strike: the prey dies with probability `kill_p` = 0.5. A kill feeds the predator (+60), starts its digestion (§3) and leaves a carcass for up to two other predators. | no prey outside cover and no carcass it may eat from within 20 cells |

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

**Speed and stamina.** Nobody moves more cells than its stamina allows (§4).
A hunting predator runs 2 cells per tick and a fleeing prey animal 1, so the
predator gains a cell per tick for as long as its stamina lasts: 15 ticks from
full. After that it alternates a tick standing still (2 points back) and a tick
of running (2 cells): one cell per tick on average, no faster than a fleeing
prey animal, which keeps its distance while its own stamina lasts (60 ticks of
flight). A prey animal gets caught when the predator starts close, or when it
stops (to eat, rest or mate), runs out of stamina, moves toward the predator or
reaches a border. Animals decide only every 4 ticks (§7), so a predator can
close in between two decisions. A predator strikes in the same tick it arrives
next to its prey, so fleeing has to start before the predator is adjacent.
Until the stamina change both species moved at most one cell per tick, and a
prey animal that fled kept its distance.

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
  answered 52–67 % of prey decisions and 60–78 % of predator decisions (61–81 %
  and 75–84 % before stamina and carcasses were sensed). Before the distance
  bands (5 genes, 12-cell vision, no mutation) it was 77–89 %.

All decisions due on the same tick are sent to the brain in one batch per
species. The simulation waits for the answers before moving on (**lockstep**),
so a slow brain makes the run slower but never changes its result.

## 8. Reproduction

**Sexual (default, `evolution.sexual: true`).** A litter is born when two
animals of the same species:

- at least one of them chose `mate` at its last decision (since 2026-10-07; before, both had to),
- are both mate-ready (age ≥ 150 ticks and energy ≥ 50),
- are adjacent (Chebyshev distance ≤ 1),
- haven't bred yet in this decision period.

**Litters (since 2026-10-09; one child before).** A mating makes 2–4 babies
(`litter: [2, 4]` for both species, drawn uniformly from the `litter` random
stream). Every baby gets its own crossover of the two parents and its own
mutations ([04 — Genome and evolution](04-genome-and-evolution.md)), so
siblings usually differ. Each baby costs the parents `child_energy` = 40,
shared (20 each), and starts with 40 energy on the first parent's cell. Its
generation is the larger parent generation + 1.

The litter is cut back, never below 2, so that no parent pays more energy than
it has: a parent at 50 energy pays for 2 babies and keeps 10, a parent at 100
can pay for 4. With the old cap rule (`block`, §9) it is also cut to the places
left under the cap. `litter: [1, 1]` gives the old rule. `birth` events carry
the litter size.

**Asexual (`evolution.sexual: false`, the old lab's regime).** A single
mate-ready animal that chose `mate` copies its genome (plus mutation, per baby)
and pays the full 40 energy per baby.

## 9. Population limits: cap and floor

| Parameter | Prey (`agents.*`) | Predators (`predators.*`) | Effect |
|---|---|---|---|
| `cap` | 270 (`small`: 40; 135 in the 96 × 96 world) | 68 (`small`: 6; 34 before) | the species' limit (below) |
| `cap_rule` | `migrate` | `migrate` | what happens at the cap (below) |
| `floor` | 10 | 3 | if fewer remain, newcomers with fresh founder genomes are added at random cells |

**Cap rule `migrate` (since 2026-10-09).** Births go on at the cap. At the end
of the tick, if a species is above its cap, randomly chosen animals of that
species leave the world until it is back at the cap. They are logged as deaths
with cause `migrated`. Every animal is equally likely to leave, so leaving
favours no gene; only babies born in that tick are spared. With plentiful food
migration was the most common death (57–63 % of prey deaths); in the hungrier,
deadlier world of §13 it fell to 50 % in the screen and 20–23 % in the JEV runs
([06 §5.20–5.21](06-experiments-and-results.md#520-jev-runs-egg-bank-cover-litters-and-migration)).

**Cap rule `block` (before 2026-10-09).** No births while the species is at its
cap. With litters, populations then sat at their caps and most births just
filled free places.

**One shared cap (tried 2026-10-09, reverted the same day).** One cap of 500
for both species together (`sim.cap`), with migration over the total. In the
screen the crashes came back: without their own cap, predators multiplied to
250–330 and ate the prey out (2 of 8 runs lasted); with slower-breeding
predators the prey filled the 500 places and migration pushed the predators out
(0 of 8). The separate predator cap is what keeps predators in check
([06 §5.19](06-experiments-and-results.md#519-boom-and-bust-how-to-stop-the-crashes)).

**Floors and newcomers.** Newcomers are logged as `immigrant` events and
counted in `stats.csv`. They keep a species alive while its behaviour is poor.
For example, predators with a random brain survive only because of their floor
(§13). But a newcomer brings founder genes, so a crash that empties a species
restarts its evolution. With litters and migration no newcomer was needed in
any run since 2026-10-09.

**Egg bank (option, off).** `evolution.egg_bank` refills the floor with eggs
instead of founders: every birth lays an egg (the parents' genomes), and below
the floor a random egg from the last `egg_ticks` = 2 000 ticks hatches as a
delayed birth, with crossover and mutation at hatching and its real parents
and generation. Founders come only if no egg is left. It kept lineages going
through crashes, but the crashes went on, and the owner judged it not a valid
idea (2026-10-08).

## 10. One tick, in order

`Simulation.step()` does the following:

1. **Decide:** every 4th tick all animals of both species decide, one batch per
   species; on other ticks only animals without an action (newborns, newcomers,
   predators done digesting). Digesting predators don't decide.
2. **Prey act** in a random order, then pay the tick's energy cost and spend
   or recover stamina (§4).
3. **Prey breed:** an animal that chose `mate` and a ready partner next to it produce a litter of 2–4 (§8).
4. **Predators act** in a random order and pay their energy cost. A hunting
   predator can kill, which leaves a carcass, or eat from a carcass (§6); a
   digesting one stays still.
5. **Predators breed.**
6. **Killed prey are removed**, with cause `predator` and the killer's id.
7. **Age and die:** ages increase by 1; animals with energy ≤ 0 starve, animals
   older than 1 500 ticks die of old age.
8. **Migrate:** a species above its cap loses random animals, except this
   tick's babies, until it is back at the cap (`cap_rule: migrate`, §9).
9. **Food regrows** (§2) and **carcasses rot**: eaten-up ones and those 100
   ticks old are removed.
10. **Floors:** newcomers are added to each species below its floor (eggs
    first if the egg bank is on, §9).
11. Every `sim.stats_every` = 100 ticks, a row is written to `stats.csv`.

## 11. Randomness and reproducibility

- There is no global random generator. Each part of the simulation draws from
  its own named stream (`promptevo/rng.py`): `world`, `agents` (prey founders
  and newcomers), `predators` (predator founders and newcomers), `sampling`,
  `actions` (moves and kill rolls), `mutation`, `food`, `litter` (litter sizes)
  and `migration` (who leaves above the cap). Changing one part,
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
| `events.jsonl` | one line per event: `founder`, `immigrant` (id, genome), `birth` (child id, parents, generation, genome, mutations with the locus, parent and new allele, instruction number and new text, `litter` size since 2026-10-09; a hatched egg also has `laid`, the tick its egg was laid), `death` (cause `predator`, `starvation`, `old_age` or `migrated`, age, generation, food eaten or a predator's meals (kills and carcass portions), offspring; `killer` when a predator killed it). Predator events carry `"species": "predator"`; prey events have no `species` field. |
| `stats.csv` | every 100 ticks: `t, pop, mean_energy, mean_stamina, exhausted, mean_gen, max_gen, births, immigrants, deaths_starve, deaths_pred, deaths_age, decisions, backend_queries, invalid, alleles`, decisions per action `act_eat` … `act_mate`, then the predators' columns with the prefix `pred_` (`pred_pop` … `pred_invalid`, with `pred_portions` in place of `deaths_pred`, then `pred_act_hunt` … `pred_act_mate`). `deaths_pred` counts prey killed by predators, `pred_portions` carcass portions eaten, `exhausted` animal-ticks that began without stamina for one cell; counts are cumulative; `alleles` covers both species. Runs before the stamina change have no stamina or portion columns. |
| `alleles.jsonl` | every allele seen in the run, both species: id, locus, text, origin, parent allele, operator (`llm#<n>`: the mutation instruction drawn), model, seed (the lineage of every gene) |
| `final_population.json` | the living animals at the end: id, generation, genome (allele ids); predators carry `"species": "predator"` |
| `run_info.json` | what ran: command (with any `--set` changes), git commit, Ollama version and model digests, the mutator (API, host, model, digest), the JEV checkpoint for `--backend jev`, brain, seed, the main config sections |
| `summary.json` | totals for the prey at the top level (births, newcomers, hatched eggs, deaths by cause, mean lifespan, maximum generation, decisions, brain queries, memo hit rate, brain time, invalid rate, share of animal-ticks out of breath `exhausted_share`, share of each action), the same for the predators under `predators` (plus `kills` and `portions`), mutation counts, number of alleles, event hash; why the run stopped, minutes, model calls not answered by the cache, failures |

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

Measured on 2026-10-09 with the current settings: 192 × 192, caps 270 / 68
with migration, litters of 2–4, food regrowth 0.0015 and 10 % at the start,
hungry cover on 20 % of cells, `kill_p` 0.5. Keyword brain, no mutation, 5 000
ticks, populations from tick 1 000 onward, seeds 1234, 7 and 42
(`results/runs/ref_1009_<profile>_<seed>`).

| | full (192 × 192, caps 270 / 68) | small (48 × 48, caps 40 / 6) |
|---|---|---|
| **Prey:** mean population | 264–268 (264.2 / 267.8 / 267.4) | 38 (38.1 / 37.8 / 37.8) |
| Time at the floor / at the cap | 0 % / 2–29 % | 0 % / 22–29 % |
| Births (5 000 ticks) | 8 293–9 046 | 1 106–1 197 |
| Deaths: predator / starvation / migrated | 2 817–2 859 / 1 905–2 366 / 2 945–4 149 | 317–337 / 341–388 / 393–488 |
| Mean lifespan · generations reached | 143–154 ticks · 28–29 | 153–164 ticks · 29 |
| Newcomers | 0 | 0 |
| Mean stamina (of 60) · animal-ticks out of breath | 25–38 · 1–6 % | 34–41 · 1–2 % |
| **Predators:** mean population | 66 (66.1 / 66.5 / 66.0) | 6 (5.9 / 5.9 / 6.0) |
| Time at the floor (3) / at the cap | 0 % / 46–56 % | 0 % / 90–98 % |
| Births · newcomers | 1 757–1 786 · 0 | 162–173 · 0 |
| Kills · carcass portions eaten | 2 817–2 859 · 4 972–5 024 | 317–337 · 495–533 |
| Deaths: starvation / old age / migrated | 507–526 / 0 / 1 191–1 234 | 2–3 / 0 / 158–169 |
| Mean lifespan · generations reached | 180–183 ticks · 27–28 | 168–182 ticks · 25–27 |
| Invalid actions (the animal searches instead, §6): prey / predators | 1 % / 13–16 % | 0–1 % / 1 % |
| Memo hit rate: prey / predators | 65–80 % / 46–58 % | 55–59 % / 71–76 % |

Decisions in the full world: prey eat 60 %, rest 15 %, flee 12 %, follow 10 %,
mate 3 %; predators hunt 72 %, rest 14 %, follow 8 %, mate 6 %.

- **Fast turnover.** About 6 generations per 1 000 ticks (4–6 before), and
  animals live 140–180 ticks (228–1 380 before). In the screen with the LLM's
  answers, migration instead of blocked births tripled the generations reached
  ([06 §5.20](06-experiments-and-results.md#520-jev-runs-egg-bank-cover-litters-and-migration)).
- **Deaths mostly depend on behaviour.** Of the keyword prey's deaths, 33 % are
  kills, 25 % starvation and 42 % migration. JEV-read prey eat and flee worse:
  in the JEV runs of this world 39 % were killed, 38 % starved and 20–23 %
  migrated ([06 §5.21](06-experiments-and-results.md#521-an-evolution-test-world)).
- **Predators search more.** 13–16 % of predator decisions find no prey in
  sight, against 0–6 % before: the world is bigger and prey hide in cover.
- **No crashes** in these runs or in the no-rescue screen of this world with the
  LLM's own answers (8 of 8 lasted 10 000 ticks; 06 §5.19). The random brain
  wasn't measured again.

One keyword tick takes about 17 ms in the full world (5 000 ticks ≈ 1.4 min) and
1 ms in the small one.

### 2026-10-07: the 96 × 96 world, before litters, migration and cover

Measured on 2026-10-07, after the stamina change, with the keyword brain and
no mutation, 5 000 ticks, populations from tick 1 000 onward, seeds 1234, 7 and
42.

| | full (96 × 96, caps 135 / 34) | small (48 × 48, caps 40 / 6) |
|---|---|---|
| **Prey:** mean population | 81–94 (81.1 / 94.1 / 92.2) | 28–31 (30.8 / 28.1 / 29.2) |
| Time at the floor / at the cap | 0–5 % / 0–3 % | 0 % / 0–1 % |
| Deaths (5 000 ticks): predator / starvation | 995–1 206 / 396–644 | 214–252 / 282–325 |
| Mean lifespan · generations reached | 228–245 ticks · 20–28 | 252–271 ticks · 25–27 |
| Newcomers | 0–24 | 0 |
| Mean stamina (of 60) · animal-ticks out of breath | 23–26 · 2–3 % | 23–25 · 2–4 % |
| **Predators:** mean population | 28–34 (33.9 / 28.2 / 33.9) | 5.6–5.8 (5.8 / 5.7 / 5.6) |
| Time at the floor (3) / at the cap | 0 % / 66–97 % | 1–10 % / 84–90 % |
| Births · newcomers | 126–152 · 0 | 16–20 · 1–2 |
| Kills · carcass portions eaten | 995–1 206 · 1 496–1 806 | 214–252 · 281–299 |
| Deaths: starvation / old age | 19–90 / 42–89 | 0–1 / 16–18 |
| Mean lifespan · generations reached | 824–1 380 ticks · 8–12 | 1 436–1 501 ticks · 7–9 |
| Invalid actions (the animal searches instead, §6): prey / predators | 1–2 % / 0–6 % | 2 % / 0 % |
| **Random brain** instead | prey 90–98; predators stay at their floor, 88–104 newcomers | prey 19–20 (at their floor 3–6 % of the time); predators stay at their floor, 75–98 newcomers |

Decisions in the full world: prey eat 48 %, rest 20 %, flee 20 %, follow 7 %,
mate 5 %; predators mate 46 %, hunt 41 %, rest 9 %, follow 4 %.

With carcasses each kill feeds up to three predators, so the keyword predators
are limited by their cap rather than by food: they rarely starve, and most die
of old age. Well fed, they spend much of their time digesting (50 ticks per
kill, 25 per portion), and their stamina stays near full (28–29 of 30). The
prey run low on stamina (23–26 of 60 on average) and rest in a fifth of their
decisions, three times as often as before. In the full world random prey are as
many as keyword prey, because random predators hardly catch anything (81–94
kills in 5 000 ticks).

**Before the stamina change** (the same day: one cell per tick for both species,
no stamina, no carcasses), the same runs gave prey 74–93 and predators 15–21 in
the full world (200–266 predator births, 18–19 generations, mean predator
lifespan 362–369 ticks, most predators starved), and prey 22–25 and predators
5.3–5.8 in the small world. Decisions in the full world: prey eat 63 %, flee
18 %, rest 7 %, follow 7 %, mate 6 %; predators hunt 78 %, mate 10 %, rest 8 %,
follow 5 %.

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
- Stamina for both species, predators running 2 cells per tick when hunting,
  and carcasses that feed up to two more predators (§3, §4, §6). The keyword
  predators went from 15–21 to 28–34 in the full world (their cap is 34), and
  the prey stayed at 81–94.

Until the stamina change predators stayed few: prey production set the limit,
and making predators cheaper or deadlier gave more predators only until the
prey collapsed (§3). Carcasses changed that, since a kill now feeds up to three
predators. Before 2026-10-07, with
scripted predators and the 12-cell vision, the same runs gave 25–31 prey in
the small world and 50–55 in the full world.

Food regrowth was tuned to 0.0007 (2026-10-01, with the 10-gene genome and
scripted predators) so that the population stays limited by food, below the
cap. At the earlier 0.001 the flat world sat at the cap 25–68 % of the time,
and births then depend on free slots rather than on finding food. With
predators that hunt, the prey stay below the cap almost all the time in both
worlds. One simulated tick with the keyword brain takes about 1 ms in the small
world and 4 ms in the full one on a desktop CPU (5 000 ticks ≈ 3 s and 22 s).
