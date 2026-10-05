# Docs — Genetic Agents (prompt-genome evolutionary agents)

Documentation for the redesigned **Crowds & Evolution** lab: animals whose
genome is a list of short natural-language genes, read by an LLM that
decides what they do, mixed by crossover and rewritten by mutation.

| Folder | Read it when you want to… | Start at |
|---|---|---|
| [`system/`](system/README.md) | understand **how the system works today**: world, agents, genome, evolution, decision backends, data, metrics, configuration. Reference implementation: the Python prototype in [`../prototype/`](../prototype/). | [system/01-overview](system/01-overview.md) |
| [`unity/`](unity/README.md) | see the **proposed Unity architecture**: components, assets, pluggable strategies, tick pipeline, editor tooling, design patterns, extension recipes, change scenarios. Includes a [Claude Design prompt](unity/claude-design-prompt.md) to render it visually. | [unity/README](unity/README.md) |
| [`redesign/`](redesign/README.md) | know **why**: the vision, the assessment of the old lab, design decisions, the Phase 0 spike plan, and the progress log. | [redesign/09-progress-log](redesign/09-progress-log.md) |
| `ControllableCharacter.pdf` | the character-animation lab handout (unrelated to the genetic agents) | — |

## Suggested reading paths

- **Student, first day:** [system/01-overview](system/01-overview.md) →
  [unity/README](unity/README.md) (the five placement rules) →
  [unity/10-extension-recipes](unity/10-extension-recipes.md).
- **Implementing the Unity version:** [unity/01](unity/01-goals-and-principles.md) →
  [02](unity/02-architecture-overview.md) → [05](unity/05-simulation-loop.md) →
  [12](unity/12-testing-headless-migration.md), with
  [system/](system/README.md) open as the behavioural reference.
- **Course owner, deciding:** [redesign/09-progress-log](redesign/09-progress-log.md) →
  [redesign/07-open-questions](redesign/07-open-questions-and-roadmap.md) →
  [unity/11-change-scenarios](unity/11-change-scenarios.md).
