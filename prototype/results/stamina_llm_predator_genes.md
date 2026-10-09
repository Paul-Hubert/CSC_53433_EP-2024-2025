# Which genes did best — check_stamina_llm

Run: 2000 ticks, 7 generations, 131 births, 113 deaths (predators 0%, starvation 97%, old age 3%), 76 mutations (78 tried), brain `llm`, seed 1234.
Living animals at the end: 32; their genes: 5% mutants, 98% still use a word of the animal's world, 4.5 words on average.

**Fitness** = mean number of offspring of the animals that carried the gene and died during the run (113 animals here), each divided by the mean of the animals that died in the same 5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the population over thousands of different texts, so genes are grouped by what the keyword brain reads in them (strength, action, conditions): texts that read the same behave the same. Marks: **★** best reading of its slot and clearly above average; **▲** best of the readings that are above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. Readings carried by fewer than 100 animals are left out.

## Marked genes

- **predator.hunt** — no steady leader.
- **predator.follow** — no steady leader.
- **predator.rest** — no steady leader.
- **predator.mate** — no steady leader.

## Selection by predators over time

| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–10000 | 30 | 0 | 110 | 0.00 |

## Slot by slot: what the keyword brain reads in each gene

Living share: share of the living animals carrying that reading at the start, at tick 0 and at the end.

### predator.hunt

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|

### predator.follow

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|

### predator.rest

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|

### predator.mate

| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |
|---|---|---|---|---|---|---|---|

## Single texts

The same fitness for each exact text carried by at least 100 animals (no text is clearly above average).

| slot | gene | origin | reading | carriers | fitness | killed by predators |
|---|---|---|---|---|---|---|

## The most common genes at the end, and where they came from

**predator.follow: "Stay close to predators."** — predator.follow +0.8 (names the action) (2 of 32 living animals)

```text
Stay close to other predators.  ← founder
Stay close to predators.  ← "Randomly remove a word from this sentence."
```

**predator.rest: "Maximum urgency."** — no effect (2 of 32 living animals)

```text
No preference.  ← neutral
Maximum urgency.  ← "Randomly change the meaning of this sentence a little."
```

**predator.follow: "No preference, honestly."** — no effect (1 of 32 living animals)

```text
No preference.  ← neutral
No preference, honestly.  ← "Make a small random edit to this sentence."
```

**predator.mate: "Some preference."** — no effect (1 of 32 living animals)

```text
No preference.  ← neutral
Some preference.  ← "Randomly change a few words in this sentence."
```

