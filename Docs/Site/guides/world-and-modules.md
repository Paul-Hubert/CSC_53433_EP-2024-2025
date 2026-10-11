---
uid: guide-world-and-modules
---

# The world and its modules

A simulation is one GameObject with a <xref:EvoSim.World> component, and everything under it. Each feature is a
component, a **module**: a sense, an action, a food layer, a brain, a tick phase. To add a feature you add a component; to
switch it off you disable it. The World finds its modules, sets them up in a fixed order, checks the configuration, and
then runs ticks. This page explains how the World reads its tree and what it calls, when.

## The tree of a reference scene

`Assets/EvoSim/Scenes/HideVsFlee.unity`, as *EvoSim ▸ Build Reference Scenes* makes it (the other Lab 1 scenes differ
only in sizes and numbers):

```
Lab1 World                  World
├── Ground                  FlatGround, 64 × 64 m
├── Environment
│   ├── Grass               FoodGrid, Edible (grazed, 25 points per item)
│   ├── Thickets            CoverLayer (30 % of the ground)
│   ├── Carcasses           CarcassSystem
│   └── Eggs                EggSystem
├── Brains                  JEV (JevBrain, the default brain), Random, Ollama points,
│                           Answer cache, Mutator (MutatorService), Mutator client
├── Recording               RunRecorder, LiveStatistics
├── Phases                  Sense, Ask brains, Choose actions, Act, Breed, Hatch,
│                           Deaths, Migration, Environment, Floor, Record (one phase each)
├── Prey                    Species (40 founders), SearchRule, Edible (struck), Body
│   ├── Locomotion          KinematicLocomotion
│   ├── Stats               Energy, Stamina, Metabolism
│   ├── Senses              Energy, Stamina, Food, Predator, Cover, Animal, Age
│   ├── Actions             eat, flee, hide, follow, rest, mate (each with its TextGene)
│   ├── Food                Diet (Grass)
│   ├── Life                MatingRule, Litter, UniformCrossover, Incubation, Starvation,
│   │                       OldAge, CapRule, FloorRule, DecisionSchedule
│   └── Mutation            LlmMutation
└── Predator                the same groups: hunt, follow, rest, mate; Digestion; Diet (prey, carcass:prey)
```

The camera, the light and the *Run views* object sit beside the World, not under it: they only display
([Views and real time](views-and-real-time.md)). *GameObject ▸ EvoSim ▸ New World* makes the same World tree with the
random brain as default, and *GameObject ▸ EvoSim ▸ New Species* adds a species to it.

## Who owns a module

The World reads its tree with three rules (ARCH-05, ARCH-06):

1. **A module belongs to the nearest owner above it.** A <xref:EvoSim.SpeciesModule> belongs to the nearest
   <xref:EvoSim.Species> above it (or on the same GameObject). A species inside another species is an error (V-01). A
   species module with no species above it is an error too (V-02).
2. **Disabled means absent.** A disabled component, or one on an inactive GameObject, is not part of the world. Disabling
   an action also removes the text gene on it or under it, from the genome, the prompt and the signature.
3. **Order matters only within a kind.** Groups such as `Senses` or `Life` are only folders. The modules of one kind are
   ordered depth first in the hierarchy: the n-th action found is action n (its place in the probabilities and the
   prompt), the n-th sense gives the n-th fragment of the situation text, the n-th gene is locus n.

A text gene steers the nearest enabled action on its GameObject or above it. A text gene under no action is treated as
free, with a warning (V-06) unless it is marked *free*.

## World modules: phases and services

A <xref:EvoSim.WorldModule> sits under the World and outside every species (one under a species is ignored, V-15). There
are two kinds:

| Kind | Base class | What it is | Found by |
|---|---|---|---|
| Phase | <xref:EvoSim.TickPhase> | one step of every tick | the World runs every phase in hierarchy order |
| Service | <xref:EvoSim.WorldService> | something phases and modules use: the ground, food layers, cover, brains, the recorder | type: [`World.Service<T>()`](xref:EvoSim.World.Service*); type and name: `World.Service<FoodGrid>("Grass")` |

