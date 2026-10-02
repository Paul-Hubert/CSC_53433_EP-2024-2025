# Mutation system review: simpler, more random and more universal operators

2026-10-02 · analysis only, no code changed · brief: `Docs/prompts/02-review-mutation-system.md`
plus the owner's request "simpler, more random and universal approaches to gene modification, with
genes having potentially more impact than the obvious choices".

> **Owner decision, 2026-10-02:** none of options A–G. Mutation is one blind operator: the LLM gets one
> instruction drawn from 16 "random change" variants (`prompts/mutate_v2.txt`) plus the gene sentence,
> nothing else, because "evolution and mutation does not care about state and success: pure random".
> Implemented in dda3a61 and measured in `results/mutation_test.md`; see `Docs/prompt-genome/04` §5.
> This note stays as the record of the analysis that came before.

## 1. Summary

- **Today:** 6 operators, 45 hand-written table entries, 7 LLM styles, a fallback chain and 2 guards. That's about 16 rules to predict one mutation, and a new operator touches up to 7 places.
- **The word operators are blunt.** On the founder pool, 5–36 % of their outputs are broken (negate is the worst). With the keyword brain, 33–73 % of their edits change nothing, and broken but harmless genes spread by drift: "Do not no preference." reached 21 copies.
- **The LLM styles jump far.** Rewrites add 2.2 words on average (130 cached 26b rewrites), and about a third are theatrical or about things that don't exist in the world: "They will traverse the entire universe in search of a single crumb."
- **Fully random word edits (DNA-style)** are the simplest and most universal, but unreadable genes take over the population: "You last when you.", "With."
- **Recommended (F), two universal operators:** **borrow** gives a gene the "what" or the "when" part of another gene of the same animal or of a founder sentence (no model, no word tables); **change** is the owner's own prompt, "Make one random change to this sentence" (one-word edits, same length, sometimes a flip: "Flee…" → "Fight…").
- **More impact:** borrow reaches bigger steps than intensity (largest change 0.42 vs 0.13) and carries content across slots. With the LLM brain, a follow sentence copied into the wander slot also moved eat and mate. The keyword brain can't show cross-slot effects, because each action gene only feeds its own action.
- **Cost and trade-off.** About 100 lines instead of 166, and the old word operators become a Lab 1 exercise. In 10 000-tick keyword-brain runs, population size didn't differ between designs (25–28 animals). So the choice rests on simplicity, readable genes and what the LLM brain gets to read.

## 2. The current system on one page

After crossover, each of the child's 10 genes mutates with probability `p_mut` = 0.03. One
mutation:

```text
draw an operator by weight (llm_rewrite weight → 0 without a mutator model)
├─ intensity       ladder never/rarely/sometimes/often/always; ±1 step; no ladder word → prefix Rarely/Often
├─ negate          6 opposite pairs, both directions, in order → else strip "Never/Do not"
│                  → else "Not …" for "Label: …" genes → else prefix "Do not"
├─ condition_swap  8 conditions by substring → replace one, else append one
├─ synonym         19-entry table; no word from the table → no mutation
├─ founder_reintroduce   another founder/neutral sentence of the slot (skips the guards)
└─ llm_rewrite     1 of 7 styles, temperature 0.9, seed; up to 3 attempts (seed + attempt)
                   → all fail: fall back to intensity / negate / condition_swap
clean (LLM answers) → valid (≤ 12 or 15 words by slot, changed, plain characters)
→ registry (same text in the same slot = same allele) → birth event + Mutator.stats
```

