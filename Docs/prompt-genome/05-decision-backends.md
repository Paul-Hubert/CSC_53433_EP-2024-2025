# 05 — The brain: decision backends

The **brain** (decision backend) turns an animal's genes and its current
situation into a probability for each action of its species: five for prey
animals, three for predators. The simulation then draws the action from those
probabilities ([03 §7](03-world-and-simulation.md#7-decisions)). Both species
use the same brain, each with its own prompt (since 2026-10-07). Code:
`prototype/promptevo/backends/` and `prototype/promptevo/llm/`.

## Contents

1. [The contract](#1-the-contract)
2. [`random`: the null model](#2-random-the-null-model)
3. [`rule_based`: the transparent keyword brain](#3-rule_based-the-transparent-keyword-brain)
4. [`llm`: the LLM brain](#4-llm-the-llm-brain)
5. [Choosing the model](#5-choosing-the-model)
6. [What the LLM brain costs](#6-what-the-llm-brain-costs)
7. [Parked: Laya](#7-parked-laya)

---

## 1. The contract

```python
class Backend(Protocol):
    name: str
    def decide(self, queries: list[Query]) -> np.ndarray: ...   # shape [len(queries), actions of the species]
```

A `Query` holds the `genome_key` (hash of the gene texts), the `genes`
(action → text), the `obs` (an `Observation` for a prey animal, a
`PredatorObservation` for a predator) and the `species`. One batch holds one
species. The columns follow that species' actions: eat, flee, follow, rest,
mate for the prey; hunt, rest, mate for predators (`species.PREY.actions`,
`species.PREDATOR.actions`). Rows sum to 1.

Pick a brain by name with `--backend` or `backend.name` in the config:

| Name | Needs | Speed | Use |
|---|---|---|---|
| `random` | nothing | instant | null model: does behaviour matter? |
| `rule_based` | nothing | ≈ 1 ms per tick | fast reference, CPU-only work, debugging, control C5 |
| `llm` | Ollama + a model | ≈ 0.5 s per new decision (gemma4:12b, desktop GPU) | the real experiment |
| `laya` | parked | — | — |

## 2. `random`: the null model

Every action gets the same probability (1/5 for prey, 1/3 for predators),
whatever the genes. In the Lab 1 world random predators can't sustain
themselves: they stay at their floor of 3 and depend on 83–113 newcomers per
5 000 ticks. Random prey hold 14–16 animals in the small world, against 21–25
with the keyword brain
([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)).
So behaviour matters in this world.

## 3. `rule_based`: the transparent keyword brain

`backends/rule_based.py` is an "ideal interpreter" written to be read. It
computes a score (logit) per action, adds the effect of the genes, and applies
a softmax:

1. **Situation defaults** (`default_logits`), per species:
   - **prey:** eat is higher the nearer the food (here 3.0, adjacent 2.5,
     close 2.0, medium 1.5, far 1.0) and higher when energy is low; with no
     food in sight, eat (which then means searching) gets a fixed 1.5. Flee is
     high when a predator is adjacent or close (3.5, 3.0), weak at medium range
     (1.0) and 0 when it is far. Mate is high only when another animal within
     4 cells is ready, and this animal is adult with energy that isn't low.
   - **predators:** hunt is higher the nearer the prey (adjacent 3.5 … far
     1.0; with no prey in sight, searching gets 1.5) and when energy is low.
     Rest gains 1.0 when energy is high. Mate is high when another predator
     within 4 cells is ready; a well-fed adult also goes looking for a partner
     it sees farther away (0.5). Predators are few, and without this they
     rarely met ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)).
2. **Genes** (`gene_weight`). Each gene adds a weight to its own action:
   - **intensity words:** *never / do not / avoid* −2.5, *rarely* −1.2,
     *always / whatever happens* +2.5, *whenever / often / quickly* +1.2,
     *sometimes* +0.3. With no intensity word, merely mentioning the action
     gives +0.8.
   - **conditions** such as *hungry*, *food is close*, *safe*, *alone* are
     checked against the observation. Each species has its own list: a predator
     reads *prey is close* or *no prey*, a prey animal *food is close* or
     *predator*. An unmet condition keeps only 20 % of the weight.
   - **"unless …":** if the exception holds, the effect flips mildly.

Gibberish and neutral genes contribute nothing. That's why its gene-sensitivity
reference is clean: directed tests 100 % correct, random text has no effect
([06](06-experiments-and-results.md#51-rule-based-reference-e1)). It reads only
a fixed list of keywords. A gene it doesn't understand has no effect, however
meaningful it is to a person.

## 4. `llm`: the LLM brain

`backends/ollama_policy.py` (`LLMPolicyBackend`) asks a local or cloud LLM
served by [Ollama](https://ollama.com).

### The prompt

Each species has its template (`policy.prompt` and `policy.predator_prompt`).
For the prey, `prompts/teacher_v3.md` (since 2026-10-07; `teacher_v2.md` for
the 5-gene genome with the 12-cell vision, `teacher_v1.md` before):

```text
You decide what a wild animal does next in a simple grid world.

Actions:
- eat: go to the nearest visible food and eat it
- flee: run away from the nearest predator
- follow: move toward the nearest other animal
- rest: stay still to save energy
- mate: approach a ready partner to breed
If the chosen action has nothing to act on in sight (no food, predator, animal or
ready partner), the animal searches the surroundings instead.
Animals and predators move at the same speed: at most one cell per step.

This animal's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}

Situation: {situation}

{ask}
```

For predators, `prompts/predator_v1.md` has the same layout:

```text
You decide what a predator does next in a simple grid world.

Actions:
- hunt: chase the nearest visible prey animal; next to it, try to kill and eat it
- rest: stay still to save energy
- mate: approach a ready partner (another predator) to breed
If the chosen action has nothing to act on in sight (no prey or ready partner),
the predator searches the surroundings instead.
Predators and their prey move at the same speed: at most one cell per step.

This predator's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}

Situation: {situation}

{ask}
```

`{genes}` becomes one line per gene (`- eat: "Eat whenever food is close."`,
`- hunt: "Chase any prey you see."`). `{situation}` is the observation text (V1
or V2) with distances in cells, for example `Energy: low. Prey: 2-4 cells
away. Other predator: none within 20 cells. Age: adult.`
([03 §5](03-world-and-simulation.md#5-perception-what-an-animal-knows)). `{ask}`
is the mode's instruction. In points mode that is: *"Distribute 100 points
across the actions according to how likely this animal is to choose each."*
The line after the actions tells the model what the simulation does with an
action that has nothing to act on
([03 §6](03-world-and-simulation.md#6-actions-five-for-prey-three-for-predators)).
The last line of the world description states the equal speed. It is a fact
about the world, like the actions, and says nothing about what is wise.

With founder genes and a typical situation, a full prey prompt has 1 076
characters (967 with `teacher_v2.md` and the old situation text) and a
predator prompt 917. The 10-gene prompt of `teacher_v1.md` was about 290
tokens.

### Points mode (default)

- **Structured output:** the request carries a JSON schema with the actions
  of the species as required integer fields (0–100). Ollama constrains
  generation to that shape.
- **Reproducible:** temperature 0 and a fixed seed (`seed` = 0). Three
  identical requests gave identical answers. Answers can still shift a little
  with the seed or the server's batching: the probe's example below used seed
  1, and the same genes and situation with seed 0 gave flee 48 %, eat 43 %.
  The caches are what make reruns exactly reproducible.
- **Answer → probabilities:** points are clipped at 0, a small 0.01 is added to
  each action, and the vector is divided by its sum.

A real example (gemma4:12b, probe of 2026-10-01). It used the 10-gene genome,
`teacher_v1.md` and the near/far situation text of the time, so the answer
also has wander and attack.
Genes: eat *"Eat whenever food is close."*, flee *"Always run away, whatever
happens."*, follow *"Stay close to other animals."*, wander *"Keep moving to
new places."*, rest *"Rest when you are tired."*, mate *"Look for a partner
when energy is high."*, attack *"Never fight."*, and temperament *"Cautious:
safety comes before food." "Social: feels safer in a group." "Prefers staying
near water."* Situation: *Energy: low. Food: near. Predator: near. Animal:
none. Age: adult.*

```json
{"eat": 25, "flee": 45, "follow": 5, "wander": 5, "rest": 10, "mate": 0, "attack": 0}
```

That's 90 points. Normalised, flee ≈ 50 %, eat ≈ 28 %, rest ≈ 11 %, follow and
wander ≈ 6 % each.

**Why the points often don't add up to 100.** A JSON schema can require five
integers between 0 and 100, but it can't require that they sum to 100. The model
writes the numbers one after another, without thinking first (thinking is
off) and without tracking a running total. In 60 decisions from the gate
sample (2026-10-01, 10 genes), 44 (73 %) summed to exactly 100, 13 to 56–98
and 3 to 0; none went above 100. Normalising makes short totals harmless.

**The exception: all-zero answers.** 3 of 17 answers for random-text genomes
gave every action 0 points. The model seems to read *"Instincts that are
meaningless have no effect"* as "give no points". The normalisation then
turns all zeros into a uniform 1/5 distribution, a random animal rather than a
"no effect" one. This probably exaggerates how much random text changes
behaviour, which is the G2 problem
([06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions)).
A proposed fix, awaiting the course owner's approval: treat an all-zero answer
as "no effect" and use the neutral genome's answer for that situation.

**Failures.** If a call still fails after three attempts, the decision falls back to
the uniform distribution and `failures` is counted. In `strict` mode it raises
an error instead. The Lab 1 LLM run had 0 failures in 761 calls.

### Other modes

`policy.mode` selects one of four modes:

| Mode | How | Status |
|---|---|---|
| `points` | one call per (genome, situation), answer = points | default |
| `table` | one call per genome answers up to `table_k` = 8 situations at once, topped up with the situations seen most often in the run ("prefetch") | fewer requests but 4–6× more generated text (fake-LLM measurement); only worth it when the number of requests is limited, e.g. a cloud plan |
| `ksample` | k = 8 calls at temperature 0.8, each naming one action; the distribution is the count + 1 smoothing | expensive; for checks |
| `logprobs` | one generated token (the action word); the distribution comes from Ollama's `top_logprobs` | **not usable with gemma4**: the first token is "f", shared by *flee* and *follow*, and the top choice gets ≈ 100 % (gap ≈ 18 nats), so there's no graded distribution. It falls back to points automatically. |

### Caching

Each layer avoids asking the same question twice:

| Layer | Where | Key | Lifetime |
|---|---|---|---|
| Decision memo | `Simulation.memo` (memory) | (species, genome key, observation) | one run |
| Policy cache | `cache/policy.sqlite` | model, model digest, prompt file name and hash (one per species), mode, text style, genome key, situation text | across runs |
| Request cache | `cache/ollama.sqlite` | the exact request: model, digest, messages, schema, options | across runs |

Because of the model digest and the prompt hash, a new model version or an
edited prompt never reuses old answers: answers given to `teacher_v2.md` are
not reused with `teacher_v3.md`, and prey and predator answers never mix. Before 2026-10-01 a bug (an empty cache
file counted as "no cache") meant the request cache was never written; the
policy cache was not affected. It's fixed, so reruns of the same genomes in the
same situations now cost nothing.

### Ollama settings that matter

Every request sends `num_ctx` and `think`. The brain's requests also send
`keep_alive`, temperature 0 and seed 0. Mutations use `evolution.temperature`
(1.2) and a seed per mutation; ksample uses temperature 0.8 and seeds 0–7.

| Setting | Value | Why |
|---|---|---|
| `options.num_ctx` | 4096 | Without it Ollama loaded gemma4 with its 256 k default context, which pushed 32 % of the model onto the CPU. Requests with different context sizes also make Ollama reload the model, which took 60–100 s each time. Prompts are under 300 tokens, so 4 k is plenty. |
| `think` | false | gemma4 is a "thinking" model. Without this it reasons at length before answering. |
| `keep_alive` (brain) | 30 min | keeps the model loaded between calls |
| `temperature`, `seed` (brain, points mode) | 0, 0 | reproducible answers |

`policy.workers` (default 2) sends requests in parallel. That only helps if
the Ollama server accepts several at once (environment variable
`OLLAMA_NUM_PARALLEL` on the server) and the model fits in GPU memory.

**Cloud:** set `ollama.host: https://ollama.com` and put the key in the
environment variable named by `ollama.api_key_env` (`OLLAMA_API_KEY`). Never
write the key to a file. Alternatively, run `ollama signin` and use a cloud
model tag through the local server.

## 5. Choosing the model

Measured on 2026-10-01 on an RTX 5080 (16 GB) with Ollama 0.32.0, with the
10-gene genome and `teacher_v1.md` of the time:

| | gemma4:26b | **gemma4:12b** (chosen) |
|---|---|---|
| Download size | 18 GB | 8 GB |
| Fits in 16 GB GPU memory | only just: `ollama ps` showed 100 % GPU at a 4 k context, with no room for a second model | yes, with room to spare |
| Generation speed | 26 tokens/s | 90 tokens/s |
| Time per decision (one call) | ≈ 2.9 s (probe) | ≈ 0.5 s with a free GPU; 1.6–34 s (probe median 6.5 s) while the GPU was shared with a training job |
| Gate: directed sign accuracy | 0.99 | 0.96 |
| Gate: mean ΔP | 0.62 | 0.75 |
| Gate: MI_G founders / random text | 0.244 / 0.314 | 0.250 / 0.297 |
| Gate verdict | G1 ✔, G2 ✘ | G1 ✔, G2 ✘ |

12b reads genes as well as 26b, shifts behaviour more strongly, is about 3×
faster to generate and leaves GPU memory free, so `policy.model` is
`gemma4:12b`. Both fail G2 in the same way
([06](06-experiments-and-results.md#53-decision-model-gate-gemma4-12b-vs-26b)).

**Mutator model.** `ollama.mutator_model` is `gemma4:12b`, the same model as
the brain (decided 2026-10-02). A 26b mutator next to a 12b brain didn't fit in
16 GB, so Ollama swapped models, a minute or more each time. Mutation is the
only change to genes, so every run with mutation needs this model, whatever the
brain ([04 §5](04-genome-and-evolution.md#5-mutation)).

**On another machine:** a smaller GPU (8 GB) needs a smaller model; check that
it fits fully in GPU memory with `ollama ps`. Without a GPU, use the
rule-based brain, a shared lab server, or Ollama Cloud. Whatever the model, run
the probe and the gate before trusting it
([07](07-setup-and-usage.md#4-check-a-new-model)).

## 6. What the LLM brain costs

First end-to-end run with the LLM brain on the Lab 1 world: small profile, seed
1234, 500 ticks, gemma4:12b, free GPU, empty caches (2026-10-01). It used the
10-gene genome of the time, with seven actions including wander and attack.

| Measure | Value |
|---|---|
| Decisions | 1 722 |
| Decisions answered from the memo | 56 % |
| LLM calls | 761 (0 failures) |
| Time in the brain | 425 s of 425 s total, ≈ 0.56 s per call (2 parallel requests) |
| Effective speed | ≈ 4 decisions/s (G5 asks for ≥ 50 on a GPU) |
| Extrapolated 5 000-tick run | roughly 1 hour on this machine |

The same 500 ticks, compared with the rule-based brain:

| | rule-based brain | gemma4:12b brain |
|---|---|---|
| Population at tick 500 | 17 | 10 (at the floor) |
| Births / newcomers | 23 / 0 | 10 / 9 |
| Deaths: predator / starvation | 25 / 5 | 22 / 11 |
| Action shares | eat 40 %, wander 24 %, flee 12 %, follow 9 %, rest 8 %, mate 5 %, attack 2 % | eat 33 %, wander 20 %, **attack 16 %**, follow 9 %, mate 9 %, rest 8 %, flee 5 % |

Read with care: one seed and 500 ticks. As read by gemma4:12b, the founder
genes give animals that attack far more and flee less than the keyword
reading. Attacking costs energy, so these animals starve more, breed less and
need newcomers. Whether evolution can repair this, by selecting genes that the
LLM turns into better behaviour, is the central open question (gate G4).
Attack was removed on 2026-10-07, so these numbers don't describe the current
genome.

**With 5 genes** (2026-10-07, same world and seed, `prompts/teacher_v2.md`):

| | gemma4:12b brain, 5 genes |
|---|---|
| Decisions / LLM calls | 3 016 / 973 (0 failures) |
| Time | 13 minutes, ≈ 0.83 s per call |
| Population | 19–26, 25 at tick 500 |
| Births / newcomers | 31 / 0 |
| Deaths: predator / starvation | 19 / 11 |
| Action shares | eat 28 %, rest 23 %, mate 23 %, follow 22 %, flee 4 % |

One seed and 500 ticks, so only a first sign: as gemma4:12b reads them, the
5-gene founders kept a population without newcomers.

**With genetic predators** (2026-10-07, same world and seed, `teacher_v3.md`
and `predator_v1.md`, vision 20 with distance bands; the brain decides for both
species):

| | 500 ticks | 2 000 ticks |
|---|---|---|
| Questions to the brain: prey / predators | 1 264 / 74 | 4 345 / 255 |
| LLM calls from an empty cache | ≈ 1 340 | ≈ 4 600 (0 failures) |
| Time from an empty cache | ≈ 10 minutes | ≈ 32 minutes (0.41 s per call, 2 parallel requests) |
| Memo hit rate: prey / predators | 0.39 / 0.67 | 0.40 / 0.71 |

The distance bands make more situations distinct: the memo answered about 40 %
of prey decisions, against 68 % in the 5-gene run above. Predators add few
calls: there are only 3–5 of them, and a digesting predator doesn't decide. At
about 2.2 calls per tick after the start, a 5 000-tick run needs roughly
11 000 calls, about 1.3 hours on this machine. What happened in the world:
[06 §5.12](06-experiments-and-results.md#512-first-llm-brain-run-with-genetic-predators).

## 7. Parked: Laya

The first design used **Laya**, a small local "typed decision" model, as the
brain, with a large LLM teaching it to read genes (distillation). On
2026-09-30 the owner decided to drop fine-tuning for now and let the LLM decide
directly. The Laya backend, probes and the distillation dataset pipeline are
kept and tested with fakes, but unused. The analysis is in
`Docs/redesign/05-decision-backend.md`.
