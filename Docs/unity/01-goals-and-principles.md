# 01 — Goals & principles

What the Unity framework must achieve, the constraints inherited from the
prototype, and the rules every file follows.

## Goals, in priority order

1. **Readable.** A Master 2 student can trace one decision — from sensing to
   the LLM prompt to the chosen action to the energy cost — by opening fewer
   than ten short files, all named after what they do.
2. **Extensible without framework edits.** Adding a sense, an action, a
   locus, a backend, a mutation operator, a world rule or a visualiser is
   done by **adding** a file (and maybe an asset). Framework code is closed
   for modification and open for extension.
3. **Graceful under big change.** A predator that evolves, continuous
   terrain, a physics/DRL body, VR interaction, "LLM at birth" instead of
   "LLM per decision": each touches one or two seams, never the whole design
   ([11](11-change-scenarios.md)).
4. **Scientifically usable.** Deterministic given seeds and caches, headless
   batch runs, logs in the same format as the Python prototype so its
   analysis code keeps working.
5. **Helpful editor.** Mistakes are caught before Play by validators that
   explain the fix. Every system can be seen in the Scene view or in a window.

Non-goals: maximum agent count (the LLM, not the engine, is the bottleneck;
populations are 20–60), DOTS/ECS, networking, a polished game.

## Constraints inherited from the prototype

| Constraint | Why | Consequence in Unity |
|---|---|---|
| **Lockstep, sim-time ticks** | Decisions can take seconds; slow ≠ wrong | One `SimulationRunner` drives everything; agent components have **no `Update()`** |
| **Named RNG streams** | Reproducible runs; changing one subsystem doesn't reshuffle another | `RngStreams.Get("sampling")`; `UnityEngine.Random` is banned in simulation code |
| **Discrete observations** | Cache hit rate 60–75 %, short prompts | Senses emit labelled buckets (`near`, `far`), not floats |
| **Homologous loci** | Crossover only makes sense if locus *i* means the same in both parents | Genome layout is data (`GenomeSchema`), shared by the species |
| **Every model call cached** | Cost and reproducibility | Persistent cache is a decorator on the decision chain and on the LLM mutation operator |
| **Keys never in files** | Shared repo, students | API key only from an environment variable; the editor shows whether it is set, never its value |

## Lessons from the prototype, built in

Writing the [system reference](../system/README.md) surfaced a few
prototype pitfalls. The Unity design closes each one structurally rather
than with a fix in one place:

| Prototype pitfall | Unity rule |
|---|---|
| A failed LLM call returns a uniform distribution that is then **cached like a real answer**, permanently | Distributions carry a `Source`; memo and persistent caches **never store** `Fallback` results ([06](06-decisions.md)) |
| Control C4 swaps the founders, but `founder_reintroduce` still draws **real** founder genes | One source of truth: every consumer (founders, immigrants, `FounderReintroduceOperator`) reads `species.founders`, and `UseFounderPool` replaces that reference |
| Death after an attack drains energy is logged as **starvation** | `Metabolism.Spend(amount, source)` remembers the last loss source; `DeathCause` is attributed (`attacked` vs `starvation`) |
| `summary.json` lacks the world seed, config and model digests | Run folders always include `run_info.json`: profile hash, seeds, world seed, git commit, backend ids, model digests, prompt hashes |
| The mutation prompt says 12 words even for 15-word temperament genes | Limits come from the `LocusDefinition` only; prompts are filled from it |
| Actions, loci and observation fields are hard-coded tuples/dataclasses | Derived from components and assets (`ActionSet`, `GenomeSchema`, `FieldSpec`) |
| Rule-based keywords match substrings (`strong` ⊂ `strongest`) | `RuleBook` patterns are whole-word by default; golden tests pin the behaviour |

## Principles

**P1 — Composition first.** An agent is the sum of its components. What it
can sense is the list of `AgentSense` components; what it can do is the list
of `AgentAction` components. Nothing is hard-coded in a central enum.

**P2 — Data defines kinds.** Species, loci, founder alleles, prompt
wording, phrase books and experiment conditions are assets, so a new kind
of creature or a new experiment condition is usually no code at all.

**P3 — Behaviour is pluggable through interfaces.** Every algorithmic
choice sits behind a small interface (`IDecisionBackend`, `ICrossover`,
`IMutationOperator`, `IAgentCondition`, `ISimulationSystem`, `IWorld`,
`ILocomotion`). Implementations are chosen in the Inspector from a dropdown
that lists every class implementing the interface, including students' own.

**P4 — Wrap, don't modify.** Cross-cutting features (caching, temperature,
fallbacks, tracing, experimental controls) are **decorators** around a
backend or operator, never flags inside it.

**P5 — Explicit context, no singletons.** Systems, senses and actions
receive a context object (`SimContext`, `SenseContext`, `ActionContext`) with
exactly what they may use. No `FindObjectOfType`, no static mutable state.
Reading a method's parameters tells you its dependencies.

**P6 — Logic and presentation are separate.** Simulation components never
render. Views (meshes, animators, labels) read state and listen to events.
Gizmos and debug drawing live in the Editor assembly. Turning off rendering
changes nothing in the results.

**P7 — Observers never change the simulation.** Event subscribers (loggers,
dashboards, views) are read-only. Anything that changes the world is a
system, so the tick order stays visible in one list.

**P8 — Validate early, explain the fix.** Each asset and component can
validate itself, and cross-object rules run before Play and before batch
runs. An error message says what is wrong, why it matters, and how to fix it.

## Readability rules (enforced by review and, where possible, by validators)

1. One public type per file; file name = type name.
2. Suffixes say what a type is: `*Sense`, `*Action`, `*Locomotion`,
   `*Backend`, `*Decorator`, `*Operator`, `*Guard`, `*Condition`, `*System`,
   `*Definition` / `*Profile` (assets), `*Rule` (validation), `*Window`,
   `*Drawer`, `*Gizmos`.
3. Namespaces follow folders: `GeneticAgents.Core.Genetics`,
   `GeneticAgents.Agents`, `GeneticAgents.Decisions`, `GeneticAgents.Evolution`,
   `GeneticAgents.World`, `GeneticAgents.Editor.*`.
4. A student-facing class fits on one screen (≈ 80 lines). If it doesn't, it
   is doing two jobs.
5. Tunables are `[SerializeField]` with `[Tooltip]` and a unit in the name
   or tooltip (`visionCells`, `costPerTick`), never magic numbers.
6. Every extension point ships with: an interface, a base class (Template
   Method), one sample implementation, a script template, a validation hook
   and a gizmo/inspector hook.
7. Comments explain *why*, and link to the course concept
   (`// Homologous loci: see Docs/unity/07`). The code says *what*.
8. No `async void`; asynchronous work returns `Task`/`Awaitable` and is
   awaited by the runner.

## Target Unity version

Recommended: **Unity 6 LTS** (open question 5 in
[07-open-questions](../redesign/07-open-questions-and-roadmap.md)). It gives
`Awaitable`, Scene-view Overlays, UI Toolkit inspectors by default, and the
Inference Engine for on-device ONNX models. Everything here also works on
2021.3 with `Task` instead of `Awaitable` and IMGUI drawers; the differences
are noted where they matter.
