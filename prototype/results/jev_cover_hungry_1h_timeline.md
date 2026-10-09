# How the genes developed — jev_cover_hungry_1h

Run: 1293 ticks, 8 generations, 1872 births, 1743 deaths (predators 39%), 290 mutations, brain `jev`, seed 1234.
Checkpoints every 100 ticks; gene dropping with 500 random inheritances; behaviour per 400 ticks. Data: `jev_cover_hungry_1h_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 5 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 134 | 0.0 | 134 | 0% | 0.00 | 5.4 | 100% | 5.0 | 4.8 | 0.04 |
| 100 | 87 | 0.0 | 87 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.7 | 0.04 |
| 200 | 160 | 0.7 | 63 | 2% | 0.02 | 5.6 | 100% | 8.0 | 4.8 | 0.04 |
| 400 | 259 | 1.5 | 58 | 4% | 0.04 | 5.7 | 100% | 11.0 | 4.7 | 0.05 |
| 500 | 269 | 2.0 | 58 | 5% | 0.05 | 5.8 | 100% | 13.2 | 4.8 | 0.05 |
| 600 | 255 | 2.6 | 57 | 6% | 0.07 | 5.9 | 100% | 14.2 | 4.8 | 0.05 |
| 700 | 263 | 3.0 | 53 | 8% | 0.08 | 6.1 | 100% | 15.2 | 4.8 | 0.06 |
| 800 | 264 | 3.6 | 52 | 8% | 0.08 | 6.1 | 99% | 16.0 | 4.6 | 0.05 |
| 900 | 254 | 4.2 | 52 | 10% | 0.11 | 6.1 | 98% | 17.8 | 4.6 | 0.06 |
| 1100 | 269 | 5.3 | 50 | 11% | 0.12 | 6.1 | 99% | 20.2 | 4.4 | 0.06 |
| 1200 | 268 | 5.9 | 50 | 13% | 0.13 | 6.1 | 99% | 18.8 | 4.7 | 0.06 |
| 1293 | 265 | 6.5 | 50 | 14% | 0.16 | 6.1 | 98% | 19.6 | 4.8 | 0.06 |

Slot by slot at the end (tick 1293):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| eat | 13 | 2.9 | Eat whenever food is close. | 48% | 8% | 0.08 | 99% |
| flee | 18 | 5.1 | Flee only when a predator is very close. | 30% | 8% | 0.08 | 100% |
| follow | 25 | 5.5 | Keep your distance from other animals. | 34% | 30% | 0.35 | 95% |
| rest | 20 | 4.8 | Rest only when you feel safe. | 31% | 15% | 0.15 | 99% |
| mate | 22 | 5.9 | Look for a partner when energy is high. | 25% | 12% | 0.12 | 99% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| eat | Eat whenever food is close. | founder text | 0 | 100 | — | — | 48% (t=1293) | present, 48% |
| eat | Only look for food when energy is low. | founder text | 0 | 400 | — | — | 35% (t=1200) | present, 31% |
| flee | Run from any predator you see. | founder text | 0 | 200 | — | — | 26% (t=200) | present, 11% |
| flee | Flee only when a predator is very close. | founder text | 0 | 800 | — | — | 30% (t=1293) | present, 30% |
| flee | Stay calm unless danger is right next to you. | founder text | 0 | 300 | — | — | 27% (t=300) | present, 22% |
| flee | Run away from anything that attacks you. | founder text | 0 | 100 | — | — | 26% (t=100) | present, 17% |
| follow | Follow others when you are lost or hungry. | founder text | 0 | 800 | — | — | 28% (t=800) | present, 22% |
| follow | Keep your distance from other animals. | founder text | 0 | 0 | — | — | 47% (t=900) | present, 34% |
| rest | No preference. | founder text | 0 | 0 | — | — | 29% (t=1293) | present, 29% |
| rest | Rest only when you feel safe. | founder text | 0 | 700 | — | — | 36% (t=1100) | present, 31% |
| mate | Look for a partner when energy is high. | founder text | 0 | 1000 | — | — | 26% (t=1200) | present, 25% |
| mate | Mate only when food is plentiful. | founder text | 0 | 800 | — | — | 26% (t=800) | present, 9% |
| mate | Seek a partner before growing old. | founder text | 0 | 100 | — | — | 27% (t=300) | present, 15% |

Leader of each slot over time (a row when it changes):

- **eat** (2 changes): t=0 "Eat whenever food is close." (25%) → t=600 "Only look for food when energy is low." (32%) → t=900 "Eat whenever food is close." (41%)
- **flee** (5 changes): t=0 "Run away from anything that attacks you." (23%) → t=200 "Run from any predator you see." (26%) → t=300 "Stay calm unless danger is right next to you." (27%) → t=400 "Run from any predator you see." (24%) → t=500 "Stay calm unless danger is right next to you." (22%) → t=800 "Flee only when a predator is very close." (27%)
- **follow** (0 changes): t=0 "Keep your distance from other animals." (28%)
- **rest** (4 changes): t=0 "No preference." (27%) → t=300 "Rest only when you feel safe." (22%) → t=400 "Never stop moving." (22%) → t=500 "No preference." (21%) → t=600 "Rest only when you feel safe." (23%)
- **mate** (5 changes): t=0 "Seek a partner before growing old." (25%) → t=400 "No preference." (22%) → t=500 "Mate only when food is plentiful." (23%) → t=700 "Look for a partner when energy is high." (22%) → t=800 "Mate only when food is plentiful." (26%) → t=1000 "Look for a partner when energy is high." (26%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 286 | 1.1 | 0% | 54% | 5.2 | 39% | 6.9 | 88% |
| 5+ carriers at once | 30 | 1.0 | 0% | 57% | 4.3 | 33% | 6.7 | 87% |
| alive when mutants were most common (t=1293) | 75 | 1.1 | 0% | 52% | 5.2 | 37% | 7.3 | 87% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 92% |

155 mutations per 1 000 births; 1 answers rejected by the guards.

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
| eat | Eat whenever food is close. | 48% / 28% / 0.018 | 48% / 28% / 0.018 | 1.01 [0.88–1.14] (609) |
| eat | Only look for food when energy is low. | 35% / 35% / 0.498 | 31% / 35% / 0.658 | 1.09 [0.93–1.25] (517) |
| flee | Run from any predator you see. | 26% / 27% / 0.680 | 11% / 19% / 0.804 | 1.02 [0.81–1.23] (282) |
| flee | Flee only when a predator is very close. | 30% / 21% / 0.130 | 30% / 21% / 0.130 | 1.03 [0.86–1.21] (365) |
| flee | Stay calm unless danger is right next to you. | 27% / 25% / 0.150 | 22% / 15% / 0.160 | 1.05 [0.88–1.22] (412) |
| flee | Run away from anything that attacks you. | 26% / 26% / 1.000 | 17% / 26% / 0.860 | 1.04 [0.84–1.25] (294) |
| follow | Follow others when you are lost or hungry. | 28% / 21% / 0.104 | 22% / 20% / 0.424 | 1.06 [0.89–1.22] (411) |
| follow | Keep your distance from other animals. | 47% / 39% / 0.100 | 34% / 34% / 0.498 | 1.04 [0.91–1.17] (727) |
| rest | No preference. | 29% / 14% / 0.026 | 29% / 14% / 0.026 | 0.94 [0.76–1.11] (386) |
| rest | Rest only when you feel safe. | 36% / 30% / 0.258 | 31% / 33% / 0.560 | 1.10 [0.92–1.27] (456) |
| mate | Look for a partner when energy is high. | 26% / 19% / 0.184 | 25% / 20% / 0.296 | 1.06 [0.88–1.23] (365) |
| mate | Mate only when food is plentiful. | 26% / 18% / 0.064 | 9% / 10% / 0.534 | 0.98 [0.80–1.16] (336) |
| mate | Seek a partner before growing old. | 27% / 32% / 0.974 | 15% / 19% / 0.728 | 0.97 [0.78–1.16] (299) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 134 |
| 100 | 87 |
| 200 | 63 |
| 400 | 58 |
| 500 | 58 |
| 600 | 57 |
| 700 | 53 |
| 800 | 52 |
| 900 | 52 |
| 1100 | 50 |
| 1200 | 50 |
| 1293 | 50 |

No single founder or newcomer is an ancestor of every living animal.

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 28 (arrived t=0): 8%
- founder 13 (arrived t=0): 8%
- founder 75 (arrived t=0): 8%
- founder 54 (arrived t=0): 7%
- founder 103 (arrived t=0): 6%
- founder 8 (arrived t=0): 6%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | eat | flee | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–400 | 168.2 | 5.27 | 2.36 | 1.14 | 0 | 127 | 79 | 63% | 3% | 12% | 9% | 14% |
| 400–800 | 261.2 | 6.10 | 2.23 | 2.74 | 0 | 165 | 64 | 53% | 6% | 13% | 10% | 17% |
| 800–1200 | 262.0 | 6.56 | 2.26 | 2.35 | 0 | 156 | 69 | 55% | 4% | 13% | 10% | 18% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 5.4 | 0.04 | — |
| 100 | 100% | 5.5 | 0.04 | — |
| 200 | 100% | 5.6 | 0.04 | — |
| 400 | 100% | 5.7 | 0.05 | — |
| 500 | 100% | 5.8 | 0.05 | — |
| 600 | 100% | 5.9 | 0.05 | — |
| 700 | 100% | 6.1 | 0.06 | — |
| 800 | 99% | 6.1 | 0.05 | — |
| 900 | 98% | 6.1 | 0.06 | — |
| 1100 | 99% | 6.1 | 0.06 | — |
| 1200 | 99% | 6.1 | 0.06 | — |
| 1293 | 98% | 6.1 | 0.06 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
