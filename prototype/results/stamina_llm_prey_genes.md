# Which genes did best — check_stamina_llm

Run: 2000 ticks, 11 generations, 552 births, 575 deaths (predators 65%, starvation 35%, old age 0%), 76 mutations (78 tried), brain `llm`, seed 1234.
Living animals at the end: 45; their genes: 7% mutants, 99% still use a word of the animal's world, 4.9 words on average.

**Fitness** = mean number of offspring of the animals that carried the gene and died during the run (575 animals here), each divided by the mean of the animals that died in the same 5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the population over thousands of different texts, so genes are grouped by what the keyword brain reads in them (strength, action, conditions): texts that read the same behave the same. Marks: **★** best reading of its slot and clearly above average; **▲** best of the readings that are above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. Readings carried by fewer than 100 animals are left out.

## Marked genes

- ▲ **eat** — eat +2.5 (always): fitness 1.07 [0.70–1.44] over 121 carriers (per run: 1.07), killed by predators 62%. Most carried texts: "Always finish eating before doing anything else."; "Anything else before always finish eating doing."; "Always finish eating before deciding to exist.".
- ▲ **flee** — flee +0.8 (names the action), when predator: fitness 1.05 [0.83–1.26] over 330 carriers (per run: 1.05), killed by predators 65%. Most carried texts: "Run from any predator you see."; "Run away from anything that attacks you."; "Escape from whatever is threatening you.".
- ▲ **follow** — follow +0.8 (names the action): fitness 1.10 [0.79–1.42] over 175 carriers (per run: 1.10), killed by predators 58%. Most carried texts: "Stay close to other animals."; "Follow the fastest animal nearby.".
- ▲ **rest** — rest +0.8 (names the action), when safe: fitness 1.02 [0.65–1.39] over 105 carriers (per run: 1.02), killed by predators 70%. Most carried texts: "Rest only when you feel safe."; "Rest only when you feel truly safe.".
- ▲ **mate** — mate +0.8 (names the action), when plentiful: fitness 1.08 [0.74–1.42] over 147 carriers (per run: 1.08), killed by predators 69%. Most carried texts: "Mate only when food is plentiful."; "Mate only when water is plentiful."; "Mate only when flowers are plentiful.".

## Selection by predators over time

| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–10000 | 58 | 374 | 201 | 0.65 |

## Slot by slot: what the keyword brain reads in each gene

Living share: share of the living animals carrying that reading at the start, at tick 0 and at the end.

### eat

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | eat +2.5 (always) | 121 | 1.07 [0.70–1.44] | 62% | 24% → 24% → 13% | 3 | "Always finish eating before doing anything else." (116)<br>"Anything else before always finish eating doing." (4)<br>"Always finish eating before deciding to exist." (1) |
|  | eat +1.2 (often), when food is close | 155 | 1.00 [0.69–1.30] | 69% | 22% → 22% → 22% | 3 | "Eat whenever food is close." (152)<br>"Eat whenever food is closed." (2)<br>"Whenever eat food is close." (1) |
|  | no effect | 167 | 0.92 [0.65–1.18] | 62% | 24% → 24% → 42% | 3 | "No preference." (165)<br>"A toaster is actually a very polite underwater submarine." (1)<br>"Only look for snacks when energy is low." (1) |

### flee

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | flee +0.8 (names the action), when predator | 330 | 1.05 [0.83–1.26] | 65% | 50% → 50% → 64% | 5 | "Run from any predator you see." (192)<br>"Run away from anything that attacks you." (133)<br>"Escape from whatever is threatening you." (3) |
|  | flee +0.8 (names the action), when predator & very close | 115 | 1.03 [0.69–1.38] | 66% | 12% → 12% → 0% | 2 | "Flee only when a predator is very close." (114)<br>"Stand your ground until a predator is very close." (1) |
|  | no effect | 100 | 0.95 [0.61–1.29] | 61% | 18% → 18% → 36% | 8 | "No preference." (73)<br>"Some dislike." (17)<br>"Total preference." (3) |

### follow

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | follow +0.8 (names the action) | 175 | 1.10 [0.79–1.42] | 58% | 21% → 21% → 38% | 2 | "Stay close to other animals." (174)<br>"Follow the fastest animal nearby." (1) |
|  | no effect | 295 | 0.98 [0.77–1.18] | 67% | 47% → 47% → 60% | 9 | "Keep your distance from other animals." (193)<br>"No preference." (93)<br>"Keep your distance from other humans." (3) |

