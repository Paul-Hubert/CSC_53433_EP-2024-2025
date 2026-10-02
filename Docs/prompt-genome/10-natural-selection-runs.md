# 10 — Natural selection in long runs

What happens to the genes when the Lab 1 world runs for hundreds of
generations, with predators and food as the only judges, and how to tell which
genes did best. Code: `prototype/experiments/smoke_run.py` (the runs) and
`prototype/experiments/gene_report.py` (the analysis). Full results:
`prototype/results/long_1234_genes.md` (2026-10-02).

## Contents

1. [The experiment](#1-the-experiment)
2. [Measuring which genes did best](#2-measuring-which-genes-did-best)
3. [Results](#3-results)
4. [What it means](#4-what-it-means)
5. [Making selection easier to see](#5-making-selection-easier-to-see)
6. [Reproduce it, and use it in Lab 1](#6-reproduce-it-and-use-it-in-lab-1)
7. [One hour with the LLM brain](#7-one-hour-with-the-llm-brain)

---

## 1. The experiment

**Question:** after a long time with no fitness function, only predators and
food deciding who lives and breeds, which genes do best?

| | |
|---|---|
| World | the Lab 1 flat world, full profile: 64 × 64 cells, 3 predators, food regrowth 0.0007, at most 60 animals ([03](03-world-and-simulation.md)) |
| Brain | the rule-based (keyword) brain, the only one fast enough for hundreds of generations; the LLM brain needs about an hour per 5 000 ticks ([05](05-decision-backends.md)) |
| Mutation | blind LLM mutation: gemma4:12b, the 16 instructions of `prompts/mutate_v2.txt`, temperature 1.2, `p_mut` 0.03 ([04 §5](04-genome-and-evolution.md#5-mutation)) |
| Length | 50 000 ticks, about 230 generations |
| Seeds | 1234 (main), 7 and 42, run in parallel: about 14 minutes, about 2 400 mutation calls each |

A run is reproducible: the same seed gives the same run, because every mutation
answer is cached with its seed.

## 2. Measuring which genes did best

`python -m experiments.gene_report RUN [RUN …]` reads the run files
(`events.jsonl`, `alleles.jsonl`, `stats.csv`, `final_population.json`) and
ranks the genes.

### Fitness

- **An animal's success** is its number of offspring over a complete life. Only
  animals that died during the run count. Each birth counts once for each
  parent.
- **Relative offspring** = that number ÷ the mean of the animals that died in
  the same 5 000 ticks. While the population grows (from 30 towards 50
  animals), everyone has more children; comparing each animal with its
  contemporaries removes that.
- **Fitness of a gene** = the mean relative offspring of the animals that
  carried it. 1.00 is average. The 95 % interval is the mean ± 1.96 standard
  errors.

### Grouping genes by what they do

After about 230 generations, 92–100 % of the living genes are mutants, spread
over about 1 700 different texts, so most texts have too few carriers to judge.
Texts that the keyword brain reads the same way behave the same, so the report
groups them by **reading**, using the brain's own rules
([05](05-decision-backends.md)):

- **Action gene:** a strength and conditions. Strength: *never* −2.5, *rarely*
  −1.2, *sometimes* +0.3, *often* or *whenever* +1.2, *always* +2.5; a gene
  that only names its action counts +0.8. Conditions come from words such as
  *hungry*, *predator*, *very close*, *food is far*, and *unless* reverses
  them. "Flee only when a predator is very close." reads as *flee +0.8, when
  predator & very close*. "Never fight." and "Never potato." both read as
  *attack −2.5 (never)*.
- **Temperament gene:** the temperament words it contains: *cautious*,
  *bold*, *social*, *solitary*, *restless*, *familiar*. "Nervous: any movement
  nearby means danger." reads as *cautious*.

### Marks

Marks use all the runs given (different seeds) together:

| Mark | Meaning |
|---|---|
| **★** | best reading of its slot, and clearly above average (interval above 1.00) |
| **▲** | steady leader: the best of the readings that are above average in every run where they appear (at least two); a lead this small can still be chance |
| **✗** | clearly below average (interval below 1.00) |

The report also gives:
- for each reading, the share of its carriers killed by predators;
- its share of the living animals at the start, at tick 20 000 and at the end;
- the number of different texts and the most carried ones;
- predator kills per 1 000 animal-ticks for each 10 000 ticks;
- the same ranking for exact texts;
- the lineage of the most common genes at the end, with the instruction that made each step.

## 3. Results

### 3.1 The three runs

| Seed | Generations | Births | Deaths (by predators) | Mutations accepted / tried | Animals at the end | Their genes: mutants · use a word of the animal's world · different texts |
|---|---|---|---|---|---|---|
| 1234 | 236 | 8 116 | 8 096 (43 %) | 2 369 / 2 416 | 50 | 92 % · 79 % · 62 |
| 7 | 238 | 8 334 | 8 312 (43 %) | 2 489 / 2 552 | 52 | 100 % · 62 % · 55 |
| 42 | 225 | 7 986 | 7 968 (43 %) | 2 328 / 2 397 | 48 | 97 % · 68 % · 65 |

Starvation caused the other 57 % of deaths. Fewer than 0.3 % of animals died of
old age.

### 3.2 Marked genes (the three runs together)

No reading is clearly above average, so none gets ★.

| | Slot | Reading | Fitness | Carriers | Killed by predators | Most carried texts |
|---|---|---|---|---|---|---|
| ▲ | flee | flee +0.8, when predator & very close | 1.02 [0.99–1.05] | 7 874 | 40 % | "Flee only when a predator is very close." |
| ▲ | rest | rest +0.8, when food is far | 1.02 [0.99–1.06] | 5 836 | 45 % | "Save energy by resting when food is far." |
| ▲ | risk | cautious | 1.01 [0.99–1.02] | 22 683 | 42 % | "Nervous: any movement nearby means danger.", "Cautious: safety comes before food." |
| ▲ | social | solitary | 1.01 [0.99–1.03] | 15 421 | 43 % | "Alone is preferred by the solitary one." |
| ▲ | place | familiar | 1.01 [0.99–1.03] | 20 568 | 42 % | "Attached to familiar places.", "Prefers staying near water." |
| ▲ | attack | attack −2.5 (never) | 1.01 [0.98–1.03] | 12 022 | 42 % | "Never fight.", "Never potato.", "Never dance." |
| ▲ | eat | eat +1.2 (often) | 1.01 [0.98–1.03] | 9 333 | 45 % | "Eat quickly, then move on.", "Eat whenever food is enough to hear it screaming." |
| ▲ | mate | mate +0.8, when (food is) plentiful | 1.01 [0.96–1.06] | 3 197 | 45 % | "Mate only when food is plentiful." |
| ✗ | attack | attack +2.5 (always) | 0.77 [0.58–0.96] | 124 | 40 % | "Always dance." |
| ✗ | place | restless | 0.79 [0.61–0.97] | 182 | **60 %** | "Restless: always wants somewhere new." |
| ✗ | risk | no effect | 0.89 [0.81–0.98] | 816 | **53 %** | "No particular temperament." |
| ✗ | place | no effect | 0.96 [0.91–1.00] | 3 625 | 45 % | "Odder things are joined." |

**follow** and **wander** have no steady leader. **Exact texts:** none is
clearly above average. The top of that list mixes founder sentences with
nonsense such as "Calculate the recipe for a holographic blender." (1.08, 164
carriers), and its intervals are wide.

### 3.3 Selection by predators

- **Careless animals get eaten.**
  - Restless carriers: 60 % killed by predators.
  - Carriers that lost the risk temperament: 53 %.
  - Average: 43 %.

  Both genes are clearly below average fitness. Cautious and home-loving
  (*familiar*) carriers: 41–42 %. Carriers of the flee leader: 40 %.
- **The population as a whole didn't clearly learn to escape.**
  - In the first 2 000 ticks of seed 1234, predators caused 58 % of deaths and
    animals fled in 8 % of decisions. Over the whole run: 43 % and 12.6 %.
  - But the population also grew from about 39 to 53 animals, so more animals
    starved, and each predator had more prey to share out.
  - Predator kills per 1 000 animal-ticks fell in seed 1234, rose in seed 7,
    and fell a little in seed 42.

| Ticks (seed 1234) | Mean population | Killed by predators | Starved | Predator kills per 1 000 animal-ticks |
|---|---|---|---|---|
| 0–10 000 | 39 | 717 | 507 | 1.84 |
| 10 000–20 000 | 53 | 688 | 1 017 | 1.31 |
| 20 000–30 000 | 52 | 673 | 1 021 | 1.29 |
| 30 000–40 000 | 53 | 688 | 1 064 | 1.30 |
| 40 000–50 000 | 53 | 679 | 1 026 | 1.27 |

Seed 7: 1.44 in the first 10 000 ticks, 1.53 in the last. Seed 42: 1.49, then 1.33.

### 3.4 Drift

Readings swing widely. Share of the living animals in seed 1234, at the start →
tick 20 000 → the end:

| Slot | Reading | Living share |
|---|---|---|
| flee | flee +0.8, when predator & very close | 27 % → 98 % → 48 % |
| attack | attack −2.5 (never) | 23 % → 81 % → 10 % |
| attack | no effect ("Swim.", "Fly.") | 7 % → 16 % → 76 % |
| wander | "Explore when there is nothing else to do." | 53 % → 4 % → 0 % |
| wander | no effect | 20 % → 96 % → 98 % |
| mate | no effect | 17 % → 46 % → 100 % |
| risk | cautious | 37 % → 100 % → 100 % |
| social | solitary | 17 % → 100 % → 80 % |
| place | familiar | 47 % → 89 % → 100 % |

With about 50 animals, a gene variant can go from rare to everywhere in about
100 generations by chance alone. That's why the "never attack" reading rose to
81 % and fell to 10 % with a fitness of 1.01. By the end of seed 1234, most
follow, wander and mate genes have no effect: their keywords were lost by
mutation, and the keyword brain's defaults took over.

### 3.5 What the genes look like at the end

Most carried texts in seed 1234 (carriers over the whole run):

| Slot | Texts |
|---|---|
| flee | "Flee only when a predator is very close." (founder, 4 311) |
| risk | "Nervous: any sound nearby means danger." (3 164), "Nervous: any movement nearby means a celebration." (1 346) |
| place | "Attached to familiar places." (3 681), "Attached to refrigerator places." (1 449) |
| social | "Alone is preferred by the solitary one." (2 594), "Crowds are preferred by the solitary one." (1 664) |
| eat | "Heavy food is only look for when energy low." (3 431) |
| wander | "Stay running to old places." (3 017) |
| attack | "Swim." (1 093), "Fly." (789) |

Two lineages of genes that most animals carried at the end:

```text
Only look for food when energy is low.            founder
Low energy when food only look for is.            "Mutate this sentence at random."
Low energy when food only look for is heavy.      "Randomly add a word to this sentence."
Heavy food is only look for when energy low.      "Change this sentence randomly."
Heavy food is only sought for when energy low.    "Randomly change one word in this sentence."   45 of 50 animals

Solitary: prefers to be alone.                    founder
Alone is preferred by the solitary one.           "Change this sentence in a random way, big or small."
Crowds are preferred by the solitary one.         "Randomly change the meaning of this sentence a little."
Silence is preferred by the solitary one.         "Change one random detail in this sentence."   40 of 50 animals
```

In the first lineage, losing "is" from "energy is low" removed the condition:
the keyword brain now pushes eating in every situation. A grammar slip changed
behaviour.

## 4. What it means

- **Selection removes harmful genes; it doesn't yet pick a best one.**
  "Always attack", "restless" and losing the cautious temperament are clearly
  worse. No gene is clearly better.
- **Predators are the clearest selective force on temperament.** Restless or
  careless animals die by predation much more often (53–60 % against 43 %).
- **Drift is strong at this population size.** About 50 animals let gene
  variants swing from 25 % to 98 % and back by chance. Leads of 1–2 % are hard
  to see:
  - the interval for *cautious* is ± 0.015 with 22 683 carriers;
  - a 1 % lead needs about 50 000 carriers to be sure of, about seven runs like
    these.
- **The keyword brain reads keywords, not meaning.** Nonsense keeps working
  when the keyword survives: "Never potato.", "Nervous: any movement nearby
  means a celebration.". So this brain can't favour meaningful genes over
  nonsense with the right words; only the LLM brain can. That needs the
  hour-per-5 000-ticks runs (a first one is in §7) and the unsolved gate G2
  ([06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions)).
- **Genes travel together.** Children inherit whole parental gene sets slot by
  slot, so a gene's fitness also reflects the genes it was usually inherited
  with.
- **Readability survives partly.** At the end, 62–79 % of the living genes
  still use a word of the animal's world. The mutation test without selection
  ([04 §5](04-genome-and-evolution.md#5-mutation)) found a similar share after
  about seven mutations, which is the average here (0.03 × 230). So there's no
  clear sign that selection keeps genes meaningful.

## 5. Making selection easier to see

| Lever | Setting | Effect | Cost |
|---|---|---|---|
| More seeds | rerun with other `--seed`, pool them in `gene_report` | more carriers: about seven runs for 1 % leads | 14 min per three runs in parallel |
| Larger population | `agents.cap`, `world.width/height` | drift weakens as the population grows | slower runs; more mutation calls |
| Stronger predation | `predators.count`, `predators.kill_p` | bigger fitness gaps for flee and caution genes | changes the Lab 1 tuning; check the population stays below the cap |
| Gentler mutation | lower `p_mut`, or small-edit instructions only | genes stay readable longer; readings keep their meaning | less variation |
| The LLM brain | `--backend llm` | meaning, not keywords, decides | about 1 hour per 5 000 ticks on a 16 GB GPU |

## 6. Reproduce it, and use it in Lab 1

From `prototype/`, with Ollama running (the mutation needs it):

```bash
for s in 1234 7 42; do
  python -m experiments.smoke_run --profile full --ticks 50000 --seed $s --snapshots 5 \
         --out results/runs/long_$s > logs/long_$s.log 2>&1 &
done; wait                                        # ≈ 14 min; watch with: python -m experiments.status
python -m experiments.gene_report results/runs/long_1234 results/runs/long_7 results/runs/long_42 --tag long_1234
```

`smoke_run --minutes N` stops a run after N minutes of wall-clock time, which
is useful with the slow LLM brain. Mutation answers are cached, so rerunning the
same seeds costs no new model calls.

Questions for students (Lab 1 activity D, [02](02-lab1.md#d-watch-evolution)):

- Run your own seeds: which readings get ▲ or ✗? Do they match the table above?
- Double the predators (`predators.count: 6`): do flee or caution genes earn ★?
- Run with `--no-mutation`: which founder genes win when nothing new appears,
  and is selection easier to see?
- Compare the genes at the end with the mutation test, which has no selection:
  does selection keep genes meaningful?

## 7. One hour with the LLM brain

The keyword brain reads keywords. This run asks what happens when the LLM reads
the genes (`prototype/results/llm_60min_genes.md`, 2026-10-02).

| | |
|---|---|
| World | the Lab 1 flat world, small profile: 48 × 48 cells, 2 predators, at most 40 animals |
| Brain and mutation | gemma4:12b for both (points mode), seed 1234 |
| Length | `smoke_run --backend llm --minutes 60`: 5 269 ticks, 21 generations |
| Cost | 19 211 decisions (5.3 per second), 6 784 LLM calls, no failures, 68 mutations |

### What happened

| Tick | 500 | 1 000 | 2 000 | 3 000 | 3 500 | 4 000 | 4 500 | 5 000 | 5 269 |
|---|---|---|---|---|---|---|---|---|---|
| Animals | 10 | 13 | 11 | 10 | 11 | 17 | 18 | 29 | 25 |
| Newcomers so far | 6 | 16 | 36 | 50 | 65 | 65 | 65 | 65 | 65 |

- **For 3 400 ticks the population sat at the floor of 10.** 65 newcomers
  (fresh founder genomes) kept it alive. As in the first LLM run
  ([06 §5.6](06-experiments-and-results.md#56-first-run-with-the-llm-brain)),
  the founders as the LLM reads them can't sustain a population.
- **Then it took off.** No newcomer was needed after tick 3 437. The last
  1 269 ticks saw 96 births, against 27–41 per 1 000 ticks before, and the
  population reached 25–29 animals.
- **Predators caused 71 % of deaths.** Animals fled in 4.5 % of decisions and
  attacked in 9.5 %. With the keyword brain on the same world, predators cause
  about 53–55 % of deaths, animals flee in about 12 % of decisions and attack
  in about 2 %.

### Who rescued the population

- **Newcomers, not the original lineages.** Four newcomers that arrived
  between ticks 2 982 and 3 228 account for 84 % of the ancestry of the 25
  animals alive at the end. The rescue came from a workable combination of
  founder genes, then breeding.
- **Their descendants converged on these genes** (animals carrying each, out
  of 25):
  - "Never fight." (23);
  - "Only look for food when energy is low." (20);
  - "Look for a partner when energy is high." (18);
  - "Stay near where you last found food." (15);
  - "Follow others when you are lost or hungry." (13);
  - "Rest only when you feel safe." (12);
  - "Solitary: prefers to be alone." (10).
- **"Never fight." looks selected.**
  - Only about 30 % of the survivors' ancestry carried it: one of the four main
    newcomers, plus two minor ancestors.
  - Yet 23 of the 25 survivors carry it, and its share rose from 39 % to 92 %
    while the population grew (ticks 4 000 to 5 269).
  - The keyword-brain runs point the same way: "never attack" ▲, "always
    attack" ✗ (§3.2).
- **The numbers are thin.** With 295 deaths, every gene's interval is ± 0.4 or
  wider, so the report marks nothing as clearly better. This is one seed: a
  lead, not a result.
- **Mutation barely mattered yet.** In 21 generations, only 13 % of the living
  genes are mutants, and 98 % still use a word of the animal's world.

### What it suggests

- **The open question gets a first, partial yes.** The question was whether
  evolution can fix founders that the LLM makes unviable
  ([06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions)).
  Here, recombining founder genes, helped by newcomers, found a viable
  animal, and non-aggression then spread.
- **Mutation and meaning need more time.** At 5.3 decisions per second, an hour
  buys about 5 000 ticks on the small world. Seeing them, and checking this
  run, needs several seeds and runs of several hours, best run in the
  background or on a lab server.

```bash
python -m experiments.smoke_run --backend llm --minutes 60 --seed 1234 --ticks 20000 --snapshots 10 \
       --out results/runs/llm_60min > logs/llm_60min.log 2>&1 &
python -m experiments.gene_report results/runs/llm_60min --min-carriers 25 --every 1000 --tag llm_60min
```
