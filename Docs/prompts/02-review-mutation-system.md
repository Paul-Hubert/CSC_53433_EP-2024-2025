Review the gene-mutation system (the "modifiers": the operators that change a gene's sentence when a child is born) of the prompt-genome prototype, with one question in mind: **is it as simple as it can be while still giving evolution what it needs?** This is an analysis. Don't change the repository; I'll decide what to implement from your recommendation.

## Context

- Repository root: this directory. Read `prototype/STATUS.md` and `prototype/CLAUDE.md` first.
- Each animal has 10 genes: short English sentences in fixed slots (7 action genes: eat, flee, follow, wander, rest, mate, attack; 3 temperament genes: risk, social, place).
- A brain turns genes plus situation into action probabilities. That is gemma4:12b via local Ollama, or a keyword brain. Children get each gene from one parent or the other, then mutation may change some genes.
- The system becomes **Lab 1** of a Master 2 course. Students should be able to understand the mutation system in about 15 minutes and add or change an operator in one place.
- The course owner's vision (`Docs/redesign/00-vision.md`): genes are free English text, and a small LLM mutates them "without further context". Risk R3 in `Docs/redesign/02-assessment.md`: repeated LLM rewriting may make genes long, bland and alike.

### The current system

Code: `prototype/promptevo/evolution/mutation.py` (166 lines), `configs/base.yaml › evolution`, `prompts/mutate_v1.md`, `backends/factory.py › make_rewriter`, `llm/ollama_client.py › make_rewriter`, `sim.py › Simulation._birth`, `experiments/e1_sensitivity.py › single_edit`. Docs: `Docs/prompt-genome/04-genome-and-evolution.md` §5.

- **Rate:** each gene of each child is mutated with p = 0.03 (≈ 26 % of children get at least one attempt).
- **Operators**, picked by weight:

  | Operator | Weight | What it does |
  |---|---|---|
  | `intensity` | 1 | moves along never/rarely/sometimes/often/always |
  | `negate` | 1 | 6 opposite pairs, else "Do not …" / "Not …" |
  | `condition_swap` | 1 | 8 fixed conditions such as "when hungry" |
  | `synonym` | 1 | a 19-entry word table |
  | `founder_reintroduce` | 0.5 | another founder sentence of the same slot |
  | `llm_rewrite` | 1 | 7 styles, temperature 0.9, seeded, 3 attempts, then falls back to a random word operator |

- **Guards:** `clean` (strip meta-text, keep the first sentence) and `valid` (length ≤ 12 or 15 words, changed, plain characters). `founder_reintroduce` bypasses both.
- LLM rewrites are used only with `--backend llm`. The mutator model is gemma4:26b, which forces model swaps next to the 12b brain on a 16 GB GPU.
- Every mutation is logged (operator, parent allele, new text) and cached, so runs are reproducible.

### Observed problems

1. Blunt word edits, all real outputs:
   - "Rest when you are tired." → "Rest when you are tired when tired." (the condition list doesn't recognise "when you are tired")
   - "Keep moving to new places." → "Stay put to new places."
   - "Attack weaker animals when you are hungry." → "Attack weaker animals when you are hungry when safe."
   - "Cautious: safety comes before food." → "Often cautious: safety comes before food."
2. `synonym` often has nothing to change: 14 of 24 attempts succeeded in one 5 000-tick run.
3. An LLM "invert" can change two things at once: gemma4:12b turned "Eat whenever food is close." into "Avoid eating whenever food is far away.".
4. `Mutator.stats` can count `ok > tried` after an LLM fallback.
5. The rewrite prompt says ≤ 12 words even for temperament genes, whose limit is 15.
6. The concept count is high: 6 operators, their weights, 7 styles, a fallback, 2 guards and per-slot limits.

## What to do

1. **Map it.** Write down the concepts a student must hold to predict what a mutation does: decision points, constants and tables, lines of code. Note where the rules live and how many places a new operator touches.
2. **Measure it**, with small scratch scripts outside the repo. Nothing over 2 minutes; at most 100 new LLM calls with gemma4:12b; never delete `prototype/cache/`.
   - Apply every word operator to all 40 founder sentences + the neutral ones. Tabulate no-op rate, guard rejections, ungrammatical or meaning-breaking outputs (judge them and say how), and length change.
   - Get a small sample of LLM rewrites per style (cached), and judge whether each style does what its name says.
   - Measure behaviour change per operator: d(parent, child) with the existing E1 machinery (`metrics.behaviour_distance`), using the rule-based brain. Add the LLM brain only where already cached or within budget. Is each operator a small step (good for locality, gate G3) or a jump?
   - Optionally run a few rule-based evolution runs (`smoke_run`, seconds each). Track per-slot diversity (`metrics.shannon_diversity`), mean gene length and which operators produced the alleles that spread.
3. **Compare designs.** Cover at least:
   - **A. Fix in place:** keep the operators, repair the rough edges.
   - **B. Trim:** e.g. intensity + negate + founder_reintroduce + LLM rewrite. Which operators earn their place?
   - **C. LLM-centred:** one rewrite prompt with 2–3 styles, plus founder reintroduction. Say what happens offline, without a model.
   - **D. Structured "modifier" genes:** a gene = intensity word + behaviour phrase + optional condition, rendered to a sentence. Mutation changes one field from a short list, and an LLM rewrite of the phrase is optional. This trades free text for clarity, so flag it as an owner decision.
   - Anything better you find.

   Score each design on:
   - simplicity: concepts and lines a student reads;
   - evolvability: locality, diversity, open-endedness, bloat risk;
   - works without a model;
   - determinism and logging;
   - student extensibility;
   - migration cost: code, tests, `single_edit` in E1 and the G3 measurement, configs, `Docs/prompt-genome/04` and `08`.
4. **Recommend** one design: an API sketch (signatures, config block), what's deleted, migration steps, and the experiments that would confirm it works (with time and LLM-call budgets).

## Deliverable

Write `prototype/notes/mutation-review.md` with these sections:

1. Summary (≤ 10 lines)
2. The current system on one page
3. Evidence (tables)
4. Options compared (one table plus short notes)
5. Recommendation
6. Migration and validation plan
7. Questions for the course owner

Show me the summary and the options table, then stop.
