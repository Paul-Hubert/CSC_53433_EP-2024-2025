# How the genes developed — long_v4_llm

Run: 7000 ticks, 7 generations, 345 births, 332 deaths (predators 0%), 40 mutations, brain `llm`, seed 1234.
Checkpoints every 500 ticks; gene dropping with 500 random inheritances; behaviour per 2000 ticks. Data: `long_v4_llm_predator_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 4 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 14 | 0.0 | 14 | 0% | 0.00 | 4.7 | 100% | 4.5 | 3.8 | 0.03 |
| 500 | 32 | 0.9 | 13 | 2% | 0.02 | 4.6 | 100% | 5.0 | 3.6 | 0.04 |
| 1500 | 15 | 4.2 | 11 | 8% | 0.10 | 4.1 | 100% | 4.0 | 2.8 | 0.08 |
| 2000 | 3 | 0.0 | 3 | 0% | 0.00 | 5.0 | 100% | 2.8 | 2.7 | 0.07 |
| 2500 | 21 | 2.2 | 3 | 6% | 0.07 | 5.4 | 100% | 3.5 | 2.6 | 0.09 |
| 3000 | 34 | 3.4 | 3 | 6% | 0.07 | 5.5 | 100% | 3.2 | 2.2 | 0.08 |
| 4000 | 34 | 6.5 | 3 | 6% | 0.06 | 5.6 | 99% | 3.0 | 1.9 | 0.06 |
| 4500 | 32 | 8.7 | 3 | 9% | 0.11 | 5.6 | 99% | 3.8 | 2.1 | 0.09 |
| 5000 | 14 | 11.1 | 3 | 21% | 0.25 | 5.4 | 100% | 2.8 | 1.7 | 0.16 |
| 5500 | 3 | 0.0 | 3 | 0% | 0.00 | 5.7 | 100% | 2.0 | 1.9 | 0.06 |
| 6500 | 16 | 2.9 | 3 | 11% | 0.11 | 5.8 | 100% | 3.0 | 2.1 | 0.18 |
| 7000 | 34 | 3.9 | 3 | 6% | 0.06 | 5.7 | 100% | 3.0 | 2.0 | 0.18 |

Slot by slot at the end (tick 7000):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| predator.hunt | 2 | 1.6 | Keep chasing until the prey is caught. | 74% | 0% | 0.00 | 100% |
| predator.follow | 3 | 1.2 | Stay close to other predators. | 91% | 9% | 0.09 | 100% |
| predator.rest | 5 | 3.3 | No preference. | 47% | 15% | 0.15 | 100% |
| predator.mate | 2 | 1.8 | Look for a mate when well fed. | 68% | 0% | 0.00 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| predator.hunt | No preference. | founder text | 0 | 2000 | 2000 | — | 67% (t=2000) | lost by t=5500 |
| predator.hunt | Chase any prey you see. | founder text | 0 | 1000 | — | — | 33% (t=5500) | present, 26% |
| predator.hunt | Hunt only when you are hungry. | founder text | 0 | 2000 | 3000 | — | 62% (t=4000) | lost by t=5500 |
| predator.hunt | Attack only when prey is close. | founder text | 0 | 0 | — | — | 38% (t=500) | lost by t=2000 |
| predator.hunt | Keep chasing until the prey is caught. | founder text | 0 | 0 | 1500 | — | 83% (t=6000) | present, 74% |
| predator.hunt | No preference for any distance. | mutant, depth 1 | 2180 | 2820 | 2820 | — | 64% (t=5000) | lost by t=5500 |
| predator.follow | No preference. | founder text | 0 | 0 | — | — | 33% (t=1500) | lost by t=2000 |
| predator.follow | Stay close to other predators. | founder text | 0 | 0 | 5500 | 5500 | 100% (t=5500) | present, 91% |
| predator.follow | Hunt as a pack. | founder text | 0 | 1000 | — | — | 28% (t=1000) | lost by t=2000 |
| predator.follow | Keep away from other predators. | founder text | 0 | 2000 | 3000 | — | 79% (t=5000) | lost by t=5500 |
| predator.follow | Follow others when no prey is in sight. | founder text | 0 | 1500 | — | — | 38% (t=2500) | lost by t=5500 |
| predator.rest | No preference. | founder text | 0 | 0 | 1500 | — | 60% (t=1500) | present, 47% |
| predator.rest | Rest when your belly is full. | founder text | 0 | 0 | 4000 | — | 50% (t=4000) | present, 18% |
| predator.rest | Never stop moving. | founder text | 0 | 2000 | 3500 | — | 64% (t=5000) | lost by t=5500 |
| predator.rest | Lie still and let prey come to you. | founder text | 0 | 5500 | — | — | 33% (t=5500) | present, 21% |
| predator.rest | Rest when no prey is in sight. | founder text | 1864 | 136 | — | — | 33% (t=2000) | lost by t=5000 |
| predator.rest | Strong preference for local resources. | mutant, depth 1 | 5868 | 632 | — | — | 25% (t=6500) | present, 9% |
| predator.mate | No preference. | founder text | 0 | 500 | — | — | 47% (t=1500) | lost by t=2000 |
| predator.mate | Look for a mate when well fed. | founder text | 0 | 2000 | 2500 | 4000 | 94% (t=4000) | present, 68% |
| predator.mate | Hunt first, mate later. | founder text | 0 | 0 | 1000 | — | 69% (t=1000) | lost by t=5500 |
| predator.mate | Seek a partner before growing old. | founder text | 0 | 0 | 5500 | — | 67% (t=5500) | present, 32% |
| predator.mate | Mate with any nearby adult. | founder text | 1864 | 136 | — | — | 33% (t=2000) | lost by t=3500 |

Lineages of the mutants that swept:

**predator.hunt: "No preference for any distance."** (peak 64%, lost by t=5500)

```text
No preference.  ← neutral
No preference for any distance.  ← "Change how near, how far or how much this rule is about."
```

**predator.rest: "Strong preference for local resources."** (peak 25%, present, 9%)

```text
No preference.  ← neutral
Strong preference for local resources.  ← "Change how near, how far or how much this rule is about."
```


Leader of each slot over time (a row when it changes):

- **predator.hunt** (5 changes): t=0 "Attack only when prey is close." (29%) → t=1500 "Keep chasing until the prey is caught." (60%) → t=2000 "No preference." (67%) → t=2500 "Hunt only when you are hungry." (48%) → t=5000 "No preference for any distance." (64%) → t=5500 "Keep chasing until the prey is caught." (67%)
- **predator.follow** (5 changes): t=0 "No preference." (29%) → t=1000 "Hunt as a pack." (28%) → t=1500 "No preference." (33%) → t=2000 "Stay close to other predators." (33%) → t=2500 "Keep away from other predators." (43%) → t=5500 "Stay close to other predators." (100%)
- **predator.rest** (6 changes): t=0 "Rest when your belly is full." (43%) → t=500 "No preference." (47%) → t=2000 "Rest when your belly is full." (33%) → t=3500 "Never stop moving." (50%) → t=4000 "Rest when your belly is full." (50%) → t=5000 "Never stop moving." (64%) → t=5500 "No preference." (33%)
- **predator.mate** (4 changes): t=0 "Hunt first, mate later." (36%) → t=1500 "No preference." (47%) → t=2000 "Look for a mate when well fed." (33%) → t=5500 "Seek a partner before growing old." (67%) → t=6000 "Look for a mate when well fed." (67%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 38 | 1.2 | 16% | 8% | 2.6 | 13% | 5.2 | 95% |
| 5+ carriers at once | 3 | 1.0 | 0% | 0% | 4.0 | 33% | 4.0 | 100% |
| alive when mutants were most common (t=5000) | 3 | 1.3 | 0% | 33% | 3.7 | 0% | 6.3 | 100% |
| alive at the end | 4 | 1.0 | 25% | 25% | 4.2 | 25% | 7.0 | 100% |
| mutation test (no selection, T = 1.2) | 288 | 1.0 | 15% | 9% | 2.8 | 5% | 6.3 | 96% |

116 mutations per 1 000 births; 0 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 1 | 0 (0–1) | 0.480 |
| mutants that reached 90 % | 0 | 0 (0–0) | 1.000 |
| founder texts that reached 90 % | 2 | 3 (1–4) | 0.870 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| predator.hunt | No preference. | 67% / 67% / 1.000 | 0% / 0% / 1.000 | 0.99 [0.59–1.39] (39) |
| predator.hunt | Chase any prey you see. | 33% / 33% / 1.000 | 26% / 17% / 0.314 | 1.13 [0.50–1.77] (18) |
| predator.hunt | Hunt only when you are hungry. | 62% / 37% / 0.152 | 0% / 0% / 1.000 | 1.04 [0.77–1.31] (102) |
| predator.hunt | Attack only when prey is close. | 38% / 35% / 0.392 | 0% / 0% / 1.000 | 1.02 [0.52–1.52] (34) |
| predator.hunt | Keep chasing until the prey is caught. | 83% / 83% / 1.000 | 74% / 83% / 0.778 | 0.91 [0.60–1.22] (75) |
| predator.hunt | No preference for any distance. | 64% / 29% / 0.082 | 0% / 0% / 1.000 | 1.17 [0.60–1.74] (36) |
| predator.follow | No preference. | 33% / 24% / 0.318 | 0% / 0% / 1.000 | 1.14 [0.60–1.69] (29) |
| predator.follow | Stay close to other predators. | 100% / 100% / 1.000 | 91% / 90% / 0.598 | 0.95 [0.61–1.29] (58) |
| predator.follow | Hunt as a pack. | 28% / 23% / 0.350 | 0% / 0% / 1.000 | 0.89 [0.40–1.37] (32) |
| predator.follow | Keep away from other predators. | 79% / 57% / 0.382 | 0% / 0% / 1.000 | 1.04 [0.80–1.28] (128) |
| predator.follow | Follow others when no prey is in sight. | 38% / 47% / 0.660 | 0% / 0% / 1.000 | 1.00 [0.67–1.33] (75) |
| predator.rest | No preference. | 60% / 43% / 0.240 | 47% / 27% / 0.164 | 0.93 [0.60–1.26] (65) |
| predator.rest | Rest when your belly is full. | 50% / 46% / 0.448 | 18% / 18% / 0.534 | 0.99 [0.76–1.21] (133) |
| predator.rest | Never stop moving. | 64% / 51% / 0.444 | 0% / 0% / 1.000 | 1.13 [0.81–1.45] (93) |
| predator.rest | Lie still and let prey come to you. | 33% / 33% / 1.000 | 21% / 29% / 0.666 | 1.24 [0.24–2.23] (12) |
| predator.rest | Rest when no prey is in sight. | 33% / 33% / 1.000 | 0% / 0% / 1.000 | 1.01 [0.54–1.47] (12) |
| predator.rest | Strong preference for local resources. | 25% / 27% / 0.652 | 9% / 20% / 0.838 | 0.72 [-0.07–1.50] (6) |
| predator.mate | No preference. | 47% / 18% / 0.066 | 0% / 0% / 1.000 | 0.92 [0.38–1.45] (31) |
| predator.mate | Look for a mate when well fed. | 94% / 50% / 0.076 | 68% / 36% / 0.104 | 1.06 [0.85–1.28] (173) |
| predator.mate | Hunt first, mate later. | 69% / 47% / 0.018 | 0% / 0% / 1.000 | 0.96 [0.68–1.24] (88) |
| predator.mate | Seek a partner before growing old. | 67% / 67% / 1.000 | 32% / 64% / 0.928 | 1.15 [0.55–1.74] (21) |
| predator.mate | Mate with any nearby adult. | 33% / 33% / 1.000 | 0% / 0% / 1.000 | 0.88 [0.09–1.66] (6) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 14 |
| 500 | 13 |
| 1500 | 11 |
| 2000 | 3 |
| 2500 | 3 |
| 3000 | 3 |
| 4000 | 3 |
| 4500 | 3 |
| 5000 | 3 |
| 5500 | 3 |
| 6500 | 3 |
| 7000 | 3 |

From tick 3000, at least one founder or newcomer is an ancestor of every living animal (family trees mix within a few generations in a small population; genes don't: see the shares below).

Expected share of the living animals' genes coming from each family at the end (top 6):

- newcomer 1545 (arrived t=5103): 45%
- newcomer 1570 (arrived t=5222): 37%
- newcomer 1553 (arrived t=5130): 18%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | hunt | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–2000 | 22.1 | 2.51 | 6.25 | 2.82 | 3 | 360 | 57 | 33% | 12% | 24% | 31% |
| 2000–4000 | 26.2 | 2.21 | 5.96 | 1.60 | 0 | 411 | 62 | 31% | 11% | 17% | 41% |
| 4000–6000 | 16.4 | 1.98 | 5.78 | 2.83 | 4 | 494 | 61 | 29% | 13% | 22% | 37% |
| 6000–7000 | 20.1 | 2.64 | 6.67 | 1.19 | 0 | 374 | 66 | 34% | 9% | 14% | 44% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 4.7 | 0.03 | 100% |
| 500 | 100% | 4.6 | 0.04 | 99% |
| 1500 | 100% | 4.1 | 0.08 | 98% |
| 2000 | 100% | 5.0 | 0.07 | 100% |
| 2500 | 100% | 5.4 | 0.09 | 100% |
| 3000 | 100% | 5.5 | 0.08 | 100% |
| 4000 | 99% | 5.6 | 0.06 | 100% |
| 4500 | 99% | 5.6 | 0.09 | 99% |
| 5000 | 100% | 5.4 | 0.16 | 100% |
| 5500 | 100% | 5.7 | 0.06 | 100% |
| 6500 | 100% | 5.8 | 0.18 | 92% |
| 7000 | 100% | 5.7 | 0.18 | 96% |

Judge: gemma4:12b (`prompts/judge_sense_v1.txt`, temperature 0) on 28 genes that had at least 3 carriers at once (0 new calls): 86% called usable. *Judged usable* above is the carrier-weighted share among judged genes. One model's opinion; a sample to check by hand:

| slot | gene | judge |
|---|---|---|
| predator.follow | No preference. | usable |
| predator.follow | Stay close to other predators. | usable |
| predator.follow | Stay close to other prey. | not usable |
| predator.follow | Hunt as a pack. | usable |
| predator.follow | Keep away from other predators. | usable |
| predator.follow | Follow others when no prey is in sight. | usable |
| predator.follow | Hunt. | not usable |
| predator.hunt | No preference. | usable |
| predator.hunt | Chase any prey you see. | usable |
| predator.hunt | No preference for any distance. | usable |
| predator.hunt | No preference for any distance, unless a predator is nearby. | usable |
| predator.hunt | Hunt only when you are hungry. | usable |
| predator.hunt | Attack only when prey is close. | usable |
| predator.hunt | Keep chasing until the prey is caught. | usable |
| predator.hunt | Keep chasing. | usable |
| predator.mate | No preference. | usable |
| predator.mate | Look for a mate when well fed. | usable |
| predator.mate | Look for a predator when well fed. | not usable |
| predator.mate | Mate with any nearby adult. | usable |
| predator.mate | Hunt first, mate later. | usable |
| predator.mate | Seek a partner before growing old. | usable |
| predator.rest | No preference. | usable |
| predator.rest | Rest when your belly is full. | usable |
| predator.rest | Strong preference for local resources. | not usable |
| predator.rest | Wait for prey to come near you. | usable |
| predator.rest | Never stop moving. | usable |
| predator.rest | Lie still and let prey come to you. | usable |
| predator.rest | Rest when no prey is in sight. | usable |
