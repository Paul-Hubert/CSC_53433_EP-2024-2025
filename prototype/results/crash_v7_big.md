# Boom-bust screening — crash_v7_big

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 10000 ticks, stopped at the first extinction. 4 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.2 | 8/8 | 10000 | 0 / 0 | 88-270 (mean 266) | 4-68 (mean 64) | 0.07 | 41% | 45% | 56 / 52 | 0 / 0 |
