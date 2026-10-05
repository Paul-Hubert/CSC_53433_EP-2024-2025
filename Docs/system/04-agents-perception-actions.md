# 04 — Agents, perception, actions

What an agent is, what it perceives, how a situation becomes text, and what each of the 7 actions does (`sim.py`, `perception.py`, `obs_text.py`, `actions.py`). Status ✅.

## Agent

`Agent` is a plain mutable dataclass in `prototype/promptevo/sim.py` (`eq=False`,
so identity comparison).

| Field | Type / default | Meaning |
|---|---|---|
| `id` | int | Unique per run, increasing. |
| `y`, `x` | int | Grid cell. Several agents may share a cell. |
| `energy` | float | Starts at `energy_start` (60) for founders/immigrants, `child_energy` (40) for children; capped at `energy_max` (100) when gaining. |
| `genome` | `Genome` | 10 allele ids ([05](05-genome.md)). |
| `generation` | 0 | 0 for founders and immigrants; children: max(parents) + 1. |
| `parents` | `()` | Parent ids (2, 1 in asexual mode, none for founders). |
| `born` | 0 | Tick of creation. |
| `age` | 0 | Ticks lived; +1 at the end of each tick. |
| `heading` | 0 | Direction 0–7 for the random walk; random at creation. |
| `action` | `None` | Current action; `None` until the first decision (marks a newcomer). |
| `invalid` | False | Set when the executor fell back to wander this period. |
| `attacked` | False | Set after the first attack attempt this period (max one per period). |
| `bred` | False | Set after breeding this period (max once per period). |
| `food_eaten`, `offspring`, `steals` | 0 | Lifetime counters, logged at death. |
| `immigrant` | False | Spawned by the floor rule. |
| `killed` | False | Marked by a predator; removed the same tick. |

`invalid`, `attacked` and `bred` are reset at each decision.

## Observation

`Observation` (`perception.py`) is a **frozen** (hashable) dataclass with
discretised values only, so the number of distinct situations is small
(a few hundred) and caches hit often.

| Field | Values | Rule (`sense()`) |
|---|---|---|
| `energy` | `low` / `medium` / `high` | `low` if energy < `energy_low` (30); `high` if energy > `energy_high` (70); else `medium` |
| `food` | `none` / `far` / `near` / `here` | nearest food in the vision window: distance 0 → `here`; ≤ `near` (3) → `near`; ≤ `vision` (12) → `far`; none within 12 → `none` |
| `predator` | `none` / `far` / `near` | nearest predator (resting ones included): ≤ 3 → `near`, ≤ 12 → `far`, else `none` |
| `animal` | `none` / `far` / `near` | nearest other agent, same buckets |
| `animal_ready` | `True` / `False` / `None` | only when `animal == near`: `mate_ready(other)` (age ≥ 150 and energy ≥ 50), regardless of its action |
| `animal_stronger` | `True` / `False` / `None` | only when `animal == near`: other's energy **>** own energy (equal → `False`, rendered "weaker") |
| `age` | `young` / `adult` | `young` if age < `maturity` (150) |

All distances are Chebyshev. Constructor order is
`Observation(energy, food, predator, animal, animal_ready=None,
animal_stronger=None, age="adult")`; tests build observations positionally.

### Tags and `RELEVANT_TAG`

`Observation.tags()` says which directed tests ([09](09-metrics-and-experiments.md))
an observation is relevant for:

| Tag | When |
|---|---|
| `any` | always |
| `food_present` | `food != none` |
| `predator_present` | `predator != none` |
| `animal_present` | `animal != none` |
| `animal_near` | `animal == near` |

`RELEVANT_TAG` maps each action locus to the tag its directed test needs:
`eat → food_present`, `flee → predator_present`, `follow → animal_present`,
`wander → any`, `rest → any`, `mate → animal_near`, `attack → animal_near`.

The committed observation set `data/observations_v1.jsonl` has 48 rows
(32 synthetic + 16 sampled from a rule-based run, `experiments/make_obs.py`);
tag counts: any 48, food_present 39, animal_present 39, animal_near 34,
predator_present 18.

## Observation text

`obs_text.render(obs, style)`; the style comes from `backend.obs_style` (V1).
Unknown styles raise `ValueError`.

