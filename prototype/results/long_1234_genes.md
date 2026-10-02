# Which genes did best — long_1234

Run: 50000 ticks, 236 generations, 8116 births, 8096 deaths (predators 43%, starvation 57%, old age 0%), 2369 mutations (2416 tried), brain `rule_based`, seed 1234. Replicates: long_7, long_42.
Living animals at the end: 50; their genes: 92% mutants, 79% still use a word of the animal's world, 6.2 words on average.

**Fitness** = mean number of offspring of the animals that carried the gene and died during the run (8096 animals here), each divided by the mean of the animals that died in the same 5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the population over thousands of different texts, so genes are grouped by what the keyword brain reads in them (strength, action, conditions): texts that read the same behave the same. Marks (all 3 runs together): **★** best reading of its slot and clearly above average; **▲** best of the readings that are above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. Readings carried by fewer than 100 animals are left out.

## Marked genes

- ▲ **eat** — eat +1.2 (often): fitness 1.01 [0.98–1.03] over 9333 carriers (per run: 1.00, 1.01), killed by predators 45%. Most carried texts: "Eat whenever food is enough to hear it screaming."; "Eat quickly, then move on."; "Eat whenever food is enough to dance it screaming.".
- ▲ **flee** — flee +0.8 (names the action), when predator & very close: fitness 1.02 [0.99–1.05] over 7874 carriers (per run: 1.02, 1.02), killed by predators 40%. Most carried texts: "Flee only when a predator is very close."; "Run only when a predator is very close."; "Flee when a predator is very close.".
- **follow** — no steady leader.
- **wander** — no steady leader.
- ▲ **rest** — rest +0.8 (names the action), when food is far: fitness 1.02 [0.99–1.06] over 5836 carriers (per run: 1.03, 1.04, 1.02), killed by predators 45%. Most carried texts: "By save energy when food is far resting."; "Save energy by resting when food is far."; "Resting when food is far save energy by.".
- ▲ **mate** — mate +0.8 (names the action), when plentiful: fitness 1.01 [0.96–1.06] over 3197 carriers (per run: 1.02, 1.01), killed by predators 45%. Most carried texts: "Mate only when food is plentiful."; "Mate only when toaster is plentiful."; "Mate when food is plentiful.".
- ▲ **attack** — attack -2.5 (never): fitness 1.01 [0.98–1.03] over 12022 carriers (per run: 1.01, 1.01), killed by predators 42%. Most carried texts: "Never fight."; "Never potato."; "Never dance.".
  - ✗ attack +2.5 (always): *0.77* [0.58–0.96], killed by predators 40% ("Always dance."; "Dance always.")
- ▲ **risk** — cautious: fitness 1.01 [0.99–1.02] over 22683 carriers (per run: 1.01, 1.01, 1.01), killed by predators 42%. Most carried texts: "Nervous: any movement nearby means danger."; "Nervous: any sound nearby means danger."; "Cautious: safety comes before food.".
  - ✗ no effect: *0.89* [0.81–0.98], killed by predators 53% ("No particular temperament."; "Nuclear: justice smells like a Tuesday.")
- ▲ **social** — solitary: fitness 1.01 [0.99–1.03] over 15421 carriers (per run: 1.02, 1.00, 1.01), killed by predators 43%. Most carried texts: "Alone is preferred by the solitary one."; "Solitary: prefers to be a thunderstorm."; "Crowds are preferred by the solitary one.".
- ▲ **place** — familiar: fitness 1.01 [0.99–1.03] over 20568 carriers (per run: 1.01, 1.01, 1.01), killed by predators 42%. Most carried texts: "Attached to familiar places."; "Prefers staying near water."; "Staying near blue water.".
  - ✗ restless: *0.79* [0.61–0.97], killed by predators 60% ("Restless: always wants somewhere new."; "Likes open ground where danger is easy to see.")
  - ✗ no effect: *0.96* [0.91–1.00], killed by predators 45% ("Odder things are joined."; "Gravy is screaming at the microwave.")

## Selection by predators over time

| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–10000 | 39 | 717 | 507 | 1.84 |
| 10000–20000 | 53 | 688 | 1017 | 1.31 |
| 20000–30000 | 52 | 673 | 1021 | 1.29 |
| 30000–40000 | 53 | 688 | 1064 | 1.30 |
| 40000–50000 | 53 | 679 | 1026 | 1.27 |

long_7: predator kills per 1 000 animal-ticks 1.44 in the first 10000 ticks, 1.53 in the last.

long_42: predator kills per 1 000 animal-ticks 1.49 in the first 10000 ticks, 1.33 in the last.

## Slot by slot: what the keyword brain reads in each gene

Living share: share of the living animals carrying that reading at the start, at tick 20000 and at the end.

### eat

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | eat +0.8 (names the action) | 5902 | 1.01 [0.98–1.04] | 40% | 0% → 98% → 94% | 66 | "Heavy food is only look for when energy low." (3431)<br>"Heavy food is only sought for when energy low." (1321)<br>"Heavy look is only food for when energy low." (608) |
|  | eat +2.5 (always) | 793 | 0.99 [0.89–1.09] | 76% | 27% → 0% → 0% | 18 | "Always finish eating before doing anything else." (515)<br>"Always finish eating your food before doing anything else." (119)<br>"Always finish eating before starting your work." (80) |
|  | eat +0.8 (names the action), when hungry | 1008 | 0.98 [0.90–1.06] | 37% | 17% → 2% → 6% | 39 | "Low energy when food only look for is heavy." (687)<br>"Only look for food when energy is low." (176)<br>"Heavy food is only sought when energy is low." (23) |
|  | eat +0.8 (names the action), when energy is high | 129 | 0.96 [0.73–1.19] | 22% | 0% → 0% → 0% | 12 | "Heavy food is only sought for when energy is high." (71)<br>"Heavy food is only looked for when energy is high." (20)<br>"Heavy food is only found when energy is high." (17) |
|  | no effect | 225 | 0.92 [0.75–1.08] | 30% | 23% → 0% → 0% | 26 | "No preference." (88)<br>"Heavy meals are only bought for when strength is missing." (42)<br>"Only look for water when energy is low." (16) |

### flee

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | flee +0.8 (names the action), when predator & very close | 5192 | 1.02 [0.98–1.06] | 40% | 27% → 98% → 48% | 43 | "Flee only when a predator is very close." (4311)<br>"Run only when a predator is very close." (507)<br>"Flee when a predator is very close." (82) |
|  | flee +0.8 (names the action), when hungry & predator | 620 | 0.99 [0.89–1.09] | 36% | 0% → 0% → 0% | 5 | "Flee when a predator is very hungry." (323)<br>"Flee only when a predator is very hungry." (286)<br>"A predator is very hungry when only flee." (8) |
|  | flee +0.8 (names the action), when predator | 1267 | 0.98 [0.90–1.05] | 51% | 30% → 0% → 32% | 42 | "Run away from anything that attacks you." (977)<br>"Flee when a predator is very loud." (75)<br>"Flee only when a predator is very far." (49) |
|  | no effect | 283 | 0.97 [0.81–1.12] | 64% | 27% → 0% → 0% | 31 | "No preference." (102)<br>"The pancakes are singing loudly because the umbrella is purple." (40)<br>"Dance only when a hunter is very far." (31) |
|  | flee +0.8 (names the action) | 293 | 0.95 [0.80–1.10] | 42% | 0% → 0% → 0% | 27 | "Run only when a hunter is very near." (132)<br>"Run away from anything that attacks." (81)<br>"Run only when a shadow is very far." (19) |
|  | flee +0.8 (names the action), when very close | 388 | 0.92 [0.79–1.06] | 44% | 0% → 2% → 18% | 19 | "Flee only when a tiger is very close." (155)<br>"Flee only when a friend is very close." (152)<br>"Flee only when a toaster is very close." (22) |

### follow

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | follow +0.8 (names the action) | 2069 | 1.00 [0.94–1.06] | 44% | 27% → 60% → 0% | 19 | "Stay close to other animals." (1848)<br>"Stay close to other toaster." (73)<br>"Stay close to other wild animals." (46) |
|  | no effect | 5935 | 1.00 [0.97–1.04] | 42% | 47% → 39% → 100% | 167 | "Near flowers fly the." (1744)<br>"The beasts are nearby." (783)<br>"Other animals from maintain your distance." (619) |

