# 05 — Genome

Loci, alleles, the allele registry, genome keys and the v1 allele files (`genome.py`, `founder.py`, `data/*.json`). Status ✅; founder texts are a DRAFT pending owner review (H1).

## Loci

Ten loci in a fixed order (`genome.py`):

| # | Locus | Kind | Word cap (mutation guard) |
|---|---|---|---|
| 0 | `eat` | action | 12 |
| 1 | `flee` | action | 12 |
| 2 | `follow` | action | 12 |
| 3 | `wander` | action | 12 |
| 4 | `rest` | action | 12 |
| 5 | `mate` | action | 12 |
| 6 | `attack` | action | 12 |
| 7 | `risk` | temperament | 15 |
| 8 | `social` | temperament | 15 |
| 9 | `place` | temperament | 15 |

```python
ACTION_LOCI = ("eat", "flee", "follow", "wander", "rest", "mate", "attack")
TEMPERAMENT_LOCI = ("risk", "social", "place")
LOCI = ACTION_LOCI + TEMPERAMENT_LOCI
ACTIONS = ACTION_LOCI          # action order used for every probability vector
```

- An **action locus** holds a gene about *one* action, so loci are homologous by
  construction (the `flee` gene is always about fleeing) and per-locus crossover
  is meaningful (risk R4 in [`../redesign/02`](../redesign/02-assessment.md)).
- A **temperament locus** holds a general disposition that may affect several
  actions.
- The word caps come from `evolution.max_action_words` / `max_temperament_words`
  and are enforced by the mutation guard only; founder alleles respect them too
  (checked in `tests/test_genome.py`).

## Allele

`Allele` is a frozen dataclass:

| Field | Meaning |
|---|---|
| `id` | `"<locus>:<n>"`, e.g. `eat:3`; `n` counts per locus in registration order |
| `locus` | one of `LOCI` |
| `text` | the gene text (whitespace collapsed) |
| `origin` | `founder`, `neutral`, `contrast`, `control`, `mutant`, `ood` (or `custom`, the default of `make_genome`) |
| `parent_id` | allele it was mutated from (mutants) |
| `operator` | mutation operator that produced it |
| `model` | mutator model, for `llm_rewrite` only |
| `seed` | seed drawn for the mutation |

`ood` (out-of-distribution, LLM-written novel alleles) is only produced by the
parked `experiments/make_mutants.py`.

## AlleleRegistry

One registry per run (or per experiment) holds every allele seen.

- `add(locus, text, origin, **meta)` collapses whitespace, then **dedupes by
  `(locus, text)`**: if that text already exists at that locus, the existing
  allele is returned and the *first origin is kept*. So a mutation that
  recreates a founder text yields the founder allele, and "No preference." is a
  different allele at each locus.
- `get(id)`, `text(id)`, `by_locus(locus, origin=None)`, `len()`.
- `genes(genome) → {locus: text}` (what backends read).
- `make_genome({locus: text}, origin)` registers texts and builds a genome.
- `dump_jsonl(path)` writes one JSON object per allele (`alleles.jsonl`).

Ids are deterministic given the registration order. With the v1 files,
`AllelePools` registers per locus (in `LOCI` order) the neutral allele first
(`:0`), then the 4 founders (`:1`–`:4`), then the 14 contrast alleles
(`<action>:5` pro, `<action>:6` anti). A fresh run therefore starts with 64
alleles; mutants continue the per-locus numbering.

## Genome

`Genome(alleles: tuple[str, ...])` is frozen: one allele id per locus, in `LOCI`
order; any other length raises `ValueError`. `at(locus)` reads one id;
`replace(locus, id)` returns a **new** genome (nothing mutates in place), which
makes genomes hashable and safe to share between parent and child.

### Genome key

```text
genome_key(g) = sha256( json.dumps([text of each allele in LOCI order], ensure_ascii=False) )[:24]
```

- Built from **texts**, not ids, so it is run-independent: the same genes give
  the same key in any run or registry (`tests/test_genome.py`). Persistent
  caches can therefore be shared across runs.
- Memoised per `Genome` in the registry.
- Used as the decision memo key (with the observation) and in every backend
  cache key.

## Founder pool v1 (`data/founder_pool_v1.json`)

Structure:

```json
{"version": "v1", "note": "Draft for owner review (H1). …",
 "loci": {"eat": {"neutral": "No preference.", "alleles": ["…", "…", "…", "…"]}, …}}
```

