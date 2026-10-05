# 07 — Decision backends

How genes plus a situation become a probability over the 7 actions: the protocol, the reference backends, the LLM brain via Ollama, and parked Laya (`backends/`, `llm/ollama_client.py`).

## Protocol

`backends/base.py`:

```python
@dataclass(frozen=True)
class Query:
    genome_key: str      # run-independent hash of the gene texts
    genes: dict          # locus -> text
    obs: Observation     # hash = hash((genome_key, obs))

class Backend(Protocol):
    name: str
    def decide(self, queries: list[Query]) -> np.ndarray:
        """probabilities, shape [len(queries), 7], rows sum to 1, columns in ACTIONS order"""
```

- The simulation sends one batch per decision step containing only memo misses
  ([03](03-simulation-loop.md)). A backend may answer the batch however it likes
  (loop, parallel requests, grouping per genome).
- `normalise(p, eps=1e-6)` clips negatives to 0, adds `eps`, renormalises.
  Every backend output passes through it (with its own `eps`), so no action has
  probability exactly 0.
- `N_ACTIONS = 7`.

### Factory

`backends/factory.make_backend(name, cfg, **over)`:

| Name(s) | Backend | Status |
|---|---|---|
| `rule_based` | `RuleBasedBackend()` | ✅ |
| `random` | `RandomBackend()` | ✅ |
| `llm`, `teacher`, `ollama_policy` | `LLMPolicyBackend.from_config(cfg, **over)` | 🧪 |
| `laya` | `LayaBackend.from_config(cfg, placement=…, style=…)` | 💤 |

Unknown names exit with `unknown backend … (random | rule_based | llm | laya)`.
`make_rewriter(cfg, client=None)` returns `(rewriter, model)` for LLM mutation,
or `(None, None)` when `ollama.mutator_model` is unset or the `llm_rewrite`
weight is 0.

## `random`

Uniform `1/7` for every query. Floor baseline: a population with this brain
collapses and survives only through immigrants (STATUS, 2026-09-28).

## `rule_based` — the transparent "ideal interpreter"

`backends/rule_based.py`. No model: sensible default logits plus a keyword
reading of the genes, then softmax. Purpose: instant, deterministic, readable by
students; the fast reference for pipeline tests and control C5. Gibberish and
neutral genes add exactly 0, so they behave identically (tested).

```text
logits[a] = default_logits(obs)[a] + gene_weight(genes[a], a, obs) + temperament_delta[a]
p         = softmax(logits / temperature)          temperature = 1.0, then normalise(eps=1e-6)
```

### 1. Default logits (no genes)

| Action | Logit |
|---|---|
| eat | food: here 3.0, near 2.0, far 1.0, none −2.0; plus energy: low +1.5, medium +0.5, high −1.0 |
| flee | predator: near 3.0, far 0.5, none −3.0 |
| follow | animal: near −0.5, far 0.3, none −2.5 |
| wander | 0.5, +1.0 if food is none |
| rest | −0.5, +0.5 if energy high and no predator |
| mate | 2.5 if animal near **and** it is ready **and** self adult **and** energy not low; else −3.0 |
| attack | −3.0 if no animal near; 0.5 if energy low and the other is not stronger; else −1.0 |

### 2. Action gene weight (`gene_weight`)

Each action gene only moves **its own** action's logit.

1. **Intensity** — first matching pattern in this order wins:

   | Words | Weight |
   |---|---|
   | never, do not, don't, avoid, refuse | −2.5 |
   | rarely, seldom, hardly | −1.2 |
   | always, whatever happens, at all costs | +2.5 |
   | whenever, often, usually, eagerly, quickly | +1.2 |
   | sometimes, occasionally, maybe | +0.3 |

2. **Action words** — if no intensity word: a mild +0.8 if the gene mentions its
   own action (`ACTION_WORDS`, e.g. eat: eat, eating, food, feed, graze, forage;
   flee: flee, run, escape, hide, danger, predator; rest: rest, sleep, stay
   still, save energy, stop). Otherwise the weight is 0 and the gene has no
   effect.
