# 01 — Overview

## The idea in one paragraph

Each animal in a small simulated world carries a **genome of five short English
sentences**, one per behaviour, for example *"Eat whenever food is close."* or
*"Rest only when you feel safe."*. Whenever an animal has to act, its sentences
and a description of its situation are given to a **local LLM**. The LLM answers
with how likely each of five behaviours is (eat, flee, follow, rest, mate), and
one behaviour is drawn from those probabilities.
Animals that find food, avoid predators and mate leave children. A child takes
each sentence from one parent or the other, and now and then a sentence is
**mutated**: an LLM makes one random change to it, seeing nothing but that sentence.
Nobody scores the genomes; survival does. Because genes are sentences, what
evolves can be **read**.

The predators work the same way (since 2026-10-07). Each carries four
sentences, for hunting, following, resting and mating, read by the same kind of LLM, and
predators that catch prey and mate pass them on. Prey and predators evolve
together.

## Why the lab is being redesigned

The previous Crowds & Evolution lab (`Assets/02 - Scripts/04 - Crowds and
Evolution/`, audited in `Docs/redesign/01-current-system.md`) evolved small
neural networks:

| | Previous lab | This project |
|---|---|---|
| Genome | weights of a 5-neuron network | readable sentences in fixed slots, one per behaviour (5 for prey, 4 for predators) |
| Behaviour | one output: a turn angle | 5 behaviours (predators: 4) with probabilities |
| Reproduction | eating spawns a mutated copy (asexual) | two parents mate; gene-by-gene crossover |
| Mutation | random noise on weights | blind random changes made by an LLM, logged with their lineage |
| Selection | hard-wired into the agent's update | implicit: survival, food, mating |
| Result | a population count | which sentences survived, and why |
| AI content | none beyond a tiny network | local LLMs, structured output, prompts, LLMs as operators |
| Experiment tooling | none | seeds, logs, caches, metrics, controls, pre-set gates |

## How it works

```text
             founder pool (frozen sentences)
                         │ sample one sentence per slot
                         ▼
   ┌──────────►  GENOME: 5 sentences  ◄───────────────────────────────┐
   │                     │                                            │
   │   world ──► PERCEPTION ──► situation text                        │
   │                     │            │                               │
   │                     ▼            ▼                               │
   │              PROMPT = actions + genes + situation                │
   │                                  │                               │
   │                                  ▼                               │
   │                 BRAIN (local LLM via Ollama, or rule-based)      │
   │                                  │ points for 5 actions          │
   │                                  ▼                               │
   │                  probabilities ──► draw one action               │
   │                                  │                               │
   │                                  ▼                               │
   │      EXECUTOR: eat, flee, follow, rest, mate … for 4 ticks       │
   │                                  │                               │
   │                                  ▼                               │
   │      energy, predators, ageing ──► who survives and mates        │
   │                                  │                               │
   │                                  ▼                               │
   └──── child genome = crossover(parent A, parent B) + mutation ─────┘
                             (an LLM makes one random change)
```

The diagram shows a prey animal. A predator goes through the same loop with 4
sentences and the actions hunt, follow, rest and mate; its prey are part of its world.

## Design choices and why