Per locus: 4 instinct-style alleles plus 1 neutral allele. `AllelePools.founders[locus]`
lists the 4 founder ids **plus** the neutral id, so a founder genome samples
uniformly among 5 options per locus (5¹⁰ ≈ 9.8 M combinations). The note sets
the style: imperative, plain words, no numbers, action genes ≤ 12 words,
temperament ≤ 15. **Status: DRAFT written by Claude, awaiting owner review
(H1).** Once approved it is meant to be frozen and versioned (v2 for any change).

| Locus | Neutral | Founder alleles |
|---|---|---|
| eat | No preference. | Eat whenever food is close. · Only look for food when energy is low. · Always finish eating before doing anything else. · Eat quickly, then move on. |
| flee | No preference. | Run from any predator you see. · Flee only when a predator is very close. · Stay calm unless danger is right next to you. · Run away from anything that attacks you. |
| follow | No preference. | Stay close to other animals. · Follow others when you are lost or hungry. · Keep your distance from other animals. · Follow the strongest animal nearby. |
| wander | No preference. | Keep moving to new places. · Explore when there is nothing else to do. · Stay near where you last found food. · Roam far when food is scarce. |
| rest | No preference. | Rest when you are tired. · Never stop moving. · Rest only when you feel safe. · Save energy by resting when food is far. |
| mate | No preference. | Look for a partner when energy is high. · Mate with any nearby adult. · Mate only when food is plentiful. · Seek a partner before growing old. |
| attack | No preference. | Never fight. · Attack weaker animals when you are hungry. · Fight anyone who comes too close. · Attack only to defend your food. |
| risk | No particular temperament. | Cautious: safety comes before food. · Bold: take risks when the reward is food. · Nervous: any movement nearby means danger. · Reckless when starving, careful when fed. |
| social | No particular temperament. | Social: feels safer in a group. · Solitary: prefers to be alone. · Curious about other animals. · Wary of strangers, friendly to companions. |
| place | No particular temperament. | Likes open ground where danger is easy to see. · Prefers staying near water. · Attached to familiar places. · Restless: always wants somewhere new. |

Some founder genes refer to things the agent cannot perceive (e.g. "where you
last found food", "defend your food", "strongest", "near water"). An LLM may
still read them; the rule-based backend only reacts to its keywords.

## Contrast pairs (`data/contrast_alleles_v1.json`)

For **directed tests only**, never used as founders. One `pro` and one `anti`
allele per action locus:

| Locus | pro | anti |
|---|---|---|
| eat | Always eat, whatever happens. | Never eat unless starving. |
| flee | Always run away, whatever happens. | Never run away from anything. |
| follow | Always follow other animals. | Never go near other animals. |
| wander | Always keep exploring new ground. | Never wander away from here. |
| rest | Always rest and stay still. | Never rest, keep moving. |
| mate | Always try to mate. | Never mate. |
| attack | Always attack other animals. | Never attack anyone. |

`contrast_pair(locus)` returns two genomes that are the **neutral genome**
except at that locus (pro vs anti), so any behaviour difference is due to that
one gene.

## Control alleles (`data/control_alleles_v1.json`)

"Length-matched random text" for control C4 and gibberish tests (the length
matching is stated in the file's note, not checked by code):

- `shuffled` — 20 word salads, e.g. "Closes mountains spreadsheet at before weekdays."
- `irrelevant` — 20 fluent but irrelevant sentences, e.g. "The museum opens at nine on weekdays."

`AllelePools.control_texts` is the concatenation (40 texts). `sample_control(rng)`
draws one text per locus independently (with replacement; the same text may
appear at several loci) and registers it with origin `control`.

## Genome builders (`AllelePools`)

| Method | Returns | Used by |
|---|---|---|
| `neutral_genome()` | neutral allele at every locus | E1 (reference "neutral"), contrast pairs |
| `sample_founder(rng)` | one of the 5 options per locus, uniform | initial population, immigrants, E1 founders |
| `sample_control(rng)` | random control text per locus | C4 founders/immigrants, E1 random-text set |
| `contrast_pair(locus)` | `(pro, anti)` genomes | E1 directed tests, teacher gate |

`AllelePools(registry, data_dir="data", version="v1")` loads
`founder_pool_<v>.json`, `contrast_alleles_<v>.json` and
`control_alleles_<v>.json`. `Simulation` always constructs it with the default
version; switching to v2 needs a code change or passing `pools=` explicitly.
