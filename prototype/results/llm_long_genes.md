# Which genes did best — llm_long

Run: 57061 ticks, 5 generations, 2737 births, 3769 deaths (predators 68%, starvation 32%, old age 0%), 812 mutations (826 tried), brain `llm`, seed 1234.
Living animals at the end: 10; their genes: 0% mutants, 100% still use a word of the animal's world, 4.9 words on average.

**Fitness** = mean number of offspring of the animals that carried the gene and died during the run (3769 animals here), each divided by the mean of the animals that died in the same 5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the population over thousands of different texts, so genes are grouped by what the keyword brain reads in them (strength, action, conditions): texts that read the same behave the same. Marks: **★** best reading of its slot and clearly above average; **▲** best of the readings that are above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. Readings carried by fewer than 25 animals are left out.

## Marked genes

- ▲ **eat** — eat +0.8 (names the action): fitness 1.20 [0.23–2.16] over 28 carriers (per run: 1.20), killed by predators 64%. Most carried texts: "Only seek for food when power is low."; "Only look for food when energy is very low."; "Only look for food when energy is radioactive.".
- ▲ **flee** — flee +0.8 (names the action), when predator: fitness 1.04 [0.91–1.17] over 909 carriers (per run: 1.04), killed by predators 74%. Most carried texts: "Run from any predator you see."; "Run away from anything that attacks you."; "Anything that attacks you run away from.".
- ▲ **follow** — follow +0.8 (names the action), when hungry & alone: fitness 1.15 [0.97–1.33] over 599 carriers (per run: 1.15), killed by predators 70%. Most carried texts: "Follow others when you are lost or hungry."; "Follow others when you are hungry or lost, unless they are ghosts."; "Follow others when you are lost or hungry now.".
- ▲ **wander** — wander +0.8 (names the action), when food is far: fitness 1.05 [0.91–1.19] over 820 carriers (per run: 1.05), killed by predators 78%. Most carried texts: "Roam far when food is scarce."; "Explore when there is nothing else to do.".
- ▲ **rest** — rest +0.8 (names the action): fitness 1.14 [0.63–1.65] over 27 carriers (per run: 1.14), killed by predators 85%. Most carried texts: "Rest only when you feel happy."; "Rest only when you feel toaster."; "Sleep is for those who are secure.".
  - ✗ rest +0.8 (names the action), when food is far: *0.78* [0.61–0.94], killed by predators 82% ("Save energy by resting when food is far."; "When food is far, rest to save energy.")
- ▲ **mate** — mate +0.8 (names the action), when energy is high: fitness 1.07 [0.99–1.15] over 1927 carriers (per run: 1.07), killed by predators 61%. Most carried texts: "Look for a partner when energy is high."; "Look partner for a when energy is high."; "Look partner for a quick when energy is high.".
- ▲ **attack** — attack +0.8 (names the action), when hungry: fitness 1.05 [0.83–1.26] over 366 carriers (per run: 1.05), killed by predators 75%. Most carried texts: "Attack weaker animals when you are hungry.".
- ▲ **risk** — cautious: fitness 1.01 [0.89–1.14] over 924 carriers (per run: 1.01), killed by predators 69%. Most carried texts: "Cautious: safety comes before food."; "Nervous: any movement nearby means danger."; "Cautious: safety comes before explosions.".
  - ✗ cautious + bold: *0.79* [0.61–0.97], killed by predators 80% ("Reckless when starving, careful when fed."; "Reckless when hungry, careful when fed.")
- ▲ **social** — social: fitness 1.02 [0.88–1.16] over 737 carriers (per run: 1.02), killed by predators 76%. Most carried texts: "Social: feels safer in a group."; "Curious about other animals."; "Curious about animals.".
- **place** — no steady leader.

## Selection by predators over time

| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–2000 | 12 | 75 | 32 | 3.21 |
| 2000–4000 | 12 | 74 | 26 | 3.03 |
| 4000–6000 | 25 | 100 | 48 | 1.99 |
| 6000–8000 | 32 | 110 | 76 | 1.72 |
| 8000–10000 | 30 | 107 | 88 | 1.77 |
| 10000–12000 | 29 | 113 | 98 | 1.92 |
| 12000–14000 | 28 | 113 | 97 | 2.03 |
| 14000–16000 | 28 | 111 | 104 | 2.00 |
| 16000–18000 | 28 | 110 | 99 | 1.94 |
| 18000–20000 | 27 | 107 | 96 | 1.98 |
| 20000–22000 | 20 | 98 | 51 | 2.40 |
| 22000–24000 | 20 | 100 | 37 | 2.52 |
| 24000–26000 | 11 | 64 | 11 | 2.83 |
| 26000–28000 | 10 | 82 | 24 | 3.98 |
| 28000–30000 | 10 | 77 | 27 | 3.72 |
| 30000–32000 | 11 | 75 | 16 | 3.41 |
| 32000–34000 | 10 | 89 | 17 | 4.38 |
| 34000–36000 | 11 | 82 | 24 | 3.87 |
| 36000–38000 | 11 | 77 | 31 | 3.56 |
| 38000–40000 | 10 | 93 | 14 | 4.47 |
| 40000–42000 | 11 | 79 | 24 | 3.67 |
| 42000–44000 | 10 | 88 | 17 | 4.25 |
| 44000–46000 | 12 | 80 | 25 | 3.39 |
| 46000–48000 | 11 | 77 | 25 | 3.55 |
| 48000–50000 | 11 | 81 | 27 | 3.82 |
| 50000–52000 | 10 | 91 | 10 | 4.33 |
| 52000–54000 | 11 | 79 | 31 | 3.51 |
| 54000–56000 | 11 | 81 | 21 | 3.57 |
| 56000–58000 | 10 | 43 | 9 | 2.07 |

## Slot by slot: what the keyword brain reads in each gene

Living share: share of the living animals carrying that reading at the start, at tick 28000 and at the end.

### eat

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | eat +0.8 (names the action) | 28 | 1.20 [0.23–2.16] | 64% | 0% → 0% → 0% | 8 | "Only seek for food when power is low." (16)<br>"Only look for food when energy is very low." (5)<br>"Only look for food when energy is radioactive." (2) |
|  | eat +0.8 (names the action), when hungry | 808 | 1.03 [0.91–1.15] | 68% | 21% → 18% → 40% | 5 | "Only look for food when energy is low." (802)<br>"Only look for food when energy is low today." (3)<br>"Look only for food when energy is low." (1) |
|  | no effect | 1854 | 1.02 [0.94–1.10] | 62% | 25% → 55% → 0% | 48 | "Only look for water when energy is low." (1267)<br>"No preference." (402)<br>"Only look for gold when energy is low." (63) |
|  | eat +1.2 (often) | 323 | 0.96 [0.74–1.17] | 77% | 17% → 18% → 40% | 8 | "Eat quickly, then move on." (311)<br>"Eat whenever food is loud." (4)<br>"Eat quickly, then sit down." (2) |
|  | eat +1.2 (often), when food is close | 343 | 0.94 [0.74–1.15] | 80% | 17% → 9% → 10% | 2 | "Eat whenever food is close." (342)<br>"Food is close whenever eat." (1) |
|  | eat +2.5 (always) | 409 | 0.94 [0.75–1.12] | 78% | 21% → 0% → 10% | 4 | "Always finish eating before doing anything else." (406)<br>"Anything else finish eating before doing always." (1)<br>"Always finish eating quickly before doing anything else." (1) |

