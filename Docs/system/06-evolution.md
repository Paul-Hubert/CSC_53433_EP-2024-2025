# 06 — Evolution

Reproduction, crossover, mutation (word operators and LLM rewrite), implicit selection, immigration and the experimental controls (`sim.py`, `genome.py`, `evolution/mutation.py`). Status ✅ (word operators) · 🧪 (LLM rewrite, fake-tested).

## Reproduction

Triggered in `Simulation._breed()` after all agents have acted (tick order:
[03](03-simulation-loop.md)).

| | Sexual (default, `evolution.sexual: true`) | Asexual (`sexual: false`, control C7) |
|---|---|---|
| Who | two agents that both chose `mate`, both `mate_ready`, Chebyshev distance ≤ 1, neither bred this period | one agent that chose `mate`, `mate_ready`, not bred this period |
| Genome | `crossover_uniform(a, b)` then mutation | parent genome then mutation |
| Energy | each parent pays `child_energy / 2` = 20 | the parent pays 40 |
| Child | `child_energy` = 40, on the first parent's cell, generation `max(parents) + 1`, `parents` = ids | same, one parent id |
| Limit | blocked while population ≥ `cap` | same |

`mate_ready(a)`: `age ≥ maturity` (150) and `energy ≥ mate_energy` (50). Since a
parent needs ≥ 50 and pays at most 40, breeding alone never kills a parent.

## Uniform crossover

```python
def crossover_uniform(a, b, rng):
    pick = rng.random(len(LOCI)) < 0.5
    return Genome(tuple(x if p else y for x, y, p in zip(a.alleles, b.alleles, pick)))
```

Each locus independently takes the allele of parent A or parent B with
probability ½. Because loci are homologous, the child always has exactly one
`flee` gene, one `eat` gene, and so on. Draws come from the `mutation` stream.
Alternatives listed in doc 04 (one-point, locus blocks, dominant/recessive) are
not implemented.

## Mutation

`Mutator(cfg, registry, founders, rewriter=None, rewriter_model=None)`,
called once per birth: `mutate(genome, rng) → (genome, events)`.

```text
for each locus in LOCI order:
    if rng.random() ≥ p_mut: continue                       # p_mut = 0.03
    op   = weighted choice from the operator menu
    seed = rng.integers(2**31)
    new  = mutate_text(locus, old_text, op, rng, seed)
    if new is None and op == "llm_rewrite":                 # LLM failed 3× or produced junk
        op  = random choice of intensity | negate | condition_swap
        new = mutate_text(locus, old_text, op, rng, seed)
    if new is None: continue                                 # no mutation at this locus
    allele = registry.add(locus, new, "mutant", parent_id=old_id, operator=op, seed=seed,
                          model=rewriter_model if op == "llm_rewrite" else None)
    genome = genome.replace(locus, allele.id)
    events.append({locus, parent, child, op, text})
```

With 10 loci and `p_mut = 0.03`, a child gets on average 0.3 mutation attempts.

### Operator menu

Weights come from `evolution.operators` and are normalised. If no rewriter is
given, `llm_rewrite` gets weight 0.

| Operator | Weight | P (with LLM) | P (without) |
|---|---|---|---|
| `intensity` | 1.0 | 0.18 | 0.22 |
| `negate` | 1.0 | 0.18 | 0.22 |
| `condition_swap` | 1.0 | 0.18 | 0.22 |
| `synonym` | 1.0 | 0.18 | 0.22 |
| `founder_reintroduce` | 0.5 | 0.09 | 0.11 |
| `llm_rewrite` | 1.0 | 0.18 | 0 |

The rewriter exists only when `ollama.mutator_model` is set and the
`llm_rewrite` weight is > 0 (`backends/factory.make_rewriter`).

### What each operator does

Examples are actual outputs of the v1 code on founder texts.

| Operator | Rule | Examples |
|---|---|---|
| `intensity` | Find the first ladder word (`never, rarely, sometimes, often, always`) and move one rung up or down (at the ends, the only possible direction). No ladder word: prefix "Rarely" or "Often". | "Sometimes rest." → "Rarely rest." / "Often rest." · "Never fight." → "Rarely fight." · "Eat whenever food is close." → "Rarely eat whenever food is close." |
| `negate` | Swap the first matching pair, in either direction: always/never, seek/avoid, stay close to/keep your distance from, run from/stand up to, follow/ignore, keep moving/stay put. Else: drop a leading "Do not" (a leading "Never" is already caught by the always/never pair); for "Word: …" temperament genes toggle a leading "Not"; else prefix "Do not". | "Always run away." → "Never run away." · "Stay close to other animals." → "Keep your distance from other animals." · "Rest when you are tired." → "Do not rest when you are tired." · "Cautious: safety comes before food." → "Not cautious: safety comes before food." |
| `condition_swap` | If the text contains one of 8 conditions (`when hungry, when full, when threatened, when alone, when food is close, when food is scarce, when tired, when safe`), replace it with another at random. Else append a random condition. | "Never fight." → "Never fight when tired." |
| `synonym` | Replace one word or phrase from a small two-way lexicon (close↔near, run→dash, fight↔attack, rest↔sleep, explore↔roam, others↔other animals, …). Returns nothing when no lexicon word occurs. | "Never fight." → "Never attack." · "Eat whenever food is close." → "Eat whenever something to eat is close." |
| `founder_reintroduce` | Replace with a different founder allele of the same locus (neutral included). The registry returns the existing founder allele, so the "mutant" keeps origin `founder`. | — |
| `llm_rewrite` | Pick a style at random from `random change, invert, exaggerate, soften, add a condition, make more specific, make more general`; ask the mutator model with `prompts/mutate_v1.md`; up to 3 attempts with seeds `seed, seed+1, seed+2`; each answer goes through `clean` and `valid`. | (fake-tested only) |

