---
uid: extend-before-you-start
---

# Before you start a feature

You have an idea for EvoSim: animals that get thirsty, prey that call out when they see a predator, a brain of your own,
winters that slow the grass down. This page helps you turn the idea into modules: which mechanism to use, what to read
first, how to start, which rules keep the simulation working, and the mistakes that are easy to make. It teaches
nothing in depth: every section links to the page that does.

## What a feature is in EvoSim

Everything is a component in the scene. A world is a tree of GameObjects: a World at the root, its species below it,
and under each species its senses, actions, genes, stats and brain. Phases, services and food layers sit under the
World, outside any species. When you press Play, the World **discovers** every component, **validates** the tree and
**runs** the modules in a fixed tick order ([A tick, phase by phase](../guides/the-tick.md)).

Your feature is one class (sometimes two or three) that derives from a public base class, exactly like the reference
modules. They use no hidden shortcut, so the closest reference module is always a working example of what you write.
Some features need no code at all: a number gene, a free text gene, a new species.

## Before you write code

1. **Say in one sentence** what your feature changes in an animal's life, and which species it applies to. *"Prey get
   thirsty and die if they don't drink."*
2. **Choose the mechanism** with the table below. If your sentence names several things (a stat, a sense of it, an
   action that resets it), that is several modules: one row each.
3. **Read the closest reference module** the table names, and its tests. Then read the page of its kind.
4. **Decide how you will see that it works**: a column of `stats.csv`, a cause of death, the
   *Window ▸ EvoSim ▸ Animal Inspector*, the *Prompt preview* of the Species inspector, or a scenario.

## Which mechanism