3. **Conditions** — the text before any "unless" is searched for condition
   phrases (`CONDITIONS`), each a predicate on the observation:

   | Phrase (regex) | True when |
   |---|---|
   | hungry, starving, energy is low, low energy, weak; tired | energy low |
   | energy is high, well fed, full, strong, fed | energy high |
   | predator, danger, threat, attack(s/ed) you | predator ≠ none |
   | very close, right next to you, too close | predator near or animal near |
   | food is close/near, food nearby; plentiful | food near or here |
   | food is far/scarce, no food, nothing else to do | food none or far |
   | alone, lost | animal none |
   | safe | predator none |
   | old | age adult |

   If the gene names conditions and **none** holds, the weight is multiplied by
   0.2 (weak effect).
4. **"unless"** — if a condition after "unless" holds, the weight becomes
   −0.5 × weight (mild flip).

Examples (medium energy): "Never eat unless starving." → −2.5, but +1.25 when
energy is low. "Run from any predator you see." → +0.8 with a predator in view,
+0.16 without. Patterns are plain substrings (e.g. `strong` matches
"strongest", `weak` matches "weaker"), and a gene like "Stay calm unless danger
is right next to you." gets −0.4 on `flee` exactly when a predator is near (the
"unless" flip inverts the intended meaning).

### 3. Temperament deltas

All three temperament texts are scanned; every matching pattern adds its deltas
(they accumulate):

| Pattern | Deltas |
|---|---|
| cautious, careful, safety, nervous, shy | flee +1.0, attack −1.0, wander −0.3 |
| bold, brave, risk, reckless | flee −0.8, attack +0.6, wander +0.4 |
| social, group, together, friendly, curious | follow +1.0, mate +0.3 |
| solitary, alone, wary, strangers | follow −1.0, attack +0.2 |
| restless, somewhere new, explor, open ground | wander +1.0, rest −0.5 |
| familiar, attached, home, water | wander −0.6, rest +0.4 |

Conditions are ignored here: "Reckless when starving, careful when fed." matches
both the cautious and bold rows (net flee +0.2, wander +0.1, attack −0.4).

## LLM brain — `LLMPolicyBackend` (`backends/ollama_policy.py`) 🧪

The current design default (doc 05 §0): an instruction-following LLM served by
Ollama reads the genes and the situation directly, no training step.
`TeacherBackend` is the same class with `name = "ollama_policy"`, kept for the
parked distillation pipeline.

### Prompt (`prompts/teacher_v1.md`)

```text
You decide what a wild animal does next in a simple grid world.

Actions:
- eat: go to the nearest visible food and eat it
- flee: run away from the nearest predator
- follow: move toward the nearest other animal
- wander: explore the surroundings
- rest: stay still to save energy
- mate: approach a ready partner to breed
- attack: fight the nearest animal to steal its energy

This animal's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}
Temperament: {temperament}

Situation: {situation}

{ask}
```

| Placeholder | Filled with |
|---|---|
| `{genes}` | one line per action locus: `- eat: "Eat whenever food is close."` |
| `{temperament}` | the three temperament texts, each quoted, space-separated, in order risk, social, place |
| `{situation}` | `render(obs, style)` (V1 by default) |
| `{ask}` | points: "Distribute 100 points across the actions according to how likely this animal is to choose each." · logprobs: "Answer with exactly one word, the action this animal takes now: eat, flee, follow, wander, rest, mate, attack." · ksample: "Answer with the single action this animal takes now." |

Filled example (points, V1):

```text
- eat: "No preference."
- flee: "Run away from anything that attacks you."
…
- attack: "Never fight."
Temperament: "Cautious: safety comes before food." "Social: feels safer in a group." "No particular temperament."

Situation: Energy: low. Food: near. Predator: none. Animal: near, ready to mate, weaker. Age: adult.

Distribute 100 points across the actions according to how likely this animal is to choose each.
```