Word operators are pure text rules, so they can produce awkward sentences that
still pass the guards, e.g. "Keep moving to new places." → (negate) "Stay put to
new places." or "Rest when you are tired." → (condition_swap) "Rest when you are
tired when tired." (the condition list matches "when tired", not "when you are
tired"). The "toggle back" branch of `negate` is never reached: "Not cautious: …"
does not match the `Word:` pattern, so it becomes "Do not not cautious: …".

### `prompts/mutate_v1.md`

```text
Here is a short instruction describing an animal's instinct:

"{text}"

Rewrite it with this kind of change: {style}.
Keep it one plain sentence of at most {max_words} words, about the same behaviour topic.
Reply with the new sentence only.
```

The rewriter (`llm/ollama_client.make_rewriter`) calls `client.chat` with
`seed` and temperature 0.9, through the client's sqlite cache, so the same
`(text, style, seed, model)` gives the same answer across runs. `max_words` in
the prompt is the locus limit: `max_action_words` (12) for action loci,
`max_temperament_words` (15) for temperament loci (the `Mutator` passes it to
any rewriter that accepts a `max_words` keyword). No fitness context is given (vision item 5).

### Guards

`clean(text)` normalises raw output (LLM or word operator):

1. If the text contains a quoted span of ≥ 3 characters, keep only that span.
2. Strip quotes/backticks; drop a leading meta phrase ("Here is…:", "Modified…:",
   "New…:", "Mutated…:", "Rewritten…:").
3. Keep the first line, then the first sentence; collapse whitespace.
4. Add a final "." if missing; capitalise the first letter.

Example (test): `Here is the new sentence: "Eat fast when hungry" and more` →
`Eat fast when hungry.`

`valid(new, old, max_words)` accepts only if: 1 ≤ words ≤ cap (12 action, 15
temperament); different from the parent ignoring case; characters only from
`A–Z a–z 0–9 space , . ' ’ ; : ! ? -`. This character whitelist is the only
"English" check. `founder_reintroduce` skips `valid` (its texts are trusted).

### Mutation statistics and logging

- `Mutator.stats[op] = {tried, ok}`; the run summary keeps operators with
  `tried > 0`. When `llm_rewrite` falls back, `tried` is counted for
  `llm_rewrite` but `ok` for the fallback word operator.
- Every successful mutation is listed in the child's `birth` event as
  `{"locus", "parent", "child", "op", "text"}` and the new allele (with
  `parent_id`, `operator`, `seed`, `model`) is in `alleles.jsonl`. Together they
  give the full text lineage of every gene. Failed attempts are not logged.

## Implicit selection

There is no fitness function. An allele spreads only if its carriers survive
(enough food, avoid predators, avoid starvation by attackers), reach maturity,
keep ≥ 50 energy, choose `mate` next to another agent that also chose `mate`,
and the population is below the cap. Measures such as lifespan, offspring,
food eaten and steals are logged at death for analysis only.

## Immigration floor

When the population drops below `agents.floor` (10), new agents are spawned
from the **founder pool** (not random text) until the floor is met (risk R6 in
doc 02). They are generation 0 and logged as `immigrant`. Too much immigration
can swamp selection; plan 08 §A2 lists "check that immigration isn't swamping
selection" as the first thing to look at if G4 fails.

## Experimental controls

Conditions of the E4 matrix (plan 08 §A10) and how the code switches them on.
There are no `configs/exp_*.yaml` files and no `run_matrix.py` yet (☐); set the
keys through `load_config(profile, overrides=…)` or by editing a config file.

| Condition | Meaning | Switch | Status |
|---|---|---|---|
| C1 FULL | LLM brain, sexual, mixed mutation, selection | `backend.name: llm` (or `--backend llm`), `policy.model`, optionally `ollama.mutator_model` | 🧪 |
| C2 NO-MUT | selection on founder variation only | `evolution.p_mut: 0` | ✅ (test: no new alleles) |
| C3 SHUFFLED | each decision uses a random **other** living agent's genome; genes are inherited but do not affect their carrier (drift only) | `evolution.shuffled: true` | ✅ (test) |
| C4 RANDOM-FOUNDERS | founders, immigrants and `founder_reintroduce` all drawn from control texts | `evolution.random_founders: true` | ✅ (test) |
| C5 RULE-BASED | C1 with the rule-based brain | `backend.name: rule_based` | ✅ |
| C6 ZERO-SHOT | zero-shot Laya | — | dropped (rev. 2026-09-30) |
| C7 ASEXUAL | one parent, copy + mutation (old lab's regime) | `evolution.sexual: false` | ✅ (no dedicated test) |

Notes:

- **C3** happens in `Simulation.decide()`: when `shuffled` is on and more than one
  agent lives, the genome used for the query is drawn (stream `agents`) from the
  other living agents; the observation is still the carrier's own. Reproduction
  still copies the carrier's own genome.
- **C4**: `random_founders` switches the run's single founder source
  (`Simulation.founder_pool`) to `AllelePools.control_pool()`: the initial
  population, immigrants **and** the `founder_reintroduce` operator all draw
  from the control texts, so no real founder gene can enter a C4 run. (All
  control texts are registered at start, so `alleles` starts higher in C4.)
- Plan 08 S6.1 names the third flag `asexual`; in code it is `sexual: false`.
