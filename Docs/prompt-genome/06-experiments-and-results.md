# 06 — Experiments, metrics and results

The project's central claim, that *evolution can improve behaviour by rewriting
English sentences*, only holds if the brain really reads the genes. This page
explains how that is measured, which pass/fail **gates** were fixed in advance,
and every result so far, dated. The full plan with sessions and budgets is
`Docs/redesign/08-phase0-spike-plan.md`.

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
  genes longer, blander and alike.

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
| **Locality ratio** | median d(parent, child with one word edit) ÷ median d(two unrelated founders) | are small edits small changes? |
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
| Observation set `data/observations_v1.jsonl` | 48 situations: 32 adult situations drawn from the 54 combinations of energy (low/medium/high) × food (none/far/near) × predator (none/near) × other animal (none / near, ready and weaker / near, not ready and stronger), plus the 16 most frequent other situations of a rule-based run (8 adult, 8 young) |
| Founder genomes | random draws from the founder pool |
| Random-text genomes | every gene drawn from `control_alleles_v1.json`: 20 shuffled-word sentences ("Needs bakery green bread records nine attic.") and 20 irrelevant ones ("Trains leave from the north platform.") |
| Neutral genome | "No preference." / "No particular temperament." everywhere |
| Contrast pairs | for each action locus, two genomes that are neutral everywhere except that locus: "pro" ("Always eat, whatever happens.") vs "anti" ("Never eat unless starving.") |
| Single edits | founder genomes with one word-operator edit (full E1 suite only) |

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
- **G3 fails:** restrict mutation to word operators, shorten genes, reduce
  loci, re-test.
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
touch its keywords.

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

### 5.6 First run with the LLM brain

Flat world, small profile, 500 ticks, gemma4:12b, 2026-10-01: 1 722
decisions, 761 LLM calls, 425 s, ≈ 4 effective decisions/s, 0 failures. The
population fell to the floor, against 17 animals with the rule-based brain over
the same ticks. Details:
[05 §6](05-decision-backends.md#6-what-the-llm-brain-costs).

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
4. **Mutator model swap.** A 26b mutator next to a 12b brain forces model
   reloads in 16 GB. Proposed: mutator = 12b (owner decision).
5. **Word operators are blunt.** "Rest when you are tired when tired.",
   "Stay put to new places." ([04 §5](04-genome-and-evolution.md#5-mutation)).
6. **Founder pool v1 is a draft** awaiting the owner's review (H1).
7. **Fixed on 2026-10-01** (no longer issues): the request cache
   (`cache/ollama.sqlite`) was never written (an empty cache file counted as
   "no cache"); `smoke_run` used LLM mutation with every brain;
   `experiments.status` reported live jobs as `DEAD?` on Windows (signal 0 is
   Ctrl+C there, not a liveness probe); the ASCII map used one letter for two
   meanings.

## 7. Next experiments

In the order of the plan (`prototype/STATUS.md` holds the live position):

1. **Owner decisions** on the G2 fixes, the mutator model and the founder
   pool, then rerun the gate. Cached answers make reruns cheap.
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

From `prototype/`:

```bash
python -m experiments.e1_sensitivity --backend rule_based                       # 5.1
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b # 5.2
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b # 5.3
python -m experiments.smoke_run --ticks 5000 --snapshots 0 --seed 7             # 5.5, one seed
python -m experiments.smoke_run --backend llm --ticks 500 --snapshots 1         # 5.6
```

The gate writes `results/teacher_gate.md`. Rename it per model to keep both,
as was done for the committed files.