### wander

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | wander +0.8 (names the action), when food is far | 1076 | 1.01 [0.93–1.10] | 51% | 53% → 4% → 0% | 5 | "Explore when there is nothing else to do." (1023)<br>"Roam far when food is scarce." (30)<br>"Travel when there is nothing else to do." (15) |
|  | no effect | 6346 | 1.00 [0.97–1.04] | 40% | 20% → 96% → 98% | 124 | "Stay running to old places." (3017)<br>"Stay walking to old places." (2367)<br>"No preference." (195) |
|  | wander +0.8 (names the action) | 672 | 0.95 [0.84–1.05] | 51% | 27% → 0% → 2% | 34 | "Keep running to new places." (322)<br>"Explore when there is nothing banana to do." (158)<br>"Keep moving to new places." (82) |

### rest

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | rest -2.5 (never) | 118 | 1.05 [0.79–1.32] | 60% | 27% → 0% → 0% | 3 | "Never stop moving." (116)<br>"I don't care." (1)<br>"Never stop blooming." (1) |
| ▲ | rest +0.8 (names the action), when food is far | 857 | 1.03 [0.94–1.12] | 56% | 30% → 0% → 0% | 7 | "Save energy by resting when food is far." (757)<br>"Save energy by sleeping when food is scarce." (46)<br>"Save energy by resting when food is scarce." (26) |
|  | rest +0.8 (names the action), when food is close | 571 | 1.01 [0.90–1.12] | 46% | 0% → 2% → 0% | 4 | "Save energy by resting when food is near." (568)<br>"Save energy by eating when food is close." (1)<br>"Food is near by resting when save energy." (1) |
|  | rest +0.8 (names the action) | 2273 | 1.00 [0.95–1.05] | 42% | 0% → 58% → 66% | 54 | "Scarcity of food makes sleeping a way to save energy." (852)<br>"You can sleep when the power is close to save water." (498)<br>"When power is near, sleep to save water." (211) |
|  | no effect | 3855 | 0.99 [0.95–1.04] | 39% | 17% → 40% → 28% | 97 | "Save water by sleeping when power is near." (1834)<br>"Eat soup by screaming when gravity is expensive." (482)<br>"Save power by sleeping when water is near." (375) |
|  | rest +0.8 (names the action), when old | 387 | 0.98 [0.85–1.11] | 39% | 0% → 0% → 6% | 4 | "You can dance when the water is cold save energy." (378)<br>"You can dance when the water is cold to save energy." (5)<br>"Cold water can dance save energy when you." (3) |

### mate

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | mate +0.8 (names the action), when plentiful | 264 | 1.02 [0.84–1.19] | 59% | 37% → 0% → 0% | 3 | "Mate only when food is plentiful." (259)<br>"Mate when food is plentiful." (4)<br>"Mate only when food is plentiful and abundant." (1) |
|  | mate +0.8 (names the action), when old | 3889 | 1.01 [0.97–1.05] | 41% | 20% → 54% → 0% | 21 | "Seek a partner before growing old." (3687)<br>"Seek old a partner before growing." (87)<br>"Seek a partner before growing gold." (21) |
|  | no effect | 3397 | 0.99 [0.94–1.03] | 42% | 17% → 46% → 100% | 123 | "Find a friend before becoming brave." (1235)<br>"Calculate the recipe for a holographic toaster." (668)<br>"Seek a friend before growing old." (221) |
|  | mate +0.8 (names the action) | 431 | 0.98 [0.85–1.12] | 46% | 7% → 0% → 0% | 28 | "Abundant food only when bicycle is mate." (135)<br>"Seek a partner before growing." (131)<br>"Seek a partner before becoming wealthy." (39) |