| Choice | Reason | More |
|---|---|---|
| **Fixed gene slots**, one per behaviour (prey: eat, flee, follow, rest, mate; predators: hunt, follow, rest, mate) | crossover swaps like with like; position in the prompt is the same for everyone | [04 §1](04-genome-and-evolution.md#1-genes-are-sentences-in-fixed-slots) |
| **Predators are genetic animals too**, with the same brain, energy, breeding and mutation | selection on the prey comes from predators that evolve as well; one mechanism for both species | [03 §3](03-world-and-simulation.md#3-predators) |
| **A frozen founder pool** of instinct-like sentences | random text gives random behaviour that mutation can't climb out of; a shared origin makes runs comparable | [04 §3](04-genome-and-evolution.md#3-the-founder-pool) |
| **The LLM gives probabilities** (points per action), not a single choice | graded behaviour; selection can act on small differences | [05 §4](05-decision-backends.md#4-llm-the-llm-brain) |
| **High-level decisions every 4 ticks**, executed by plain code | the LLM never does motor control; far fewer calls | [03 §6–7](03-world-and-simulation.md#6-actions-five-for-prey-four-for-predators) |
| **Discrete observations** with distances in bands (adjacent, 2-4 cells, 5-10, 11-20, none), the same 20-cell vision and the same speed for both species | short stable prompts with real distances; identical situations are cached | [03 §5](03-world-and-simulation.md#5-perception-what-an-animal-knows) |
| **Lockstep simulation** that waits for the brain | a slower machine gives a slower run, never a different one | [03 §7](03-world-and-simulation.md#7-decisions) |
| **Blind mutation**: the LLM gets one random-change instruction, drawn from a list of 16, and the gene sentence, nothing else | mutation must not know what helps; the instruction list, the seed and the temperature make it random, and a length guard stops genes growing | [04 §5](04-genome-and-evolution.md#5-mutation) |
| **No fitness function** | selection emerges from the world, as in nature; easy to change the world without touching evolution | [04 §6](04-genome-and-evolution.md#6-selection) |
| **Swappable brains** (random, rule-based, LLM) | null model, transparent reference, CPU-only work | [05](05-decision-backends.md) |
| **Measure before building** (gates G1–G5) | the whole idea fails if the LLM doesn't really read the genes | [06](06-experiments-and-results.md) |
| **Flat world in Lab 1** | behaviour first; terrain and foliage come in the next labs | [02](02-lab1.md) |

## The lab sequence

1. **Lab 1 — Evolution on a flat world** (this project): flat ground, food at
   random, prey and predators that both evolve. Behaviour and evolution only.
2. **Terrain lab:** students build terrain. Water and steep ground shape where
   animals can go and where food is.
3. **Foliage lab:** students build vegetation. Plants become the food sources
   and cover.

Animals that evolved in Lab 1 can be brought into each new world to see how
behaviour re-adapts ([02 §7](02-lab1.md#7-toward-the-next-labs-terrain-and-foliage)).
The lab numbers after Lab 1 are to be confirmed by the course owner.

## What students learn

- **AI:** running LLMs locally; structured (typed) output; prompt design; LLMs
  as tools inside an algorithm (decision maker, mutation operator); checking
  model output rather than trusting it.
- **Evolution:** genotype → phenotype → behaviour; heredity, variation,
  selection; crossover between matching gene slots; mutation; drift; loss of
  diversity; "bloat".
- **Method:** null models and controls, seeds, reproducibility, logging,
  metrics, pass/fail criteria fixed in advance.
- **Engineering:** a small simulation split into replaceable parts (world,
  perception, brain, actions, evolution).

## Research context

Pointers for students (from `Docs/redesign/02-assessment.md`):

- *Evolution through Large Models* (ELM), Lehman et al., 2022: LLMs as
  mutation operators.
- *Promptbreeder*, Fernando et al., 2023: self-referential evolution of
  prompts.
- *EvoPrompt*, Guo et al., 2023: genetic algorithms over prompts with LLM
  operators.
- *Generative Agents*, Park et al., 2023: LLM-driven agents in a simulated
  world.

This project combines these threads: prompts that evolve, used by agents in an
ecology where selection is implicit rather than a scored fitness.

## Glossary

| Term | Meaning here |
|---|---|
| **Locus** (plural loci) | one of the fixed gene slots, one per behaviour: 5 for prey (eat, flee, follow, rest, mate), 4 for predators (hunt, follow, rest, mate) |
| **Allele** | one sentence that can occupy a locus |
| **Genome** | the alleles of one animal, one per locus of its species |
| **Founder pool** | the frozen sentences every run starts from: 4 per locus + neutral |
| **Neutral allele** | "No preference.": switches a drive off |
| **Contrast pair** | two genomes differing only at one locus ("Always eat…" vs "Never eat…"), for directed tests |
| **Random-text genome** | genes taken from shuffled or irrelevant sentences; a control |
| **Observation / situation** | what an animal senses, in discrete values (distances in bands), and its text |
| **Brain / backend** | what turns (genome, situation) into action probabilities |
| **Tick** | one simulation step |
| **Decision period** | ticks between decisions (4) |
| **Memo** | in-run store of answers for (species, genome, situation) already seen |
| **Floor / newcomers** | minimum population of a species (prey 10, predators 3), topped up with fresh founders ("immigrants" in the logs) |
| **Digestion** | the 50 ticks after a kill during which a predator stays still and doesn't decide |
| **Cap** | maximum population; no births above it |
| **Generation** | a child's generation is the higher of its parents' generations + 1 |
| **Gate** | a pass/fail criterion fixed before measuring (G1–G5) |
| **Directed test** | does making a gene "pro" vs "anti" move its own action the right way? |
| **MI_G / MI_O** | how much the genome / the situation determines the action, in bits |
| **Common garden** | evolved and founder genomes compared in identical fresh worlds |
| **Spike / Phase 0** | the headless Python prototype that tests the idea before any Unity work |