The sentence "follow them even when they seem unwise" is the key instruction
from plan 08 §A8: without it a model applies its own common sense and ignores
the genes. The action list in the prompt is plain text, separate from the
`ACTIONS` tuple.

### Modes (`policy.mode`)

| Mode | Request | Output → distribution | Cost per uncached situation | When |
|---|---|---|---|---|
| `points` (default) | 1 chat call, JSON schema `POINTS_SCHEMA`, temperature 0, seed 0 | integer 0–100 per action; negatives → 0; `normalise(eps=0.01)` (points need not sum to 100) | 1 request, ~60 output tokens (doc 05) | default |
| `logprobs` | 1 raw chat call, `num_predict: 1`, `logprobs: true`, `top_logprobs: 10`, temperature 0 | first token's top-10 alternatives mapped to actions by prefix (`" Flee"` → flee; ambiguous or too short → ignored); summed probabilities, `normalise(eps=1e-4)`; if mapped mass < 0.5 → **fall back to points** (`logprob_fallbacks += 1`) | 1 request, 1 output token | if the probe shows Ollama returns logprobs (UNVERIFIED) |
| `table` | 1 call per genome for up to `table_k` (8) situations, schema `{s1…sK: POINTS_SCHEMA}`, topped up by **prefetch** | each row as points; a missing or all-zero row is re-asked alone in points mode | fewer requests, but many more generated answers | cloud with tight *request* limits only |
| `ksample` | `k` = `ollama.ksample_k` (8) calls, `CHOICE_SCHEMA` (enum), temperature 0.8, seeds 0…k−1 | counts + 1 (add-one smoothing), normalised | k requests | checks only |

**Prefetch (table mode).** In a simulation, each genome usually misses on one
situation per step. `_top_up` fills the call up to K with the situations seen
most often so far in this run (`_seen` counter) that are not yet cached for that
genome; all K answers are stored. Measured with a fake near-random LLM (small
profile, 2 000 ticks): points made 2 211 LLM queries for 5 349 decisions (0.41
per decision); table + prefetch (K = 8) made 1 185 requests but generated 9 342
answers, about 4× (STATUS, 2026-09-30).

`workers` (`policy.workers`, 2) runs requests in a thread pool; match it to
`OLLAMA_NUM_PARALLEL` or cloud limits. `keep_alive` is fixed at `"30m"`.

### JSON schemas

```text
POINTS_SCHEMA  = {type: object, properties: {eat…attack: {type: integer, minimum: 0, maximum: 100}}, required: all 7}
CHOICE_SCHEMA  = {type: object, properties: {action: {type: string, enum: ACTIONS}}, required: [action]}
table_schema(n)= {type: object, properties: {s1…sn: POINTS_SCHEMA}, required: s1…sn}
```

Passed as Ollama's `format` field (structured output).

### Failure handling