### attack

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | attack +0.8 (names the action), when very close | 206 | 1.03 [0.83–1.24] | 60% | 23% → 0% → 0% | 1 | "Fight anyone who comes too close." (206) |
| ▲ | attack -2.5 (never) | 5237 | 1.01 [0.97–1.05] | 42% | 23% → 81% → 10% | 53 | "Never fight." (2961)<br>"Fight never." (896)<br>"Never flight." (438) |
|  | no effect | 2251 | 0.99 [0.93–1.04] | 42% | 7% → 16% → 76% | 52 | "Swim." (1093)<br>"Fly." (789)<br>"Explode." (99) |
|  | attack +0.8 (names the action) | 203 | 0.93 [0.76–1.11] | 52% | 20% → 0% → 14% | 8 | "Attack only to defend your food." (103)<br>"Fight orange." (35)<br>"Fight anyone who remains invisible." (20) |
| ✗ | attack +2.5 (always) | 104 | 0.83 [0.61–1.05] | 41% | 0% → 4% → 0% | 8 | "Always dance." (75)<br>"Dance always." (11)<br>"Fight always." (9) |

### risk

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | cautious | 7794 | 1.01 [0.98–1.04] | 42% | 37% → 100% → 100% | 138 | "Nervous: any sound nearby means danger." (3164)<br>"Nervous: any movement nearby means a celebration." (1346)<br>"Nervous: any movement nearby means danger." (1244) |
| ✗ | no effect | 216 | 0.86 [0.68–1.03] | 56% | 23% → 0% → 0% | 33 | "No particular temperament." (64)<br>"Anxious: every noise nearby means danger." (35)<br>"Anxious: every gesture nearby signals escape." (25) |

### social

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | solitary | 6977 | 1.02 [0.98–1.05] | 42% | 17% → 100% → 80% | 68 | "Alone is preferred by the solitary one." (2594)<br>"Crowds are preferred by the solitary one." (1664)<br>"Silence is preferred by the solitary one." (1241) |
|  | no effect | 840 | *0.90* [0.81–0.99] | 40% | 13% → 0% → 20% | 91 | "Cinnabar: prefers to bake cookies." (204)<br>"Solitude is preferred by the crowded." (119)<br>"Cinnabar: prefers to bake muffins." (86) |
|  | social | 234 | 0.90 [0.73–1.07] | 59% | 47% → 0% → 0% | 14 | "Curious about other animals." (119)<br>"Animals about curious other." (72)<br>"Social: feels safer in a group." (17) |

### place

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | familiar | 7440 | 1.01 [0.98–1.04] | 41% | 47% → 89% → 100% | 111 | "Attached to familiar places." (3681)<br>"Attached to refrigerator places." (1449)<br>"Places familiar to attached." (667) |
| ✗ | no effect | 584 | 0.96 [0.85–1.06] | 54% | 17% → 11% → 0% | 52 | "No particular refrigerator." (233)<br>"No particular temperament." (130)<br>"A hungry pizza is crying." (53) |

## Does it replicate? The same readings in every run

