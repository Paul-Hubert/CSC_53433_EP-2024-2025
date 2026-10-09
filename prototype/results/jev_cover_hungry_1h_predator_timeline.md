# How the genes developed — jev_cover_hungry_1h

Run: 1293 ticks, 7 generations, 372 births, 333 deaths (predators 0%), 35 mutations, brain `jev`, seed 1234.
Checkpoints every 100 ticks; gene dropping with 500 random inheritances; behaviour per 400 ticks. Data: `jev_cover_hungry_1h_predator_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 4 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 28 | 0.0 | 28 | 0% | 0.00 | 4.7 | 100% | 5.0 | 4.2 | 0.03 |
| 100 | 28 | 0.0 | 28 | 0% | 0.00 | 4.7 | 100% | 5.0 | 4.2 | 0.03 |
| 200 | 44 | 0.5 | 28 | 0% | 0.00 | 4.4 | 100% | 5.0 | 3.9 | 0.02 |
| 400 | 60 | 1.3 | 23 | 3% | 0.03 | 4.3 | 100% | 6.8 | 3.9 | 0.03 |
| 500 | 67 | 1.6 | 23 | 3% | 0.03 | 4.4 | 100% | 7.2 | 3.8 | 0.03 |
| 600 | 68 | 2.2 | 16 | 7% | 0.07 | 4.5 | 99% | 7.0 | 3.7 | 0.04 |
| 700 | 68 | 2.8 | 13 | 3% | 0.03 | 4.5 | 100% | 5.5 | 3.0 | 0.04 |
| 800 | 68 | 3.3 | 13 | 5% | 0.06 | 4.6 | 99% | 6.0 | 3.3 | 0.04 |
| 900 | 66 | 3.9 | 13 | 8% | 0.08 | 4.6 | 98% | 6.5 | 3.2 | 0.04 |
| 1100 | 67 | 4.8 | 12 | 10% | 0.10 | 4.6 | 97% | 7.0 | 3.5 | 0.04 |
| 1200 | 66 | 5.0 | 12 | 9% | 0.09 | 4.6 | 95% | 7.2 | 3.4 | 0.04 |
| 1293 | 67 | 5.5 | 12 | 10% | 0.10 | 4.6 | 95% | 7.2 | 3.4 | 0.04 |

Slot by slot at the end (tick 1293):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| predator.hunt | 6 | 3.9 | Hunt only when you are hungry. | 31% | 1% | 0.01 | 100% |
| predator.follow | 8 | 3.2 | Keep away from other predators. | 48% | 13% | 0.13 | 94% |
| predator.rest | 9 | 3.0 | No preference. | 52% | 15% | 0.16 | 93% |
| predator.mate | 6 | 3.6 | Seek a partner before growing old. | 34% | 10% | 0.10 | 93% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| predator.hunt | No preference. | founder text | 0 | 1000 | — | — | 31% (t=1100) | present, 30% |
| predator.hunt | Chase any prey you see. | founder text | 0 | 600 | — | — | 26% (t=600) | present, 22% |
| predator.hunt | Hunt only when you are hungry. | founder text | 0 | 300 | — | — | 31% (t=1293) | present, 31% |
| predator.hunt | Attack only when prey is close. | founder text | 0 | 0 | — | — | 29% (t=0) | present, 1% |
| predator.hunt | Keep chasing until the prey is caught. | founder text | 0 | 400 | — | — | 31% (t=1000) | present, 13% |
| predator.follow | No preference. | founder text | 0 | 200 | — | — | 32% (t=200) | present, 4% |
| predator.follow | Hunt as a pack. | founder text | 0 | 0 | — | — | 34% (t=1000) | present, 27% |
| predator.follow | Keep away from other predators. | founder text | 0 | 0 | 700 | — | 53% (t=700) | present, 48% |
| predator.rest | No preference. | founder text | 0 | 0 | 300 | — | 72% (t=1000) | present, 52% |
| predator.rest | Rest when your belly is full. | founder text | 0 | 0 | — | — | 32% (t=200) | present, 22% |
| predator.mate | No preference. | founder text | 0 | 200 | — | — | 32% (t=200) | lost by t=700 |
| predator.mate | Mate with any nearby adult. | founder text | 0 | 700 | — | — | 35% (t=900) | present, 31% |
| predator.mate | Hunt first, mate later. | founder text | 0 | 0 | — | — | 42% (t=500) | present, 24% |
| predator.mate | Seek a partner before growing old. | founder text | 0 | 0 | — | — | 48% (t=1100) | present, 34% |

Leader of each slot over time (a row when it changes):

- **predator.hunt** (6 changes): t=0 "Attack only when prey is close." (29%) → t=300 "Hunt only when you are hungry." (26%) → t=400 "Keep chasing until the prey is caught." (25%) → t=500 "Hunt only when you are hungry." (30%) → t=900 "Keep chasing until the prey is caught." (29%) → t=1100 "No preference." (31%) → t=1293 "Hunt only when you are hungry." (31%)
- **predator.follow** (3 changes): t=0 "Hunt as a pack." (25%) → t=200 "No preference." (32%) → t=300 "Hunt as a pack." (28%) → t=400 "Keep away from other predators." (35%)
- **predator.rest** (0 changes): t=0 "No preference." (36%)
- **predator.mate** (3 changes): t=0 "Hunt first, mate later." (32%) → t=200 "No preference." (32%) → t=300 "Hunt first, mate later." (40%) → t=900 "Seek a partner before growing old." (41%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 35 | 1.1 | 0% | 71% | 4.6 | 43% | 5.6 | 69% |
| 5+ carriers at once | 4 | 1.0 | 0% | 75% | 3.0 | 50% | 5.0 | 50% |
| alive when mutants were most common (t=1100) | 12 | 1.0 | 0% | 83% | 3.6 | 42% | 5.2 | 58% |
| alive at the end | 13 | 1.1 | 0% | 69% | 3.2 | 38% | 4.8 | 69% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 88% |

94 mutations per 1 000 births; 1 answers rejected by the guards.

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
| predator.hunt | No preference. | 31% / 23% / 0.230 | 30% / 29% / 0.482 | 0.98 [0.67–1.28] (81) |
| predator.hunt | Chase any prey you see. | 26% / 26% / 0.558 | 22% / 21% / 0.756 | 1.14 [0.51–1.78] (44) |
| predator.hunt | Hunt only when you are hungry. | 31% / 28% / 0.430 | 31% / 28% / 0.430 | 1.07 [0.78–1.36] (84) |
| predator.hunt | Attack only when prey is close. | 29% / 29% / 1.000 | 1% / 9% / 0.614 | 0.72 [0.30–1.13] (25) |
| predator.hunt | Keep chasing until the prey is caught. | 31% / 31% / 1.000 | 13% / 13% / 0.530 | 1.05 [0.74–1.37] (83) |
| predator.follow | No preference. | 32% / 25% / 0.048 | 4% / 8% / 0.560 | 1.03 [0.61–1.44] (41) |
| predator.follow | Hunt as a pack. | 34% / 21% / 0.078 | 27% / 14% / 0.102 | 0.89 [0.63–1.15] (92) |
| predator.follow | Keep away from other predators. | 53% / 42% / 0.146 | 48% / 46% / 0.450 | 1.10 [0.78–1.42] (117) |
| predator.rest | No preference. | 72% / 67% / 0.234 | 52% / 49% / 0.382 | 1.05 [0.85–1.25] (198) |
| predator.rest | Rest when your belly is full. | 32% / 28% / 0.180 | 22% / 26% / 1.000 | 1.03 [0.33–1.72] (39) |
| predator.mate | No preference. | 32% / 26% / 0.082 | 0% / 9% / 1.000 | 0.94 [0.40–1.48] (28) |
| predator.mate | Mate with any nearby adult. | 35% / 23% / 0.126 | 31% / 19% / 0.096 | 1.10 [0.78–1.42] (85) |
| predator.mate | Hunt first, mate later. | 42% / 42% / 0.550 | 24% / 36% / 0.858 | 0.99 [0.64–1.35] (94) |
| predator.mate | Seek a partner before growing old. | 48% / 21% / 0.008 | 34% / 15% / 0.040 | 0.95 [0.72–1.18] (118) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 28 |
| 100 | 28 |
| 200 | 28 |
| 400 | 23 |
| 500 | 23 |
| 600 | 16 |
| 700 | 13 |
| 800 | 13 |
| 900 | 13 |
| 1100 | 12 |
| 1200 | 12 |
| 1293 | 12 |

No single founder or newcomer is an ancestor of every living animal.

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 154 (arrived t=0): 20%
- founder 149 (arrived t=0): 19%
- founder 140 (arrived t=0): 19%
- founder 159 (arrived t=0): 8%
- founder 162 (arrived t=0): 8%
- founder 150 (arrived t=0): 6%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | hunt | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–400 | 43.2 | 3.99 | 9.19 | 2.14 | 0 | 163 | 74 | 46% | 32% | 7% | 15% |
| 400–800 | 67.8 | 4.80 | 8.60 | 1.40 | 0 | 212 | 70 | 42% | 25% | 4% | 29% |
| 800–1200 | 66.8 | 5.02 | 8.88 | 1.46 | 0 | 183 | 74 | 45% | 20% | 4% | 32% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 4.7 | 0.03 | — |
| 100 | 100% | 4.7 | 0.03 | — |
| 200 | 100% | 4.4 | 0.02 | — |
| 400 | 100% | 4.3 | 0.03 | — |
| 500 | 100% | 4.4 | 0.03 | — |
| 600 | 99% | 4.5 | 0.04 | — |
| 700 | 100% | 4.5 | 0.04 | — |
| 800 | 99% | 4.6 | 0.04 | — |
| 900 | 98% | 4.6 | 0.04 | — |
| 1100 | 97% | 4.6 | 0.04 | — |
| 1200 | 95% | 4.6 | 0.04 | — |
| 1293 | 95% | 4.6 | 0.04 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
