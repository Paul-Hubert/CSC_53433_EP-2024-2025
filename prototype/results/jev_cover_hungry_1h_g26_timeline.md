# How the genes developed — jev_cover_hungry_1h_g26

Run: 1369 ticks, 8 generations, 1934 births, 1802 deaths (predators 40%), 296 mutations, brain `jev`, seed 1234.
Checkpoints every 100 ticks; gene dropping with 500 random inheritances; behaviour per 400 ticks. Data: `jev_cover_hungry_1h_g26_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 5 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 134 | 0.0 | 134 | 0% | 0.00 | 5.4 | 100% | 5.0 | 4.8 | 0.04 |
| 100 | 87 | 0.0 | 87 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.7 | 0.04 |
| 300 | 140 | 0.8 | 58 | 2% | 0.02 | 5.5 | 100% | 8.0 | 4.5 | 0.04 |
| 400 | 237 | 1.6 | 57 | 3% | 0.03 | 5.5 | 100% | 11.8 | 4.3 | 0.05 |
| 500 | 233 | 2.0 | 52 | 5% | 0.05 | 5.5 | 100% | 11.8 | 4.2 | 0.05 |
| 600 | 257 | 2.6 | 52 | 5% | 0.06 | 5.4 | 100% | 13.8 | 4.4 | 0.05 |
| 800 | 252 | 3.6 | 52 | 8% | 0.08 | 5.4 | 100% | 14.4 | 4.4 | 0.06 |
| 900 | 270 | 4.1 | 52 | 9% | 0.09 | 5.5 | 100% | 14.6 | 4.7 | 0.06 |
| 1000 | 269 | 4.8 | 50 | 11% | 0.11 | 5.5 | 100% | 15.6 | 5.0 | 0.07 |
| 1100 | 266 | 5.3 | 49 | 11% | 0.12 | 5.4 | 100% | 16.8 | 4.9 | 0.07 |
| 1300 | 263 | 6.2 | 49 | 10% | 0.11 | 5.6 | 100% | 17.2 | 4.4 | 0.07 |
| 1369 | 268 | 6.7 | 48 | 11% | 0.11 | 5.5 | 99% | 17.8 | 4.3 | 0.08 |

Slot by slot at the end (tick 1369):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| eat | 17 | 5.6 | Eat whenever food is close. | 29% | 11% | 0.12 | 99% |
| flee | 18 | 3.0 | Run from any predator you see. | 53% | 9% | 0.09 | 100% |
| follow | 20 | 4.0 | Keep your distance from other animals. | 41% | 12% | 0.12 | 100% |
| rest | 13 | 5.4 | Save energy by resting when food is far. | 28% | 9% | 0.10 | 98% |
| mate | 21 | 3.7 | Seek a partner before growing old. | 36% | 11% | 0.12 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| eat | Eat whenever food is close. | founder text | 0 | 100 | — | — | 29% (t=1369) | present, 29% |
| eat | Only look for food when energy is low. | founder text | 0 | 400 | — | — | 26% (t=400) | present, 13% |
| eat | Always finish eating before doing anything else. | founder text | 0 | 300 | — | — | 31% (t=500) | present, 18% |
| flee | Run from any predator you see. | founder text | 0 | 200 | 1300 | — | 54% (t=1300) | present, 53% |
| flee | Run away from anything that attacks you. | founder text | 0 | 100 | — | — | 26% (t=100) | present, 10% |
| follow | Follow others when you are lost or hungry. | founder text | 0 | 1200 | — | — | 25% (t=1300) | present, 25% |
| follow | Keep your distance from other animals. | founder text | 0 | 0 | 400 | — | 55% (t=600) | present, 41% |
| rest | No preference. | founder text | 0 | 0 | — | — | 37% (t=700) | present, 21% |
| rest | Save energy by resting when food is far. | founder text | 0 | 1300 | — | — | 30% (t=1300) | present, 28% |
| mate | No preference. | founder text | 0 | 200 | — | — | 35% (t=1300) | present, 34% |
| mate | Seek a partner before growing old. | founder text | 0 | 100 | — | — | 39% (t=400) | present, 36% |

Leader of each slot over time (a row when it changes):

- **eat** (8 changes): t=0 "Eat whenever food is close." (25%) → t=400 "Only look for food when energy is low." (26%) → t=500 "Always finish eating before doing anything else." (31%) → … 1 more changes … → t=700 "Eat whenever food is close." (23%) → t=900 "Always finish eating before doing anything else." (24%) → t=1000 "Eat whenever food is close." (23%) → t=1200 "Always finish eating before doing anything else." (21%) → t=1300 "Eat whenever food is close." (23%)
- **flee** (1 changes): t=0 "Run away from anything that attacks you." (23%) → t=200 "Run from any predator you see." (28%)
- **follow** (0 changes): t=0 "Keep your distance from other animals." (28%)
- **rest** (1 changes): t=0 "No preference." (27%) → t=1300 "Save energy by resting when food is far." (30%)
- **mate** (0 changes): t=0 "Seek a partner before growing old." (25%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 175 | 1.1 | 0% | 61% | 2.4 | 6% | 6.0 | 95% |
| 5+ carriers at once | 28 | 1.0 | 0% | 43% | 1.8 | 7% | 5.1 | 96% |
| alive when mutants were most common (t=1200) | 65 | 1.1 | 0% | 58% | 2.4 | 8% | 5.7 | 95% |
| alive at the end | 64 | 1.1 | 0% | 52% | 2.4 | 5% | 5.9 | 97% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 92% |

153 mutations per 1 000 births; 0 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 0 | 0 (0–0) | 1.000 |
| mutants that reached 90 % | 0 | 0 (0–0) | 1.000 |
| founder texts that reached 90 % | 0 | 0 (0–0) | 1.000 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| eat | Eat whenever food is close. | 29% / 25% / 0.334 | 29% / 25% / 0.334 | 1.05 [0.86–1.24] (383) |
| eat | Only look for food when energy is low. | 26% / 22% / 0.102 | 13% / 13% / 0.416 | 1.02 [0.82–1.21] (310) |
| eat | Always finish eating before doing anything else. | 31% / 23% / 0.022 | 18% / 14% / 0.318 | 1.00 [0.85–1.14] (401) |
| flee | Run from any predator you see. | 54% / 34% / 0.034 | 53% / 34% / 0.042 | 1.12 [0.97–1.28] (593) |
| flee | Run away from anything that attacks you. | 26% / 26% / 1.000 | 10% / 21% / 0.912 | 0.91 [0.71–1.11] (289) |
| follow | Follow others when you are lost or hungry. | 25% / 9% / 0.006 | 25% / 8% / 0.014 | 0.99 [0.81–1.17] (329) |
| follow | Keep your distance from other animals. | 55% / 53% / 0.330 | 41% / 48% / 0.768 | 1.07 [0.95–1.20] (830) |
| rest | No preference. | 37% / 37% / 0.580 | 21% / 44% / 0.982 | 0.98 [0.83–1.14] (502) |
| rest | Save energy by resting when food is far. | 30% / 11% / 0.026 | 28% / 11% / 0.028 | 1.03 [0.83–1.23] (294) |
| mate | No preference. | 35% / 30% / 0.310 | 34% / 30% / 0.314 | 1.15 [0.98–1.31] (491) |
| mate | Seek a partner before growing old. | 39% / 36% / 0.208 | 36% / 35% / 0.472 | 1.05 [0.90–1.20] (627) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 134 |
| 100 | 87 |
| 300 | 58 |
| 400 | 57 |
| 500 | 52 |
| 600 | 52 |
| 800 | 52 |
| 900 | 52 |
| 1000 | 50 |
| 1100 | 49 |
| 1300 | 49 |
| 1369 | 48 |

No single founder or newcomer is an ancestor of every living animal.

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 40 (arrived t=0): 11%
- founder 51 (arrived t=0): 10%
- founder 3 (arrived t=0): 10%
- founder 71 (arrived t=0): 7%
- founder 12 (arrived t=0): 7%
- founder 76 (arrived t=0): 6%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | eat | flee | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–400 | 156.2 | 5.46 | 2.45 | 1.39 | 0 | 134 | 80 | 64% | 3% | 10% | 9% | 14% |
| 400–800 | 252.0 | 6.01 | 2.31 | 2.73 | 0 | 159 | 67 | 56% | 6% | 11% | 9% | 18% |
| 800–1200 | 268.0 | 6.38 | 2.23 | 2.51 | 0 | 158 | 68 | 58% | 4% | 11% | 10% | 18% |
| 1200–1300 | 260.0 | 5.81 | 2.42 | 1.46 | 0 | 141 | 74 | 59% | 5% | 10% | 9% | 17% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 5.4 | 0.04 | — |
| 100 | 100% | 5.5 | 0.04 | — |
| 300 | 100% | 5.5 | 0.04 | — |
| 400 | 100% | 5.5 | 0.05 | — |
| 500 | 100% | 5.5 | 0.05 | — |
| 600 | 100% | 5.4 | 0.05 | — |
| 800 | 100% | 5.4 | 0.06 | — |
| 900 | 100% | 5.5 | 0.06 | — |
| 1000 | 100% | 5.5 | 0.07 | — |
| 1100 | 100% | 5.4 | 0.07 | — |
| 1300 | 100% | 5.6 | 0.07 | — |
| 1369 | 99% | 5.5 | 0.08 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