A service's name is its GameObject's name (<xref:EvoSim.WorldService.ServiceName>). Modules find services by type or
name when they initialise, never through a scene reference: that is what lets one prefab serve several scenes (ARCH-08).

## Initialisation, step by step

[Initialize](xref:EvoSim.World.Initialize) sets up a run. It is called by Play mode when **Start On Play** is on, by
[Play](xref:EvoSim.World.Play), or by a test. Modules never set themselves up in `Awake` or `Start`, whose order Unity does
not guarantee: the World calls them, always in this order (ARCH-04). The step numbers are those of 20 §4, which the API
comments use too.

| Step | What happens |
|---|---|
| — | **Reset**: tick 0; new random streams and a new event log from the seed |
| 0 | a World inside another World is an error (V-16) |
| 1 | **species**: every enabled species under the World, in hierarchy order |
| 2 | **world modules**: every enabled phase and service, in hierarchy order; the phase list is the tick's order (TICK-01) |
| 3 | **services**: each service's `Initialize`, the ground first, then the others in hierarchy order |
| 4–5 | **species modules**: each species collects the modules it owns, binds each text gene to its action, and builds its action, gene and sense orders; ids and display names must be unique (V-07) |
| 6 | every species module's `Declare` (stats and traits), then every species module's `Initialize` (look-ups), then every phase's `Initialize`; the founder alleles are registered |
| 7 | the food web is derived from the diets; each species is signed (SPEC-03) and its prompt template built |
| 8 | **validation**: the core checks, then every module's `Validate`; an error stops here, `Initialize` returns false and keeps the messages in [LastReport](xref:EvoSim.World.LastReport) |
| 9 | **views**: a pool of views per species that has a <xref:EvoSim.Body> |
| — | **Begin**: every world module's `Begin`, in hierarchy order, for initial content such as the first food items |
| 10 | **founders**: each species' **Initial Population**, at random walkable positions, genomes drawn from the founder pools (POP-04) |

The reset and steps 0 to 8 are [Prepare](xref:EvoSim.World.Prepare(EvoSim.ValidationReport)), which creates no
animal. The editor validates with it, so `Declare`, `Initialize` and `Validate` also run in Edit mode. They must not
change the scene: put anything that creates content in `Begin`.

## The lifecycle hooks

| Hook | On | Called |
|---|---|---|
| `Initialize` | services | step 3, before the species' modules exist |
| `Declare` | species modules | step 6, before any species module initialises |
| `Initialize` | species modules, then phases | step 6, after every `Declare` |
| `WritePromptRules` | species modules, services | step 7, while each species' prompt is built |
| `Validate` | every module | step 8; also by the *Validate* button, the play-mode gate, and when a species is added during the run |
| `Begin` | world modules | once, after validation passed, before the founders; never in Edit mode |
| `OnBorn` | the species, its modules, then every world module | each animal that joins the living, founders included |
| `OnDied` | the species, its modules, then every world module | each death, once it is recorded |
| `OnSpeciesAdded` | world modules | when [AddSpecies](xref:EvoSim.World.AddSpecies(EvoSim.Species,System.String,EvoSim.Species,System.Int32)) adds a species during the run |
| `OnRunStopped` | world modules | once, when the run stops, whatever the reason |

Within each line, modules are called in hierarchy order. The extension pages explain each hook from the module's side:
[Species modules](../extending/species-modules.md) and [Phases, services and world hooks](../extending/world-modules.md).

## Validation and the play-mode gate

One validation engine runs everywhere (EDIT-01): the *Validate* button of the World and Species inspectors, the
play-mode gate, `Initialize`, and the tests. Each message has a validator id (V-01 to V-62, listed in 21 §2), a
severity, the object at fault, and often a fix button.

| Severity | Effect |
|---|---|
| Error | blocks Play and the run |
| Warning | the run starts; something is probably not what you meant |
| Information | worth knowing |