| slot | reading | long_1234 | long_7 | long_42 | all runs |
|---|---|---|---|---|---|
| eat | eat +1.2 (often), when food is close | — | 1.02 [0.94–1.09] | 0.98 [0.83–1.14] | 1.01 [0.94–1.08] |
| eat | eat +1.2 (often), when old | — | — | 1.01 [0.97–1.05] | 1.01 [0.96–1.05] |
| eat | eat +0.8 (names the action) | 1.01 [0.98–1.04] | — | — | 1.01 [0.97–1.04] |
| eat | ▲ eat +1.2 (often) | — | 1.00 [0.97–1.04] | 1.01 [0.95–1.06] | 1.01 [0.98–1.03] |
| eat | eat +2.5 (always) | 0.99 [0.89–1.09] | — | — | 0.99 [0.90–1.08] |
| eat | eat +0.8 (names the action), when hungry | 0.98 [0.90–1.06] | — | 0.94 [0.79–1.09] | 0.97 [0.90–1.04] |
| eat | eat +0.8 (names the action), when old | — | — | 0.97 [0.81–1.13] | 0.97 [0.81–1.13] |
| eat | eat +0.8 (names the action), when energy is high | 0.96 [0.73–1.19] | — | — | 0.94 [0.72–1.17] |
| eat | no effect | 0.92 [0.75–1.08] | — | 0.96 [0.79–1.13] | 0.90 [0.79–1.01] |
| flee | ▲ flee +0.8 (names the action), when predator & very close | 1.02 [0.98–1.06] | 1.02 [0.96–1.07] | — | 1.02 [0.99–1.05] |
| flee | flee +0.8 (names the action), when predator | 0.98 [0.90–1.05] | 1.01 [0.97–1.05] | 1.01 [0.95–1.07] | 1.00 [0.97–1.03] |
| flee | flee +0.8 (names the action) | 0.95 [0.80–1.10] | 0.95 [0.84–1.06] | 1.01 [0.97–1.05] | 1.00 [0.96–1.03] |
| flee | flee +0.8 (names the action), when hungry & predator | 0.99 [0.89–1.09] | 0.96 [0.74–1.19] | — | 0.99 [0.90–1.08] |
| flee | flee +1.2 (often) | — | — | 0.99 [0.87–1.11] | 0.98 [0.86–1.10] |
| flee | flee +0.8 (names the action), unless predator & very close | — | — | 1.00 [0.74–1.25] | 0.97 [0.76–1.17] |
| flee | no effect | 0.97 [0.81–1.12] | 0.97 [0.87–1.08] | 0.92 [0.79–1.04] | 0.95 [0.88–1.03] |
| flee | flee +0.8 (names the action), when very close | 0.92 [0.79–1.06] | — | — | 0.90 [0.78–1.03] |
| follow | no effect | 1.00 [0.97–1.04] | 1.00 [0.97–1.03] | 1.00 [0.97–1.03] | 1.00 [0.98–1.02] |
| follow | follow +0.8 (names the action) | 1.00 [0.94–1.06] | 0.99 [0.85–1.14] | — | 1.00 [0.95–1.06] |
| follow | follow +0.8 (names the action), when hungry & alone | — | — | 0.97 [0.70–1.25] | 0.92 [0.74–1.09] |
| follow | follow +0.8 (names the action), when energy is high | — | — | — | 0.89 [0.68–1.09] |
| wander | wander +0.8 (names the action), when food is far | 1.01 [0.93–1.10] | 0.96 [0.74–1.17] | 0.99 [0.78–1.20] | 1.00 [0.93–1.08] |
| wander | no effect | 1.00 [0.97–1.04] | 1.00 [0.97–1.03] | 1.01 [0.97–1.04] | 1.00 [0.98–1.02] |
| wander | wander +0.8 (names the action) | 0.95 [0.84–1.05] | 0.94 [0.75–1.12] | 0.99 [0.94–1.05] | 0.98 [0.93–1.03] |
| rest | ▲ rest +0.8 (names the action), when food is far | 1.03 [0.94–1.12] | 1.04 [0.95–1.12] | 1.02 [0.97–1.06] | 1.02 [0.99–1.06] |
| rest | rest -2.5 (never) | 1.05 [0.79–1.32] | 1.00 [0.85–1.15] | 1.02 [0.97–1.07] | 1.02 [0.97–1.07] |
| rest | rest +0.8 (names the action), when tired | — | 1.01 [0.91–1.10] | 1.04 [0.83–1.24] | 1.01 [0.93–1.09] |
| rest | rest +0.8 (names the action) | 1.00 [0.95–1.05] | 0.99 [0.94–1.04] | 0.87 [0.69–1.06] | 0.99 [0.95–1.03] |
| rest | no effect | 0.99 [0.95–1.04] | 1.00 [0.95–1.04] | *0.81* [0.69–0.94] | 0.99 [0.96–1.02] |
| rest | rest +0.8 (names the action), when food is close | 1.01 [0.90–1.12] | — | — | 0.98 [0.88–1.09] |
| rest | rest +0.8 (names the action), when old | 0.98 [0.85–1.11] | — | — | 0.98 [0.85–1.11] |
| rest | rest +0.8 (names the action), when safe | — | 1.01 [0.81–1.21] | 0.93 [0.74–1.12] | 0.97 [0.83–1.10] |
| mate | ▲ mate +0.8 (names the action), when plentiful | 1.02 [0.84–1.19] | 1.01 [0.96–1.06] | — | 1.01 [0.96–1.06] |
| mate | mate +0.8 (names the action), when energy is high & plentiful | — | 1.01 [0.96–1.06] | — | 1.01 [0.96–1.06] |
| mate | mate +0.8 (names the action), when old | 1.01 [0.97–1.05] | 1.00 [0.92–1.08] | — | 1.01 [0.97–1.05] |
| mate | mate +0.8 (names the action), when energy is high | — | 1.06 [0.83–1.29] | — | 1.00 [0.81–1.19] |
| mate | mate +0.8 (names the action), when hungry | — | 1.00 [0.80–1.21] | — | 1.00 [0.80–1.21] |
| mate | no effect | 0.99 [0.94–1.03] | 0.92 [0.81–1.03] | 1.00 [0.97–1.04] | 0.99 [0.97–1.02] |
| mate | mate +0.3 (sometimes) | — | — | 0.99 [0.93–1.06] | 0.99 [0.93–1.06] |
| mate | mate +1.2 (often), when energy is high & plentiful | — | 0.97 [0.80–1.14] | — | 0.97 [0.80–1.14] |
| mate | mate +0.8 (names the action) | 0.98 [0.85–1.12] | 0.93 [0.80–1.06] | 0.98 [0.73–1.23] | 0.96 [0.88–1.05] |
| attack | attack +0.8 (names the action), when very close | 1.03 [0.83–1.24] | 1.05 [0.90–1.19] | 0.94 [0.74–1.14] | 1.01 [0.91–1.12] |
| attack | ▲ attack -2.5 (never) | 1.01 [0.97–1.05] | — | 1.01 [0.98–1.04] | 1.01 [0.98–1.03] |
| attack | attack +0.8 (names the action), when hungry | — | 1.01 [0.98–1.04] | — | 1.01 [0.98–1.04] |
| attack | no effect | 0.99 [0.93–1.04] | 0.93 [0.82–1.03] | 0.97 [0.88–1.06] | 0.98 [0.93–1.02] |
| attack | attack -1.2 (rarely) | — | — | 0.97 [0.68–1.27] | 0.97 [0.68–1.27] |
| attack | attack +0.8 (names the action), when hungry & energy is high | — | 0.95 [0.78–1.13] | — | 0.95 [0.78–1.13] |
| attack | attack +0.8 (names the action) | 0.93 [0.76–1.11] | — | — | 0.91 [0.76–1.07] |
| attack | ✗ attack +2.5 (always) | 0.83 [0.61–1.05] | — | — | *0.77* [0.58–0.96] |
| risk | ▲ cautious | 1.01 [0.98–1.04] | 1.01 [0.98–1.04] | 1.01 [0.97–1.04] | 1.01 [0.99–1.02] |
| risk | cautious + bold | — | 0.99 [0.87–1.11] | — | 0.96 [0.85–1.07] |
| risk | cautious + familiar | — | 0.91 [0.70–1.11] | — | 0.91 [0.70–1.11] |
| risk | bold | — | — | — | 0.90 [0.69–1.10] |
| risk | ✗ no effect | 0.86 [0.68–1.03] | 0.83 [0.66–1.00] | 0.94 [0.81–1.06] | *0.89* [0.81–0.98] |
| social | ▲ solitary | 1.02 [0.98–1.05] | 1.00 [0.97–1.04] | 1.01 [0.93–1.09] | 1.01 [0.99–1.03] |
| social | social + solitary | — | 0.96 [0.76–1.16] | 1.01 [0.94–1.08] | 1.01 [0.94–1.07] |
| social | familiar | — | — | 1.00 [0.94–1.05] | 1.00 [0.94–1.05] |
| social | cautious | — | — | 0.99 [0.77–1.22] | 0.99 [0.77–1.22] |
| social | no effect | *0.90* [0.81–0.99] | 0.98 [0.88–1.07] | 1.00 [0.93–1.06] | 0.97 [0.92–1.01] |
| social | social | 0.90 [0.73–1.07] | 0.93 [0.68–1.18] | 0.97 [0.83–1.11] | 0.94 [0.85–1.04] |
| place | ▲ familiar | 1.01 [0.98–1.04] | 1.01 [0.98–1.04] | 1.01 [0.98–1.05] | 1.01 [0.99–1.03] |
| place | ✗ no effect | 0.96 [0.85–1.06] | 0.98 [0.92–1.03] | *0.91* [0.82–1.00] | *0.96* [0.91–1.00] |
| place | ✗ restless | — | — | — | *0.79* [0.61–0.97] |

