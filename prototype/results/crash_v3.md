# Boom-bust screening — crash_v3

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 20000 ticks, stopped at the first extinction. 20 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 0/8 | 10096 | 0 / 8 | 1-135 (mean 62) | 1-34 (mean 29) | 0.42 | 58% | 1% | 0 / 0 | 0 / 0 |
| baseline | current rules | 0.15 | 1/8 | 3846 | 2 / 5 | 1-135 (mean 62) | 1-34 (mean 26) | 0.45 | 56% | 6% | 100 / 51 | 0 / 0 |
| baseline | current rules | 0.2 | 0/8 | 5347 | 6 / 2 | 1-135 (mean 58) | 1-34 (mean 29) | 0.44 | 60% | 1% | 0 / 0 | 0 / 0 |
| cap25 | predator cap 34 → 25 | 0.1 | 8/8 | 20000 | 0 / 0 | 46-131 (mean 101) | 13-25 (mean 25) | 0.12 | 95% | 0% | 101 / 35 | 0 / 0 |
| cap25 | predator cap 34 → 25 | 0.15 | 8/8 | 20000 | 0 / 0 | 40-130 (mean 99) | 13-25 (mean 25) | 0.13 | 96% | 0% | 101 / 32 | 0 / 0 |
| cap25 | predator cap 34 → 25 | 0.2 | 7/8 | 20000 | 0 / 1 | 24-135 (mean 96) | 1-25 (mean 23) | 0.18 | 88% | 3% | 101 / 31 | 0 / 0 |
| interf3 | kill_p / (1 + 3 x other predators within 3 cells) | 0.1 | 7/8 | 20000 | 1 / 0 | 3-135 (mean 83) | 4-34 (mean 29) | 0.23 | 38% | 1% | 102 / 66 | 0 / 0 |
| interf3 | kill_p / (1 + 3 x other predators within 3 cells) | 0.15 | 2/8 | 9506 | 1 / 5 | 1-135 (mean 60) | 1-34 (mean 27) | 0.46 | 42% | 3% | 101 / 59 | 0 / 0 |
| interf3 | kill_p / (1 + 3 x other predators within 3 cells) | 0.2 | 1/8 | 6590 | 3 / 4 | 1-135 (mean 62) | 1-34 (mean 27) | 0.43 | 55% | 3% | 98 / 58 | 0 / 0 |
| lg3 | predators breed only with ≥ 3 prey per predator | 0.1 | 8/8 | 20000 | 0 / 0 | 46-122 (mean 86) | 13-34 (mean 30) | 0.12 | 11% | 0% | 100 / 35 | 0 / 0 |
| lg3 | predators breed only with ≥ 3 prey per predator | 0.15 | 8/8 | 20000 | 0 / 0 | 42-113 (mean 84) | 13-34 (mean 30) | 0.12 | 8% | 0% | 101 / 35 | 0 / 0 |
| lg3 | predators breed only with ≥ 3 prey per predator | 0.2 | 8/8 | 20000 | 0 / 0 | 33-114 (mean 83) | 14-34 (mean 29) | 0.14 | 9% | 0% | 101 / 32 | 0 / 0 |
| lg5 | predators breed only with ≥ 5 prey per predator | 0.1 | 8/8 | 20000 | 0 / 0 | 46-135 (mean 106) | 13-27 (mean 22) | 0.10 | 0% | 0% | 101 / 34 | 0 / 0 |
| lg5 | predators breed only with ≥ 5 prey per predator | 0.15 | 8/8 | 20000 | 0 / 0 | 44-134 (mean 106) | 13-27 (mean 22) | 0.10 | 0% | 0% | 100 / 32 | 0 / 0 |
| lg5 | predators breed only with ≥ 5 prey per predator | 0.2 | 8/8 | 20000 | 0 / 0 | 43-135 (mean 106) | 14-27 (mean 22) | 0.10 | 0% | 0% | 100 / 31 | 0 / 0 |
| lg4 | predators breed only with ≥ 4 prey per predator | 0.1 | 8/8 | 20000 | 0 / 0 | 46-128 (mean 99) | 13-33 (mean 26) | 0.10 | 0% | 0% | 101 / 34 | 0 / 0 |
| lg4 | predators breed only with ≥ 4 prey per predator | 0.15 | 8/8 | 20000 | 0 / 0 | 44-134 (mean 97) | 13-33 (mean 26) | 0.11 | 0% | 0% | 101 / 33 | 0 / 0 |
| lg4 | predators breed only with ≥ 4 prey per predator | 0.2 | 8/8 | 20000 | 0 / 0 | 43-125 (mean 97) | 14-32 (mean 26) | 0.11 | 0% | 0% | 101 / 30 | 0 / 0 |
| seen5 | predators breed only with ≥ 5 prey within their vision | 0.1 | 0/8 | 3518 | 3 / 5 | 1-135 (mean 56) | 1-34 (mean 28) | 0.45 | 51% | 1% | 0 / 0 | 0 / 0 |
| seen5 | predators breed only with ≥ 5 prey within their vision | 0.15 | 0/8 | 8066 | 4 / 4 | 1-135 (mean 60) | 1-34 (mean 28) | 0.45 | 54% | 2% | 0 / 0 | 0 / 0 |
| seen5 | predators breed only with ≥ 5 prey within their vision | 0.2 | 1/8 | 12771 | 2 / 5 | 2-135 (mean 67) | 1-34 (mean 27) | 0.46 | 55% | 5% | 101 / 45 | 0 / 0 |
| seen10 | predators breed only with ≥ 10 prey within their vision | 0.1 | 2/8 | 11704 | 1 / 5 | 1-135 (mean 69) | 1-34 (mean 28) | 0.38 | 54% | 2% | 100 / 42 | 0 / 0 |
| seen10 | predators breed only with ≥ 10 prey within their vision | 0.15 | 3/8 | 7114 | 1 / 4 | 1-135 (mean 64) | 1-34 (mean 28) | 0.39 | 51% | 1% | 99 / 44 | 0 / 0 |
| seen10 | predators breed only with ≥ 10 prey within their vision | 0.2 | 1/8 | 12708 | 3 / 4 | 1-135 (mean 66) | 1-34 (mean 29) | 0.39 | 54% | 1% | 100 / 41 | 0 / 0 |
| seen15 | predators breed only with ≥ 15 prey within their vision | 0.1 | 8/8 | 20000 | 0 / 0 | 25-134 (mean 78) | 6-34 (mean 31) | 0.21 | 51% | 0% | 101 / 41 | 0 / 0 |
| seen15 | predators breed only with ≥ 15 prey within their vision | 0.15 | 6/8 | 20000 | 1 / 1 | 3-135 (mean 72) | 1-34 (mean 30) | 0.26 | 48% | 1% | 100 / 39 | 0 / 0 |
| seen15 | predators breed only with ≥ 15 prey within their vision | 0.2 | 6/8 | 20000 | 0 / 2 | 1-135 (mean 71) | 1-34 (mean 30) | 0.29 | 45% | 0% | 100 / 40 | 0 / 0 |
| lg3_eggs | lg3 + floors refilled by eggs | 0.1 | 8/8 | 20000 | 0 / 0 | 46-122 (mean 86) | 13-34 (mean 30) | 0.12 | 11% | 0% | 100 / 35 | 0 / 0 |
| lg3_eggs | lg3 + floors refilled by eggs | 0.15 | 8/8 | 20000 | 0 / 0 | 42-113 (mean 84) | 13-34 (mean 30) | 0.12 | 8% | 0% | 101 / 35 | 0 / 0 |
| lg3_eggs | lg3 + floors refilled by eggs | 0.2 | 8/8 | 20000 | 0 / 0 | 33-114 (mean 83) | 14-34 (mean 29) | 0.14 | 9% | 0% | 101 / 32 | 0 / 0 |
| seen10_eggs | seen10 + floors refilled by eggs | 0.1 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 73) | 3-34 (mean 29) | 0.34 | 58% | 2% | 97 / 43 | 0 / 11 |
| seen10_eggs | seen10 + floors refilled by eggs | 0.15 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 68) | 3-34 (mean 28) | 0.41 | 49% | 3% | 96 / 44 | 0 / 18 |
| seen10_eggs | seen10 + floors refilled by eggs | 0.2 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 68) | 3-34 (mean 28) | 0.40 | 52% | 2% | 97 / 40 | 0 / 23 |
| eggs | floors back on, refilled by eggs of the last 2 000 ticks (founders only if none) | 0.1 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 72) | 3-34 (mean 26) | 0.44 | 53% | 4% | 95 / 49 | 0 / 47 |
| eggs | floors back on, refilled by eggs of the last 2 000 ticks (founders only if none) | 0.15 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 68) | 3-34 (mean 27) | 0.47 | 56% | 3% | 95 / 49 | 0 / 51 |
| eggs | floors back on, refilled by eggs of the last 2 000 ticks (founders only if none) | 0.2 | 8/8 | 20000 | 0 / 0 | 10-135 (mean 61) | 3-34 (mean 27) | 0.53 | 55% | 2% | 88 / 50 | 0 / 109 |