### flee

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | flee +0.8 (names the action), when predator | 909 | 1.04 [0.91–1.17] | 74% | 25% → 36% → 20% | 8 | "Run from any predator you see." (452)<br>"Run away from anything that attacks you." (417)<br>"Anything that attacks you run away from." (31) |
|  | no effect | 2111 | 1.01 [0.94–1.08] | 62% | 25% → 9% → 50% | 38 | "No preference." (1859)<br>"No preference, really." (97)<br>"Any preference." (31) |
|  | flee +0.8 (names the action), unless predator & very close | 304 | 0.93 [0.71–1.15] | 78% | 21% → 18% → 0% | 1 | "Stay calm unless danger is right next to you." (304) |
|  | flee +0.8 (names the action), when predator & very close | 400 | 0.92 [0.72–1.11] | 80% | 29% → 36% → 30% | 2 | "Flee only when a predator is very close." (399)<br>"Flee predator when a only is very close." (1) |
|  | flee -2.5 (never) | 25 | 0.89 [0.36–1.42] | 44% | 0% → 0% → 0% | 2 | "I don't mind at all." (13)<br>"I don't care either way." (12) |

### follow

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | follow +0.8 (names the action), when hungry & alone | 599 | 1.15 [0.97–1.33] | 70% | 8% → 27% → 20% | 8 | "Follow others when you are lost or hungry." (483)<br>"Follow others when you are hungry or lost, unless they are ghosts." (109)<br>"Follow others when you are lost or hungry now." (2) |
|  | follow +0.8 (names the action) | 1904 | 1.02 [0.94–1.09] | 61% | 33% → 45% → 20% | 30 | "Follow the largest vehicle nearby." (695)<br>"Follow the smallest vehicle nearby." (444)<br>"Follow the smallest red vehicle nearby." (401) |
|  | no effect | 944 | 0.93 [0.82–1.04] | 78% | 50% → 18% → 40% | 29 | "Keep your distance from other animals." (424)<br>"No preference." (391)<br>"The tiny nearby car should be trailed." (97) |
|  | follow +0.8 (names the action), when energy is high | 312 | 0.84 [0.65–1.03] | 75% | 8% → 9% → 20% | 1 | "Follow the strongest animal nearby." (312) |

### wander

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | wander +0.8 (names the action), when food is far | 820 | 1.05 [0.91–1.19] | 78% | 58% → 36% → 40% | 2 | "Roam far when food is scarce." (431)<br>"Explore when there is nothing else to do." (389) |
|  | no effect | 2558 | 1.00 [0.93–1.07] | 63% | 21% → 45% → 30% | 60 | "Stay near where you last found food." (1097)<br>"Stay near where you last found bicycle." (734)<br>"No preference." (394) |
|  | wander +0.8 (names the action) | 391 | 0.91 [0.73–1.09] | 79% | 21% → 18% → 30% | 12 | "Keep moving to new places." (375)<br>"Keep moving to new colors." (3)<br>"Keep places to new moving." (3) |

### rest

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | rest +0.8 (names the action) | 27 | 1.14 [0.63–1.65] | 85% | 0% → 0% → 0% | 4 | "Rest only when you feel happy." (14)<br>"Rest only when you feel toaster." (7)<br>"Sleep is for those who are secure." (5) |
|  | no effect | 2079 | 1.06 [0.99–1.14] | 61% | 8% → 36% → 20% | 48 | "No banana." (955)<br>"No preference." (526)<br>"The moon is allergic to geometry." (170) |
|  | rest +0.8 (names the action), when safe | 580 | 1.00 [0.85–1.14] | 72% | 21% → 0% → 0% | 4 | "Rest only when you feel safe." (577)<br>"You only feel safe when you rest." (1)<br>"Rest only when you feel truly safe." (1) |
|  | rest -2.5 (never) | 378 | 0.93 [0.75–1.10] | 75% | 25% → 18% → 10% | 5 | "Never stop moving." (368)<br>"Never stop lasagna." (6)<br>"Moving never stop." (2) |
|  | rest +0.8 (names the action), when tired | 344 | 0.91 [0.69–1.14] | 78% | 8% → 9% → 40% | 2 | "Rest when you are tired." (343)<br>"Tired when you are Rest." (1) |
| ✗ | rest +0.8 (names the action), when food is far | 349 | *0.78* [0.61–0.94] | 82% | 38% → 36% → 30% | 3 | "Save energy by resting when food is far." (347)<br>"When food is far, rest to save energy." (1)<br>"Save energy by resting when food is scarce." (1) |