Recipes are sections of 22 (the specification's recipes); the samples are in `Assets/EvoSim/Samples`. A conformance
suite is a test of `ConformanceTests` that finds your class by itself ([Testing your module](testing-your-module.md)).

| Feature | Base class | Recipe | Read first | Suite | Page | Watch out |
|---|---|---|---|---|---|---|
| a sense | <xref:EvoSim.Sense>, <xref:EvoSim.BandedSense> for distances | 22 §2, §13 | `LevelSense`, `NearestAnimalSense`; sample `ChasedSense` | `SenseConformance` | [A sense](sense.md) | few tokens: each one multiplies the species' observation space |
| an action | <xref:EvoSim.AnimalAction> | 22 §3, §11 | `RestAction`, `FleeAction`, `HideAction`; sample `DrinkAction` | `ActionConformance` | [An action](action.md) | `Act` only plans: walk, search, an interaction on arrival |
| a stat and a death rule | <xref:EvoSim.SpeciesModule> declaring a <xref:EvoSim.StatId>; <xref:EvoSim.DeathRule> | 22 §1 | `Stamina`, `Starvation`; samples `Thirst`, `DiesOfThirst` | `StatConformance` (the stat); none for the rule | [Stats, costs and death rules](stats-and-death.md) | the value lives in the animal, not in a module field |
| a gene | <xref:EvoSim.NumberGene>, <xref:EvoSim.TextGene> (no code); <xref:EvoSim.Gene> for a new kind | 22 §4, §5 | a number gene under the reference species; `NeuralGenomeGene` for a vector | none; `MutationOperatorConformance` checks results against your gene's `Check` | [Genes](genes.md) | a number gene needs a cost (V-47) and an operator that accepts it (V-48) |
| a mutation operator | <xref:EvoSim.MutationOperator> | 22 §8 | `GaussianMutation`; sample `IntensityLadder` | `MutationOperatorConformance` | [Mutation](mutation.md) | blind: it sees only the gene and the parent allele |
| a brain | <xref:EvoSim.Brain> | 22 §7 | `RandomBrain`; `NeuralBrain` for numbers | none runs on yours: copy `DecisionTests.BrainConformance` | [A brain](brain.md) | one row per query, in order; the same query gives the same row |
| a food layer or a world change | <xref:EvoSim.ResourceLayer>; <xref:EvoSim.TickPhase>; <xref:EvoSim.Cover>, <xref:EvoSim.Ground> | 22 §6, §9 | `FoodGrid`; sample `SeasonsPhase` | `LayerConformance`, `PhaseConformance` | [Food, cover, entities and ground](environment.md), [Phases, services and world hooks](world-modules.md) | a layer changes only in its own phase, with its own stream |
| a new species | duplicate one; <xref:EvoSim.Species> only for hooks | 22 §10 | the reference prey; *GameObject ▸ EvoSim ▸ New Neural Species* for a neural one | none: validation | [Species modules](species-modules.md#species), [A neural species](neural-species.md) | add it to the diets of what should eat it |

Not on the list? [Extending EvoSim](index.md) has a longer *I want to…* table, and
[Every extension point](extension-points.md) lists every base class and member.

## The workflow

1. **Create the class.** In the Project window, *Assets ▸ Create ▸ EvoSim ▸ Script ▸* **Sense**, **Action**, **Gene
   kind**, **Stat**, **Phase**, **Brain** or **Mutation operator** writes `Assets/Student/<Name>.cs` and a test in
   `Assets/Student/Tests`. For a death rule, a layer, a cover or a ground, derive from the base class yourself in
   `Assets/Student` ([Testing your module](testing-your-module.md#step-by-step)).
2. **Place the component** in a copy of a reference scene (*File ▸ Save As*), for example
   `Assets/EvoSim/Scenes/Lab1_Small.unity`: under the species it belongs to, or under the World for a phase or a layer
   ([Species modules](species-modules.md), [Phases, services and world hooks](world-modules.md)).
3. **Press Play and read the validation messages.** The World and Species inspectors end with a validation list; each
   message names the object to fix, and some have a fix button. *Validation is green* means the tree is consistent
   ([Editor tooling](../guides/editor-tooling.md#validation)).
4. **Run the conformance suite of its kind**: *Window ▸ General ▸ Test Runner*, tab **EditMode**, `ConformanceTests`,
   the cases named after your class. Then your own tests, under `Student.Tests`.
5. **Record a short run and read `stats.csv`.** Each run writes a folder under `Logs/EvoSim/runs/`; `stats.csv` has a
   row every 100 ticks ([Recording and outputs](../guides/recording-and-outputs.md)). Check that your feature shows: a
   population that changes, a `deaths_<cause>` column, a column of your own ([Recording and statistics](recording.md)).

## The rules every module keeps

Break one of these and the simulation still runs, but it no longer means what you think. Each id links to the rule in
the specification.

| Rule | Why | How to keep it |
|---|---|---|
| CORE-05: no per-animal state in a module | one module serves every animal of its species | declare a stat or a trait in `Declare`, and read it through its id: `a[thirst]` |
| RAND-01: no global randomness | a seed must replay the run exactly | draw only from the <xref:EvoSim.RandomStream> you are given, never `UnityEngine.Random`, `System.Random` or the clock |
| RAND-05: no decision from a `Dictionary` or `HashSet` order | their order is not fixed | iterate lists |
| ACT-06: actions only produce intents | the act phase moves animals; interactions check diet and reach | in `Act`, call `WalkTo`, `Search`, `OnArrival`; change animals only in an interaction's `Apply` |
| SENSE-01: a sense returns a declared token | the brain reads a fixed list of situations | build `Tokens` once, with a token for "nothing"; `Read` returns an index into it |
| SENSE-05: the observation space stays small | the memo and the answer cache stop working when it is large | keep to a few tokens per sense; V-50 warns above 100 000 situations |
| PROMPT-06: the prompt stays under the brain's limit | a longer prompt is cut or rejected | read the *Prompt preview* and its token estimate; V-11 reports a prompt above the limit |
| ARCH-04: set up in `Initialize` | the World initialises modules in a fixed order | look services and modules up in `Initialize`, never in `Awake` or `Start` |

Together they give **determinism**: the same scene and seed, with the same brain answers, give the same run, event for
event, and the same events hash. A test that fails once fails every time, which is what makes it useful. See
[Determinism and replay](../guides/determinism.md). Its static checks scan the reference code, not `Assets/Student`:
in your own code the rules are yours to keep.

## Common mistakes

1. **A `List<Animal>` or a `Dictionary<Animal, …>` field in a module**, to remember something per animal (CORE-05).
   Declare a stat instead ([Stats, traits and their ids](species-modules.md#stats-traits-and-their-ids)).
2. **Calling `UnityEngine.Random`** in a sense, an action or a phase (RAND-01). The run still works, but a seed no
   longer replays it, and your tests become flaky.
3. **Moving an animal, or changing its stats, from `Act`** (ACT-06). `ActionConformance` fails. Plan the move with the
   act context, and put the effect in an <xref:EvoSim.IInteraction>.
4. **A sense with dozens of tokens**, such as a distance in every metre. One such sense can multiply the observation
   space by 50. Use a few bands: <xref:EvoSim.BandedSense> does it for you.
5. **A new prey nobody eats.** Duplicating the prey gives a species no predator hunts until you add it to their diets
   (22 §10); validation only notes it (V-25). For a neural pair, V-79 warns.
6. **An action without a gene.** The brain can't be steered for it (V-05). Put a text gene on the same GameObject, with
   founder sentences (22 §3, [An action](action.md#where-to-put-it)).
7. **A brain that accepts attachments and ignores them.** If `AcceptsAttachments` is true, senses may send float
   arrays or images instead of text (SENSE-41); read them, or leave it false and V-51 tells you which sense needs them.
8. **Setting up in `Awake` or `Start`**, or keeping a field from the last run. Reset your fields in `Initialize`: the
   same object starts every run ([Species modules](species-modules.md#rules-your-module-keeps)).

## Ways in

To get a feel for the code first, rewrite one small reference module (`RestAction`, `LevelSense`, `Starvation`) from
its stub: [the teaching path](exercises.md). To see a whole idea built, read
[Worked example: alarm calls](worked-example-alarm-calls.md): a sense, an action, a gene and a world service, from the
idea to a working scene. The [samples](samples.md) are small complete features (thirst, water, seasons, slopes) to
copy, change and combine.

## Watching your idea at work

Once it runs, the editor shows what your modules do. All are under *Window ▸ EvoSim ▸*
([Editor tooling](../guides/editor-tooling.md#windows)):

| To see | Open |
|---|---|
| what one animal senses, chooses and carries, and its stats | **Animal Inspector**, then click the animal in the Scene view |
| what a gene sentence does to the brain's answer, in a situation you pick | **Ask the Brain** |
| populations, births and deaths by cause, action shares over time | **Ecology Monitor** |
| which sentences spread and where they came from | **Gene Pool** |
| who eats whom, once your species or food is in | **Food Web** |
| every event as it happens, filtered to one animal | **Event Log** |

Change one thing at a time and keep the seed: the same seed and the same brain answers replay a run exactly, so any
difference comes from your change. The **Scenario Runner** runs several seeds headless, when one run is not enough to
tell.
