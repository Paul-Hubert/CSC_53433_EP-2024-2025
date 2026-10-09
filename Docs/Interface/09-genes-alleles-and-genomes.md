# 09 — Genes, alleles and genomes

In the prototype every gene is an English sentence, one per action. Here a gene
is an interface: the reference kinds are **text genes** (sentences the brain
reads) and **number genes** (values expressed into traits, such as maximum
stamina). Both evolve the same way: inherited by crossover, changed by blind
mutation, kept or lost by selection.

## 1. Genes (loci)

- **GENE-01 (MUST)** A gene has a locus id, unique in the world, a label (what
  the brain or the inspector shows), a kind and a founder pool.
- **GENE-02 (MUST)** A gene kind defines how its alleles are stored, when two
  alleles are the same (GENE-10), how they are expressed, and which mutation
  operators accept them ([10](10-mutation.md)).
- **GENE-03 (MUST)** A **text gene** is expressed only through the brain: its
  sentence goes into the genes block of the prompt (PROMPT-02) and to the
  keyword brain. It has no other effect.
- **GENE-04 (MUST)** A **number gene** is expressed into one trait (ANIM-15): it
  sets the trait, or multiplies its default, and the result is clamped to the
  trait's range. It does not enter the prompt unless it is configured to
  (MAY, e.g. "Stamina: high.").
- **GENE-05 (MUST)** By default each action has one text gene, bound to it and
  labelled with the action's name. Genes not bound to an action (**free genes**,
  e.g. a temperament) are allowed; they appear in the genes block under their
  own label.
- **GENE-06 (MUST)** A genome holds exactly one allele per locus of its species,
  in the species' locus order. A genome of the wrong length or of another
  species is an error.
- **GENE-07 (MAY)** Other kinds may exist: a choice among a few options, a small
  vector, the weights of a small neural network (the previous lab's genome).

## 2. Alleles and the registry

- **GENE-10 (MUST)** Every allele seen in a run is registered once per
  *(locus, value)*: sentences are compared after collapsing whitespace, numbers
  at a declared precision (reference: 4 decimals). Registering a known value
  returns the existing allele and keeps its first origin.
- **GENE-11 (MUST)** An allele id is `<locus id>:<n>`, n counting from 0 per
  locus in registration order. Reference locus ids: `<species id>.<label>`
  (`prey.eat`, `predator.hunt`); the prototype writes the prey's without the
  species prefix (`eat:3`, `predator.hunt:0`).
- **GENE-12 (MUST)** An allele records: id, locus, value, origin (`founder`,
  `neutral`, `contrast`, `control`, `mutant`, `custom`), parent allele id,
  mutation operator (e.g. `llm#3` = instruction 3 of the deck, `gauss`), model,
  seed. Following parent ids gives the lineage of every gene.
- **GENE-13 (MUST)** A **genome key** is a hash of the species id and the allele
  *values* in locus order, so the same genes give the same key in any run.
  The brain-visible key (DEC-31) uses only the alleles the brain reads.

## 3. Founder pools

- **GENE-20 (MUST)** Each gene has a founder pool: a list of alleles and, for
  text genes, a **neutral** allele ("No preference.") that is part of the pool.
  A founder genome draws each locus independently and uniformly from its pool.
- **GENE-24 (MUST)** The founder pool and the neutral allele are configured on
  the gene's component (owner decision): the founder values, whether a neutral
  allele exists, its text, and whether it may mutate (MUT-22). Founder lists
  may live in a shared asset that several gene components use.
- **GENE-21 (MUST)** Founder pools are frozen for an experiment and shared by all
  its runs and seeds, so that runs start from the same origin.
- **GENE-22 (MUST)** Founder sentences pass the same checks as mutated sentences
  (MUT-12): at most `maxWords` words (reference 12), allowed characters only.
- **GENE-23 (SHOULD)** Each text gene also has a contrast pair (a "pro" and an
  "anti" sentence) used only to test brains, and the world has a list of control
  sentences (shuffled words, irrelevant facts) for controls and tests
  ([15](15-controls-metrics-and-gates.md)). Neither is ever used as a founder
  except in the random-founders control.

**Reference founder pools** (drafts awaiting the owner's review; at most 12
words, imperative, plain words, no numbers; plus the neutral "No preference."):

| Gene | Founder sentences |
|---|---|
| prey eat | Eat whenever food is close. · Only look for food when energy is low. · Always finish eating before doing anything else. · Eat quickly, then move on. |
| prey flee | Run from any predator you see. · Flee only when a predator is very close. · Stay calm unless danger is right next to you. · Run away from anything that attacks you. |
| prey hide (Unity, new draft) | Hide when a predator is close. · Stay in cover when danger is near. · Hide only when you are tired. · Leave cover to find food when hungry. |
| prey follow | Stay close to other animals. · Follow others when you are lost or hungry. · Keep your distance from other animals. · Follow the strongest animal nearby. |
| prey rest | Rest when you are tired. · Never stop moving. · Rest only when you feel safe. · Save energy by resting when food is far. |
| prey mate | Look for a partner when energy is high. · Mate with any nearby adult. · Mate only when food is plentiful. · Seek a partner before growing old. |
| predator hunt | Chase any prey you see. · Hunt only when you are hungry. · Attack only when prey is close. · Keep chasing until the prey is caught. |
| predator follow | Stay close to other predators. · Hunt as a pack. · Keep away from other predators. · Follow others when no prey is in sight. |
| predator rest | Rest when your belly is full. · Never stop moving. · Lie still and let prey come to you. · Rest when no prey is in sight. |
| predator mate | Look for a mate when well fed. · Mate with any nearby adult. · Hunt first, mate later. · Seek a partner before growing old. |

**Reference contrast pairs.** eat: "Always eat, whatever happens." / "Never eat
unless starving."; flee: "Always run away, whatever happens." / "Never run away
from anything."; follow: "Always follow other animals." / "Never go near other
animals."; rest: "Always rest and stay still." / "Never rest, keep moving.";
mate: "Always try to mate." / "Never mate."; hide (new): "Always hide, whatever
happens." / "Never hide in cover."; hunt: "Always hunt, whatever
happens." / "Never hunt unless starving."; predator follow: "Always follow other
predators." / "Never go near other predators."

**Reference control sentences.** 20 shuffled-word sentences ("Needs bakery green
bread records nine attic.") and 20 irrelevant ones ("Trains leave from the north
platform."), length-matched to the founders.

## 4. Crossover

- **GENE-30 (MUST)** Sexual reproduction: for each locus independently the baby
  takes the allele of one parent or the other with probability ½, from a named
  stream. Loci are homologous: the eat gene only ever crosses with the eat gene.
- **GENE-31 (MUST)** Asexual reproduction: the baby copies its parent's genome.
- **GENE-32 (MUST)** Every baby of a litter gets its own crossover, so siblings
  usually differ.
- **GENE-33 (MAY)** A number gene may declare a blending crossover (e.g. the mean
  of the parents' values) instead of picking one.

## 5. Number genes: an example and a warning

A **stamina gene**: trait `stamina.max`, founder values {45, 60, 75}, range
[20, 120], mutated by a Gaussian rule (σ = 5), clamped. Without a cost, a number
gene drifts to whichever end helps and stops being interesting: more stamina is
always better. Pair it with a cost through the same trait, for example a base
metabolism that grows with `stamina.max`, so that selection finds a balance.
The editor warns about number genes whose trait no cost depends on
([21](21-editor-tooling.md)).