### mate

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | mate +0.8 (names the action), when energy is high | 1927 | 1.07 [0.99–1.15] | 61% | 21% → 55% → 30% | 9 | "Look for a partner when energy is high." (1327)<br>"Look partner for a when energy is high." (532)<br>"Look partner for a quick when energy is high." (32) |
|  | mate +0.8 (names the action), when old | 326 | 1.01 [0.79–1.23] | 78% | 17% → 0% → 20% | 2 | "Seek a partner before growing old." (325)<br>"Seek a partner growing old." (1) |
|  | no effect | 708 | 0.94 [0.81–1.06] | 73% | 17% → 36% → 30% | 39 | "No preference." (302)<br>"A salad is hiding under a bicycle." (155)<br>"Look for a teammate when energy is high." (118) |
|  | mate +0.8 (names the action), when plentiful | 380 | 0.90 [0.73–1.07] | 77% | 42% → 0% → 0% | 5 | "Mate only when food is plentiful." (376)<br>"Mate when food is plentiful." (1)<br>"Plentiful food is only for mate." (1) |
|  | mate +0.8 (names the action) | 412 | 0.89 [0.73–1.04] | 73% | 4% → 9% → 20% | 19 | "Mate with any nearby adult." (321)<br>"High energy is the time to find a partner." (23)<br>"When high is partner for look a energy." (15) |

### attack

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | attack +0.8 (names the action), when hungry | 366 | 1.05 [0.83–1.26] | 75% | 33% → 64% → 20% | 1 | "Attack weaker animals when you are hungry." (366) |
|  | attack -2.5 (never) | 2239 | 1.04 [0.97–1.11] | 63% | 17% → 0% → 10% | 20 | "Never fight." (2154)<br>"Fight never." (17)<br>"Never dance." (11) |
|  | attack +0.8 (names the action) | 405 | 0.94 [0.76–1.12] | 81% | 25% → 9% → 10% | 5 | "Attack only to defend your food." (400)<br>"Attack only to provide your neighbors with a feast." (2)<br>"Attack only to protect your food." (1) |
|  | no effect | 382 | 0.93 [0.72–1.15] | 75% | 4% → 9% → 40% | 14 | "No preference." (361)<br>"The blue muffins smell like algebra during the winter." (5)<br>"Combat is forbidden." (3) |
|  | attack +0.8 (names the action), when very close | 350 | 0.85 [0.67–1.03] | 67% | 21% → 18% → 20% | 1 | "Fight anyone who comes too close." (350) |

### risk

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 2231 | 1.05 [0.97–1.12] | 64% | 25% → 9% → 50% | 43 | "No particular temperament." (1874)<br>"The temperament is not particularly." (83)<br>"No temperament." (47) |
| ▲ | cautious | 924 | 1.01 [0.89–1.14] | 69% | 42% → 55% → 30% | 15 | "Cautious: safety comes before food." (433)<br>"Nervous: any movement nearby means danger." (371)<br>"Cautious: safety comes before explosions." (92) |
|  | bold | 335 | 0.82 [0.64–1.00] | 81% | 21% → 9% → 10% | 4 | "Bold: take risks when the reward is food." (329)<br>"Risk is food when bold take reward." (4)<br>"No particular bold temperament." (1) |
| ✗ | cautious + bold | 279 | *0.79* [0.61–0.97] | 80% | 12% → 27% → 10% | 6 | "Reckless when starving, careful when fed." (274)<br>"Reckless when hungry, careful when fed." (1)<br>"When fed, careful; when starving, reckless." (1) |

