# Boom-bust screening — crash_v2_check

Brain: llmlike (llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 20000 ticks, stopped at the first extinction. 4 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 8/8 | 20000 | 0 / 0 | 48-135 (mean 131) | 14-34 (mean 34) | 0.07 | 97% | 40% | 96 / 28 | 0 / 0 |
| cap20 | predator cap 34 → 20 | 0.1 | 8/8 | 20000 | 0 / 0 | 48-135 (mean 134) | 5-20 (mean 20) | 0.06 | 96% | 71% | 61 / 29 | 0 / 0 |
