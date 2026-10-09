# 02 — Space and time

The prototype lives on a grid of cells with 8-neighbour moves. The Unity system
does not: positions are continuous and change through direction and speed
vectors, on a flat plane or a Unity Terrain. Grids may still exist *inside*
modules (a food layer stored per cell, a spatial index), but never constrain
where an animal stands or how it moves.

## 1. Space

- **SPACE-01 (MUST)** An animal's position is a continuous point. Movement
  changes it by a velocity (direction × speed) over a tick, in a straight line
  in the reference. The speed belongs to the species (its locomotion
  component, [07 §2](07-actions-and-locomotion.md#2-locomotion)). Nothing snaps
  positions to cells.
- **SPACE-02 (MUST)** Distances are measured on the horizontal plane (x, z),
  Euclidean, through one world-level distance function that every module uses.
  A module MAY replace the function (e.g. path length on a NavMesh); no module
  computes distances its own way.
- **SPACE-03 (MUST)** The world is a bounded rectangle with hard borders. No
  animal or entity ever leaves it; there is no wrap-around.
- **SPACE-04 (MUST)** A *ground* module answers two questions for any point:
  its height, and whether it is walkable. No animal ever stands on a
  non-walkable point (water, too steep, an obstacle, outside the NavMesh).
- **SPACE-05 (SHOULD)** All walkable ground is connected, so every animal can in
  principle reach every walkable point. A generator that produces isolated
  pockets either removes them (makes them non-walkable) or retries.
- **SPACE-06 (MUST)** An animal's height follows the ground under it. Height
  does not count in distances (reference); slope MAY count in movement costs.

### Ground modules

| Module | Walkable | Height | Use |
|---|---|---|---|
| Flat ground (reference, Lab 1) | everywhere inside the rectangle | 0 | the evolution lab |
| Terrain | above the water level, steepness below a maximum, outside obstacle footprints (trees, rocks) | the terrain's heightmap | terrain and foliage labs |
| NavMesh | on the NavMesh baked from the terrain | the NavMesh surface | terrain labs, path finding around obstacles |

**Reference (Python).** Lab 1 is flat and fully walkable. A terrain preview
exists: a value-noise heightmap (three octaves) whose lowest 15 % becomes water
and highest 10 % mountain (both impassable); only the largest connected walkable
region is kept, and the generator retries (up to 50 times) until it covers at
least 60 % of the world. An option splits the world into k × k areas with
one-meter ridges, each with a gap of a few meters (tried against population
crashes, made them worse, off).

## 2. Spatial queries

- **SPACE-07 (MUST)** "The nearest X within r of an animal" returns, among the
  candidates that pass the query's filter, the one at the smallest world
  distance, or nothing if none is within r. The animal itself is never a
  candidate.
- **SPACE-08 (MUST)** Ties are broken deterministically (by a fixed key such as
  the animal id, or by a named random stream), never by the iteration order of
  an unordered collection.
- **SPACE-09 (SHOULD)** Queries within one phase see one consistent state: either
  the state at the start of the phase, or the state after each animal's move if
  the act order is sequential ([07 §6](07-actions-and-locomotion.md#6-act-order)).

## 3. Time

- **SPACE-10 (MUST)** Simulation time is the tick count, an integer starting at 0.
  Every duration (age, digestion, rot, incubation, decision period, stats
  interval) is in ticks.
- **SPACE-11 (MUST)** A run's result does not depend on frame rate,
  `Time.deltaTime`, `Time.fixedDeltaTime`, or how many ticks run per frame.
  Speeds are in meters per tick.
- **SPACE-12 (MUST)** Visuals (meshes, animation, interpolation between two tick
  states) read the simulation and never write to it.

## 4. Waiting for the brain: lockstep

A batch of LLM decisions takes seconds (JEV-9B: about 2.8 s per tick for about
330 animals; Ollama gemma4:12b: about 4 decisions per second). The owner's choice:
the simulation waits, so that a decision is never made on old observations.

- **SPACE-13 (MUST)** A tick that needs answers from a service (decisions from a
  brain, mutated genes from the mutator) does not continue past the phase that
  needs them until they are all in. A decision always uses the observation made
  in its own tick.
- **SPACE-14 (SHOULD)** Two wait modes, with **identical results** (the same
  events hash):

  | Mode | What happens while waiting | Where |
  |---|---|---|
  | **Freeze** | The main thread blocks until the answers arrive. Unity freezes. | batch mode, tests, simplest code |
  | **Responsive** | The tick stops at the phase that needs the answers. Unity keeps rendering and the editor stays usable; the tick resumes at that phase when the answers are in. | the editor (default) |

- **SPACE-15 (MAY)** A real-time mode in which answers are applied when they
  arrive (on observations that are by then old) is outside the contract: it is
  not reproducible. If built, it is labelled as such and never used for
  measurements. Not planned for now (owner decision); the reference is lockstep.

### Run controls

| Control | Effect |
|---|---|
| Pause / resume | stops between ticks (or inside one, while waiting) |
| Step | one tick, then pause |
| Run N | N ticks, then pause |
| Fast | as many ticks per frame as fit in a time budget (e.g. 10 ms) |
| Real time | k ticks per second, for watching |
| Stop condition | a tick count, a wall-clock limit, a stop file or button, extinction of a species |

**Reference.** One tick ≈ one step of one meter at walking speed. With the
keyword brain the prototype takes about 17 ms per tick for about 330 animals in the
192 × 192 world (Python); a C# implementation should be well below that.
