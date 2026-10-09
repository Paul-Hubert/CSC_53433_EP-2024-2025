# 10 — Mutation

Mutation is blind: it changes a gene at random, without knowing what helps
(CORE-07). Selection alone decides what stays. The prototype mutates sentences
with a small LLM; the contract also covers number genes and rule-based text
mutation, and lets mutation run asynchronously while eggs incubate.

## 1. When and what mutates

- **MUT-01 (MUST)** Each baby, after its crossover, mutates each locus
  independently with probability `rate` (reference 0.03 per gene per baby, so
  about one baby in seven of a five-gene species gets at least one attempt).
- **MUT-02 (MUST)** Exactly one operator applies to a locus: the one configured
  for that gene, or else the species' default for the gene's kind (reference:
  the LLM instruction deck for text genes, a Gaussian rule for number genes).
- **MUT-03 (MUST)** A successful mutation registers a new allele (origin
  `mutant`, parent allele, operator, model, seed; GENE-12) and is recorded in the
  baby's birth event: locus, parent allele, new allele, operator detail, new value.
- **MUT-04 (MUST)** A mutation that fails (every try rejected, or the mutator
  unavailable in non-strict mode) leaves the inherited allele unchanged and is
  counted.
- **MUT-05 (MUST)** Mutation statistics are kept per run: attempts, successes,
  model calls, rejections by reason.

## 2. The LLM instruction deck (reference operator for text genes)

For each mutating gene, one instruction is drawn from a deck and sent to the
mutator model with the sentence, after one fixed context line, and nothing
else.

- **MUT-10 (MUST)** The deck is data: one instruction per line, lines starting
  with `#` ignored. Adding a line adds an instruction; no code changes.
- **MUT-11 (MUST)** The prompt is exactly:

  ```text
  {context line}
  {instruction}

  "{sentence}"

  Reply with the new sentence only.
  ```

  (without the first line when there is no context line). Reference context
  line: *The sentence below is a rule that a wild animal follows.*
- **MUT-12 (MUST)** The answer is cleaned, then checked:
  - cleaning: take the quoted part if there is one; strip quotes and backticks;
    drop a leading "Here is …:", "Modified …:", "New …:", "Rewritten …:"; keep
    the first line, then the first sentence; collapse spaces; end with a period;
    capitalise the first letter;
  - rejected as **invalid** if it has 0 words or more than `maxWords`
    (reference 12), equals the parent ignoring case, or contains characters
    other than letters, digits, spaces and `, . ' ’ ; : ! ? -`;
  - rejected as **unchanged** if only punctuation differs from the parent.
- **MUT-13 (MUST)** A rejected answer is drawn again with a new instruction and a
  new seed, up to `tries` attempts (reference 5). If all are rejected the gene
  doesn't mutate.
- **MUT-14 (MUST)** Mutator answers are cached by (model, prompt, seed,
  temperature), so a repeated run produces the same mutants without model calls.

**Reference deck** (v4, 2026-10-08, small edits to what the rule says):

1. Make the rule in this sentence a little weaker.
2. Change when this rule applies.
3. Add a short condition to this rule.
4. Remove a condition from this rule, or make it simpler.
5. Change how near, how far or how much this rule is about.
6. Make this rule say the opposite.
7. Change one word of this rule into a related word.

**Reference mutator.** A small model on the CPU (qwen3.5:0.8b; gemma4:26b makes
smaller, more meaningful edits but needs about 19 GB of RAM), temperature 1.2,
thinking off. With litters, a 192 × 192 run makes about 400 mutation calls per
1 300 ticks.

**Measured, without selection** (genes mutated again and again): after 10
mutations 50 % of genes still give a usable rule (8 % with the earlier
"random change" deck); after 30, 36 %; 72 % still mention something of the
animal's world. Real examples: "Run from any predator you see." → "Run quickly
from any predator you see."; "Stay close to other animals." → "Stay far from
other animals."

## 3. Other operators

- **MUT-20 (MUST)** Numeric operators draw from the mutation stream and clamp to
  the trait's range. Reference: Gaussian, v′ = clamp(v + N(0, σ)).
- **MUT-21 (MAY)** Rule-based text operators work without a model: move along an
  intensity ladder (never, rarely, sometimes, often, always), negate, swap a
  condition, replace a word by a synonym from a table, bring back another founder
  sentence. The prototype used these until 2026-10-02 and dropped them as blunt
  ("Rest when you are tired." → "Rest when you are tired when tired."); they
  remain useful for runs without any model.
- **MUT-22 (MAY)** An operator may be switched off for one gene (e.g. the neutral
  "No preference." could be exempt; an open owner question).

## 4. Asynchronous mutation and eggs

The mutator takes time. Instead of stopping the world at every birth, a mating
lays eggs whose genomes are completed while they incubate
([11 §3](11-reproduction-and-population.md#3-eggs-and-incubation)).

- **MUT-30 (MUST)** The random choices of a baby's mutations (which loci mutate,
  and for each the instruction and seed of every try) are drawn at conception,
  in a fixed order: babies in conception order, loci in locus order, tries in
  order. They never depend on when answers arrive.
- **MUT-31 (MUST)** Requests to the mutator are batched: all first tries of a
  tick together, then the redraws of the rejected ones as a second round, and so
  on.
- **MUT-32 (MUST)** A baby hatches only with its genome complete. If the answers
  aren't in when it is due to hatch, the tick waits (SPACE-13).
- **MUT-33 (SHOULD)** With an incubation of a few ticks the answers are usually in
  before hatching, so the world never waits for the mutator. With no incubation
  (the prototype) the birth waits in the same tick.