### social

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 1447 | 1.05 [0.95–1.14] | 62% | 17% → 0% → 20% | 42 | "Loner: enjoys community." (680)<br>"No particular temperament." (578)<br>"Loner: enjoys solitude." (40) |
| ▲ | social | 737 | 1.02 [0.88–1.16] | 76% | 38% → 55% → 30% | 6 | "Social: feels safer in a group." (366)<br>"Curious about other animals." (364)<br>"Curious about animals." (4) |
|  | social + solitary | 314 | 0.96 [0.75–1.17] | 77% | 25% → 18% → 30% | 3 | "Wary of strangers, friendly to companions." (302)<br>"Solitary: prefers to be together." (11)<br>"Hostile to strangers, friendly to companions." (1) |
|  | solitary | 1271 | 0.95 [0.86–1.04] | 68% | 21% → 27% → 20% | 22 | "Solitary: prefers to be alone." (699)<br>"Solitary: desires to be hidden." (445)<br>"Solitary: desires to be a skyscraper." (47) |

### place

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 2141 | 1.02 [0.95–1.09] | 62% | 17% → 27% → 20% | 65 | "No particular temperament." (1408)<br>"Not a specific disposition." (277)<br>"No particular temperature." (237) |
|  | restless | 851 | 0.98 [0.85–1.11] | 74% | 33% → 64% → 20% | 9 | "Likes open ground where danger is easy to see." (523)<br>"Restless: always wants somewhere new." (318)<br>"Likes open ground where danger is easy to smell like blueberries." (3) |
|  | familiar | 777 | 0.97 [0.82–1.11] | 77% | 50% → 9% → 60% | 5 | "Attached to familiar places." (394)<br>"Prefers staying near water." (375)<br>"Prefers staying near the water." (6) |

## Single texts

The same fitness for each exact text carried by at least 25 animals.

| slot | gene | origin | reading | carriers | fitness | killed by predators |
|---|---|---|---|---|---|---|
| mate | A salad is hiding under a bicycle. | mutant (llm#14) | no effect | 155 | 1.32 [0.99–1.64] | 83% |
| follow | Follow others when you are lost or hungry. | founder | follow +0.8 (names the action), when hungry & alone | 483 | 1.21 [1.00–1.42] | 74% |
| rest | No preference. | neutral | no effect | 526 | **1.20** [1.00–1.40] | 71% |
| social | Social: feels safer in a group. | founder | social | 366 | 1.18 [0.96–1.41] | 78% |
| place | No particular temperature. | mutant (llm#13) | no effect | 237 | 1.13 [0.91–1.35] | 73% |
| wander | Elephant was found last near where stay. | mutant (llm#15) | no effect | 61 | 1.12 [0.73–1.50] | 54% |
| wander | Stay far away from where you last found the bicycle. | mutant (llm#13) | no effect | 28 | 1.11 [0.58–1.64] | 75% |
| flee | Run from any predator you see. | founder | flee +0.8 (names the action), when predator | 452 | 1.11 [0.90–1.31] | 75% |
| mate | Look for a partner when energy is high. | founder | mate +0.8 (names the action), when energy is high | 1327 | 1.10 [0.99–1.21] | 64% |
| social | No particular temperament. | neutral | no effect | 578 | 1.09 [0.92–1.27] | 74% |
| wander | Stay near where you last found bicycle. | mutant (llm#5) | no effect | 734 | 1.09 [0.97–1.22] | 60% |
| rest | No banana. | mutant (llm#5) | no effect | 955 | 1.09 [0.98–1.20] | 58% |
| social | Loner: enjoys solitude. | mutant (llm#1) | no effect | 40 | 1.08 [0.67–1.50] | 55% |
| follow | Follow the largest vehicle nearby. | mutant (llm#6) | follow +0.8 (names the action) | 695 | 1.08 [0.96–1.21] | 61% |
| place | No specific personality. | mutant (llm#10) | no effect | 27 | 1.08 [0.40–1.75] | 70% |

## The most common genes at the end, and where they came from

