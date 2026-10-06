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

A **genome** has 5 **loci** (gene slots), always in the same order. Each locus
holds one short English sentence, its **allele**.

| Locus | Role | Max words |
|---|---|---|
| `eat` | when and how much to go for food | 12 |
| `flee` | reaction to predators | 12 |
| `follow` | staying with other animals | 12 |
| `rest` | saving energy | 12 |
| `mate` | reproduction | 12 |

There is one gene per behaviour: each gene belongs to the action of the same
name, one of the five the brain can choose
([03 §6](03-world-and-simulation.md#6-actions-the-five-behaviours)). The LLM
brain sees the genes as a labelled list ([05](05-decision-backends.md#the-prompt)).

Until 2026-10-07 the genome had 10 genes: 7 action genes (the five above plus
`wander` and `attack`) and 3 temperament genes (`risk`, `social`, `place`).
The owner asked to keep the genes to a minimum before predators become genetic
animals too ([09](09-status-and-roadmap.md#3-decisions-taken)). The runs in
[10](10-natural-selection-runs.md) and [11](11-gene-development.md) used the
old genome.

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
| `operator`, `model`, `seed` | how it was made (mutants only); `operator` is `llm#<n>`, the number of the mutation instruction drawn |

The `AlleleRegistry` holds every allele seen in a run. The same text at the
same locus is always the same allele, so if two mutations produce the same
sentence they share one id. A **genome** is just the tuple of 5 allele ids. Its
`genome_key` is a hash of the 5 texts, so the same genes have the same key in
every run. The caches use this key.

`parent_id` links form a **lineage**: you can follow any gene back through its
mutations to the founder sentence it came from.

## 3. The founder pool

Every run starts from the same frozen pool, `data/founder_pool_v2.json`. Each
locus offers four instinct-like sentences plus a **neutral** allele that
switches the drive off. A founder (and every later newcomer) draws one of the
five options per locus uniformly at random, so there are 5^5 = 3 125 possible
starting genomes. All runs share the same origin and can be compared.

> **Status:** the pool is a draft written by Claude. It is waiting for the
> course owner's review (checkpoint H1 in the spike plan). v2 (2026-10-07)
> keeps the v1 sentences of the five remaining slots. Rules used: imperative
> voice, plain words, no numbers, at most 12 words.

| Locus | Founder alleles | Neutral |
|---|---|---|
| eat | "Eat whenever food is close." · "Only look for food when energy is low." · "Always finish eating before doing anything else." · "Eat quickly, then move on." | "No preference." |
| flee | "Run from any predator you see." · "Flee only when a predator is very close." · "Stay calm unless danger is right next to you." · "Run away from anything that attacks you." | "No preference." |
| follow | "Stay close to other animals." · "Follow others when you are lost or hungry." · "Keep your distance from other animals." · "Follow the strongest animal nearby." | "No preference." |
| rest | "Rest when you are tired." · "Never stop moving." · "Rest only when you feel safe." · "Save energy by resting when food is far." | "No preference." |
| mate | "Look for a partner when energy is high." · "Mate with any nearby adult." · "Mate only when food is plentiful." · "Seek a partner before growing old." | "No preference." |

Some founder sentences refer to things an animal doesn't sense. "Follow the
strongest animal nearby." is an example: the observation says nothing about
the other animal's strength ([03 §5](03-world-and-simulation.md#5-perception-what-an-animal-knows)).
Such genes can only act through how the brain interprets them.

Two more allele files exist for **measurement and controls**. They never appear
in a normal run; the control sentences become founder genes only in control C4
(§7) ([06](06-experiments-and-results.md#3-test-material)):

- `contrast_alleles_v2.json`: a "pro" and an "anti" sentence per locus
  ("Always eat, whatever happens." / "Never eat unless starving.") for directed
  tests.
- `control_alleles_v1.json`: 20 shuffled-word sentences ("Needs bakery green
  bread records nine attic.") and 20 irrelevant sentences ("Trains leave from
  the north platform.") for random-text genomes.

## 4. Crossover

`genome.crossover_uniform`: for each of the 5 loci the child takes the allele
of one parent or the other with probability ½ each. Because loci are
homologous, a child always has exactly one gene of each kind.

Other schemes are natural student exercises: one-point crossover, blocks of
loci, or diploid genomes with dominant and recessive alleles.

## 5. Mutation

Mutation is **blind**. It doesn't know the world, the other genes or what
helps; selection alone decides what stays (owner decision, 2026-10-02; the
review behind it is `prototype/notes/mutation-review.md`).

### Rate

After crossover, each of the child's 5 genes mutates with probability
`evolution.p_mut` = 0.03. On average that's 0.15 mutations per child, and
about 14 % of children (1 − 0.97⁵) get at least one.

### One operator: the LLM makes a random change

For each mutating gene, the code draws one instruction at random from
`prompts/mutate_v2.txt` and sends it, with the gene sentence and nothing
else, to the mutator model (`ollama.mutator_model`, gemma4:12b):

```text
Randomly change one word in this sentence.

"Rest when you are tired."

Reply with the new sentence only.
```

All 16 instructions are variants of "make a random change". Some ask for a
small edit (change, add, remove or swap words), others for a big one
("Randomly change the meaning of this sentence a lot.", "Change this sentence
in a random way, big or small."). Randomness comes from three places: the
instruction drawn, the seed (drawn from the simulation's `mutation` stream,
so a run can be replayed) and the sampling temperature
(`evolution.temperature`, 1.2). Answers are cached in `cache/ollama.sqlite`.

There is no other way for a gene to change. Without a mutator model
(`ollama.mutator_model: null`, or `smoke_run --no-mutation`), children only
recombine their parents' genes. To add an instruction, add a line to the file.

### Guards

The answer goes through `clean` (keep the quoted sentence if there is one,
strip meta-text such as "Here is the new sentence:", keep the first sentence,
capitalise, end with a full stop) and `valid` (1–12 words, set by
`evolution.max_words`; different from the old text; plain characters only).
If it fails, the gene doesn't mutate this time. The guards check form, never
meaning: 99 % of answers pass.

Every accepted mutation is logged in the child's `birth` event (locus, parent
allele, new allele, instruction number, text) and registered as an allele with
its parent, `operator` = `llm#<instruction number>`, model and seed.

### One mutation: what comes out

`python -m experiments.mutation_test` mutates every founder sentence with 8
seeds at four temperatures, with no selection (`results/mutation_test.md`,
2026-10-02, gemma4:12b, 2 000 calls in about 4 minutes, on the 10-slot
founder pool of the time):

| Temperature | 0.9 | 1.2 | 1.5 | 2.0 |
|---|---|---|---|---|
| valid answers | 99 % | 99 % | 99 % | 99 % |
| different mutants per sentence (out of 8) | 7.0 | 7.1 | 7.2 | 7.3 |
| words changed per mutation | 2.9 | 3.0 | 3.0 | 3.0 |
| one-word edits | 47 % | 46 % | 46 % | 47 % |
| big jumps (little left of the parent) | 21 % | 21 % | 22 % | 22 % |
| length change (words) | +0.19 | +0.15 | +0.08 | +0.07 |
| no effect on the keyword brain | 50 % | 51 % | 52 % | 55 % |

- **The instruction matters more than the temperature.** Nine small-edit
  instructions change 1–2 words and almost never jump (0–3 %). Three change
  about 3 words ("a few words", "one part", "an unexpected change"). "Change
  this sentence randomly.", "Mutate this sentence at random.", "…change the
  meaning of this sentence a lot." and "…big or small." rewrite 6–7 words and
  jump in 41–88 % of cases. Raising the temperature from 0.9 to 2.0 adds only 0.3
  different mutants per sentence.
- **One mutation usually stays on topic.** 87 % of mutants still use a word
  of the animal's world (the founder sentences' words and the situation
  words), but 61 % bring in an unrelated word: "Run from any butterfly you
  see.", "Eat whenever food is expensive.", "Never dance.".

### Many mutations, no selection: where genes go

The test also mutates founder sentences 30 times in a row at each temperature,
keeping each new sentence. On 2026-10-02 it took six (24 lineages), the first
of the eat, flee, follow, rest, attack and risk slots (today: one per slot of
the five):

| Mutations so far | 0 | 1 | 3 | 5 | 10 | 15 | 20 | 30 |
|---|---|---|---|---|---|---|---|---|
| genes still using a word of the animal's world | 100 % | 83 % | 83 % | 67 % | 58 % | 21 % | 17 % | 12 % |

"Rest when you are tired." at temperature 1.2:

```text
 1  Run when you are tired.
 2  Stop when you are tired.
 3  Stop when you are hungry.
 7  When are you hungry, stay?
 9  Stay, when are you toaster?
21  Please, is the pizza ready?
24  The gravitational waves are screaming.
30  The dancing is a banana wave.
```

"Never fight.": 1 "Never fly." → 3 "Don't sneeze." → 5 "Sneezes banana." →
14 "Cybernetic gears hum." → 30 "The toaster ate the pancakes quickly.".

- **Genes leave the animal's world after 10–15 mutations.** After 30 the
  sentence shares almost nothing with its founder, and genes grow from 4.7 to
  6.5–7.8 words.
- **The model's "random" has favourite words.** "Toaster" appears in 18 of
  the 24 lineages, "bicycle" in 13, "gravity" in 11, "banana" in 10. LLM
  randomness is a style with its own biases, not a uniform draw.
- **In a run, selection is the only force against this drift.** A gene meets
  about 0.03 mutations per generation, so about 1.4 in a 10 000-tick Lab 1 run
  (≈ 46 generations): the first steps above. In longer runs, watch whether
  selection keeps genes meaningful. The keyword brain reads only its keywords,
  so most nonsense is neutral for it and can spread by drift.

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
frequencies), never used to choose parents. How it is measured, and what
230-generation runs with predators showed, is in
[10 — Natural selection in long runs](10-natural-selection-runs.md).

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
| C2 NO-MUT | `p_mut: 0` (or `smoke_run --no-mutation`) | How far does selection get with founder variation alone? |
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

**First analysis tool:** `python -m experiments.gene_report results/runs/<run>`
ranks every gene by the fitness of the animals that carried it (their mean
number of offspring, relative to all animals), with lifespan, the share
killed by predators, frequency at the start, peak and end, and the lineage of
mutant genes; ★ marks the best gene of each slot when the difference is clear
([08 §4](08-code-and-config-reference.md)). **Timeline:**
`python -m experiments.gene_timeline results/runs/<run>` adds share charts
per slot, diversity over time, the genes that swept with their lineages,
gene dropping to tell selection from drift, and behaviour over time
([11](11-gene-development.md)). Whether Lab 1 students should write these
themselves is an open decision
([09 §4](09-status-and-roadmap.md#4-decisions-waiting-on-the-course-owner)).

`gene_report` and `gene_timeline` take the slots from the run itself, so runs
made before 2026-10-07, with 10 slots, stay readable. `gene_swap` needs a run
made with the current 5 genes.
