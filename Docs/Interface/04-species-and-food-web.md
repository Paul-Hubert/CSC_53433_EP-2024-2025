# 04 — Species and the food web

The prototype has exactly two species, prey and predators, and their
relations are written into its code. Here a species is a configuration of
modules, any number of species can live in one world, and who eats whom is data:
the **food web**.

## 1. What a species is

- **SPEC-01 (MUST)** A species has a unique, stable **id** and a **display name**.
  The display name is what the brain reads in situation texts and prompts, and
  what genes may mention ("Run from any wolf you see.").
- **SPEC-02 (MUST)** A species defines, in a fixed order: its actions (at least
  one), its genes (loci), its senses; and, unordered: its stats, traits, diet,
  metabolism, reproduction, mutation, death and population rules, and which
  brain decides for it.
- **SPEC-03 (MUST)** The order of actions, genes and senses is part of the
  species' identity. It fixes the order of the probability vector, the action
  list in the prompt, the option letters of a multiple-choice brain, the locus
  order of the genome and the order of the situation text. A change of the
  order or of the set is reported (the species' **signature** changes), because
  it changes results and invalidates cached answers.
- **SPEC-04 (MUST)** Genomes never cross species: no crossover between species,
  and allele ids are unique within a species' locus.
- **SPEC-05 (MUST)** A species never contains another species. A configuration
  that nests one species inside another is an error found before the run.
- **SPEC-06 (SHOULD)** Two species may share module *definitions* (the same flee
  action, the same stamina stat, the same founder sentences) while keeping their
  own parameter values.

## 2. The food web

- **SPEC-10 (MUST)** Each species has a **diet**: a list of entries
  *(target, method, gain)*:

  | Method | Target | Effect |
  |---|---|---|
  | graze | a resource layer | consume an item within reach, gain its energy |
  | strike | a species | try to kill an animal within reach; on success gain energy and maybe leave a carcass |
  | scavenge | carcasses of a species | eat one portion within reach |

- **SPEC-11 (MUST)** A species may hunt any species, including its own
  (cannibalism). An animal never targets itself.
- **SPEC-12 (MUST)** The **threats** of a species are the species whose diet
  strikes it. They are derived from the food web, not configured twice. Senses
  and actions that refer to "threats", "prey" or "kin" resolve these sets from
  the food web; each MAY override the set explicitly (e.g. flee only from wolves).
- **SPEC-13 (MUST)** An interaction happens only if the diet allows it: an
  animal grazes, strikes or scavenges only what its diet lists.
- **SPEC-14 (SHOULD)** The editor shows the food web and warns about: a species
  with no food source; a hunter whose prey doesn't exist; a flee action with no
  threat to flee from; a cycle where every species hunts every other.

## 3. Names in texts

- **SPEC-20 (MUST)** Situation texts and prompts name species by their display
  names, and a sense that targets a set of species uses a label (e.g. the set
  of threats labelled "Predator"). Renaming a species or a label changes what
  the brain reads, and therefore its answers and cache keys.
- **SPEC-21 (SHOULD)** When a species is renamed, the editor lists the alleles
  (founder sentences) that mention the old name.

## 4. Species created during a run

- **SPEC-30 (MUST)** Species can be added while a run goes on, from a template
  species or by a speciation rule. A new species gets a new id and name, its own
  population, its own allele namespace, caps and floors, and its own random
  streams.
- **SPEC-31 (MUST)** By default a new species born from a parent species
  inherits the parent's relations in both directions: it eats what the parent
  eats, and species that hunted the parent hunt it. A speciation rule MAY change
  this.
- **SPEC-32 (MUST)** Species ids are never reused within a run. An extinct
  species keeps its id and its records.
- **SPEC-33 (MAY)** A speciation rule splits part of a species into a new one,
  for example when a group's genomes drift far from the rest, or when a group is
  cut off by terrain. The prototype has no speciation.

## 5. Reference: the Lab 1 ecology

Two species, as in the prototype (2026-10-09):

| | Prey ("Animal") | Predator ("Predator") |
|---|---|---|
| Actions, in order | eat, flee, follow, rest, mate | hunt, follow, rest, mate |
| Text genes | one per action, same order | one per action, same order |
| Senses, in situation-text order | energy level, stamina level, food, nearest threat ("Predator"), nearest kin with readiness ("Animal"), age | energy level, stamina level, nearest prey ("Prey"), nearest edible carcass ("Carcass"), nearest kin with readiness ("Other predator"), age |
| Diet | graze the food layer (+25) | strike prey (kill chance 0.5, +60, leaves a carcass), scavenge prey carcasses (+30) |
| Threats (derived) | predators | none |
| Hidden from threats in cover | yes | — |

The Unity reference scenes also contain a variant of the prey with a hide
action, a cover sense and a hide gene (owner decision), and the scenarios of
[31](31-scenarios.md) use up to six species.
