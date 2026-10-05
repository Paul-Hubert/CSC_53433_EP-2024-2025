# 02 — World

The headless grid world: terrain, walkability, food, predators and movement helpers (`prototype/promptevo/world.py`). Status ✅.

## Grid and cell types

| Constant | Value | Walkable |
|---|---|---|
| `GRASS` | 0 | yes |
| `WATER` | 1 | no |
| `MOUNTAIN` | 2 | no |

- Size `world.width × world.height`: 64 × 64 in `base`/`full`, 48 × 48 in `small`.
- Coordinates are `(y, x)`, row first. There is no wrap-around.
- `DIRS` lists the 8 neighbour offsets `(dy, dx)` in this fixed order:
  `(-1,-1) (-1,0) (-1,1) (0,-1) (0,1) (1,-1) (1,0) (1,1)`. A *heading* is an
  index 0–7 into `DIRS`.

## Distance

`cheb(ay, ax, by, bx) = max(|ay − by|, |ax − bx|)` — **Chebyshev** distance (a
diagonal step counts as 1). It is used for everything that is a *range*: vision
(12), "near" (3), predator chase radius (6), adjacency for mating and attacking
(≤ 1), and the food search window. Only `step_toward` scores candidate steps with
Euclidean distance (`np.hypot`).

## Terrain generation

`World.__init__(cfg, rng_world, rng_pred)`:

1. **Height map** — `value_noise(h, w, octaves, rng)`. For each octave cell size
   `c` in `noise_octaves` (`[16, 8, 4]`), draw a random lattice of
   `(h//c + 2) × (w//c + 2)` values, interpolate bilinearly with a smoothstep
   weight (`3f² − 2f³`), and add it with amplitude `1 / 2^i` (octave `i`). The
   sum is normalised to [0, 1].
2. **Quantile thresholds** — cells below the `water_fraction` quantile (0.15)
   become water; cells above the `1 − mountain_fraction` quantile (0.90) become
   mountain. Exact fractions, independent of the noise scale.
3. **Connectivity repair** — `largest_component()` labels 8-connected grass
   regions (BFS). If the largest region covers at least
   `min_walkable_connected × h × w` (0.60), every grass cell outside it is turned
   into mountain ("unreachable pockets become rock"). Otherwise a new height map
   is drawn, up to 50 attempts, then `RuntimeError("could not generate a
   connected world; relax fractions")`.

Result: one connected walkable region. `tests/test_world.py` checks the walkable
share is between 0.6 and 0.8 and that the same seed gives the same terrain.

Derived arrays kept on the world:

| Attribute | Meaning |
|---|---|
| `terrain` | int8 grid of cell types |
| `walkable` | `terrain == GRASS` |
| `walk_cells` | list of walkable `(y, x)`, used by `random_cell(rng)` for spawns |
| `near_water` | walkable cells with any water within a square window of radius `water_bonus_radius` (3), i.e. Chebyshev ≤ 3 |
| `regrow_p` | per-cell regrowth probability (below) |

`is_walkable(y, x)` is false outside the grid, so the border acts as a wall.

## Food

| Aspect | Rule | Config |
|---|---|---|
| Initial food | each walkable cell has food with probability 0.08 (drawn from the `world` stream) | `world.food_initial_fraction` |
| Regrowth | every tick, each cell gains food with probability `regrow_p` (`food \|= rng.random() < regrow_p`, `food` stream) | `world.food_regrow_p` = 0.001 |
| Near-water bonus | `regrow_p` × 2 on `near_water` cells, so 0.002 | `world.food_water_bonus`, `world.water_bonus_radius` |
| Non-walkable cells | `regrow_p = 0`; food never appears there | — |
| Capacity | boolean grid: at most one food item per cell | — |
| Eating | `do_eat` sets the cell to `False` and adds `eat_gain` (25) energy, capped at `energy_max` (100) | `agents.*` |

`nearest_food(y, x, radius)` searches the square window of Chebyshev radius
`radius` (the agent's vision, 12) and returns `(fy, fx, distance)` of the
nearest food, or `None`. Ties go to the first hit in row-major order
(`np.argmin`).

The rate 0.001 was tuned on 2026-09-28 so that the population is food-limited
(STATUS › Decisions).

## Predators

Scripted, never evolved. `Predator(y, x, heading, rest=0)`.

| Aspect | Rule | Config (base) |
|---|---|---|
| Count | `predators.count` at random walkable cells, random heading | 3 (small: 2) |
| Rest | while `rest > 0`: decrement and do not move; cannot kill | `rest_after_kill` = 20 |
| Chase | otherwise, target the nearest agent with Chebyshev distance ≤ `chase_radius`; ties go to the earlier agent in the list; move one step with `step_toward` | `chase_radius` = 6 |
| Wander | no target in range: one step of `step_heading` (persistent random walk) | `turn_p` = 0.2 |
| Kill | handled by the simulation after predators move: a non-resting predator on the same cell as an agent kills the first such agent with probability `kill_p`, then rests | `kill_p` = 0.3 |

Movement (`move_predators`) and kill draws use `world.rng_pred`, which is the
**simulation's** `predators` stream, not the world stream (see
[08](08-reproducibility-and-data.md)). Predators move one cell per tick, like
agents, and several predators can share a cell. Plan 08 §A4 started from 3–5
predators and kill probability 0.5; the tuned values above replaced them.

## Movement helpers

Both helpers only ever return walkable cells.

### `step_toward(y, x, ty, tx, rng, away=False)` — greedy step

```text
for each walkable neighbour n:  score = −dist(n, target)   (or +dist when away=True)
keep the best score; ties → random choice among them
if the best neighbour does not improve on the current Euclidean distance
   ("stuck", e.g. behind water):  step to a random walkable neighbour instead
no walkable neighbour:          stay
```

Used by `eat`, `flee` (with `away=True`), `follow`, `mate`, `attack` and
predator chasing. There is no path-finding: an agent can oscillate along a lake
shore, and the random side-step is the only escape.

### `step_heading(y, x, heading, turn_p, rng)` — persistent random walk

```text
with probability turn_p: heading += one of {−1, +1, −2, +2}  (mod 8)   # 45° or 90° turn
up to 8 tries: if the cell ahead is walkable → move there and return
               else heading = random 0..7
all blocked: stay
```

Returns `(y, x, heading)`; the heading persists on the agent or predator. Used by
`wander`, by every invalid action, and by predators without a target. Agents use
`agents.wander_turn_p` (0.25), predators `predators.turn_p` (0.2).

## ASCII renderer

`render.ascii_map(world, agents, max_w=48, max_h=24)` downsamples the grid into
blocks: `~` if more than half the block is water, `^` if more than half is
mountain, `.` if the block has any food, blank otherwise. Each agent is drawn as
the upper-case first letter of its current action (`?` before its first
decision), then each predator as `W`. Letters collide: `flee`/`follow` are both
`F`, and a wandering agent (`W`) looks like a predator.

## Relation to the Unity terrain

Doc [`../redesign/06`](../redesign/06-world-terrain-foliage.md) is still a
placeholder awaiting the owner's terrain and foliage labs. The prototype grid
only stands in for "some areas are inaccessible": no slopes, no movement cost
by terrain, no perception of terrain.