**V1 — terse.** `Energy: {energy}. Food: {food}. Predator: {predator}. Animal: {animal}[, ready to mate|, not ready to mate][, stronger|, weaker]. Age: {age}.`

```text
Energy: low. Food: near. Predator: none. Animal: near, ready to mate, weaker. Age: adult.
Energy: medium. Food: here. Predator: far. Animal: none. Age: adult.
```

**V2 — first person.** One fixed sentence per field value:

| Field | Sentences |
|---|---|
| energy | low "I am hungry and weak." · medium "I have some energy." · high "I am well fed and strong." |
| food | none "I see no food." · far "There is food far away." · near "Food is close." · here "I am standing on food." |
| predator | none "No predator in sight." · far "A predator is far away." · near "A predator is very close!" |
| animal | none "I am alone." · far "Another animal is far away." · near "Another animal is next to me." + "It is (not) ready to mate." + "It is stronger than me." / "It is weaker than me." |
| age | young "I am young." · adult "I am an adult." |

```text
I am hungry and weak. Food is close. No predator in sight. Another animal is next to me. It is ready to mate. It is weaker than me. I am an adult.
I have some energy. I am standing on food. A predator is far away. I am alone. I am an adult.
```

Note: V2 says "next to me" for `near`, which covers distances up to 3.

## Actions

`ACTIONS = ("eat", "flee", "follow", "wander", "rest", "mate", "attack")` —
this order indexes every probability vector. `EXECUTORS` maps each name to a
function `(agent, world, agents, cfg, rng) → moved: bool`, run **once per tick**
for the D ticks until the next decision. `rng` is the `actions` stream.

| Action | Executor behaviour (one tick) | Invalid when |
|---|---|---|
| `eat` | On a food cell: eat (+25, cap 100), no move. Else step toward the nearest food within vision; if the new cell has food, eat it in the same tick. | no food within vision |
| `flee` | Step **away** from the nearest predator within vision (`step_toward(…, away=True)`). | no predator within vision |
| `follow` | Nearest other agent within vision: if adjacent (≤ 1) stay; else step toward it. | no agent within vision |
| `wander` | One step of the persistent random walk (`wander_turn_p` = 0.25). | never |
| `rest` | Stay. | never |
| `mate` | Nearest **mate-ready** agent within vision (whatever that agent chose): if adjacent stay (breeding is resolved by the simulation); else step toward it. | no mate-ready agent within vision |
| `attack` | Nearest other agent within vision: if adjacent and no attack yet this period, resolve one attack (below) and stay; if adjacent and already attacked, stay; else step toward it. | no agent within vision |

**Invalid-action fallback.** When the target is missing, `_invalid()` sets
`agent.invalid = True` and performs a `wander` step instead (plan 08 §A6; STATUS
› Decisions 2026-09-28). The simulation counts it once per decision
(`invalid_rate` in the summary). The flag is not cleared within the period: if
a target appears on a later tick, the executor acts normally.

### Attack resolution

```text
attacker.energy −= attack_cost                         (3)
p_win = E_attacker / max(1e-6, E_attacker + E_target)  (energies after paying the cost)
if rng < p_win:
    stolen = min(attack_steal, E_target)               (10)
    target.energy −= stolen
    attacker.energy = min(energy_max, attacker.energy + stolen)
    attacker.steals += 1
```

At most one attack per agent per decision period (`attacked` flag). The target
is any nearest agent, whatever it is doing; the target does not fight back. A
target that ends the tick at ≤ 0 energy after being robbed is logged as an
`attacked` death (not `starvation`).

### Energy costs

| Source | Amount | Where |
|---|---|---|
| Base metabolism (any action except an idle `rest`) | −0.7 per tick | `sim.step` |
| Moving one cell | additional −0.5 | `sim.step` |
| Resting (`rest`, not moving) | −0.2 per tick instead of base | `sim.step` |
| Eating | +25, capped at 100 | `do_eat` |
| Attack attempt | −3, then +up to 10 on success | `do_attack` |
| Breeding | −20 per parent (sexual), −40 (asexual) | `sim._birth` |

An agent standing on food and choosing `eat` pays the base cost (0.7) that tick;
a moving `eat` pays 1.2. Plan 08 §A4 started from a base cost of 0.5; 0.7 is the
tuned value (STATUS, 2026-09-28).
