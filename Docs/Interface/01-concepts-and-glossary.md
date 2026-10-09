# 01 — Concepts and glossary

## 1. The loop in one picture

```
                ┌────────────────────────── one tick ──────────────────────────┐
 genes ──┐      │                                                              │
 (text,  │      │  senses ──► observation ──► brain ──► probabilities ──► draw │
 numbers)│      │  (discrete)  (+ situation    (batched,   over the       an   │
         │      │               text)          cached)     species'     action │
         │      │                                          actions             │
         │      │  action ──► intent ──► locomotion ──► interactions           │
         │      │  (repeated every tick until the next decision)               │
         │      │                                                              │
         │      │  metabolism ─► deaths ─► mating ─► (mutation) ─► births      │
         │      │  environment: food regrows, carcasses rot, ...               │
         │      └──────────────────────────────────────────────────────────────┘
         └──── number genes set traits at birth (e.g. stamina) ─────────────────
```

- Nobody programs which behaviour is good. Animals that eat, escape and mate
  leave more children, and their genes spread. There is **no fitness function**.
- Genes are **readable**: most of them are short English sentences ("Rest when
  you are tired."). A brain (an LLM, or a transparent keyword reader) turns the
  sentences and the animal's situation into probabilities over its actions.
- Ordinary code does everything physical: moving, eating, striking, breeding.
  The brain only chooses among actions.
- Mutation is **blind**: it changes a gene at random (an LLM making a small edit
  to a sentence, or a numeric rule changing a number) without knowing what
  helps. Selection decides what stays.

## 2. Core rules

- **CORE-01 (MUST)** No fitness function exists anywhere. Survival and
  reproduction follow only from the world rules and the animals' behaviour.
- **CORE-02 (MUST)** Every behaviour-relevant feature (a sense, an action, a
  gene, a stat, a resource, a population rule, a mutation operator, a brain, a
  tick phase, a locomotion) is a module behind a documented interface. Adding,
  removing or replacing one needs no change to core code.
- **CORE-03 (MUST)** No species, action, sense or gene name is special in core
  code. The prey/predator ecology of the prototype is a *configuration* of
  reference modules.
- **CORE-04 (MUST)** The system runs with any number of species (≥ 1), actions
  per species (≥ 1), genes (≥ 0) and senses (≥ 0). Limits imposed by a module
  (e.g. at most 16 options for the JEV brain) are checked before the run.
- **CORE-05 (MUST)** Per-animal state lives in the animal's record. Modules hold
  settings and logic; they never keep hidden per-animal state of their own
  (a module needing per-animal data declares a stat or a trait, [05](05-animals-stats-and-life-cycle.md)).
- **CORE-06 (MUST)** The brain never moves an animal or changes the world. It
  returns probabilities over the actions of the animal's species; everything
  else is done by actions, the locomotion and the tick phases.
- **CORE-07 (MUST)** Mutation is blind: a mutation operator sees the gene it
  changes (and, for text genes, at most one fixed context line), never the
  animal, its fitness, the world or the other genes.
- **CORE-08 (SHOULD)** Configuration errors are found before the first tick
  (validation in the editor and at start), not in the middle of a run.
- **CORE-09 (MUST)** A run is reproducible from its configuration, its seed and
  its answer caches ([12](12-tick-order-and-determinism.md)).

## 3. Glossary

| Term | Meaning |
|---|---|
| **World** | The root of a simulation: space, environment, species, tick phases, services, random streams. One world per run; several worlds may exist side by side (tests). |
| **Tick** | One simulation step. All rules are written per tick. Unity time only drives the visuals. |
| **Phase** | One step of a tick (decide, act, breed, …). A world holds an ordered list of phases ([12](12-tick-order-and-determinism.md)). |
| **Run** | Ticks from 0 to a stop condition, with a seed, writing outputs ([13](13-outputs-and-recording.md)). |
| **Species** | A kind of animal: its genes, senses, actions, stats, diet, reproduction, mutation and population rules. |
| **Animal** | One individual: position, heading, stats, genome, traits, current action, counters. Data, not a GameObject. |
| **Kin** | Other animals of the same species. |
| **Food web, diet** | Who eats what. A species' diet lists resource layers (grazing) and species (hunting) with the energy gained. |
| **Threat** | A species that has the animal's species in its diet. Derived from the food web. |
| **Resource layer** | Something edible spread over the ground, e.g. food items on a grid of cells that regrow. |
| **Entity** | A world object with a position that is not an animal: a carcass, an egg, a water hole. May move. |
| **Carcass** | What a kill leaves: portions that other hunters may eat, rotting after a while. |
| **Cover** | Places where an animal is hidden from the species that hunt it. |
| **Stat** | A per-animal number that changes during life: energy, stamina, age, thirst. |
| **Trait** | A per-animal number fixed at birth, with a default from the species' settings, that number genes can set: maximum stamina, speed, vision. |
| **Metabolism** | The rules that charge and refill stats each tick. |
| **Busy** | A state in which an animal neither decides nor moves for a number of ticks (e.g. digesting a meal). |
| **Gene, locus** | A slot of the genome with a fixed meaning, e.g. "the eat gene". Identified by a locus id. |
| **Text gene** | A gene whose value is a sentence, read by the brain through the prompt. |
| **Number gene** | A gene whose value is a number, expressed into a trait. |
| **Allele** | One value a gene can take (a sentence or a number), with an id and a lineage. |
| **Genome** | One allele per locus of the animal's species, in the species' locus order. |
| **Expression** | Turning a genome into what acts at run time: sentences into the prompt, numbers into traits. |
| **Founder pool** | The alleles from which the first animals and newcomers are drawn, per locus, including a **neutral** allele ("No preference."). |
| **Contrast alleles** | A "pro" and an "anti" sentence per locus, used only in tests of the brain. |
| **Control alleles** | Meaningless or irrelevant sentences, used in controls and tests. |
| **Crossover** | Building a child's genome from its parents', locus by locus. |
| **Mutation operator** | A rule that changes one allele: an LLM instruction deck for sentences, a numeric rule for numbers. |
| **Mutator** | The service that runs LLM mutations (a small model, batched). |
| **Guard, redraw** | A check on a mutated sentence; a rejected answer is drawn again with a new instruction and seed. |
| **Mating, litter** | Two ready animals in contact produce a litter of several babies, each with its own crossover and mutations. |
| **Egg, incubation** | A conceived baby waiting to hatch. Incubation gives the mutator time to answer. |
| **Generation** | The larger parent generation + 1; founders are generation 0. |
| **Cap, migration** | The maximum population of a species. Above it, random older animals leave the world ("migrated"). |
| **Floor, newcomer** | The minimum population. Below it, newcomers with founder genomes are added. |
| **Sense** | A module that reads the world for one animal and returns a discrete **token** (e.g. food "2-4 m away"). |
| **Observation** | The tuple of tokens of all the species' senses for one animal at one decision. |
| **Band** | A distance range with a name: adjacent, close, medium, far, none. |
| **Situation text** | The observation written as text for the brain. |
| **Action** | A behaviour the brain can choose (eat, flee, hide, hunt, …). It chooses a target, produces an **intent** (a direction, walk or run, how far at most) and may trigger an **interaction**. |
| **Search** | What an action does when it has nothing to act on in sight: a random walk. Counted as an invalid choice. |
| **Locomotion** | The species' module that turns an intent into movement over the ground: kinematic straight lines in the reference; terrain, NavMesh or physics subclasses later. |
| **Interaction** | A contact effect: graze, strike, eat a carcass portion, enter cover. |
| **Act order** | The order in which animals act within a tick (species in turn, all mixed, or simultaneous). |
| **Decision period** | Ticks between two decisions (4 in the reference). |
| **Brain** | A module that maps a batch of queries to probability rows over actions. |
| **Query** | What the brain receives for one animal: species, brain-visible genes, observation, situation text. |
| **Sampling temperature** | τ applied to the brain's probabilities before drawing: p^(1/τ), renormalised. |
| **Memo** | Within a run, the answer for each (species, brain, brain-visible genome, observation) is computed once. |
| **Answer cache** | A persistent store of brain and mutator answers, so a repeated run replays without model calls. |
| **Prompt** | The full text sent to an LLM brain: rules, actions, genes, situation, answer instruction. |
| **Gate** | A pass/fail test of a brain or of evolution, fixed in advance ([15](15-controls-metrics-and-gates.md)). |
| **Control** | A run setting that removes one ingredient (mutation, gene expression, meaningful founders) to compare against. |
| **Event, events hash** | One logged fact (birth, death, …); a running hash of all events identifies a run exactly. |
