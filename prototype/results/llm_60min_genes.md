# Which genes did best — llm_60min

Run: 5269 ticks, 21 generations, 231 births, 295 deaths (predators 71%, starvation 29%, old age 0%), 68 mutations (68 tried), brain `llm`, seed 1234.
Living animals at the end: 25; their genes: 13% mutants, 98% still use a word of the animal's world, 5.2 words on average.

**Fitness** = mean number of offspring of the animals that carried the gene and died during the run (295 animals here), each divided by the mean of the animals that died in the same 5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the population over thousands of different texts, so genes are grouped by what the keyword brain reads in them (strength, action, conditions): texts that read the same behave the same. Marks: **★** best reading of its slot and clearly above average; **▲** best of the readings that are above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. Readings carried by fewer than 25 animals are left out.

## Marked genes

- ▲ **eat** — eat +0.8 (names the action), when hungry: fitness 1.25 [0.86–1.63] over 107 carriers (per run: 1.25), killed by predators 73%. Most carried texts: "Only look for food when energy is low."; "Look only for food when energy is low.".
- ▲ **flee** — flee +0.8 (names the action), when predator: fitness 1.13 [0.78–1.48] over 123 carriers (per run: 1.13), killed by predators 73%. Most carried texts: "Run away from anything that attacks you."; "Run from any predator you see."; "Flee very when a predator is only close.".
- ▲ **follow** — follow +0.8 (names the action), when hungry & alone: fitness 1.13 [0.76–1.50] over 77 carriers (per run: 1.13), killed by predators 71%. Most carried texts: "Follow others when you are lost or hungry."; "Follow others when you are hungry or lost, unless they are ghosts.".
- ▲ **wander** — wander +0.8 (names the action): fitness 1.12 [0.73–1.51] over 93 carriers (per run: 1.12), killed by predators 65%. Most carried texts: "Keep moving to new places."; "Keep moving to new colors."; "Explore when there is nothing to do.".
- ▲ **rest** — rest +0.8 (names the action), when safe: fitness 1.05 [0.58–1.51] over 58 carriers (per run: 1.05), killed by predators 78%. Most carried texts: "Rest only when you feel safe.".
- ▲ **mate** — mate +0.8 (names the action), when energy is high: fitness 1.21 [0.81–1.60] over 108 carriers (per run: 1.21), killed by predators 70%. Most carried texts: "Look for a partner when energy is high.".
- ▲ **attack** — attack +0.8 (names the action), when hungry: fitness 1.33 [0.79–1.87] over 61 carriers (per run: 1.33), killed by predators 61%. Most carried texts: "Attack weaker animals when you are hungry.".
- ▲ **risk** — cautious: fitness 1.10 [0.80–1.40] over 154 carriers (per run: 1.10), killed by predators 68%. Most carried texts: "Cautious: safety comes before food."; "Nervous: any movement nearby means danger.".
- ▲ **social** — social + solitary: fitness 1.18 [0.73–1.63] over 57 carriers (per run: 1.18), killed by predators 72%. Most carried texts: "Wary of strangers, friendly to companions.".
- **place** — no steady leader.

## Selection by predators over time

| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–1000 | 12 | 36 | 18 | 2.95 |
| 1000–2000 | 11 | 39 | 14 | 3.48 |
| 2000–3000 | 11 | 38 | 13 | 3.39 |
| 3000–4000 | 13 | 36 | 13 | 2.73 |
| 4000–5000 | 22 | 45 | 17 | 2.08 |
| 5000–6000 | 27 | 15 | 11 | 0.56 |

## Slot by slot: what the keyword brain reads in each gene

Living share: share of the living animals carrying that reading at the start, at tick 3000 and at the end.

