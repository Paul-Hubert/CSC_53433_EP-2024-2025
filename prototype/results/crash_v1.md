# Boom-bust screening — crash_v1

Keyword brain, full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 4 seeds x 20000 ticks, stopped at the first extinction. 5 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 3/4 | 20000 | 1 / 0 | 8-135 (mean 107) | 13-34 (mean 34) | 0.15 | 95% | 0% | 104 / 28 |
| baseline | current rules | 0.2 | 4/4 | 20000 | 0 / 0 | 45-135 (mean 105) | 14-34 (mean 34) | 0.16 | 96% | 0% | 104 / 26 |
| baseline | current rules | 0.3 | 2/4 | 11366 | 1 / 1 | 1-135 (mean 77) | 3-34 (mean 31) | 0.34 | 77% | 0% | 104 / 26 |
| cap20 | predator cap 34 → 20 | 0.1 | 4/4 | 20000 | 0 / 0 | 49-135 (mean 120) | 5-20 (mean 20) | 0.09 | 94% | 3% | 104 / 30 |
| cap20 | predator cap 34 → 20 | 0.2 | 4/4 | 20000 | 0 / 0 | 45-135 (mean 119) | 5-20 (mean 20) | 0.09 | 95% | 2% | 104 / 27 |
| cap20 | predator cap 34 → 20 | 0.3 | 4/4 | 20000 | 0 / 0 | 43-135 (mean 118) | 3-20 (mean 20) | 0.10 | 94% | 2% | 103 / 29 |
| food_low | less food (paradox of enrichment) | 0.1 | 0/4 | 1054 | 3 / 1 | 0-72 (mean 35) | 3-34 (mean 29) | 0.58 | 54% | 0% | 0 / 0 |
| food_low | less food (paradox of enrichment) | 0.2 | 0/4 | 1979 | 3 / 1 | 1-106 (mean 35) | 4-34 (mean 24) | 0.58 | 32% | 0% | 0 / 0 |
| food_low | less food (paradox of enrichment) | 0.3 | 0/4 | 1751 | 2 / 2 | 1-130 (mean 36) | 3-34 (mean 26) | 0.54 | 43% | 0% | 0 / 0 |
| terrain | noise terrain: water and mountains | 0.1 | 4/4 | 20000 | 0 / 0 | 45-135 (mean 130) | 14-34 (mean 34) | 0.09 | 96% | 47% | 87 / 29 |
| terrain | noise terrain: water and mountains | 0.2 | 3/4 | 20000 | 0 / 1 | 6-135 (mean 104) | 14-34 (mean 32) | 0.23 | 76% | 31% | 88 / 31 |
| terrain | noise terrain: water and mountains | 0.3 | 4/4 | 20000 | 0 / 0 | 22-135 (mean 128) | 4-34 (mean 34) | 0.12 | 95% | 41% | 89 / 28 |
