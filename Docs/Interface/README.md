# Interface — the contract of the evolving-animals system

This folder describes **what the system does**, as a contract that a Unity
implementation must keep, and then proposes **how to build it in Unity** so that
students can read it and extend it.

The behaviour comes from the Python prototype (`prototype/promptevo`, state of
commit `1324b43`, 2026-10-09). The contract records that behaviour as a set of
**rules and reference modules**, not as the prototype's code: no Python class,
function or file layout is part of the contract. Python numbers appear as
**reference values** (the defaults a Unity scene starts from), and exact
numeric agreement with Python is **not** required.

The Unity system differs from the prototype on purpose in a few places, decided
with the course owner in two rounds on 2026-10-09 (tables below): space is
continuous, everything is a replaceable component, any number of species eat
each other through a food web, genes can be sentences or numbers, and hiding in
cover is its own behaviour.

## Contents

### Part A — the contract (what must hold)

| # | Document | Covers |
|---|---|---|
| 01 | [Concepts and glossary](01-concepts-and-glossary.md) | the loop, every term used in the other documents |
| 02 | [Space and time](02-space-and-time.md) | continuous positions, terrain, walkability, distances, ticks, waiting for the brain |
| 03 | [Environment](03-environment.md) | resource layers (food), regrowth, cover, carcasses and other entities |
| 04 | [Species and the food web](04-species-and-food-web.md) | species, who eats whom, threats, names, species created at run time |
| 05 | [Animals, stats and life cycle](05-animals-stats-and-life-cycle.md) | animal state, stats (energy, stamina, …), traits, metabolism, busy states, death |
| 06 | [Senses and observations](06-senses-and-observations.md) | discrete observations, distance bands, situation text, batched raycasts, camera senses |
| 07 | [Actions and locomotion](07-actions-and-locomotion.md) | actions, search fallback, movement, stamina budget, interactions, act order |
| 08 | [Decisions, brains and prompts](08-decisions-brains-and-prompts.md) | when animals decide, the brain interface, the prompt, sampling, memo, cache, failures, reference brains |
| 09 | [Genes, alleles and genomes](09-genes-alleles-and-genomes.md) | text and number genes, expression, alleles, registry, founder pools, crossover |
| 10 | [Mutation](10-mutation.md) | rates, operators (LLM instruction deck, numeric rules), guards, redraws, asynchronous mutation |
| 11 | [Reproduction and population](11-reproduction-and-population.md) | mating, litters, eggs and incubation, caps, migration, floors, newcomers |
| 12 | [Tick order and determinism](12-tick-order-and-determinism.md) | the phase list, act order policies, random streams, event hash, replay |
| 13 | [Outputs and recording](13-outputs-and-recording.md) | events, stats, alleles, summary, run info |
| 14 | [Configuration reference](14-configuration-reference.md) | every parameter with its reference value and unit |
| 15 | [Controls, metrics and gates](15-controls-metrics-and-gates.md) | experimental controls, sensitivity metrics, gates G1–G5 |

### Part B — the proposed Unity implementation

| # | Document | Covers |
|---|---|---|
| 20 | [Unity architecture](20-unity-architecture.md) | the GameObject tree, base classes, data layout, the tick loop, services, assemblies, Unity 6000.3 notes |
| 21 | [Editor tooling](21-editor-tooling.md) | validation, inspectors, windows, gizmos, run controls |
| 22 | [Extending: recipes for students](22-extending-recipes.md) | add a sense, an action, a gene, a stat, a brain, a mutation, a phase, a resource, a species |

### Part C — verification

| # | Document | Covers |
|---|---|---|
| 30 | [Tests](30-tests.md) | unit, regression and integration tests for every system, mapped to rule IDs |
| 31 | [Scenarios](31-scenarios.md) | test worlds with different options, genes, mutations, senses and actions |
| 32 | [Integrity prompts and CI](32-integrity-prompts-and-ci.md) | brain integrity checks, prompts for coding agents, the CI tiers (with real LLMs) |
| 40 | [Open questions](40-open-questions.md) | blind spots and decisions still to take |