### eat

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | eat +0.8 (names the action), when hungry | 107 | 1.25 [0.86–1.63] | 73% | 21% → 30% → 80% | 2 | "Only look for food when energy is low." (106)<br>"Look only for food when energy is low." (1) |
|  | eat +1.2 (often) | 46 | 0.94 [0.54–1.34] | 61% | 17% → 10% → 0% | 2 | "Eat quickly, then move on." (44)<br>"Eat quickly, then sit down." (2) |
|  | eat +2.5 (always) | 61 | 0.94 [0.49–1.38] | 70% | 21% → 10% → 16% | 1 | "Always finish eating before doing anything else." (61) |
|  | no effect | 58 | 0.93 [0.45–1.41] | 72% | 25% → 50% → 0% | 4 | "No preference." (53)<br>"Seek nourishment only if power is depleted." (2)<br>"Highly preferred." (2) |

### flee

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | flee +0.8 (names the action), when predator | 123 | 1.13 [0.78–1.48] | 73% | 25% → 40% → 56% | 3 | "Run away from anything that attacks you." (66)<br>"Run from any predator you see." (56)<br>"Flee very when a predator is only close." (1) |
|  | no effect | 80 | 1.07 [0.66–1.48] | 62% | 25% → 40% → 44% | 4 | "No preference." (72)<br>"Everything is acceptable." (6)<br>"Absolutely any choice." (1) |
|  | flee +0.8 (names the action), when predator & very close | 54 | 0.86 [0.45–1.26] | 76% | 29% → 0% → 0% | 1 | "Flee only when a predator is very close." (54) |
|  | flee +0.8 (names the action), unless predator & very close | 36 | 0.67 [0.21–1.14] | 75% | 21% → 20% → 0% | 1 | "Stay calm unless danger is right next to you." (36) |

### follow

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | follow +0.8 (names the action), when hungry & alone | 77 | 1.13 [0.76–1.50] | 71% | 8% → 10% → 64% | 2 | "Follow others when you are lost or hungry." (76)<br>"Follow others when you are hungry or lost, unless they are ghosts." (1) |
|  | follow +0.8 (names the action), when energy is high | 36 | 1.12 [0.40–1.83] | 56% | 8% → 0% → 0% | 1 | "Follow the strongest animal nearby." (36) |
|  | no effect | 111 | 0.97 [0.61–1.32] | 77% | 50% → 50% → 4% | 3 | "No preference." (64)<br>"Keep your distance from other animals." (46)<br>"The oven is screaming at the refrigerator." (1) |
|  | follow +0.8 (names the action) | 71 | 0.85 [0.49–1.22] | 69% | 33% → 40% → 32% | 2 | "Stay close to other animals." (52)<br>"Follow the smallest vehicle nearby." (19) |

### wander

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | wander +0.8 (names the action) | 93 | 1.12 [0.73–1.51] | 65% | 21% → 20% → 8% | 4 | "Keep moving to new places." (88)<br>"Keep moving to new colors." (3)<br>"Explore when there is nothing to do." (1) |
|  | wander +0.8 (names the action), when food is far | 96 | 0.99 [0.63–1.35] | 73% | 58% → 60% → 0% | 2 | "Explore when there is nothing else to do." (57)<br>"Roam far when food is scarce." (39) |
|  | no effect | 106 | 0.91 [0.59–1.22] | 75% | 21% → 20% → 92% | 5 | "No preference." (49)<br>"Stay near where you last found food." (44)<br>"No choice." (9) |

### rest

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 86 | 1.17 [0.75–1.58] | 69% | 8% → 70% → 20% | 3 | "No preference." (84)<br>"Total preference." (1)<br>"Run only when you feel safe." (1) |
| ▲ | rest +0.8 (names the action), when safe | 58 | 1.05 [0.58–1.51] | 78% | 21% → 10% → 48% | 1 | "Rest only when you feel safe." (58) |
|  | rest -2.5 (never) | 63 | 1.00 [0.51–1.49] | 63% | 25% → 10% → 32% | 3 | "Never stop moving." (57)<br>"Never stop lasagna." (5)<br>"Never stop blooming." (1) |
|  | rest +0.8 (names the action), when food is far | 58 | 0.85 [0.48–1.22] | 74% | 38% → 10% → 0% | 2 | "Save energy by resting when food is far." (57)<br>"When food is far, rest to save energy." (1) |

