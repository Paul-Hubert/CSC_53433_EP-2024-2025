# Boom-bust screening — crash_v6_migrate

Brain: llmtable (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; 8 seeds x 20000 ticks, stopped at the first extinction. 3 min.

| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) | predators min-max (mean) | prey CV | predators at cap | prey at cap | generation prey / pred (lasted) | founder newcomers / eggs hatched per run |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline | current rules | 0.1 | 8/8 | 20000 | 0 / 0 | 43-135 (mean 133) | 10-34 (mean 33) | 0.05 | 60% | 44% | 114 / 106 | 0 / 0 |
