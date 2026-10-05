# Unity architecture — Genetic Agents framework (proposal)

How to rebuild the prompt-genome evolutionary agents ([system reference](../system/README.md))
in Unity so that students can **read** it in an afternoon and **extend** it
without editing framework code. Status: **proposal**, nothing below is
implemented in Unity yet. The Python prototype in [`prototype/`](../../prototype/)
is the reference behaviour.

| # | Document | What it holds |
|---|----------|---------------|
| 01 | [Goals & principles](01-goals-and-principles.md) | Constraints, the **five placement rules**, readability rules. Read first. |
| 02 | [Architecture overview](02-architecture-overview.md) | Layers, assemblies, folder layout, dependency rule, runtime object graph, Python → Unity map. |
| 03 | [Agent anatomy](03-agent-anatomy.md) | The agent prefab as a stack of components: senses, actions, brain, metabolism, locomotion, views. |
| 04 | [Data assets](04-data-assets.md) | ScriptableObjects: profile, species, genome schema, loci, founder pool, prompt, phrase book, experiment. |
| 05 | [Simulation loop](05-simulation-loop.md) | Runner, phases, systems, lockstep async barrier, RNG streams, events, headless mode. |
| 06 | [Decisions](06-decisions.md) | Open observation model, renderers, decision requests, the **backend decorator chain**, option A/B. |
| 07 | [Evolution](07-evolution.md) | Mating conditions, crossover, mutation pipeline with guards, development, population policy, controls. |
| 08 | [Pattern catalogue](08-patterns-catalogue.md) | Every pattern used, where, why, and how patterns **combine**. |
| 09 | [Editor tooling](09-editor-tooling.md) | Validation framework, Simulation Doctor, inspectors, windows, scene overlays, script templates. |
| 10 | [Extension recipes](10-extension-recipes.md) | Step-by-step with code: new sense, action, backend, decorator, operator, system, rule, visualiser. |
| 11 | [Change scenarios](11-change-scenarios.md) | How the design absorbs big changes: predators that evolve, terrain, DRL/physics locomotion, VR, option B, NN genomes… |
| 12 | [Testing, headless runs & migration](12-testing-headless-migration.md) | Test pyramid, Python parity, batch CLI, build order from today's repo. |
| — | [Claude Design prompt](claude-design-prompt.md) | A prompt to render this architecture as an editable visual design. |

## The architecture in one picture

```text
                          ┌──────────────── SimulationProfile (asset) ────────────────┐
                          │ world · species[] · systems[] · seeds · recorders          │
                          └───────┬───────────────────────┬───────────────────────────┘
                                  │                       │
          ┌───────────────────────▼───┐        ┌──────────▼──────────────────────────────┐
          │ SpeciesDefinition (asset) │        │ Simulation (plain C#) — one tick:        │
          │  prefab ─────────────┐    │        │  World → Decide → Act → Interact →       │
          │  GenomeSchema/Loci   │    │        │  Lifecycle → Record      (systems[])     │
          │  FounderPool         │    │        └──────────┬───────────────────────────────┘
          │  decision chain ◄─┐  │    │                   │ iterates
          │  evolution rules  │  │    │                   ▼
          └───────────────────┼──┼────┘        ┌────────────────────────────────────────┐
                              │  └────────────►│ Agent prefab (GameObject)               │
   [SerializeReference]       │                │  AgentRoot  Metabolism  Brain           │
   strategies picked from     │                │  Senses: Energy Food Predator Neighbour │
   a dropdown:                │                │  Actions: Eat Flee Follow Wander …      │
   Trace→Temperature→Memo→    │                │  Locomotion (grid | kinematic | physics)│
   Fallback(Ollama, Rules)  ──┘                │  Views (render only, optional)          │
                                               └────────────────────────────────────────┘
   Editor assembly (separate): Validation rules · Simulation Doctor · inspectors ·
   Genome Browser · Brain Debugger · Gene Playground · Scene overlays & gizmos
```

## The five placement rules (the whole design in one table)

| You are writing… | It goes… | Unity mechanism | Example |
|---|---|---|---|
| State or a capability of **one agent** | a **component** on the agent prefab | `MonoBehaviour` deriving from `AgentSense`, `AgentAction`, … | `ThirstSense`, `DrinkAction` |
| **Shared data**: content, tunables, definitions | an **asset** | `ScriptableObject` (`*Definition`, `*Profile`) | `FounderPool`, `SpeciesDefinition` |
| A **swappable rule or algorithm** | a plain C# class implementing an interface, **picked from a dropdown** | `[SerializeReference, SubclassSelector]` | `UniformCrossover`, `TemperatureDecorator` |
| A **world-wide process** run every tick | a **simulation system** in the profile's list | `ISimulationSystem` | `FoodRegrowthSystem`, `WeatherSystem` |
| Anything that **checks, shows or manages** | the **Editor assembly** | `ValidationRule`, `Editor`, `EditorWindow`, `[DrawGizmo]` | `LociMatchActionsRule` |

A student who knows these five rules knows where every file belongs and
where to look for any behaviour.