### mate

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | mate +0.8 (names the action), when energy is high | 108 | 1.21 [0.81–1.60] | 70% | 21% → 40% → 72% | 1 | "Look for a partner when energy is high." (108) |
|  | mate +0.8 (names the action), when plentiful | 88 | 1.08 [0.76–1.39] | 69% | 42% → 40% → 4% | 2 | "Mate only when food is plentiful." (87)<br>"Mate when food is plentiful." (1) |
|  | mate +0.8 (names the action), when old | 47 | 0.74 [0.22–1.26] | 72% | 17% → 10% → 0% | 1 | "Seek a partner before growing old." (47) |
|  | no effect | 34 | 0.56 [0.09–1.03] | 79% | 17% → 10% → 20% | 6 | "No preference." (22)<br>"Search for a friend when power is low." (5)<br>"Search for a power when friend is low." (4) |

### attack

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | attack +0.8 (names the action), when hungry | 61 | 1.33 [0.79–1.87] | 61% | 33% → 20% → 0% | 1 | "Attack weaker animals when you are hungry." (61) |
|  | attack -2.5 (never) | 105 | 1.06 [0.69–1.43] | 79% | 17% → 60% → 96% | 1 | "Never fight." (105) |
|  | attack +0.8 (names the action) | 57 | 0.92 [0.47–1.37] | 65% | 25% → 10% → 4% | 2 | "Attack only to defend your food." (55)<br>"Attack only to provide your neighbors with a feast." (2) |
|  | no effect | 39 | 0.70 [0.33–1.07] | 77% | 4% → 10% → 0% | 3 | "No preference." (33)<br>"The blue muffins smell like algebra during the winter." (5)<br>"The blue muffins smell like spicy cinnamon during the winter." (1) |
|  | attack +0.8 (names the action), when very close | 32 | 0.62 [0.18–1.06] | 66% | 21% → 0% → 0% | 1 | "Fight anyone who comes too close." (32) |

### risk

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | cautious | 154 | 1.10 [0.80–1.40] | 68% | 42% → 80% → 40% | 2 | "Cautious: safety comes before food." (107)<br>"Nervous: any movement nearby means danger." (47) |
|  | no effect | 97 | 0.99 [0.68–1.31] | 72% | 25% → 10% → 60% | 2 | "No particular temperament." (92)<br>"Temperament particular No." (5) |
|  | bold | 33 | 0.87 [0.19–1.56] | 73% | 21% → 0% → 0% | 2 | "Bold: take risks when the reward is food." (29)<br>"Risk is food when bold take reward." (4) |

### social

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 66 | 1.25 [0.76–1.73] | 76% | 17% → 10% → 60% | 3 | "No particular temperament." (64)<br>"The spicy tacos are screaming loudly." (1)<br>"I want to eat a giant chocolate muffin." (1) |
| ▲ | social + solitary | 57 | 1.18 [0.73–1.63] | 72% | 25% → 10% → 0% | 1 | "Wary of strangers, friendly to companions." (57) |
|  | solitary | 108 | 0.92 [0.59–1.25] | 69% | 21% → 20% → 40% | 4 | "Solitary: prefers to be alone." (100)<br>"Solitary: prefers to be a pizza." (6)<br>"Solitary: prefers to be joined." (1) |
|  | social | 64 | 0.72 [0.31–1.13] | 69% | 38% → 60% → 0% | 3 | "Curious about other animals." (50)<br>"Social: feels safer in a group." (11)<br>"Curious about animals." (3) |

