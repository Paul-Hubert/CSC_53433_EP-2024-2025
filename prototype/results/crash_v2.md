# Boom-bust screening — crash_v2

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 20000 ticks, stopped at the first extinction. 9 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 0/8 | 10096 | 0 / 8 | 1-135 (mean 62) | 1-34 (mean 29) | 0.42 | 58% | 1% | 0 / 0 | 0 / 0 |
| cap25 | predator cap 34 → 25 | 0.1 | 8/8 | 20000 | 0 / 0 | 46-131 (mean 101) | 13-25 (mean 25) | 0.12 | 95% | 0% | 101 / 35 | 0 / 0 |
| cap20 | predator cap 34 → 20 | 0.1 | 8/8 | 20000 | 0 / 0 | 46-135 (mean 110) | 13-20 (mean 20) | 0.11 | 95% | 0% | 101 / 34 | 0 / 0 |
| cap15 | predator cap 34 → 15 | 0.1 | 7/8 | 20000 | 0 / 1 | 46-135 (mean 119) | 1-15 (mean 14) | 0.11 | 87% | 7% | 100 / 34 | 0 / 0 |
| terrain | noise terrain: water and mountains | 0.1 | 6/8 | 20000 | 0 / 2 | 2-135 (mean 107) | 1-34 (mean 32) | 0.23 | 83% | 8% | 100 / 38 | 0 / 0 |
| cover10 | cover on 10 % of cells, fleeing prey run to cover within 6 cells | 0.1 | 3/8 | 9436 | 3 / 2 | 1-135 (mean 64) | 2-34 (mean 30) | 0.39 | 62% | 1% | 101 / 52 | 0 / 0 |
| cover20 | cover on 20 %, seek 6 | 0.1 | 5/8 | 20000 | 0 / 3 | 1-135 (mean 80) | 1-34 (mean 32) | 0.26 | 75% | 1% | 100 / 46 | 0 / 0 |
| cover10_passive | cover on 10 %, prey don't run to it | 0.1 | 3/8 | 11192 | 4 / 1 | 1-135 (mean 68) | 1-34 (mean 31) | 0.29 | 77% | 1% | 99 / 42 | 0 / 0 |
| interf1 | kill_p / (1 + other predators within 3 cells of the prey) | 0.1 | 2/8 | 7780 | 3 / 3 | 1-135 (mean 68) | 1-34 (mean 28) | 0.40 | 53% | 2% | 99 / 55 | 0 / 0 |
| interf3 | kill_p / (1 + 3 x other predators within 3 cells) | 0.1 | 7/8 | 20000 | 1 / 0 | 3-135 (mean 83) | 4-34 (mean 29) | 0.23 | 38% | 1% | 102 / 66 | 0 / 0 |
| lg3 | predators breed only with ≥ 3 prey per predator | 0.1 | 8/8 | 20000 | 0 / 0 | 46-122 (mean 86) | 13-34 (mean 30) | 0.12 | 11% | 0% | 100 / 35 | 0 / 0 |
| lg5 | predators breed only with ≥ 5 prey per predator | 0.1 | 8/8 | 20000 | 0 / 0 | 46-135 (mean 106) | 13-27 (mean 22) | 0.10 | 0% | 0% | 101 / 34 | 0 / 0 |
| patches3 | ridges: 3 x 3 patches, 4-cell gaps | 0.1 | 0/8 | 2416 | 0 / 8 | 18-134 (mean 76) | 1-25 (mean 8) | 0.30 | 0% | 0% | 0 / 0 | 0 / 0 |
| patches4 | ridges: 4 x 4 patches, 2-cell gaps | 0.1 | 0/8 | 1100 | 0 / 8 | 11-99 (mean 58) | 1-20 (mean 8) | 0.25 | 0% | 0% | 0 / 0 | 0 / 0 |
| eggs | floors back on, refilled by eggs of the last 2 000 ticks (founders only if none) | 0.1 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 72) | 3-34 (mean 26) | 0.44 | 53% | 4% | 95 / 49 | 0 / 47 |
