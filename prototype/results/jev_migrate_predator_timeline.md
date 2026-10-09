# How the genes developed — jev_migrate

Run: 22817 ticks, 119 generations, 3593 births, 3575 deaths (predators 0%), 413 mutations, brain `jev`, seed 1234.
Checkpoints every 500 ticks; gene dropping with 500 random inheritances; behaviour per 2000 ticks. Data: `jev_migrate_predator_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 4 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 14 | 0.0 | 14 | 0% | 0.00 | 4.7 | 100% | 4.5 | 3.8 | 0.03 |
| 2000 | 34 | 8.9 | 9 | 20% | 0.20 | 5.5 | 99% | 4.8 | 2.5 | 0.14 |
| 4000 | 32 | 20.0 | 7 | 52% | 0.56 | 5.9 | 100% | 5.0 | 2.6 | 0.15 |
| 6500 | 33 | 33.7 | 7 | 78% | 1.01 | 5.7 | 100% | 4.2 | 2.1 | 0.33 |
| 8500 | 34 | 44.6 | 7 | 93% | 1.32 | 5.9 | 99% | 3.5 | 2.1 | 0.17 |
| 10500 | 34 | 54.9 | 7 | 88% | 1.54 | 6.6 | 99% | 4.2 | 2.3 | 0.24 |
| 12500 | 30 | 64.4 | 7 | 77% | 1.47 | 6.7 | 99% | 4.2 | 2.2 | 0.28 |
| 14500 | 33 | 74.6 | 7 | 75% | 1.62 | 6.7 | 98% | 3.0 | 1.8 | 0.34 |
| 16500 | 31 | 85.6 | 7 | 75% | 1.38 | 6.5 | 99% | 3.0 | 1.4 | 0.35 |
| 19000 | 34 | 97.8 | 7 | 76% | 1.49 | 6.7 | 100% | 2.8 | 1.6 | 0.34 |
| 21000 | 15 | 107.9 | 7 | 75% | 1.33 | 6.5 | 100% | 1.8 | 1.3 | 0.27 |
| 22817 | 32 | 117.9 | 7 | 76% | 1.45 | 6.8 | 98% | 3.0 | 1.4 | 0.56 |

Slot by slot at the end (tick 22817):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| predator.hunt | 3 | 1.3 | Hunt when hungry. | 88% | 100% | 1.12 | 94% |
| predator.follow | 3 | 1.9 | When other animals approach them closely, they must not join in. | 66% | 100% | 3.41 | 100% |
| predator.rest | 4 | 1.5 | Listen whenever you feel hungry, not just after eating. | 81% | 100% | 1.22 | 100% |
| predator.mate | 2 | 1.1 | Hunt first, mate later. | 97% | 3% | 0.03 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| predator.hunt | Hunt only when you are hungry. | founder text | 0 | 1000 | — | — | 32% (t=1500) | lost by t=3000 |
| predator.hunt | Attack only when prey is close. | founder text | 0 | 0 | — | — | 34% (t=3000) | lost by t=5500 |
| predator.hunt | Keep chasing until the prey is caught. | founder text | 0 | 0 | — | — | 29% (t=0) | lost by t=2500 |
| predator.hunt | Hunt when hungry. | mutant, depth 1 | 1316 | 684 | 1184 | 5184 | 100% (t=16000) | present, 88% |
| predator.hunt | Attack only when prey is not far. | mutant, depth 1 | 4601 | 1399 | — | — | 45% (t=6000) | lost by t=6500 |
| predator.hunt | It may hunt again later. | mutant, depth 3 | 9869 | 1131 | — | — | 32% (t=11500) | lost by t=13000 |
| predator.follow | No preference. | founder text | 0 | 0 | — | — | 29% (t=0) | lost by t=500 |
| predator.follow | Stay close to other predators. | founder text | 0 | 0 | 500 | — | 50% (t=500) | lost by t=4000 |
| predator.follow | Keep away from other predators. | founder text | 0 | 500 | 2000 | — | 72% (t=3000) | lost by t=7500 |
| predator.follow | Follow others when no prey is in sight. | founder text | 0 | 4000 | — | — | 28% (t=4000) | lost by t=7000 |
| predator.follow | When other animals move with prey nearby, do not follow. | mutant, depth 2 | 5014 | 986 | 1486 | — | 88% (t=7500) | lost by t=14000 |
| predator.follow | When other animals approach them closely, they must not join in. | mutant, depth 3 | 7253 | 5247 | 6247 | — | 82% (t=16000) | present, 66% |
| predator.follow | When other animals avoid moving towards prey nearby, do not approach them. | mutant, depth 3 | 10600 | 1400 | — | — | 35% (t=13000) | lost by t=17500 |
| predator.follow | When other animals approach them closely, they must stay on their own. | mutant, depth 4 | 16457 | 2543 | — | — | 48% (t=22000) | present, 28% |
| predator.follow | When other animals approach them closely, they shall never join in. | mutant, depth 4 | 18170 | 830 | — | — | 41% (t=19000) | lost by t=21000 |
| predator.rest | No preference. | founder text | 0 | 0 | 1000 | — | 50% (t=1000) | lost by t=3000 |
| predator.rest | Rest when your belly is full. | founder text | 0 | 0 | 1500 | — | 82% (t=2000) | lost by t=9000 |
| predator.rest | When you are hungry, rest when your belly is full. | mutant, depth 1 | 1919 | 1081 | — | — | 47% (t=4000) | lost by t=6000 |
| predator.rest | Don't stop eating because you are feeling hungry. | mutant, depth 1 | 2566 | 1434 | — | — | 25% (t=4000) | lost by t=6500 |
| predator.rest | When your belly is not full, Rest. | mutant, depth 1 | 2942 | 2558 | 3058 | — | 71% (t=8500) | lost by t=11500 |
| predator.rest | Listen whenever you feel hungry, not just after eating. | mutant, depth 1 | 7252 | 1748 | 2748 | 11748 | 100% (t=21000) | present, 81% |
| predator.rest | Do not start because you are hungry. | mutant, depth 3 | 7262 | 3238 | — | — | 47% (t=11500) | lost by t=15500 |
| predator.rest | Listen when you feel hungry. | mutant, depth 2 | 15058 | 2442 | — | — | 25% (t=17500) | lost by t=19500 |
| predator.mate | Hunt first, mate later. | founder text | 0 | 0 | 1000 | 11500 | 100% (t=12000) | present, 97% |
| predator.mate | Seek a partner before growing old. | founder text | 0 | 0 | — | — | 29% (t=0) | lost by t=4000 |
| predator.mate | Seek a friend or someone else before aging. | mutant, depth 1 | 1619 | 881 | — | — | 26% (t=2500) | lost by t=4500 |
| predator.mate | Seek a friend before aging. | mutant, depth 2 | 2210 | 1290 | — | — | 29% (t=3500) | lost by t=6000 |
| predator.mate | Hunt and not mate earlier. | mutant, depth 1 | 5230 | 1270 | 1770 | — | 62% (t=7500) | lost by t=12000 |
| predator.mate | Hunt. | mutant, depth 1 | 6613 | 1887 | — | — | 43% (t=9000) | lost by t=11500 |

Lineages of the mutants that swept:

**predator.hunt: "Hunt when hungry."** (peak 100%, present, 88%)

```text
Hunt only when you are hungry.  ← founder
Hunt when hungry.  ← "Remove a condition from this rule, or make it simpler."
```

**predator.hunt: "Attack only when prey is not far."** (peak 45%, lost by t=6500)

```text
Attack only when prey is close.  ← founder
Attack only when prey is not far.  ← "Change one word of this rule into a related word."
```

**predator.hunt: "It may hunt again later."** (peak 32%, lost by t=13000)

```text
Hunt only when you are hungry.  ← founder
Hunt when hungry.  ← "Remove a condition from this rule, or make it simpler."
Hunter when fatigued.  ← "Make the rule in this sentence a little weaker."
It may hunt again later.  ← "Change when this rule applies."
```

**predator.follow: "When other animals move with prey nearby, do not follow."** (peak 88%, lost by t=14000)

```text
Follow others when no prey is in sight.  ← founder
When other animals move without any prey nearby, do not follow.  ← "Make the rule in this sentence a little weaker."
When other animals move with prey nearby, do not follow.  ← "Change when this rule applies."
```

**predator.follow: "When other animals approach them closely, they must not join in."** (peak 82%, present, 66%)

```text
Follow others when no prey is in sight.  ← founder
When other animals move without any prey nearby, do not follow.  ← "Make the rule in this sentence a little weaker."
When other animals move with prey nearby, do not follow.  ← "Change when this rule applies."
When other animals approach them closely, they must not join in.  ← "Change how near, how far or how much this rule is about."
```

**predator.follow: "When other animals avoid moving towards prey nearby, do not approach them."** (peak 35%, lost by t=17500)

```text
Follow others when no prey is in sight.  ← founder
When other animals move without any prey nearby, do not follow.  ← "Make the rule in this sentence a little weaker."
When other animals move with prey nearby, do not follow.  ← "Change when this rule applies."
When other animals avoid moving towards prey nearby, do not approach them.  ← "Make this rule say the opposite."
```

**predator.follow: "When other animals approach them closely, they must stay on their own."** (peak 48%, present, 28%)

```text
Follow others when no prey is in sight.  ← founder
When other animals move without any prey nearby, do not follow.  ← "Make the rule in this sentence a little weaker."
When other animals move with prey nearby, do not follow.  ← "Change when this rule applies."
When other animals approach them closely, they must not join in.  ← "Change how near, how far or how much this rule is about."
When other animals approach them closely, they must stay on their own.  ← "Remove a condition from this rule, or make it simpler."
```

**predator.follow: "When other animals approach them closely, they shall never join in."** (peak 41%, lost by t=21000)

```text
Follow others when no prey is in sight.  ← founder
When other animals move without any prey nearby, do not follow.  ← "Make the rule in this sentence a little weaker."
When other animals move with prey nearby, do not follow.  ← "Change when this rule applies."
When other animals approach them closely, they must not join in.  ← "Change how near, how far or how much this rule is about."
When other animals approach them closely, they shall never join in.  ← "Add a short condition to this rule."
```

**predator.rest: "When you are hungry, rest when your belly is full."** (peak 47%, lost by t=6000)

```text
Rest when your belly is full.  ← founder
When you are hungry, rest when your belly is full.  ← "Add a short condition to this rule."
```

**predator.rest: "Don't stop eating because you are feeling hungry."** (peak 25%, lost by t=6500)

```text
Rest when your belly is full.  ← founder
Don't stop eating because you are feeling hungry.  ← "Make this rule say the opposite."
```

**predator.rest: "When your belly is not full, Rest."** (peak 71%, lost by t=11500)

```text
Rest when your belly is full.  ← founder
When your belly is not full, Rest.  ← "Change one word of this rule into a related word."
```

**predator.rest: "Listen whenever you feel hungry, not just after eating."** (peak 100%, present, 81%)

```text
Rest when your belly is full.  ← founder
Listen whenever you feel hungry, not just after eating.  ← "Make the rule in this sentence a little weaker."
```

**predator.rest: "Do not start because you are hungry."** (peak 47%, lost by t=15500)

```text
Rest when your belly is full.  ← founder
Don't stop eating because you are feeling hungry.  ← "Make this rule say the opposite."
Don't stop because of hunger.  ← "Remove a condition from this rule, or make it simpler."
Do not start because you are hungry.  ← "Make this rule say the opposite."
```

**predator.rest: "Listen when you feel hungry."** (peak 25%, lost by t=19500)

```text
Rest when your belly is full.  ← founder
Listen whenever you feel hungry, not just after eating.  ← "Make the rule in this sentence a little weaker."
Listen when you feel hungry.  ← "Remove a condition from this rule, or make it simpler."
```

**predator.mate: "Seek a friend or someone else before aging."** (peak 26%, lost by t=4500)

```text
Seek a partner before growing old.  ← founder
Seek a friend or someone else before aging.  ← "Make the rule in this sentence a little weaker."
```

**predator.mate: "Seek a friend before aging."** (peak 29%, lost by t=6000)

```text
Seek a partner before growing old.  ← founder
Seek a friend or someone else before aging.  ← "Make the rule in this sentence a little weaker."
Seek a friend before aging.  ← "Remove a condition from this rule, or make it simpler."
```

**predator.mate: "Hunt and not mate earlier."** (peak 62%, lost by t=12000)

```text
Hunt first, mate later.  ← founder
Hunt and not mate earlier.  ← "Remove a condition from this rule, or make it simpler."
```

**predator.mate: "Hunt."** (peak 43%, lost by t=11500)

```text
Hunt first, mate later.  ← founder
Hunt.  ← "Remove a condition from this rule, or make it simpler."
```


Leader of each slot over time (a row when it changes):

- **predator.hunt** (2 changes): t=0 "Attack only when prey is close." (29%) → t=1000 "Hunt only when you are hungry." (26%) → t=2500 "Hunt when hungry." (53%)
- **predator.follow** (8 changes): t=0 "No preference." (29%) → t=500 "Stay close to other predators." (50%) → t=1500 "Keep away from other predators." (29%) → … 1 more changes … → t=12500 "When other animals approach them closely, they must not join in." (37%) → t=13000 "When other animals avoid moving towards prey nearby, do not approach them." (35%) → t=13500 "When other animals approach them closely, they must not join in." (56%) → t=19000 "When other animals approach them closely, they shall never join in." (41%) → t=19500 "When other animals approach them closely, they must not join in." (41%)
- **predator.rest** (6 changes): t=0 "Rest when your belly is full." (43%) → t=500 "No preference." (47%) → t=1500 "Rest when your belly is full." (59%) → t=4000 "When you are hungry, rest when your belly is full." (47%) → t=4500 "Rest when your belly is full." (44%) → t=6000 "When your belly is not full, Rest." (55%) → t=10000 "Listen whenever you feel hungry, not just after eating." (59%)
- **predator.mate** (4 changes): t=0 "Hunt first, mate later." (36%) → t=7000 "Hunt and not mate earlier." (62%) → t=8500 "Hunt." (35%) → t=10000 "Hunt and not mate earlier." (35%) → t=10500 "Hunt first, mate later." (50%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 391 | 2.1 | 0% | 53% | 5.2 | 35% | 7.0 | 89% |
| 5+ carriers at once | 71 | 2.2 | 0% | 46% | 5.4 | 32% | 7.2 | 92% |
| alive when mutants were most common (t=9500) | 19 | 2.2 | 0% | 53% | 5.4 | 21% | 7.7 | 89% |
| alive at the end | 11 | 2.4 | 0% | 64% | 4.5 | 27% | 7.2 | 91% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 88% |

115 mutations per 1 000 births; 8 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 6 | 10 (6–14) | 0.972 |
| mutants that reached 90 % | 2 | 2 (1–5) | 0.788 |
| founder texts that reached 90 % | 1 | 2 (0–3) | 0.876 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| predator.hunt | Hunt only when you are hungry. | 32% / 32% / 0.534 | 0% / 3% / 1.000 | 1.09 [0.81–1.37] (84) |
| predator.hunt | Attack only when prey is close. | 34% / 41% / 0.570 | 0% / 6% / 1.000 | 1.02 [0.83–1.21] (227) |
| predator.hunt | Keep chasing until the prey is caught. | 29% / 29% / 1.000 | 0% / 1% / 1.000 | 0.93 [0.58–1.27] (57) |
| predator.hunt | Hunt when hungry. | 100% / 6% / 0.014 | 88% / 3% / 0.020 | 1.03 [0.97–1.09] (2619) |
| predator.hunt | Attack only when prey is not far. | 45% / 11% / 0.016 | 0% / 1% / 1.000 | 1.04 [0.65–1.43] (65) |
| predator.hunt | It may hunt again later. | 32% / 18% / 0.218 | 0% / 3% / 1.000 | 0.95 [0.68–1.22] (93) |
| predator.follow | No preference. | 29% / 29% / 1.000 | 0% / 0% / 1.000 | 1.00 [0.12–1.87] (5) |
| predator.follow | Stay close to other predators. | 50% / 22% / 0.008 | 0% / 0% / 1.000 | 0.83 [0.61–1.05] (124) |
| predator.follow | Keep away from other predators. | 72% / 62% / 0.472 | 0% / 6% / 1.000 | 1.03 [0.89–1.18] (441) |
| predator.follow | Follow others when no prey is in sight. | 28% / 7% / 0.116 | 0% / 0% / 1.000 | 1.07 [0.85–1.30] (161) |
| predator.follow | When other animals move with prey nearby, do not follow. | 88% / 10% / 0.000 | 0% / 1% / 1.000 | 1.02 [0.90–1.13] (652) |
| predator.follow | When other animals approach them closely, they must not join in. | 82% / 2% / 0.000 | 66% / 1% / 0.008 | 1.06 [0.97–1.14] (1026) |
| predator.follow | When other animals avoid moving towards prey nearby, do not approach them. | 35% / 3% / 0.020 | 0% / 1% / 1.000 | 0.97 [0.77–1.17] (213) |
| predator.follow | When other animals approach them closely, they must stay on their own. | 48% / 5% / 0.052 | 28% / 4% / 0.066 | 1.01 [0.82–1.20] (230) |
| predator.follow | When other animals approach them closely, they shall never join in. | 41% / 16% / 0.000 | 0% / 2% / 1.000 | 1.00 [0.68–1.33] (69) |
| predator.rest | No preference. | 50% / 40% / 0.298 | 0% / 2% / 1.000 | 0.94 [0.72–1.16] (123) |
| predator.rest | Rest when your belly is full. | 82% / 64% / 0.234 | 0% / 6% / 1.000 | 1.04 [0.92–1.17] (576) |
| predator.rest | When you are hungry, rest when your belly is full. | 47% / 3% / 0.000 | 0% / 0% / 1.000 | 0.94 [0.69–1.20] (118) |
| predator.rest | Don't stop eating because you are feeling hungry. | 25% / 23% / 0.464 | 0% / 2% / 1.000 | 1.15 [0.49–1.81] (35) |
| predator.rest | When your belly is not full, Rest. | 71% / 12% / 0.044 | 0% / 4% / 1.000 | 1.00 [0.87–1.14] (445) |
| predator.rest | Listen whenever you feel hungry, not just after eating. | 100% / 5% / 0.020 | 81% / 4% / 0.026 | 1.03 [0.96–1.10] (1569) |
| predator.rest | Do not start because you are hungry. | 47% / 9% / 0.060 | 0% / 2% / 1.000 | 1.01 [0.83–1.19] (267) |
| predator.rest | Listen when you feel hungry. | 25% / 10% / 0.172 | 0% / 7% / 1.000 | 1.12 [0.77–1.47] (106) |
| predator.mate | Hunt first, mate later. | 100% / 17% / 0.050 | 97% / 13% / 0.070 | 1.03 [0.97–1.09] (2509) |
| predator.mate | Seek a partner before growing old. | 29% / 29% / 1.000 | 0% / 6% / 1.000 | 1.01 [0.77–1.24] (119) |
| predator.mate | Seek a friend or someone else before aging. | 26% / 25% / 0.472 | 0% / 5% / 1.000 | 1.02 [0.61–1.44] (61) |
| predator.mate | Seek a friend before aging. | 29% / 3% / 0.020 | 0% / 1% / 1.000 | 1.06 [0.72–1.40] (66) |
| predator.mate | Hunt and not mate earlier. | 62% / 20% / 0.060 | 0% / 9% / 1.000 | 0.98 [0.82–1.13] (332) |
| predator.mate | Hunt. | 43% / 5% / 0.024 | 0% / 3% / 1.000 | 1.09 [0.85–1.33] (156) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 14 |
| 2000 | 9 |
| 4000 | 7 |
| 6500 | 7 |
| 8500 | 7 |
| 10500 | 7 |
| 12500 | 7 |
| 14500 | 7 |
| 16500 | 7 |
| 19000 | 7 |
| 21000 | 7 |
| 22817 | 7 |

From tick 2500 on, at least one founder or newcomer is an ancestor of every living animal (family trees mix within a few generations in a small population; genes don't: see the shares below).

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 73 (arrived t=0): 30%
- founder 74 (arrived t=0): 18%
- founder 72 (arrived t=0): 18%
- founder 81 (arrived t=0): 11%
- founder 69 (arrived t=0): 10%
- founder 70 (arrived t=0): 10%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | hunt | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–2000 | 31.6 | 4.41 | 9.61 | 0.82 | 0 | 213 | 74 | 63% | 16% | 2% | 18% |
| 2000–4000 | 32.9 | 4.82 | 8.78 | 1.29 | 0 | 201 | 71 | 57% | 15% | 3% | 24% |
| 4000–6000 | 33.5 | 4.66 | 9.43 | 1.05 | 0 | 221 | 76 | 66% | 12% | 3% | 18% |
| 6000–8000 | 33.1 | 4.95 | 9.21 | 0.98 | 0 | 208 | 76 | 70% | 11% | 5% | 14% |
| 8000–10000 | 33.1 | 5.05 | 8.81 | 1.15 | 0 | 194 | 72 | 67% | 11% | 6% | 17% |
| 10000–12000 | 33.4 | 4.74 | 9.22 | 1.06 | 0 | 220 | 74 | 71% | 10% | 4% | 15% |
| 12000–14000 | 33.2 | 4.71 | 8.16 | 1.40 | 0 | 212 | 70 | 65% | 15% | 6% | 14% |
| 14000–16000 | 33.7 | 4.76 | 8.46 | 1.22 | 0 | 207 | 71 | 68% | 13% | 5% | 14% |
| 16000–18000 | 32.8 | 4.74 | 8.55 | 1.65 | 0 | 211 | 72 | 65% | 15% | 6% | 14% |
| 18000–20000 | 33.3 | 4.88 | 8.66 | 1.25 | 0 | 208 | 71 | 70% | 12% | 5% | 14% |
| 20000–22000 | 29.2 | 5.13 | 8.39 | 2.05 | 0 | 198 | 72 | 63% | 14% | 8% | 15% |
| 22000–22800 | 33.1 | 5.06 | 8.23 | 1.06 | 0 | 197 | 72 | 68% | 12% | 6% | 14% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 4.7 | 0.03 | — |
| 2000 | 99% | 5.5 | 0.14 | — |
| 4000 | 100% | 5.9 | 0.15 | — |
| 6500 | 100% | 5.7 | 0.33 | — |
| 8500 | 99% | 5.9 | 0.17 | — |
| 10500 | 99% | 6.6 | 0.24 | — |
| 12500 | 99% | 6.7 | 0.28 | — |
| 14500 | 98% | 6.7 | 0.34 | — |
| 16500 | 99% | 6.5 | 0.35 | — |
| 19000 | 100% | 6.7 | 0.34 | — |
| 21000 | 100% | 6.5 | 0.27 | — |
| 22817 | 98% | 6.8 | 0.56 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