| What a student must hold | Where it lives |
|---|---|
| rate; 6 weights; LLM weight dropped without a model | `configs/base.yaml › evolution`, `Mutator.__init__` |
| 4 word-operator rule sets and their tables (LADDER 5, NEGATIONS 6 pairs, CONDITIONS 8, SYNONYMS 19) | `evolution/mutation.py` |
| founder reintroduction, and that it bypasses the guards | `Mutator.mutate_text` |
| LLM prompt, 7 styles, temperature, seed, 3 attempts, fallback list | `prompts/mutate_v1.md`, `LLM_STYLES`, `Mutator`, `llm/ollama_client.make_rewriter` |
| `clean` (5 rules), `valid` (3 rules), 2 word limits (the prompt always says 12) | `mutation.py`, `backends/factory.make_rewriter` |
| LLM mutation only with `--backend llm` | `experiments/smoke_run.py` |

- **Size:** about 16 concepts and about 12 branches per mutation. The code is `mutation.py` (166 lines), plus the prompt (7), the two `make_rewriter` functions (23) and the config (10): about 206 lines.
- **Adding a word operator** touches its function, `WORD_OPS`, a config weight, and docs 04 and 08.
- **An operator that needs more than `(text, rng)`** (the genome, or the founders) also changes `Mutator.mutate_text`.
- **To be used by E1's locality test, or as an LLM fallback,** it must also be added to the hard-coded lists in `e1_sensitivity.single_edit` and in `Mutator.mutate`. That makes 7 places in total.
- **Defects found on the way:**
  - `stats` can show ok > tried after a fallback;
  - the rewrite prompt says 12 words for temperament genes;
  - `founder_reintroduce` skips the guards;
  - both lists above hard-code operator names.

## 3. Evidence

How it was measured is in §3.7. All numbers come from this session.

### 3.1 Word operators on the whole founder pool

Every operator was applied to all 50 founder slots: the 40 founder sentences plus the 10 neutral slots, with 20 seeds each. Every distinct output was judged:

- **OK:** grammatical and coherent as an instinct;
- **awkward:** grammatical but redundant, stacked or odd;
- **broken:** ungrammatical or self-contradictory.

| Operator | No-op | Rejected by guards | Distinct outputs | Δ words | OK / awkward / broken | Typical problem |
|---|---|---|---|---|---|---|
| intensity | 0 % | 0 % | 96 | +0.9 | 68 / 28 / 5 % (n = 80) | "Often no preference.", "Rarely only look for food when energy is low." |
| negate | 0 % | 0 % | 50 | +1.5 | 48 / 17 / **36 %** (n = 42) | "Do not no preference.", "Do not curious about other animals.", "Not cautious: safety comes before food.", "Stay put to new places." |
| condition_swap | 0 % | 0.7 % (too long) | 373 | **+2.4** | 42 / 47 / 11 % (n = 316) | 42 % stack a second condition: "Rest when you are tired when tired.", "…when food is close when food is scarce." |
| synonym | **40 %** | 0 % | 38 | +0.5 | 71 / 21 / 8 % (n = 38) | can change only 30 of 50 slots; "Prefers staying close water." |
| founder_reintroduce | 0 % | (skips guards) | 200 | +0.1 | always a founder sentence | — |

"Distinct outputs" are counted per slot. "n" counts distinct (old, new) pairs: the neutral sentences repeat across slots, so n is smaller.

### 3.2 Universal offline operators, prototyped on the same 50 slots