When you press Play, the gate (<xref:EvoSim.Editor.PlayModeGate>) validates every enabled World of the open scenes. An
error cancels Play and logs `EvoSim: Play blocked — V-xx …`; clicking the line selects the object at fault. The
inspectors' validation lists and fix buttons are described in [Editor tooling](editor-tooling.md#validation).

## Run controls

Once initialised, the World advances as its run controls say. In Play mode the World inspector shows them as buttons
(*Play*, *Pause*, *Step*, *Run N*, *Stop*).

| Method | What it does |
|---|---|
| [Play](xref:EvoSim.World.Play) | initialises if needed, then runs |
| [Pause](xref:EvoSim.World.Pause), [Resume](xref:EvoSim.World.Resume) | stops advancing, goes on again; a tick that waits for the brain stays where it is |
| [Step](xref:EvoSim.World.Step) | runs one tick, then pauses |
| [RunFor](xref:EvoSim.World.RunFor(System.Int32)) | runs that many ticks, then pauses |
| [RequestStop](xref:EvoSim.World.RequestStop(System.String)) | ends the run at the next tick boundary (the inspector's *Stop*) |
| [Stop](xref:EvoSim.World.Stop(System.String)) | ends the run now; for a tick boundary, or inside a tick before anything changed |

A stopped run is over: its files are complete, and a new run needs `Initialize` again. Leaving Play mode, or disabling
the World, stops the run too. [State](xref:EvoSim.World.State) says where the run is: *Idle*, *Running*, *Paused*,
*Stepping*, *Waiting* or *Blocked* (a tick waits for answers), *Stopped*. Tests and batch runs skip the controls and call
[Advance](xref:EvoSim.World.Advance(System.Int32)) directly ([A tick, phase by phase](the-tick.md)).

## The main types

| Type | Role |
|---|---|
| <xref:EvoSim.World> | the root: finds, sets up, validates and runs everything below it |
| <xref:EvoSim.Species> | a kind of animal; owns its modules and its living animals |
| <xref:EvoSim.SpeciesModule> | the base of every module under a species |
| <xref:EvoSim.WorldModule> | the base of phases and services |
| <xref:EvoSim.TickPhase>, <xref:EvoSim.WorldService> | the two kinds of world module |
| <xref:EvoSim.Ownership> | the ownership rules above, as functions |
| <xref:EvoSim.ValidationReport>, <xref:EvoSim.ValidationMessage>, <xref:EvoSim.ValidationFix> | the messages of a validation pass |

## Settings

The World's run settings (seed, wait mode, speed, stop conditions) are listed in
[Running and building](running-and-building.md#the-worlds-run-settings). The others:

| Field | What it does | Reference |
|---|---|---|
| **Separate World Seed**, **World Seed** | give the map (terrain, cover, initial food) its own seed (RAND-04) | off |
| **Decision Period** | ticks between two decisions of an animal (DEC-01) | 4 |
| **Sampling Temperature** | sharpens (below 1) or flattens (above 1) the brain's probabilities before the draw (DEC-20) | 1 |
| **Memo Capacity** | most answers the decision memo keeps (DEC-30); 0 = no limit | 262 144 |
| **Text Style** | situation texts terse (V1) or in the first person (V2) (SENSE-11) | V1 |
| **Default Brain** | the brain of species that name none (DEC-13) | JEV |
| **Shuffled Genes**, **Random Founders**, **No Mutation**, **Asexual** | the experimental controls C3, C4, C2, C7 (15 §1) | off |
| **Show Views**, **Interpolate Views**, **Views Outside Play Mode** | what the views do; they never change a run | on, on, off |

## Go further

- [A tick, phase by phase](the-tick.md): what happens once the run starts.
- [Species modules](../extending/species-modules.md) and [Phases, services and world hooks](../extending/world-modules.md):
  writing your own modules.
- [Editor tooling](editor-tooling.md): the World and Species inspectors.
- Specification: [20 — Unity architecture](../spec/20-unity-architecture.md) (a design proposal: the code is the
  reference where they differ) and [21 — Editor tooling](../spec/21-editor-tooling.md).