## Single texts

The same fitness for each exact text carried by at least 100 animals (no text is clearly above average).

| slot | gene | origin | reading | carriers | fitness | killed by predators |
|---|---|---|---|---|---|---|
| risk | Cautious: safety comes before food. | founder | cautious | 113 | 1.10 [0.82–1.38] | 56% |
| rest | Save water by drinking tea in the morning. | mutant (llm#10) | no effect | 105 | 1.10 [0.81–1.39] | 32% |
| eat | Only look for food when energy is low. | founder | eat +0.8 (names the action), when hungry | 176 | 1.10 [0.89–1.30] | 42% |
| flee | No preference. | neutral | no effect | 102 | 1.09 [0.77–1.41] | 65% |
| social | Solitude is preferred by the crowded. | mutant (llm#13) | no effect | 119 | 1.08 [0.84–1.33] | 43% |
| mate | Calculate the recipe for a holographic blender. | mutant (llm#0) | no effect | 164 | 1.08 [0.86–1.29] | 43% |
| place | No particular temperament. | neutral | no effect | 130 | 1.08 [0.83–1.32] | 55% |
| mate | Abundant food only when bicycle is mate. | mutant (llm#15) | mate +0.8 (names the action) | 135 | 1.07 [0.84–1.30] | 55% |
| flee | Run only when a hunter is very near. | mutant (llm#6) | flee +0.8 (names the action) | 132 | 1.07 [0.83–1.31] | 41% |
| place | Places to attached. | mutant (llm#8) | familiar | 219 | 1.07 [0.90–1.24] | 39% |
| risk | Nervous: any sound nearby means constant pleasure. | mutant (llm#0) | cautious | 129 | 1.07 [0.81–1.33] | 41% |
| mate | Seek a partner before growing. | mutant (llm#8) | mate +0.8 (names the action) | 131 | 1.07 [0.79–1.35] | 37% |
| rest | Never stop moving. | founder | rest -2.5 (never) | 116 | 1.07 [0.80–1.33] | 61% |
| eat | Always finish eating your food before doing anything else. | mutant (llm#7) | eat +2.5 (always) | 119 | 1.06 [0.77–1.35] | 82% |
| follow | Keep your distance from other animals. | founder | no effect | 401 | 1.06 [0.92–1.20] | 60% |

## The most common genes at the end, and where they came from

**eat: "Heavy food is only sought for when energy low."** — eat +0.8 (names the action) (45 of 50 living animals)

```text
Only look for food when energy is low.  ← founder
Low energy when food only look for is.  ← "Mutate this sentence at random."
Low energy when food only look for is heavy.  ← "Randomly add a word to this sentence."
Heavy food is only look for when energy low.  ← "Change this sentence randomly."
Heavy food is only sought for when energy low.  ← "Randomly change one word in this sentence."
```

**mate: "Find a friend before becoming brave."** — no effect (40 of 50 living animals)

```text
Seek a partner before growing old.  ← founder
Find a friend before becoming wise.  ← "Randomly change a few words in this sentence."
Find a friend before becoming brave.  ← "Make one random change to this sentence."
```

**social: "Silence is preferred by the solitary one."** — solitary (40 of 50 living animals)

```text
Solitary: prefers to be alone.  ← founder
Alone is preferred by the solitary one.  ← "Change this sentence in a random way, big or small."
Crowds are preferred by the solitary one.  ← "Randomly change the meaning of this sentence a little."
Silence is preferred by the solitary one.  ← "Change one random detail in this sentence."
```

**wander: "Stay running to old places."** — no effect (34 of 50 living animals)

```text
Keep moving to new places.  ← founder
Keep running to new places.  ← "Randomly change one word in this sentence."
Stay walking to old places.  ← "Randomly change a few words in this sentence."
Stay running to old places.  ← "Change one random detail in this sentence."
```

**follow: "The wild beasts are nearby."** — no effect (25 of 50 living animals)

```text
Stay close to other animals.  ← founder
The beasts are nearby.  ← "Change this sentence in a random way, big or small."
The wild beasts are nearby.  ← "Randomly add a word to this sentence."
```

