# How the genes developed — jev_cover_hungry_1h_g26

Run: 1369 ticks, 7 generations, 384 births, 344 deaths (predators 0%), 61 mutations, brain `jev`, seed 1234.
Checkpoints every 100 ticks; gene dropping with 500 random inheritances; behaviour per 400 ticks. Data: `jev_cover_hungry_1h_g26_predator_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 4 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 28 | 0.0 | 28 | 0% | 0.00 | 4.7 | 100% | 5.0 | 4.2 | 0.03 |
| 100 | 28 | 0.0 | 28 | 0% | 0.00 | 4.7 | 100% | 5.0 | 4.2 | 0.03 |
| 300 | 38 | 0.6 | 24 | 1% | 0.01 | 4.3 | 99% | 5.5 | 4.1 | 0.03 |
| 400 | 59 | 1.2 | 24 | 3% | 0.03 | 4.4 | 100% | 6.8 | 4.2 | 0.04 |
| 500 | 65 | 1.5 | 23 | 3% | 0.03 | 4.4 | 100% | 7.2 | 4.2 | 0.05 |
| 600 | 65 | 2.0 | 22 | 4% | 0.04 | 4.4 | 100% | 6.5 | 3.7 | 0.04 |
| 800 | 67 | 2.9 | 19 | 5% | 0.05 | 4.4 | 100% | 7.0 | 3.3 | 0.05 |
| 900 | 68 | 3.3 | 16 | 9% | 0.10 | 4.5 | 99% | 7.8 | 3.5 | 0.07 |
| 1000 | 68 | 3.8 | 16 | 8% | 0.09 | 4.5 | 99% | 7.2 | 3.3 | 0.06 |
| 1100 | 68 | 4.2 | 16 | 6% | 0.07 | 4.5 | 99% | 7.0 | 3.2 | 0.07 |
| 1300 | 67 | 5.2 | 16 | 8% | 0.09 | 4.5 | 100% | 8.0 | 3.4 | 0.08 |
| 1369 | 68 | 5.3 | 16 | 9% | 0.09 | 4.5 | 100% | 7.5 | 3.3 | 0.07 |

Slot by slot at the end (tick 1369):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| predator.hunt | 9 | 5.2 | Chase any prey you see. | 32% | 15% | 0.15 | 100% |
| predator.follow | 10 | 2.5 | Keep away from other predators. | 60% | 9% | 0.10 | 100% |
| predator.rest | 6 | 2.4 | No preference. | 57% | 6% | 0.06 | 100% |
| predator.mate | 5 | 3.0 | Hunt first, mate later. | 51% | 6% | 0.06 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| predator.hunt | Chase any prey you see. | founder text | 0 | 700 | — | — | 32% (t=900) | present, 32% |
| predator.hunt | Hunt only when you are hungry. | founder text | 0 | 500 | — | — | 31% (t=600) | present, 15% |
| predator.hunt | Attack only when prey is close. | founder text | 0 | 0 | — | — | 31% (t=200) | present, 10% |
| predator.hunt | Keep chasing until the prey is caught. | founder text | 0 | 1100 | — | — | 30% (t=1200) | present, 21% |
| predator.follow | No preference. | founder text | 0 | 200 | — | — | 27% (t=200) | present, 15% |
| predator.follow | Hunt as a pack. | founder text | 0 | 0 | — | — | 29% (t=200) | present, 9% |
| predator.follow | Keep away from other predators. | founder text | 0 | 0 | 700 | — | 60% (t=1100) | present, 60% |
| predator.rest | No preference. | founder text | 0 | 0 | 600 | — | 64% (t=1200) | present, 57% |
| predator.rest | Rest when your belly is full. | founder text | 0 | 0 | — | — | 31% (t=1300) | present, 26% |
| predator.mate | No preference. | founder text | 0 | 200 | — | — | 33% (t=200) | present, 9% |
| predator.mate | Mate with any nearby adult. | founder text | 0 | 1100 | — | — | 27% (t=1200) | present, 19% |
| predator.mate | Hunt first, mate later. | founder text | 0 | 0 | 900 | — | 53% (t=1100) | present, 51% |
| predator.mate | Seek a partner before growing old. | founder text | 0 | 0 | — | — | 32% (t=0) | present, 15% |

Leader of each slot over time (a row when it changes):

- **predator.hunt** (6 changes): t=0 "Attack only when prey is close." (29%) → t=300 "Hunt only when you are hungry." (24%) → t=700 "Chase any prey you see." (29%) → t=800 "Hunt only when you are hungry." (30%) → t=900 "Chase any prey you see." (32%) → t=1200 "Keep chasing until the prey is caught." (30%) → t=1300 "Chase any prey you see." (30%)
- **predator.follow** (1 changes): t=0 "Hunt as a pack." (25%) → t=300 "Keep away from other predators." (29%)
- **predator.rest** (0 changes): t=0 "No preference." (36%)
- **predator.mate** (2 changes): t=0 "Hunt first, mate later." (32%) → t=200 "No preference." (33%) → t=300 "Hunt first, mate later." (39%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 48 | 1.0 | 0% | 50% | 1.7 | 6% | 4.3 | 94% |
| 5+ carriers at once | 4 | 1.0 | 0% | 50% | 1.2 | 0% | 5.0 | 100% |
| alive when mutants were most common (t=900) | 14 | 1.1 | 0% | 57% | 1.4 | 0% | 4.6 | 93% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 88% |

159 mutations per 1 000 births; 0 answers rejected by the guards.

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
| predator.hunt | Chase any prey you see. | 32% / 26% / 0.180 | 32% / 24% / 0.186 | 1.01 [0.63–1.40] (74) |
| predator.hunt | Hunt only when you are hungry. | 31% / 17% / 0.028 | 15% / 10% / 0.218 | 0.96 [0.64–1.27] (84) |
| predator.hunt | Attack only when prey is close. | 31% / 28% / 0.294 | 10% / 15% / 0.766 | 1.00 [0.64–1.37] (44) |
| predator.hunt | Keep chasing until the prey is caught. | 30% / 28% / 0.442 | 21% / 26% / 0.858 | 1.07 [0.74–1.40] (77) |
| predator.follow | No preference. | 27% / 23% / 0.268 | 15% / 19% / 0.646 | 1.04 [0.67–1.41] (60) |
| predator.follow | Hunt as a pack. | 29% / 29% / 0.648 | 9% / 19% / 0.802 | 1.03 [0.63–1.43] (58) |
| predator.follow | Keep away from other predators. | 60% / 37% / 0.008 | 60% / 38% / 0.014 | 1.04 [0.78–1.29] (147) |
| predator.rest | No preference. | 64% / 60% / 0.164 | 57% / 53% / 0.140 | 1.08 [0.85–1.31] (174) |
| predator.rest | Rest when your belly is full. | 31% / 28% / 0.362 | 26% / 27% / 0.568 | 0.99 [0.69–1.30] (87) |
| predator.mate | No preference. | 33% / 28% / 0.070 | 9% / 21% / 0.890 | 1.05 [0.74–1.36] (67) |
| predator.mate | Mate with any nearby adult. | 27% / 15% / 0.094 | 19% / 11% / 0.180 | 0.98 [0.56–1.41] (56) |
| predator.mate | Hunt first, mate later. | 53% / 42% / 0.166 | 51% / 45% / 0.248 | 1.03 [0.76–1.30] (129) |
| predator.mate | Seek a partner before growing old. | 32% / 32% / 1.000 | 15% / 14% / 0.504 | 1.11 [0.74–1.48] (67) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 28 |
| 100 | 28 |
| 300 | 24 |
| 400 | 24 |
| 500 | 23 |
| 600 | 22 |
| 800 | 19 |
| 900 | 16 |
| 1000 | 16 |
| 1100 | 16 |
| 1300 | 16 |
| 1369 | 16 |

No single founder or newcomer is an ancestor of every living animal.

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 144 (arrived t=0): 16%
- founder 149 (arrived t=0): 15%
- founder 141 (arrived t=0): 13%
- founder 140 (arrived t=0): 12%
- founder 154 (arrived t=0): 12%
- founder 159 (arrived t=0): 11%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | hunt | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–400 | 42.5 | 3.94 | 9.00 | 2.12 | 0 | 141 | 71 | 44% | 33% | 7% | 17% |
| 400–800 | 66.2 | 4.72 | 8.79 | 1.81 | 0 | 211 | 73 | 44% | 25% | 7% | 24% |
| 800–1200 | 67.8 | 4.87 | 8.82 | 1.18 | 0 | 202 | 73 | 46% | 18% | 6% | 30% |
| 1200–1300 | 67.0 | 5.52 | 9.40 | 0.60 | 0 | 182 | 77 | 47% | 12% | 4% | 37% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 4.7 | 0.03 | — |
| 100 | 100% | 4.7 | 0.03 | — |
| 300 | 99% | 4.3 | 0.03 | — |
| 400 | 100% | 4.4 | 0.04 | — |
| 500 | 100% | 4.4 | 0.05 | — |
| 600 | 100% | 4.4 | 0.04 | — |
| 800 | 100% | 4.4 | 0.05 | — |
| 900 | 99% | 4.5 | 0.07 | — |
| 1000 | 99% | 4.5 | 0.06 | — |
| 1100 | 99% | 4.5 | 0.07 | — |
| 1300 | 100% | 4.5 | 0.08 | — |
| 1369 | 100% | 4.5 | 0.07 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
