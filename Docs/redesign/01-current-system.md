# 01 — Current system audit

Unity **2021.3.8f1** project. Relevant code in
`Assets/02 - Scripts/04 - Crowds and Evolution/` (≈420 lines total).

## What it does

| File | Role |
|------|------|
| `GeneticAlgo.cs` | Spawns `popSize` animals at random positions; keeps population ≥ `popSize/2` by spawning **fresh random** animals; grows grass at random detail-map cells at `vegetationGrowthRate`. |
| `Animal.cs` | Per-frame: lose energy; if standing on grass, eat it, gain energy **and immediately spawn one mutated offspring**; die at 0 energy. Perception = `nEyes` ray-marches over the grass detail map, returning normalised distance to nearest grass. Action = **one output: a turn angle**. Always moves forward (implicitly). |
| `NeuralNet.cs` | Fixed MLP `[nEyes, 5, 1]`, sigmoid. Mutation = per-weight random reset (`swapRate`) or additive noise (`mutateRate`). |

Terrain access goes through `CustomTerrain` (heightmap, steepness, normals,
detail layer = grass, tree instances). Brushes (`TerrainBrush`,
`InstanceBrush`) are the terrain/foliage labs.

## Why students struggle with it

- **Opaque genome.** Evolved weights are unreadable; students cannot tell
  *why* something works, and "results" are just a population count.
- **Asexual only.** One parent, copy + noise. No crossover, no mating, no
  population genetics to reason about.
- **Tiny behaviour space.** A single steering output. No eat/attack/flee
  choice, no other agents, no predators.
- **Selection is hard-wired into `Animal.Update()`.** Eating ⇒ reproduce.
  Changing the fitness or reproduction rule means editing the agent itself.
- **No separation of concerns.** Perception, decision, action, metabolism and
  reproduction live in one `MonoBehaviour`. Nothing is swappable.
- **Frame-rate coupled.** Energy loss and turning are per *frame*, not per
  simulated second → results depend on the student's machine.
- **No logging / no experiment tooling.** No seeds, no saved genomes, no
  per-generation statistics, no way to compare runs.
- **Terrain is ignored** by the agents (no water, no slopes, wrap-around
  vision).
- **Old Unity version** (2021.3). Current LTS is Unity 6.

## Worth keeping

- `CustomTerrain` as the single façade over terrain data — extend, don't
  replace.
- The **MLP brain** — keep it as one pluggable decision backend so students
  can compare *numeric* vs *prompt* genomes on the same world (great
  pedagogical contrast).
- Energy/metabolism model (simple, understandable).
