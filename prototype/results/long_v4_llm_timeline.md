# How the genes developed — long_v4_llm

Run: 7000 ticks, 11 generations, 1810 births, 1843 deaths (predators 50%), 272 mutations, brain `llm`, seed 1234.
Checkpoints every 500 ticks; gene dropping with 500 random inheritances; behaviour per 2000 ticks. Data: `long_v4_llm_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 5 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 68 | 0.0 | 68 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.7 | 0.04 |
| 500 | 81 | 1.5 | 30 | 1% | 0.01 | 5.1 | 100% | 6.0 | 4.0 | 0.04 |
| 1500 | 10 | 0.6 | 20 | 0% | 0.00 | 5.0 | 100% | 4.6 | 3.9 | 0.04 |
| 2000 | 51 | 2.1 | 7 | 9% | 0.09 | 5.2 | 100% | 6.2 | 3.3 | 0.06 |
| 2500 | 135 | 3.4 | 7 | 11% | 0.11 | 4.9 | 100% | 8.8 | 3.4 | 0.08 |
| 3000 | 76 | 5.2 | 7 | 11% | 0.11 | 4.2 | 100% | 8.2 | 2.7 | 0.10 |
| 4000 | 55 | 11.0 | 7 | 19% | 0.21 | 3.6 | 98% | 5.0 | 1.9 | 0.30 |
| 4500 | 27 | 13.1 | 7 | 22% | 0.30 | 3.8 | 99% | 4.0 | 2.1 | 0.24 |
| 5000 | 10 | 0.0 | 10 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.4 | 0.04 |
| 5500 | 110 | 2.2 | 9 | 5% | 0.05 | 5.7 | 100% | 7.2 | 3.8 | 0.05 |
| 6500 | 111 | 5.8 | 9 | 12% | 0.13 | 5.2 | 100% | 9.8 | 3.9 | 0.07 |
| 7000 | 79 | 8.0 | 9 | 18% | 0.19 | 5.1 | 100% | 8.8 | 4.3 | 0.08 |

Slot by slot at the end (tick 7000):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| eat | 10 | 4.6 | No preference. | 35% | 14% | 0.18 | 99% |
| flee | 10 | 4.1 | Flee only when a predator is very close. | 41% | 8% | 0.08 | 100% |
| follow | 9 | 4.6 | No preference. | 33% | 27% | 0.29 | 100% |
| rest | 6 | 3.8 | Rest only when you feel safe. | 37% | 19% | 0.19 | 100% |
| mate | 9 | 4.3 | Mate only when food is plentiful. | 38% | 22% | 0.22 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| eat | No preference. | founder text | 0 | 1000 | — | — | 38% (t=1000) | present, 35% |
| eat | Eat whenever food is close. | founder text | 0 | 1500 | 3000 | — | 87% (t=4000) | present, 19% |
| eat | Only look for food when energy is low. | founder text | 0 | 1500 | — | — | 49% (t=2000) | present, 20% |
| eat | Always finish eating before doing anything else. | founder text | 0 | 500 | — | — | 39% (t=5500) | present, 11% |
| eat | Eat quickly, then move on. | founder text | 0 | 2000 | — | — | 27% (t=2000) | lost by t=5500 |
| flee | No preference. | founder text | 0 | 500 | 2500 | 4000 | 91% (t=4000) | present, 18% |
| flee | Run from any predator you see. | founder text | 0 | 500 | — | — | 36% (t=1000) | present, 18% |
| flee | Flee only when a predator is very close. | founder text | 0 | 5000 | — | — | 41% (t=7000) | present, 41% |
| flee | Run away from anything that attacks you. | founder text | 0 | 0 | — | — | 39% (t=2000) | present, 10% |
| follow | No preference. | founder text | 0 | 1500 | 2000 | 4000 | 91% (t=4000) | present, 33% |
| follow | Stay close to other animals. | founder text | 0 | 500 | — | — | 36% (t=500) | lost by t=7000 |
| follow | Follow others when you are lost or hungry. | founder text | 0 | 500 | — | — | 46% (t=1000) | present, 25% |
| follow | Keep your distance from other animals. | founder text | 0 | 0 | — | — | 34% (t=0) | present, 15% |
| follow | Follow the strongest animal nearby. | founder text | 0 | 1000 | — | — | 28% (t=1000) | lost by t=6500 |
| rest | No preference. | founder text | 0 | 0 | — | — | 44% (t=1000) | present, 27% |
| rest | Rest when you are tired. | founder text | 0 | 1500 | — | — | 31% (t=2000) | lost by t=7000 |
| rest | Never stop moving. | founder text | 0 | 500 | — | — | 30% (t=500) | lost by t=5500 |
| rest | Rest only when you feel safe. | founder text | 0 | 0 | 5500 | — | 55% (t=6000) | present, 37% |
| rest | Save energy by resting when food is far. | founder text | 0 | 2000 | — | — | 31% (t=2000) | present, 18% |
| rest | Rest only when you are exhausted. | mutant, depth 1 | 2143 | 1857 | — | — | 27% (t=4000) | lost by t=5000 |
| rest | Rest only when you are tired. | mutant, depth 2 | 3668 | 832 | — | — | 33% (t=4500) | lost by t=5000 |
| mate | No preference. | founder text | 0 | 1000 | 2000 | — | 79% (t=3500) | present, 24% |
| mate | Look for a partner when energy is high. | founder text | 0 | 5000 | — | — | 38% (t=5500) | present, 3% |
| mate | Mate with any nearby adult. | founder text | 0 | 500 | — | — | 41% (t=500) | present, 14% |
| mate | Mate only when food is plentiful. | founder text | 0 | 6000 | — | — | 38% (t=7000) | present, 38% |
| mate | Seek a partner before growing old. | founder text | 0 | 0 | — | — | 25% (t=0) | lost by t=5500 |
| mate | Preference for water. | mutant, depth 1 | 3120 | 1380 | — | — | 26% (t=4500) | lost by t=5000 |

Lineages of the mutants that swept:

**rest: "Rest only when you are exhausted."** (peak 27%, lost by t=5000)

```text
Rest when you are tired.  ← founder
Rest only when you are exhausted.  ← "Change how near, how far or how much this rule is about."
```

**rest: "Rest only when you are tired."** (peak 33%, lost by t=5000)

```text
Rest when you are tired.  ← founder
Rest only when you are exhausted.  ← "Change how near, how far or how much this rule is about."
Rest only when you are tired.  ← "Change one word of this rule into a related word."
```

**mate: "Preference for water."** (peak 26%, lost by t=5000)

```text
No preference.  ← neutral
Preference for water.  ← "Change when this rule applies."
```


Leader of each slot over time (a row when it changes):

- **eat** (6 changes): t=0 "No preference." (24%) → t=500 "Always finish eating before doing anything else." (31%) → t=1000 "No preference." (38%) → t=1500 "Only look for food when energy is low." (40%) → t=3000 "Eat whenever food is close." (51%) → t=5500 "Always finish eating before doing anything else." (39%) → t=7000 "No preference." (35%)
- **flee** (6 changes): t=0 "Run away from anything that attacks you." (26%) → t=500 "No preference." (30%) → t=2000 "Run away from anything that attacks you." (39%) → t=2500 "No preference." (53%) → t=5000 "Flee only when a predator is very close." (30%) → t=6000 "Run from any predator you see." (33%) → t=7000 "Flee only when a predator is very close." (41%)
- **follow** (3 changes): t=0 "Keep your distance from other animals." (34%) → t=500 "Stay close to other animals." (36%) → t=1000 "Follow others when you are lost or hungry." (46%) → t=1500 "No preference." (30%)
- **rest** (3 changes): t=0 "Rest only when you feel safe." (26%) → t=500 "No preference." (32%) → t=1500 "Rest when you are tired." (30%) → t=3000 "Rest only when you feel safe." (32%)
- **mate** (4 changes): t=0 "Seek a partner before growing old." (25%) → t=500 "Mate with any nearby adult." (41%) → t=1500 "No preference." (40%) → t=5000 "Look for a partner when energy is high." (30%) → t=6000 "Mate only when food is plentiful." (34%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 206 | 1.1 | 17% | 13% | 2.6 | 11% | 5.6 | 91% |
| 5+ carriers at once | 26 | 1.1 | 31% | 12% | 2.3 | 4% | 5.3 | 96% |
| alive when mutants were most common (t=4500) | 11 | 1.2 | 27% | 0% | 1.9 | 9% | 4.5 | 91% |
| alive at the end | 25 | 1.1 | 28% | 12% | 2.2 | 8% | 5.3 | 96% |
| mutation test (no selection, T = 1.2) | 288 | 1.0 | 15% | 9% | 2.8 | 5% | 6.3 | 99% |

150 mutations per 1 000 births; 0 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 0 | 0 (0–1) | 1.000 |
| mutants that reached 90 % | 0 | 0 (0–0) | 1.000 |
| founder texts that reached 90 % | 2 | 0 (0–1) | 0.012 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| eat | No preference. | 38% / 22% / 0.126 | 35% / 15% / 0.062 | 0.93 [0.72–1.14] (244) |
| eat | Eat whenever food is close. | 87% / 34% / 0.042 | 19% / 30% / 0.784 | 1.11 [0.95–1.27] (670) |
| eat | Only look for food when energy is low. | 49% / 30% / 0.046 | 20% / 10% / 0.160 | 1.00 [0.79–1.20] (270) |
| eat | Always finish eating before doing anything else. | 39% / 34% / 0.296 | 11% / 29% / 0.906 | 1.00 [0.81–1.18] (420) |
| eat | Eat quickly, then move on. | 27% / 21% / 0.280 | 0% / 0% / 1.000 | 0.97 [0.68–1.26] (127) |
| flee | No preference. | 91% / 36% / 0.044 | 18% / 19% / 0.546 | 1.09 [0.93–1.24] (747) |
| flee | Run from any predator you see. | 36% / 32% / 0.428 | 18% / 21% / 0.580 | 0.98 [0.80–1.17] (364) |
| flee | Flee only when a predator is very close. | 41% / 14% / 0.028 | 41% / 14% / 0.028 | 1.00 [0.81–1.18] (301) |
| flee | Run away from anything that attacks you. | 39% / 36% / 0.398 | 10% / 29% / 0.920 | 1.00 [0.77–1.22] (240) |
| follow | No preference. | 91% / 43% / 0.030 | 33% / 25% / 0.254 | 1.10 [0.96–1.23] (822) |
| follow | Stay close to other animals. | 36% / 24% / 0.036 | 0% / 11% / 1.000 | 0.96 [0.68–1.24] (165) |
| follow | Follow others when you are lost or hungry. | 46% / 42% / 0.440 | 25% / 34% / 0.748 | 0.96 [0.77–1.15] (353) |
| follow | Keep your distance from other animals. | 34% / 34% / 1.000 | 15% / 4% / 0.102 | 0.95 [0.72–1.19] (223) |
| follow | Follow the strongest animal nearby. | 28% / 7% / 0.018 | 0% / 6% / 1.000 | 0.97 [0.61–1.32] (128) |
| rest | No preference. | 44% / 33% / 0.218 | 27% / 13% / 0.150 | 1.02 [0.83–1.21] (278) |
| rest | Rest when you are tired. | 31% / 27% / 0.382 | 0% / 11% / 1.000 | 1.03 [0.82–1.24] (315) |
| rest | Never stop moving. | 30% / 31% / 0.642 | 0% / 5% / 1.000 | 1.04 [0.67–1.40] (130) |
| rest | Rest only when you feel safe. | 55% / 40% / 0.104 | 37% / 40% / 0.588 | 1.01 [0.86–1.16] (638) |
| rest | Save energy by resting when food is far. | 31% / 34% / 0.610 | 18% / 6% / 0.106 | 1.04 [0.77–1.31] (178) |
| rest | Rest only when you are exhausted. | 27% / 15% / 0.282 | 0% / 0% / 1.000 | 1.18 [0.69–1.68] (94) |
| rest | Rest only when you are tired. | 33% / 8% / 0.008 | 0% / 0% / 1.000 | 0.95 [-0.02–1.92] (24) |
| mate | No preference. | 79% / 61% / 0.094 | 24% / 17% / 0.260 | 1.08 [0.93–1.23] (736) |
| mate | Look for a partner when energy is high. | 38% / 25% / 0.046 | 3% / 16% / 0.906 | 0.84 [0.59–1.09] (178) |
| mate | Mate with any nearby adult. | 41% / 38% / 0.382 | 14% / 23% / 0.706 | 0.98 [0.73–1.22] (230) |
| mate | Mate only when food is plentiful. | 38% / 17% / 0.054 | 38% / 17% / 0.054 | 1.07 [0.86–1.27] (281) |
| mate | Seek a partner before growing old. | 25% / 25% / 1.000 | 0% / 5% / 1.000 | 0.89 [0.64–1.14] (160) |
| mate | Preference for water. | 26% / 19% / 0.346 | 0% / 0% / 1.000 | 1.09 [0.45–1.73] (44) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 68 |
| 500 | 30 |
| 1500 | 20 |
| 2000 | 7 |
| 2500 | 7 |
| 3000 | 7 |
| 4000 | 7 |
| 4500 | 7 |
| 5000 | 10 |
| 5500 | 9 |
| 6500 | 9 |
| 7000 | 9 |

From tick 3500, at least one founder or newcomer is an ancestor of every living animal (family trees mix within a few generations in a small population; genes don't: see the shares below).

Expected share of the living animals' genes coming from each family at the end (top 6):

- newcomer 1528 (arrived t=4906): 30%
- newcomer 1531 (arrived t=4938): 16%
- newcomer 1513 (arrived t=4809): 12%
- newcomer 1535 (arrived t=4989): 12%
- newcomer 1522 (arrived t=4855): 9%
- newcomer 1520 (arrived t=4837): 7%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | eat | flee | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–2000 | 41.6 | 4.48 | 3.33 | 1.60 | 19 | 187 | 68 | 34% | 10% | 13% | 19% | 23% |
| 2000–4000 | 85.2 | 3.69 | 1.84 | 1.82 | 0 | 270 | 56 | 33% | 6% | 12% | 21% | 29% |
| 4000–6000 | 61.0 | 3.28 | 1.56 | 1.27 | 25 | 238 | 67 | 33% | 5% | 12% | 18% | 32% |
| 6000–7000 | 106.9 | 3.82 | 1.25 | 3.09 | 0 | 299 | 48 | 36% | 6% | 13% | 24% | 22% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 5.5 | 0.04 | 92% |
| 500 | 100% | 5.1 | 0.04 | 94% |
| 1500 | 100% | 5.0 | 0.04 | 92% |
| 2000 | 100% | 5.2 | 0.06 | 90% |
| 2500 | 100% | 4.9 | 0.08 | 88% |
| 3000 | 100% | 4.2 | 0.10 | 85% |
| 4000 | 98% | 3.6 | 0.30 | 81% |
| 4500 | 99% | 3.8 | 0.24 | 74% |
| 5000 | 100% | 5.5 | 0.04 | 90% |
| 5500 | 100% | 5.7 | 0.05 | 86% |
| 6500 | 100% | 5.2 | 0.07 | 85% |
| 7000 | 100% | 5.1 | 0.08 | 88% |

Judge: gemma4:12b (`prompts/judge_sense_v1.txt`, temperature 0) on 82 genes that had at least 3 carriers at once (0 new calls): 76% called usable. *Judged usable* above is the carrier-weighted share among judged genes. One model's opinion; a sample to check by hand:

| slot | gene | judge |
|---|---|---|
| eat | Only look for food when energy is low. | usable |
| eat | Never finish eating before doing anything else. | usable |
| eat | Always finish eating before doing anything else. | usable |
| eat | Eat quickly, then move on. | usable |
| flee | No patience. | usable |
| flee | Flee only when a predator is very close. | usable |
| flee | Run away from anything that attacks you. | usable |
| flee | Flee only when a predator is nearby. | usable |
| follow | Keep your distance from other predators. | not usable |
| follow | Any preference. | not usable |
| follow | Avoid the strongest animal nearby. | not usable |
| follow | Eat everything. | not usable |
| follow | Keep your distance from other animals. | usable |
| follow | Approach other animals. | not usable |
| follow | No preference, unless it is hungry. | usable |
| follow | No local preference. | usable |
| follow | Strong preference. | usable |
| mate | Look for a partner when energy is high. | usable |
| mate | Seek a partner before moving into a new territory. | usable |
| mate | Preference for water. | not usable |
| mate | Always prefers the largest prey. | not usable |
| mate | Mate only when food is plentiful. | usable |
| mate | Minor preference. | not usable |
| mate | Mate only when water is scarce. | usable |
| mate | Avoid partners when energy is low. | usable |
| mate | Mate only when water is plentiful. | usable |
| rest | Rest when you are tired. | not usable |
| rest | Rest only when you are in a cave. | usable |
| rest | Save energy by resting when food is scarce. | usable |
| rest | Save energy by resting when food is far. | usable |
