# Boom-bust screening — crash_v5_litter_food

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 20000 ticks, stopped at the first extinction. 4 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 8/8 | 20000 | 0 / 0 | 43-135 (mean 134) | 13-34 (mean 34) | 0.05 | 95% | 76% | 42 / 31 | 0 / 0 |
| cover20 | cover on 20 %, seek 6 | 0.1 | 8/8 | 20000 | 0 / 0 | 46-135 (mean 134) | 10-34 (mean 34) | 0.05 | 96% | 78% | 44 / 27 | 0 / 0 |