### rest

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | rest +0.8 (names the action), when safe | 105 | 1.02 [0.65–1.39] | 70% | 26% → 26% → 9% | 2 | "Rest only when you feel safe." (104)<br>"Rest only when you feel truly safe." (1) |
|  | rest -2.5 (never) | 192 | 1.00 [0.74–1.26] | 61% | 15% → 15% → 47% | 3 | "Never stop moving." (176)<br>"Moving stop never." (14)<br>"Never stop moving forward." (2) |
|  | no effect | 152 | 0.99 [0.65–1.33] | 64% | 25% → 25% → 20% | 6 | "No preference." (143)<br>"No choice." (5)<br>"Eat when the distance to food is long to conserve power." (1) |

### mate

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|
| ▲ | mate +0.8 (names the action), when plentiful | 147 | 1.08 [0.74–1.42] | 69% | 22% → 22% → 22% | 5 | "Mate only when food is plentiful." (143)<br>"Mate only when water is plentiful." (1)<br>"Mate only when flowers are plentiful." (1) |
|  | mate +0.8 (names the action) | 212 | 1.01 [0.76–1.25] | 65% | 21% → 21% → 40% | 5 | "Mate with any nearby adult." (206)<br>"Mate with any nearby adult bird." (3)<br>"Mate with any nearby toddler." (1) |

## Single texts

The same fitness for each exact text carried by at least 100 animals (no text is clearly above average).

| slot | gene | origin | reading | carriers | fitness | killed by predators |
|---|---|---|---|---|---|---|
| flee | Run from any predator you see. | founder | flee +0.8 (names the action), when predator | 192 | 1.14 [0.84–1.44] | 67% |
| follow | Stay close to other animals. | founder | follow +0.8 (names the action) | 174 | 1.11 [0.79–1.42] | 58% |
| mate | Mate only when food is plentiful. | founder | mate +0.8 (names the action), when plentiful | 143 | 1.11 [0.76–1.46] | 69% |
| eat | Always finish eating before doing anything else. | founder | eat +2.5 (always) | 116 | 1.08 [0.70–1.46] | 62% |
| flee | Flee only when a predator is very close. | founder | flee +0.8 (names the action), when predator & very close | 114 | 1.04 [0.69–1.39] | 67% |
| mate | Mate with any nearby adult. | founder | mate +0.8 (names the action) | 206 | 1.03 [0.78–1.28] | 64% |
| rest | Rest only when you feel safe. | founder | rest +0.8 (names the action), when safe | 104 | 1.02 [0.65–1.39] | 71% |
| rest | No preference. | neutral | no effect | 143 | 1.02 [0.66–1.37] | 64% |
| eat | Eat whenever food is close. | founder | eat +1.2 (often), when food is close | 152 | 1.01 [0.70–1.31] | 70% |
| rest | Never stop moving. | founder | rest -2.5 (never) | 176 | 1.00 [0.73–1.28] | 61% |
| follow | Keep your distance from other animals. | founder | no effect | 193 | 0.96 [0.72–1.20] | 66% |
| flee | Run away from anything that attacks you. | founder | flee +0.8 (names the action), when predator | 133 | 0.94 [0.63–1.25] | 61% |
| eat | No preference. | neutral | no effect | 165 | 0.93 [0.66–1.20] | 62% |

## The most common genes at the end, and where they came from

**rest: "Save energy by roasting when food is far."** — rest +0.8 (names the action), when food is far (4 of 45 living animals)

```text
Save energy by resting when food is far.  ← founder
Save energy by roasting when food is far.  ← "Make an unexpected change to this sentence."
```

**mate: "Mate with any nearby adult bird."** — mate +0.8 (names the action) (2 of 45 living animals)

```text
Mate with any nearby adult.  ← founder
Mate with any nearby adult bird.  ← "Make one random change to this sentence."
```

**rest: "Rest only when you feel edible."** — rest +0.8 (names the action) (2 of 45 living animals)

```text
Rest only when you feel safe.  ← founder
Rest only when you feel edible.  ← "Make an unexpected change to this sentence."
```

**eat: "Only look for snacks when energy is low."** — no effect (1 of 45 living animals)

```text
Only look for food when energy is low.  ← founder
Only look for snacks when energy is low.  ← "Make a small random edit to this sentence."
```

**eat: "No preference really."** — no effect (1 of 45 living animals)

```text
No preference.  ← neutral
No preference really.  ← "Randomly add a word to this sentence."
```