## How to read the rules

Each contract document lists numbered rules:

> **SENSE-01 (MUST)** A sense reads one animal and the world and returns one
> token from a finite list declared before the run.

- **MUST**: every implementation keeps it; a test checks it.
- **SHOULD**: the reference implementation keeps it; another implementation
  may deviate if it says so in its own documentation.
- **MAY**: allowed, optional.
- **Reference**: the behaviour or value of the reference modules (the Lab 1
  ecology). It is a default, not an obligation.

Rule ID prefixes:

| Prefix | Document | | Prefix | Document |
|---|---|---|---|---|
| `CORE` | 01 | | `GENE` | 09 |
| `SPACE` | 02 | | `MUT` | 10 |
| `ENV` | 03 | | `REPRO`, `POP` | 11 |
| `SPEC` | 04 | | `TICK`, `RAND` | 12 |
| `ANIM` | 05 | | `OUT` | 13 |
| `SENSE` | 06 | | `CFG` | 14 |
| `ACT`, `MOVE` | 07 | | `CTRL` | 15 |
| `DEC`, `PROMPT` | 08 | | `ARCH`, `EDIT` | 20, 21 |

Every test in [30](30-tests.md) names the rules it covers, and every MUST rule
has at least one test.

## Units

| Quantity | Unit | Python prototype |
|---|---|---|
| Distance | meter (m), measured on the horizontal plane | one grid cell |
| Time | tick (one simulation step) | one tick |
| Speed | meters per tick | cells per tick |
| Energy, stamina | points; stamina = meters an animal can move before it must stop | points; cells |

The reference values convert one cell to one meter and are otherwise **kept
exactly as in the prototype** (owner decision). Distances in Python are
Chebyshev (a diagonal step counts as 1); here they are Euclidean and movement
goes in straight lines, so a vision disc of 20 m covers about 75 % of the area
of Python's 20-cell square. The first accepted Unity runs become the new
reference numbers ([30](30-tests.md), R-02). Texts the brain reads say
"meters".

## Decisions taken with the course owner (2026-10-09, first round)

| Topic | Decision |
|---|---|
| Space | **Continuous**: float positions changed by direction and speed vectors, on a Unity Terrain with a heightmap and vegetation. Grids exist only inside layers (food cells, cover cells, spatial index). No 8-neighbour moves. |
| Movement | Replaceable: flat ground, terrain-aware, or a NavMesh baked on the terrain. |
| Genes | An interface. A gene can be a **sentence** that goes into the brain's prompt or a **number** that changes a trait directly (e.g. a stamina gene). Any number of genes. |
| Extensibility | Energy, stamina, levels, distance bands, litter sizes, mutation rules (LLM prompts or manual rules), senses and actions are all modules a student adds by subclassing a base class and placing the component in a species' hierarchy. |
| Python parity | Not required. Behaviour matters; Python values are reference defaults. |
| Waiting for the brain | Lockstep: the simulation waits for the answers (a decision is never made on old observations). Freezing `FixedUpdate` during a batch is acceptable; [02](02-space-and-time.md) and [20](20-unity-architecture.md) add a wait mode that keeps the editor responsive with identical results. |
| Animals | **Data**, processed in batches by systems. GameObjects are only their visible bodies. |
| Nesting | A species nested inside another species is an editor error. |
| Species | Not hard-coded. Any species may eat any species, its own included; genes and observations may name species ("Run from any wolf you see."); five or six species, or species created at run time. |
| Act order | Configurable: species in turn (as in Python) or all animals of all species mixed in one random order. |
| Mutation | A system of its own. It may be asynchronous: a mating lays eggs that hatch a few ticks later, when the mutator's answer is back. |
| Species class | One class, which may be subclassed; composition with components is the main way. |
| World | Holds an ordered list of tick phase components, each replaceable. |
| Environment | Food, cover, carcasses, walkability, caps and floors and the brain service are components that can be replaced or removed. Carcasses may move. Cover is optional and replaceable. Hiding in cover is an action of its own, separate from fleeing, with its own sense and gene. |
| Raycasts | Allowed inside a tick phase, batched. |
| CI | May include real LLM calls; duration is not a concern. |
| Unity | 6000.3, to be upgraded soon: version-specific code stays isolated. |
| Defaults | One text gene per action (free genes allowed), prompts generated from components (frozen text optional), log files in Python's formats, Python's phase order as the default, reference brains = random, keyword, Ollama points, JEV choice; owner-rejected options documented as off; Newtonsoft JSON and the Unity Test Framework; numbered rules; contract pinned to `1324b43`. |
| Integrity prompts | Both: checks that the brain reads genes, and prompts for coding agents that audit an implementation; used in CI and by hand. |

