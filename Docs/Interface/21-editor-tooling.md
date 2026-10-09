# 21 — Editor tooling

The editor is where students meet the system. Its job: make the configuration
visible (orders, food web, prompt, observation space), catch mistakes before
Play, and make a running world readable (who does what, which genes spread).

## 1. Rules

- **EDIT-01 (MUST)** One validation engine runs everywhere: live in inspectors, on
  a *Validate* button, before entering Play mode (errors block Play), in
  `World.Initialize`, and in CI (an EditMode test validates every scene and
  module prefab).
- **EDIT-02 (MUST)** Every message names the object at fault (clicking it selects
  it) and, where possible, offers a fix button.
- **EDIT-03 (MUST)** Editor code lives in the editor assembly. Runtime code never
  depends on it.
- **EDIT-04 (SHOULD)** Inspectors show computed facts, not only fields: orders,
  signature, observation-space size, prompt preview, food web.
- **EDIT-05 (SHOULD)** Every tool works in edit mode on the configuration and in
  Play mode on the running world.

## 2. Validation catalogue

Severity: **E** error (blocks Play), **W** warning, **I** information.

| Id | Sev. | Check | Fix offered |
|---|---|---|---|
| **Structure** |
| V-01 | E | a species inside another species (SPEC-05) | move it up |
| V-02 | E | a species module with no species above it | — |
| V-03 | E | two actions of a species with the same name | rename |
| V-04 | E | a species with no action | add Rest |
| V-05 | W | an action without a gene (the brain can't be steered for it) | add a text gene |
| V-06 | W | a text gene neither bound to an action nor marked free | mark free |
| V-07 | E | two species with the same id or display name | rename |
| V-08 | E | phases in an impossible order (Choose actions before Ask brains, Hatch before Breed) | reorder |
| V-09 | W | a core phase missing (no Deaths phase: nobody dies) | add it |
| V-10 | E | a species without a brain and no world default | pick one |
| V-11 | E | a brain's limits exceeded: more than 16 actions for JEV, prompt estimate above its token limit (DEC-14) | — |
| V-12 | W | the species' signature changed since it was last accepted (SPEC-03): results and caches change | accept |
| V-13 | E | a species without exactly one locomotion component (MOVE-01) | add `KinematicLocomotion` |
| **References** |
| V-20 | E | a module names a layer, entity kind or species that doesn't exist | pick from a list |
| V-21 | W | an animal set resolves to nothing (flee with no threat, hunt with no prey) | — |
| V-22 | W | a species with no food source in its diet | — |
| V-23 | W | a gene or founder sentence mentions a species name that doesn't exist (SPEC-21) | — |
| **Values** |
| V-30 | E | band edges not increasing, or not below the vision (SENSE-21) | sort |
| V-31 | E | level thresholds with low > high | swap |
| V-32 | E | floor > cap, or initial population > cap | — |
| V-33 | E | litter min > max, or min < 1 | — |
| V-34 | W | `childEnergy × litterMin / parents > mateEnergy`: parents can fall below zero (REPRO-10) | — |
| V-35 | E | a probability or rate outside [0, 1]; a decision period < 1 | clamp |
| V-36 | W | a trait default outside its range | clamp |
| **Genes** |
| V-40 | E | a founder sentence that fails the mutation guards: too many words, forbidden characters (GENE-22) | highlights the problem |
| V-41 | E | an empty founder pool | add the neutral allele |
| V-42 | W | duplicate founder sentences | remove |
| V-43 | W | a text gene with no neutral allele | add "No preference." |
| V-44 | I | a text gene with no contrast pair: brain tests skip it | — |
| V-45 | E | a number gene whose trait no module declares | pick from a list |
| V-46 | W | number-gene founder values outside the gene's range | clamp |
| V-47 | I | a number gene's trait affects no cost: the gene will drift to one end (09 §5) | — |
| V-48 | W | a gene that no mutation operator accepts | add an operator |
| **Observations and brains** |
| V-50 | W / E | observation space above 100 000 (W) or 10 000 000 (E) (SENSE-05) | — |
| V-51 | E | a sense with attachments and a brain that can't read them (SENSE-41) | — |
| V-52 | W | the keyword brain is used and an action has no keyword pattern or default score | — |
| V-53 | E | a prompt field (header, action line, rule line) or a frozen prompt with an unknown placeholder; a frozen prompt without `{genes}`, `{situation}` or `{ask}` | — |
| **Services and secrets** |
| V-60 | W | an HTTP brain or the mutator is unreachable (checked on demand: *Test connection*) | — |
| V-61 | E | a key or token in a serialized field of a scene, prefab or asset (OUT-04) | move to an environment variable |
| V-62 | E | the answer cache folder can't be written | pick another |

## 3. Inspectors

**World.** State (not initialised, running, waiting: "waiting for JEV: 213
queries, 1.4 s"), tick, run controls (Play, Pause, Step, Run N, Fast, Real time),
seed, wait mode, act order; the phase list as a reorderable view of the
`Phases` children with enable toggles; the services; the validation panel.

**Species.** Read-only tables computed from the subtree:

| Table | Columns |
|---|---|
| Actions | order, name, bound gene, keyword pattern, relevance condition |
| Senses | order, label, token count, example fragment (V1 and V2) |
| Genes | locus, kind, founder pool size, mutation operator and rate |
| Stats and traits | name, declared by, read by, written by, default, range |
| Diet | target, method, gain; derived threats |
| Population | initial, floor, cap, cap rule |

Plus: observation-space size (e.g. "4 860 situations"), the signature, and a
**prompt preview**: pick a founder genome and a situation (dropdowns per sense,
or *random*), switch V1/V2, see the full prompt with its token estimate, copy it.

**Module drawers.**

- *Text gene*: each founder sentence with its word count and forbidden characters
  highlighted; neutral and contrast fields.
- *Number gene*: range slider, founder values on it; in Play mode a histogram of
  the living animals' values.
- *Sense*: its tokens and the fragment each writes.
- *Bands*: the edges drawn on a ruler up to the vision.
- *LLM mutation*: the deck, and a *Try* button that mutates a sample sentence
  with the real mutator and shows the raw answer, the cleaned sentence and the
  guards' verdict.

## 4. Windows

| Window | Shows | Lab activity |
|---|---|---|
| **Ecology monitor** | population per species over time, deaths by cause, births, mean energy and stamina, action shares, memo hit rate, brain time, ticks per second | A, B |
| **Gene pool** | per locus: allele frequencies over time (stacked), current top alleles with their text and origin, the lineage of a selected allele as a chain of text diffs; number genes as histograms over time | D, E |
| **Animal inspector** | the selected animal: stats, traits, genome sentences, last situation text, last probabilities as bars, current action and target, busy reason, parents and children | C |
| **Ask the brain** | build a genome (founders, edited sentences, contrast pro/anti, random text), pick a situation, query several brains side by side; one click for a directed test (pro vs anti ΔP) and a random-text comparison | C, I |
| **Food web** | species and layers as a graph with graze, strike and scavenge edges; threats; warnings | — |
| **Event log** | events filtered by kind, species and animal; follow a lineage | D |
| **Scenario runner** | scenario assets, run N seeds headless, results against expected ranges, pass or fail ([31](31-scenarios.md)) | G |
| **Brain health** | hosts, *Test connection*, model digests or revisions, latency, failures, cache size and hit rate | — |

## 5. Scene view

- **Gizmos** for the selected animal: vision circle and band rings, a line to its
  target coloured by action, its reach, its NavMesh path.
- **An overlay** (Unity 6 Overlays) with toggles: food items, cover, carcasses,
  eggs; a letter or colour per action above each animal (stable colours from
  the action order); species filters; heat maps of deaths and food.

## 6. Creating things

- `GameObject ▸ EvoSim ▸ New World` builds the default tree: ground,
  environment, brains, recorder and the eleven phases.
- `GameObject ▸ EvoSim ▸ New Species` adds a species with a kinematic
  locomotion, energy, stamina, metabolism, diet, mating, litter, crossover,
  starvation, old age, cap and floor.
- `EvoSim ▸ Exercises ▸ Replace with stub` swaps a reference module for an empty
  student class of the same base class, keeping its inspector settings, so that
  the reference module's tests grade the student's version (the teaching path,
  [22 §0](22-extending-recipes.md#0-the-teaching-path)); `Restore reference`
  undoes it.
- `GameObject ▸ EvoSim ▸ Add Action / Sense / Gene` instantiates module prefabs
  from `Modules/`.
- `Assets ▸ Create ▸ EvoSim ▸` *Allele Pool*, *Scenario*, *Mutation Deck*, and
  *Script* templates for a new sense, action, gene kind, stat, phase, brain or
  mutation operator, each with the methods to override and a matching test file.

## 7. A student's loop

Open the scene → validation is green → Play → watch the ecology monitor → pause →
click an animal → read its genes and why it chose → open *Ask the brain*, change a
sentence, compare → stop → change a gene in the founder pool → run again with the
same seed → compare the two runs in the gene pool window.
