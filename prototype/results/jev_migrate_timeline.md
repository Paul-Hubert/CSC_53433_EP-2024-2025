# How the genes developed — jev_migrate

Run: 22817 ticks, 132 generations, 23605 births, 23538 deaths (predators 28%), 3481 mutations, brain `jev`, seed 1234.
Checkpoints every 500 ticks; gene dropping with 500 random inheritances; behaviour per 2000 ticks. Data: `jev_migrate_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 5 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 66 | 0.0 | 66 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.7 | 0.04 |
| 2000 | 122 | 11.0 | 32 | 32% | 0.38 | 6.1 | 97% | 15.0 | 5.5 | 0.09 |
| 4000 | 135 | 22.7 | 32 | 35% | 0.68 | 6.6 | 98% | 10.6 | 2.3 | 0.13 |
| 6500 | 134 | 36.5 | 32 | 52% | 0.74 | 6.4 | 94% | 8.6 | 2.4 | 0.08 |
| 8500 | 135 | 48.3 | 32 | 50% | 0.78 | 6.7 | 99% | 9.4 | 2.3 | 0.22 |
| 10500 | 134 | 59.4 | 32 | 48% | 0.73 | 6.5 | 100% | 8.6 | 2.5 | 0.30 |
| 12500 | 135 | 70.7 | 32 | 57% | 0.83 | 6.9 | 100% | 7.8 | 2.1 | 0.27 |
| 14500 | 135 | 82.3 | 32 | 58% | 0.93 | 7.3 | 100% | 6.6 | 1.9 | 0.45 |
| 16500 | 135 | 93.9 | 32 | 59% | 0.95 | 7.3 | 97% | 6.8 | 1.8 | 0.33 |
| 19000 | 135 | 108.3 | 32 | 63% | 1.13 | 7.5 | 91% | 7.6 | 2.2 | 0.34 |
| 21000 | 133 | 120.2 | 32 | 62% | 1.12 | 6.8 | 99% | 8.6 | 2.1 | 0.28 |
| 22817 | 135 | 130.9 | 32 | 54% | 1.06 | 6.5 | 99% | 9.8 | 2.2 | 0.30 |

Slot by slot at the end (tick 22817):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| eat | 8 | 1.2 | Only look for food when energy is low. | 92% | 8% | 0.08 | 100% |
| flee | 8 | 2.3 | If you see any wild animals, do so immediately. | 51% | 100% | 3.08 | 100% |
| follow | 10 | 3.1 | Keep your distance from other animals. | 46% | 54% | 0.56 | 98% |
| rest | 7 | 1.1 | Rest when you are tired. | 93% | 7% | 0.10 | 100% |
| mate | 16 | 3.1 | Mate whenever food is plentiful. | 53% | 100% | 1.50 | 99% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| eat | Eat whenever food is close. | founder text | 0 | 500 | — | — | 33% (t=500) | lost by t=5000 |
| eat | Only look for food when energy is low. | founder text | 0 | 2000 | 3500 | 8000 | 99% (t=18000) | present, 92% |
| flee | Run from any predator you see. | founder text | 0 | 500 | 2500 | — | 84% (t=4000) | lost by t=7500 |
| flee | Run away from anything that attacks you. | founder text | 0 | 0 | — | — | 26% (t=0) | lost by t=2500 |
| flee | Run into any wild animal you encounter. | mutant, depth 1 | 2863 | 2137 | 2637 | — | 88% (t=10000) | lost by t=15500 |
| flee | If you see any wild animals, run into them immediately. | mutant, depth 2 | 7127 | 3873 | 6373 | 7873 | 97% (t=15500) | lost by t=22000 |
| flee | If you see any wild animals. | mutant, depth 3 | 17052 | 5448 | — | — | 41% (t=22817) | present, 41% |
| flee | If you see any wild animals, do so immediately. | mutant, depth 3 | 17124 | 1376 | 1876 | — | 83% (t=21500) | present, 51% |
| follow | Stay close to other animals. | founder text | 0 | 500 | — | — | 29% (t=500) | lost by t=12500 |
| follow | Follow others when you are lost or hungry. | founder text | 0 | 500 | — | — | 30% (t=1000) | lost by t=2500 |
| follow | Keep your distance from other animals. | founder text | 0 | 0 | 2500 | 11000 | 97% (t=12500) | present, 46% |
| follow | Avoid other animals. | mutant, depth 1 | 2358 | 3142 | — | — | 25% (t=5500) | lost by t=7500 |
| follow | Don't get too close to other wildlife. | mutant, depth 1 | 16470 | 3030 | — | — | 38% (t=21000) | present, 21% |
| follow | Do not enter the territory of other species within 50 meters. | mutant, depth 2 | 17204 | 796 | — | — | 49% (t=18500) | lost by t=21000 |
| follow | Do not be near other animals. | mutant, depth 1 | 20098 | 2719 | — | — | 25% (t=22817) | present, 25% |
| rest | No preference. | founder text | 0 | 500 | 11000 | — | 58% (t=11000) | lost by t=14500 |
| rest | Rest when you are tired. | founder text | 0 | 500 | 6500 | 22817 | 93% (t=22817) | present, 93% |
| rest | Rest only when you feel safe. | founder text | 0 | 0 | — | — | 27% (t=0) | lost by t=3500 |
| rest | Your choice of food might not affect your survival. | mutant, depth 1 | 8510 | 3990 | 5990 | — | 64% (t=15000) | lost by t=22817 |
| mate | No preference. | founder text | 0 | 500 | — | — | 29% (t=500) | lost by t=3000 |
| mate | Look for a partner when energy is high. | founder text | 0 | 2500 | — | — | 29% (t=2500) | lost by t=4500 |
| mate | Mate only when food is plentiful. | founder text | 0 | 3000 | — | — | 31% (t=3000) | lost by t=6500 |
| mate | Mate whenever food is plentiful. | mutant, depth 1 | 846 | 5154 | 5654 | — | 74% (t=20500) | present, 53% |
| mate | The preference must no longer be considered objective or fixed. | mutant, depth 3 | 1455 | 2045 | 2545 | — | 69% (t=4000) | lost by t=10500 |
| mate | Never mate when food is scarce. | mutant, depth 2 | 6065 | 5435 | — | — | 44% (t=12000) | lost by t=21500 |
| mate | If food is not plentiful, never mate. | mutant, depth 2 | 6768 | 1732 | — | — | 42% (t=9500) | lost by t=12000 |
| mate | Mate whenever meat is plentiful. | mutant, depth 2 | 10182 | 2318 | 2818 | — | 59% (t=13000) | lost by t=19500 |

Lineages of the mutants that swept:

**flee: "Run into any wild animal you encounter."** (peak 88%, lost by t=15500)

```text
Run from any predator you see.  ← founder
Run into any wild animal you encounter.  ← "Make this rule say the opposite."
```

**flee: "If you see any wild animals, run into them immediately."** (peak 97%, lost by t=22000)

```text
Run from any predator you see.  ← founder
Run into any wild animal you encounter.  ← "Make this rule say the opposite."
If you see any wild animals, run into them immediately.  ← "Change when this rule applies."
```

**flee: "If you see any wild animals."** (peak 41%, present, 41%)

```text
Run from any predator you see.  ← founder
Run into any wild animal you encounter.  ← "Make this rule say the opposite."
If you see any wild animals, run into them immediately.  ← "Change when this rule applies."
If you see any wild animals.  ← "Remove a condition from this rule, or make it simpler."
```

**flee: "If you see any wild animals, do so immediately."** (peak 83%, present, 51%)

```text
Run from any predator you see.  ← founder
Run into any wild animal you encounter.  ← "Make this rule say the opposite."
If you see any wild animals, run into them immediately.  ← "Change when this rule applies."
If you see any wild animals, do so immediately.  ← "Remove a condition from this rule, or make it simpler."
```

**follow: "Avoid other animals."** (peak 25%, lost by t=7500)

```text
Keep your distance from other animals.  ← founder
Avoid other animals.  ← "Remove a condition from this rule, or make it simpler."
```

**follow: "Don't get too close to other wildlife."** (peak 38%, present, 21%)

```text
Keep your distance from other animals.  ← founder
Don't get too close to other wildlife.  ← "Change one word of this rule into a related word."
```

**follow: "Do not enter the territory of other species within 50 meters."** (peak 49%, lost by t=21000)

```text
Keep your distance from other animals.  ← founder
Keep your range from other animals.  ← "Change how near, how far or how much this rule is about."
Do not enter the territory of other species within 50 meters.  ← "Add a short condition to this rule."
```

**follow: "Do not be near other animals."** (peak 25%, present, 25%)

```text
Keep your distance from other animals.  ← founder
Do not be near other animals.  ← "Make this rule say the opposite."
```

**rest: "Your choice of food might not affect your survival."** (peak 64%, lost by t=22817)

```text
No preference.  ← neutral
Your choice of food might not affect your survival.  ← "Make the rule in this sentence a little weaker."
```

**mate: "Mate whenever food is plentiful."** (peak 74%, present, 53%)

```text
Mate only when food is plentiful.  ← founder
Mate whenever food is plentiful.  ← "Remove a condition from this rule, or make it simpler."
```

**mate: "The preference must no longer be considered objective or fixed."** (peak 69%, lost by t=10500)

```text
No preference.  ← neutral
The preference must remain absolute.  ← "Change one word of this rule into a related word."
The preference must become subjective.  ← "Change one word of this rule into a related word."
The preference must no longer be considered objective or fixed.  ← "Make this rule say the opposite."
```

**mate: "Never mate when food is scarce."** (peak 44%, lost by t=21500)

```text
Mate only when food is plentiful.  ← founder
Mate whenever food is plentiful.  ← "Remove a condition from this rule, or make it simpler."
Never mate when food is scarce.  ← "Make this rule say the opposite."
```

**mate: "If food is not plentiful, never mate."** (peak 42%, lost by t=12000)

```text
Mate only when food is plentiful.  ← founder
Mate whenever food is plentiful.  ← "Remove a condition from this rule, or make it simpler."
If food is not plentiful, never mate.  ← "Make this rule say the opposite."
```

**mate: "Mate whenever meat is plentiful."** (peak 59%, lost by t=19500)

```text
Mate only when food is plentiful.  ← founder
Mate whenever food is plentiful.  ← "Remove a condition from this rule, or make it simpler."
Mate whenever meat is plentiful.  ← "Change one word of this rule into a related word."
```


Leader of each slot over time (a row when it changes):

- **eat** (2 changes): t=0 "Always finish eating before doing anything else." (24%) → t=500 "Eat whenever food is close." (33%) → t=1500 "Only look for food when energy is low." (21%)
- **flee** (6 changes): t=0 "Run away from anything that attacks you." (26%) → t=500 "Run from any predator you see." (35%) → t=5500 "Run into any wild animal you encounter." (75%) → t=12000 "If you see any wild animals, run into them immediately." (49%) → t=12500 "Run into any wild animal you encounter." (67%) → t=13500 "If you see any wild animals, run into them immediately." (64%) → t=19000 "If you see any wild animals, do so immediately." (59%)
- **follow** (5 changes): t=0 "Keep your distance from other animals." (35%) → t=500 "Follow others when you are lost or hungry." (30%) → t=1000 "Keep your distance from other animals." (37%) → t=18500 "Do not enter the territory of other species within 50 meters." (49%) → t=20000 "Don't get too close to other wildlife." (30%) → t=20500 "Keep your distance from other animals." (48%)
- **rest** (12 changes): t=0 "Rest only when you feel safe." (27%) → t=500 "No preference." (33%) → t=2500 "Rest when you are tiring." (24%) → … 5 more changes … → t=13500 "Rest when you are tired." (48%) → t=14500 "Your choice of food might not affect your survival." (55%) → t=17500 "Rest when you are tired." (47%) → t=18000 "Your choice of food might not affect your survival." (43%) → t=18500 "Rest when you are tired." (51%)
- **mate** (14 changes): t=0 "Seek a partner before growing old." (24%) → t=500 "No preference." (29%) → t=1500 "Look for a partner when energy is high." (21%) → … 7 more changes … → t=11000 "Never mate when food is scarce." (24%) → t=12500 "Mate whenever meat is plentiful." (41%) → t=13500 "Mate whenever food is plentiful." (52%) → t=14500 "Mate whenever meat is plentiful." (38%) → t=16500 "Mate whenever food is plentiful." (58%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 2956 | 1.8 | 0% | 58% | 5.7 | 37% | 7.8 | 86% |
| 5+ carriers at once | 397 | 1.8 | 0% | 58% | 5.4 | 35% | 7.6 | 88% |
| alive when mutants were most common (t=18500) | 43 | 1.9 | 0% | 49% | 4.9 | 37% | 7.8 | 88% |
| alive at the end | 46 | 2.0 | 0% | 52% | 5.4 | 35% | 7.3 | 96% |
| mutation test (no selection, T = 1.2) | 144 | 1.0 | 0% | 58% | 4.8 | 35% | 6.7 | 92% |

147 mutations per 1 000 births; 8 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 7 | 9 (6–13) | 0.892 |
| mutants that reached 90 % | 1 | 1 (0–3) | 0.816 |
| founder texts that reached 90 % | 3 | 1 (0–3) | 0.074 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| eat | Eat whenever food is close. | 33% / 26% / 0.110 | 0% / 5% / 1.000 | 1.01 [0.88–1.13] (934) |
| eat | Only look for food when energy is low. | 99% / 8% / 0.000 | 92% / 5% / 0.000 | 1.03 [1.00–1.06] (18354) |
| flee | Run from any predator you see. | 84% / 17% / 0.008 | 0% / 2% / 1.000 | 1.02 [0.94–1.09] (3372) |
| flee | Run away from anything that attacks you. | 26% / 26% / 1.000 | 0% / 3% / 1.000 | 1.03 [0.77–1.30] (196) |
| flee | Run into any wild animal you encounter. | 88% / 5% / 0.000 | 0% / 2% / 1.000 | 1.04 [0.98–1.09] (6197) |
| flee | If you see any wild animals, run into them immediately. | 97% / 6% / 0.004 | 0% / 3% / 1.000 | 1.03 [0.97–1.08] (6445) |
| flee | If you see any wild animals. | 41% / 10% / 0.024 | 41% / 10% / 0.024 | 1.08 [0.82–1.35] (289) |
| flee | If you see any wild animals, do so immediately. | 83% / 3% / 0.000 | 51% / 2% / 0.014 | 1.03 [0.94–1.12] (2755) |
| follow | Stay close to other animals. | 29% / 27% / 0.416 | 0% / 1% / 1.000 | 1.00 [0.88–1.13] (954) |
| follow | Follow others when you are lost or hungry. | 30% / 25% / 0.296 | 0% / 2% / 1.000 | 1.00 [0.84–1.16] (481) |
| follow | Keep your distance from other animals. | 97% / 8% / 0.000 | 46% / 3% / 0.040 | 1.04 [1.00–1.07] (15408) |
| follow | Avoid other animals. | 25% / 2% / 0.030 | 0% / 0% / 1.000 | 1.00 [0.81–1.20] (539) |
| follow | Don't get too close to other wildlife. | 38% / 2% / 0.016 | 21% / 1% / 0.028 | 1.00 [0.86–1.14] (927) |
| follow | Do not enter the territory of other species within 50 meters. | 49% / 13% / 0.008 | 0% / 8% / 1.000 | 1.04 [0.89–1.19] (1084) |
| follow | Do not be near other animals. | 25% / 6% / 0.088 | 25% / 6% / 0.088 | 1.00 [0.72–1.28] (265) |
| rest | No preference. | 58% / 5% / 0.034 | 0% / 2% / 1.000 | 1.04 [0.97–1.11] (4089) |
| rest | Rest when you are tired. | 93% / 2% / 0.000 | 93% / 2% / 0.000 | 1.03 [0.98–1.08] (9203) |
| rest | Rest only when you feel safe. | 27% / 27% / 1.000 | 0% / 1% / 1.000 | 1.01 [0.86–1.16] (516) |
| rest | Your choice of food might not affect your survival. | 64% / 3% / 0.008 | 0% / 2% / 1.000 | 1.03 [0.97–1.10] (4601) |
| mate | No preference. | 29% / 29% / 0.560 | 0% / 1% / 1.000 | 1.01 [0.84–1.19] (446) |
| mate | Look for a partner when energy is high. | 29% / 10% / 0.118 | 0% / 1% / 1.000 | 1.04 [0.89–1.18] (779) |
| mate | Mate only when food is plentiful. | 31% / 8% / 0.084 | 0% / 1% / 1.000 | 1.02 [0.87–1.16] (763) |
| mate | Mate whenever food is plentiful. | 74% / 0% / 0.004 | 53% / 0% / 0.004 | 1.04 [0.99–1.08] (8224) |
| mate | The preference must no longer be considered objective or fixed. | 69% / 7% / 0.000 | 0% / 0% / 1.000 | 1.03 [0.94–1.12] (2520) |
| mate | Never mate when food is scarce. | 44% / 3% / 0.010 | 0% / 5% / 1.000 | 1.00 [0.87–1.14] (1136) |
| mate | If food is not plentiful, never mate. | 42% / 5% / 0.020 | 0% / 1% / 1.000 | 1.02 [0.87–1.16] (991) |
| mate | Mate whenever meat is plentiful. | 59% / 5% / 0.008 | 0% / 1% / 1.000 | 1.03 [0.94–1.12] (2429) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 66 |
| 2000 | 32 |
| 4000 | 32 |
| 6500 | 32 |
| 8500 | 32 |
| 10500 | 32 |
| 12500 | 32 |
| 14500 | 32 |
| 16500 | 32 |
| 19000 | 32 |
| 21000 | 32 |
| 22817 | 32 |

From tick 1500 on, at least one founder or newcomer is an ancestor of every living animal (family trees mix within a few generations in a small population; genes don't: see the shares below).

Expected share of the living animals' genes coming from each family at the end (top 6):

- founder 50 (arrived t=0): 11%
- founder 9 (arrived t=0): 8%
- founder 40 (arrived t=0): 8%
- founder 46 (arrived t=0): 6%
- founder 17 (arrived t=0): 6%
- founder 16 (arrived t=0): 5%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | eat | flee | follow | rest | mate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–2000 | 125.0 | 7.29 | 2.43 | 1.39 | 0 | 134 | 77 | 57% | 7% | 12% | 7% | 17% |
| 2000–4000 | 133.8 | 7.71 | 2.16 | 1.18 | 0 | 131 | 74 | 53% | 6% | 14% | 8% | 18% |
| 4000–6000 | 133.7 | 7.98 | 2.36 | 0.76 | 0 | 126 | 78 | 48% | 11% | 15% | 7% | 20% |
| 6000–8000 | 133.6 | 7.84 | 2.28 | 1.12 | 0 | 127 | 74 | 45% | 14% | 15% | 7% | 19% |
| 8000–10000 | 133.4 | 8.06 | 2.19 | 0.97 | 0 | 126 | 75 | 45% | 14% | 16% | 6% | 19% |
| 10000–12000 | 132.5 | 7.85 | 2.32 | 1.03 | 0 | 128 | 73 | 42% | 17% | 17% | 5% | 19% |
| 12000–14000 | 133.4 | 7.90 | 2.03 | 1.03 | 0 | 129 | 72 | 41% | 16% | 17% | 6% | 19% |
| 14000–16000 | 131.8 | 7.50 | 2.16 | 1.76 | 0 | 134 | 67 | 36% | 21% | 18% | 6% | 18% |
| 16000–18000 | 132.8 | 7.99 | 2.11 | 1.10 | 0 | 126 | 71 | 37% | 18% | 20% | 6% | 19% |
| 18000–20000 | 133.6 | 8.22 | 2.16 | 1.04 | 0 | 124 | 71 | 37% | 17% | 22% | 5% | 19% |
| 20000–22000 | 130.9 | 7.70 | 1.88 | 1.39 | 0 | 131 | 72 | 39% | 18% | 18% | 6% | 19% |
| 22000–22800 | 133.0 | 7.73 | 2.05 | 1.45 | 0 | 130 | 72 | 41% | 18% | 16% | 6% | 19% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 5.5 | 0.04 | — |
| 2000 | 97% | 6.1 | 0.09 | — |
| 4000 | 98% | 6.6 | 0.13 | — |
| 6500 | 94% | 6.4 | 0.08 | — |
| 8500 | 99% | 6.7 | 0.22 | — |
| 10500 | 100% | 6.5 | 0.30 | — |
| 12500 | 100% | 6.9 | 0.27 | — |
| 14500 | 100% | 7.3 | 0.45 | — |
| 16500 | 97% | 7.3 | 0.33 | — |
| 19000 | 91% | 7.5 | 0.34 | — |
| 21000 | 99% | 6.8 | 0.28 | — |
| 22817 | 99% | 6.5 | 0.30 | — |

No judge in this report (run with --judge after the run, when the GPU is free).