## Decisions, second round (2026-10-09)

| Topic | Decision |
|---|---|
| Space and distance | Continuous, Euclidean distance, straight-line movement. |
| Speed | Set per species (walking and running speeds of the species' locomotion component). |
| Reference values | Exactly the prototype's (one cell = one meter); no number genes in the reference species. |
| Units in texts | "meters". |
| Prompts | The answer instruction is hard-coded in each brain, as in the prototype. Everything else (header, the line explaining each action, rule lines) is set in the inspector. |
| Observations | Their wording is written in each sense's code; labels and thresholds in the inspector. Distance only for now; angles later. |
| Act order | Species in turn by default. Simultaneous order: contested items go to a random winner. |
| Breeding | In its own phase, after the act phase. |
| Eggs | No incubation by default; a few ticks of incubation can hide the mutator's latency. |
| Species names | `prey` and `predator`. |
| Speciation | None; later. Adding species at run time stays possible. |
| Cannibals | Flee from their own kind. Knowing whether a cannibal is following you is a sense a student can write. |
| Hide and flee | Separate actions, separate genes, separate senses (cover). |
| Gates | G2 stays even though current models fail it. |
| Neutral gene, founder pools, caps | Configured on their components (the gene, the cap rule…). |
| Teaching path | The reference modules are written exactly like student modules. Students first reimplement existing components against their tests, then invent their own. |
| Real time | Not by default; maybe later. |
| Movement | Kinematic by default (no rigid bodies, no physics). An action only gives a direction; the species' locomotion component decides the actual movement, and whatever it does is the tick's result. Locomotion is a component that can be inherited: slopes, terrain costs, NavMesh, physics come later as subclasses. |
| Camera senses | Later. |
| Determinism | Per machine only; results are never compared across machines. |
| Answer cache | A local JSON file for now. |
| Python golden fixtures | Dropped. |
| Scale | Not needed now; the architecture keeps the path open. |
| Unity | The owner upgrades the project to 6000.3 before work starts. |
| CI hardware | The owner's machine (the one that ran the prototype) as a self-hosted runner. |

## Decisions, third round (2026-10-10)

| Topic | Decision |
|---|---|
| Where it runs | All local on the owner's computer for now: JEV and the mutator served there; the default brain is JEV. |
| Keyword brain | Not included in the Unity system. It stays documented as the prototype's brain and as a possible student exercise. Fast tests use the random brain and a test-only scripted brain. |
| Edibility | Anything can be eaten or not, decided by components: the eaten thing carries an **edible** component (how it is eaten, how much energy it holds), the eater's **diet** component lists what it eats. Eggs have no edible component, so they can't be eaten until a student adds one. Everything stays modifiable. |

## Status

Written 2026-10-09, revised the same day after the second round of decisions.
Nothing in Unity exists yet; the project moves to Unity 6000.3 first. The
Python prototype keeps evolving; when it changes behaviour, update the matching
rule and its reference value, and note the commit.
