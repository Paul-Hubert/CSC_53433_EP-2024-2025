# 11 — How genes develop in one long run

What happens to the genes, the animals and their behaviour over 12 hours with
the LLM brain, about 110 generations. Code: `prototype/experiments/smoke_run.py`
(the run), `gene_timeline.py` (the analysis), `gene_swap.py` (a direct test of
one gene) and `gene_report.py`. Results: `prototype/results/llm_long_timeline.md`
(tables), `llm_long_timeline.html` (charts), `llm_long_genes.md`
(2026-10-05/06). The prompt that ran it: `Docs/prompts/04-long-run-gene-development.md`.

> **Genome and world changes (2026-10-07).** This run used the 10-gene genome
> of the time: 7 action genes, including wander and attack, plus 3 temperament
> genes (risk, social, place). The current genome has 5 genes, one per action ([04 §1](04-genome-and-evolution.md#1-genes-are-sentences-in-fixed-slots));
> `gene_swap` (§7, §10) now needs a 5-gene run. The predators were scripted and
> animals saw 12 cells; since 2026-10-07 predators are genetic animals and both
> species see 20 cells ([03 §3](03-world-and-simulation.md#3-predators)).
> `gene_timeline --species predator` follows the predators' genes.

## Contents

1. [The run](#1-the-run)
2. [Five phases](#2-five-phases)
3. [The gene pool over time](#3-the-gene-pool-over-time)
4. [How genes changed: lineages](#4-how-genes-changed-lineages)
5. [What mutation offers, and what survives](#5-what-mutation-offers-and-what-survives)
6. [Selection or drift?](#6-selection-or-drift)
7. [Why the population collapsed](#7-why-the-population-collapsed)
8. [Do genes stay meaningful?](#8-do-genes-stay-meaningful)
9. [What it means](#9-what-it-means)
10. [Reproduce it, and use it in Lab 1](#10-reproduce-it-and-use-it-in-lab-1)

---

## 1. The run

| | |
|---|---|
| World | the Lab 1 flat world, small profile: 48 × 48 cells, 2 predators, 10 to 40 animals |
| Brain and mutation | gemma4:12b for both (points mode; blind mutation, 16 instructions, temperature 1.2, `p_mut` 0.03) |
| Seed | 1234: it continues the hour-long run of [10 §7](10-natural-selection-runs.md#7-one-hour-with-the-llm-brain). Its first 5 269 ticks replayed from the cache in 1.5 s, with no model call and the same events hash |
| Length | `smoke_run --minutes 720`: 57 061 ticks; the oldest line reached generation 114 |
| Cost | 237 709 decisions (5.5 per second), 79 667 brain calls, no failures; 812 mutations (826 tried, 693 new mutation calls) |
| Recorded | Ollama 0.35.0, gemma4:12b digest `6114515d63c1`, code `d725645` (`results/runs/llm_long/run_info.json`) |

Before the run, `smoke_run` was made safe for long runs: a stop file, clean
stops on errors, resume by replay, files saved as it goes
([03 §12](03-world-and-simulation.md#12-what-a-run-writes-to-disk)).

## 2. Five phases

Rates per 1 000 animal-ticks; decisions as shares of all decisions in the phase.

| Phase | Ticks | Animals (mean) | Births | Predator kills | Starved | Newcomers | eat / wander / mate / attack / flee |
|---|---|---|---|---|---|---|---|
| Floor (the hour-long run) | 0–3 400 | 10–18 (11) | 2.6 | 3.3 | 1.3 | 64 | 32 / 17 / 15 / 11 / 5 % |
| Growth | 3 400–6 000 | 11–33 (23) | 3.4 | 2.1 | 1.0 | 1 | 26 / 27 / 19 / 5 / 4 % |
| Thriving | 6 000–20 000 | 20–40 (29) | 3.5 | 1.9 | 1.6 | 0 | 34 / 25 / 21 / 2 / 5 % |
| Decline | 20 000–27 300 | 31 → 10 (16) | 3.0 | 2.7 | 1.0 | 76 | 41 / 14 / 19 / 8 / 8 % |
| Floor again | 27 300–57 061 | 10–18 (11) | 2.1 | 3.8 | 1.0 | 874 | 34 / 18 / 15 / 13 / 5 % |

- **The rescued population lived for about 24 000 ticks.** It was founded by
  four newcomers around tick 3 000 (10 §7). It stayed at 29 animals on average
  for 14 000 ticks, about 60 generations, with births and deaths in balance.
- **Then it died out.** The last animal of that line, generation 113, was
  killed by a predator at tick 27 337.
- **No second rescue.** For the last 30 000 ticks, 874 newcomers kept the
  population at its floor of 10. None of their combinations took off, so the
  first rescue was luck.

## 3. The gene pool over time

Means over the 10 slots. *Mutant genes*: share of the living animals' genes that
are mutants. *Depth*: mutations since the founder text. *World*: share using a
word of the animal's world. *Effective*: 1 / Σ share², how many equally common
genes a slot amounts to. *Overlap*: words shared by two animals with different
texts in a slot.

| Tick | Animals | Generation | Mutant genes | Depth | World | Effective | Overlap |
|---|---|---|---|---|---|---|---|
| 0 | 24 | 0 | 0 % | 0.00 | 100 % | 4.2 | 0.02 |
| 5 000 | 29 | 17 | 12 % | 0.13 | 97 % | 2.4 | 0.06 |
| 10 000 | 33 | 39 | 37 % | 0.52 | 87 % | 2.0 | 0.25 |
| 15 000 | 25 | 64 | 57 % | 0.86 | 83 % | 1.8 | 0.44 |
| 20 000 | 23 | 86 | 73 % | 1.15 | 78 % | 1.7 | 0.48 |
| 24 000 | 15 | 103 | 75 % | 1.17 | 83 % | 1.5 | 0.32 |
| 27 500 | 10 | 0 | 2 % | 0.02 | 100 % | 3.9 | 0.03 |
| 45 000 | 12 | 5 | 3 % | 0.03 | 100 % | 2.5 | 0.03 |
| 57 061 | 10 | 0 | 0 % | 0.00 | 100 % | 3.6 | 0.01 |

- **Mutants took over.** By tick 20 000, three quarters of the genes were
  mutants. Most slots were near-fixed (1.5–1.7 effective genes), and the
  variants left were small edits of one another (overlap 0.48).
- **Meaning faded.** The share of genes using a word of the animal's world fell
  from 97 % to 78 %.
- **Everything reset with the newcomers.** After the collapse, the genes were
  fresh founder texts again.

The charts in `results/llm_long_timeline.html` show each slot's genes as
stacked bands over time.

## 4. How genes changed: lineages

The genes that took over a slot in the thriving phase, from founder text to
mutant, with the instruction that made each step:

```text
rest    No preference.                              neutral
        No choice.                                  "Randomly change one word in this sentence."
        No banana.                                  "Replace one word in this sentence with a random word."   100 % at t 13 500
        The moon is allergic to geometry.           "Randomly change the meaning of this sentence a lot."     77 % at t 17 000
        Geometry makes the moon sneeze.             "Change this sentence in a random way, big or small."     62 % at t 20 500

mate    Look for a partner when energy is high.     founder                                                   100 % at t 10 500
        Look partner for a when energy is high.     "Randomly swap two words in this sentence."               78 % at t 21 000
        Look partner for a microwave's screaming.   "Make an unexpected change to this sentence."
        A salad is hiding under a bicycle.          "Randomly change the meaning of this sentence a lot."     91 % at t 24 500

eat     Only look for food when energy is low.      founder                                                   96 % at t 6 500
        Only look for water when energy is low.     "Change one random detail in this sentence."              100 % at t 12 500

wander  Stay near where you last found food.        founder                                                   100 % at t 12 000
        Stay near where you last found bicycle.     "Replace one word in this sentence with a random word."   100 % at t 16 000

follow  Follow the strongest animal nearby.         founder
        Follow the smallest vehicle nearby.         "Randomly change a few words in this sentence."           65 % at t 9 000
        Follow the largest vehicle nearby.          "Randomly change a few words in this sentence."           100 % at t 20 500

place   No particular temperament.                  neutral                                                   100 % at t 10 500
        No particular temperature.                  "Randomly change the meaning of this sentence a little."  100 % at t 23 500
```

- "Never fight." stayed at 93–100 % of the attack slot through the thriving
  phase.
- In all, 91 genes reached 25 % of their slot at some point, 64 reached 50 %,
  and 16 of those 64 were mutants.

## 5. What mutation offers, and what survives

| Mutants | n | Depth | Small-edit instructions | Big-change instructions | Words changed | Jumps | Words | World words |
|---|---|---|---|---|---|---|---|---|
| All produced | 648 | 1.5 | 42 % | 32 % | 3.0 | 24 % | 5.5 | 69 % |
| 5 or more living carriers at once | 79 | 1.5 | 38 % | 32 % | 3.0 | 24 % | 5.4 | 71 % |
| Alive when mutants were most common (t 24 000) | 16 | 1.7 | 44 % | 19 % | 2.2 | 12 % | 5.4 | 75 % |
| Mutation test, no selection ([04 §5](04-genome-and-evolution.md#5-mutation)) | 317 | 1.0 | 50 % | 26 % | 3.0 | 21 % | 6.2 | 87 % |
| *Keyword brain, `long_1234`:* all produced | 1 658 | 2.5 | 41 % | 33 % | 3.4 | 21 % | 6.4 | 78 % |
| *Keyword brain:* alive at t 50 000 | 60 | 3.1 | 62 % | 22 % | 2.3 | 18 % | 5.9 | 75 % |

*Small-edit* and *big-change* instructions change at most 2, or at least 4,
words on average in the mutation test. *Jumps*: similarity to the parent below
0.3.

- **The LLM brain barely filters mutants.** The mutants that spread look like
  the ones produced: the same edit sizes and the same share of world words.
- **The keyword brain keeps small edits.** Under it, 62 % of the mutants alive
  late in the run came from small-edit instructions, against 41 % of those
  produced. Big changes destroy the keywords it reads.

## 6. Selection or drift?

`gene_timeline` uses **gene dropping**. Genes are handed down the real family
tree at random, 500 times: each child takes each slot from a random parent, and
the real mutations are kept. This separates two kinds of luck: which families
do well (kept as it happened) and which genes a child inherits (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of
their slot in reality, and in the random-inheritance worlds (median and 90 %
range). *P*: share of worlds with at least as many.

| Genes | This run: real | Inheritance alone | P | Keyword run `long_1234`: real | Inheritance alone | P |
|---|---|---|---|---|---|---|
| Mutants that reached 50 % | 16 | 18 (13–24) | 0.80 | 20 | 24 (19–30) | 0.92 |
| Mutants that reached 90 % | 8 | 6 (3–10) | 0.30 | 7 | 4 (1–7) | 0.06 |
| Founder texts that reached 90 % | 12 | 9 (6–12) | 0.05 | 2 | 2 (0–5) | 0.72 |

- **The sweeps are what drift gives.** Mutants, nonsense included, took over
  slots about as often as random inheritance does on the same family tree.
  This holds under both brains.
- **Per-gene P-values mislead.** A gene picked *because* it swept has a small
  P-value even under pure chance: it won among many genes. "No particular
  temperature." has P = 0.002 at its peak, for example. The counts above avoid
  that bias.

**"Never fight." tested on what came after.** [10 §7](10-natural-selection-runs.md#7-one-hour-with-the-llm-brain)
singled out "Never fight." at tick 5 269 (P = 0.024). Here genes are handed down
at random only after that tick:

| Tick | 5 500 | 11 500 | 17 000 | 23 000 | 28 500 |
|---|---|---|---|---|---|
| Real share | 90 % | 82 % | 88 % | 84 % | 20 % |
| Expected from inheritance | 88 % | 79 % | 78 % | 63 % | 15 % |
| P(≥) | 0.52 | 0.75 | 0.62 | 0.45 | 0.50 |

After tick 5 269, "Never fight." did no better than inheritance predicts. But
92 % of animals already carried it, so there was little left to select. This
doesn't confirm an advantage, and doesn't rule one out. The hour-long run's
"looks selected" stays a lead.

## 7. Why the population collapsed

**A predation trap.** The two predators kill at a nearly steady pace, so each
animal's risk grows as the population shrinks:

| | Animals (mean) | Predator kills per 2 000 ticks | per 1 000 animal-ticks |
|---|---|---|---|
| Thriving | 29 | 100–113 | 1.9 |
| Floor | 11 | 75–93 | 3.8 |

Births reach at most about 3.8 per 1 000 animal-ticks. Below about 20
animals, predators and starvation outpace them, and the population sinks. This
is an Allee effect: small populations do worse per animal. The floor of 10,
topped up by newcomers, hides extinctions: 1 018 newcomers arrived in this run.

**What pushed it over.** The population swung between 23 and 34 animals
until tick 20 000.

- **A dip.** In 500 ticks it fell from 23 to 14 animals.
- **The salad gene spread.** "A salad is hiding under a bicycle." appeared in
  the mate slot at tick 20 040. In the small population it drifted to 65 % by
  tick 22 000, and to 91 % at tick 24 500.
- **Behaviour changed.** Mate decisions fell from 21 % to 19 % and then 16 %.
  Attacks rose from 2 % to 8 % of decisions.
- **Births fell.** They went from 3.6 to 3.3 and then 2.3 per 1 000
  animal-ticks.

**A direct test.** Is the salad gene harmful, or just a passenger? `gene_swap`
took 6 of the 9 genomes alive at tick 23 000 that carried the salad gene. It put
each of three texts in their mate slot and asked the brain about the 34 test
situations with another animal near (511 new calls):

| Mate gene | P(mate) | P(eat) | P(attack) |
|---|---|---|---|
| "A salad is hiding under a bicycle." (carried) | 13.1 % | 32.9 % | 4.4 % |
| "Look partner for a when energy is high." (the gene it replaced) | 20.8 % | 24.2 % | 1.6 % |
| "No preference." | 17.2 % | 32.2 % | 1.2 % |

- **The salad gene cut mating.** Mating was 7.7 points lower than with the gene
  it replaced (95 % interval 2.3 to 13.1 points) and 4.1 points lower than
  with "No preference." (−1.1 to 9.2, not significant).
- **It raised attacking.** The brain's prompt says meaningless instincts have
  no effect, but this one wasn't neutral.
- **The likely story** is that a harmful mutant drifted to high frequency in a
  small population, and the predation trap did the rest. With one run, it
  remains a likely story, not a proof.

## 8. Do genes stay meaningful?

| Tick | 0 | 5 000 | 10 500 | 15 500 | 21 000 | 26 000 | 31 500 | 57 061 |
|---|---|---|---|---|---|---|---|---|
| World words | 100 % | 97 % | 87 % | 83 % | 80 % | 88 % | 100 % | 100 % |
| Judged usable | 91 % | 82 % | 62 % | 56 % | 54 % | 72 % | 92 % | 87 % |

*Judged usable*: gemma4:12b, asked with a fixed prompt
(`prompts/judge_sense_v1.txt`, temperature 0) whether a gene still gives the
animal a usable rule for its slot. It judged the 216 genes that had at least 3
carriers at once (216 calls); the share is weighted by carriers.

- **The judge only partly agrees with a human.** I checked 30 of its answers by
  hand:
  - it agreed on all 15 clear cases: nonsense such as "The moon is allergic to
    geometry." was not usable, plain rules were;
  - it was inconsistent on 10 texts that state no rule, such as "No
    preference." (usable) and "Whatever works." (not usable), calling 4 usable
    and 6 not;
  - it was lenient on 5 rules about odd objects, such as "Stay near where you
    last found bicycle.".

  Read it as a rough measure.
- **Selection did not keep genes meaningful.** Over about 100 generations with
  the LLM brain, both measures fell steadily while the population thrived, and
  nonsense genes swept as often as chance predicts (§6). The keyword-brain runs
  gave the same answer ([10 §4](10-natural-selection-runs.md#4-what-it-means)).

## 9. What it means

- **Most nonsense is close to neutral for the LLM brain, so it drifts.** The
  brain's prompt tells it to ignore meaningless instincts. With about 30 animals,
  drift then fixes whatever mutation produces, and meaning decays at about the
  pace blind mutation sets.
- **Some nonsense isn't neutral, and drift can fix harmful genes in small
  populations.** The salad gene cut mating and raised attacking, and it spread
  anyway. This is how small populations accumulate harmful genes.
- **The Lab 1 small world is barely viable for the LLM-read founders.**
  - The population survives only above about 20 animals, because predation per
    animal grows as numbers fall.
  - One lucky rescue in 57 000 ticks, then extinction, shows how fragile it is.
  - To make LLM-brain runs about evolution rather than survival, an owner could
    choose one of these levers:
    - a larger world or cap;
    - fewer predators, a lower `kill_p` or a longer rest after a kill;
    - a gentler mutation (lower `p_mut`, or prompts that keep to the gene's
      world, [mutation review](../../prototype/notes/mutation-review.md)).
- **Gene dropping belongs in the analysis.** Without it, the hour-long run read
  as "Never fight. looks selected" and a mid-run look flagged "Follow others
  when you are lost or hungry." (P = 0.004). Neither survived a fair test.
- **One seed.** All of this is one run. The phases, the collapse and the drift
  counts need other seeds before they become results.

## 10. Reproduce it, and use it in Lab 1

From `prototype/`, with Ollama running. The run takes 12 hours; the analysis
takes seconds, plus about 2 minutes for the judge and 5 for the swap test.

```bash
nohup python -m experiments.smoke_run --backend llm --profile small --seed 1234 --minutes 720 \
      --ticks 200000 --snapshots 40 --out results/runs/llm_long > logs/llm_long.log 2>&1 &
# stop early: touch logs/run_llm_long.stop · resume: the same command (the done part replays)
python -m experiments.gene_timeline results/runs/llm_long --every 500 --window 2000 --tag llm_long \
       --test "Never fight." --test-from 5269 --judge
python -m experiments.gene_report results/runs/llm_long --min-carriers 25 --every 2000 --tag llm_long
python -m experiments.gene_swap results/runs/llm_long --tick 23000 --slot mate \
       --carrying "A salad is hiding under a bicycle." \
       --texts "Look partner for a when energy is high." "No preference." --genomes 6
```

Every answer is cached, so the same commands give the same numbers. Running the
first command again replays the run from the cache, at about 3 000 ticks per
second.

Questions for students (Lab 1 activity D, [02](02-lab1.md#d-watch-evolution)):

- Run `gene_timeline` on your own run. Which genes swept, and does the count of
  sweeps beat inheritance alone?
- Pick a gene that swept and test it with `gene_swap`: does it change
  behaviour, or is it a passenger?
- Find the population size below which your world can't sustain itself:
  births against predator kills per 1 000 animal-ticks, phase by phase.
- Change one lever (predators, world size, `p_mut`) and see whether genes keep
  their meaning longer.