| Operator (scratch prototype) | Valid | Δ words | OK / awkward / broken (judged sample) | Example |
|---|---|---|---|---|
| splice: swap 0–3 random words with 0–3 words of another gene | 81 % | 0.0 | 7 / 17 / **77 %** (n = 30) | "Eat whenever no close.", "Look for a partner when energy is water." |
| point: substitute, insert or delete 1 word from the founder vocabulary | 99.5 % | 0.0 | 23 / 13 / **63 %** (n = 30) | "Only look for is when energy is low.", "Then fight." |
| gene copy: the whole sentence of another slot | 98 % | −0.1 | always grammatical, always off-slot | wander ← "Run away from anything that attacks you." |
| **borrow** (clause swap: replace the head or the tail with another sentence's) | 95–98 % | +1.7 to +2.1 | **55 / 33 / 12 %** (3 samples, n = 84) | "Roam far when food is scarce." → "Mate when food is scarce." · "Look for a partner when energy is high." → "Never fight when energy is high." · "Flee only when a predator is very close." → "Attack only when a predator is very close." |

The borrow samples come from two variants (donor = the same animal, donor = founder pool) and the final sketch (§5).

- **Main source of "awkward":** a temperament explanation after a colon lands on an action, as in "Never fight: feels safer in a group.".
- **Main source of "broken" in the early variant:** the connector "by", as in "Restless by resting when food is far.". The sketch drops "by" and "then".

### 3.3 LLM rewrites

All 12b samples used temperature 0.9 and a fixed seed.

| Prompt | Model | n | Valid | Δ words | What comes out |
|---|---|---|---|---|---|
| `mutate_v1`, 7 styles (cached from earlier runs) | 26b | 130 | — | **+2.2**, and 26 % reach ≥ 10 words | Sample of 40: 65 % OK, 20 % theatrical ("They descend into a primal, bloodthirsty frenzy the moment prey appears."), 15 % off-topic ("Always keep dancing through the chaos."). Rewrites switch to "It…/They…" descriptions; the same gene was rewritten into "traverse the entire universe…" three times. |
| `mutate_v1`, 7 styles × 3 genes | 12b | 21 | 21 | **+3.1** | 4 of 21 add things the world doesn't have ("small rodents or insects", "a wolf", "dense foliage", "shadows"). Temperament label kept in 2 of 7. invert works on 2 of 3 genes ("Approach any predator you see.", "Bold: food comes before safety."). On the eat gene it flips two things at once: "Stop eating whenever food is far away.". |
| **U1** "Make one random change to this sentence" (no context) | 12b | 12, plus a 6-step chain | all | **0.0** | One-word edits. 4 of 6 change what the gene says: "Flee only…" → "Fight only…", "tired" → "hungry", "Cautious" → "Courageous", "Never fight." → "Never dance.". The other 2 swap an irrelevant word ("cold", "humans"). Not very random: "close" → "cold" for 4 of 6 seeds, and the chain flips cold ↔ hot. |
| U2 = U1 + a random word (4+ letters) from another gene | 12b | 6, plus a 6-step chain | all | +1.2; the chain grew from 5 to 10–11 words | Fluent, brings in other slots: "Keep your distance or you might fight other animals.". Keeps adding words. |
| Code picks the word, the LLM replaces it | 12b | 6 | all | 0.0 | 6 of 6 near-synonyms ("available", "exhausted", "precedes"), so mostly no change in behaviour. |
| "Make one **big** random change" | 12b | 4 | all | — | 3 of 4 off-topic: "Fly a spaceship to Mars.", "The toasted marshmallows tasted like summer memories." |
| Blend: "change A by borrowing a part of B" (B = another gene of the animal) | 12b | 4 | all | +3.5 | 4 of 4 fluent: "Never fight when you are tired.", "Keep your distance from any nearby adult.". |

The wording sets the step size:

- "rewrite" → big, decorated jumps;
- "one random change" → one word;
- "replace word X" → a synonym;
- "big change" → nonsense.

### 3.4 Behaviour change per operator (keyword brain)

Setup:

- 30 founder genomes × 10 slots × 3 seeds per operator;
- d = mean JSD over the 48 E1 situations;
- reference: 435 unrelated founder pairs, median d 0.107, mean 0.114.

| Operator | Neutral (d < 10⁻⁴) | Mean d | 90th pct | Max | Mean d / unrelated |
|---|---|---|---|---|---|
| intensity | 33 % | 0.011 | 0.040 | 0.13 | 0.09 |
| negate | 35 % | **0.037** | 0.095 | 0.42 | 0.33 |
| condition_swap | 64 % | 0.003 | 0.005 | 0.09 | 0.03 |
| synonym | 73 % (plus 41 % no-op) | 0.001 | 0.004 | 0.02 | 0.01 |
| founder_reintroduce | 9 % | 0.015 | 0.041 | 0.16 | 0.14 |
| splice (words) | 73 % | 0.002 | 0.005 | 0.09 | 0.02 |
| point (1 word) | 79 % | 0.002 | 0.003 | 0.21 | 0.02 |
| gene copy | 21 % | 0.014 | 0.031 | 0.33 | 0.12 |
| **borrow**, donor = same animal | 42 % | 0.009 | 0.017 | 0.33 | 0.08 |
| **borrow**, donor = founder pool | 41 % | 0.011 | 0.020 | **0.42** | 0.10 |

- **Every operator is a small step on average.** E1's G3 ratio uses medians: it is ≤ 0.06 for every operator, because most single edits are neutral for this brain, so G3 passes trivially. Report the neutral share and a mean-based ratio as well.
- **Action-gene edits never have an off-target effect with the keyword brain.** This is built in: in every action-gene edit measured, only the edited slot's own score moved (off-target 0.000 in §3.5). An action gene only adds to its own action's score, so a borrowed "predator" phrase in the eat gene changes *when* the animal eats, never how often it flees.
- **The keyword brain reads intensity words whatever the verb.** "Never dance." in the attack slot equals "Never fight.", and "Not cautious:" still matches "cautious".

### 3.5 One-gene edits read by the LLM brain

Setup: gemma4:12b, points mode. One founder genome from `lab1_llm_500`, 8 E1 situations whose parent answers were cached, 40 child decisions (35 of them new model calls). "Off-target" is the JSD between parent and child among the *other* actions after renormalising them; it is 0 when only the edited slot's action moves.

| Edit | LLM d | LLM off-target | Largest Δp (LLM) | Keyword d |
|---|---|---|---|---|
| attack "Never fight." → "Always fight." (negate) | **0.176** | 0.074 | attack +0.21, flee −0.09, eat −0.06 | 0.059 |
| attack "Never fight." → "Never dance." (one irrelevant word) | 0.064 | 0.016 | attack +0.09 (the ban is gone) | 0.000 |
| flee "…right next to you." → "…right behind you." (U1) | 0.020 | 0.022 | eat +0.04, flee −0.03 | 0.000 |
| wander ← "Keep your distance from other animals." (copied from follow) | 0.044 | **0.062** | eat +0.05, wander −0.05, mate +0.03 | 0.013 |
| place "Restless: always…" → "…often…" (intensity) | 0.017 | 0.017 | eat +0.04, flee −0.03 | 0.000 |

- **Effects spread across actions.** With the LLM brain a one-gene edit can move actions other than its slot's, which is the "more impact than the obvious" the owner asked for.
- **Meaningless edits still matter.** Losing a meaning ("Never dance.") also moves behaviour, like a loss-of-function mutation.
- **This is one parent and 8 situations,** so it is an illustration, not a measurement.

### 3.6 Evolution runs (keyword brain, Lab 1 flat world, small profile)

Each run is 10 000 ticks, about 46 generations: 3 seeds at p_mut 0.03, 2 seeds at 0.15. Diversity is the Shannon diversity of alleles per slot (bits), averaged over the run.

| Design | p_mut | Mean pop | Mutant share of living genes (end) | Words per gene, start → end | Diversity | A mutant that spread (copies) |
|---|---|---|---|---|---|---|
| no mutation | 0 | 25.3 | 0 % | 5.4 → 5.3 | 0.54 | — |
| current (no LLM) | 0.03 | 25.6 | 51 % | 5.5 → 6.0 | 1.11 | "Do not no preference." (21), "No preference when hungry." (19) |
| current | 0.15 | 26.6 | 91 % | 5.5 → **7.7** | 2.34 | "Not solitary: prefers to be alone when safe." (17), "Prefers staying close water when threatened." (13) |
| founder only | 0.03 | 25.9 | 0 % | 5.3 → 4.7 | 1.03 | — |
| borrow (same animal / founder pool / both) | 0.03 | 24.9 / 26.0 / 25.7 | 37–50 % | 5.4 → 6.3–6.5 | 1.12–1.15 | eat: "Fight anyone who comes too close whenever food is close." (19); flee: "Restless only when a predator is very close." (26); mate: "Follow the strongest animal nearby only when food is plentiful." (27) |
| borrow (both) | 0.15 | 26.2 | 100 % | 5.7 → **7.7** | 2.50 | "Run from any predator you see, then move on." (10), "Nervous by resting when food is far." (13) |
| splice (words) | 0.03 / 0.15 | 25.1 / 25.6 | 43 / 97 % | 5.5 → 5.2 / 5.8 | 1.09 / 2.43 | "Attached to familiar places you." (18), "You last when you." (10), "Other." (13) |
| point (1 word) | 0.03 / 0.15 | 28.1 / 25.7 | 69 / 100 % | 5.3 → 5.3 / 5.2 | 1.43 / 2.53 | "Away no look." (16), "With." (13), "No." (16) |

- **Population size:** 25–28 for every design, inside the seed ranges (for example 20.8–29.6 without mutation). In Lab 1 the mutation design changes *what text survives*, not yet survival.
- **Drift:** most mutations are neutral for the keyword brain, so whatever an operator writes spreads by drift. Readable output must come from the operator itself, not from selection.
- **Length:** borrow lengthens genes a little faster than the current operators at p_mut 0.03 (+0.9 vs +0.5 words in 46 generations) and about as fast at 0.15 (+2.0 vs +2.2). The word cap holds both. Splice and point don't grow genes.

### 3.7 Method and budgets

- **Scripts:** scratch scripts outside the repo (session scratchpad: `rv.py`, `s1`–`s8`, `mutation_v2_sketch.py`). They import `promptevo` read-only. Ask if they should be kept under `prototype/experiments/`.
- **LLM budget:** 100 new gemma4:12b calls, exactly the brief's budget:
  - 45 rewrites;
  - 15 prompt variants;
  - 4 blends;
  - 36 for the decision test (one rewrite plus 35 child decisions; the other 5 child answers and the parent answers were already cached).
- **Hard cap:** a counter in the client's transport enforced the 100-call limit.
- **Run times:** every script ran in under 30 s. The 32 evolution runs ran in parallel in 25 s.
- **Cache:** `prototype/cache/` was only added to, through normal cached calls.
- **Judgement:** judged by hand with the rubric in §3.1. Condition_swap was judged by rule:
  - appended to a sentence without a condition: OK;
  - stacked on an existing condition: awkward;
  - stacked with a duplicate or opposite condition: broken;
  - appended to a label or neutral gene: awkward.
- **Not measured:**
  - the keyword-brain step size of U1 (no budget left);
  - the LLM brain on more than one parent;
  - evolution with the LLM brain.

## 4. Options compared

| | Design | Simplicity (concepts · lines) | Evolvability (step size, reach, diversity, bloat) | Readable genes | Works without a model | Determinism & logging | Add an operator | Migration | Fits "simple, random, universal, more impact" |
|---|---|---|---|---|---|---|---|---|---|
| A | **Fix in place**: keep 6 operators; replace conditions instead of appending, fix negate grammar and temperament labels, per-slot prompt limit, stats bug | ~18 · ~200 | as today: small steps, negate the biggest; less stacking | better (broken maybe < 10 %) | yes | yes | up to 7 places | small | **no**: more special cases, still the designer's edits |
| B | **Trim**: intensity + negate + founder + LLM | ~11 · ~120 | drops the most neutral operators (synonym 73 %, condition_swap 64 %); keeps the biggest step | negate still 36 % broken | yes | yes | 4–6 places | small | partly: fewer tables, same kind of edits |
| C | **LLM-centred**: one prompt, 2–3 styles + founder | ~8 · ~60 | big, decorated jumps (+2–3 words); bloat risk R3 | mostly; tone drifts | **founder sentences only** (0 % mutants in founder-only runs) | yes (seeded, cached) | 2 places | medium | random and universal online; offline evolution stops inventing |
| D | **Structured "modifier" genes**: intensity + behaviour phrase + condition | ~6 for mutation, plus a new gene format · ~100 + renderer | perfect locality; reach bounded by the lists | always | yes | yes | a list entry | **large** (founders, both brains' input, E1, tests, docs) | **no**: less random and less universal (owner decision) |
| E | **Word-level "DNA"**: point / splice | 3 · ~25 | 73–79 % neutral, rare jumps; no bloat; highest diversity | **no**: "With.", "You last when you." spread | yes | yes | 2 places | small | simple, random, universal; breaks the readable outcome and feeds noise to the LLM brain (G2) |
| **F** | **Borrow + change** (recommended) | **~8 · ~100** | 41 % neutral, mean 0.010, max 0.42; carries meaning across slots; diversity as today; +0.9 words / 46 generations (cap holds) | mostly (55 % OK, 12 % broken); spreading genes are sentences | yes (borrow) | yes; log the donor | **2 places** (function + weight) | medium | **yes** |
| G | **Borrow + LLM blend** (F with "change A by borrowing from B") | ~8 · ~100 | like F, more fluent, +3.5 words per blend (n = 4) | yes (4/4) | yes (borrow) | yes | 2 places | medium | yes; F's alternative, decided by experiment V2 |

- **A** repairs symptoms but keeps the problem the owner points at: a designer picks which edits exist. Every fix adds a rule (for example, "don't prefix Do not to an adjective").
- **B:** synonym and condition_swap earn the least (mostly neutral, most bloat). But the survivors (ladder, negation pairs) are exactly the hand-picked "obvious choices".
- **C** matches the owner's vision best online. Offline (the default keyword brain and student laptops) the only new genes would be founder sentences: Lab 1 evolution would just sort 4 alleles per slot.
- **D** gives the cleanest science (perfect locality, readable) at the price of free text. It's the opposite of the new request, so it's listed for completeness.
- **E** is the most random and universal. It fails on evidence: with neutral drift, nonsense fills the gene pool. It's still a good exercise or control ("what does blind mutation do?").
- **F** recombines what genes already say, across slots, and adds one LLM "point mutation". The randomness comes from the code (donor, part) and from the model (which word).
- **G** is F with a more fluent but longer-growing LLM step. Choose between F and G with experiment V2.

## 5. Recommendation: F, "borrow + change"

**Rules a student needs (one per line):**

1. Each gene of a child mutates with probability `p_mut`.
2. The operator is drawn by weight: `borrow` always; `change` only when a mutator model is set.
3. **borrow** splits sentences at the first connector after the first word (`when whenever unless if before after only until while , :`): *head* = what to do, *tail* = when/how. It takes a donor sentence, half the time another gene of the same animal and otherwise a founder sentence of any slot. Then it replaces either this gene's tail with the donor's tail, or its head with the donor's head. If neither sentence has a tail, the gene becomes the donor sentence.
4. **change** asks the mutator LLM: "Make one random change to this sentence", seeded, temperature 0.9, one attempt.
5. Guards are unchanged (`clean` for LLM answers, `valid`). If the result is invalid, the gene doesn't mutate this time.
6. Each mutation logs the operator, parent allele, new allele, text and donor.

**API sketch** (a full draft of 103 lines with docstrings was tested in the scratchpad; 976 of 1 000 borrow attempts were valid):

```python
CONNECTOR = re.compile(r"\s*(?:[,:]|\b(?:when|whenever|unless|if|before|after|only|until|while)\b)", re.I)

def split(text: str) -> tuple[str, str]          # "Rest when you are tired." -> ("Rest", "when you are tired")

@dataclass
class Context:                                   # everything an operator may look at
    locus: str; genes: dict[str, str]; founders: list[str]; max_words: int; seed: int
    llm: Callable[[str, int, int], str] | None   # llm(text, max_words, seed) -> answer

def borrow(text: str, ctx: Context, rng) -> str | None
def change(text: str, ctx: Context, rng) -> str | None    # None without a model
OPS = {"borrow": borrow, "change": change}       # a new operator = one function here + one weight in the config

class Mutator:
    def __init__(self, cfg, registry, founders, llm=None, llm_model=None)
    def mutate(self, g, rng) -> tuple[Genome, list[dict]]                  # sim._birth: call unchanged
    def mutate_locus(self, g, locus, op, rng) -> tuple[Genome, dict] | None  # one edit, for E1
```

**Config:**

```yaml
evolution:
  sexual: true
  p_mut: 0.03
  shuffled: false
  random_founders: false
  operators:          # weights; "change" is used only when ollama.mutator_model is set
    borrow: 1.0
    change: 1.0
  max_words: 15       # one cap for every gene (question 5); was 12 / 15
```

**Prompt** `prompts/mutate_v2.md`:

```text
Make one random change to this sentence:

"{text}"

Reply with the new sentence only, at most {max_words} words.
```

**What goes away:**

- `LADDER`, `NEGATIONS`, `CONDITIONS`, `SYNONYMS`, `LLM_STYLES`;
- `op_intensity`, `op_negate`, `op_condition_swap`, `op_synonym`, `_sub_once`, `_decap`;
- `founder_reintroduce` (founder sentences stay available as donors);
- the 3-attempt loop, the fallback list, `mutate_text`;
- `max_action_words` / `max_temperament_words`;
- `prompts/mutate_v1.md` (kept in git history).

The four word operators move to a Lab 1 exercise file, ported to the `(text, ctx, rng)` signature with weight 0.

**Why F.**

- **Simpler.** About 8 concepts instead of about 16, no behaviour tables, and one prompt instead of 7 styles. Adding an operator takes one function and one weight, and the stats bug and the 12-word prompt bug disappear.
- **More random and universal.**
  - The same code runs for every slot and both brains.
  - Randomness comes from the donor and the part, not from a designer's list.
  - `change` is the owner's own "make a random change" idea, used as a point mutation.
- **More impact.**
  - Steps reach further than intensity (max 0.42 vs 0.13).
  - Behaviour crosses slots. Genes that spread said things like "Fight anyone who comes too close whenever food is close." (eat slot) or "Follow others only when a predator is very close." (flee slot). With the LLM brain such content moves other actions too.
- **Still readable.** Spreading genes stay sentences, unlike splice and point.
- **Works offline.** Lab 1 on a laptop without a model still gets new genes.

**What F gives up, and the mitigations:**

1. **No exactly targeted edits** such as "never → rarely". Students can add them back as operators: the exercise.
2. **Offline, no new words appear,** only recombination. That's fine for the keyword brain, which reads only its keyword lists, and the founder pool keeps feeding material.
3. **Awkward colon tails** ("Never fight: feels safer in a group."). Accept them, or add one rule: borrow a ":" tail only into "Label:" genes.
4. **Slow lengthening.** Watch the mean length in V1; the cap holds it.
5. **`change` isn't very random for a given sentence** ("cold" in 4 of 6 seeds). V2 checks this; G is the fallback design.
6. **No cross-slot effects with the keyword brain** (question 4).

## 6. Migration and validation plan

**Migration** (about half a day, one commit per step, tests green after each):

1. `evolution/mutation.py`: write borrow, change, `OPS`, `Context`, and the `Mutator` with `mutate` and `mutate_locus`. Log the donor in each event. Add the exercise file with the 4 old word operators at weight 0.
2. `prompts/mutate_v2.md`. `llm/ollama_client.make_rewriter` returns `rewrite(text, max_words, seed)`. `backends/factory.make_rewriter` stops passing a fixed limit.
3. `configs/base.yaml › evolution` gets the block above. Set `ollama.mutator_model` per question 6. `sim.py` and `smoke_run` stay unchanged, because `mutate(g, rng)` keeps its signature.
4. `experiments/e1_sensitivity.single_edit` uses `mut.mutate_locus(g, locus, "borrow", rng)`. The E1 report adds the neutral share and a mean-based locality ratio next to G3's median ratio.
5. Tests:
   - `split` / `join` cases;
   - borrow head, tail and both donor sources;
   - same seed → same result;
   - guard rejections;
   - `ok ≤ tried`;
   - `change` with a fake LLM;
   - without an LLM, borrow only;
   - keep "no mutation → no new alleles".
6. Docs:
   - `Docs/prompt-genome/04` §5 rewritten (rules, real examples from §3, the "step size depends on wording" finding);
   - `08` rows (mutation.py, config, prompts, tests, "add a mutation operator");
   - `02` activity E (compare borrow with an operator you write; measure locality, diversity, length);
   - `09` status;
   - `STATUS.md` decision row.

**Validation** (a pass criterion for each; LLM calls with gemma4:12b on a free GPU):

| Step | What | Budget | Pass if |
|---|---|---|---|
| V1 | Operator table on the 50 founder slots; keyword-brain step sizes; 10 seeds × 10 000 ticks rule-based | ≤ 5 min CPU, 0 calls | ≥ 95 % valid; judged sample of 50 ≤ 15 % broken; max d ≥ 0.2; population within ±2 of the current design; mean words per gene ≤ 7.5 at the end; ≥ 80 % of mutants with ≥ 3 copies readable |
| V2 | `change` (U1) on 40 founder sentences × 4 seeds, plus 2 chains × 10 steps | ≈ 180 calls, ≈ 2 min | ≥ 95 % valid; mean Δ words ≤ +1; ≥ 3 distinct outputs of 4 for at least half the sentences; no drift to off-topic in the chains. If randomness fails, run the same for G (+180 calls) and pick. |
| V3 | G3 on the LLM brain with borrow and change edits: 10 parents × 4 edits × 12 situations | ≈ 600 calls, ≈ 5 min | report neutral share, mean ratio and off-target share next to the keyword brain; no gate change without the owner |
| V4 | 3 seeds × 500 ticks with `--backend llm` | ≈ 760 calls and ≈ 7 min each | 0 failures; mutation events carry donors; final genes readable; compare with `lab1_llm_500` |

Total: about 3 000 LLM calls, about 30 min of GPU. Each run stays short and goes in the background if it takes more than 2 min.

## 7. Questions for the course owner

1. **Design:** is F (free text; borrow + one LLM change) the Lab 1 default? Or do you prefer structured genes (D) despite less randomness?
2. **LLM wording:** keep your original "Make one random change to this sentence" (recommended: same length, small steps with occasional flips)? Or the blend with another gene (G: more variety, longer genes)? Decide after V2?
3. **Cross-slot content:** is it wanted? An eat gene can end up saying "Fight anyone who comes too close whenever food is close". That's where the extra impact comes from, and slots then mean "where the sentence sits" rather than "what it's about".
4. **Keyword brain:** should it read action words in *every* gene, so a borrowed "run from any predator" in the eat gene also raises flee? Today it can't have off-target effects. Changing it touches the brain doc and the E1 references.
5. **Word cap:** one cap for all genes (15 words) instead of 12 / 15?
6. **Mutator model:** gemma4:12b for mutation too, so one model stays in memory with no swaps? The current 7-style prompt produced theatrical text with both 12b and 26b.
7. **Old word operators:** keep them as a Lab 1 exercise (weight 0), not in the default?
8. **Founder reintroduction:** drop it, since founder sentences remain borrow donors and the floor still brings founder immigrants?
9. **Scripts:** keep this review's scratch scripts in the repo (for V1 and V2) or leave them out?
