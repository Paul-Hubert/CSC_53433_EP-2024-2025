# 06 — Experiments, metrics and results

The project's central claim, that *evolution can improve behaviour by rewriting
English sentences*, only holds if the brain really reads the genes. This page
explains how that is measured, which pass/fail **gates** were fixed in advance,
and every result so far, dated. The full plan with sessions and budgets is
`Docs/redesign/08-phase0-spike-plan.md`.

> **Genome and world changes (2026-10-07).** Every result on this page up to
> §5.10 used the 10-gene genome of before that date (7 action genes, including
> wander and attack, plus 3 temperament genes: risk, social, place), scripted
> predators and a 12-cell vision with near/far distances. The current genome
> has 5 genes, one per action
> ([04 §1](04-genome-and-evolution.md#1-genes-are-sentences-in-fixed-slots)).
> Since the same day predators are genetic animals, and both species see 20
> cells with distances in bands ([03 §3](03-world-and-simulation.md#3-predators)).
> §5.11 used the 5-gene genome with the old world; §5.12 is the first run of
> today's world.

## Contents

1. [Why measure before building](#1-why-measure-before-building)
2. [Metrics](#2-metrics)
3. [Test material](#3-test-material)
4. [Gates G1–G5](#4-gates-g1g5)
5. [Results so far](#5-results-so-far)
6. [Known issues and open questions](#6-known-issues-and-open-questions)
7. [Next experiments](#7-next-experiments)
8. [Reproducing the results](#8-reproducing-the-results)

---

## 1. Why measure before building

Three risks can sink the idea (`Docs/redesign/02-assessment.md`):

- **R1 — Genes barely matter, or matter chaotically.** If the brain ignores
  the genes, every genome behaves the same and selection acts on noise. If one
  changed word flips behaviour completely, children don't resemble their
  parents and nothing accumulates. Evolution needs *heritable, graded*
  variation.
- **R2 — Compute.** Every new (genome, situation) pair costs an LLM call.
- **R3 — Mutation collapses diversity.** Repeated LLM rewriting may make
  genes longer, blander and alike (measured for the blind LLM mutation in
  §5.7).

So the prototype is a headless, measurable Python spike, and the Unity work
waits for its verdict.

## 2. Metrics

All in `promptevo/metrics.py`. Distributions are over the 7 actions, and
information is in bits. P[g, o, a] is the probability of action a for genome g
in observation o.

| Metric | Definition | Question |
|---|---|---|
| Entropy H(p) | −Σ p log₂ p | how uncertain is a decision? |
| **MI_G** | mean over observations of H(mean over genomes of p) − mean over genomes of H(p) | how much does the *genome* decide the action? (mutual information between genome and action) |
| **MI_O** | the same with the roles of genome and observation swapped | how much does the *situation* decide the action? |
| JSD(p, q) | H((p+q)/2) − ½ (H(p) + H(q)), between 0 and 1 | how different are two decisions? |
| Behaviour distance d(g, g′) | mean over observations of JSD | how differently do two genomes behave? |
| **Directed ΔP** | for a contrast pair at locus ℓ: mean over relevant observations of p(ℓ \| pro) − p(ℓ \| anti); **sign accuracy** = share of cases with ΔP > 0 | does a gene push its own action in the stated direction? |
| **Locality ratio** | median d(parent, child with one gene mutated) ÷ median d(two unrelated founders) | are small edits small changes? |
| **Spearman ρ** | rank correlation between text distance (1 − difflib similarity) and behaviour distance | do more different texts behave more differently? |
| **Gibberish → neutral** | mean d(random-text genome, all-neutral genome), compared with founder → neutral | does nonsense stay close to "no preference"? |
| Shannon diversity | entropy of allele frequencies | how diverse is a locus? (evolution analysis) |
| Bootstrap CI | resampled confidence interval of a mean | is a difference real? (G4) |

Relevant observations for directed tests: `eat` needs food in sight, `flee` a
predator, `follow` an animal, `mate` and `attack` an animal near; `wander` and
`rest` use all observations.

## 3. Test material

| Material | Content |
|---|---|
| Observation set `data/observations_v1.jsonl` (results on this page) | 48 situations: 32 adult situations drawn from the 54 combinations of energy (low/medium/high) × food (none/far/near) × predator (none/near) × other animal (none / near, ready and weaker / near, not ready and stronger), plus the 16 most frequent other situations of a rule-based run (8 adult, 8 young) |
| Observation set `data/observations_v2.jsonl` (since 2026-10-07) | the same recipe with distance bands: 32 adult situations from energy × food (none/far/close) × predator (none/close) × other animal (none / adjacent and ready / adjacent and not ready), plus the 16 most frequent other prey situations of a rule-based run with predators (6 adult, 10 young) |
| Founder genomes | random draws from the founder pool |
| Random-text genomes | every gene drawn from `control_alleles_v1.json`: 20 shuffled-word sentences ("Needs bakery green bread records nine attic.") and 20 irrelevant ones ("Trains leave from the north platform.") |
| Neutral genome | "No preference." / "No particular temperament." everywhere |
| Contrast pairs | for each action locus, two genomes that are neutral everywhere except that locus: "pro" ("Always eat, whatever happens.") vs "anti" ("Never eat unless starving.") |
| Single edits | founder genomes with one gene mutated by the mutator LLM (full E1 suite only; skipped without a mutator) |

`teacher_gate` uses a reduced set: 10 founders, 10 random-text genomes, the
neutral genome and the 7 contrast pairs, times n observations (default 24).
`e1_sensitivity` uses the full set: 30 founders, 30 random, 10 parents × 4
edits on the small profile, times all 48 observations.

## 4. Gates G1–G5

Fixed in advance (plan A2) so that results can't be rationalised afterwards.
Changing a gate needs the course owner's decision.

| Gate | Pass criterion | Protects against |
|---|---|---|
| **G1 Semantics** | directed sign accuracy ≥ 85 % **and** mean ΔP ≥ 0.25 | genes ignored |
| **G2 Information** | MI_G ≥ 0.25 bits **and** MI_O ≥ 0.25 bits, **and** gibberish → neutral ≤ ½ × founder → neutral. The quick gate script uses MI_G(founders) ≥ 2 × MI_G(random text). | behaviour driven by any text, not by meaning |
| **G3 Locality** | locality ratio ≤ 0.5 **and** Spearman ρ ≥ 0.3 | chaotic map from text to behaviour |
| **G4 Evolution** | in a common garden, evolved genomes survive longer than founders (bootstrap 95 % CI of the difference > 0) in ≥ 4 of 5 seeds, **and** gain more than the shuffled control | "evolution" that is only drift |
| **G5 Throughput** | ≥ 50 effective decisions/s on a GPU and ≥ 10 on a CPU, at population 40 (cache included) | a lab too slow to run |

What happens on failure (plan A2, adapted to the LLM brain):

- **G1 or G2 fails:** iterate on the prompt or the model, up to three
  documented tries. If it still fails, switch to "LLM as development": one LLM
  call at birth turns the genome into a fixed behaviour profile.
- **G3 fails:** keep only the small-edit mutation instructions, shorten
  genes, reduce loci, re-test.
- **G4 fails while G1–G3 pass:** the world is the problem. Raise selection
  pressure, lengthen runs, check that newcomers aren't swamping selection.
- **G5 fails:** LLM-at-birth for students, the per-decision LLM on a GPU or a
  lab server.

## 5. Results so far

### 5.1 Rule-based reference (E1)

`results/e1_rule_based.md`, 2026-09-28: 114 genomes × 48 observations = 5 472
decisions.

| Metric | Value | Gate |
|---|---|---|
| Directed sign accuracy | 1.00 | G1 ✔ |
| Mean ΔP | 0.484 | G1 ✔ |
| MI_G founders / random text | 0.229 / 0.000 | G2 ✘ (MI_G < 0.25) |
| MI_O founders | 0.829 | |
| Gibberish → neutral / founder → neutral | 0.000 / 0.068 | |
| Locality ratio / Spearman ρ | 0.00 / 0.45 | G3 ✔ |

This brain is the "ideal keyword interpreter", and even it scores MI_G 0.229,
just under the G2 threshold of 0.25. Whether that threshold is realistic is an
open decision. Its locality ratio is 0 because most single-word edits don't
touch its keywords (measured with the word operators that mutation used
before 2026-10-02).

### 5.2 Ollama probe

`results/e0_ollama.md` (26b) and `results/e0_ollama_gemma4-12b.md`,
2026-10-01, RTX 5080 16 GB, Ollama 0.32.0.

| | gemma4:26b | gemma4:12b |
|---|---|---|
| Model digest | 001e5dafc3c7 | 6114515d63c1 |
| JSON-schema output | works | works |
| Same answer 3× at temperature 0 + seed | yes | yes |
| Generation / prompt reading | 26 / 210 tokens/s | 90 / 764 tokens/s |
| Logprobs mode usable | no | no |

Lessons: send `think: false` and a fixed `num_ctx`
([05](05-decision-backends.md#ollama-settings-that-matter)). Logprobs fail
because the first token "f" is ambiguous (*flee/follow*) and the distribution
is saturated.

### 5.3 Decision-model gate: gemma4 12b vs 26b

`results/teacher_gate_gemma4-12b.md` and `…-26b.md`, 2026-10-01, points mode,
12 observations, 420 decisions per model.

| | gemma4:12b | gemma4:26b |
|---|---|---|
| Directed sign accuracy | 0.96 | 0.99 |
| Mean ΔP | 0.75 | 0.62 |
| MI_G founders | 0.250 | 0.244 |
| MI_G random text | 0.297 | 0.314 |
| MI_O founders | 0.865 | 0.735 |
| Gibberish → neutral / founder → neutral | 0.162 / 0.230 | 0.204 / 0.133 |
| Failures | 0 | 0 |
| **G1 / G2** | ✔ / ✘ | ✔ / ✘ |

Per-locus ΔP for 12b: eat 0.53, flee 0.86, follow 0.75, wander 0.56, rest
0.94, mate 0.74, attack 0.89. Sign accuracy is 1.00 everywhere except eat
(0.88) and wander (0.83).

**Reading:** both models follow the genes: change "eat" from *always* to
*never* and the eat probability drops. But random text changes behaviour at
least as much as real founder genes, so G2 fails. Section 6 lists the
suspected causes.

### 5.4 Point totals

60 decisions from the gate sample, gemma4:12b, 2026-10-01: 44 answers (73 %)
sum to exactly 100, 13 to 56–98 and 3 to 0; none above 100. The 3 all-zero
answers all came from random-text genomes (3 of 17)
([05](05-decision-backends.md#points-mode-default)).

### 5.5 Lab 1 world tuning

Flat world, rule-based brain, 5 000 ticks, 3 seeds, 2026-10-01. Food
regrowth 0.0007 keeps the population food-limited and below the cap. Details:
[03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world).

> 03 §13 now gives the numbers for today's world (5 genes, genetic predators,
> distance bands; 2026-10-07). The 10-gene runs of this section gave 25–28
> animals (small) and 47–49 (full); with 5 genes and scripted predators it was
> 25–31 and 50–55.

### 5.6 First run with the LLM brain

Flat world, small profile, 500 ticks, gemma4:12b, 2026-10-01: 1 722
decisions, 761 LLM calls, 425 s, ≈ 4 effective decisions/s, 0 failures. The
population fell to the floor, against 17 animals with the rule-based brain over
the same ticks. Details:
[05 §6](05-decision-backends.md#6-what-the-llm-brain-costs).

### 5.7 Mutation test

`results/mutation_test.md`, 2026-10-02, gemma4:12b, 2 000 calls in 223 s, no
selection. Every founder sentence was mutated with 8 seeds at temperatures
0.9, 1.2, 1.5 and 2.0, and six founder sentences were mutated 30 times in a row
at each temperature.

- **Single mutations:**
  - 99 % valid at every temperature, with 7.0–7.3 different mutants out of 8
    per sentence.
  - About 3 words change per mutation: 46–47 % are one-word edits, 21–22 % are
    big jumps, and genes grow by only 0.07–0.19 words.
- **The instruction sets the step size:** 1–2 words for the small-edit
  instructions, 6–7 words with 41–88 % jumps for the four "big" ones. Going
  from temperature 0.9 to 2.0 adds only 0.3 different mutants per sentence.
- **Keyword brain:** 50–55 % of single mutations change nothing, because it
  reads only its keywords.
- **Lineages:**
  - Genes still using a word of the animal's world: 83 % after 1 mutation,
    58 % after 10, 21 % after 15, 12 % after 30.
  - Genes grow from 4.7 to 6.5–7.8 words.
  - "Toaster" appears in 18 of the 24 lineages.

Examples and discussion: [04 §5](04-genome-and-evolution.md#5-mutation).

### 5.8 Long runs with selection

Full write-up, method and tables: [10](10-natural-selection-runs.md).

`results/long_1234_genes.md`, 2026-10-02:

- **Setup:** full profile (64 × 64, 3 predators, cap 60), rule-based brain, blind LLM mutation (gemma4:12b, `mutate_v2`), 50 000 ticks (≈ 230 generations). Three seeds (1234, 7, 42) ran in parallel in about 14 minutes, with about 2 400 mutation calls each.
- **Deaths:** predators caused 42–43 % of them, starvation the rest.
- **No gene is clearly better than average, even with the three runs pooled.** Genes are grouped by what the keyword brain reads in them. The steady leaders (above average in every seed) are only 1–2 % ahead:
  - cautious;
  - solitary;
  - attached to familiar places;
  - "flee when a predator is very close";
  - "never attack";
  - "rest when food is far".
- **Selection works against harmful genes:**
  - "always attack": fitness 0.77;
  - restless: 0.79, with 60 % of carriers killed by predators against 43 % on average;
  - losing the cautious temperament: 0.89, with 53 % killed by predators.
- **Drift is strong with about 50 animals.** Readings swing from about 25 % to 98 % and back. At the end, 92–100 % of living genes are mutants, 62–79 % still use a word of the animal's world, and only 55–65 different texts remain.
- **The keyword brain can't tell nonsense from sense.** "Never potato." reads like "Never fight.", and "Eat whenever food is enough to hear it screaming." like "Eat whenever food is close.".

### 5.9 One hour with the LLM brain

`results/llm_60min_genes.md`, 2026-10-02.

- **Run:** small Lab 1 world, gemma4:12b deciding and mutating, 60 minutes: 5 269 ticks, 19 211 decisions (5.3 per second), 6 784 LLM calls, no failures.
- **Population:** it sat at the floor of 10 for 3 400 ticks (65 newcomers), then grew to 25–29 animals with no newcomers after tick 3 437.
- **Who rescued it:** four newcomers account for 84 % of the survivors' ancestry. "Never fight." spread from about 30 % of that ancestry to 23 of the 25 survivors.
- **Behaviour:** predators caused 71 % of deaths; animals fled in 4.5 % of decisions and attacked in 9.5 %.
- **Caveat:** one seed only.

Details: [10 §7](10-natural-selection-runs.md#7-one-hour-with-the-llm-brain).

### 5.10 Twelve hours with the LLM brain

`results/llm_long_timeline.md`, 2026-10-05/06.

- **Run:** the hour-long run continued (seed 1234) for 12 hours: 57 061 ticks, 79 667 brain calls, no failures, 812 mutations, peak generation 114.
- **Population:** the rescued line thrived at about 29 animals for 60 generations, then died out at tick 27 337. 874 newcomers in the last 30 000 ticks never rescued it again.
- **Predation trap:** predators kill at a nearly steady pace, so each animal's risk doubles at the floor (1.9 against 3.8 kills per 1 000 animal-ticks).
- **Genes:** mutants reached 75 % of the genes and the share using a world word fell from 97 % to 78 %. Nonsense genes such as "No banana." and "A salad is hiding under a bicycle." took over slots.
- **Selection or drift:** gene dropping shows the sweeps are what random inheritance gives on the same family tree (16 mutants reached 50 %, against 18 expected). "Never fight." was not confirmed after tick 5 269.
- **One harmful gene:** the salad mate gene lowered the brain's mating probability by 7.7 points (`gene_swap`).
- **Caveat:** one seed only.

Details: [11](11-gene-development.md).

### 5.11 First LLM-brain check with 5 genes

`results/runs/check_5genes_llm` (not committed), 2026-10-07.

- **Run:** small Lab 1 world, seed 1234, 500 ticks, gemma4:12b deciding and mutating, the 5-gene genome and `prompts/teacher_v2.md`: 3 016 decisions, 973 LLM calls, no failures, 13 minutes.
- **Population:** 19–26 animals, 25 at tick 500; 31 births, no newcomers. The 10-gene run of §5.6 fell to the floor with 9 newcomers in the same 500 ticks.
- **Behaviour:** eat 28 %, rest 23 %, mate 23 %, follow 22 %, flee 4 % of decisions; 5 % of decisions had nothing in sight and searched instead.
- **Caveat:** one seed and 500 ticks.

### 5.12 First LLM-brain run with genetic predators

`results/runs/check_predators_llm` (500 ticks) and `check_predators_llm_2000`
(the same run to 2 000 ticks), not committed, 2026-10-07.

- **Run:** small Lab 1 world, seed 1234, gemma4:12b deciding for both species
  and mutating, `prompts/teacher_v3.md` and `predator_v1.md`, vision 20 with
  distance bands, the keyword-tuned predator settings
  ([03 §3](03-world-and-simulation.md#3-predators)). 2 000 ticks: 8 131
  decisions (7 252 prey, 879 predator), 4 600 questions to the brain, 0
  failures, about 32 minutes from an empty cache.
- **Prey:** 24 at the start, 16 at tick 500, then at their floor of 10 from
  tick 600 to 1 500, with 35 newcomers. When the founder predators died of old
  age (1 500 ticks), the prey grew to 28 by tick 2 000. 83 births; 90 killed by
  predators, 24 starved.
- **Predators:** 4 founders, 3 births (the first at about tick 650), 4
  newcomers; 6 starved and 2 died of old age; 3 at tick 2 000. They chose
  `mate` in 1 % of decisions in the first 500 ticks, while they were young, and
  11 % over the whole run.
- **Behaviour:** prey eat 38 %, mate 25 %, follow 16 %, rest 12 %, flee 9 %;
  predators hunt 58 %, rest 32 %, mate 11 %. With the keyword brain the prey
  flee about 21 % of the time and predators hunt 73 %
  ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)).
  The same first 500 ticks with the keyword brain: 11 prey and 5 predators at
  tick 500, 29 prey killed, 1 predator birth.
- **Reading:** the machinery works for both species, with no failures. As in
  the 12-hour run with scripted predators
  ([11 §9](11-gene-development.md#9-what-it-means)), the LLM-read prey of the
  small world don't hold against this predation and depend on newcomers. Three
  predator births in 2 000 ticks are too few for their genes to evolve yet.
- **Caveat:** one seed.

### 5.13 LLM brain in the 64 × 64 world, partners seen across the vision

`results/runs/check_predators_llm_full` (not committed), 2026-10-07, after the
owner asked for a bigger world and for partners seen farther away: animals see
whether the nearest other animal of their kind is ready to mate up to 20 cells
(`perception.partner_range`; 4 before), and `smoke_run` runs the full world.

- **Run:** 64 × 64 world, seed 1234, 2 000 ticks, gemma4:12b deciding for both
  species and mutating: 12 142 decisions (11 109 prey, 1 033 predator), about
  5 300 calls, 0 failures, 35 minutes (500 ticks ≈ 11 minutes).
- **Prey:** 30 at the start, down to 12 at tick 1 000 against 5 predators
  (6–7 killed and about 5 born per 100 ticks), at their floor from tick 1 100
  to 1 400 (8 newcomers), then up to 57 at tick 2 000 once only 3 predators were
  left. 146 births; 103 killed by predators, 24 starved.
- **Predators:** 6 founders, **no births** in 2 000 ticks; 4 starved and 2 died
  of old age, 3 newcomers; 3 at tick 2 000. They chose `mate` in 9 % of
  decisions.
- **Why predators don't breed:** asked directly (8 founder genomes, well fed,
  29 calls), the brain gives `mate` 0.60 when a ready partner is next to the
  predator, 0.34 at 2–4 cells and 0.13 at 11–20 cells, where `hunt` gets 0.69.
  Seeing a far partner rarely makes it walk over. Breeding needs two adjacent
  predators that both chose `mate`, and 3–6 predators in 64 × 64 cells seldom
  meet.
- **Behaviour:** prey eat 29 %, mate 25 %, follow 22 %, rest 17 %, flee 7 %;
  predators hunt 64 %, rest 27 %, mate 9 %.
- **Reading:** the bigger world helps the prey (8 newcomers here, 35 in the
  small world of §5.12), but while 5 predators hunt, the LLM-read prey still
  decline: they flee in only 7 % of decisions, against 17 % with the keyword
  brain. Predator genes can't evolve without births.
- **Caveat:** one seed.

### 5.14 LLM brain in the 96 × 96 world, with all four changes

`results/runs/check_all4_llm` (not committed), 2026-10-07. The owner asked for
all four options, for both species: one partner's `mate` is enough to breed,
predators can `follow`, a 96 × 96 world, and a breeding line in both prompts
(`teacher_v4.md`, `predator_v2.md`).

- **Run:** 96 × 96 world, seed 1234, gemma4:12b deciding for both species and
  mutating. It stopped at its 3-hour limit at tick 1 673: 50 072 decisions
  (48 407 prey, 1 665 predator), 26 604 calls, 0 failures, 97 mutations.
- **Predators breed now:** 29 births without any newcomer, up to generation 4;
  35 starved. They stayed at 6–14 (8 at the end) and chose `mate` in 28 % of
  decisions, `follow` in 5 %. Asked directly (8 founder genomes, well fed), the
  brain now gives `mate` 0.85 with a ready partner next to the predator (0.60
  with the old prompt), 0.66 at 2–4 cells (0.34) and 0.20 at 11–20 cells
  (0.13).
- **Prey:** after an early loss (68 → 44 in 100 ticks) they grew to their cap
  of 135 by tick 1 000 and stayed near it: 823 births, 584 starved, 172 killed
  by predators. They flee in 4 % of decisions and mate in 22 %.
- **Reading:** the predators' breeding problem is solved, but the balance
  flipped. The prey are limited by food and by their cap, not by the 6–10
  predators, which catch about one prey per 100 ticks each and often starve.
  The cost grew with the population: about 16 calls per tick, so 5 000 ticks
  would take about 9 hours.
- **Caveat:** one seed, 1 673 ticks.

### 5.15 Stamina, speed and carcasses

2026-10-07, owner request: moving costs stamina and energy; standing still
brings stamina back, which costs energy until it is full; predators are faster
but have less stamina than the prey; a kill leaves part of the carcass for up to
two more predators. The rules are in [03 §4](03-world-and-simulation.md#4-animals)
and [§6](03-world-and-simulation.md#6-actions-five-for-prey-four-for-predators):
stamina 60 cells for prey and 30 for predators, 2 back per tick standing still
(0.3 energy per tick until full); predators run 2 cells per tick when hunting,
prey 1 when fleeing, and every other move walks one cell; a carcass holds one
portion (30 energy) for each of up to two other predators and rots after 100
ticks. Prompts `teacher_v5.md` and `predator_v3.md`.

- **Keyword brain** (5 000 ticks, 3 seeds, no mutation;
  [03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)): in the full world the predators went from 15–21
  to 28–34, at their cap 66–97 % of the time, and rarely starve; the prey stayed
  at 81–94 (74–93 before). Predators ate about 1.5 carcass portions per kill. In
  the small world: prey 28–31 (22–25 before), predators 5.6–5.8, at their cap
  84–90 % of the time.
- **Tuning:** smaller portions (20 energy), with or without `kill_p` 0.07, kept
  the predators below their cap but starving in waves, and the prey sat at their
  floor up to 10 % of the time; `kill_p` 0.07, a digestion of 80 ticks or a
  slower recovery (1.5 per tick) left the predators at their cap. The first
  values were kept.
- **LLM probe** (gemma4:12b, 8 founder genomes per species, energy medium, no
  other animal in sight): mean probabilities.

  | Predator's situation | hunt | follow | rest | mate |
  |---|---|---|---|---|
  | prey 2–4 cells away, rested | 0.82 | 0.01 | 0.04 | 0.13 |
  | prey 2–4 cells away, out of breath | 0.25 | 0.00 | 0.69 | 0.06 |
  | prey 11–20 cells away, rested | 0.62 | 0.01 | 0.19 | 0.18 |
  | prey 11–20 cells away, out of breath | 0.14 | 0.03 | 0.77 | 0.06 |
  | no prey, a carcass 2–4 cells away | 0.44 | 0.00 | 0.19 | 0.37 |
  | no prey, no carcass | 0.09 | 0.09 | 0.43 | 0.38 |

  | Prey's situation (food 2–4 cells away) | eat | flee | follow | rest | mate |
  |---|---|---|---|---|---|
  | predator 2–4 cells away, rested | 0.34 | 0.46 | 0.00 | 0.09 | 0.11 |
  | predator 2–4 cells away, out of breath | 0.13 | 0.68 | 0.01 | 0.18 | 0.01 |
  | no predator, rested | 0.55 | 0.00 | 0.01 | 0.10 | 0.34 |
  | no predator, out of breath | 0.17 | 0.00 | 0.01 | 0.79 | 0.03 |

  The brain reads both new senses. Out of breath, predators and prey stop to
  recover, except prey with a predator close, which flee even more. A carcass
  in sight makes a predator hunt five times as often as with nothing in sight.
- **Cost:** the prompts grew to 1 527 characters (prey) and 1 633 (predators),
  and situations repeat less often (stamina triples the possible observations;
  the keyword runs' memo answered 52–67 % of prey decisions against 61–81 %
  before), so an LLM run needs more calls per tick.

### 5.16 LLM brain with stamina and carcasses

`results/runs/check_stamina_llm` (not committed), 2026-10-07: the same check as
§5.14 (96 × 96 world, seed 1234, gemma4:12b deciding for both species and
mutating), with the rules of §5.15.

- **Run:** all 2 000 ticks in 2 h 37 min: 37 585 decisions (30 068 prey, 7 517
  predator), 23 608 calls, 0 failures, 76 mutations.
- **Predators:** from 14 to their cap of 34 by tick 800, then at or near it (32
  at the end): 131 births without any newcomer, up to generation 7; 110 starved
  and 3 died of old age. 374 kills and 544 carcass portions eaten (1.5 per
  kill). They chose `mate` in 34 % of decisions, `hunt` 33 %, `rest` 21 %,
  `follow` 12 %.
- **Prey:** after the early loss (68 → 43 in 100 ticks) they stayed between 38
  and 83, never at their floor and without newcomers: 552 births, 374 killed,
  201 starved, mean lifespan 190 ticks, up to generation 11. They chose `eat` in
  33 % of decisions, `mate` 24 %, `rest` 20 %, `follow` 13 % and `flee` 10 % (4 %
  in §5.14). Animals were out of breath in 0.3 % of their ticks.
- **Reading:** the balance turned around again. In §5.14 the prey filled their
  cap and starved while 6–14 predators starved too. Now the predators fill
  theirs, and predation rather than food limits the prey (374 killed against
  201 starved). Both species bred without newcomers. As with the keyword brain
  ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)),
  the predators' cap sets their number.
- **Cost:** 11.8 calls per tick, against 16 in §5.14 when 135 prey lived. Fewer
  animals, but situations repeat less: the memo answered 35 % of prey decisions
  and 47 % of predator decisions (47 % and 51 % in §5.14). 5 000 ticks would
  take about 6.5 hours.
- **Caveat:** one seed, 2 000 ticks.

### Summary

| Gate | Status (2026-10-01) |
|---|---|
| G1 Semantics | ✔ for gemma4 12b and 26b (small gate) |
| G2 Information | ✘, open problem |
| G3 Locality | not measured on the LLM brain yet (needs the full E1 suite) |
| G4 Evolution | not measured (evolution matrix not written yet) |
| G5 Throughput | ≈ 4 decisions/s on a 16 GB GPU, far below 50 → fails at the current settings |

## 6. Known issues and open questions

1. **G2: random text moves behaviour.** Suspected causes, cheapest first:
   - **All-zero answers become uniform.** The model gives 0 points to
     everything for some nonsense genomes, and normalisation turns that into a
     random animal. Proposed fix: use the neutral genome's answer instead.
     Needs the owner's approval because it changes what is measured.
   - **The controls are not neutral.** Shuffled and irrelevant sentences
     contain words that mean something in this world (*mountains, river,
     bread, flour*). Proposed fix: rewrite the control set without world
     words.
   - **The prompt.** Tell the model more explicitly to ignore genes unrelated
     to behaviour. The plan allows three documented prompt iterations.
   - **The threshold.** The ideal keyword interpreter scores MI_G 0.229 < 0.25.
     Whether G2's MI_G threshold stays is an owner decision.
2. **Throughput (G5).** ≈ 4 decisions/s against 50 targeted. Levers: workers
   and `OLLAMA_NUM_PARALLEL`, a longer decision period, smaller populations,
   the persistent cache across runs, table mode for request-limited setups, a
   lab server, or LLM-at-birth.
3. **LLM-read founders are less viable.** In the first LLM run the animals
   attacked 16 % of the time and the population fell to the floor. That could
   be a world-tuning problem (tuned with the rule-based brain), a founder-pool
   problem, or exactly what evolution should fix. More seeds are needed
   before concluding.
4. **Blind mutation leaves the animal's world.** Without selection, 79 % of
   genes use no word of the world after 15 mutations, and the model favours a
   few words ("toaster" in 18 of 24 lineages, §5.7). A gene meets about 1.4
   mutations in a 10 000-tick Lab 1 run, so this matters for long runs, where
   only selection can keep genes meaningful. With the keyword brain most
   nonsense is neutral and can spread by drift.
5. **Founder pool v1 is a draft** awaiting the owner's review (H1).
6. **Changed on 2026-10-02:** mutation is one blind LLM operator (the word
   operators and rewrite styles are gone, [04 §5](04-genome-and-evolution.md#5-mutation)),
   and the mutator is gemma4:12b like the brain, so there are no more model
   swaps. Every run with mutation needs the model.
7. **Fixed on 2026-10-01** (no longer issues): the request cache
   (`cache/ollama.sqlite`) was never written (an empty cache file counted as
   "no cache"); `smoke_run` used LLM mutation with every brain;
   `experiments.status` reported live jobs as `DEAD?` on Windows (signal 0 is
   Ctrl+C there, not a liveness probe); the ASCII map used one letter for two
   meanings.

## 7. Next experiments

In the order of the plan (`prototype/STATUS.md` holds the live position):

1. **Owner decisions** on the G2 fixes and the founder pool, then rerun
   the gate. Cached answers make reruns cheap.
2. **Full E1 suite on the LLM brain:** `e1_sensitivity --backend llm`, about
   5 500 decisions, ≈ 45–60 min. Gives G3 (locality) and a larger G1/G2
   sample.
3. **More LLM runs on the Lab 1 world** (several seeds, longer): population
   dynamics under the LLM brain, and world tuning if needed.
4. **Evolution matrix E4 and common garden E5** (scripts not written yet):
   conditions C1 FULL, C2 NO-MUT, C3 SHUFFLED, C4 RANDOM-FOUNDERS, C5
   RULE-BASED; then allele-frequency and lineage analysis, G4, and the go/no-go
   report. At ≈ 4 decisions/s the budget needs a decision (run length,
   decision period, seeds) before launching.

## 8. Reproducing the results

From `prototype/`. Since 2026-10-07 these commands run the 5-gene genome,
genetic predators and the distance bands, so their numbers differ from the
ones above. The E1 commands read `data/observations_v2.jsonl`; the results of
§5.1–5.3 used `observations_v1.jsonl`, which today's brains can't read.

```bash
python -m experiments.e1_sensitivity --backend rule_based                       # 5.1
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b # 5.2
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b # 5.3
python -m experiments.smoke_run --profile small --ticks 5000 --snapshots 0 --seed 7       # 5.5, one seed
python -m experiments.smoke_run --profile small --backend llm --ticks 500 --snapshots 1   # 5.6
python -m experiments.mutation_test                                             # 5.7 (≈ 4 min, cached afterwards)
python -m experiments.smoke_run --profile full --ticks 50000 --seed 1234 --snapshots 5 --out results/runs/long_1234   # 5.8 (also seeds 7, 42)
python -m experiments.gene_report results/runs/long_1234 results/runs/long_7 results/runs/long_42 --tag long_1234
python -m experiments.smoke_run --profile small --backend llm --ticks 2000 --seed 1234 --out results/runs/check_predators_llm_2000   # 5.12 (code of 865c2c5)
python -m experiments.smoke_run --backend llm --ticks 2000 --seed 1234 --out results/runs/check_predators_llm_full                  # 5.13 (code of 08e0c57)
python -m experiments.smoke_run --backend llm --ticks 2000 --minutes 180 --seed 1234 --out results/runs/check_all4_llm              # 5.14 (code of 9aaac2d)
python -m experiments.smoke_run --ticks 5000 --no-mutation --snapshots 0 --seed 1234 --out results/runs/stamina_1234                # 5.15, keyword brain (also seeds 7, 42; --profile small)
python -m experiments.smoke_run --backend llm --ticks 2000 --minutes 180 --seed 1234 --out results/runs/check_stamina_llm           # 5.16
```

§5.12–5.14 ran the code of the commits in brackets (smaller worlds, older
rules, no stamina): check them out to rerun those exactly.

The gate writes `results/teacher_gate.md`. Rename it per model to keep both,
as was done for the committed files.
