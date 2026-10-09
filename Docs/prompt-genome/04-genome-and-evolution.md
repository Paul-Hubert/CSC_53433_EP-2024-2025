# 04 — Genome and evolution

What a gene is, where the first genes come from, and how genes change from one
generation to the next, for both species: prey animals and, since 2026-10-07,
predators. Code: `prototype/promptevo/species.py`, `genome.py`, `founder.py`,
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

Both species are genetic animals. A **genome** has one **locus** (gene slot)
per action of its species, always in the same order. Each locus holds one short
English sentence, its **allele**.

| Species | Locus | Role | Max words |
|---|---|---|---|
| prey | `eat` | when and how much to go for food | 12 |
| prey | `flee` | reaction to predators | 12 |
| prey | `follow` | staying with other animals | 12 |
| prey | `rest` | saving energy | 12 |
| prey | `mate` | reproduction | 12 |
| predator | `hunt` | when and how to chase prey | 12 |
| predator | `follow` | staying with other predators (since 2026-10-07, pool v2) | 12 |
| predator | `rest` | saving energy, waiting | 12 |
| predator | `mate` | reproduction | 12 |

There is one gene per behaviour: each gene belongs to the action of the same
name, one of the actions the brain can choose for that species
([03 §6](03-world-and-simulation.md#6-actions-five-for-prey-four-for-predators)).
The LLM brain sees the genes as a labelled list ([05](05-decision-backends.md#the-prompt)).

In ids and files, predator loci carry the prefix `predator.`: `predator.hunt`,
`predator.follow`, `predator.rest`, `predator.mate`. One registry then holds both species,
although `rest` and `mate` exist in both. The prey loci keep their plain names,
so older runs and caches stay valid. The brain sees the plain action names.

Until 2026-10-07 the genome had 10 genes: 7 action genes (the five above plus
`wander` and `attack`) and 3 temperament genes (`risk`, `social`, `place`), and
predators were scripted. The owner asked to keep the genes to a minimum, then
to make predators genetic animals the same way
([09](09-status-and-roadmap.md#3-decisions-taken)). The runs in
[10](10-natural-selection-runs.md) and [11](11-gene-development.md) used the
old genome and scripted predators.

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
| `id` | `locus:n`, e.g. `eat:3` or `predator.hunt:0`, numbered in order of appearance within a run |
| `text` | the sentence (whitespace normalised) |
| `origin` | `founder`, `neutral`, `contrast`, `control`, `mutant` or `ood` |
| `parent_id` | the allele it was mutated from (mutants only) |
| `operator`, `model`, `seed` | how it was made (mutants only); `operator` is `llm#<n>`, the number of the mutation instruction drawn |

The `AlleleRegistry` holds every allele seen in a run, of both species. The
same text at the same locus is always the same allele, so if two mutations
produce the same sentence they share one id. A **genome** is the tuple of its
allele ids (5 for a prey animal, 4 for a predator) and its species. Its
`genome_key` is a hash of the texts, so the same genes have the same key in
every run. A predator's key also includes the species. The caches use this key.

`parent_id` links form a **lineage**: you can follow any gene back through its
mutations to the founder sentence it came from.

## 3. The founder pool

Every run starts from the same frozen pools: `data/founder_pool_v2.json` for
the prey and `data/predator_founder_pool_v2.json` for the predators. Each
locus offers four instinct-like sentences plus a **neutral** allele that
switches the drive off. A founder (and every later newcomer) draws one of the
five options per locus uniformly at random. That gives 5^5 = 3 125 possible
prey genomes and 5^4 = 625 predator genomes. All runs share the same origin
and can be compared.

> **Status:** both pools are drafts written by Claude. They are waiting for the
> course owner's review (checkpoint H1 in the spike plan). The prey pool v2
> (2026-10-07) keeps the v1 sentences of the five remaining slots; the predator
> pool was written on 2026-10-07 (v1: hunt, rest, mate; v2 adds follow). Rules used: imperative voice, plain words,
> no numbers, at most 12 words.

**Prey:**

| Locus | Founder alleles | Neutral |
|---|---|---|
| eat | "Eat whenever food is close." · "Only look for food when energy is low." · "Always finish eating before doing anything else." · "Eat quickly, then move on." | "No preference." |
| flee | "Run from any predator you see." · "Flee only when a predator is very close." · "Stay calm unless danger is right next to you." · "Run away from anything that attacks you." | "No preference." |
| follow | "Stay close to other animals." · "Follow others when you are lost or hungry." · "Keep your distance from other animals." · "Follow the strongest animal nearby." | "No preference." |
| rest | "Rest when you are tired." · "Never stop moving." · "Rest only when you feel safe." · "Save energy by resting when food is far." | "No preference." |
| mate | "Look for a partner when energy is high." · "Mate with any nearby adult." · "Mate only when food is plentiful." · "Seek a partner before growing old." | "No preference." |

**Predators:**

| Locus | Founder alleles | Neutral |
|---|---|---|
| hunt | "Chase any prey you see." · "Hunt only when you are hungry." · "Attack only when prey is close." · "Keep chasing until the prey is caught." | "No preference." |
| follow | "Stay close to other predators." · "Hunt as a pack." · "Keep away from other predators." · "Follow others when no prey is in sight." | "No preference." |
| rest | "Rest when your belly is full." · "Never stop moving." · "Lie still and let prey come to you." · "Rest when no prey is in sight." | "No preference." |
| mate | "Look for a mate when well fed." · "Mate with any nearby adult." · "Hunt first, mate later." · "Seek a partner before growing old." | "No preference." |

The predator sentences cover the same kinds of rule as the prey's: a drive
that is always on, one tied to energy, one tied to distance, and an odd one
("Hunt first, mate later." in the mate slot).

Some founder sentences refer to things an animal doesn't sense. "Follow the
strongest animal nearby." is an example: the observation says nothing about
the other animal's strength ([03 §5](03-world-and-simulation.md#5-perception-what-an-animal-knows)).
Such genes can only act through how the brain interprets them.

Two more allele files exist for **measurement and controls**. They never appear
in a normal run; the control sentences become founder genes only in control C4
(§7) ([06](06-experiments-and-results.md#3-test-material)):

- `contrast_alleles_v2.json` and `predator_contrast_alleles_v2.json`: a "pro"
  and an "anti" sentence per locus ("Always eat, whatever happens." / "Never
  eat unless starving.", "Always hunt, whatever happens." / "Never hunt unless
  starving.") for directed tests.
- `control_alleles_v1.json`: 20 shuffled-word sentences ("Needs bakery green
  bread records nine attic.") and 20 irrelevant sentences ("Trains leave from
  the north platform.") for random-text genomes.

## 4. Crossover

`genome.crossover_uniform`: for each locus (5 for prey, 4 for predators) the
child takes the allele of one parent or the other with probability ½ each.
Because loci are homologous, a child always has exactly one gene of each kind.
Parents are always of the same species.

Other schemes are natural student exercises: one-point crossover, blocks of
loci, or diploid genomes with dominant and recessive alleles.

## 5. Mutation

Mutation is **blind**. It doesn't know the world, the other genes or what
helps; selection alone decides what stays (owner decision, 2026-10-02; the
review behind it is `prototype/notes/mutation-review.md`).

### Rate

After crossover, each of the child's genes mutates with probability
`evolution.p_mut` = 0.03. A prey child gets 0.15 mutations on average, and
about 14 % of prey children (1 − 0.97⁵) get at least one. For a predator
child it's 0.12 and 11 % (1 − 0.97⁴). Predator genes mutate exactly like prey
genes, with the same instructions, model and guards. Since litters (2026-10-09)
every baby of a litter gets its own crossover and its own mutation draws
([03 §8](03-world-and-simulation.md#8-reproduction)), so siblings usually
differ. The measured rates match: 147–150 mutations per 1 000 prey births and
115–116 per 1 000 predator births.

### One operator: the LLM makes a small random change

For each mutating gene, the code draws one instruction at random from
`prompts/mutate_v4.txt` (since 2026-10-08) and sends it, with the gene sentence
and one fixed context line (`evolution.mutation_context`), to the mutator model
(`mutator.model`, below):

```text
The sentence below is a rule that a wild animal follows.
Make the rule in this sentence a little weaker.

"Rest when you are tired."

Reply with the new sentence only.
```

The 7 instructions ask for small edits of what the rule says: make it a
little weaker, change when it applies, add a short condition, remove a
condition (or make it simpler), change how near, how far or how much it is
about, make it say the opposite, or change one word into a related word. The
context line is the same for every gene of both species. Which instruction is
drawn is random, so mutation stays blind: the mutator never sees the world's
details, the other genes, the gene's slot or how well the animal does. Randomness comes from three places: the instruction
drawn, the seed (drawn from the simulation's `mutation` stream, so a run can be
replayed) and the sampling temperature (`evolution.temperature`, 1.2). Answers
are cached in `cache/ollama.sqlite` (`cache/mutator.sqlite` for a mutator on an
OpenAI-compatible server).

From 2026-10-02 to 2026-10-07 the 16 instructions of `prompts/mutate_v2.txt`
asked for random word edits, some of them big ("Randomly change the meaning of
this sentence a lot.", "Make an unexpected change to this sentence."). Genes
then left the animal's world within 10–15 mutations (below). The owner asked
for mutation that stays random but small and keeps to meaning most of the
time ([below](#small-edits-since-2026-10-08)). The first version of the small
edits (`mutate_v3.txt`, 9 instructions, no context line) is kept for the
record.

There is no other way for a gene to change. Without a mutator model
(`mutator.model: null`, or `smoke_run --no-mutation`), children only
recombine their parents' genes. To add an instruction, add a line to the file.

### The mutator model

The mutator has its own section, `mutator.*`, and its own address, separate
from the brain's (since 2026-10-08):

| Period | Model | Where | Why |
|---|---|---|---|
| 2026-10-02 to 2026-10-08 | gemma4:12b (the LLM brain's model) | Ollama, GPU | one model for both jobs, no model swaps |
| since 2026-10-08 | qwen3.5:0.8b | Ollama, CPU (`options: {num_gpu: 0, num_ctx: 1024}`) | the JEV brain fills the GPU ([05](05-decision-backends.md)); a small model was thought enough, since mutations are random anyway |
| tried 2026-10-09 | gemma4:26b | Ollama, CPU | owner: bigger CPU models are fine; `--set mutator.model=gemma4:26b` |

On 2026-10-09 the two CPU models ran the same 1-hour JEV run (seed 1234, the
world of [03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world);
[06 §5.22](06-experiments-and-results.md#522-a-bigger-cpu-mutator)):

| | qwen3.5:0.8b | gemma4:26b |
|---|---|---|
| Answers rejected by the guards and drawn again | 22 % | 3 % |
| Words changed per prey mutation | 5.2 | 2.4 |
| Prey mutations changing 4 or more words | 64 % | 24 % |
| "Change one word …": words changed | 4.2 | 1.0 |
| Mutants using a word of the animal's world (prey / predators) | 88 % / 69 % | 95 % / 94 % |
| Different mutants / mutations | 321 / 325 | 224 / 357 (75 made more than once) |
| Time per call on the CPU (16-core Ryzen 9 9950X3D) | 0.4 s | 0.9 s |
| Memory while loaded | about 2 GB | about 19 GB |

gemma4:26b follows the small-edit instructions and keeps nearly every mutant a
readable rule ("Rest only when you feel safe." → "Rest only when you feel
unsafe."), but it often gives the same answer to the same request, so it offers
fewer new variants. qwen3.5:0.8b rewrites most of the sentence and often loses
the meaning ("No preference." → "Nobody prefers me."). gemma4:26b is a
mixture-of-experts model, which runs only part of its weights per token, so it
is faster on the CPU than the dense gemma4:12b (1.1 s). Its process holds the
whole model file in RAM, about twice what Ollama reports. The default is still
qwen3.5:0.8b; switching means setting `mutator.model: gemma4:26b`.

### Guards

The answer goes through `clean` (keep the quoted sentence if there is one,
strip meta-text such as "Here is the new sentence:", keep the first sentence,
capitalise, end with a full stop) and `valid` (1–12 words, set by
`evolution.max_words`; different from the old text; plain characters only). A
rejected answer is drawn again, with a new instruction and seed, up to 5
attempts (`evolution.mutation_tries`; one attempt before 2026-10-08). The guards
check form, never meaning: with gemma4 about 97–99 % of answers pass, with
qwen3.5:0.8b 78 %; after the redraws nearly every mutation happens. Setting `mutation_prompts` to `prompts/mutate_v2.txt` and
`mutation_tries` to 1 gives back the mutation of 2026-10-02.

Two more checks were tried on 2026-10-08 and removed the same day on the
owner's request: at most 3 words changed, and only words of a 493-word list of
the animal's world ([below](#small-edits-since-2026-10-08)).

Every accepted mutation is logged in the child's `birth` event (locus, parent
allele, new allele, instruction number, text) and registered as an allele with
its parent, `operator` = `llm#<instruction number>`, model and seed.

### One mutation: what comes out

This and the next section describe the instructions of 2026-10-02
(`mutate_v2.txt`); today's rules are compared with them
[at the end](#small-edits-since-2026-10-08).

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

### Small edits (since 2026-10-08)

The owner asked for mutation that stays random but small and keeps to meaning
most of the time. The instructions became small edits of the rule
(`mutate_v3.txt`, 9 instructions). For a few hours two checks were added as
well (at most 3 words changed, only words of a world word list, with redraws),
then removed on the owner's request. Then, also on the owner's request, every
mutation prompt got one context line ("The sentence below is a rule that a wild
animal follows.") and the two instructions that rewrote the most words were
dropped ("a little stronger", "say this rule in slightly different words"):
`mutate_v4.txt`, 7 instructions. Measured with `experiments.mutation_test` on the 36
founder sentences of both species (8 seeds each, and 30 mutations in a row from
each), temperature 1.2, no selection. gemma4:12b judged whether each gene still
gives a usable rule for its slot (the question of `gene_timeline --judge`):

| | Before (v2) | v3 with the checks (removed) | v3 alone | **v4: context line, 7 instructions (today)** |
|---|---|---|---|---|
| mutations that succeed | 99 % | 88 % (2.4 attempts each) | 100 % | **100 %** |
| words changed per mutation · big edits (≥ 4 words) | 2.7 · 28 % | 1.8 · 0 % | 3.1 · 39 % | **2.8 · 30 %** |
| different mutants per sentence (of 8) | 7.2 | 4.6 | 6.7 | **6.1** |
| one mutation: still a usable rule | 69 % | 83 % | 80 % | **81 %** |
| usable after 1 / 5 / 10 / 20 / 30 mutations | 61 / 36 / 8 / 8 / 0 % | 92 / 75 / 67 / 53 / 44 % | 89 / 67 / 56 / 39 / 33 % | **89 / 78 / 50 / 50 / 36 %** |
| using a world word after 1 / 5 / 10 / 20 / 30 mutations | 81 / 53 / 42 / 31 / 22 % | 100 / 97 / 81 / 89 / 83 % | 94 / 81 / 53 / 47 / 33 % | **100 / 89 / 83 / 78 / 72 %** |
| words per gene after 30 mutations (6.1 at the start) | 8.6 | 6.5 | 7.0 | **7.0** |

"Rest when you are tired.", 30 mutations in a row:

```text
     v2 (before)                                v3 alone                                          v4 (today)
 1   Exterminate the fruit of your ancestors.   Take a break when you feel exhausted.             Sleep when you are tired.
 5   Bake a batch of chocolate chip cookies.    You should not take a break if you are ...        Stay awake even if you feel exhausted.
10   Fry a batch of muffins.                    You are required to take a break.                 Be aware that a hunter might be nearby.
20   The cookies are waffles.                   Breaks may be taken.                              A predator is approaching.
30   Sometimes donuts are telescope.            Breaks are mandatory except during the ...        The hunter may hunt.
```

- **One mutation usually keeps the meaning.** About 80 % of single mutations
  are still a usable rule (69 % before), and they bring in plain rule words
  ("try", "avoid", "approach") rather than random objects ("toaster", "purple",
  "dance" before).
- **The context line keeps genes about animals.** Without it (v3), genes
  drifted into office, school or game rules after 20–30 mutations ("Achievements
  must be unlocked before the final boss."); only 33 % still used a word of the
  world after 30. With it, 72 % do, and the genes read like animal behaviour
  ("Stay hidden.", "Fight if cornered.", "Hide when a predator is nearby.").
- **What still drifts:** genes bring in wildlife that the simulation doesn't
  have ("Avoid the village unless you are very hungry.", "Stay within the
  forest.", distances in miles), and they can wander into another slot's topic
  (the rest gene above ends as a rule about hunters). "Remove a condition, or
  make it simpler" gives the fewest usable rules (41 %): it can strip a gene
  down to one word.
- **Edits are smaller than in v3 but not small:** 2.8 words on average, 30 %
  of them 4 words or more ("add a short condition" adds 4.9).
- **Rules weaken a little.** Without "a little stronger", "a little weaker" has
  no counterpart: genes with firm words (always, never, must, only, every) fall
  from 25 % to 8–14 % over the lineages (19–25 % with v3), while hedging words
  (may, might, try, sometimes) rise to 14–19 %.
- **Judge:** strict; it also rejects sensible paraphrases ("Keep your distance
  from other creatures.") and unwise but clear rules ("Rest only when you feel
  unsafe."), so every column understates the meaningful genes.
- Details, the variants tried and the per-instruction numbers:
  [06 §5.17](06-experiments-and-results.md#517-small-mutations-that-keep-their-meaning).

## 6. Selection

There is **no fitness function**. Nothing scores genomes. Prey animals that
find food, avoid predators and mate leave more children; predators that catch
prey and mate leave more children. Their genes become more common. That is the
whole of selection. Concretely, an animal of either species reproduces only
if it:

1. survives to 150 ticks,
2. keeps at least 50 energy,
3. chooses `mate` next to a ready partner of its species, or is that ready
   partner (since 2026-10-07 one partner's choice is enough; before, both had
   to choose `mate`),
4. and, with the old cap rule (`block`, before 2026-10-09), while its species
   is below its cap. Since then births go on at the cap and random animals
   migrate away instead
   ([03 §9](03-world-and-simulation.md#9-population-limits-cap-and-floor)).

A mating gives 2–4 babies since 2026-10-09, each with its own genes, so a pair
that breeds often spreads its genes fast.

Since 2026-10-07 the two species evolve together. The prey's flee genes face
predators whose hunt genes evolve, and the other way round. Predators are
fewer than the prey: about 66 against 265 in the 192 × 192 world and 6 against
38 in the small one
([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)),
so chance (drift) weighs more on their genes than on the prey's.

Each of these depends on the decisions its genes produce. Selection is
*measured* afterwards for analysis (lifespan, offspring, food eaten, allele
frequencies), never used to choose parents. How it is measured, and what
230-generation runs with predators showed, is in
[10 — Natural selection in long runs](10-natural-selection-runs.md).

Four things weaken selection, and all are watched in the experiments:

- **Newcomers.** Founders added at the floor bring fresh founder genes. If
  they are frequent, they swamp what selection has achieved, and a crash that
  empties a species restarts its evolution from the founders: in the long
  gemma run of 2026-10-08 both species were wiped and refilled this way
  ([06 §5.18](06-experiments-and-results.md#518-the-long-llm-run-with-mutation-v4)).
  Since litters and migration (2026-10-09) no run has needed a newcomer.
- **The cap.** With the old rule (`block`), births at the cap depend on free
  slots rather than on finding food. That's why food regrowth was first tuned
  to keep the population below the cap.
- **Random deaths.** With migration, an animal that leaves is picked at random:
  its genes don't matter. When migration was most of the deaths (57 % of prey
  deaths in the 10-hour JEV run with plentiful food), gene shares moved about
  as much as chance alone predicts
  ([06 §5.20](06-experiments-and-results.md#520-jev-runs-egg-bank-cover-litters-and-migration)).
  Less food, more dangerous predators and cover brought migration down to
  20–23 % of prey deaths with the JEV brain
  ([06 §5.21](06-experiments-and-results.md#521-an-evolution-test-world)).
- **Small differences.** About 135–270 prey means a gene needs an advantage of
  a few percent to beat chance. Doubling the world and the populations on
  2026-10-09 was meant to help here.

The C3 control below and a common-garden test (evolved against founder
genomes in the same world, gate G4) are the ways to show that a change is
selection and not chance.

## 7. Experimental controls

Switches in `configs/base.yaml › evolution` turn the simulation into control
conditions (the experiment matrix in the spike plan, A10). They apply to both
species:

| Control | Setting | Question it answers |
|---|---|---|
| C2 NO-MUT | `p_mut: 0` (or `smoke_run --no-mutation`) | How far does selection get with founder variation alone? |
| C3 SHUFFLED | `shuffled: true` | Each decision uses the genome of a random *other* living animal of the same species. Genes are inherited but don't affect their carrier, so any change is drift. |
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

Both tools read the prey by default; `--species predator` reads the
predators' genes. They take the slots from the run itself, so runs made before
2026-10-07, with 10 slots, stay readable. `gene_swap` reads prey slots only and
needs a run made with the current 5 prey genes. Until 2026-10-08
`gene_timeline`'s table "What mutation offers" counted the mutants of both
species in each species' report; it now counts only the species read.

**Every mutation of a run:** `python -m experiments.mutation_list results/runs/<run>`
writes `results/<run>_mutations.md`: for each mutant gene of both species its
slot, parent text, new text, the instruction drawn, words changed, whether it
uses a word of the animal's world, the most living carriers at once and the
carriers at the end. It prints a summary per instruction.