| Failure | Default (`strict=False`) | `strict=True` |
|---|---|---|
| points/ksample call raises (after the client's 3 retries) | `failures += 1`; answer **uniform** 1/7 for this decision only (row listed in `last_fallback`, not cached) | re-raise |
| logprobs call raises or returns no usable logprobs | fall back to points for that query | same |
| table call raises | every row falls back to a single points call; rows whose call also fails are uniform and not cached | same |
| reply is not a JSON object | treated as a failed call; the client does not cache it (and ignores such a reply if one is already cached) | same |

`strict` is used by the parked labelling script so failed labels are not
written. In a simulation a failed call never enters any cache: the backend
reports the row in `last_fallback` (see `backends/base.fallback_rows`), the
`Simulation` uses the uniform answer for that one decision without memoising it
(counted in `summary["fallbacks"]`), and the same situation is asked again the
next time it comes up. Check `failures` / `fallbacks` after every run.

### Caches

```text
policy key = sha256(json(["policy", model, digest(model), prompt_id, mode*, style,
                          genome_key, render(obs, style)]))
prompt_id  = "<prompt file name>:<first 8 hex of sha256(template)>"
mode*      = "points" for table mode (table and points share entries)
```

Lookup order: in-process dict → `cache/policy.sqlite` (when built with
`from_config`). A change of model, model digest, prompt text, mode or style
gives new keys, so stale answers are never reused. Below it, the Ollama client
caches `chat()` calls in `cache/ollama.sqlite`
([08](08-reproducibility-and-data.md)). `digest(model)` comes from
`GET /api/tags`; when the model is not listed (e.g. a cloud endpoint without
tags access) it is `"unknown"`, and model updates on the server are then
invisible to the cache.

## Ollama client (`llm/ollama_client.py`) 🧪

Stdlib only (`urllib`). `OllamaClient(host, cache, timeout, transport, retries=3, api_key)`.

| Method | Endpoint | Cached |
|---|---|---|
| `version()` | `GET /api/version` | no |
| `models()` | `GET /api/tags` (name, 12-char digest) | no |
| `digest(model)` | via `models()`, memoised; `:latest` alias handled | in memory |
| `chat(model, messages, schema, options, keep_alive, use_cache, extra)` | `POST /api/chat`, `stream: false` | yes, `ollama.sqlite` |
| `chat_json(...)` | `chat` + `json.loads` | yes |
| `chat_raw(...)` | `POST /api/chat`, whole response (for logprobs) | no |
| `embed(model, texts)` | `POST /api/embed` | yes |

- **Retries:** up to 3 tries with 1 s, 2 s, 4 s back-off, then `RuntimeError`.
- **Local vs cloud:** `ollama.host` is `http://localhost:11434` by default.
  Cloud models work either through the local server after `ollama signin` (cloud
  model tag, host unchanged) or directly with `ollama.host: https://ollama.com`
  plus a key. Both routes are unverified (S1.3).
- **API key:** read **only** from the environment variable named by
  `ollama.api_key_env` (default `OLLAMA_API_KEY`) in `client_from_config`, and
  sent as `Authorization: Bearer …` only when set. Never write keys into
  configs, files or logs (prototype `CLAUDE.md`).
- `transport(method, path, payload) → dict` can be injected; all tests use fake
  transports.

## Laya backend (parked) 💤

`backends/laya_backend.py`, written from the Laya model card and **unverified**
against the real package. Laya is a ~420 M encoder decision model that takes a
state plus typed questions and returns one distribution per question. Zero-shot
it barely reads instructions (doc 05 §3), which would have required the
distillation pipeline (doc 05 §5); that is why it was parked on 2026-09-30.

| Placement | Genes go… | Options carry |
|---|---|---|
| P1 | action genes as option criteria; state = situation | the genes |
| P2 | all genes in the state (`Instincts: …`) | fixed neutral descriptions |
| P3 | genes in the question instructions | fixed neutral descriptions |
| P4 (config default) | P1 plus `Temperament: …` in the state | the genes |

`extract_probs` looks for per-option probabilities under several key names; if
only `choice` + `confidence` exist, it spreads the rest uniformly and counts
`approx`. Cache: `cache/laya.sqlite`, key `("laya", client_id, placement, style,
genome_key, situation)`. Config section `laya.*` ([10](10-configuration-reference.md)).

## Design context: option A vs option B

From [`../redesign/02`](../redesign/02-assessment.md):

| | Option A — LLM as brain | Option B — LLM as development |
|---|---|---|
| When the model runs | every (uncached) decision | once per agent, at birth |
| Output | action distribution per situation | a typed phenotype (utility weights, thresholds, …) evaluated cheaply every tick |
| Cost | highest; dominated by LLM answers | one call per birth (doc 02: 100–1 000× cheaper) |
| Context sensitivity | full | limited to what the phenotype schema expresses |

The prototype implements **option A only**. Doc 02 recommended B as the
classroom default and A as the advanced track; plan 08 §A2 routes to B if G5
(throughput) fails on CPU. Option B is not built (☐; plan 08 "optional S8").
