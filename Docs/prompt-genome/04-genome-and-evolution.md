# 04 — Genome and evolution

What a gene is, where the first genes come from, and how genes change from one
generation to the next. Code: `prototype/promptevo/genome.py`, `founder.py`,
`evolution/mutation.py`, plus `sim.py` for births. Data:
`prototype/data/*.json`.

## Contents

1. [Genes are sentences, in fixed slots](#1-genes-are-sentences-in-fixed-slots)
2. [Alleles, the registry and lineage](#2-alleles-the-registry-and-lineage)
3. [The founder pool](#3-the-founder-pool)
4. [Crossover](#4-crossover)
5. [Mutation](#5-mutation)
6. [Selection](#6-selection)
7. [Experimental controls](#7-experimental-controls)
8. [Reading evolution from a run](#8-reading-evolution-from-a-run)

---

## 1. Genes are sentences, in fixed slots

A **genome** has 10 **loci** (gene slots), always in the same order. Each locus
holds one short English sentence, its **allele**.

| Locus | Kind | Role | Max words |
|---|---|---|---|
| `eat` | action | when and how much to go for food | 12 |
| `flee` | action | reaction to predators | 12 |
| `follow` | action | staying with other animals | 12 |
| `wander` | action | exploring | 12 |
| `rest` | action | saving energy | 12 |
| `mate` | action | reproduction | 12 |
| `attack` | action | fighting for energy | 12 |
| `risk` | temperament | boldness vs caution | 15 |
| `social` | temperament | attitude to other animals | 15 |
| `place` | temperament | where it likes to be | 15 |

Each of the seven **action genes** belongs to one of the seven behaviours the
brain can choose ([03 §6](03-world-and-simulation.md#6-actions-the-seven-behaviours)).
The three **temperament genes** colour every decision. The LLM brain sees the
action genes as a labelled list and the temperament genes as one line
([05](05-decision-backends.md#the-prompt)).

The slots are fixed for two reasons:

- **Crossover makes sense.** Slot 3 is always about following, in every
  animal, so mixing two parents swaps like with like. Without fixed slots a
  child could inherit two hunger genes and no fear gene.
- **Position is not a hidden gene.** The order of text in a prompt affects
  LLMs. With fixed slots the order is the same for everyone.

## 2. Alleles, the registry and lineage

An **allele** (`genome.Allele`) records:

| Field | Meaning |
|---|---|
| `id` | `locus:n`, e.g. `eat:3`, numbered in order of appearance within a run |
| `text` | the sentence (whitespace normalised) |
| `origin` | `founder`, `neutral`, `contrast`, `control`, `mutant` or `ood` |
| `parent_id` | the allele it was mutated from (mutants only) |
| `operator`, `model`, `seed` | how it was made (mutants only) |

The `AlleleRegistry` holds every allele seen in a run. The same text at the
same locus is always the same allele, so if two mutations produce the same
sentence they share one id. A **genome** is just the tuple of 10 allele ids. Its
`genome_key` is a hash of the 10 texts, so the same genes have the same key in
every run. The caches use this key.

`parent_id` links form a **lineage**: you can follow any gene back through its
mutations to the founder sentence it came from.

## 3. The founder pool

Every run starts from the same frozen pool, `data/founder_pool_v1.json`. Each
locus offers four instinct-like sentences plus a **neutral** allele that
switches the drive off. A founder (and every later newcomer) draws one of the
five options per locus uniformly at random, so there are 5^10 ≈ 9.8 million
possible starting genomes. All runs share the same origin and can be compared.

> **Status:** v1 is a draft written by Claude. It is waiting for the course
> owner's review (checkpoint H1 in the spike plan). Rules used: imperative
> voice, plain words, no numbers, action genes ≤ 12 words, temperament ≤ 15.

| Locus | Founder alleles | Neutral |
|---|---|---|
| eat | "Eat whenever food is close." · "Only look for food when energy is low." · "Always finish eating before doing anything else." · "Eat quickly, then move on." | "No preference." |
| flee | "Run from any predator you see." · "Flee only when a predator is very close." · "Stay calm unless danger is right next to you." · "Run away from anything that attacks you." | "No preference." |
| follow | "Stay close to other animals." · "Follow others when you are lost or hungry." · "Keep your distance from other animals." · "Follow the strongest animal nearby." | "No preference." |
| wander | "Keep moving to new places." · "Explore when there is nothing else to do." · "Stay near where you last found food." · "Roam far when food is scarce." | "No preference." |
| rest | "Rest when you are tired." · "Never stop moving." · "Rest only when you feel safe." · "Save energy by resting when food is far." | "No preference." |
| mate | "Look for a partner when energy is high." · "Mate with any nearby adult." · "Mate only when food is plentiful." · "Seek a partner before growing old." | "No preference." |
| attack | "Never fight." · "Attack weaker animals when you are hungry." · "Fight anyone who comes too close." · "Attack only to defend your food." | "No preference." |
| risk | "Cautious: safety comes before food." · "Bold: take risks when the reward is food." · "Nervous: any movement nearby means danger." · "Reckless when starving, careful when fed." | "No particular temperament." |
| social | "Social: feels safer in a group." · "Solitary: prefers to be alone." · "Curious about other animals." · "Wary of strangers, friendly to companions." | "No particular temperament." |
| place | "Likes open ground where danger is easy to see." · "Prefers staying near water." · "Attached to familiar places." · "Restless: always wants somewhere new." | "No particular temperament." |

Some founder sentences refer to features the Lab 1 world doesn't have yet
("Prefers staying near water.", "Likes open ground…"). On the flat world they
can only act through how the brain interprets them. Once the terrain lab adds
water, they become testable hypotheses: will a water-loving allele spread when
food grows faster near water?

Two more allele files exist for **measurement and controls**. They never appear
in a normal run; the control sentences become founder genes only in control C4
(§7) ([06](06-experiments-and-results.md#3-test-material)):

- `contrast_alleles_v1.json`: a "pro" and an "anti" sentence per action locus
  ("Always eat, whatever happens." / "Never eat unless starving.") for directed
  tests.
- `control_alleles_v1.json`: 20 shuffled-word sentences ("Needs bakery green
  bread records nine attic.") and 20 irrelevant sentences ("Trains leave from
  the north platform.") for random-text genomes.

## 4. Crossover

`genome.crossover_uniform`: for each of the 10 loci the child takes the allele
of one parent or the other with probability ½ each. Because loci are
homologous, a child always has exactly one gene of each kind.

Other schemes are natural student exercises: one-point crossover, blocks of
loci (action vs temperament), or diploid genomes with dominant and recessive
alleles.

## 5. Mutation

### Rate

After crossover, each of the child's 10 genes mutates with probability
`evolution.p_mut` = 0.03. On average that's 0.3 mutation attempts per child,
and about 26 % of children (1 − 0.97¹⁰) get at least one. A few attempts
change nothing (see below).

### Operators

Each mutation picks one operator at random, in proportion to its weight
(`evolution.operators`):

| Operator | Weight | Share (with LLM) | Share (no LLM) | Uses a model |
|---|---|---|---|---|
| `intensity` | 1.0 | 18 % | 22 % | no |
| `negate` | 1.0 | 18 % | 22 % | no |
| `condition_swap` | 1.0 | 18 % | 22 % | no |
| `synonym` | 1.0 | 18 % | 22 % | no |
| `founder_reintroduce` | 0.5 | 9 % | 11 % | no |
| `llm_rewrite` | 1.0 | 18 % | — | yes (`ollama.mutator_model`) |

Without a mutator, `llm_rewrite` gets weight 0 and the other weights are
rescaled. That happens when `ollama.mutator_model` is `null`, and in
`smoke_run` whenever the brain isn't `llm`. Runs with the random or rule-based
brain therefore need no model at all. If an operator can't change the
sentence, for example `synonym` with no word from its table, that gene simply
doesn't mutate this time.

### Word operators: what they do

Real outputs, produced by running each operator on founder sentences:

| Operator | Rule | Examples |
|---|---|---|
| `intensity` | Moves one step along *never → rarely → sometimes → often → always*. Without such a word, it prefixes *Rarely* or *Often*. | "Never fight." → "Rarely fight." · "Eat whenever food is close." → "Often eat whenever food is close." |
| `negate` | Swaps opposites: *always/never, seek/avoid, stay close to/keep your distance from, run from/stand up to, follow/ignore, keep moving/stay put*. Otherwise it removes a leading *Never/Do not*, prefixes *Not* (temperament), or prefixes *Do not*. | "Never fight." → "Always fight." · "Stay close to other animals." → "Keep your distance from other animals." · "Run from any predator you see." → "Stand up to any predator you see." |
| `condition_swap` | Replaces a known condition (*when hungry, when full, when threatened, when alone, when food is close, when food is scarce, when tired, when safe*) with another, or appends one. | "Never fight." → "Never fight when alone." · "Keep moving to new places." → "Keep moving to new places when alone." |
| `synonym` | Swaps one word from a small table (*close/near, run/dash, fight/attack, tired/exhausted, weaker/smaller…*). | "Run from any predator you see." → "Dash from any predator you see." · "Rest when you are tired." → "Rest when you are exhausted." |
| `founder_reintroduce` | Replaces the gene with another founder or neutral allele of the same locus. | any → e.g. "Flee only when a predator is very close." |

Word operators are cheap and predictable, but blunt. Real outputs include "Rest
when you are tired." → "Rest when you are tired when tired." (the condition list
doesn't recognise *when you are tired*, so a new condition is appended),
"Keep moving to new places." → "Stay put to new places.", and "Attack weaker
animals when you are hungry." → "…when you are hungry when safe.". Improving
them is a good student exercise.

### LLM rewrite

`llm_rewrite` asks the mutator model to rewrite the gene in one of seven
styles, chosen at random: *random change, invert, exaggerate, soften, add a
condition, make more specific, make more general*. The model is **never told
what is good** for survival. The prompt (`prompts/mutate_v1.md`):

```text
Here is a short instruction describing an animal's instinct:

"{text}"

Rewrite it with this kind of change: {style}.
Keep it one plain sentence of at most {max_words} words, about the same behaviour topic.
Reply with the new sentence only.
```

Settings: temperature 0.9 and a seed drawn from the mutation stream, so the
same (gene, style, seed) always gives the same answer. Answers are cached in
`cache/ollama.sqlite`. The prompt says at most 12 words, and the guards below
accept 12 for action genes and 15 for temperament genes.

Real outputs from the probe ([06](06-experiments-and-results.md#52-ollama-probe)),
starting from "Eat whenever food is close.":

| Style | gemma4:26b | gemma4:12b |
|---|---|---|
| invert | "Avoid food whenever it is near." | "Avoid eating whenever food is far away." |
| add a condition | "Eat whenever food is close and you are hungry." | "Eat whenever food is close and you are hungry." |
| random change | "Snack when snacks are nearby." | "Hunt when a prey animal appears nearby." |

The 12b "invert" answer flips two things at once and so is not a clean
inversion. Rewrites are more fluent than word operators but less
predictable. The spike watches whether repeated rewriting makes genes longer
and blander ("bloat") or makes them all alike.

### Guards

Every new sentence from a word operator or the LLM passes through `clean` and
`valid` before it can enter a genome (`founder_reintroduce` copies an existing
founder sentence as is):

- **clean:** keep the quoted sentence if the answer quotes one; strip
  meta-text such as "Here is the modified prompt:"; keep only the first
  sentence; collapse spaces; capitalise; end with a full stop.
- **valid:** 1 to max words long, different from the old text (ignoring
  case), and only plain characters (letters, digits, spaces and `,.'’;:!?-`).

An LLM rewrite gets up to three attempts with different seeds. If all fail, the
mutation falls back to `intensity`, `negate` or `condition_swap`.

Every accepted mutation is logged in the child's `birth` event (locus, parent
allele, new allele, operator, text) and registered as an allele with its
parent, operator, model and seed.

## 6. Selection

There is **no fitness function**. Nothing scores genomes. Animals that find
food, avoid predators and mate leave more children, and their genes become more
common. That is the whole of selection. Concretely, an animal reproduces only
if it:

1. survives to 150 ticks,
2. keeps at least 50 energy,
3. chooses `mate` at a moment when an adjacent partner also chooses `mate`,
4. while the population is below the cap.

Each of these depends on the decisions its genes produce. Selection is
*measured* afterwards for analysis (lifespan, offspring, food eaten, allele
frequencies), never used to choose parents.

Two things weaken selection, and both are watched in the experiments:

- **Newcomers.** Founders added at the floor bring fresh founder genes. If
  they are frequent, they swamp what selection has achieved.
- **The cap.** At the cap, births depend on free slots rather than on finding
  food. That's why food regrowth was tuned to keep the Lab 1 population below
  the cap ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)).

## 7. Experimental controls

Switches in `configs/base.yaml › evolution` turn the simulation into control
conditions (the experiment matrix in the spike plan, A10):

| Control | Setting | Question it answers |
|---|---|---|
| C2 NO-MUT | `p_mut: 0` | How far does selection get with founder variation alone? |
| C3 SHUFFLED | `shuffled: true` | Each decision uses a random *other* living animal's genome. Genes are inherited but don't affect their carrier, so any change is drift. |
| C4 RANDOM-FOUNDERS | `random_founders: true` | Founders get random-text genes. Can evolution climb out of nonsense? |
| C5 RULE-BASED | `--backend rule_based` | The same experiment with the transparent keyword brain. |
| C7 ASEXUAL | `sexual: false` | One parent, copy and mutation: the old lab's regime. |

## 8. Reading evolution from a run

Everything needed to study evolution after the fact is logged
([03 §12](03-world-and-simulation.md#12-what-a-run-writes-to-disk)):

- `events.jsonl` → births with parents, genomes and mutations; deaths with
  lifespan, food and offspring.
- `alleles.jsonl` → every allele's text and origin, with parent links for
  lineage trees.
- `final_population.json` → the surviving genomes.
- `stats.csv` → population, energy, generations and the number of alleles over
  time.

**Not built yet:** the analysis layer the design promises for students:
allele frequencies per locus over time, lineage trees with gene diffs, a "top
surviving genes" table, per-locus diversity and gene length. These are planned
for the spike's S6/S7 sessions and would make a natural Lab 1 exercise. The
building blocks exist: `metrics.shannon_diversity` and `metrics.bootstrap_ci`.
