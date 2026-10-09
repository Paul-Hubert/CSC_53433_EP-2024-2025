# 12 — Tick order and determinism

## 1. Phases

- **TICK-01 (MUST)** A world runs an ordered list of **phases** every tick. The
  list is configuration: a phase can be replaced, removed, or added anywhere
  (seasons, disease, weather) without changing the others.
- **TICK-02 (MUST)** A phase changes only what it is responsible for. What it
  changes is visible to the phases after it in the same tick.
- **TICK-03 (MUST)** A phase that needs answers from a service (decisions,
  mutations) says whether it is ready; the world doesn't run it before it is
  (SPACE-13).

**TICK-04 (MUST) The reference phase list.**

| # | Phase | Does | Waits for |
|---|---|---|---|
| 1 | **Sense** | picks who decides (DEC-01); builds spatial indexes; reads every sense of every deciding animal (batched raycasts) | — |
| 2 | **Ask brains** | memo lookups; sends one batch per brain | — |
| 3 | **Choose actions** | stores answers in the memo and cache; draws each animal's action | the brains |
| 4 | **Act** | in the act order (ACT-30): each animal's action → intent → locomotion → interactions; then its metabolism; busy animals count down | — |
| 5 | **Breed** | conceives litters (REPRO-02), lays eggs, draws and sends mutation requests | — |
| 6 | **Hatch** | eggs due this tick become animals | the mutator |
| 7 | **Deaths** | removes killed animals; every animal ages by 1; starvation; old age | — |
| 8 | **Migration** | cap rules (POP-01) | — |
| 9 | **Environment** | resource layers regrow; entities rot, move or vanish | — |
| 10 | **Floor** | newcomers for species below their floor (POP-03) | — |
| 11 | **Record** | a stats row every `statsEvery` ticks; periodic flushes | — |

Then the tick count grows by one.

- **TICK-05 (MUST)** Decisions use the state at the start of the tick, i.e. after
  the previous tick's floor phase.
- **TICK-06 (MUST)** Breeding is its own phase, after all animals acted (owner
  decision). The prototype bred each species right after it acted (prey act,
  prey breed, predators act, predators breed); the difference only affects an
  animal that would breed and then be killed later in the same tick.
- **TICK-07 (MUST)** Phases run in the same order in every tick; a phase that has
  nothing to do (no decisions due, no eggs) returns at once.

## 2. Randomness

- **RAND-01 (MUST)** Simulation code uses no global or unseeded random source:
  no `UnityEngine.Random`, no `new System.Random()` without a seed, no GUIDs or
  time as randomness.
- **RAND-02 (MUST)** Every draw comes from a **named stream** derived from the run
  seed and the stream's name. Streams are independent: adding draws to one
  never shifts the numbers of another, so changing how food regrows doesn't
  change who mates.
- **RAND-03 (MUST)** Streams are per purpose and, where a species is involved, per
  species, so adding a species doesn't change the draws of the others:

  | Stream | Used for |
  |---|---|
  | `world` (own seed allowed) | terrain, cover, initial food |
  | `<layer>` | regrowth of one resource layer |
  | `act-order` | the order animals act in |
  | `<species>/founders` | founder and newcomer genomes and positions |
  | `<species>/sampling` | drawing actions |
  | `<species>/actions` | wandering, sidesteps, strike rolls |
  | `<species>/litter` | litter sizes |
  | `<species>/mutation` | crossover and mutation choices |
  | `<species>/migration` | who leaves above the cap |

- **RAND-04 (MUST)** The world may have its own seed, so many runs share one map
  while everything else varies.
- **RAND-05 (MUST)** Simulation code iterates only ordered collections (lists by
  creation or id). Hash sets and dictionaries are never iterated to make a
  decision.

## 3. The events hash

- **RAND-10 (MUST)** Every recorded event updates a running SHA-256 hash of its
  canonical text (keys sorted, fixed number formatting). The final hash
  identifies the run.
- **RAND-11 (MUST)** The same configuration, seed and answer caches give the same
  hash on the same platform and build, whatever the wait mode, frame rate, ticks
  per frame, and whether anything is rendered.
- **RAND-12 (MAY)** Hashes are compared on one machine only (owner decision).
  Across machines and scripting backends floating-point results may differ, and
  so may hashes; nothing relies on them matching.
- **RAND-13 (MUST)** Two different seeds give different hashes.

## 4. Stopping and resuming

- **RAND-20 (MUST)** A run stops cleanly at a tick boundary (button, stop file,
  tick limit, wall-clock limit, a brain failure in strict mode, an exception),
  with every output written.
- **RAND-21 (SHOULD)** Resuming = running the same configuration and seed again:
  the part already done replays from the answer caches without model calls,
  then the run continues. Saving and loading a full snapshot MAY replace replay.
- **RAND-22 (SHOULD)** Outputs are flushed regularly (reference: events and stats
  every 500 ticks, the allele list every 5 000), so a hard stop loses little.
