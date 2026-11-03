# 16 — Neural species and the brain comparison

A neural species is an ordinary EvoSim species whose brain is a small neural network carried in its genome. It lives
beside the JEV species, in the same worlds, under the same rules. The question it answers: does an evolved network
survive better or worse than an evolved sentence genome read by an LLM? The design and its reasons are in
[`Docs/Neuro/PLAN.md`](../Neuro/PLAN.md); this chapter is the reference for what was built. Code:
`Assets/EvoSim/Neuro` (assembly `EvoSim.Neuro`, editor tools in `EvoSim.Neuro.Editor`).

## 1. What a neural animal is

| Part | Class | What it does |
|---|---|---|
| Genome | `NeuralGenomeGene` | one locus holding every weight as a vector allele (`vec[n]#hash`, values in `vectors.jsonl`) |
| Network | `NeuralLayout`, `NeuralNet` | inputs from the senses in order, hidden layers (default one of 12, tanh), one linear output per action |
| Senses | `RayFanSense`, `InteroceptionSense` | 12 rays over 360° (one channel per kind of thing, 1 − distance/range), and energy, stamina, age, readiness, food, cover, the last action, ticks since a meal |
| Actions | `TurnAction`, `ForwardAction`, `StopAction` | `left`, `right` (20°), `walk`, `run`, `stop`; each eats, strikes or scavenges what is in reach, and breeds when ready |
| Brain | `NeuralBrain` | one forward pass per decision; Greedy (the largest output) by default, or Sample (softmax); from `SyncBelow` (32) queries on, on a worker task beside the other brains, with the same rows |
| Evolution | `NeuralMutation`, `NeuronCrossover` | adds or replaces weights at random; a baby takes each neuron whole from one parent |
| Stats | `HungerClock`, `NeuroStats` | the meal clock the interoception reads; speed, meals per tick and per meter, action shares, loops in `stats.csv` |

Founders are random networks from a seeded pool, the same in every run (GENE-21). Rays stop at the first thing they
meet: animals, food, carcasses, cover and the world's edge all occlude.

## 2. Matched to JEV

`NeuralTemplates` (menu `GameObject ▸ EvoSim ▸ New Neural Species ▸ Prey | Predator`) builds `prey-nn` ("neural prey")
or `predator-nn` ("neural predator") as a copy of a JEV species: every body and life setting is kept, only the
controller changes. `MatchedPairs` names the pairs and validation reports:

| Code | Severity | Check |
|---|---|---|
| V-70 | error | a species with `NeuralBrain` has no `NeuralGenomeGene` |
| V-71 | error | founder vectors don't match the layout's length |
| V-72 | error | a neural sense's width changed since the layout was built |
| V-73 | warning | sexual mating without `NeuronCrossover` |
| V-74 | info | a neural species decides less often than every tick |
| V-75 | warning | a contact the diet can't use |
| V-76 | error | ray fan settings out of range |
| V-77 | info | the Sample policy in a World whose sampling τ ≠ 1 |
| V-78 | warning | a matched pair differs in a body or life setting (each one is named) |
| V-79 | warning | a diet or a cover treats the two of a pair differently |

## 3. Scenes and scenarios

| Scenario | Scene | Prey | Predators | Tier |
|---|---|---|---|---|
| S29 NeuroLab | `Neuro_Lab` (Lab1_Small's map) | neural | neural | T2 |
| S30 JevPreyNeuroPredator | `Neuro_JevPrey` | JEV | neural | T4, L0 in T2 |
| S31 NeuroPreyJevPredator | `Neuro_JevPredator` | neural | JEV | T4, L0 in T2 |
| S32 Brains2x2 | `Brains_2x2` (Lab1_Full's map) | JEV and neural | JEV and neural | T4 measured, L0 in T2 |
| S33 PreyVsPrey | `Prey_vs_Prey` (Lab1_Full's map) | JEV and neural | none | T4 measured, L0 in T2 |

S32 and S33 run asexual (with the JEV mating line "An adult that chooses mate breeds alone as soon as it is ready."),
with grass regrowth ×2, seeds 1 and 2, and two arms: `default` (neural species decide every tick) and `matched` (every
4 ticks, staggered, as JEV). L0 variants replace JEV by the random brain and switch the JEV species' LLM mutation off.
The JEV species keep their senses and prompts, so their answer caches stay valid; in `Prey_vs_Prey` the placeholders
that named the removed predator are written as their values, so the JEV prey's prompt is exactly Lab1_Full's. The
builders are `EvoSim ▸ Neuro ▸ Build Neuro Scenes` and `Build Comparison Scenes`.

## 4. Survival

- **`SurvivalStats`** (world service) adds `sv_<species>_alive`, `_births`, `_immigrants`, `_deaths_<cause>` and
  `sv_<killer>_kills_<victim>` to `stats.csv`, and writes `survival.json` at the run's end: each species' maturity, cap,
  floor, initial population, counts, and the matched pairs and plateau tick.
- **`PlateauStop`** (a phase after Record) closes a window every 2 000 ticks and stops the run cleanly (RAND-20) at the
  first boundary where every neural species has a mean generation of at least 50 and its mean population, starvation
  hazard, predation hazard and births per animal stay within ±10 % over the last three windows. Cap: 60 000 ticks
  (reason `plateau cap`). With **Stops Run** off it only records the tick.
- **`SurvivalAnalysis`** rebuilds each life from `events.jsonl` (founder, birth or newcomer to death; migration and the
  run's end are censoring) and computes Kaplan–Meier curves with Greenwood bands, death-rate ratios with a 95 % interval
  from the log rate ratio (SE = √(1/d₁ + 1/d₂)), per-window measures and the kill matrix.
- **`SurvivalReportWindow`** (`Window ▸ EvoSim ▸ Survival Report`) opens the runs of one scenario and arm and writes
  `Logs/EvoSim/Reports/<scenario>-<variant>-<date>.md` and `.csv`. Headline: the death hazard ratio neural/JEV per role
  over the late windows, per seed and pooled; an effect counts only when both seeds agree and the pooled interval
  excludes 1.

## 5. Tests

T-NEURO-01…16 (plan §8) in `Assets/EvoSim/Neuro/Tests` (assembly `EvoSim.Tests.Neuro`), plus the conformance suites,
which pick up every neural sense, action, brain, gene and operator. T-NEURO-16 calls JEV: it runs only with
`EVOSIM_T4=1` in the environment.
