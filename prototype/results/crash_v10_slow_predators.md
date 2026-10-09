# Boom-bust screening — crash_v10_slow_predators

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 10000 ticks, stopped at the first extinction. 8 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | all animals at the cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|
| pred_litter12 | predator litters of 1-2 (prey 2-4) | 0.5 | 1/8 | 4316 | 1 / 6 | 1-499 (mean 323) | 1-304 (mean 85) | 0.46 | 28% | 58 / 43 | 0 / 0 |
| pred_slow | predators: one cub per mating, adult at 300 ticks (prey 2-4, 150) | 0.5 | 0/8 | 2144 | 0 / 8 | 83-499 (mean 425) | 1-73 (mean 24) | 0.28 | 46% | 0 / 0 | 0 / 0 |