### place

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
|  | no effect | 63 | 1.13 [0.66–1.60] | 73% | 17% → 10% → 52% | 4 | "No particular temperament." (60)<br>"No particular destination." (1)<br>"No particular personality." (1) |
|  | restless | 124 | 0.98 [0.65–1.30] | 73% | 33% → 30% → 24% | 4 | "Likes open ground where danger is easy to see." (107)<br>"Restless: always wants somewhere new." (15)<br>"Likes open ground where danger is easy to see clearly." (1) |
|  | familiar | 108 | 0.95 [0.63–1.27] | 68% | 50% → 60% → 24% | 2 | "Attached to familiar places." (61)<br>"Prefers staying near water." (47) |

## Single texts

The same fitness for each exact text carried by at least 25 animals (no text is clearly above average).

| slot | gene | origin | reading | carriers | fitness | killed by predators |
|---|---|---|---|---|---|---|
| attack | Attack weaker animals when you are hungry. | founder | attack +0.8 (names the action), when hungry | 61 | 1.33 [0.79–1.87] | 61% |
| social | No particular temperament. | neutral | no effect | 64 | 1.28 [0.78–1.77] | 77% |
| eat | Only look for food when energy is low. | founder | eat +0.8 (names the action), when hungry | 106 | 1.26 [0.87–1.65] | 74% |
| flee | Run from any predator you see. | founder | flee +0.8 (names the action), when predator | 56 | 1.23 [0.66–1.80] | 64% |
| risk | Cautious: safety comes before food. | founder | cautious | 107 | 1.23 [0.84–1.61] | 67% |
| mate | Look for a partner when energy is high. | founder | mate +0.8 (names the action), when energy is high | 108 | 1.21 [0.81–1.60] | 70% |
| rest | No preference. | neutral | no effect | 84 | 1.19 [0.77–1.62] | 68% |
| social | Wary of strangers, friendly to companions. | founder | social + solitary | 57 | 1.18 [0.73–1.63] | 72% |
| place | No particular temperament. | neutral | no effect | 60 | 1.18 [0.69–1.67] | 72% |
| wander | Keep moving to new places. | founder | wander +0.8 (names the action) | 88 | 1.16 [0.75–1.57] | 62% |
| follow | Follow others when you are lost or hungry. | founder | follow +0.8 (names the action), when hungry & alone | 76 | 1.14 [0.77–1.51] | 71% |
| follow | No preference. | neutral | no effect | 64 | 1.13 [0.57–1.69] | 75% |
| place | Likes open ground where danger is easy to see. | founder | restless | 107 | 1.13 [0.76–1.50] | 70% |
| flee | No preference. | neutral | no effect | 72 | 1.12 [0.67–1.56] | 64% |
| follow | Follow the strongest animal nearby. | founder | follow +0.8 (names the action), when energy is high | 36 | 1.12 [0.40–1.83] | 56% |

## The most common genes at the end, and where they came from

**follow: "Follow the smallest vehicle nearby."** — follow +0.8 (names the action) (8 of 25 living animals)

```text
Follow the strongest animal nearby.  ← founder
Follow the smallest vehicle nearby.  ← "Randomly change a few words in this sentence."
```

**social: "No particular personality."** — no effect (5 of 25 living animals)

```text
No particular temperament.  ← neutral
No particular personality.  ← "Randomly rewrite one part of this sentence."
```

**mate: "Search for a friend when power is low."** — no effect (4 of 25 living animals)

```text
Look for a partner when energy is high.  ← founder
Search for a friend when power is low.  ← "Randomly change a few words in this sentence."
```

**social: "Loner: enjoys solitude."** — no effect (2 of 25 living animals)

```text
Solitary: prefers to be alone.  ← founder
Loner: enjoys solitude.  ← "Change this sentence randomly."
```

**follow: "Follow others when you are hungry or lost, unless they are ghosts."** — follow +0.8 (names the action), when hungry & alone (2 of 25 living animals)

```text
Follow others when you are lost or hungry.  ← founder
Follow others when you are hungry or lost, unless they are ghosts.  ← "Make an unexpected change to this sentence."
```

